using System;
using System.Collections.Generic;

namespace StoreAndInventory
{
    [Serializable]
    public class InventoryData
    {
        // 0 = unlimited (project decision). Kept for save compatibility.
        public int capacity = 0;
        public List<ItemStack> slots = new();
    }
}
