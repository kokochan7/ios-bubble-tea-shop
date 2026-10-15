using System.Collections.Generic;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// Every so often (Matcha House) a tour bus pulls up out front and drops off a whole group of
    /// tourists at once. A banner warns the player a few seconds before it arrives. Tourists wear
    /// sun hats, order up to two drinks and wait longer than usual. Serve the whole group and it
    /// leaves a big bonus at the counter plus a star; if anyone walks out, there's no bonus.
    /// </summary>
    public class TourBus : MonoBehaviour
    {
        [SerializeField, Tooltip("The spawner of the counter the tourists line up at.")]
        private CustomerSpawner spawner;

        [SerializeField, Tooltip("The bus model. Hidden while it's away.")]
        private Transform bus;

        [SerializeField, Tooltip("Where the bus comes from, where it stops, and where it drives off to.")]
        private Vector3 roadStart, stop, roadEnd;

        [SerializeField, Tooltip("Where tourists step off, relative to the bus stop.")]
        private Vector3 doorOffset = new(0f, 0f, 1.6f);

        [SerializeField, Tooltip("Driving speed in meters per second.")]
        private float speed = 10f;

        [SerializeField, Tooltip("Seconds between buses, picked at random in this range (counted while the shop can serve).")]
        private Vector2 secondsBetweenBuses = new(80f, 130f);

        [SerializeField, Tooltip("Seconds of warning before the bus arrives.")]
        private float warningSeconds = 10f;

        [SerializeField, Tooltip("Tourists per bus, picked at random in this range.")]
        private Vector2Int groupSize = new(6, 8);

        [SerializeField, Tooltip("Seconds between tourists stepping off.")]
        private float secondsBetweenTourists = 0.6f;

        [SerializeField, Min(0), Tooltip("Bonus per tourist when the whole group is served.")]
        private int bonusPerTourist = 100;

        [SerializeField, Tooltip("Stars added when the whole group is served.")]
        private float starBonus = 1f;

        private static readonly Color Blue = new(0.2f, 0.45f, 0.8f);

        private enum State { Waiting, Arriving, Unloading, Leaving }

        private State state;
        private float timer;
        private bool warned;
        private int toUnload;
        private int groupTotal;
        private int groupLeft;
        private bool groupFailed;
        private readonly List<Customer> group = new();

        private void Start()
        {
            PlanNextBus();
            if (bus != null) bus.gameObject.SetActive(false);
        }

        private void PlanNextBus()
        {
            state = State.Waiting;
            warned = false;
            timer = Random.Range(secondsBetweenBuses.x, secondsBetweenBuses.y);
        }

        private void Update()
        {
            switch (state)
            {
                case State.Waiting: Wait(); break;
                case State.Arriving:
                    if (Drive(stop)) StartUnloading();
                    break;
                case State.Unloading: Unload(); break;
                default:
                    if (Drive(roadEnd))
                    {
                        if (bus != null) bus.gameObject.SetActive(false);
                        // The next bus is only planned once this group has gone (see OnTouristGone).
                        if (groupLeft <= 0) PlanNextBus();
                    }
                    break;
            }
        }

        private void Wait()
        {
            // No buses until the shop can serve, and never while the last group is still inside.
            ServingCounter counter = ServingCounter.FindLane("Main");
            if (spawner == null || counter == null || !counter.CanMakeAnything || groupLeft > 0) return;

            timer -= Time.deltaTime;
            if (!warned && timer <= warningSeconds)
            {
                warned = true;
                groupTotal = Random.Range(groupSize.x, groupSize.y + 1);
                GameEvents.RaiseAnnouncement($"A tour bus is coming with {groupTotal} customers!\nServe them all for a ${groupTotal * bonusPerTourist} bonus and +{starBonus:0.#} star.", Blue);
            }
            if (timer > 0f) return;

            if (groupTotal <= 0) groupTotal = Random.Range(groupSize.x, groupSize.y + 1);
            if (bus != null)
            {
                bus.position = roadStart;
                bus.gameObject.SetActive(true);
            }
            state = State.Arriving;
        }

        private void StartUnloading()
        {
            toUnload = groupTotal;
            groupLeft = groupTotal;
            groupFailed = false;
            group.Clear();
            timer = 0f;
            state = State.Unloading;
        }

        private void Unload()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = secondsBetweenTourists;

            if (toUnload > 0)
            {
                toUnload--;
                Customer tourist = spawner != null
                    ? spawner.SpawnVisitor(stop + doorOffset, Customer.Visit.Tourist, OnTouristGone)
                    : null;
                if (tourist != null) group.Add(tourist);
                else
                {
                    groupLeft--; // couldn't step off (the counter is gone): doesn't count against the group
                }
                return;
            }
            state = State.Leaving;
        }

        private void OnTouristGone(Customer tourist)
        {
            group.Remove(tourist);
            if (!tourist.WasServed) groupFailed = true;
            groupLeft--;
            if (groupLeft > 0) return;

            if (!groupFailed)
            {
                int bonus = groupTotal * bonusPerTourist;
                ServingCounter counter = ServingCounter.FindLane("Main");
                if (counter != null && counter.CashPile != null) counter.CashPile.AddValue(bonus);
                GameEvents.RaiseStarsAwarded(starBonus);
                GameEvents.RaiseAnnouncement($"The whole tour group was served!\n+${bonus} bonus at the counter and +{starBonus:0.#} star.", Blue);
            }
            else
            {
                GameEvents.RaiseAnnouncement("Some tourists walked out, so no tour bonus this time.", new Color(0.82f, 0.26f, 0.24f));
            }
            groupTotal = 0;
            if (state == State.Waiting || (bus != null && !bus.gameObject.activeSelf)) PlanNextBus();
        }

        // Drives the bus toward a point. Returns true once there.
        private bool Drive(Vector3 target)
        {
            if (bus == null) return true;
            Vector3 offset = target - bus.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.01f) return true;
            bus.rotation = Quaternion.LookRotation(offset);
            bus.position = Vector3.MoveTowards(bus.position, target, speed * Time.deltaTime);
            return false;
        }
    }
}
