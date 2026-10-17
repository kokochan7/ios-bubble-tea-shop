using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BubbleTea.Extras
{
    /// <summary>
    /// Makes every menu behave the same way: it draws in front of the whole HUD, only one is open at
    /// a time (opening one closes the last), tapping outside it closes it, and a red X in its top
    /// right corner replaces the gray Close button at the bottom. The menus the scene builder made
    /// (MENU, GOALS, STATS, prices, GM tools) are found by their Close button; the SHOP, TEAM, PETS
    /// and PLAY pop-ups sign up through <see cref="Watch"/>.
    /// </summary>
    public static class MenuLayers
    {
        /// <summary>Canvas order for open menus: above the HUD (0), the extras (30) and move mode (40).</summary>
        public const int Order = 50;

        private static readonly List<MenuWatch> open = new();

        /// <summary>Sets up the scene's own menus (called once as a shop opens).</summary>
        public static void Scan()
        {
#if UNITY_2023_1_OR_NEWER
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
#endif
            foreach (Canvas canvas in canvases)
            {
                if (canvas == null || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
                foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
                {
                    if (button.name != "Close" || !button.gameObject.activeSelf) continue;
                    // The part that opens and closes is the first hidden one above the Close button
                    // (the menus all start closed).
                    Transform shown = button.transform.parent;
                    while (shown != null && shown.gameObject.activeSelf && shown != canvas.transform) shown = shown.parent;
                    if (shown == null || shown == canvas.transform || shown.GetComponent<MenuWatch>() != null) continue;

                    Button gray = button;
                    UnityAction close = () => gray.onClick.Invoke();
                    gray.gameObject.SetActive(false);
                    CloseX((RectTransform)gray.transform.parent, close);
                    Watch(shown.gameObject, close);
                }
            }
        }

        /// <summary>A red X button in the top right corner of a menu's panel.</summary>
        public static Button CloseX(RectTransform panel, UnityAction close)
        {
            Button x = UiKit.Button(panel, "", UiKit.Red, Vector2.zero, close, 52);
            x.name = "CloseX";
            var rect = (RectTransform)x.transform;
            UiKit.Pin(rect, new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(96f, 96f));
            Image image = x.GetComponent<Image>();
            image.sprite = UiKit.Circle;
            image.type = Image.Type.Simple;
            Text label = UiKit.TextOf(x);
            label.text = "X";
            label.fontSize = 52;
            x.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -5f);
            return x;
        }

        /// <summary>
        /// Treats <paramref name="menu"/> as a menu: shown in front of the HUD while it's active, closing
        /// any other open menu, and closed (with <paramref name="close"/>) by a tap outside it.
        /// </summary>
        public static void Watch(GameObject menu, UnityAction close)
        {
            var watch = menu.GetComponent<MenuWatch>();
            if (watch == null)
            {
                bool wasActive = menu.activeSelf;
                if (wasActive) menu.SetActive(false); // so OnEnable runs after the setup below
                watch = menu.AddComponent<MenuWatch>();
                if (menu.GetComponent<Canvas>() == null) menu.AddComponent<Canvas>();
                if (menu.GetComponent<GraphicRaycaster>() == null) menu.AddComponent<GraphicRaycaster>();
                menu.AddComponent<SortOver>().Order = Order;

                // A screen-wide menu with its own dark backdrop closes when that backdrop is tapped;
                // a panel on its own gets a backdrop just behind it.
                var rect = (RectTransform)menu.transform;
                bool screenWide = rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one && menu.GetComponent<Image>() != null;
                if (screenWide) menu.AddComponent<TapToClose>().Close = () => watch.Close?.Invoke();
                else
                {
                    RectTransform backdrop = UiKit.Box(menu.transform.parent, menu.name + " Backdrop", new Color(0f, 0f, 0f, 0.45f));
                    Image image = backdrop.GetComponent<Image>();
                    image.sprite = null;
                    image.type = Image.Type.Simple;
                    UiKit.Pin(backdrop, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8000f, 8000f));
                    backdrop.SetSiblingIndex(menu.transform.GetSiblingIndex());
                    backdrop.gameObject.AddComponent<Canvas>();
                    backdrop.gameObject.AddComponent<GraphicRaycaster>();
                    backdrop.gameObject.AddComponent<SortOver>().Order = Order - 1;
                    backdrop.gameObject.AddComponent<TapToClose>().Close = () => watch.Close?.Invoke();
                    backdrop.gameObject.SetActive(false);
                    watch.Backdrop = backdrop.gameObject;
                }
                if (wasActive) menu.SetActive(true);
            }
            watch.Close = close;
        }

        // A menu just opened: close the one that was open before.
        internal static void Opened(MenuWatch menu)
        {
            foreach (MenuWatch other in open.ToArray())
            {
                if (other != null && other != menu) other.Close?.Invoke();
            }
            open.RemoveAll(m => m == null || !m.isActiveAndEnabled);
            if (!open.Contains(menu)) open.Add(menu);
        }

        internal static void Closed(MenuWatch menu) => open.Remove(menu);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => open.Clear();
    }

    /// <summary>Marks a menu so <see cref="MenuLayers"/> can keep only one open.</summary>
    public class MenuWatch : MonoBehaviour
    {
        public UnityAction Close;
        public GameObject Backdrop;

        private void OnEnable()
        {
            if (Backdrop != null) Backdrop.SetActive(true);
            MenuLayers.Opened(this);
        }

        private void OnDisable()
        {
            if (Backdrop != null) Backdrop.SetActive(false);
            MenuLayers.Closed(this);
        }
    }

    /// <summary>Draws this part of the screen above the rest of its canvas (set again on every show).</summary>
    public class SortOver : MonoBehaviour
    {
        public int Order;

        private void OnEnable()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) return;
            canvas.overrideSorting = true;
            canvas.sortingOrder = Order;
        }
    }

    /// <summary>Closes a menu when its backdrop itself (not the panel on it) is tapped.</summary>
    public class TapToClose : MonoBehaviour, IPointerClickHandler
    {
        public UnityAction Close;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.pointerPressRaycast.gameObject == gameObject) Close?.Invoke();
        }
    }
}
