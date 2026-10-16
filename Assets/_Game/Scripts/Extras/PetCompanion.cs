using System;
using BubbleTea.Core;
using BubbleTea.Customers;
using BubbleTea.Economy;
using BubbleTea.Player;
using BubbleTea.Staff;
using UnityEngine;

namespace BubbleTea.Extras
{
    /// <summary>
    /// The shop pet. It roams the shop on its own and goes to say hello to customers: each one it
    /// visits gets back some patience and is happier (likelier to tip, and tips more), and some
    /// give the pet a little tip. Now and then it naps on an empty chair (hopping off when a customer
    /// wants the seat) or sniffs out some money, or a pearl. Pets and outfits are bought with pearls.
    /// </summary>
    public class PetCompanion : MonoBehaviour
    {
        public class Pet
        {
            public string Id;
            public string Name;
            public int Price;
            public Color Fur;
            public Color Belly;
            public Color Ears;
            public bool LongEars;   // bunny
            public bool Patches;    // panda eye patches
            public bool CurlyTail;  // shiba
        }

        public class Outfit
        {
            public string Id;
            public string Name;
            public int Price;
        }

        public static readonly Pet[] Pets =
        {
            new Pet { Id = "cat", Name = "Mango the Cat", Price = 15, Fur = new Color(0.98f, 0.66f, 0.32f), Belly = new Color(1f, 0.93f, 0.82f), Ears = new Color(0.98f, 0.66f, 0.32f) },
            new Pet { Id = "shiba", Name = "Kuma the Shiba", Price = 20, Fur = new Color(0.86f, 0.55f, 0.3f), Belly = new Color(1f, 0.97f, 0.92f), Ears = new Color(0.86f, 0.55f, 0.3f), CurlyTail = true },
            new Pet { Id = "bunny", Name = "Mochi the Bunny", Price = 20, Fur = new Color(0.97f, 0.96f, 0.95f), Belly = new Color(1f, 0.85f, 0.88f), Ears = new Color(0.97f, 0.96f, 0.95f), LongEars = true },
            new Pet { Id = "panda", Name = "Bao the Panda", Price = 30, Fur = new Color(0.97f, 0.97f, 0.97f), Belly = new Color(0.97f, 0.97f, 0.97f), Ears = new Color(0.15f, 0.15f, 0.17f), Patches = true },
        };

        public static readonly Outfit[] Outfits =
        {
            new Outfit { Id = "bow", Name = "Pink Bow", Price = 5 },
            new Outfit { Id = "scarf", Name = "Cozy Scarf", Price = 6 },
            new Outfit { Id = "party", Name = "Party Hat", Price = 6 },
            new Outfit { Id = "crown", Name = "Golden Crown", Price = 10 },
        };

        private enum State { Roam, Visit, Nap, Sniff }

        private const float PatienceBack = 0.25f;               // share of a customer's patience a visit gives back
        private const float Joy = 0.25f;                        // extra happiness from a visit (likelier and bigger tips)
        private const float TipChance = 0.35f;                  // chance a visited customer tips the pet
        private static readonly Vector2 SniffEvery = new(40f, 70f);
        private static readonly Vector2 NapEvery = new(80f, 130f);
        private static readonly Vector2 NapSeconds = new(10f, 16f);
        private static readonly Color Pink = new(1f, 0.55f, 0.7f);

        private static PetCompanion current;

        private Transform body;
        private State state;
        private Vector3 goal;            // where it's trotting to while roaming
        private float pause;             // a little sit-and-look-around between walks
        private float stateTimer;
        private float sniffTimer;
        private float napTimer;
        private float stuckTimer;
        private Vector3 stuckCheck;
        private Customer friend;         // the customer it's visiting
        private bool greeted;
        private SeatingTable napTable;
        private int napSeat = -1;
        private FloatingText label;
        private Wallet wallet;
        private OfflineEarnings earnings;
        private float labelTimer;

        public static Pet Find(string id) => Array.Find(Pets, p => p.Id == id);
        public static Outfit FindOutfit(string id) => Array.Find(Outfits, o => o.Id == id);

