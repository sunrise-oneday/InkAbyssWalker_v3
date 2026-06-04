using System;

namespace StoreAndInventory
{
    [Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public float flat;
        public float percent;
    }
}
