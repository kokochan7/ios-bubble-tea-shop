using BubbleTea.Upgrades;
using UnityEngine;

namespace BubbleTea.UI
{
    /// <summary>
    /// Shows the upgrade menu while the player stands at the upgrade desk and hides it
    /// when they walk away. Put this on an object that stays active, and point it at the panel.
    /// </summary>
    public class UpgradePanel : MonoBehaviour
    {
        [SerializeField, Tooltip("The panel to show and hide (a child of this object).")]
        private GameObject panel;

        private void Start()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void Update()
        {
            if (panel == null) return;
            bool show = UpgradeZone.AnyPlayerInside();
            if (panel.activeSelf != show) panel.SetActive(show);
        }
    }
}
