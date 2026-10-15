using System.Collections.Generic;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// A sign over the counter showing the drinks the customer at the front of the line still
    /// wants. Each drink disappears as it's handed over, and the sign hides when nobody is waiting.
    /// </summary>
    public class OrderBoard : MonoBehaviour
    {
        [SerializeField, Tooltip("The counter whose front customer is shown.")]
        private ServingCounter counter;

        [SerializeField, Tooltip("The sign to show and hide (turned to face the camera).")]
        private Transform board;

        [SerializeField, Tooltip("Where the drink models go, a child of the board.")]
        private Transform iconRow;

        [SerializeField, Tooltip("The sign's background, stretched to fit the drinks. Optional.")]
        private Transform backing;

        [SerializeField, Tooltip("Size of the drink models.")]
        private float iconScale = 2f;

        [SerializeField, Tooltip("Gap between drink models, in meters.")]
        private float iconSpacing = 0.55f;

        private readonly List<GameObject> icons = new();
        private Customer shown;
        private int shownCount = -1;

        private void LateUpdate()
        {
            if (counter == null || board == null) return;
            Customer front = counter.FrontCustomer;
            int count = front != null ? front.Orders.Count : 0;

            if (front != shown || count != shownCount)
            {
                shown = front;
                shownCount = count;
                if (count > 0) DrinkIcons.Show(iconRow, front.Orders, iconScale, iconSpacing, icons);
                else DrinkIcons.Clear(icons);
                board.gameObject.SetActive(count > 0);
                if (backing != null && count > 0)
                {
                    Vector3 size = backing.localScale;
                    backing.localScale = new Vector3(count * iconSpacing + 0.3f, size.y, size.z);
                }
            }

            if (count > 0 && Camera.main != null) board.rotation = Camera.main.transform.rotation;
        }
    }
}
