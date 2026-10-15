using UnityEngine;

namespace BubbleTea.Upgrades
{
    /// <summary>
    /// Marks the upgrade desk as tappable: tapping the desk (its model or anything under it)
    /// opens the upgrade menu, the same way tapping a counter opens its prices
    /// (see <see cref="BubbleTea.UI.UpgradePanel"/>). The desk needs a collider for the tap to hit.
    /// </summary>
    public class UpgradeZone : MonoBehaviour
    {
    }
}
