namespace BubbleTea.Core
{
    /// <summary>
    /// The numbers upgrades can change. Each one is read by the script it affects:
    /// gameplay code asks <see cref="Stats"/> for the value instead of knowing about upgrades.
    /// </summary>
    public enum StatId
    {
        /// <summary>Multiplies the player's walking speed (1 = normal).</summary>
        MoveSpeed,

        /// <summary>Extra items the player can carry (0 = none).</summary>
        CarryBonus,

        /// <summary>Multiplies how fast stations work (2 = twice as fast).</summary>
        BrewSpeed,

        /// <summary>Multiplies what customers pay for drinks (1 = menu price).</summary>
        DrinkPrice,

        /// <summary>Multiplies how often customers arrive (1 = normal). Set by the shop rating.</summary>
        CustomerRate,

        /// <summary>Extra customers allowed in line at once (0 = none). Set by the shop rating.</summary>
        ExtraCustomers,

        /// <summary>Extra finished drinks each station can hold (0 = none).</summary>
        StationCapacity,
    }
}
