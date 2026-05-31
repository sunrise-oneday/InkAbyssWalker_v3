using UnityEngine;

namespace StoreAndInventory
{
    /// <summary>
    /// StatType ↔ 主工程 <see cref="CharacterStats"/> 字段映射。背包只显示上限类，不显示 currentHP/currentMP/currentBreakValue。
    /// </summary>
    public static class CharacterStatFieldMap
    {
        /// <summary>是否允许出现在背包属性行（由 AttributeStatLineUI 配置）。</summary>
        public static bool IsInventoryPanelStat(StatType stat)
        {
            return stat switch
            {
                StatType.Attack => true,
                StatType.MaxHp => true,
                StatType.MaxMp => true,
                StatType.Defense => true,
                StatType.MaxBreak => true,
                _ => false
            };
        }

        /// <summary>从 CharacterStats 读取基础上限（不含装备加成、不含 current*）。</summary>
        public static float ReadBaseValue(StatType stat, CharacterStats stats)
        {
            if (stats == null)
                return 0f;

            return stat switch
            {
                StatType.MaxHp => stats.maxHP,
                StatType.MaxMp => stats.maxMP,
                StatType.Attack => stats.attack,
                StatType.Defense => stats.defense,
                StatType.MaxBreak => stats.maxBreakValue,
                _ => 0f
            };
        }
    }
}
