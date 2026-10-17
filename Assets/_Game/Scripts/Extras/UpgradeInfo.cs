using BubbleTea.Core;
using BubbleTea.Upgrades;

namespace BubbleTea.Extras
{
    /// <summary>One line on what a kitchen upgrade does, with what it gives now and at the next level.</summary>
    public static class UpgradeInfo
    {
        public static string Describe(UpgradeDefinition upgrade, int level)
        {
            float now = upgrade.ValueAt(level);
            bool maxed = level >= upgrade.MaxLevel;
            float next = upgrade.ValueAt(level + 1);
            string Change(string format) => maxed ? $"Now {string.Format(format, Value(upgrade.Stat, now))} (max)" :
                $"Now {string.Format(format, Value(upgrade.Stat, now))}, next {string.Format(format, Value(upgrade.Stat, next))}";

            switch (upgrade.Stat)
            {
                case StatId.CarryBonus: return "Each level: +1 drink on your tray. " + Change("+{0} items");
                case StatId.BrewSpeed: return "Bars make drinks faster. " + Change("{0}% speed");
                case StatId.StationCapacity: return "Each bar holds +1 finished drink per level. " + Change("+{0}");
                case StatId.AutoWhisk: return maxed ? "The Whisk Bar whisks by itself. Bought!" : "The Whisk Bar whisks by itself, no need to stand there.";
                case StatId.HandoffSpeed: return "Drinks hop on and off trays faster. " + Change("+{0}%");
                case StatId.MoveSpeed: return "You and your staff walk faster. " + Change("+{0}%");
                case StatId.Patience: return "Calm tunes: customers wait longer. " + Change("+{0}%");
                case StatId.TipBonus: return "A tip jar: customers tip more often. " + Change("+{0}%");
                case StatId.DrinkPrice: return "Fancy cups: every drink sells for more. " + Change("+{0}%");
                default: return "";
            }
        }

        // The number shown for a stat's value: whole items, a speed in %, or a bonus in %.
        private static string Value(StatId stat, float value) => stat switch
        {
            StatId.CarryBonus or StatId.StationCapacity => value.ToString("0"),
            StatId.BrewSpeed => (value * 100f).ToString("0"),
            StatId.TipBonus => (value * 100f).ToString("0"),
            _ => ((value - 1f) * 100f).ToString("0"),
        };
    }
}
