using System;
using System.Collections.Generic;

namespace StoreAndInventory
{
    [Serializable]
    public class RuneSlotEntry
    {
        public string instanceGuid;
    }

    [Serializable]
    public class EquipmentData
    {
        public List<RuneSlotEntry> runes = new();
    }

    [Serializable]
    public class EquippedCacheEntry
    {
        public string instanceGuid;
        public ItemStack stack;
    }

    [Serializable]
    public class EquippedCacheData
    {
        public List<EquippedCacheEntry> entries = new();
    }
}
