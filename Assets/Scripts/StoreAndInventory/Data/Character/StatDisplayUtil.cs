using System.Collections.Generic;

namespace StoreAndInventory
{
    public static class StatDisplayUtil
    {
        public static string Label(StatType stat)
        {
            return stat switch
            {
                StatType.Attack => "攻击",
                StatType.MaxHp => "生命",
                StatType.MaxMp => "法力",
                StatType.Defense => "格挡数",
                StatType.MaxBreak => "破防上限",
                StatType.CritRate => "暴击率",
                StatType.CritDamage => "暴击伤害",
                StatType.DamageReduction => "减伤",
                StatType.FireDamageBonus => "火伤加成",
                StatType.IceDamageBonus => "冰伤加成",
                StatType.WaterDamageBonus => "水伤加成",
                StatType.ShieldBonus => "护盾加成",
                StatType.HealBonus => "治疗加成",
                _ => stat.ToString()
            };
        }

        public static string FormatValue(StatType stat, float value)
        {
            return stat switch
            {
                StatType.CritRate => (value * 100f).ToString("0") + "%",
                StatType.CritDamage => "+" + (value * 100f).ToString("0") + "%",
                StatType.DamageReduction => (value * 100f).ToString("0") + "%",
                StatType.FireDamageBonus => "+" + (value * 100f).ToString("0") + "%",
                StatType.IceDamageBonus => "+" + (value * 100f).ToString("0") + "%",
                StatType.WaterDamageBonus => "+" + (value * 100f).ToString("0") + "%",
                StatType.ShieldBonus => "+" + (value * 100f).ToString("0") + "%",
                StatType.HealBonus => "+" + (value * 100f).ToString("0") + "%",
                _ => value.ToString("0"),
            };
        }

        /// <summary>
        /// 符文 statMods 对单项属性的加成：sum(flat) + base * sum(percent)。
        /// </summary>
        public static float SumEquipmentBonus(float baseValue, StatType stat, IReadOnlyList<StatModifier> mods)
        {
            if (mods == null || mods.Count == 0)
                return 0f;

            var flat = 0f;
            var pct = 0f;
            for (var i = 0; i < mods.Count; i++)
            {
                var m = mods[i];
                if (m.stat != stat) continue;
                flat += m.flat;
                pct += m.percent;
            }

            return flat + baseValue * pct;
        }

        public static float ComputeEffective(float baseValue, StatType stat, IReadOnlyList<StatModifier> mods)
        {
            return baseValue + SumEquipmentBonus(baseValue, stat, mods);
        }
    }
}
