using System;
using System.Collections.Generic;

namespace StoreAndInventory
{
    [Serializable]
    public class CurrencyEntry
    {
        public CurrencyId id;
        public int amount;
    }

    [Serializable]
    public class CurrencyWallet
    {
        public List<CurrencyEntry> entries = new();
    }
}
