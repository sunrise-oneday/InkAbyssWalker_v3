namespace StoreAndInventory
{
    /// <summary>
    /// 面板用：基础属性 + 装备 statMods 有效值（不写回 CharacterStats）。
    /// </summary>
    public static class EffectiveStatCalculator
    {
        public static float GetBase(ICharacterStatSource source, StatType stat)
        {
            return source != null ? source.Get(stat) : 0f;
        }

        public static float GetEquipmentBonus(
            ICharacterStatSource source,
            StatType stat,
            EquipmentService equipment)
        {
            var baseValue = GetBase(source, stat);
            var mods = equipment != null ? equipment.GetAllStatMods() : null;
            return StatDisplayUtil.SumEquipmentBonus(baseValue, stat, mods);
        }

        public static float GetEffective(
            ICharacterStatSource source,
            StatType stat,
            EquipmentService equipment)
        {
            var baseValue = GetBase(source, stat);
            return StatDisplayUtil.ComputeEffective(baseValue, stat,
                equipment != null ? equipment.GetAllStatMods() : null);
        }
    }
}
