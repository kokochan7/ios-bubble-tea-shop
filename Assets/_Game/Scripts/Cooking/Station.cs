using System.Collections.Generic;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// A generic workstation. It knows a list of <see cref="RecipeStep"/>s: steps with an
    /// input wait for the player to drop that item off, steps without one run on their
    /// own. Finished items pile up on the output stack for the player to pick up.
    /// A tea brewer and a topping bar are both this component with different steps.
    /// </summary>
    public class Station : MonoBehaviour
    {
        // Stations currently in the shop, so customers only order drinks that can be made.
        private static readonly List<Station> active = new();

        [SerializeField, Tooltip("What this station can make.")]
        private RecipeStep[] steps;

        [SerializeField, Tooltip("Items waiting to be worked on.")]
        private ItemStack input = new();

        [SerializeField, Tooltip("Finished items, ready to pick up.")]
        private ItemStack output = new();

        [SerializeField, Tooltip("Where the player stands to drop off and pick up.")]
        private HandoffZone zone = new();

        private RecipeStep current;
        private float workTimer;

        /// <summary>0 to 1 progress on the current item (for a future progress bar).</summary>
        public float Progress => current != null ? Mathf.Clamp01(workTimer / current.Seconds) : 0f;

        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);

        public static IReadOnlyList<Station> All => active;

        /// <summary>Where a worker stands to drop off and pick up.</summary>
        public Vector3 ZoneCenter => zone.Center(transform);

        /// <summary>True if a finished <paramref name="item"/> is waiting to be picked up here.</summary>
        public bool HasReady(ItemDefinition item) => item != null && output.Contains(i => i == item);

        /// <summary>True if this station uses <paramref name="item"/> and has room for it.</summary>
        public bool Accepts(ItemDefinition item) => !input.IsFull && IsInput(item);

        /// <summary>The input this station turns into <paramref name="product"/>, or null if it can't.</summary>
        public ItemDefinition InputFor(ItemDefinition product)
        {
            if (steps == null || product == null) return null;
            foreach (RecipeStep step in steps)
            {
                if (step != null && step.Input != null && step.Output == product) return step.Input;
            }
            return null;
        }

        /// <summary>A station with a finished <paramref name="item"/> ready, or null.</summary>
        public static Station FindReady(ItemDefinition item)
        {
            foreach (Station station in active)
            {
                if (station.HasReady(item)) return station;
            }
            return null;
        }

        /// <summary>True if any station in the shop produces <paramref name="item"/>.</summary>
        public static bool AnyCanMake(ItemDefinition item)
        {
            if (item == null) return false;
            foreach (Station station in active)
            {
                if (station.steps == null) continue;
                foreach (RecipeStep step in station.steps)
                {
                    if (step != null && step.Output == item) return true;
                }
            }
            return false;
        }

        private void Update()
        {
            Work();
            zone.Tick(transform, Handoff);
        }

        private void Work()
        {
            if (current == null && !output.IsFull) current = StartNextStep();
            if (current == null) return;

            workTimer += Time.deltaTime * Stats.Get(StatId.BrewSpeed, 1f);
            if (workTimer < current.Seconds) return;

            if (current.Output != null)
            {
                CarriedItem made = current.Output.Spawn(transform.position + Vector3.up);
                if (!output.Push(made)) Destroy(made.gameObject);
            }
            current = null;
            workTimer = 0f;
        }

        // Steps that use up an input come first, so dropped-off items get processed
        // before the station makes anything from scratch.
        private RecipeStep StartNextStep()
        {
            if (steps == null) return null;

            foreach (RecipeStep step in steps)
            {
                if (step == null || step.Input == null) continue;
                if (input.TryTake(item => item == step.Input, out CarriedItem used))
                {
                    Destroy(used.gameObject);
                    return step;
                }
            }
            foreach (RecipeStep step in steps)
            {
                if (step != null && step.Input == null) return step;
            }
            return null;
        }

        // Drop off anything this station uses first; otherwise pick up a finished item.
        private bool Handoff(ItemCarrier carrier)
        {
            if (!input.IsFull && carrier.WillGiveTo(this) && carrier.Stack.TryTake(IsInput, out CarriedItem dropped))
            {
                input.Push(dropped);
                return true;
            }
            if (!carrier.Stack.IsFull && output.TryTake(carrier.WillTake, out CarriedItem finished))
            {
                carrier.Stack.Push(finished);
                return true;
            }
            return false;
        }

        private bool IsInput(ItemDefinition item)
        {
            if (steps == null || item == null) return false;
            foreach (RecipeStep step in steps)
            {
                if (step != null && step.Input == item) return true;
            }
            return false;
        }

        private void OnDrawGizmosSelected() => zone.DrawGizmo(transform);
    }
}
