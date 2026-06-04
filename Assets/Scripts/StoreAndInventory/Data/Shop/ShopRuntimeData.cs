using System;
using System.Collections.Generic;

namespace StoreAndInventory
{
    [Serializable]
    public class ShopRuntimeData
    {
        public string shopId;
        public List<StockEntry> stockOverrides = new();
        public List<string> randomSnapshot = new();
        public long lastRefreshTick;
    }
}
