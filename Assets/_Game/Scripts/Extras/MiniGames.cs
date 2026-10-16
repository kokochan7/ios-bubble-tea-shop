using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.Extras
{
    /// <summary>
    /// Quick tap games for bonus money (and pearls for a great score), playable every few minutes:
    /// <list type="bullet">
    /// <item>SHAKE IT!: a marker sweeps across a bar; tap when it's in the green zone, five times.</item>
    /// <item>PEARL DROP: pearls fall from the top; tap as many as you can before they reach the bottom.</item>
    /// </list>
    /// The game is paused while playing, so these run on real (unscaled) time.
    /// </summary>
    public class MiniGames : MonoBehaviour
    {
        /// <summary>Real seconds between mini-games.</summary>
        public const int CooldownSeconds = 180;

        private enum Game { None, Shake, Drop, Done }

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

        /// <summary>Seconds until the next game can be played (0 = ready).</summary>
        public static int SecondsUntilReady =>
            (int)Math.Max(0L, Profile.NextMiniGameUnix - DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        /// <summary>Starts one of the games inside <paramref name="parent"/>.</summary>
        public static MiniGames Play(RectTransform parent, bool shake, Action<float> onFinished)
        {
            var go = new GameObject("Mini Game", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var games = go.AddComponent<MiniGames>();
            games.area = (RectTransform)go.transform;
            UiKit.Fill(games.area);
            games.finished = onFinished;
            Profile.NextMiniGameUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + CooldownSeconds;
            if (shake) games.StartShake();
            else games.StartDrop();
            return games;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (game == Game.Shake) UpdateShake(dt);
            else if (game == Game.Drop) UpdateDrop(dt);
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
    }
}
