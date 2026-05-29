using System.Collections.Generic;
using UnityEngine;

namespace StoreAndInventory
{
    [CreateAssetMenu(fileName = "equip_", menuName = "MoYuan/Item/Equipment")]
    public class Equipment : ItemBase
    {
        [Header("① 基础属性加成")]
        public List<StatModifier> statMods = new();

        [Header("② 技能 / 卡牌加成")]
        public List<SkillModifier> skillMods = new();

        [Header("③ 特殊效果（本期不发动，仅数据）")]
        public List<GameplayEffectSO> extraEffects = new();

        public override ItemCategory Category => ItemCategory.Equipment;

        public override int MaxStack => 1;

        protected override string FilePrefix => "equip_";

        void Reset()
        {
            maxStack = 1;
            canSell = true;
        }
    }
}
