using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// Rain showers at the Night Market: every so often it pours for a little while and everyone in
    /// the street runs under the stall's awning, so customers flood in (a big rush) until it stops.
    /// The rain is drawn as falling streaks around the player, and the night gets a little darker.
    /// Only comes once the stall is open (a counter can serve something).
    /// </summary>
    public class RainShower : MonoBehaviour
    {
        /// <summary>Multiplies how fast customers arrive (3 while it rains, 1 otherwise).</summary>
        public static float FlowMultiplier { get; private set; } = 1f;

        /// <summary>Extra customers allowed in each line while it rains.</summary>
        public static int ExtraCustomers { get; private set; }

        /// <summary>True while it's raining.</summary>
        public static bool Raining { get; private set; }

        [SerializeField, Tooltip("Real seconds of dry weather between showers, picked at random in this range.")]
        private Vector2 secondsBetweenShowers = new(70f, 120f);

        [SerializeField, Tooltip("Real seconds a shower lasts, picked at random in this range.")]
        private Vector2 secondsOfRain = new(20f, 30f);

        [SerializeField, Tooltip("How much faster customers come while it rains.")]
        private float rushRate = 3f;

        [SerializeField, Tooltip("Extra customers allowed in each line while it rains.")]
        private int rushExtraCustomers = 4;

        [SerializeField, Tooltip("Number of rain streaks drawn around the player.")]
        private int streaks = 140;

        [SerializeField, Tooltip("Size of the rainy area around the player, in meters.")]
        private Vector3 area = new(30f, 12f, 26f);

        private Transform rain;
        private Transform[] drops;
        private float[] dropSpeed;
        private Light sun;
        private float sunIntensity;
        private float timer;
        private float strength; // 0 = dry, 1 = full shower (fades in and out)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            FlowMultiplier = 1f;
            ExtraCustomers = 0;
            Raining = false;
        }

        private void Start()
        {
            timer = Random.Range(secondsBetweenShowers.x, secondsBetweenShowers.y) * 0.6f; // the first one comes a bit sooner
            sun = RenderSettings.sun;
            if (sun == null)
            {
#if UNITY_2023_1_OR_NEWER
                foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
#else
                foreach (Light light in FindObjectsOfType<Light>())
#endif
                {
                    if (light.type == LightType.Directional) sun = light;
                }
            }
            if (sun != null) sunIntensity = sun.intensity;
            BuildRain();
        }

        private void OnDestroy() => ResetOnPlay();

        private void Update()
        {
            bool open = ServingCounter.All.Count > 0;
            if (open) timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                Raining = !Raining;
                if (Raining)
                {
                    timer = Random.Range(secondsOfRain.x, secondsOfRain.y);
                    GameEvents.RaiseAnnouncement("Rain shower! Everyone's running under your awning.\nGet ready for a big rush!",
                        new Color(0.3f, 0.5f, 0.85f));
                }
                else timer = Random.Range(secondsBetweenShowers.x, secondsBetweenShowers.y);
            }

            strength = Mathf.MoveTowards(strength, Raining ? 1f : 0f, Time.deltaTime / 2f);
            FlowMultiplier = Raining ? rushRate : 1f;
            ExtraCustomers = Raining ? rushExtraCustomers : 0;
            if (sun != null) sun.intensity = sunIntensity * Mathf.Lerp(1f, 0.7f, strength);
            MoveRain();
        }

        // Thin pale streaks, made once and recycled: each falls, then starts again at the top.
        private void BuildRain()
        {
            rain = new GameObject("Rain").transform;
            rain.SetParent(transform, false);
            drops = new Transform[Mathf.Max(0, streaks)];
            dropSpeed = new float[drops.Length];
            Color water = new(0.75f, 0.85f, 1f);
            for (int i = 0; i < drops.Length; i++)
            {
                drops[i] = BubbleTea.Extras.ModelKit.Part(rain, PrimitiveType.Cube, RandomSpot(), new Vector3(0.025f, 0.55f, 0.025f), water,
                    new Vector3(8f, 0f, 0f));
                dropSpeed[i] = Random.Range(14f, 20f);
                var renderer = drops[i].GetComponent<Renderer>();
                if (renderer != null) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            rain.gameObject.SetActive(false);
        }

        private Vector3 RandomSpot() => new(
            Random.Range(-area.x / 2f, area.x / 2f), Random.Range(0f, area.y), Random.Range(-area.z / 2f, area.z / 2f));

        private void MoveRain()
        {
            if (rain == null) return;
            // Fewer streaks as the shower starts and stops.
            int showing = Mathf.RoundToInt(drops.Length * strength);
            if (rain.gameObject.activeSelf != showing > 0) rain.gameObject.SetActive(showing > 0);
            if (showing == 0) return;

            Transform player = BubbleTea.Player.PlayerMovement.Current;
            if (player != null) rain.position = new Vector3(player.position.x, 0f, player.position.z);
            for (int i = 0; i < drops.Length; i++)
            {
                Transform drop = drops[i];
                bool on = i < showing;
                if (drop.gameObject.activeSelf != on) drop.gameObject.SetActive(on);
                if (!on) continue;
                Vector3 spot = drop.localPosition;
                spot.y -= dropSpeed[i] * Time.deltaTime;
                spot.z -= dropSpeed[i] * 0.14f * Time.deltaTime; // blown a little toward the camera
                if (spot.y < 0f)
                {
                    spot = RandomSpot();
                    spot.y = area.y;
                }
                drop.localPosition = spot;
            }
        }
    }
}
