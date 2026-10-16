using BubbleTea.Core;
using BubbleTea.Player;
using BubbleTea.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BubbleTea.Travel
{
    /// <summary>
    /// A bus stop that takes the player to another shop. Stand on it and the ring fills;
    /// when it's full the game saves this shop and opens the other one. Each shop keeps
    /// its own money and progress (they have separate save files).
    /// </summary>
    public class ShopTravel : MonoBehaviour
    {
        [SerializeField, Tooltip("Scene name of the shop this stop goes to (it must be in File > Build Profiles).")]
        private string targetScene = "Shop2";

        [SerializeField, Tooltip("Seconds the player stands here before leaving.")]
        private float holdSeconds = 1.2f;

        [SerializeField, Tooltip("Half the stop's width in meters.")]
        private float halfSize = 1.1f;

        [SerializeField, Tooltip("Grows from nothing to full size while the player waits.")]
        private Transform fillRing;

        [SerializeField, Tooltip("Sign text, turned to face the camera.")]
        private Transform label;

        private static readonly System.Collections.Generic.List<ShopTravel> active = new();

        /// <summary>Every travel spot in the open shop (moved things keep off them).</summary>
        public static System.Collections.Generic.IReadOnlyList<ShopTravel> All => active;

        /// <summary>Half the stop's width in meters.</summary>
        public float HalfSize => halfSize;

        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => active.Clear();

        private Transform player;
        private float held;
        private bool leaving;
        private bool armed; // false until the player has stepped off, so buying the stop doesn't whisk them away

        private void Start()
        {
#if UNITY_2023_1_OR_NEWER
            var movement = FindFirstObjectByType<PlayerMovement>();
#else
            var movement = FindObjectOfType<PlayerMovement>();
#endif
            if (movement != null) player = movement.transform;
            SetFill(0f);
        }

        private void Update()
        {
            if (player == null || leaving) return;

            Vector3 delta = player.position - transform.position;
            bool inside = Mathf.Abs(delta.x) <= halfSize && Mathf.Abs(delta.z) <= halfSize;
            if (!inside) armed = true;
            held = inside && armed ? held + Time.deltaTime : 0f;
            SetFill(held / Mathf.Max(holdSeconds, 0.01f));
            if (held >= holdSeconds) Go();
        }

        private void LateUpdate()
        {
            if (label != null && Camera.main != null) label.rotation = Camera.main.transform.rotation;
        }

        private void Go()
        {
            if (!Application.CanStreamedLevelBeLoaded(targetScene))
            {
                Debug.LogWarning($"Bubble Tea: the scene '{targetScene}' isn't in the build. " +
                                 "Build it with Bubble Tea > Build Second Shop Scene.");
                held = 0f;
                return;
            }

            leaving = true;
#if UNITY_2023_1_OR_NEWER
            var saveManager = FindFirstObjectByType<SaveManager>();
#else
            var saveManager = FindObjectOfType<SaveManager>();
#endif
            if (saveManager != null) saveManager.Save();
            GoTo(targetScene);
        }

        /// <summary>Opens another shop, remembering it so the game reopens there next time.</summary>
        public static void GoTo(string scene)
        {
            PlayerPrefs.SetString(SaveManager.LastShopKey, scene);
            PlayerPrefs.Save();
            Stats.Clear(); // the next shop applies its own upgrades and rating
            SceneManager.LoadScene(scene);
        }

        private void SetFill(float amount)
        {
            if (fillRing == null) return;
            float size = Mathf.Clamp01(amount);
            fillRing.localScale = new Vector3(size, 1f, size);
            fillRing.gameObject.SetActive(size > 0f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 0.5f);
            Gizmos.DrawWireCube(transform.position, new Vector3(halfSize * 2f, 0.05f, halfSize * 2f));
        }
    }
}
