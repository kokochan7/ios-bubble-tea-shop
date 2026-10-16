using System;
using BubbleTea.Cooking;
using BubbleTea.Customers;
using BubbleTea.Player;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// Tap a counter to open this panel and set what each drink costs, from half price to
    /// three times the normal price. Customers each have a limit: charge more than theirs and
    /// they walk out (losing half a star), so higher prices earn more per drink but cost customers.
    /// </summary>
    public class PricePanel : MonoBehaviour
    {
        /// <summary>One drink's row: name, price and the buttons to change it.</summary>
        [Serializable]
        public class Row
        {
            public DrinkRecipe recipe;
            public GameObject root;
            public Text nameLabel;
            public Text priceLabel;
            public Button lowerButton;
            public Button raiseButton;
        }

        [SerializeField, Tooltip("The panel to show and hide.")]
        private GameObject panel;

        [SerializeField] private Text titleLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Row[] rows = Array.Empty<Row>();

        private ServingCounter counter;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            foreach (Row row in rows)
            {
                Row r = row;
                if (r.lowerButton != null) r.lowerButton.onClick.AddListener(() => Change(r, -PriceBook.Step));
                if (r.raiseButton != null) r.raiseButton.onClick.AddListener(() => Change(r, PriceBook.Step));
            }
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            VirtualJoystick.Tapped += OnTapped;
            PriceBook.Changed += Refresh;
        }

        private void OnDisable()
        {
            VirtualJoystick.Tapped -= OnTapped;
            PriceBook.Changed -= Refresh;
        }

        // A tap on a counter (its model or anything under it) opens the panel for it.
        private void OnTapped(Vector2 screenPosition)
        {
            Camera camera = Camera.main;
            if (camera == null || (panel != null && panel.activeSelf) || BubbleTea.Building.MoveMode.Busy) return;
            Ray ray = camera.ScreenPointToRay(screenPosition);
            foreach (RaycastHit hit in Physics.RaycastAll(ray, 200f))
            {
                var tapped = hit.collider.GetComponentInParent<ServingCounter>();
                if (tapped == null) continue;
                Open(tapped);
                return;
            }
        }

        private void Open(ServingCounter target)
        {
            counter = target;
            if (titleLabel != null) titleLabel.text = target.Lane == "DriveThru" ? "DRIVE-THRU PRICES" : "DRINK PRICES";
            if (panel != null) panel.SetActive(true);
            Refresh();
        }

        private void Close()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void Change(Row row, float step)
        {
            if (row.recipe == null) return;
            PriceBook.SetFactor(row.recipe.Drink, PriceBook.Factor(row.recipe.Drink) + step);
        }

        private void Refresh()
        {
            if (panel == null || !panel.activeSelf || counter == null) return;
            foreach (Row row in rows)
            {
                if (row.recipe == null || row.root == null) continue;
                bool onMenu = Station.AnyCanMake(row.recipe.Drink);
                row.root.SetActive(onMenu);
                if (!onMenu) continue;

                float factor = PriceBook.Factor(row.recipe.Drink);
                if (row.nameLabel != null) row.nameLabel.text = row.recipe.Drink.DisplayName;
                if (row.priceLabel != null)
                {
                    row.priceLabel.text = $"${counter.PriceOf(row.recipe)}  ({Mathf.RoundToInt(factor * 100f)}%)";
                }
                if (row.lowerButton != null) row.lowerButton.interactable = factor > PriceBook.Min + 0.001f;
                if (row.raiseButton != null) row.raiseButton.interactable = factor < PriceBook.Max - 0.001f;
            }
        }
    }
}
