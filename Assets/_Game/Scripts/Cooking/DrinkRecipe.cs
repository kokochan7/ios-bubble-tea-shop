using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// A finished drink customers can buy, and its price.
    /// Create with right-click > Create > Bubble Tea > Drink.
    /// </summary>
    [CreateAssetMenu(menuName = "Bubble Tea/Drink", fileName = "NewDrink")]
    public class DrinkRecipe : ScriptableObject
    {
        [SerializeField, Tooltip("The item that is this finished drink.")]
        private ItemDefinition drink;

        [SerializeField, Min(1), Tooltip("What a customer pays for it.")]
        private int price = 10;

        public ItemDefinition Drink => drink;
        public int Price => price;
    }
}
