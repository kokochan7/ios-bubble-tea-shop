using UnityEngine;

namespace BubbleTea.Building
{
    /// <summary>
    /// Data for something the player can buy with an unlock pad: its name, price and
    /// what appears when it's paid for. Create new ones with
    /// right-click > Create > Bubble Tea > Buildable.
    /// </summary>
    [CreateAssetMenu(menuName = "Bubble Tea/Buildable", fileName = "NewBuildable")]
    public class BuildableDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Name shown on the pad.")]
        private string displayName = "Station";

        [SerializeField, Min(1), Tooltip("Price to unlock.")]
        private int cost = 50;

        [SerializeField, Tooltip("What appears on the pad once it's paid for.")]
        private GameObject prefab;

        public string DisplayName => displayName;
        public int Cost => cost;
        public GameObject Prefab => prefab;
    }
}
