using System;

namespace StoreAndInventory
{
    [Serializable]
    public class ItemStack
    {
        public string definitionId;
        public int count;
        public string instanceGuid;

        public bool IsEmpty => string.IsNullOrEmpty(definitionId) || count <= 0;
    }
}
