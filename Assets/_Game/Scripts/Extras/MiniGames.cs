using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.Extras
{
    /// <summary>
    /// Quick tap games for bonus pearls, playable once a minute:
    /// <list type="bullet">
    /// <item>SHAKE IT!: a marker sweeps across a bar; tap when it's in the green zone, five times.</item>
    /// <item>PEARL DROP: pearls fall from the top; tap as many as you can before they reach the bottom.</item>
    /// <item>FILL IT UP: tap to start pouring tea and tap again to stop it on the line, three cups.</item>
    /// <item>ORDER UP!: watch the toppings light up, then tap them back in the same order.</item>
    /// <item>RUSH HOUR: customers pop up at the tables; tap them before they give up and leave.</item>
    /// </list>
    /// The game is paused while playing, so these run on real (unscaled) time.
    /// </summary>
    public class MiniGames : MonoBehaviour
    {
        /// <summary>Real seconds between mini-games.</summary>
        public const int CooldownSeconds = 60;

        /// <summary>The games, in the order they're listed.</summary>
        public enum Kind { Shake, Drop, Pour, Memory, Rush }

        private enum Game { None, Shake, Drop, Pour, Memory, Rush, Done }

        private RectTransform area;      // where the game is drawn
        private Action<float> finished;  // called with the score (0 to 1)
        private Game game;

        // Shake It!
        private const int ShakeRounds = 5;
        private RectTransform bar, zone, sweet, marker;
        private Text info;
        private float markerPos, markerDir = 1f, markerSpeed;
        private int round, points;
        private float pauseTimer;

        // Pearl Drop
        private const float DropSeconds = 15f;
        private readonly List<RectTransform> pearls = new();
        private readonly Dictionary<RectTransform, float> fallSpeed = new();
        private float dropTimer, spawnTimer;
        private int spawned, caught;

        // Fill It Up
        private const int PourCups = 3;
        private RectTransform tea, line;
        private Text pourLabel;
        private float level, target, pourSpeed;
        private bool pouring;
        private int cup;

        // Order Up!
        private const int MemoryRounds = 4;
        private static readonly string[] ToppingNames = { "PEARLS", "MANGO", "BERRY", "MATCHA" };
        private static readonly Color[] ToppingColors =
        {
            new(0.55f, 0.35f, 0.25f), new(0.98f, 0.75f, 0.25f), new(0.92f, 0.42f, 0.55f), new(0.45f, 0.75f, 0.35f),
        };
        private readonly Button[] toppings = new Button[4];
        private readonly List<int> sequence = new();
        private int shown, typed, roundsRight;
        private float showTimer;
        private bool showing;

        // Rush Hour
        private const float RushSeconds = 18f;
        private readonly Button[] seats = new Button[9];
        private readonly float[] waiting = new float[9]; // seconds a customer has left at each seat (0 = empty)
        private float rushTimer, nextGuest;
        private int guests, served;

        /// <summary>Seconds until the next game can be played (0 = ready).</summary>
        public static int SecondsUntilReady =>
            (int)Math.Max(0L, Profile.NextMiniGameUnix - DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        /// <summary>Starts one of the games inside <paramref name="parent"/>.</summary>
        public static MiniGames Play(RectTransform parent, Kind kind, Action<float> onFinished)
        {
            var go = new GameObject("Mini Game", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var games = go.AddComponent<MiniGames>();
            games.area = (RectTransform)go.transform;
            UiKit.Fill(games.area);
            games.finished = onFinished;
            Profile.NextMiniGameUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + CooldownSeconds;
            switch (kind)
            {
                case Kind.Shake: games.StartShake(); break;
                case Kind.Drop: games.StartDrop(); break;
                case Kind.Pour: games.StartPour(); break;
                case Kind.Memory: games.StartMemory(); break;
                default: games.StartRush(); break;
            }
            return games;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (game == Game.Shake) UpdateShake(dt);
            else if (game == Game.Drop) UpdateDrop(dt);
            else if (game == Game.Pour) UpdatePour(dt);
            else if (game == Game.Memory) UpdateMemory(dt);
            else if (game == Game.Rush) UpdateRush(dt);
        }

        private void AddInfo(float top)
        {
            info = UiKit.Label(area, "", 44, Color.white);
            UiKit.Pin(info.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -top), new Vector2(900f, 160f));
        }

        private void Finish(float score)
        {
            game = Game.Done;
            finished?.Invoke(Mathf.Clamp01(score));
        }

        // ---------------------------------------------------------------- Shake It!

        private void StartShake()
        {
            game = Game.Shake;
            info = UiKit.Label(area, "", 44, Color.white);
            UiKit.Pin(info.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(900f, 160f));

            // A tea shaker that wobbles with every tap, for fun.
            RectTransform cup = UiKit.Box(area, "Shaker", new Color(0.88f, 0.75f, 0.6f));
            UiKit.Pin(cup, new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(180f, 260f));
            RectTransform lid = UiKit.Box(cup, "Lid", new Color(0.75f, 0.78f, 0.85f));
            UiKit.Pin(lid, new Vector2(0.5f, 1f), new Vector2(0f, 30f), new Vector2(200f, 60f));

            bar = UiKit.Box(area, "Bar", new Color(0.3f, 0.3f, 0.38f));
            UiKit.Pin(bar, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(880f, 90f));
            zone = UiKit.Box(bar, "Zone", new Color(0.35f, 0.78f, 0.38f));
            sweet = UiKit.Box(zone, "Perfect", new Color(0.6f, 0.95f, 0.55f));
            marker = UiKit.Box(bar, "Marker", Color.white);
            marker.anchorMin = marker.anchorMax = new Vector2(0f, 0.5f);
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.sizeDelta = new Vector2(18f, 130f);

            Button tap = UiKit.Button(area, "SHAKE!", UiKit.Pink, new Vector2(520f, 170f), OnShakeTap, 64);
            UiKit.Pin((RectTransform)tap.transform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(520f, 170f));

            round = 0;
            points = 0;
            NextShakeRound();
        }

        private void NextShakeRound()
        {
            round++;
            float width = Mathf.Lerp(0.26f, 0.12f, (round - 1) / (float)(ShakeRounds - 1)); // smaller each round
            float start = UnityEngine.Random.Range(0.05f, 0.95f - width);
            zone.anchorMin = new Vector2(start, 0f);
            zone.anchorMax = new Vector2(start + width, 1f);
            zone.offsetMin = zone.offsetMax = Vector2.zero;
            sweet.anchorMin = new Vector2(0.35f, 0f);
            sweet.anchorMax = new Vector2(0.65f, 1f);
            sweet.offsetMin = sweet.offsetMax = Vector2.zero;
            markerSpeed = 0.7f + 0.18f * round; // bar widths per second
            markerPos = 0f;
            markerDir = 1f;
            pauseTimer = 0f;
            info.text = $"Round {round} of {ShakeRounds}\nTap SHAKE! when the line is in the green";
        }

        private void UpdateShake(float dt)
        {
            if (pauseTimer > 0f)
            {
                pauseTimer -= dt;
                if (pauseTimer <= 0f)
                {
                    if (round >= ShakeRounds) Finish(points / (ShakeRounds * 2f));
                    else NextShakeRound();
                }
                return;
            }
            markerPos += markerDir * markerSpeed * dt;
            if (markerPos > 1f) { markerPos = 1f; markerDir = -1f; }
            if (markerPos < 0f) { markerPos = 0f; markerDir = 1f; }
            marker.anchorMin = marker.anchorMax = new Vector2(markerPos, 0.5f);
            marker.anchoredPosition = Vector2.zero;
        }

        private void OnShakeTap()
        {
            if (game != Game.Shake || pauseTimer > 0f) return;
            float zoneStart = zone.anchorMin.x, zoneEnd = zone.anchorMax.x;
            float zoneWidth = zoneEnd - zoneStart;
            bool perfect = markerPos >= zoneStart + zoneWidth * 0.35f && markerPos <= zoneStart + zoneWidth * 0.65f;
            bool hit = markerPos >= zoneStart && markerPos <= zoneEnd;
            if (perfect) { points += 2; info.text = "PERFECT SHAKE!"; }
            else if (hit) { points += 1; info.text = "Nice!"; }
            else info.text = "Missed!";
            pauseTimer = 0.8f;
        }

        // ---------------------------------------------------------------- Pearl Drop

        private void StartDrop()
        {
            game = Game.Drop;
            info = UiKit.Label(area, "", 44, Color.white);
            UiKit.Pin(info.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(900f, 80f));
            RectTransform cup = UiKit.Box(area, "Cup", new Color(0.88f, 0.75f, 0.6f, 0.35f));
            cup.anchorMin = new Vector2(0f, 0f);
            cup.anchorMax = new Vector2(1f, 0f);
            cup.pivot = new Vector2(0.5f, 0f);
            cup.sizeDelta = new Vector2(0f, 90f);
            cup.GetComponent<Image>().raycastTarget = false;
            dropTimer = DropSeconds;
            spawnTimer = 0.3f;
            spawned = caught = 0;
        }

        private void UpdateDrop(float dt)
        {
            dropTimer -= dt;
            info.text = $"Tap the pearls!   Caught {caught}   {Mathf.CeilToInt(Mathf.Max(0f, dropTimer))}s";

            spawnTimer -= dt;
            if (dropTimer > 1f && spawnTimer <= 0f)
            {
                spawnTimer = UnityEngine.Random.Range(0.45f, 0.8f);
                SpawnPearl();
            }

            float height = area.rect.height;
            for (int i = pearls.Count - 1; i >= 0; i--)
            {
                RectTransform pearl = pearls[i];
                if (pearl == null)
                {
                    pearls.RemoveAt(i);
                    continue;
                }
                Vector2 spot = pearl.anchoredPosition;
                spot.y -= fallSpeed[pearl] * dt;
                pearl.anchoredPosition = spot;
                if (spot.y < -height + 60f)
                {
                    Destroy(pearl.gameObject); // missed: fell into the cup
                    pearls.RemoveAt(i);
                }
            }

            if (dropTimer <= 0f && pearls.Count == 0) Finish(spawned > 0 ? caught / (float)spawned : 0f);
        }

        private void SpawnPearl()
        {
            spawned++;
            float size = 130f;
            Button button = UiKit.Button(area, "", new Color(0.25f, 0.16f, 0.12f), new Vector2(size, size), null);
            var rect = (RectTransform)button.transform;
            button.GetComponent<Image>().sprite = UiKit.Circle;
            button.GetComponent<Image>().type = Image.Type.Simple;
            RectTransform shine = UiKit.Box(rect, "Shine", new Color(1f, 1f, 1f, 0.5f), UiKit.Circle);
            UiKit.Pin(shine, new Vector2(0.32f, 0.68f), Vector2.zero, new Vector2(size * 0.28f, size * 0.28f));
            shine.pivot = new Vector2(0.5f, 0.5f);
            shine.GetComponent<Image>().raycastTarget = false;

            float width = area.rect.width > 1f ? area.rect.width : 900f;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(UnityEngine.Random.Range(size, width - size), -110f);
            float speed = UnityEngine.Random.Range(380f, 620f) + spawned * 12f;
            fallSpeed[rect] = speed;
            button.onClick.AddListener(() =>
            {
                if (rect == null || game != Game.Drop) return;
                caught++;
                pearls.Remove(rect);
                Destroy(rect.gameObject);
            });
            pearls.Add(rect);
        }
    
        // ---------------------------------------------------------------- Fill It Up

        private void StartPour()
        {
            game = Game.Pour;
            AddInfo(20f);
            RectTransform glass = UiKit.Box(area, "Cup", new Color(1f, 1f, 1f, 0.18f));
            UiKit.Pin(glass, new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(300f, 620f));
            RectTransform inside = UiKit.Group(glass, "Inside");
            UiKit.Fill(inside);
            inside.offsetMin = new Vector2(14f, 14f);
            inside.offsetMax = new Vector2(-14f, -14f);
            tea = UiKit.Box(inside, "Tea", new Color(0.82f, 0.62f, 0.42f));
            tea.anchorMin = Vector2.zero;
            tea.offsetMin = tea.offsetMax = Vector2.zero;
            line = UiKit.Box(inside, "Line", new Color(0.35f, 0.9f, 0.4f, 0.8f));
            line.pivot = new Vector2(0.5f, 0.5f);
            line.offsetMin = new Vector2(-30f, 0f);
            line.offsetMax = new Vector2(30f, 0f);

            Button pour = UiKit.Button(area, "POUR!", UiKit.Pink, new Vector2(520f, 170f), OnPourTap, 64);
            UiKit.Pin((RectTransform)pour.transform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(520f, 170f));
            pourLabel = UiKit.TextOf(pour);
            cup = 0;
            points = 0;
            NextCup();
        }

        private void NextCup()
        {
            cup++;
            level = 0f;
            pouring = false;
            pauseTimer = 0f;
            target = UnityEngine.Random.Range(0.45f, 0.88f);
            pourSpeed = 0.32f + 0.1f * cup; // cup heights per second, faster each cup
            line.anchorMin = new Vector2(0f, target - 0.012f);
            line.anchorMax = new Vector2(1f, target + 0.012f);
            ShowTea();
            pourLabel.text = "POUR!";
            info.text = $"Cup {cup} of {PourCups}\nTap to pour, tap again to stop on the green line";
        }

        private void ShowTea()
        {
            tea.anchorMax = new Vector2(1f, Mathf.Clamp01(level));
            tea.offsetMin = tea.offsetMax = Vector2.zero;
        }

        private void UpdatePour(float dt)
        {
            if (pauseTimer > 0f)
            {
                pauseTimer -= dt;
                if (pauseTimer <= 0f)
                {
                    if (cup >= PourCups) Finish(points / (PourCups * 2f));
                    else NextCup();
                }
                return;
            }
            if (!pouring) return;
            level += pourSpeed * dt;
            ShowTea();
            if (level >= 1f) StopPour(); // spilled over the top
        }

        private void OnPourTap()
        {
            if (game != Game.Pour || pauseTimer > 0f) return;
            if (!pouring)
            {
                pouring = true;
                pourLabel.text = "STOP!";
                return;
            }
            StopPour();
        }

        private void StopPour()
        {
            pouring = false;
            float miss = Mathf.Abs(level - target);
            if (level >= 1f) info.text = "Spilled!";
            else if (miss <= 0.025f) { points += 2; info.text = "PERFECT POUR!"; }
            else if (miss <= 0.07f) { points += 1; info.text = "Close enough!"; }
            else info.text = level < target ? "Too little!" : "Too much!";
            pourLabel.text = "...";
            pauseTimer = 0.9f;
        }

        // ---------------------------------------------------------------- Order Up!

        private void StartMemory()
        {
            game = Game.Memory;
            AddInfo(20f);
            for (int i = 0; i < toppings.Length; i++)
            {
                int pick = i;
                Button button = UiKit.Button(area, ToppingNames[i], ToppingColors[i], new Vector2(380f, 300f), () => OnTopping(pick), 48);
                UiKit.Pin((RectTransform)button.transform, new Vector2(0.5f, 0.5f),
                    new Vector2(i % 2 == 0 ? -205f : 205f, i < 2 ? 100f : -230f), new Vector2(380f, 300f));
                toppings[i] = button;
            }
            sequence.Clear();
            round = 0;
            roundsRight = 0;
            NextMemoryRound();
        }

        private void NextMemoryRound()
        {
            round++;
            sequence.Clear();
            for (int i = 0; i < round + 2; i++) sequence.Add(UnityEngine.Random.Range(0, toppings.Length)); // 3, 4, 5 then 6 toppings
            shown = 0;
            typed = 0;
            showing = true;
            showTimer = 0.9f; // a moment to get ready
            pauseTimer = 0f;
            info.text = $"Order {round} of {MemoryRounds}\nWatch the order...";
        }

        private void Light(int index, bool on)
        {
            Transform button = toppings[index].transform;
            button.localScale = Vector3.one * (on ? 1.12f : 1f);
            toppings[index].GetComponent<Image>().color = on ? Color.Lerp(ToppingColors[index], Color.white, 0.55f) : ToppingColors[index];
        }

        private void UpdateMemory(float dt)
        {
            if (pauseTimer > 0f)
            {
                pauseTimer -= dt;
                if (pauseTimer <= 0f)
                {
                    for (int i = 0; i < toppings.Length; i++) Light(i, false);
                    if (round >= MemoryRounds) Finish(roundsRight / (float)MemoryRounds);
                    else NextMemoryRound();
                }
                return;
            }
            if (!showing) return;
            showTimer -= dt;
            if (showTimer > 0f) return;
            // Each topping lights for 0.5 s with a short gap before the next one.
            int step = shown / 2;
            bool lit = shown % 2 == 0;
            if (step >= sequence.Count)
            {
                showing = false;
                info.text = $"Order {round} of {MemoryRounds}\nYour turn! Tap them in order";
                return;
            }
            Light(sequence[step], lit);
            showTimer = lit ? 0.5f : 0.18f;
            shown++;
        }

        private void OnTopping(int pick)
        {
            if (game != Game.Memory || showing || pauseTimer > 0f) return;
            if (pick != sequence[typed])
            {
                info.text = "Wrong topping!";
                Light(sequence[typed], true); // the one it should have been
                pauseTimer = 1f;
                return;
            }
            typed++;
            if (typed < sequence.Count) return;
            roundsRight++;
            info.text = "ORDER UP!";
            pauseTimer = 0.8f;
        }

        // ---------------------------------------------------------------- Rush Hour

        private void StartRush()
        {
            game = Game.Rush;
            AddInfo(0f);
            for (int i = 0; i < seats.Length; i++)
            {
                int seat = i;
                Button button = UiKit.Button(area, "", new Color(1f, 1f, 1f, 0.12f), new Vector2(240f, 240f), () => OnSeat(seat), 90);
                Image image = button.GetComponent<Image>();
                image.sprite = UiKit.Circle;
                image.type = Image.Type.Simple;
                UiKit.Pin((RectTransform)button.transform, new Vector2(0.5f, 0.5f),
                    new Vector2((i % 3 - 1) * 280f, -60f - (i / 3 - 1) * 280f), new Vector2(240f, 240f));
                seats[i] = button;
                waiting[i] = 0f;
            }
            rushTimer = RushSeconds;
            nextGuest = 0.5f;
            guests = served = 0;
        }

        private void ShowSeat(int seat, Color color, string text)
        {
            seats[seat].GetComponent<Image>().color = color;
            UiKit.TextOf(seats[seat]).text = text;
        }

        private void UpdateRush(float dt)
        {
            rushTimer -= dt;
            info.text = $"Tap the customers!   Served {served}   {Mathf.CeilToInt(Mathf.Max(0f, rushTimer))}s";

            nextGuest -= dt;
            if (rushTimer > 1f && nextGuest <= 0f)
            {
                nextGuest = Mathf.Lerp(0.95f, 0.45f, 1f - rushTimer / RushSeconds); // busier as it goes
                int seat = UnityEngine.Random.Range(0, seats.Length);
                if (waiting[seat] <= 0f)
                {
                    guests++;
                    waiting[seat] = Mathf.Lerp(1.6f, 0.9f, 1f - rushTimer / RushSeconds);
                    ShowSeat(seat, UiKit.Pink, "!");
                }
            }

            bool anyone = false;
            for (int i = 0; i < seats.Length; i++)
            {
                if (waiting[i] <= 0f) continue;
                waiting[i] -= dt;
                if (waiting[i] <= 0f) ShowSeat(i, new Color(1f, 1f, 1f, 0.12f), ""); // gave up and left
                else anyone = true;
            }

            if (rushTimer <= 0f && !anyone) Finish(guests > 0 ? served / (float)guests : 0f);
        }

        private void OnSeat(int seat)
        {
            if (game != Game.Rush || waiting[seat] <= 0f) return;
            waiting[seat] = 0f;
            served++;
            ShowSeat(seat, new Color(1f, 1f, 1f, 0.12f), "");
        }
    }
}
