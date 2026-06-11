using System;
using UnityEngine;

namespace StoreAndInventory
{
    [Serializable]
    public class ShopEntry
    {
        public ItemBase item;
        public int priceOverride;
        [Tooltip("仅 Equipment(符文) 生效：>0 限购次数；-1 本店无限；0 不可买。消耗品/剧情物忽略。")]
        public int stock = -1;
        [Range(0f, 1f)]
        public float weight = 1f;
    }

    [Serializable]
    public class StockEntry
    {
        public string itemId;
        public int remaining;
    }
}
