using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace StoreAndInventory
{
    /// <summary>
    /// 符文槽装/卸与效果聚合查询。
    /// statMods 供 UI/主工程读；skillMods、extraEffects 仅聚合，本系统不执行。
    /// 调用方：InventoryUI、主工程战斗/技能系统（GetAll*）、StoreSaveService。
    /// </summary>
    public class EquipmentService : MonoBehaviour
    {
        [SerializeField] Inventory inventory;
        [SerializeField] ItemDatabase database;
        [SerializeField] int runeSlotCount = 3;

        readonly EquipmentData data = new();
        readonly EquippedCacheData equippedCache = new();

        public int RuneSlotCount => runeSlotCount;
        public EquipmentData Data => data;
        public EquippedCacheData EquippedCache => equippedCache;

        public event Action<int, ItemStack> OnEquipped;
        public event Action<int, ItemStack> OnUnequipped;

        readonly List<StatModifier> statModCache = new();
        readonly List<SkillModifier> skillModCache = new();
        readonly List<GameplayEffectSO> extraEffectCache = new();
        bool modCacheDirty = true;

        void Awake()
        {
            if (inventory == null)
                inventory = FindObjectOfType<Inventory>();
            if (database == null)
                database = FindObjectOfType<ItemDatabase>();

            EnsureSlotCount();
        }

        public bool TryEquipFromBag(int bagSlotIndex, int runeSlotIndex, out string message)
        {
            message = null;

            if (inventory == null)
            {
                message = "inventory missing";
                return false;
            }

            if (runeSlotIndex < 0 || runeSlotIndex >= runeSlotCount)
            {
                message = $"invalid rune slot {runeSlotIndex}";
                return false;
            }

            if (bagSlotIndex < 0 || bagSlotIndex >= inventory.Count)
            {
                message = $"invalid bag index {bagSlotIndex}";
                return false;
            }

            var stack = inventory.Items[bagSlotIndex];
            if (stack == null || stack.IsEmpty)
            {
                message = "empty bag slot";
                return false;
            }

            if (!inventory.TryGetDefinition(stack.definitionId, out var def))
            {
                message = $"unknown item {stack.definitionId}";
                return false;
            }

            if (def.Category != ItemCategory.Equipment)
            {
                message = "not equipment";
                return false;
            }

            if (stack.count != 1)
            {
                message = "equipment count must be 1";
                return false;
            }

            if (string.IsNullOrEmpty(stack.instanceGuid))
            {
                message = "equipment missing instance guid";
                return false;
            }

            EnsureSlotCount();

            var targetGuid = stack.instanceGuid;

            var existingGuid = data.runes[runeSlotIndex].instanceGuid;
            if (!string.IsNullOrEmpty(existingGuid))
            {
                if (!TryUnequip(runeSlotIndex, out message))
                    return false;
            }

            var takeIndex = FindBagIndexByGuid(targetGuid);
            if (takeIndex < 0)
            {
                message = "item no longer in bag";
                return false;
            }

            if (!inventory.TryTakeAt(takeIndex, out var taken, out message))
                return false;

            PutInCache(taken);
            data.runes[runeSlotIndex].instanceGuid = taken.instanceGuid;

            LogEquip($"Equip slot={runeSlotIndex} guid={taken.instanceGuid} id={taken.definitionId}");
            InvalidateModCache();
            LogEquippedStatMods(def as Equipment);
            OnEquipped?.Invoke(runeSlotIndex, taken);
            return true;
        }

        public bool TryUnequip(int runeSlotIndex, out string message)
        {
            message = null;

            if (inventory == null)
            {
                message = "inventory missing";
                return false;
            }

            if (runeSlotIndex < 0 || runeSlotIndex >= runeSlotCount)
            {
                message = $"invalid rune slot {runeSlotIndex}";
                return false;
            }

            EnsureSlotCount();

            var guid = data.runes[runeSlotIndex].instanceGuid;
            if (string.IsNullOrEmpty(guid))
            {
                message = "slot empty";
                return false;
            }

            if (!TryTakeFromCache(guid, out var stack))
            {
                message = $"cache missing guid={guid}";
                data.runes[runeSlotIndex].instanceGuid = null;
                return false;
            }

            if (!inventory.TryAddStack(stack, out message))
            {
                PutInCache(stack);
                return false;
            }

            data.runes[runeSlotIndex].instanceGuid = null;
            LogEquip($"Unequip slot={runeSlotIndex} guid={stack.instanceGuid} id={stack.definitionId}");
            InvalidateModCache();
            LogAggregateStatMods("after unequip");
            OnUnequipped?.Invoke(runeSlotIndex, stack);
            return true;
        }

        public ItemStack GetEquippedStack(int runeSlotIndex)
        {
            if (runeSlotIndex < 0 || runeSlotIndex >= runeSlotCount)
                return null;

            EnsureSlotCount();

            var guid = data.runes[runeSlotIndex].instanceGuid;
            if (string.IsNullOrEmpty(guid))
                return null;

            return FindInCache(guid);
        }

        public Equipment GetEquippedItem(int runeSlotIndex)
        {
            var stack = GetEquippedStack(runeSlotIndex);
            if (stack == null || database == null)
                return null;

            return database.TryGet(stack.definitionId, out var def) ? def as Equipment : null;
        }

        public IReadOnlyList<StatModifier> GetAllStatMods()
        {
            EnsureModCache();
            return statModCache;
        }

        public IReadOnlyList<SkillModifier> GetAllSkillMods()
        {
            EnsureModCache();
            return skillModCache;
        }

        public IReadOnlyList<GameplayEffectSO> GetAllExtraEffects()
        {
            EnsureModCache();
            return extraEffectCache;
        }

        public EquipmentSaveData CaptureSaveData()
        {
            EnsureSlotCount();
            return new EquipmentSaveData
            {
                runeSlotCount = runeSlotCount,
                equipment = CloneEquipmentData(data),
                cache = CloneCache(equippedCache)
            };
        }

        public void ApplySaveData(EquipmentSaveData save)
        {
            if (save == null)
            {
                data.runes.Clear();
                equippedCache.entries.Clear();
                runeSlotCount = 3;
            }
            else
            {
                runeSlotCount = Mathf.Max(1, save.runeSlotCount);
                data.runes = save.equipment?.runes ?? new List<RuneSlotEntry>();
                equippedCache.entries = save.cache?.entries ?? new List<EquippedCacheEntry>();
            }

            EnsureSlotCount();
            InvalidateModCache();
        }

        void EnsureModCache()
        {
            if (!modCacheDirty) return;

            statModCache.Clear();
            skillModCache.Clear();
            extraEffectCache.Clear();

            for (var slot = 0; slot < runeSlotCount; slot++)
            {
                var equip = GetEquippedItem(slot);
                if (equip == null) continue;

                AppendModifiers(equip.statMods, statModCache);
                AppendModifiers(equip.skillMods, skillModCache);
                AppendEffects(equip.extraEffects, extraEffectCache);
            }

            modCacheDirty = false;
        }

        void InvalidateModCache()
        {
            modCacheDirty = true;
        }

        static void AppendModifiers(List<StatModifier> source, List<StatModifier> target)
        {
            if (source == null) return;
            for (var i = 0; i < source.Count; i++)
                target.Add(source[i]);
        }

        static void AppendModifiers(List<SkillModifier> source, List<SkillModifier> target)
        {
            if (source == null) return;
            for (var i = 0; i < source.Count; i++)
                target.Add(source[i]);
        }

        static void AppendEffects(List<GameplayEffectSO> source, List<GameplayEffectSO> target)
        {
            if (source == null) return;
            for (var i = 0; i < source.Count; i++)
            {
                var fx = source[i];
                if (fx != null)
                    target.Add(fx);
            }
        }

        static EquipmentData CloneEquipmentData(EquipmentData source)
        {
            var clone = new EquipmentData();
            if (source?.runes == null) return clone;

            for (var i = 0; i < source.runes.Count; i++)
            {
                var e = source.runes[i];
                clone.runes.Add(new RuneSlotEntry
                {
                    instanceGuid = e?.instanceGuid
                });
            }

            return clone;
        }

        static EquippedCacheData CloneCache(EquippedCacheData source)
        {
            var clone = new EquippedCacheData();
            if (source?.entries == null) return clone;

            for (var i = 0; i < source.entries.Count; i++)
            {
                var e = source.entries[i];
                if (e?.stack == null) continue;

                clone.entries.Add(new EquippedCacheEntry
                {
                    instanceGuid = e.instanceGuid,
                    stack = new ItemStack
                    {
                        definitionId = e.stack.definitionId,
                        count = e.stack.count,
                        instanceGuid = e.stack.instanceGuid
                    }
                });
            }

            return clone;
        }

        void EnsureSlotCount()
        {
            runeSlotCount = Mathf.Max(1, runeSlotCount);

            while (data.runes.Count < runeSlotCount)
                data.runes.Add(new RuneSlotEntry());

            while (data.runes.Count > runeSlotCount)
                data.runes.RemoveAt(data.runes.Count - 1);
        }

        void PutInCache(ItemStack stack)
        {
            if (stack == null || string.IsNullOrEmpty(stack.instanceGuid))
                return;

            for (var i = 0; i < equippedCache.entries.Count; i++)
            {
                if (equippedCache.entries[i].instanceGuid == stack.instanceGuid)
                {
                    equippedCache.entries[i].stack = stack;
                    return;
                }
            }

            equippedCache.entries.Add(new EquippedCacheEntry
            {
                instanceGuid = stack.instanceGuid,
                stack = stack
            });
        }

        bool TryTakeFromCache(string guid, out ItemStack stack)
        {
            stack = null;
            for (var i = 0; i < equippedCache.entries.Count; i++)
            {
                var entry = equippedCache.entries[i];
                if (entry == null || entry.instanceGuid != guid)
                    continue;

                stack = entry.stack;
                equippedCache.entries.RemoveAt(i);
                return stack != null;
            }

            return false;
        }

        ItemStack FindInCache(string guid)
        {
            for (var i = 0; i < equippedCache.entries.Count; i++)
            {
                var entry = equippedCache.entries[i];
                if (entry != null && entry.instanceGuid == guid)
                    return entry.stack;
            }

            return null;
        }

        int FindBagIndexByGuid(string guid)
        {
            if (inventory == null || string.IsNullOrEmpty(guid))
                return -1;

            var items = inventory.Items;
            for (var i = 0; i < items.Count; i++)
            {
                var s = items[i];
                if (s != null && s.instanceGuid == guid)
                    return i;
            }

            return -1;
        }

        [Conditional("DEBUG_STORE_INVENTORY")]
        static void LogEquip(string msg)
        {
            StoreInventoryLog.Info($"[EquipmentService] {msg}");
        }

        [Conditional("DEBUG_STORE_INVENTORY")]
        void LogEquippedStatMods(Equipment equip)
        {
            if (equip == null)
            {
                StoreInventoryLog.Info("[EquipmentService] equipped item has no statMods (null Equipment def)");
                return;
            }

            var count = equip.statMods?.Count ?? 0;
            StoreInventoryLog.Info($"[EquipmentService] equipped {equip.id} statMods on item={count}");
            if (equip.statMods == null) return;

            for (var i = 0; i < equip.statMods.Count; i++)
            {
                var m = equip.statMods[i];
                StoreInventoryLog.Info(
                    $"[EquipmentService]   mod[{i}] {m.stat} flat={m.flat} pct={m.percent}");
            }

            LogAggregateStatMods("after equip");
        }

        [Conditional("DEBUG_STORE_INVENTORY")]
        void LogAggregateStatMods(string reason)
        {
            var all = GetAllStatMods();
            StoreInventoryLog.Info($"[EquipmentService] {reason} aggregate statMods={all.Count}");
            for (var i = 0; i < all.Count; i++)
            {
                var m = all[i];
                StoreInventoryLog.Info($"[EquipmentService]   agg[{i}] {m.stat} flat={m.flat} pct={m.percent}");
            }
        }
    }
}
