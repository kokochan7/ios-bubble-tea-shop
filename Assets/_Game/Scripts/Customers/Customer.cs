using System;
using BubbleTea.Cooking;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// Walks in, waits in line at a <see cref="ServingCounter"/>, buys a drink when it's
    /// their turn, and walks out with it. Gray-box movement: straight lines, no pathfinding.
    /// </summary>
    public class Customer : MonoBehaviour
    {
        [SerializeField, Tooltip("Walking speed in meters per second.")]
        private float speed = 3f;

        [SerializeField, Tooltip("Seconds a customer waits at the front before taking a drink.")]
        private float pauseBeforeBuying = 0.3f;

        [SerializeField, Tooltip("Where the drink sits while the customer carries it.")]
        private Vector3 drinkHoldPosition = new(0f, 0.3f, 0.6f);

        private enum State { Queueing, Leaving }

        private ServingCounter counter;
        private Vector3 exit;
        private Action<Customer> onGone;
        private State state;
        private float waitTimer;

        public void Init(ServingCounter servingCounter, Vector3 exitPoint, Action<Customer> gone)
        {
            counter = servingCounter;
            exit = exitPoint;
            onGone = gone;
            state = State.Queueing;
            counter.JoinQueue(this);
        }

        private void Update()
        {
            if (state == State.Queueing) Queue();
            else if (WalkTo(exit)) Leave();
        }

        private void Queue()
        {
            if (counter == null)
            {
                state = State.Leaving;
                return;
            }

            int index = counter.QueueIndexOf(this);
            if (!WalkTo(counter.QueuePosition(Mathf.Max(index, 0))) || index != 0)
            {
                waitTimer = 0f;
                return;
            }

            // At the front of the line: face the counter and buy once a drink is there.
            Vector3 toCounter = counter.transform.position - transform.position;
            toCounter.y = 0f;
            if (toCounter.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toCounter);

            waitTimer += Time.deltaTime;
            if (waitTimer < pauseBeforeBuying || !counter.TrySell(out CarriedItem drink)) return;

            drink.MoveTo(transform, drinkHoldPosition);
            counter.LeaveQueue(this);
            state = State.Leaving;
        }

        // Moves toward the target on the floor. Returns true once there.
        private bool WalkTo(Vector3 target)
        {
            target.y = transform.position.y;
            Vector3 offset = target - transform.position;
            if (offset.sqrMagnitude < 0.0025f) return true;

            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(offset), 720f * Time.deltaTime);
            return false;
        }

        private void Leave()
        {
            onGone?.Invoke(this);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (counter != null) counter.LeaveQueue(this);
        }
    }
}
