using System.Collections.Generic;

namespace StoreAndInventory
{
    public static class StatDisplayUtil
    {
        public static string Label(StatType stat)
        {
            return stat switch
            {
                StatType.Attack => "Attack",
                StatType.MaxHp => "Max HP",
                StatType.Defense => "Defense",
                StatType.Speed => "Speed",
                StatType.CritRate => "Crit Rate",
                _ => stat.ToString()
            };
        }

        public static string FormatValue(StatType stat, float value)
        {
            return stat == StatType.CritRate ? value.ToString("0.##") : value.ToString("0");
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
