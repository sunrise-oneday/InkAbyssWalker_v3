#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace StoreAndInventory.Editor
{
    /// <summary>
    /// 一键创建商店默认物品 + 商店配置表。
    /// 菜单：MoYuan/Shop/Create Default Shop Data
    /// </summary>
    public static class ShopDefaultDataCreator
    {
        const string ItemDir = "Assets/Resources/Item";

        [MenuItem("MoYuan/Shop/Create Default Shop Data")]
        public static void CreateAll()
        {
            EnsureDirectory(ItemDir);

            // ── 消耗品（墨币） ──
            CreateConsumable("hp_potion", "生命药水", "恢复少量HP", 30, ShopTag.HP, 99);
            CreateConsumable("mp_potion", "法力药水", "恢复少量MP", 30, ShopTag.MP, 99);
            CreateConsumable("ap_potion", "能量药水", "恢复少量AP", 40, ShopTag.AP, 99);
            CreateConsumable("ko_potion", "大招药水", "恢复大招能量", 50, ShopTag.KO, 99);

            // ── 符文（灵石） ──
            CreateEquipment("hp_rune", "生命符文", "增加生命上限", 100, ShopTag.HP,
                new List<StatModifier> { new() { stat = StatType.MaxHp, flat = 50, percent = 0 } });
            CreateEquipment("mp_rune", "法力符文", "增加法力上限", 100, ShopTag.MP,
                new List<StatModifier> { new() { stat = StatType.MaxMp, flat = 30, percent = 0 } });
            CreateEquipment("atk_rune", "攻击符文", "增加攻击力", 120, ShopTag.AP,
                new List<StatModifier> { new() { stat = StatType.Attack, flat = 15, percent = 0 } });
            CreateEquipment("ko_rune", "破防符文", "增加破防上限", 150, ShopTag.KO,
                new List<StatModifier> { new() { stat = StatType.MaxBreak, flat = 20, percent = 0 } });

            // ── 收藏品（灵石） ──
            CreateStoryItem("souvenir_1", "古老徽章", "一枚刻有神秘符文的古老徽章");
            CreateStoryItem("souvenir_2", "破碎地图", "记录着未知遗迹位置的残破地图");

            // ── 商店配置表 ──
            var table = ScriptableObject.CreateInstance<ShopTableSO>();
            table.idSuffix = "default";
            table.shopId = "shop_default";
            table.displayName = "墨渊商店";
            table.priceMultiplier = 1f;

            var allItems = new List<ItemBase>();
            allItems.AddRange(Resources.LoadAll<Consumable>("Item"));
            allItems.AddRange(Resources.LoadAll<Equipment>("Item"));
            allItems.AddRange(Resources.LoadAll<StoryItem>("Item"));

            foreach (var item in allItems)
            {
                if (item == null) continue;

                var stock = item.Category switch
                {
                    ItemCategory.Equipment => 5,
                    ItemCategory.StoryItem => 1,
                    _ => 10,
                };

                table.fixedStock.Add(new ShopEntry
                {
                    item = item,
                    priceOverride = 0,
                    stock = stock,
                    weight = 1f
                });
            }

            AssetDatabase.CreateAsset(table, $"{ItemDir}/shop_default.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ShopDefaultDataCreator] 创建完成！");
        }

        // ── 创建方法 ──

        static void CreateConsumable(string idSuffix, string itemName, string desc, int price, ShopTag tag, int stack)
        {
            var path = $"{ItemDir}/cons_{idSuffix}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Consumable>(path);
            if (existing != null) return;

            var so = ScriptableObject.CreateInstance<Consumable>();
            so.idSuffix = idSuffix;
            so.id = "cons_" + idSuffix;
            so.Name = itemName;
            so.description = desc;
            so.basePrice = price;
            so.currencyId = CurrencyId.Ink;
            so.shopTag = tag;
            so.maxStack = stack;
            so.useContext = UseContext.Any;
            AssetDatabase.CreateAsset(so, path);
        }

        static void CreateEquipment(string idSuffix, string itemName, string desc, int price, ShopTag tag, List<StatModifier> mods)
        {
            var path = $"{ItemDir}/equip_{idSuffix}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Equipment>(path);
            if (existing != null) return;

            var so = ScriptableObject.CreateInstance<Equipment>();
            so.idSuffix = idSuffix;
            so.id = "equip_" + idSuffix;
            so.Name = itemName;
            so.description = desc;
            so.basePrice = price;
            so.currencyId = CurrencyId.Spirit;
            so.shopTag = tag;
            so.maxStack = 1;
            so.statMods = mods;
            AssetDatabase.CreateAsset(so, path);
        }

        static void CreateStoryItem(string idSuffix, string itemName, string desc)
        {
            var path = $"{ItemDir}/story_{idSuffix}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<StoryItem>(path);
            if (existing != null) return;

            var so = ScriptableObject.CreateInstance<StoryItem>();
            so.idSuffix = idSuffix;
            so.id = "story_" + idSuffix;
            so.Name = itemName;
            so.description = desc;
            so.basePrice = 200;
            so.currencyId = CurrencyId.Spirit;
            so.shopTag = ShopTag.None;
            so.maxStack = 1;
            so.canSell = false;
            AssetDatabase.CreateAsset(so, path);
        }

        static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
