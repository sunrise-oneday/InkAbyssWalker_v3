using UnityEngine;

namespace StoreAndInventory
{
    /// <summary>
    /// 战斗开战：将装备 statMods 写入主角 <see cref="CharacterStats"/> 基础五维（与面板同一公式）。
    /// 收战：Restore 还原为战前基础值。不创建 Buff，不写探索常驻数值。
    /// </summary>
    public static class EquipmentBattleStatApplicator
    {
        struct StatSnapshot
        {
            public int attack;
            public int defense;
            public int maxHP;
            public int maxMP;
            public int maxBreakValue;
        }

        static CharacterStats activeStats;
        static StatSnapshot snapshot;
        static bool hasSnapshot;

        /// <summary>
        /// 开战前调用：快照基础五维并写入 effective。无装备且无有效 Source 时不改动。
        /// </summary>
        public static void ApplyTo(
            CharacterStats stats,
            EquipmentService equipment,
            ICharacterStatSource source)
        {
            if (stats == null)
                return;

            equipment ??= Object.FindObjectOfType<EquipmentService>();
            source ??= CharacterStatSourceLocator.ResolveInterface();

            var mods = equipment != null ? equipment.GetAllStatMods() : null;
            var hasMods = mods != null && mods.Count > 0;
            var boundSource = source is MainCharacterStatSource ms && ms.IsBound;

            if (!hasMods && !boundSource)
                return;

            if (hasSnapshot && activeStats == stats)
                Restore(stats);

            snapshot = CaptureBaseSnapshot(stats, source);
            hasSnapshot = true;
            activeStats = stats;

            var effectiveAttack = RoundEffective(stats, source, StatType.Attack, mods);
            var effectiveDefense = RoundEffective(stats, source, StatType.Defense, mods);
            var effectiveMaxHp = RoundEffective(stats, source, StatType.MaxHp, mods);
            var effectiveMaxMp = RoundEffective(stats, source, StatType.MaxMp, mods);
            var effectiveMaxBreak = RoundEffective(stats, source, StatType.MaxBreak, mods);

            stats.attack = effectiveAttack;
            stats.defense = effectiveDefense;
            stats.maxHP = effectiveMaxHp;
            stats.maxMP = effectiveMaxMp;
            stats.maxBreakValue = effectiveMaxBreak;

            stats.currentHP = Mathf.Min(stats.currentHP, stats.maxHP);
            stats.currentMP = Mathf.Min(stats.currentMP, stats.maxMP);
            stats.currentBreakValue = Mathf.Min(stats.currentBreakValue, stats.maxBreakValue);

            stats.OnHPChanged?.Invoke();
            stats.OnBreakChanged?.Invoke();
        }

        /// <summary>收战后调用：还原开战前基础五维。</summary>
        public static void Restore(CharacterStats stats)
        {
            if (stats == null || !hasSnapshot || activeStats != stats)
                return;

            stats.attack = snapshot.attack;
            stats.defense = snapshot.defense;
            stats.maxHP = snapshot.maxHP;
            stats.maxMP = snapshot.maxMP;
            stats.maxBreakValue = snapshot.maxBreakValue;

            stats.currentHP = Mathf.Min(stats.currentHP, stats.maxHP);
            stats.currentMP = Mathf.Min(stats.currentMP, stats.maxMP);
            stats.currentBreakValue = Mathf.Min(stats.currentBreakValue, stats.maxBreakValue);

            stats.OnHPChanged?.Invoke();
            stats.OnBreakChanged?.Invoke();

            activeStats = null;
            hasSnapshot = false;
        }

        static StatSnapshot CaptureBaseSnapshot(CharacterStats stats, ICharacterStatSource source)
        {
            if (source is MainCharacterStatSource m && m.IsBound)
            {
                return new StatSnapshot
                {
                    attack = Round(source.Get(StatType.Attack)),
                    defense = Round(source.Get(StatType.Defense)),
                    maxHP = Round(source.Get(StatType.MaxHp)),
                    maxMP = Round(source.Get(StatType.MaxMp)),
                    maxBreakValue = Round(source.Get(StatType.MaxBreak))
                };
            }

            return new StatSnapshot
            {
                attack = stats.attack,
                defense = stats.defense,
                maxHP = stats.maxHP,
                maxMP = stats.maxMP,
                maxBreakValue = stats.maxBreakValue
            };
        }

        static int RoundEffective(
            CharacterStats stats,
            ICharacterStatSource source,
            StatType stat,
            System.Collections.Generic.IReadOnlyList<StatModifier> mods)
        {
            var baseValue = source is MainCharacterStatSource m && m.IsBound
                ? source.Get(stat)
                : CharacterStatFieldMap.ReadBaseValue(stat, stats);

            return Round(StatDisplayUtil.ComputeEffective(baseValue, stat, mods));
        }

        static int Round(float value) => Mathf.Max(0, Mathf.RoundToInt(value));
    }
}
