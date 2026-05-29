using System;
using System.Collections.Generic;

namespace StoreAndInventory
{
    [Serializable]
    public class CharacterStatEntry
    {
        public StatType stat;
        public float baseValue;
    }

    [Serializable]
    public class CharacterStatsData
    {
        public List<CharacterStatEntry> entries = new();
    }
}
