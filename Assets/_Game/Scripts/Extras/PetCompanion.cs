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
    /// The player's shop pet. It trots after the player, naps on an empty chair when they stand still
    /// for a while (hopping off when a customer wants the seat), and every so often sniffs out some
    /// money (or, now and then, a pearl). Pets and their outfits are bought with pearls.
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

        private enum State { Follow, Nap, Sniff }

        private const float NapAfter = 7f;              // seconds the player stands still before the pet naps
        private static readonly Vector2 SniffEvery = new(40f, 70f);

        private static PetCompanion current;

        private Transform body;
        private State state;
        private float stillTimer;
        private Vector3 lastPlayerSpot;
        private float sniffTimer;
        private float stateTimer;
        private SeatingTable napTable;
        private int napSeat = -1;
        private FloatingText label;
        private Wallet wallet;
        private OfflineEarnings earnings;

        public static Pet Find(string id) => Array.Find(Pets, p => p.Id == id);
        public static Outfit FindOutfit(string id) => Array.Find(Outfits, o => o.Id == id);

        /// <summary>Puts the chosen pet (in its outfit) next to the player, replacing any pet already out.</summary>
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
            if (PlayerMovement.Current != null) lastPlayerSpot = PlayerMovement.Current.position;
        }

        private void OnDestroy()
        {
            if (label != null) Destroy(label.gameObject);
            if (current == this) current = null;
        }

        private void Update()
        {
            Transform player = PlayerMovement.Current;
            if (player == null) return;

            // Is the player standing still?
            if ((player.position - lastPlayerSpot).sqrMagnitude > 0.0025f) stillTimer = 0f;
            else stillTimer += Time.deltaTime;
            Vector3 playerSpot = player.position;

            switch (state)
            {
                case State.Follow: Follow(player); break;
                case State.Nap: Nap(player); break;
                case State.Sniff: Sniff(); break;
            }
            lastPlayerSpot = playerSpot;
        }

        private void Follow(Transform player)
        {
            Vector3 behind = player.position - player.forward * 1.1f + player.right * 0.5f;
            behind.y = 0f;
            if ((behind - transform.position).sqrMagnitude > 144f) transform.position = behind; // fell far behind: catch up
            bool moving = WalkTo(behind, 0.25f);
            Hop(moving);
            if (!moving) Face(player.position);

            if (stillTimer > NapAfter) StartNap();

            sniffTimer -= Time.deltaTime;
            if (sniffTimer <= 0f && !moving)
            {
                state = State.Sniff;
                stateTimer = 1.4f;
            }
        }

        private void StartNap()
        {
            state = State.Nap;
            stateTimer = 0f;
            if (!SeatingTable.TryFindNapSeat(transform.position, 7f, out napTable, out napSeat))
            {
                napTable = null;
                napSeat = -1;
            }
        }

        private void Nap(Transform player)
        {
            bool playerLeft = stillTimer < 0.01f && (player.position - transform.position).sqrMagnitude > 9f;
            bool seatTaken = napTable != null && !napTable.SeatFree(napSeat);
            if (playerLeft || seatTaken || (napTable == null && napSeat >= 0))
            {
                WakeUp();
                return;
            }

            if (napTable != null)
            {
                Transform seat = napTable.Seat(napSeat);
                if (stateTimer <= 0f)
                {
                    if (WalkTo(new Vector3(seat.position.x, 0f, seat.position.z), 0.15f))
                    {
                        Hop(true);
                        return;
                    }
                    stateTimer = 0.01f; // arrived: hop up
                }
                transform.SetPositionAndRotation(seat.position + Vector3.up * 0.45f, seat.rotation); // curled up on the chair
            }
            Hop(false);
            stateTimer += Time.deltaTime;
            body.localScale = new Vector3(1.05f, 0.8f + Mathf.Sin(stateTimer * 2f) * 0.03f, 1.05f); // curled up, breathing
            label.Show(Mathf.Repeat(stateTimer, 1.6f) < 0.8f ? "z" : "z Z", new Color(0.6f, 0.85f, 1f));
        }

        private void WakeUp()
        {
            if (napTable != null) transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
            napTable = null;
            napSeat = -1;
            body.localScale = Vector3.one;
            label.Hide();
            state = State.Follow;
            stillTimer = 0f;
        }

        // Stops, sniffs the floor ("!"), then finds some money, or now and then a pearl.
        private void Sniff()
        {
            stateTimer -= Time.deltaTime;
            body.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 18f) * 6f + 12f, 0f, 0f);
            label.Show("!", new Color(1f, 0.85f, 0.3f));
            if (stateTimer > 0f) return;

            body.localRotation = Quaternion.identity;
            sniffTimer = UnityEngine.Random.Range(SniffEvery.x, SniffEvery.y);
            state = State.Follow;
            if (UnityEngine.Random.value < 1f / 6f)
            {
                Profile.AddPearls(1, "Your pet found a pearl!");
                label.Show("+1 pearl", new Color(1f, 0.88f, 0.55f));
            }
            else
            {
                float rate = earnings != null ? earnings.RatePerMinute : 0f;
                int amount = Mathf.Clamp(Mathf.RoundToInt(rate / 6f), 5, 500);
                if (wallet != null) wallet.Add(amount);
                GameEvents.RaiseCashCollected(transform.position);
                label.Show("+$" + amount, new Color(0.55f, 0.95f, 0.55f));
            }
            Invoke(nameof(HideLabel), 1.5f);
        }

        private void HideLabel()
        {
            if (state != State.Nap && label != null) label.Hide();
        }

        // Steps towards a spot, steering around things like the staff do. False once it's there.
        private bool WalkTo(Vector3 target, float closeEnough)
        {
            Vector3 offset = target - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance <= closeEnough) return false;
            float speed = Mathf.Clamp(distance * 3f, 1.5f, 7.5f);
            StaffSteering.Step(transform, target, speed * Time.deltaTime);
            Face(target);
            return true;
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
