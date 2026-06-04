using UnityEngine;

namespace StoreAndInventory
{
    /// <summary>
    /// 供主工程 <see cref="BattleManager"/> 调用：开战 Apply / 收战 Restore。
    /// 挂 <c>Service.prefab</c>，拖 <see cref="EquipmentService"/>；Stat Source 可拖玩家或留空自动 Find。
    /// </summary>
    public class BattleStatSyncBridge : MonoBehaviour
    {
        public static BattleStatSyncBridge Instance { get; private set; }

        [SerializeField] EquipmentService equipmentService;
        [SerializeField] MainCharacterStatSource statSource;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[BattleStatSyncBridge] Duplicate instance; keeping first.");
                return;
            }

            Instance = this;
            ResolveReferences();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>开战：主角装备加成写入 CharacterStats。</summary>
        public static void ApplyToCharacterStats(CharacterStats stats)
        {
            if (stats == null)
                return;

            if (Instance != null)
            {
                Instance.ResolveReferences();
                EquipmentBattleStatApplicator.ApplyTo(
                    stats,
                    Instance.equipmentService,
                    Instance.statSource);
                return;
            }

            EquipmentBattleStatApplicator.ApplyTo(
                stats,
                Object.FindObjectOfType<EquipmentService>(),
                CharacterStatSourceLocator.ResolveInterface());
        }

        /// <summary>收战：还原战前基础五维。</summary>
        public static void RestoreCharacterStats(CharacterStats stats)
        {
            EquipmentBattleStatApplicator.Restore(stats);
        }

        /// <summary>获取指定技能槽符文的攻击加成（技能专属，不全局应用）。</summary>
        public static int GetRuneAttackBonus(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return 0;

            if (Instance == null) return 0;
            Instance.ResolveReferences();
            var equip = Instance.equipmentService;
            if (equip == null) return 0;

            var slotIndex = equip.GetSlotIndexBySkillId(skillId);
            if (slotIndex < 0) return 0;

            var rune = equip.GetEquippedItem(slotIndex);
            if (rune == null || rune.statMods == null) return 0;

            int bonus = 0;
            for (int i = 0; i < rune.statMods.Count; i++)
            {
                var mod = rune.statMods[i];
                if (mod.stat == StatType.Attack)
                    bonus += Mathf.RoundToInt(mod.flat);
            }
            return bonus;
        }

        /// <summary>获取指定技能槽符文（供 UI 查询）。</summary>
        public static Equipment GetEquippedRuneForSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId) || Instance == null) return null;
            Instance.ResolveReferences();
            var equip = Instance.equipmentService;
            if (equip == null) return null;

            var slotIndex = equip.GetSlotIndexBySkillId(skillId);
            return slotIndex >= 0 ? equip.GetEquippedItem(slotIndex) : null;
        }

        void ResolveReferences()
        {
            if (equipmentService == null)
                equipmentService = FindObjectOfType<EquipmentService>();

            if (statSource == null)
                statSource = CharacterStatSourceLocator.Resolve();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (equipmentService == null)
                equipmentService = FindObjectOfType<EquipmentService>();
        }
#endif
    }
}
