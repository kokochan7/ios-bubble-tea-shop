using System;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.Extras
{
    /// <summary>
    /// A number pad that asks for a 4-digit code before something opens (the GM tools on a phone).
    /// The right code runs the action; a wrong one clears the code to try again.
    /// </summary>
    public static class Passcode
    {
        /// <summary>Shows the pad; <paramref name="unlocked"/> runs once the right code is typed.</summary>
        public static void Ask(string title, string code, Action unlocked)
        {
            var root = new GameObject("Passcode");
            RectTransform canvas = UiKit.Canvas(root.transform, "Passcode Canvas", 70); // over the open menus
            RectTransform backdrop = UiKit.Popup(canvas, title, new Vector2(700f, 1080f), () => UnityEngine.Object.Destroy(root),
                out RectTransform body, out _);
            backdrop.gameObject.AddComponent<TapToClose>().Close = () => UnityEngine.Object.Destroy(root);

            Text shown = UiKit.Label(body, "", 72, Color.white);
            UiKit.Pin(shown.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(560f, 120f));
            string typed = "";
            void Show(string text = null) =>
                shown.text = text ?? string.Join(" ", (new string('*', typed.Length) + new string('_', code.Length - typed.Length)).ToCharArray());
            Show();

            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "", "0", "<" };
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                if (key == "") continue;
                Button button = UiKit.Button(body, key, key == "<" ? UiKit.Gray : UiKit.Blue, Vector2.zero, () =>
                {
                    if (key == "<")
                    {
                        if (typed.Length > 0) typed = typed.Substring(0, typed.Length - 1);
                        Show();
                        return;
                    }
                    typed += key;
                    if (typed.Length < code.Length)
                    {
                        Show();
                        return;
                    }
                    if (typed == code)
                    {
                        UnityEngine.Object.Destroy(root);
                        unlocked?.Invoke();
                        return;
                    }
                    typed = "";
                    Show("Wrong code");
                }, 56);
                UiKit.Pin((RectTransform)button.transform, new Vector2(0.5f, 1f),
                    new Vector2((i % 3 - 1) * 190f, -150f - i / 3 * 190f), new Vector2(170f, 170f));
            }
        }
    }
}