        /// <summary>Lets the chosen pet (in its outfit) loose in the shop, replacing any pet already out.</summary>
        public static void Refresh()
        {
            Vector3 spot = current != null ? current.transform.position : Vector3.zero;
            bool hadPet = current != null;
            if (current != null) Destroy(current.gameObject);
            current = null;

            Pet pet = Find(Profile.ActivePet);
            if (pet == null || !Profile.OwnsPet(pet.Id) || PlayerMovement.Current == null) return;
            if (!hadPet) spot = PlayerMovement.Current.position - PlayerMovement.Current.forward * 1.2f;
            spot.y = 0f;

            var root = new GameObject("Pet " + pet.Id);
            root.transform.position = spot;
            current = root.AddComponent<PetCompanion>();
            current.Build(pet, FindOutfit(Profile.ActiveOutfit));
        }

        private void Start()
        {
#if UNITY_2023_1_OR_NEWER
            wallet = FindFirstObjectByType<Wallet>();
            earnings = FindFirstObjectByType<OfflineEarnings>();
#else
            wallet = FindObjectOfType<Wallet>();
            earnings = FindObjectOfType<OfflineEarnings>();
#endif
            label = FloatingText.Create(transform, Vector3.up * 1.1f, 0.035f);
            sniffTimer = UnityEngine.Random.Range(SniffEvery.x, SniffEvery.y);
            napTimer = UnityEngine.Random.Range(NapEvery.x, NapEvery.y);
            pause = 1f;
            goal = transform.position;
        }

        private void OnDestroy()
        {
            if (label != null) Destroy(label.gameObject);
            if (current == this) current = null;
        }

        private void Update()
        {
            if (labelTimer > 0f)
            {
                labelTimer -= Time.deltaTime;
                if (labelTimer <= 0f && state != State.Nap) label.Hide();
            }

            switch (state)
            {
                case State.Roam: Roam(); break;
                case State.Visit: Visit(); break;
                case State.Nap: Nap(); break;
                case State.Sniff: Sniff(); break;
            }
        }

        // ---------------------------------------------------------------- roaming

        private void Roam()
        {
            sniffTimer -= Time.deltaTime;
            napTimer -= Time.deltaTime;

            if (pause > 0f)
            {
                pause -= Time.deltaTime;
                Hop(false);
                if (pause > 0f) return;
                PickNext();
                return;
            }

            bool moving = WalkTo(goal, 0.3f, 2.4f);
            Hop(moving);
            if (!moving || Stuck())
            {
                pause = UnityEngine.Random.Range(1f, 3f);
                if (sniffTimer <= 0f)
                {
                    state = State.Sniff;
                    stateTimer = 1.4f;
                }
            }
        }

        // What next: say hello to a customer, take a nap, or trot somewhere in the shop.
        private void PickNext()
        {
            if (napTimer <= 0f && SeatingTable.TryFindNapSeat(transform.position, 30f, out napTable, out napSeat))
            {
                state = State.Nap;
                stateTimer = -1f; // walking to the chair
                return;
            }

            Customer pick = PickCustomer();
            if (pick != null && UnityEngine.Random.value < 0.65f)
            {
                friend = pick;
                greeted = false;
                stateTimer = 10f; // gives up if it can't reach them in time
                state = State.Visit;
                return;
            }

            goal = RandomSpot();
            ResetStuck();
        }

        // The nearest customer the pet hasn't met yet.
        private Customer PickCustomer()
        {
            Customer best = null;
            float bestDistance = float.MaxValue;
            foreach (Customer customer in Customer.Active)
            {
                if (customer == null || !customer.CanBePetted) continue;
                float distance = (customer.transform.position - transform.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = customer;
                    bestDistance = distance;
                }
            }
            return best;
        }

