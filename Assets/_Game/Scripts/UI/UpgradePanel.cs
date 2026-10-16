using BubbleTea.Player;
using BubbleTea.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// Shows the upgrade menu when the player taps the upgrade desk (like tapping a counter opens
    /// its prices) and pauses the game while it's open. The close button hides it and unpauses.
    /// Put this on an object that stays active, and point it at the panel.
    /// </summary>
    public class UpgradePanel : MonoBehaviour
    {
        [SerializeField, Tooltip("The panel to show and hide (a child of this object).")]
        private GameObject panel;

        [SerializeField, Tooltip("Closes the menu and unpauses the game.")]
        private Button closeButton;

        [SerializeField, Tooltip("Closes the menu and lets you move the upgrade desk.")]
        private Button moveButton;

        private UpgradeZone desk; // the desk that was tapped

        private void Start()
        {
            if (panel != null) panel.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (moveButton != null) moveButton.onClick.AddListener(MoveDesk);
        }

        private void OnEnable() => VirtualJoystick.Tapped += OnTapped;

        private void OnDisable()
        {
            VirtualJoystick.Tapped -= OnTapped;
            if (panel != null && panel.activeSelf) Time.timeScale = 1f;
        }

        // A tap on the upgrade desk (its model or anything under it) opens the menu.
        private void OnTapped(Vector2 screenPosition)
        {
            Camera camera = Camera.main;
            if (camera == null || panel == null || panel.activeSelf || BubbleTea.Building.MoveMode.Busy) return;
            Ray ray = camera.ScreenPointToRay(screenPosition);
            foreach (RaycastHit hit in Physics.RaycastAll(ray, 200f))
            {
                desk = hit.collider.GetComponentInParent<UpgradeZone>();
                if (desk == null) continue;
                panel.SetActive(true);
                Time.timeScale = 0f;
                return;
            }
        }

        // Closes the menu and picks up the desk: the joystick slides it, ROTATE turns it, DONE puts it down.
        private void MoveDesk()
        {
            Close();
            if (desk != null) BubbleTea.Building.MoveMode.Begin(desk.GetComponentInParent<BubbleTea.Building.Movable>());
        }

        /// <summary>Hides the menu and unpauses the game.</summary>
        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            Time.timeScale = 1f;
        }
    }
}
