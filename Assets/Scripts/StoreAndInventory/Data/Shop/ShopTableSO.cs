using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StoreAndInventory
{
    [CreateAssetMenu(fileName = "shop_", menuName = "MoYuan/Shop/Table")]
    public class ShopTableSO : ScriptableObject
    {
        const string FilePrefix = "shop_";

        [Header("基础信息")]
        [Tooltip("策划只填后缀，例如 test_zahuopu；前缀 shop_ 自动加")]
        public string idSuffix;

        [ReadOnly]
        [Tooltip("完整 shopId = shop_ + idSuffix；由 OnValidate 自动写入")]
        public string shopId;

        public string displayName;

        [Header("货架")]
        public List<ShopEntry> fixedStock = new();
        public List<ShopEntry> randomPool = new();
        public int randomSlotCount = 3;
        public float priceMultiplier = 1f;
        public int refreshCostCurrency = 0;

#if UNITY_EDITOR
        void OnValidate()
        {
            MigrateLegacyId();
            SyncIdFromSuffix();
            ValidateIdPrefix();
            ScheduleRename();
        }

        void MigrateLegacyId()
        {
            if (!string.IsNullOrEmpty(idSuffix))
                return;

            if (string.IsNullOrEmpty(shopId))
                return;

            if (shopId.StartsWith(FilePrefix))
            {
                idSuffix = shopId.Substring(FilePrefix.Length);
                return;
            }

            idSuffix = shopId;
        }

        void SyncIdFromSuffix()
        {
            if (string.IsNullOrEmpty(idSuffix))
                return;

            var target = FilePrefix + idSuffix;
            if (shopId == target)
                return;

            shopId = target;
        }

        void ValidateIdPrefix()
        {
            if (string.IsNullOrEmpty(shopId))
                return;

            if (!shopId.StartsWith(FilePrefix))
                Debug.LogWarning($"[ShopTableSO] shopId 应以 \"{FilePrefix}\" 开头：{shopId}", this);
        }

        void ScheduleRename()
        {
            if (string.IsNullOrEmpty(shopId) || string.IsNullOrEmpty(displayName))
                return;

            var targetName = shopId + "_" + displayName;
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
