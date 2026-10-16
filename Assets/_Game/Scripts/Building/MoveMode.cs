using BubbleTea.Cooking;
using BubbleTea.Core;
using BubbleTea.Customers;
using BubbleTea.Player;
using BubbleTea.Staff;
using BubbleTea.Travel;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.Building
{
    /// <summary>
    /// Tap a bar, a piece of decor or a table and a MOVE button pops up over it. Tap MOVE and
    /// the joystick slides it around the shop instead of walking you (it won't go through walls,
    /// other things or the unlock pads); ROTATE turns it a quarter turn, DONE keeps the new spot
    /// and CANCEL puts it back. Builds its own buttons, so the scenes don't need rebuilding.
    /// </summary>
    public class MoveMode : MonoBehaviour
    {
        private const float MoveSpeed = 3.5f;       // meters per second at full stick
        private const float BubbleSeconds = 4f;     // how long the MOVE button waits for a tap
        private const float Gap = 0.08f;            // things may stand this close together

        private static MoveMode instance;

        /// <summary>True while the player is moving something (other taps are ignored).</summary>
        public static bool Busy => instance != null && instance.target != null;

        private Movable picked;          // showing the MOVE button
        private float pickedTimer;
        private Movable target;          // being moved
        private Vector3 startPosition;
        private Quaternion startRotation;
        private Bounds footprint;        // the target's box when the move started
        private Vector3 footprintPivot;
        private float hintTimer;

        private VirtualJoystick joystick;
        private RectTransform moveButton;
        private GameObject bar;
        private Text hint;

        /// <summary>Makes sure the open shop has one (movable things call this when they appear).</summary>
        public static void Ensure()
        {
            if (instance != null) return;
            new GameObject("Move Mode").AddComponent<MoveMode>();
        }

        private void Awake()
        {
            instance = this;
            BuildButtons();
        }

        private void OnEnable() => VirtualJoystick.Tapped += OnTapped;

        private void OnDisable()
        {
            VirtualJoystick.Tapped -= OnTapped;
            if (target != null) Cancel();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            PlayerMovement.Locked = false;
            CameraFollow.Focus = null;
        }

        private void Start()
        {
#if UNITY_2023_1_OR_NEWER
            joystick = FindFirstObjectByType<VirtualJoystick>();
#else
            joystick = FindObjectOfType<VirtualJoystick>();
#endif
        }

        // ---------------------------------------------------------------- picking

        private void OnTapped(Vector2 screenPosition)
        {
            if (target != null || Time.timeScale == 0f) return;
            Movable tapped = FindTapped(screenPosition);
            Station station = tapped != null ? tapped.GetComponentInChildren<Station>() : null;
            if (tapped == null || (station != null && station.Jammed))
            {
                picked = null;
                return;
            }
            picked = tapped;
            pickedTimer = BubbleSeconds;
        }

        private static Movable FindTapped(Vector2 screenPosition)
        {
            Camera camera = Camera.main;
            if (camera == null) return null;
            foreach (RaycastHit hit in Physics.RaycastAll(camera.ScreenPointToRay(screenPosition), 200f))
            {
                Movable movable = hit.collider.GetComponentInParent<Movable>();
                if (movable != null) return movable;
            }
            // Things without colliders: the nearest one close to the tap on screen.
            Movable best = null;
            float bestDistance = Screen.height * 0.06f;
            foreach (Movable movable in Movable.All)
            {
                if (movable == null) continue;
                Vector3 spot = camera.WorldToScreenPoint(movable.transform.position + Vector3.up * 0.6f);
                if (spot.z <= 0f) continue;
                float distance = Vector2.Distance(screenPosition, spot);
                if (distance < bestDistance)
                {
                    best = movable;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private void Update()
        {
            if (hintTimer > 0f)
            {
                hintTimer -= Time.unscaledDeltaTime;
                if (hintTimer <= 0f && hint != null) hint.text = HowTo;
            }

            if (target != null)
            {
                Steer();
                return;
            }

            if (picked != null)
            {
                pickedTimer -= Time.unscaledDeltaTime;
                if (pickedTimer <= 0f || !picked.isActiveAndEnabled) picked = null;
            }
            Camera camera = Camera.main;
            bool show = picked != null && camera != null;
            if (show)
            {
                Vector3 spot = camera.WorldToScreenPoint(picked.transform.position + Vector3.up * 1.2f);
                show = spot.z > 0f;
                moveButton.position = new Vector3(spot.x, spot.y, 0f);
            }
            if (moveButton.gameObject.activeSelf != show) moveButton.gameObject.SetActive(show);
        }

        // ---------------------------------------------------------------- moving

        private const string HowTo = "Drag anywhere to slide it";

        private void BeginMove()
        {
            if (picked == null) return;
            if (!picked.CanMoveNow(out string reason))
            {
                GameEvents.RaiseAnnouncement(reason, new Color(0.93f, 0.45f, 0.62f));
                picked = null;
                return;
            }
            target = picked;
            picked = null;
            startPosition = target.transform.position;
            startRotation = target.transform.rotation;
            Physics.SyncTransforms();
            footprint = target.Footprint();
            footprintPivot = startPosition;
            PlayerMovement.Locked = true;
            CameraFollow.Focus = target.transform;
            moveButton.gameObject.SetActive(false);
            bar.SetActive(true);
            hint.text = HowTo;
        }

        private void Steer()
        {
            if (!target.isActiveAndEnabled)
            {
                End();
                return;
            }
            Vector2 input = joystick != null ? joystick.Value : Vector2.zero;
            if (input.sqrMagnitude < 0.0001f) input = PlayerMovement.ReadKeyboard();
            input = Vector2.ClampMagnitude(input, 1f);
            if (input.sqrMagnitude < 0.0001f) return;

            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            Camera camera = Camera.main;
            if (camera != null)
            {
                forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
            }
            Vector3 step = (forward * input.y + right * input.x) * (MoveSpeed * Time.deltaTime);

            // Slide along whatever is in the way: try each direction on its own.
            Quaternion rotation = target.transform.rotation;
            Vector3 position = target.transform.position;
            bool freeNow = Fits(position, rotation);
            Vector3 tryX = position + new Vector3(step.x, 0f, 0f);
            if (!freeNow || Fits(tryX, rotation)) position = tryX;
            Vector3 tryZ = position + new Vector3(0f, 0f, step.z);
            if (!freeNow || Fits(tryZ, rotation)) position = tryZ;
            target.transform.position = position;
        }

        private void Rotate()
        {
            if (target == null) return;
            Quaternion turned = Quaternion.Euler(0f, 90f, 0f) * target.transform.rotation;
            Vector3 position = target.transform.position;
            if (Fits(position, target.transform.rotation) && !Fits(position, turned))
            {
                hint.text = "No room to turn it here";
                hintTimer = 1.5f;
                return;
            }
            target.transform.rotation = turned;
        }

        private void Done()
        {
            if (target == null) return;
            Vector3 position = target.transform.position;
            target.Place(position, target.transform.eulerAngles.y, byPlayer: true);
            End();
        }

        private void Cancel()
        {
            if (target == null) return;
            target.transform.SetPositionAndRotation(startPosition, startRotation);
            End();
        }

        private void End()
        {
            target = null;
            PlayerMovement.Locked = false;
            CameraFollow.Focus = null;
            if (bar != null) bar.SetActive(false);
        }

        // True if the target, standing here and turned this way, doesn't overlap a wall, another
        // bar or piece of decor, or an unlock pad. People don't count (they walk around it).
        private bool Fits(Vector3 position, Quaternion rotation)
        {
            // The box it started with, carried along to the new spot and turn.
            Quaternion turn = rotation * Quaternion.Inverse(startRotation);
            Vector3 center = position + turn * (footprint.center - footprintPivot);
            Vector3 extents = footprint.extents;
            float yaw = Mathf.Repeat(turn.eulerAngles.y, 180f);
            if (yaw > 45f && yaw < 135f) extents = new Vector3(extents.z, extents.y, extents.x);
            extents.x = Mathf.Max(0.05f, extents.x - Gap);
            extents.z = Mathf.Max(0.05f, extents.z - Gap);

            // Pads and travel spots stay clear so they can still be used.
            foreach (UnlockPad pad in UnlockPad.Active)
            {
                if (pad != null && Overlaps(center, extents, pad.transform.position, pad.HalfSize)) return false;
            }
            foreach (ShopTravel travel in ShopTravel.All)
            {
                if (travel != null && Overlaps(center, extents, travel.transform.position, travel.HalfSize)) return false;
            }

            // From knee height up, so the floor doesn't count.
            var box = new Vector3(center.x, 1f, center.z);
            var half = new Vector3(extents.x, 0.65f, extents.z);
            foreach (Collider other in Physics.OverlapBox(box, half, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (other.transform.IsChildOf(target.transform)) continue;
                if (IsSomeone(other)) continue;
                return false;
            }
            return true;
        }

        private static bool Overlaps(Vector3 center, Vector3 extents, Vector3 square, float halfSize)
        {
            return Mathf.Abs(center.x - square.x) < extents.x + halfSize
                && Mathf.Abs(center.z - square.z) < extents.z + halfSize;
        }

        // People and what they carry: the player, customers, staff, and anything with a rigidbody.
        private static bool IsSomeone(Collider other)
        {
            if (other is CharacterController || other.attachedRigidbody != null) return true;
            return other.GetComponentInParent<PlayerMovement>() != null
                || other.GetComponentInParent<Customer>() != null
                || other.GetComponentInParent<Barista>() != null
                || other.GetComponentInParent<Cashier>() != null
                || other.GetComponentInParent<Cleaner>() != null
                || other.GetComponentInParent<Stocker>() != null;
        }

        // ---------------------------------------------------------------- buttons

        private void BuildButtons()
        {
            var canvasObject = new GameObject("Move Mode Buttons");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40; // above the joystick and HUD, below pop-up panels' own canvases if higher
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite rounded = RoundedSprite();

            // The MOVE button that pops up over a tapped thing.
            moveButton = MakeButton(canvasObject.transform, "MOVE", new Vector2(230f, 96f), new Color(0.91f, 0.51f, 0.6f), rounded, font,
                BeginMove);
            moveButton.anchorMin = moveButton.anchorMax = Vector2.zero;
            moveButton.gameObject.SetActive(false);

            // The bar along the bottom while moving.
            var barRect = MakeImage(canvasObject.transform, "Move Bar", rounded, new Color(0.15f, 0.15f, 0.2f, 0.92f));
            barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.sizeDelta = new Vector2(980f, 250f);
            barRect.anchoredPosition = new Vector2(0f, 120f);
            bar = barRect.gameObject;

            hint = MakeText(barRect, "Hint", font, 42, Color.white);
            RectTransform hintRect = hint.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 1f);
            hintRect.anchorMax = new Vector2(1f, 1f);
            hintRect.pivot = new Vector2(0.5f, 1f);
            hintRect.sizeDelta = new Vector2(0f, 80f);
            hintRect.anchoredPosition = new Vector2(0f, -14f);
            hint.text = HowTo;

            Place(MakeButton(barRect, "ROTATE", new Vector2(290f, 110f), new Color(0.45f, 0.62f, 0.9f), rounded, font, Rotate), -310f);
            Place(MakeButton(barRect, "DONE", new Vector2(290f, 110f), new Color(0.35f, 0.78f, 0.38f), rounded, font, Done), 0f);
            Place(MakeButton(barRect, "CANCEL", new Vector2(290f, 110f), new Color(0.45f, 0.45f, 0.55f), rounded, font, Cancel), 310f);
            bar.SetActive(false);
        }

        private static void Place(RectTransform button, float x)
        {
            button.anchorMin = button.anchorMax = new Vector2(0.5f, 0f);
            button.pivot = new Vector2(0.5f, 0f);
            button.anchoredPosition = new Vector2(x, 26f);
        }

        private static RectTransform MakeButton(Transform parent, string label, Vector2 size, Color color, Sprite sprite, Font font,
            UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = MakeImage(parent, label, sprite, color);
            rect.sizeDelta = size;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(onClick);
            Text text = MakeText(rect, "Label", font, 46, Color.white);
            text.text = label;
            text.fontStyle = FontStyle.Bold;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            return rect;
        }

        private static RectTransform MakeImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return (RectTransform)go.transform;
        }

        private static Text MakeText(Transform parent, string name, Font font, int size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        // A white rounded square that stretches without blurring its corners.
        private static Sprite RoundedSprite()
        {
            const int size = 64;
            const float radius = 24f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius)));
                    float dy = Mathf.Max(0f, Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius)));
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
    }
}
