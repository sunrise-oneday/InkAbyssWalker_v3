using System.Collections.Generic;
using UnityEngine;

namespace StoreAndInventory
{
    [CreateAssetMenu(fileName = "cons_", menuName = "MoYuan/Item/Consumable")]
    public class Consumable : ItemBase
    {
        [Header("消耗品")]
        public List<GameplayEffectSO> useEffects = new();

        [Tooltip("单次使用消耗几个")]
        public int consumeOnUse = 1;

        [Tooltip("可在哪些上下文使用")]
        public UseContext useContext = UseContext.Any;

        public override ItemCategory Category => ItemCategory.Consumable;

        protected override string FilePrefix => "cons_";

        void Reset()
        {
            maxStack = 99;
        }
    }
}
