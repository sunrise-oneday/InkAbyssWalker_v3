using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StoreAndInventory
{
    public abstract class ItemBase : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("策划只填后缀，例如 huoyan_fu；前缀由子类自动加")]
        public string idSuffix;

        [ReadOnly]
        [Tooltip("完整 id = 子类前缀 + idSuffix；由 OnValidate 自动写入，请勿手填")]
        public string id;

        public string Name;

        [TextArea]
        public string description;
        public Sprite icon;

        [Header("通用")]
        public ItemRarity rarity = ItemRarity.Common;
        public int basePrice;
        public bool canSell = true;
        public int maxStack = 1;

        public abstract ItemCategory Category { get; }
        public virtual bool CanDiscard => true;

        public virtual int MaxStack => Mathf.Max(1, maxStack);

        protected virtual string FilePrefix => "item_";

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            SyncIdFromSuffix();
            ValidateIdPrefix();
            ScheduleRename();
        }

        void SyncIdFromSuffix()
        {
            if (string.IsNullOrEmpty(idSuffix)) return;

            var target = FilePrefix + idSuffix;
            if (id == target) return;
            id = target;
        }

        void ValidateIdPrefix()
        {
            if (string.IsNullOrEmpty(id)) return;

            var expected = FilePrefix.TrimEnd('_');
            if (string.IsNullOrEmpty(expected)) return;

            if (!id.StartsWith(expected + "_") && id != expected)
                Debug.LogWarning($"[ItemBase] id 不符合前缀规则：{id}（应以 \"{expected}_\" 开头）", this);
        }

        void ScheduleRename()
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(Name))
                return;

            var targetName = id + "_" + Name;
            if (name == targetName)
                return;

            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                var path = AssetDatabase.GetAssetPath(this);
                if (string.IsNullOrEmpty(path)) return;
                if (name == targetName) return;
                AssetDatabase.RenameAsset(path, targetName);
                AssetDatabase.SaveAssets();
            };
        }
#endif
    }
}
