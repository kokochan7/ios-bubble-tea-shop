using System;
using System.Collections.Generic;

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
        public int version = 3;

        public int money;
        public List<PadSave> pads = new();

        /// <summary>Added in version 2. Older saves simply load with no upgrades bought.</summary>
        public List<UpgradeSave> upgrades = new();

        /// <summary>Added in version 3: when this was saved, in seconds since 1970 (UTC). 0 in older saves.</summary>
        public long savedAtUnix;

        /// <summary>Added in version 3: how fast the shop was earning, for money earned while away.</summary>
        public float earningsPerMinute;
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
