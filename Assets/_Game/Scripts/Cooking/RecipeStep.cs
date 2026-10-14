using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// One thing a station knows how to do: "turn this input into that output, taking N
    /// seconds". Leave the input empty for stations that make something from nothing
    /// (like a tea brewer). Create with right-click > Create > Bubble Tea > Recipe Step.
    /// </summary>
    [CreateAssetMenu(menuName = "Bubble Tea/Recipe Step", fileName = "NewRecipeStep")]
    public class RecipeStep : ScriptableObject
    {
        [SerializeField, Tooltip("Item the station consumes. Empty means it needs nothing.")]
        private ItemDefinition input;

        [SerializeField, Tooltip("Item the station produces.")]
        private ItemDefinition output;

        [SerializeField, Min(0.05f), Tooltip("Seconds to make one output.")]
        private float seconds = 2f;

        public ItemDefinition Input => input;
        public ItemDefinition Output => output;
        public float Seconds => seconds;
    }
}
