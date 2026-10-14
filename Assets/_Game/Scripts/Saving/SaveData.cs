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
        public int version = 1;

        public int money;
        public List<PadSave> pads = new();
    }

    /// <summary>One unlock pad's progress.</summary>
    [Serializable]
    public class PadSave
    {
        public string id;
        public int paid;
        public bool unlocked;
    }
}
