using UnityEngine;

namespace StoreAndInventory
{
    /// <summary>
    /// 从主工程玩家 <see cref="CharacterStats"/> 读取基础上限属性（只读）。挂探索玩家 Prefab，勿挂在 Service.prefab。
    /// </summary>
    public class MainCharacterStatSource : MonoBehaviour, ICharacterStatSource
    {
        [SerializeField] CharacterStats stats;

        public bool IsBound => stats != null;

        void Awake()
        {
            ResolveReferences();
        }

        void Reset()
        {
            stats = GetComponent<CharacterStats>();
        }

        public float Get(StatType stat)
        {
            if (stats == null)
                ResolveReferences();

            return CharacterStatFieldMap.ReadBaseValue(stat, stats);
        }

        void ResolveReferences()
        {
            if (stats == null)
                stats = GetComponent<CharacterStats>();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (stats == null)
                stats = GetComponent<CharacterStats>();
        }
#endif
    }
}
