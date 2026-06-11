using System;
using System.Collections.Generic;
using UnityEngine;

namespace StoreAndInventory
{
    /// <summary>
    /// 技能专属符文槽的装/卸与效果聚合查询。
    /// 每个槽位绑定一个技能或大招，符文只能装备到匹配的槽位。
    /// statMods 供 UI/主工程读；skillMods、extraEffects 仅聚合，本系统不执行。
    /// </summary>
    public class EquipmentService : MonoBehaviour
    {
        [SerializeField] Inventory inventory;
        [SerializeField] ItemDatabase database;

        readonly EquipmentData data = new();
        readonly EquippedCacheData equippedCache = new();

        public int RuneSlotCount => SkillRuneSlotConfig.AllSlotIds.Length;
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
            if (inventory == null) inventory = FindObjectOfType<Inventory>();
            if (database == null) database = FindObjectOfType<ItemDatabase>();
            EnsureSlotCount();
        }

        /// <summary>获取指定槽位的技能 ID。</summary>
        public string GetSlotId(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SkillRuneSlotConfig.AllSlotIds.Length)
                return null;
            return SkillRuneSlotConfig.AllSlotIds[slotIndex];
        }

        /// <summary>获取技能 ID 对应的槽位索引，未找到返回 -1。</summary>
        public int GetSlotIndexBySkillId(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return -1;
            var ids = SkillRuneSlotConfig.AllSlotIds;
            for (var i = 0; i < ids.Length; i++)
            {
                if (ids[i] == skillId) return i;
            }
            return -1;
        }

        /// <summary>检查符文是否可以装备到普通技能槽位。</summary>
        public bool IsCompatible(int slotIndex, Equipment equip)
        {
            if (equip == null) return false;

            var slotId = GetSlotId(slotIndex);
            if (string.IsNullOrEmpty(slotId)) return false;

            // 大招专属符文（有 ultimateDamageBonusPercent）不能装到普通槽
            if (equip.extraEffects != null)
            {
                for (var i = 0; i < equip.extraEffects.Count; i++)
                {
                    var fx = equip.extraEffects[i];
                    if (fx != null && fx.ultimateDamageBonusPercent > 0)
                    {
                        Debug.Log($"[IsCompatible] REJECTED: {equip.Name} has ultimateDamageBonusPercent");
                        return false;
                    }
                }
            }

            // 检查技能修改器的目标是否匹配槽位
            if (equip.skillMods != null && equip.skillMods.Count > 0)
            {
                for (var i = 0; i < equip.skillMods.Count; i++)
                {
                    var mod = equip.skillMods[i];
                    Debug.Log($"[IsCompatible] {equip.Name} mod[{i}]: targetKind={mod.targetKind}, targetId='{mod.targetId}' vs slotId='{slotId}'");
                    if (mod.targetKind == SkillModTarget.SkillId && mod.targetId == slotId)
                        return true;
                }
                // 有技能修改器但都不匹配 → 不兼容
                Debug.Log($"[IsCompatible] REJECTED: {equip.Name} skillMods none match slot '{slotId}'");
                return false;
            }

            // 纯属性符文（无 skillMods、无大招专属效果）→ 任意普通槽可装
            return true;
        }

        /// <summary>检查符文是否可以装备到大招槽。</summary>
        public bool IsUltimateCompatible(int slotIndex, Equipment equip)
        {
            if (equip == null) return false;
            var slotId = GetSlotId(slotIndex);
            if (slotId != SkillRuneSlotConfig.UltimateSlotId) return false;

            // 有大招专属效果 → 可装
            if (equip.extraEffects != null)
            {
                for (var i = 0; i < equip.extraEffects.Count; i++)
                {
                    var fx = equip.extraEffects[i];
                    if (fx != null && fx.ultimateDamageBonusPercent > 0) return true;
                }
            }

            // 有技能修改器且目标是具体技能 → 不可装到大招槽
            if (equip.skillMods != null && equip.skillMods.Count > 0)
            {
                for (var i = 0; i < equip.skillMods.Count; i++)
                {
                    if (equip.skillMods[i].targetKind == SkillModTarget.SkillId)
                        return false;
                }
            }

            // 纯属性符文 → 可装
            return true;
        }

        public bool TryEquipFromBag(int bagSlotIndex, int runeSlotIndex, out string message)
        {
            message = null;

            if (inventory == null) { message = "inventory missing"; return false; }
            if (runeSlotIndex < 0 || runeSlotIndex >= RuneSlotCount)
            { message = $"invalid rune slot {runeSlotIndex}"; return false; }
            if (bagSlotIndex < 0 || bagSlotIndex >= inventory.Count)
            { message = $"invalid bag index {bagSlotIndex}"; return false; }

            var stack = inventory.Items[bagSlotIndex];
            if (stack == null || stack.IsEmpty) { message = "empty bag slot"; return false; }

            if (!inventory.TryGetDefinition(stack.definitionId, out var def))
            { message = $"unknown item {stack.definitionId}"; return false; }

            if (def.Category != ItemCategory.Equipment) { message = "not equipment"; return false; }
            if (stack.count != 1) { message = "equipment count must be 1"; return false; }
            if (string.IsNullOrEmpty(stack.instanceGuid)) { message = "equipment missing instance guid"; return false; }

            var equip = def as Equipment;
            if (equip == null) { message = "not an Equipment definition"; return false; }

            // 兼容性检查
            var slotId = GetSlotId(runeSlotIndex);
            if (slotId == SkillRuneSlotConfig.UltimateSlotId)
            {
                if (!IsUltimateCompatible(runeSlotIndex, equip))
                { message = "this rune cannot be equipped in the ultimate slot"; return false; }
            }
            else
            {
                if (!IsCompatible(runeSlotIndex, equip))
                { message = $"this rune targets a different skill, not '{slotId}'"; return false; }
            }

            EnsureSlotCount();

            // 如果目标槽已有装备，先卸下
            var existingGuid = data.runes[runeSlotIndex].instanceGuid;
            if (!string.IsNullOrEmpty(existingGuid))
            {
                if (!TryUnequip(runeSlotIndex, out message)) return false;
            }

            var targetGuid = stack.instanceGuid;
            var takeIndex = FindBagIndexByGuid(targetGuid);
            if (takeIndex < 0) { message = "item no longer in bag"; return false; }

            if (!inventory.TryTakeAt(takeIndex, out var taken, out message)) return false;

            PutInCache(taken);
            data.runes[runeSlotIndex].instanceGuid = taken.instanceGuid;

            InvalidateModCache();
            OnEquipped?.Invoke(runeSlotIndex, taken);
            return true;
        }

        public bool TryUnequip(int runeSlotIndex, out string message)
        {
            message = null;
            if (inventory == null) { message = "inventory missing"; return false; }
            if (runeSlotIndex < 0 || runeSlotIndex >= RuneSlotCount)
            { message = $"invalid rune slot {runeSlotIndex}"; return false; }

            EnsureSlotCount();

            var guid = data.runes[runeSlotIndex].instanceGuid;
            if (string.IsNullOrEmpty(guid)) { message = "slot empty"; return false; }

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
            InvalidateModCache();
            OnUnequipped?.Invoke(runeSlotIndex, stack);
            return true;
        }

        public ItemStack GetEquippedStack(int runeSlotIndex)
        {
            if (runeSlotIndex < 0 || runeSlotIndex >= RuneSlotCount) return null;
            EnsureSlotCount();

            var guid = data.runes[runeSlotIndex].instanceGuid;
            if (string.IsNullOrEmpty(guid)) return null;
            return FindInCache(guid);
        }

        public Equipment GetEquippedItem(int runeSlotIndex)
        {
            var stack = GetEquippedStack(runeSlotIndex);
            if (stack == null || database == null) return null;
            return database.TryGet(stack.definitionId, out var def) ? def as Equipment : null;
        }

        public IReadOnlyList<StatModifier> GetAllStatMods() { EnsureModCache(); return statModCache; }
        public IReadOnlyList<SkillModifier> GetAllSkillMods() { EnsureModCache(); return skillModCache; }
        public IReadOnlyList<GameplayEffectSO> GetAllExtraEffects() { EnsureModCache(); return extraEffectCache; }

        public EquipmentSaveData CaptureSaveData()
        {
            EnsureSlotCount();
            return new EquipmentSaveData
            {
                runeSlotCount = RuneSlotCount,
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
            }
            else
            {
                data.runes = save.equipment?.runes ?? new List<RuneSlotEntry>();
                equippedCache.entries = save.cache?.entries ?? new List<EquippedCacheEntry>();
            }

            EnsureSlotCount();
            InvalidateModCache();
        }

        // ── 内部方法 ──

        void EnsureModCache()
        {
            if (!modCacheDirty) return;

            statModCache.Clear();
            skillModCache.Clear();
            extraEffectCache.Clear();

            for (var slot = 0; slot < RuneSlotCount; slot++)
            {
                var equip = GetEquippedItem(slot);
                if (equip == null) continue;

                AppendModifiers(equip.statMods, statModCache);
                AppendModifiers(equip.skillMods, skillModCache);
                AppendEffects(equip.extraEffects, extraEffectCache);
            }

            modCacheDirty = false;
        }

        void InvalidateModCache() => modCacheDirty = true;

        static void AppendModifiers(List<StatModifier> source, List<StatModifier> target)
        {
            if (source == null) return;
            for (var i = 0; i < source.Count; i++) target.Add(source[i]);
        }

        static void AppendModifiers(List<SkillModifier> source, List<SkillModifier> target)
        {
            if (source == null) return;
            for (var i = 0; i < source.Count; i++) target.Add(source[i]);
        }

        static void AppendEffects(List<GameplayEffectSO> source, List<GameplayEffectSO> target)
        {
            if (source == null) return;
            for (var i = 0; i < source.Count; i++)
            {
                var fx = source[i];
                if (fx != null) target.Add(fx);
            }
        }

        void EnsureSlotCount()
        {
            var expected = SkillRuneSlotConfig.AllSlotIds.Length;
            while (data.runes.Count < expected)
            {
                var slotId = SkillRuneSlotConfig.AllSlotIds[data.runes.Count];
                data.runes.Add(new RuneSlotEntry { slotId = slotId });
            }

            // 确保每个槽位的 slotId 正确
            for (var i = 0; i < data.runes.Count && i < expected; i++)
            {
                if (string.IsNullOrEmpty(data.runes[i].slotId))
                    data.runes[i].slotId = SkillRuneSlotConfig.AllSlotIds[i];
            }
        }

        void PutInCache(ItemStack stack)
        {
            if (stack == null || string.IsNullOrEmpty(stack.instanceGuid)) return;

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
                if (entry == null || entry.instanceGuid != guid) continue;
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
                if (entry != null && entry.instanceGuid == guid) return entry.stack;
            }
            return null;
        }

        int FindBagIndexByGuid(string guid)
        {
            if (inventory == null || string.IsNullOrEmpty(guid)) return -1;
            var items = inventory.Items;
            for (var i = 0; i < items.Count; i++)
            {
                var s = items[i];
                if (s != null && s.instanceGuid == guid) return i;
            }
            return -1;
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
                    slotId = e?.slotId,
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
    }
}
