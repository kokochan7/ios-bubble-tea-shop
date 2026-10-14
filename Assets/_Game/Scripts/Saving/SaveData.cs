using System;
using System.Collections.Generic;
using BubbleTea.Goals;

namespace BubbleTea.Saving
{
    /// <summary>
    /// Everything written to the save file. Unity's JsonUtility turns this into JSON,
    /// so it only holds plain fields and lists (no dictionaries).
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Bumped when the layout changes, so old saves can be upgraded instead of lost.</summary>
        public int version = 8;

        public int money;
        public List<PadSave> pads = new();

        /// <summary>Added in version 2. Older saves simply load with no upgrades bought.</summary>
        public List<UpgradeSave> upgrades = new();

        /// <summary>Added in version 3: when this was saved, in seconds since 1970 (UTC). 0 in older saves.</summary>
        public long savedAtUnix;

        /// <summary>Added in version 3: how fast the shop was earning, for money earned while away.</summary>
        public float earningsPerMinute;

        /// <summary>Added in version 4: the day the daily goals belong to (yyyy-MM-dd).</summary>
        public string goalsDay;

        /// <summary>Added in version 4: today's goals and their progress.</summary>
        public List<GoalState> goals = new();

        /// <summary>Added in version 5: the shop rating's service score (0 to 1). -1 = not saved yet.</summary>
        public float ratingService = -1f;

        /// <summary>Added in version 7: stars lost to walkouts and not yet earned back.</summary>
        public float ratingLost;

        /// <summary>Added in version 6: money the floor cash has handed out so far (it stops at a limit).</summary>
        public int floorCashGiven;

        /// <summary>Added in version 8: drink prices set at the counter (only ones that aren't normal).</summary>
        public List<BubbleTea.Customers.PriceSave> prices = new();

        /// <summary>Added in version 8: the shop clock (minutes since midnight, -1 = not saved) and day number.</summary>
        public float clockMinutes = -1f;
        public int clockDay = 1;
    }

    /// <summary>One unlock pad's progress.</summary>
    [Serializable]
    public class PadSave
    {
        public string id;
        public int paid;
        public bool unlocked;
    }

    /// <summary>One upgrade's level.</summary>
    [Serializable]
    public class UpgradeSave
    {
        public string id;
        public int level;
    }
}
