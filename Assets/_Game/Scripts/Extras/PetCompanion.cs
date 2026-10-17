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
    /// The shop pet. It roams the dining room on its own and goes to say hello to customers: each
    /// one it visits gets back some patience and is happier (likelier to tip, and tips more), and
    /// some give the pet a little tip. Now and then it goes through the staff door for a nap on the
    /// staff sofa, or sniffs out some money, or a pearl. Pets are bought with pearls.
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
            public bool Upright;    // stands on two legs, chibi style (the panda)
            public float Size = 1f; // the cat is the smallest; the panda is the biggest
            public float PatienceBack;  // share of a visited customer's patience it gives back
            public float Joy;           // extra happiness from a visit (likelier and bigger tips)
            public float TipChance;     // chance a visited customer tips the pet
        }

        // Cat and shiba help customers the least, the bunny a bit more, the panda the most.
        public static readonly Pet[] Pets =
        {
            new Pet { Id = "cat", Name = "Mango the Cat", Price = 25, Fur = new Color(0.98f, 0.66f, 0.32f), Belly = new Color(1f, 0.93f, 0.82f), Ears = new Color(0.98f, 0.66f, 0.32f),
                PatienceBack = 0.15f, Joy = 0.15f, TipChance = 0.25f },
            new Pet { Id = "shiba", Name = "Kuma the Shiba", Price = 25, Fur = new Color(0.86f, 0.55f, 0.3f), Belly = new Color(1f, 0.97f, 0.92f), Ears = new Color(0.86f, 0.55f, 0.3f), CurlyTail = true, Size = 1.3f,
                PatienceBack = 0.15f, Joy = 0.15f, TipChance = 0.25f },
            new Pet { Id = "bunny", Name = "Mochi the Bunny", Price = 30, Fur = new Color(0.97f, 0.96f, 0.95f), Belly = new Color(1f, 0.85f, 0.88f), Ears = new Color(0.97f, 0.96f, 0.95f), LongEars = true, Size = 1.15f,
                PatienceBack = 0.25f, Joy = 0.25f, TipChance = 0.35f },
            new Pet { Id = "panda", Name = "Bao the Panda", Price = 50, Fur = new Color(0.97f, 0.97f, 0.97f), Belly = new Color(0.97f, 0.97f, 0.97f), Ears = new Color(0.1f, 0.1f, 0.12f), Patches = true, Upright = true, Size = 1.5f,
                PatienceBack = 0.35f, Joy = 0.4f, TipChance = 0.5f },
        };

        private enum State { Roam, Visit, Nap, Sniff }

        private const float DoorApproach = 1.2f;                // meters either side of the staff door to line up on
        private static readonly Vector2 SniffEvery = new(40f, 70f);
        private static readonly Vector2 BreakEvery = new(40f, 70f);
        private static readonly Vector2 BreakSeconds = new(15f, 25f);
        private static readonly Color Pink = new(1f, 0.55f, 0.7f);

        private static PetCompanion current;

        private Transform body;          // the whole model: bobs, sits back and lies down
        private float size = 1f;         // the pet's scale
        private Pet kind;
        private Transform head;
        private Transform tail;
        private readonly Transform[] legs = new Transform[4]; // front left, front right, back left, back right
        private Vector3 lastPosition;
        private float stride;            // walk cycle, advanced by distance walked
        private float idleTime;          // seconds standing still
        private float sit;               // 0 standing .. 1 sitting
        private float lie;               // 0 .. 1 lying down for a nap
        private State state;
        private Vector3 goal;            // where it's trotting to while roaming
        private float pause;             // a little sit-and-look-around between walks
        private float stateTimer;
        private float sniffTimer;
        private float breakTimer;
        private float stuckTimer;
        private Vector3 stuckCheck;
        private Customer friend;         // the customer it's visiting
        private bool greeted;
        private Transform napSeat;       // its spot on the staff sofa during a break
        private bool napping;            // arrived and asleep
        private FloatingText label;
        private Wallet wallet;
        private OfflineEarnings earnings;
        private float labelTimer;

        // The staff door between the dining room and the kitchen: the only way through.
        private Transform staffDoor;
        private float kitchenSide = 1f;  // +1 if the kitchen is north of the door (bigger z), -1 if south
        private StaffSofa sofa;

        /// <summary>Where the pet is and how many parts it has (shown at the bottom of the PETS window).</summary>
        public static string Report => current == null
            ? "no pet out"
            : $"pet at ({current.transform.position.x:0.0}, {current.transform.position.z:0.0}), {current.GetComponentsInChildren<MeshRenderer>().Length} parts";

        public static Pet Find(string id) => Array.Find(Pets, p => p.Id == id);

        /// <summary>Lets the chosen pet loose in the shop, replacing any pet already out.</summary>
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
            current.Build(pet);
        }

        private void Start()
        {
#if UNITY_2023_1_OR_NEWER
            wallet = FindFirstObjectByType<Wallet>();
            earnings = FindFirstObjectByType<OfflineEarnings>();
            sofa = FindFirstObjectByType<StaffSofa>();
            BubbleTea.Building.SwingDoor[] doors = FindObjectsByType<BubbleTea.Building.SwingDoor>(FindObjectsSortMode.None);
#else
            wallet = FindObjectOfType<Wallet>();
            earnings = FindObjectOfType<OfflineEarnings>();
            sofa = FindObjectOfType<StaffSofa>();
            BubbleTea.Building.SwingDoor[] doors = FindObjectsOfType<BubbleTea.Building.SwingDoor>();
#endif
            foreach (BubbleTea.Building.SwingDoor door in doors)
            {
                if (door.name == "Staff Door" || staffDoor == null) staffDoor = door.transform;
            }
            if (staffDoor != null && sofa != null) kitchenSide = sofa.transform.position.z >= staffDoor.position.z ? 1f : -1f;

            label = FloatingText.Create(transform, Vector3.up * (0.3f + (kind != null && kind.Upright ? 1.05f : 0.8f) * size), 0.035f);
            sniffTimer = UnityEngine.Random.Range(SniffEvery.x, SniffEvery.y);
            breakTimer = UnityEngine.Random.Range(15f, 30f); // a first nap soon after it arrives
            pause = 1f;
            goal = transform.position;
        }

        private void OnDestroy()
        {
            if (napSeat != null) StaffSofa.FreeSeat(napSeat);
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
            Animate();
        }

        // ---------------------------------------------------------------- the dining room and the kitchen

        private bool InKitchen(Vector3 spot) => staffDoor != null && (spot.z - staffDoor.position.z) * kitchenSide > 0f;

        // Where to head next on the way to a spot: straight there in the same room, otherwise
        // lined up with the staff door on this side, then through it to the other side.
        private Vector3 NextStep(Vector3 target)
        {
            if (staffDoor == null || InKitchen(transform.position) == InKitchen(target)) return target;
            Vector3 door = staffDoor.position;
            door.y = 0f;
            float here = Mathf.Sign(transform.position.z - door.z);
            if (here == 0f) here = 1f;
            Vector3 approach = door + new Vector3(0f, 0f, here * DoorApproach);
            Vector3 through = door - new Vector3(0f, 0f, here * DoorApproach);
            Vector3 offset = approach - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude < 0.25f || Mathf.Abs(transform.position.x - door.x) < 0.4f && Mathf.Abs(transform.position.z - door.z) < DoorApproach
                ? through
                : approach;
        }

        // ---------------------------------------------------------------- roaming the dining room

        private void Roam()
        {
            sniffTimer -= Time.deltaTime;
            breakTimer -= Time.deltaTime;

            if (pause > 0f)
            {
                pause -= Time.deltaTime;
                if (pause > 0f) return;
                PickNext();
                return;
            }

            bool moving = WalkTo(goal, 0.3f, 2.4f);
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

        // What next: say hello to a customer (most of the time), go for a nap in the kitchen
        // now and then, or trot somewhere in the dining room.
        private void PickNext()
        {
            if (breakTimer <= 0f)
            {
                StartBreak();
                return;
            }

            Customer pick = PickCustomer();
            if (pick != null && UnityEngine.Random.value < 0.8f)
            {
                friend = pick;
                greeted = false;
                stateTimer = 12f; // gives up if it can't reach them in time
                state = State.Visit;
                ResetStuck();
                return;
            }

            goal = DiningSpot();
            ResetStuck();
        }

        // The nearest customer in the dining room the pet hasn't met yet.
        private Customer PickCustomer()
        {
            Customer best = null;
            float bestDistance = float.MaxValue;
            foreach (Customer customer in Customer.Active)
            {
                if (customer == null || !customer.CanBePetted || InKitchen(customer.transform.position)) continue;
                float distance = (customer.transform.position - transform.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = customer;
                    bestDistance = distance;
                }
            }
            return best;
        }

        // Somewhere in the dining room: by a table or in front of the counter, a little off to one side.
        private Vector3 DiningSpot()
        {
            Vector3 spot = transform.position;
            if (UnityEngine.Random.value < 0.6f && SeatingTable.All.Count > 0)
            {
                SeatingTable table = SeatingTable.All[UnityEngine.Random.Range(0, SeatingTable.All.Count)];
                if (table != null) spot = table.Seat(UnityEngine.Random.Range(0, 2)).position;
            }
            else
            {
                ServingCounter counter = ServingCounter.FindLane("Main");
                if (counter != null) spot = counter.QueuePosition(UnityEngine.Random.Range(0, 3));
            }
            spot += new Vector3(UnityEngine.Random.Range(-1.2f, 1.2f), 0f, UnityEngine.Random.Range(-1.2f, 1.2f));
            spot.y = 0f;
            if (InKitchen(spot) && staffDoor != null) spot.z = staffDoor.position.z - kitchenSide * 2f; // keep it on the dining side
            return spot;
        }

        // ---------------------------------------------------------------- visiting customers

        private void Visit()
        {
            stateTimer -= Time.deltaTime;
            if (friend == null || !friend.isActiveAndEnabled || stateTimer <= 0f || (!greeted && (!friend.CanBePetted || Stuck())))
            {
                EndVisit();
                return;
            }

            Vector3 toPet = transform.position - friend.transform.position;
            toPet.y = 0f;
            Vector3 beside = friend.transform.position + (toPet.sqrMagnitude > 0.01f ? toPet.normalized : Vector3.back) * 0.8f;
            beside.y = 0f;
            bool moving = WalkTo(beside, 0.25f, 3.5f);
            if (moving && !greeted) return;

            Face(friend.transform.position);
            if (!greeted)
            {
                greeted = true;
                stateTimer = 2.5f; // stays for a cuddle
                friend.PetVisit(kind.PatienceBack, kind.Joy);
                Say("\u2665", Pink, 2.5f); // a heart

                if (UnityEngine.Random.value < kind.TipChance)
                {
                    float rate = earnings != null ? earnings.RatePerMinute : 0f;
                    int tip = Mathf.Clamp(Mathf.RoundToInt(rate / 20f), 2, 200);
                    if (wallet != null) wallet.Add(tip);
                    GameEvents.RaiseTipped(transform.position, tip);
                    Say("\u2665 +$" + tip, Pink, 2.5f);
                }
            }
        }

        private void EndVisit()
        {
            friend = null;
            state = State.Roam;
            pause = UnityEngine.Random.Range(0.5f, 1.5f);
        }

        // ---------------------------------------------------------------- breaks in the kitchen

        // Off through the staff door for a nap on the staff sofa, if a seat on it is free (else later).
        private void StartBreak()
        {
            napSeat = StaffSofa.TakeSeat(this);
            if (napSeat == null)
            {
                breakTimer = 15f;
                goal = DiningSpot();
                return;
            }
            state = State.Nap;
            napping = false;
            stateTimer = UnityEngine.Random.Range(BreakSeconds.x, BreakSeconds.y);
            ResetStuck();
        }

        private Vector3 NapSpot()
        {
            if (napSeat != null) return napSeat.position;
            if (sofa != null) return sofa.transform.position + sofa.transform.forward * 1f;
            return transform.position;
        }

        private void Nap()
        {
            Vector3 spot = NapSpot();
            if (!napping)
            {
                Vector3 floor = new(spot.x, 0f, spot.z);
                bool moving = WalkTo(floor, 0.25f, 2.6f);
                    if (moving && !Stuck()) return;
                napping = true; // hops up and curls up
            }

            if (napSeat != null) transform.SetPositionAndRotation(spot + Vector3.up * 0.2f, napSeat.rotation);
            stateTimer -= Time.deltaTime;
            body.localScale = new Vector3(1.05f, 0.8f + Mathf.Sin(Time.time * 2f) * 0.03f, 1.05f) * size; // curled up, breathing
            label.Show(Mathf.Repeat(Time.time, 1.6f) < 0.8f ? "z" : "z Z", new Color(0.6f, 0.85f, 1f));
            if (stateTimer <= 0f) WakeUp();
        }

        private void WakeUp()
        {
            if (napSeat != null)
            {
                transform.position = new Vector3(transform.position.x, 0f, transform.position.z) + transform.forward * 0.5f;
                StaffSofa.FreeSeat(napSeat);
            }
            napSeat = null;
            napping = false;
            body.localScale = Vector3.one * size;
            label.Hide();
            breakTimer = UnityEngine.Random.Range(BreakEvery.x, BreakEvery.y);
            state = State.Roam;
            goal = DiningSpot(); // back to the dining room
            pause = 0f;
            ResetStuck();
        }

        // ---------------------------------------------------------------- finding money

        // Stops, sniffs the floor ("!"), then finds some money, or now and then a pearl.
        private void Sniff()
        {
            stateTimer -= Time.deltaTime;
            label.Show("!", new Color(1f, 0.85f, 0.3f));
            if (stateTimer > 0f) return;

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

        // Steps towards a spot (through the staff door if it's in the other room), steering around
        // things like the staff do. False once it's there.
        private bool WalkTo(Vector3 target, float closeEnough, float speed)
        {
            Vector3 offset = target - transform.position;
            offset.y = 0f;
            if (offset.magnitude <= closeEnough) return false;
            Vector3 step = NextStep(target);
            StaffSteering.Step(transform, step, speed * Time.deltaTime);
            Face(step);
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

        // Legs step in diagonal pairs as it walks, the body bobs, the head nods and the tail wags.
        // Standing still for a moment it sits down (back legs folded, head up), and on a break it
        // lies down. Sniffing puts its nose to the floor; meeting a customer it sits and wags hard.
        private void Animate()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 moved = transform.position - lastPosition;
            moved.y = 0f;
            lastPosition = transform.position;
            float speed = moved.magnitude / dt;
            bool walking = speed > 0.3f && speed < 20f && !napping;
            if (walking)
            {
                stride += moved.magnitude * 9f;
                idleTime = 0f;
            }
            else idleTime += dt;

            bool cuddle = state == State.Visit && greeted;
            float sitGoal = !walking && state != State.Sniff && state != State.Nap && (idleTime > 1.2f || cuddle) ? 1f : 0f;
            sit = Mathf.MoveTowards(sit, sitGoal, dt * 3f);
            lie = Mathf.MoveTowards(lie, napping ? 1f : 0f, dt * 2.5f);

            // Sitting tips the body back around the hips; lying lowers it to the floor. The panda
            // stands up: it sits down on its bottom and naps on its back.
            bool upright = kind != null && kind.Upright;
            float pitch = upright ? -12f * sit - 75f * lie : -28f * sit;
            Vector3 hips = upright ? new Vector3(0f, 0.2f, 0f) : new Vector3(0f, 0.14f, -0.2f);
            Quaternion tilt = Quaternion.Euler(pitch, 0f, 0f);
            Vector3 position = hips - tilt * hips;
            float bob = walking ? Mathf.Abs(Mathf.Sin(stride)) * 0.035f : 0f;
            position.y += bob - (upright ? 0.14f * sit + 0.05f * lie : 0.12f * lie);
            body.localPosition = position * size;
            body.localRotation = tilt;

            // Legs: a trot while walking; sitting folds the back legs and straightens the front ones.
            float swing = walking ? Mathf.Sin(stride) * 32f : 0f;
            for (int i = 0; i < 4; i++)
            {
                if (legs[i] == null) continue;
                bool front = i < 2;
                bool pairA = i == 0 || i == 3;
                float angle = pairA ? swing : -swing;
                if (upright)
                {
                    // Front "legs" are its arms; sitting puts the legs out in front.
                    angle = Mathf.Lerp(angle, front ? -20f : -80f, sit);
                    angle = Mathf.Lerp(angle, front ? 0f : -10f, lie);
                    legs[i].localRotation = Quaternion.Euler(angle, 0f, 0f);
                    continue;
                }
                angle = Mathf.Lerp(angle, front ? 28f : -62f, sit);
                angle = Mathf.Lerp(angle, front ? -80f : 80f, lie);
                legs[i].localRotation = Quaternion.Euler(angle, 0f, 0f);
                legs[i].localScale = new Vector3(1f, front ? 1f + 0.75f * sit : 1f, 1f);
            }

            // The panda waves hello to a friend, and now and then while it stands about.
            if (upright && legs[1] != null && lie < 0.5f && (cuddle || !walking && Mathf.Repeat(Time.time, 7f) < 1.8f))
                legs[1].localRotation = Quaternion.Euler(0f, 0f, 140f + Mathf.Sin(Time.time * 10f) * 20f);

            // Head: nods with each step, sniffs the floor, looks around now and then, tilts at a friend.
            if (head != null)
            {
                float nod = walking ? Mathf.Sin(stride * 2f) * 5f : 0f;
                float look = 0f, tiltSide = 0f;
                if (state == State.Sniff) nod = 38f + Mathf.Sin(Time.time * 18f) * 6f;
                else if (cuddle)
                {
                    nod = -10f;
                    tiltSide = Mathf.Sin(Time.time * 3f) * 12f;
                }
                else if (!walking) look = Mathf.Sin(Time.time * 0.8f) * 35f * Mathf.Clamp01(Mathf.Sin(Time.time * 0.35f) * 2f);
                nod += 22f * sit + 25f * lie;
                head.localRotation = Quaternion.Euler(nod, look, tiltSide);
            }

            // Tail: an easy wag, faster when walking, a happy blur with a friend, still when asleep.
            if (tail != null)
            {
                float rate = cuddle ? 20f : walking ? 11f : 5f;
                float size = cuddle ? 40f : walking ? 22f : 15f;
                tail.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * rate) * size * (1f - lie), 0f);
            }
        }

        // ---------------------------------------------------------------- model

        private void Build(Pet pet)
        {
            body = new GameObject("Body").transform;
            body.SetParent(transform, false);
            kind = pet;
            size = Mathf.Max(1f, pet.Size);
            body.localScale = Vector3.one * size;
            lastPosition = transform.position;
            Color dark = new(0.12f, 0.1f, 0.1f);
            Color pink = new(1f, 0.6f, 0.65f);

            if (pet.Upright)
            {
                BuildPanda(pet);
                return;
            }

            ModelKit.Part(body, PrimitiveType.Sphere, new Vector3(0f, 0.32f, 0f), new Vector3(0.38f, 0.32f, 0.55f), pet.Fur);
            ModelKit.Part(body, PrimitiveType.Sphere, new Vector3(0f, 0.27f, 0.06f), new Vector3(0.3f, 0.22f, 0.4f), pet.Belly);

            // Legs hang from hip joints so they can swing and fold; a round paw at the bottom of each.
            Color legColor = pet.Patches ? dark : pet.Fur;
            int index = 0;
            foreach (float z in new[] { 0.15f, -0.15f })
            foreach (float x in new[] { -0.11f, 0.11f })
            {
                Transform hip = new GameObject(index < 2 ? "FrontLeg" : "BackLeg").transform;
                hip.SetParent(body, false);
                hip.localPosition = new Vector3(x, 0.22f, z);
                ModelKit.Part(hip, PrimitiveType.Cylinder, new Vector3(0f, -0.1f, 0f), new Vector3(0.09f, 0.1f, 0.09f), legColor);
                ModelKit.Part(hip, PrimitiveType.Sphere, new Vector3(0f, -0.2f, 0.02f), new Vector3(0.11f, 0.06f, 0.13f), pet.Patches ? dark : pet.Belly);
                legs[index++] = hip;
            }

            // The head turns on a neck joint just above the chest.
            head = new GameObject("Head").transform;
            head.SetParent(body, false);
            head.localPosition = new Vector3(0f, 0.5f, 0.2f);
            Transform face = new GameObject("Face").transform;
            face.SetParent(head, false);
            face.localPosition = new Vector3(0f, 0.07f, 0.06f);
            ModelKit.Part(face, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.36f, 0.32f, 0.32f), pet.Fur);
            ModelKit.Part(face, PrimitiveType.Sphere, new Vector3(0f, -0.06f, 0.13f), new Vector3(0.16f, 0.11f, 0.1f), pet.Belly);
            ModelKit.Part(face, PrimitiveType.Sphere, new Vector3(0f, -0.03f, 0.18f), new Vector3(0.05f, 0.035f, 0.03f), dark);
            foreach (float x in new[] { -0.08f, 0.08f })
            {
                if (pet.Patches)
                    ModelKit.Part(face, PrimitiveType.Sphere, new Vector3(x, 0.03f, 0.13f), new Vector3(0.1f, 0.12f, 0.05f), dark, new Vector3(0f, 0f, x > 0 ? -20f : 20f));
                ModelKit.Part(face, PrimitiveType.Sphere, new Vector3(x, 0.03f, 0.155f), new Vector3(0.05f, 0.06f, 0.03f), pet.Patches ? Color.white : dark);
                ModelKit.Part(face, PrimitiveType.Sphere, new Vector3(x * 1.5f, -0.04f, 0.13f), new Vector3(0.06f, 0.03f, 0.02f), pink);

                if (pet.LongEars)
                    ModelKit.Part(face, PrimitiveType.Capsule, new Vector3(x, 0.27f, -0.02f), new Vector3(0.08f, 0.16f, 0.05f), pet.Ears, new Vector3(0f, 0f, x > 0 ? -8f : 8f));
                else if (pet.Patches)
                    ModelKit.Part(face, PrimitiveType.Sphere, new Vector3(x * 1.6f, 0.15f, 0f), new Vector3(0.1f, 0.1f, 0.06f), pet.Ears);
                else
                    ModelKit.Part(face, PrimitiveType.Cube, new Vector3(x * 1.3f, 0.15f, 0f), new Vector3(0.08f, 0.08f, 0.04f), pet.Ears, new Vector3(0f, 0f, 45f));
            }

            // Tail on a joint at the rump: a curl for the shiba, a fluffy ball for the bunny and panda,
            // a raised tail for the cat.
            tail = new GameObject("Tail").transform;
            tail.SetParent(body, false);
            tail.localPosition = new Vector3(0f, 0.38f, -0.25f);
            if (pet.CurlyTail)
                ModelKit.Part(tail, PrimitiveType.Sphere, new Vector3(0f, 0.1f, 0f), new Vector3(0.16f, 0.16f, 0.12f), pet.Belly);
            else if (pet.LongEars || pet.Patches)
                ModelKit.Part(tail, PrimitiveType.Sphere, new Vector3(0f, -0.04f, -0.04f), new Vector3(0.12f, 0.12f, 0.12f), pet.Patches ? dark : Color.white);
            else
                ModelKit.Part(tail, PrimitiveType.Capsule, new Vector3(0f, 0.08f, -0.06f), new Vector3(0.06f, 0.16f, 0.06f), pet.Fur, new Vector3(-30f, 0f, 0f));
        }

        // A chibi panda standing up: a big round white head with round black ears, black eye patches
        // with shiny eyes, pink cheeks and a little smile; a white tummy, black arms and legs.
        private void BuildPanda(Pet pet)
        {
            Color white = pet.Fur;
            Color black = pet.Ears;
            Color pink = new(1f, 0.68f, 0.72f);

            ModelKit.Part(body, PrimitiveType.Sphere, new Vector3(0f, 0.36f, 0f), new Vector3(0.36f, 0.4f, 0.32f), white);
            ModelKit.Part(body, PrimitiveType.Sphere, new Vector3(0f, 0.5f, -0.01f), new Vector3(0.38f, 0.14f, 0.33f), black); // shoulders

            // Arms (legs[0], legs[1]) hang from the shoulders; legs (legs[2], legs[3]) from the hips.
            var joints = new[] { new Vector3(-0.17f, 0.5f, 0f), new Vector3(0.17f, 0.5f, 0f), new Vector3(-0.09f, 0.2f, 0f), new Vector3(0.09f, 0.2f, 0f) };
            for (int i = 0; i < 4; i++)
            {
                Transform joint = new GameObject(i < 2 ? "Arm" : "Leg").transform;
                joint.SetParent(body, false);
                joint.localPosition = joints[i];
                if (i < 2) ModelKit.Part(joint, PrimitiveType.Capsule, new Vector3(0f, -0.11f, 0.02f), new Vector3(0.11f, 0.12f, 0.11f), black);
                else
                {
                    ModelKit.Part(joint, PrimitiveType.Cylinder, new Vector3(0f, -0.08f, 0f), new Vector3(0.13f, 0.08f, 0.13f), black);
                    ModelKit.Part(joint, PrimitiveType.Sphere, new Vector3(0f, -0.17f, 0.03f), new Vector3(0.15f, 0.08f, 0.18f), black);
                }
                legs[i] = joint;
            }

            head = new GameObject("Head").transform;
            head.SetParent(body, false);
            head.localPosition = new Vector3(0f, 0.6f, 0.02f);
            Transform face = new GameObject("Face").transform;
            face.SetParent(head, false);
            face.localPosition = new Vector3(0f, 0.2f, 0f);
            ModelKit.Part(face, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.5f, 0.44f, 0.42f), white);
            foreach (float x in new[] { -1f, 1f })
            {
                ModelKit.Part(face, PrimitiveType.Sphere, new Vector3(x * 0.19f, 0.17f, -0.03f), new Vector3(0.15f, 0.15f, 0.08f), black); // ear
                ModelKit.Part(face, PrimitiveType.Sphere, new Vector3(x * 0.1f, 0.01f, 0.18f), new Vector3(0.12f, 0.15f, 0.06f), black, new Vector3(0f, 0f, x * 28f)); // patch
                ModelKit.Part(face, PrimitiveType.Sphere, new Vector3(x * 0.095f, 0.025f, 0.205f), new Vector3(0.06f, 0.07f, 0.03f), white); // eye
                ModelKit.Part(face, PrimitiveType.Sphere, new Vector3(x * 0.093f, 0.022f, 0.218f), new Vector3(0.04f, 0.05f, 0.02f), black); // pupil
                ModelKit.Part(face, PrimitiveType.Sphere, new Vector3(x * 0.085f, 0.04f, 0.226f), new Vector3(0.014f, 0.014f, 0.01f), white); // shine
                ModelKit.Part(face, PrimitiveType.Sphere, new Vector3(x * 0.17f, -0.07f, 0.16f), new Vector3(0.09f, 0.06f, 0.03f), pink); // cheek
                ModelKit.Part(face, PrimitiveType.Sphere, new Vector3(x * 0.022f, -0.085f, 0.205f), new Vector3(0.04f, 0.013f, 0.012f), black, new Vector3(0f, 0f, x * -25f)); // smile
            }
            ModelKit.Part(face, PrimitiveType.Sphere, new Vector3(0f, -0.045f, 0.21f), new Vector3(0.05f, 0.032f, 0.03f), black); // nose

            tail = new GameObject("Tail").transform;
            tail.SetParent(body, false);
            tail.localPosition = new Vector3(0f, 0.24f, -0.15f);
            ModelKit.Part(tail, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.09f, 0.09f, 0.07f), white);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => current = null;
    }
}
