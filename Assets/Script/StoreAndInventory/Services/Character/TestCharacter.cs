using System.Collections.Generic;
using UnityEngine;

namespace StoreAndInventory
{
    /// <summary>
    /// 测试用角色基础属性 stub；主工程接入时替换为真实角色组件。
    /// 调用方：InventoryUI（属性显示）、StoreSaveService（character 存档块）。
    /// </summary>
    public class TestCharacter : MonoBehaviour
    {
        [SerializeField] CharacterStatsData stats = new();

        void Awake()
        {
            EnsureDefaultEntries();
        }

        public float Get(StatType stat)
        {
            for (var i = 0; i < stats.entries.Count; i++)
            {
                var e = stats.entries[i];
                if (e != null && e.stat == stat)
                    return e.baseValue;
            }

            return 0f;
        }

        public IReadOnlyList<CharacterStatEntry> GetAll()
        {
            return stats.entries;
        }

        public CharacterStatsData Data => stats;

        public void LoadFromData(CharacterStatsData source)
        {
            stats = source ?? new CharacterStatsData();
            if (stats.entries == null)
                stats.entries = new List<CharacterStatEntry>();
            EnsureDefaultEntries();
        }

        public void LogAll()
        {
            EnsureDefaultEntries();
            for (var i = 0; i < stats.entries.Count; i++)
            {
                var e = stats.entries[i];
                if (e == null) continue;
                Debug.Log($"[TestCharacter] {StatDisplayUtil.Label(e.stat)}={StatDisplayUtil.FormatValue(e.stat, e.baseValue)}");
            }
        }

        public static string StatLabel(StatType stat) => StatDisplayUtil.Label(stat);

        public static string FormatValue(StatType stat, float value) => StatDisplayUtil.FormatValue(stat, value);

        void EnsureDefaultEntries()
        {
            foreach (StatType stat in System.Enum.GetValues(typeof(StatType)))
            {
                if (HasStat(stat)) continue;
                stats.entries.Add(new CharacterStatEntry { stat = stat, baseValue = 0f });
            }
        }

        bool HasStat(StatType stat)
        {
            for (var i = 0; i < stats.entries.Count; i++)
            {
                if (stats.entries[i] != null && stats.entries[i].stat == stat)
                    return true;
            }

            return false;
        }
    }
}
