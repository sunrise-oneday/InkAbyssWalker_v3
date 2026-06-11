using System;
using UnityEngine;

namespace StoreAndInventory
{
    [Serializable]
    public struct SkillModifier
    {
        public SkillModTarget targetKind;
        public string targetId;
        public SkillModType modType;
        public float value;

        [Tooltip("留空 = 对所有形态生效；填 '初临形态'/'流墨形态'/'守墨形态' 则仅该形态生效")]
        public string requiredFormName;
    }
}
