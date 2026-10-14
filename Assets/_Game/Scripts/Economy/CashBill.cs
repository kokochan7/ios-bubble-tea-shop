using System;
using UnityEngine;

namespace BubbleTea.Economy
{
    /// <summary>
    /// A single bill in a <see cref="CashPile"/>. When collected it flies in an arc to the
    /// player and reports back so the pile can reuse it.
    /// </summary>
    public class CashBill : MonoBehaviour
    {
        [SerializeField, Tooltip("Seconds the flight to the player takes.")]
        private float flightDuration = 0.3f;

        [SerializeField, Tooltip("How high the arc goes above a straight line.")]
        private float arcHeight = 1.5f;

        [SerializeField, Tooltip("Where on the player the bill lands, measured up from the player's pivot.")]
        private float targetHeight = 1f;

        private Vector3 baseScale;
        private Vector3 start;
        private Transform target;
        private float progress;
        private Action<CashBill> onArrived;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        /// <summary>Puts the bill back in its pile at the given local position.</summary>
        public void PlaceIn(Transform pile, Vector3 localPosition)
        {
            target = null;
            onArrived = null;
            transform.SetParent(pile, false);
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.identity;
            transform.localScale = baseScale;
        }

        /// <summary>Starts flying toward <paramref name="flyTarget"/>; calls <paramref name="arrived"/> on landing.</summary>
        public void FlyTo(Transform flyTarget, Action<CashBill> arrived)
        {
            transform.SetParent(null, true);
            start = transform.position;
            target = flyTarget;
            onArrived = arrived;
            progress = 0f;
        }

        private void Update()
        {
            if (target == null) return;

            progress += Time.deltaTime / Mathf.Max(flightDuration, 0.01f);
            float t = Mathf.Clamp01(progress);

            Vector3 end = target.position + Vector3.up * targetHeight;
            Vector3 position = Vector3.Lerp(start, end, t);
            position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.position = position;
            transform.localScale = baseScale * Mathf.Lerp(1f, 0.4f, t);

            if (t >= 1f)
            {
                Action<CashBill> callback = onArrived;
                target = null;
                onArrived = null;
                callback?.Invoke(this);
            }
        }
    }
}