        // Somewhere in the shop: beside a bar, a chair or the counter, a little off to one side.
        private Vector3 RandomSpot()
        {
            Vector3 spot = transform.position;
            int pickFrom = UnityEngine.Random.Range(0, 3);
            if (pickFrom == 0 && Cooking.Station.All.Count > 0)
            {
                Cooking.Station station = Cooking.Station.All[UnityEngine.Random.Range(0, Cooking.Station.All.Count)];
                if (station != null) spot = station.ZoneCenter;
            }
            else if (pickFrom == 1 && SeatingTable.All.Count > 0)
            {
                SeatingTable table = SeatingTable.All[UnityEngine.Random.Range(0, SeatingTable.All.Count)];
                if (table != null) spot = table.Seat(UnityEngine.Random.Range(0, 2)).position;
            }
            else if (PlayerMovement.Current != null)
            {
                spot = PlayerMovement.Current.position; // comes to see what you're up to
            }
            spot += new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f));
            spot.y = 0f;
            return spot;
        }

        // ---------------------------------------------------------------- visiting customers

        private void Visit()
        {
            stateTimer -= Time.deltaTime;
            if (friend == null || !friend.isActiveAndEnabled || stateTimer <= 0f || (!greeted && !friend.CanBePetted))
            {
                EndVisit();
                return;
            }

            Vector3 toPet = transform.position - friend.transform.position;
            toPet.y = 0f;
            Vector3 beside = friend.transform.position + (toPet.sqrMagnitude > 0.01f ? toPet.normalized : Vector3.back) * 0.8f;
            beside.y = 0f;
            bool moving = WalkTo(beside, 0.25f, 3.5f);
            Hop(moving);
            if (moving && !greeted) return;

            Face(friend.transform.position);
            if (!greeted)
            {
                greeted = true;
                stateTimer = 2.5f; // stays for a cuddle
                friend.PetVisit(PatienceBack, Joy);
                Say("\u2665", Pink, 2.5f); // a heart

                if (UnityEngine.Random.value < TipChance)
                {
                    float rate = earnings != null ? earnings.RatePerMinute : 0f;
                    int tip = Mathf.Clamp(Mathf.RoundToInt(rate / 20f), 2, 200);
                    if (wallet != null) wallet.Add(tip);
                    GameEvents.RaiseTipped(transform.position, tip);
                    Say("\u2665 +$" + tip, Pink, 2.5f);
                }
            }
            // A happy wiggle while they say hello.
            body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 14f) * 8f);
        }

        private void EndVisit()
        {
            friend = null;
            body.localRotation = Quaternion.identity;
            state = State.Roam;
            pause = UnityEngine.Random.Range(0.5f, 1.5f);
        }

        // ---------------------------------------------------------------- napping

        private void Nap()
        {
            if (napTable == null || !napTable.SeatFree(napSeat))
            {
                WakeUp(); // a customer wants the chair
                return;
            }

            Transform seat = napTable.Seat(napSeat);
            if (stateTimer < 0f)
            {
                bool moving = WalkTo(new Vector3(seat.position.x, 0f, seat.position.z), 0.2f, 2.4f);
                Hop(moving);
                if (moving && !Stuck()) return;
                stateTimer = UnityEngine.Random.Range(NapSeconds.x, NapSeconds.y); // hops up and curls up
            }

            transform.SetPositionAndRotation(seat.position + Vector3.up * 0.45f, seat.rotation);
            Hop(false);
            stateTimer -= Time.deltaTime;
            body.localScale = new Vector3(1.05f, 0.8f + Mathf.Sin(Time.time * 2f) * 0.03f, 1.05f); // curled up, breathing
            label.Show(Mathf.Repeat(Time.time, 1.6f) < 0.8f ? "z" : "z Z", new Color(0.6f, 0.85f, 1f));
            if (stateTimer <= 0f) WakeUp();
        }

        private void WakeUp()
        {
            if (napTable != null && stateTimer >= 0f)
                transform.position = new Vector3(transform.position.x, 0f, transform.position.z) + transform.forward * 0.5f;
            napTable = null;
            napSeat = -1;
            body.localScale = Vector3.one;
            label.Hide();
            napTimer = UnityEngine.Random.Range(NapEvery.x, NapEvery.y);
            state = State.Roam;
            pause = 0.5f;
        }

        // ---------------------------------------------------------------- finding money

        // Stops, sniffs the floor ("!"), then finds some money, or now and then a pearl.
        private void Sniff()
        {
            stateTimer -= Time.deltaTime;
            body.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 18f) * 6f + 12f, 0f, 0f);
            label.Show("!", new Color(1f, 0.85f, 0.3f));
            if (stateTimer > 0f) return;

            body.localRotation = Quaternion.identity;
            sniffTimer = UnityEngine.Random.Range(SniffEvery.x, SniffEvery.y);
            state = State.Roam;
            if (UnityEngine.Random.value < 1f / 6f)
            {
                Profile.AddPearls(1, "Your pet found a pearl!");
                Say("+1 pearl", new Color(1f, 0.88f, 0.55f), 1.5f);
            }
            else
            {
                float rate = earnings != null ? earnings.RatePerMinute : 0f;
                int amount = Mathf.Clamp(Mathf.RoundToInt(rate / 6f), 5, 500);
                if (wallet != null) wallet.Add(amount);
                GameEvents.RaiseCashCollected(transform.position);
                Say("+$" + amount, new Color(0.55f, 0.95f, 0.55f), 1.5f);
            }
        }

        // ---------------------------------------------------------------- moving

        private void Say(string text, Color color, float seconds)
        {
            label.Show(text, color);
            labelTimer = seconds;
        }

        // Steps towards a spot, steering around things like the staff do. False once it's there.
        private bool WalkTo(Vector3 target, float closeEnough, float speed)
        {
            Vector3 offset = target - transform.position;
            offset.y = 0f;
            if (offset.magnitude <= closeEnough) return false;
            StaffSteering.Step(transform, target, speed * Time.deltaTime);
            Face(target);
            return true;
        }

        // True if it hasn't got anywhere for a few seconds (something's in the way): try somewhere else.
        private bool Stuck()
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer < 3f) return false;
            bool stuck = (transform.position - stuckCheck).sqrMagnitude < 0.25f;
            ResetStuck();
            return stuck;
        }

        private void ResetStuck()
        {
            stuckTimer = 0f;
            stuckCheck = transform.position;
        }

        private void Face(Vector3 target)
        {
            Vector3 look = target - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), 540f * Time.deltaTime);
        }

        private void Hop(bool moving)
        {
            float y = moving ? Mathf.Abs(Mathf.Sin(Time.time * 12f)) * 0.08f : 0f;
            body.localPosition = new Vector3(0f, y, 0f);
        }

        // ---------------------------------------------------------------- model

        private void Build(Pet pet, Outfit outfit)
        {
            body = new GameObject("Body").transform;
            body.SetParent(transform, false);
            Color dark = new(0.12f, 0.1f, 0.1f);
            Color pink = new(1f, 0.6f, 0.65f);

            ModelKit.Part(body, PrimitiveType.Sphere, new Vector3(0f, 0.3f, 0f), new Vector3(0.38f, 0.32f, 0.55f), pet.Fur);
            ModelKit.Part(body, PrimitiveType.Sphere, new Vector3(0f, 0.25f, 0.06f), new Vector3(0.3f, 0.22f, 0.4f), pet.Belly);
            foreach (float x in new[] { -0.11f, 0.11f })
            foreach (float z in new[] { -0.15f, 0.15f })
                ModelKit.Part(body, PrimitiveType.Cylinder, new Vector3(x, 0.08f, z), new Vector3(0.1f, 0.08f, 0.1f), pet.Patches ? dark : pet.Fur);

            Transform head = new GameObject("Head").transform;
            head.SetParent(body, false);
            head.localPosition = new Vector3(0f, 0.55f, 0.24f);
            ModelKit.Part(head, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.36f, 0.32f, 0.32f), pet.Fur);
            ModelKit.Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.06f, 0.13f), new Vector3(0.16f, 0.11f, 0.1f), pet.Belly);
            ModelKit.Part(head, PrimitiveType.Sphere, new Vector3(0f, -0.03f, 0.18f), new Vector3(0.05f, 0.035f, 0.03f), dark);
            foreach (float x in new[] { -0.08f, 0.08f })
            {
                if (pet.Patches)
                    ModelKit.Part(head, PrimitiveType.Sphere, new Vector3(x, 0.03f, 0.13f), new Vector3(0.1f, 0.12f, 0.05f), dark, new Vector3(0f, 0f, x > 0 ? -20f : 20f));
                ModelKit.Part(head, PrimitiveType.Sphere, new Vector3(x, 0.03f, 0.155f), new Vector3(0.05f, 0.06f, 0.03f), pet.Patches ? Color.white : dark);
                ModelKit.Part(head, PrimitiveType.Sphere, new Vector3(x * 1.5f, -0.04f, 0.13f), new Vector3(0.06f, 0.03f, 0.02f), pink);

                if (pet.LongEars)
                    ModelKit.Part(head, PrimitiveType.Capsule, new Vector3(x, 0.27f, -0.02f), new Vector3(0.08f, 0.16f, 0.05f), pet.Ears, new Vector3(0f, 0f, x > 0 ? -8f : 8f));
                else if (pet.Patches)
                    ModelKit.Part(head, PrimitiveType.Sphere, new Vector3(x * 1.6f, 0.15f, 0f), new Vector3(0.1f, 0.1f, 0.06f), pet.Ears);
                else
                    ModelKit.Part(head, PrimitiveType.Cube, new Vector3(x * 1.3f, 0.15f, 0f), new Vector3(0.08f, 0.08f, 0.04f), pet.Ears, new Vector3(0f, 0f, 45f));
            }

            // Tail: a curl for the shiba, a fluffy ball for the bunny and panda, a raised tail for the cat.
            if (pet.CurlyTail)
                ModelKit.Part(body, PrimitiveType.Sphere, new Vector3(0f, 0.48f, -0.25f), new Vector3(0.16f, 0.16f, 0.12f), pet.Belly);
            else if (pet.LongEars || pet.Patches)
                ModelKit.Part(body, PrimitiveType.Sphere, new Vector3(0f, 0.32f, -0.28f), new Vector3(0.12f, 0.12f, 0.12f), pet.Patches ? dark : Color.white);
            else
                ModelKit.Part(body, PrimitiveType.Capsule, new Vector3(0f, 0.45f, -0.3f), new Vector3(0.06f, 0.16f, 0.06f), pet.Fur, new Vector3(-30f, 0f, 0f));

            if (outfit == null) return;
            switch (outfit.Id)
            {
                case "bow":
                    ModelKit.Part(head, PrimitiveType.Sphere, new Vector3(0.1f, 0.15f, 0.04f), new Vector3(0.1f, 0.07f, 0.04f), pink, new Vector3(0f, 0f, 25f));
                    ModelKit.Part(head, PrimitiveType.Sphere, new Vector3(0.18f, 0.12f, 0.04f), new Vector3(0.1f, 0.07f, 0.04f), pink, new Vector3(0f, 0f, -25f));
                    break;
                case "scarf":
                    ModelKit.Part(body, PrimitiveType.Cylinder, new Vector3(0f, 0.43f, 0.18f), new Vector3(0.3f, 0.04f, 0.26f), new Color(0.85f, 0.3f, 0.3f));
                    ModelKit.Part(body, PrimitiveType.Cube, new Vector3(0.08f, 0.36f, 0.3f), new Vector3(0.06f, 0.14f, 0.03f), new Color(0.85f, 0.3f, 0.3f));
                    break;
                case "party":
                    ModelKit.Part(head, PrimitiveType.Cylinder, new Vector3(0f, 0.24f, 0f), new Vector3(0.12f, 0.1f, 0.12f), new Color(0.45f, 0.65f, 1f));
                    ModelKit.Part(head, PrimitiveType.Cylinder, new Vector3(0f, 0.33f, 0f), new Vector3(0.07f, 0.06f, 0.07f), new Color(0.45f, 0.65f, 1f));
                    ModelKit.Part(head, PrimitiveType.Sphere, new Vector3(0f, 0.41f, 0f), new Vector3(0.06f, 0.06f, 0.06f), new Color(1f, 0.85f, 0.3f));
                    break;
                case "crown":
                    Color gold = new(1f, 0.82f, 0.25f);
                    ModelKit.Part(head, PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0f), new Vector3(0.18f, 0.04f, 0.18f), gold);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * 72f * Mathf.Deg2Rad;
                        ModelKit.Part(head, PrimitiveType.Cube, new Vector3(Mathf.Sin(a) * 0.08f, 0.26f, Mathf.Cos(a) * 0.08f), new Vector3(0.03f, 0.07f, 0.03f), gold);
                    }
                    ModelKit.Part(head, PrimitiveType.Sphere, new Vector3(0f, 0.22f, 0.09f), new Vector3(0.035f, 0.035f, 0.02f), new Color(0.9f, 0.2f, 0.35f));
                    break;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => current = null;
    }
}
