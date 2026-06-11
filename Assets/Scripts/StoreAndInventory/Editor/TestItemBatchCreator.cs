#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace StoreAndInventory.Editor
{
    /// <summary>
    /// 一键生成测试物品：10 药水 + 10 符文 + 3 收藏品 + GameplayEffectSO 资产。
    /// 菜单：MoYuan/Shop/Test Data/Create All Test Items
    /// </summary>
    public static class TestItemBatchCreator
    {
        const string ItemDir = "Assets/Resources/Item";
        const string EffectDir = "Assets/Resources/Item/Effects";

        [MenuItem("MoYuan/Shop/Test Data/Create All Test Items")]
        public static void CreateAll()
        {
            EnsureDirectory(ItemDir);
            EnsureDirectory(EffectDir);

            // ── 1. 创建 GameplayEffectSO 资产 ──
            var fxHpL        = CreateEffect("hp_restore_l",       "生命秘药",    "恢复60HP",                   e => { e.instantHealHp = 60; });
            var fxMpL        = CreateEffect("mp_restore_l",       "法力秘药",    "恢复40MP",                   e => { e.instantHealMp = 40; });
            var fxShield     = CreateEffect("shield_25",          "墨盾药剂",    "获得25护盾",                 e => { e.instantShield = 25; });
            var fxCrit15     = CreateEffect("crit_bonus_15pct",   "暴击灵药",    "暴击率+15%",                 e => { e.critRateBonus = 0.15f; });
            var fxFire25     = CreateEffect("fire_bonus_25pct",   "烈焰精华",    "火伤+25%",                   e => { e.fireDamageBonusPercent = 0.25f; });
            var fxIce25      = CreateEffect("ice_bonus_25pct",    "冰霜精华",    "冰伤+25%",                   e => { e.iceDamageBonusPercent = 0.25f; });
            var fxBerserker  = CreateEffect("berserker_draft",    "狂墨药剂",    "攻击+10防御-5持续3回合",     e => { e.tempAttackBonus = 10; e.tempDefenseBonus = -5; e.tempBuffDuration = 3; });
            var fxIronSkin   = CreateEffect("iron_skin_draft",    "铁壁药剂",    "防御+8持续3回合",            e => { e.tempDefenseBonus = 8; e.tempBuffDuration = 3; });
            var fxUltCharge  = CreateEffect("ult_charge_30",      "奥义灵露",    "大招充能+30",                e => { e.instantUltCharge = 30; });
            var fxVampPotion = CreateEffect("vampiric_potion",    "吸血灵药",    "所有攻击吸血8%",             e => { e.lifestealPercent = 0.08f; });

            // 符文用效果
            var fxFireBonus10  = CreateEffect("fire_dmg_bonus_10pct",  "炎灵符文",  "火伤+10%",    e => { e.fireDamageBonusPercent = 0.10f; });
            var fxIceBonus10   = CreateEffect("ice_dmg_bonus_10pct",   "霜灵符文",  "冰伤+10%",    e => { e.iceDamageBonusPercent = 0.10f; });
            var fxLifesteal    = CreateEffect("lifesteal_cishi_10pct", "吸血符文",  "吸血10%",     e => { e.lifestealPercent = 0.10f; });
            var fxCrit8        = CreateEffect("crit_rune_8pct",        "暴击符文",  "暴击率+8%",   e => { e.critRateBonus = 0.08f; e.critDamageBonus = 0.25f; });
            var fxShieldBonus  = CreateEffect("shield_bonus_20pct",    "护盾符文",  "护盾量+20%",  e => { e.shieldBonusPercent = 0.20f; });
            var fxUltRune      = CreateEffect("ultimate_boost",        "万剑符文",  "大招伤+25%",  e => { e.ultimateDamageBonusPercent = 0.25f; });

            // ── 2. 创建消耗品 ──
            CreateConsumable("hp_potion_l",      "生命秘药",   "恢复60HP",                     60,  ShopTag.HP,      UseContext.Any,     20, ItemRarity.Rare,       new List<GameplayEffectSO> { fxHpL });
            CreateConsumable("mp_potion_l",      "法力秘药",   "恢复40MP",                     60,  ShopTag.MP,      UseContext.Any,     20, ItemRarity.Rare,       new List<GameplayEffectSO> { fxMpL });
            CreateConsumable("shield_potion",    "墨盾药剂",   "获得25护盾，持续整场战斗",     70,  ShopTag.Def,     UseContext.Any,     20, ItemRarity.Rare,       new List<GameplayEffectSO> { fxShield });
            CreateConsumable("crit_potion",      "暴击灵药",   "本场战斗暴击率+15%",           80,  ShopTag.Crit,    UseContext.Battle,  20, ItemRarity.Epic,       new List<GameplayEffectSO> { fxCrit15 });
            CreateConsumable("fire_elixir",      "烈焰精华",   "本场战斗火元素伤害+25%",       90,  ShopTag.Elem,    UseContext.Battle,  20, ItemRarity.Epic,       new List<GameplayEffectSO> { fxFire25 });
            CreateConsumable("ice_elixir",       "冰霜精华",   "本场战斗冰元素伤害+25%",       90,  ShopTag.Elem,    UseContext.Battle,  20, ItemRarity.Epic,       new List<GameplayEffectSO> { fxIce25 });
            CreateConsumable("berserker_draft",  "狂墨药剂",   "攻击+10但防御-5，持续3回合",   100, ShopTag.AP,      UseContext.Battle,  20, ItemRarity.Epic,       new List<GameplayEffectSO> { fxBerserker });
            CreateConsumable("iron_skin_potion", "铁壁药剂",   "防御+8，持续3回合",            100, ShopTag.Def,     UseContext.Battle,  20, ItemRarity.Epic,       new List<GameplayEffectSO> { fxIronSkin });
            CreateConsumable("ult_charge_potion","奥义灵露",   "大招充能+30",                  150, ShopTag.KO,      UseContext.Any,     10, ItemRarity.Legendary,  new List<GameplayEffectSO> { fxUltCharge });
            CreateConsumable("vampiric_potion",  "吸血灵药",   "本场战斗所有攻击吸血8%",       120, ShopTag.Special, UseContext.Battle,  10, ItemRarity.Legendary,  new List<GameplayEffectSO> { fxVampPotion });

            // ── 3. 创建符文 ──
            CreateEquipment("iron_wall",       "铁壁符文",     "坚如磐石的防御符文",           100, ItemRarity.Common,
                new List<StatModifier> { new() { stat = StatType.Defense, flat = 5 }, new() { stat = StatType.MaxHp, flat = 30 } },
                null, null);

            CreateEquipment("mana_flow",       "泉涌符文",     "源源不断的法力之泉",           100, ItemRarity.Common,
                new List<StatModifier> { new() { stat = StatType.MaxMp, flat = 25 } },
                null, null);

            CreateEquipment("flame_rune",      "炎灵符文",     "火元素技能伤害大幅提升",       180, ItemRarity.Rare,
                new List<StatModifier> { new() { stat = StatType.Attack, flat = 8 } },
                new List<SkillModifier> { new() { targetKind = SkillModTarget.SkillTag, targetId = "Fire", modType = SkillModType.DamageBonus, value = 0.15f } },
                new List<GameplayEffectSO> { fxFireBonus10 });

            CreateEquipment("frost_rune",      "霜灵符文",     "冰元素技能伤害大幅提升",       180, ItemRarity.Rare,
                new List<StatModifier> { new() { stat = StatType.Attack, flat = 5 } },
                new List<SkillModifier> { new() { targetKind = SkillModTarget.SkillTag, targetId = "Ice", modType = SkillModType.DamageBonus, value = 0.15f } },
                new List<GameplayEffectSO> { fxIceBonus10 });

            CreateEquipment("vampiric_rune",   "吸血符文",     "墨刺命中时吸取生命力",         200, ItemRarity.Rare,
                null,
                new List<SkillModifier> { new() { targetKind = SkillModTarget.SkillId, targetId = "墨刺", modType = SkillModType.DamageBonus, value = 0.20f } },
                new List<GameplayEffectSO> { fxLifesteal });

            CreateEquipment("crit_rune",       "暴击符文",     "提升暴击率与暴击伤害",         250, ItemRarity.Epic,
                new List<StatModifier> { new() { stat = StatType.Attack, flat = 5 } },
                null,
                new List<GameplayEffectSO> { fxCrit8 });

            CreateEquipment("mograng_rune",    "墨涌强化符文", "墨涌伤害大幅提升",             280, ItemRarity.Epic,
                null,
                new List<SkillModifier> { new() { targetKind = SkillModTarget.SkillId, targetId = "墨涌", modType = SkillModType.DamageBonus, value = 0.30f } },
                null);

            CreateEquipment("hanmo_rune",      "寒墨节能符文", "减少寒墨的MP消耗",             250, ItemRarity.Epic,
                null,
                new List<SkillModifier> { new() { targetKind = SkillModTarget.SkillId, targetId = "寒墨", modType = SkillModType.MpCostReduce, value = 1f } },
                null);

            CreateEquipment("shield_rune",     "墨壁强化符文", "提升护盾量与防御力",           300, ItemRarity.Epic,
                new List<StatModifier> { new() { stat = StatType.Defense, flat = 5 } },
                null,
                new List<GameplayEffectSO> { fxShieldBonus });

            CreateEquipment("ultimate_rune",   "万剑符文",     "大招伤害大幅提升，充能加速",   500, ItemRarity.Legendary,
                new List<StatModifier> { new() { stat = StatType.Attack, flat = 10 } },
                null,
                new List<GameplayEffectSO> { fxUltRune });

            // ── 4. 创建收藏品 ──
            CreateStoryItem("souvenir_3", "墨龙鳞片", "传说中墨龙褪下的鳞片，散发着深邃的墨气", 300);
            CreateStoryItem("souvenir_4", "古墨残卷", "记载着失传墨术的残破卷轴",               350);
            CreateStoryItem("souvenir_5", "灵墨结晶", "由纯粹墨气凝聚而成的结晶体",             400);

            // ── 5. 创建商店配置表并关联所有物品 ──
            var tablePath = $"{ItemDir}/shop_default_墨渊商店.asset";
            var table = AssetDatabase.LoadAssetAtPath<ShopTableSO>(tablePath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<ShopTableSO>();
                table.idSuffix = "default";
                table.shopId = "shop_default";
                table.displayName = "墨渊商店";
                table.priceMultiplier = 1f;
                AssetDatabase.CreateAsset(table, tablePath);
            }

            table.fixedStock.Clear();
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
                    _ => 10,  // 消耗品默认10
                };

                table.fixedStock.Add(new ShopEntry
                {
                    item = item,
                    priceOverride = 0,
                    stock = stock,
                    weight = 1f
                });
            }

            EditorUtility.SetDirty(table);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TestItemBatchCreator] 全部测试物品创建完成！商店配置表关联 {table.fixedStock.Count} 个物品。");
        }

        // ── 内部创建方法 ──

        static GameplayEffectSO CreateEffect(string effectId, string displayName, string desc, System.Action<GameplayEffectSO> configure)
        {
            var path = $"{EffectDir}/fx_{effectId}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<GameplayEffectSO>(path);
            if (existing != null) return existing;

            var so = ScriptableObject.CreateInstance<GameplayEffectSO>();
            so.effectId = effectId;
            so.displayNameKey = displayName;
            so.descriptionKey = desc;
            configure(so);
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        static void CreateConsumable(string idSuffix, string itemName, string desc,
            int price, ShopTag tag, UseContext useContext, int stack, ItemRarity rarity,
            List<GameplayEffectSO> effects)
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
            so.rarity = rarity;
            so.shopTag = tag;
            so.maxStack = stack;
            so.useContext = useContext;
            if (effects != null) so.useEffects = effects;
            AssetDatabase.CreateAsset(so, path);
        }

        static void CreateEquipment(string idSuffix, string itemName, string desc,
            int price, ItemRarity rarity,
            List<StatModifier> statMods, List<SkillModifier> skillMods, List<GameplayEffectSO> extraEffects)
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
            so.rarity = rarity;
            so.shopTag = ShopTag.None;
            so.maxStack = 1;
            if (statMods != null) so.statMods = statMods;
            if (skillMods != null) so.skillMods = skillMods;
            if (extraEffects != null) so.extraEffects = extraEffects;
            AssetDatabase.CreateAsset(so, path);
        }

        static void CreateStoryItem(string idSuffix, string itemName, string desc, int price)
        {
            var path = $"{ItemDir}/story_{idSuffix}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<StoryItem>(path);
            if (existing != null) return;

            var so = ScriptableObject.CreateInstance<StoryItem>();
            so.idSuffix = idSuffix;
            so.id = "story_" + idSuffix;
            so.Name = itemName;
            so.description = desc;
            so.basePrice = price;
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
