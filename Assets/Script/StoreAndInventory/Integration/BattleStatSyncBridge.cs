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
