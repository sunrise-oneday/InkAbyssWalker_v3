using System;

namespace StoreAndInventory
{
    [Serializable]
    public struct SkillModifier
    {
        public SkillModTarget targetKind;
        public string targetId;
        public SkillModType modType;
        public float value;
    }
}
