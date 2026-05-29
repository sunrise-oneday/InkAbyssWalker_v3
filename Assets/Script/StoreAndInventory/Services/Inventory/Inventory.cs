using System;
using System.Collections.Generic;
using UnityEngine;

namespace StoreAndInventory
{
    /// <summary>
    /// 背包运行时：增删查、堆叠、消耗品使用与对外查询。
    /// 调用方：ShopService、EquipmentService、UI（InventoryUI）。
    /// 禁止：UI 直接改 items 列表；Data 层不得引用 UGUI。
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [Header("依赖（场景拖入）")]
        [SerializeField] ItemDatabase database;

        [Header("运行时格子（无限背包，无格数上限）")]
        [SerializeField] List<ItemStack> items = new();

        public event Action OnChanged;
        public int Count => items.Count;
        public IReadOnlyList<ItemStack> Items => items;

        readonly List<ConsumableQueryEntry> consumableQueryCache = new();

        public InventoryData Data
        {
            get
            {
                return new InventoryData
                {
                    capacity = 0,
                    slots = CloneStacks(items)
                };
            }
        }

        public void LoadFromData(InventoryData data)
        {
            items.Clear();
            if (data?.slots != null)
            {
                for (var i = 0; i < data.slots.Count; i++)
                {
                    var s = data.slots[i];
                    if (s == null || s.IsEmpty) continue;
                    items.Add(new ItemStack
                    {
                        definitionId = s.definitionId,
                        count = s.count,
                        instanceGuid = s.instanceGuid
                    });
                }
            }

            OnChanged?.Invoke();
        }

        public IReadOnlyList<ConsumableQueryEntry> GetConsumables(UseContext? contextFilter = null)
        {
            consumableQueryCache.Clear();

            for (var i = 0; i < items.Count; i++)
            {
                var stack = items[i];
                if (stack == null || stack.IsEmpty) continue;

                if (!TryGetDefinition(stack.definitionId, out var def))
                    continue;

                if (def is not Consumable consumable)
                    continue;

                if (contextFilter.HasValue && !MatchesUseContext(consumable.useContext, contextFilter.Value))
                    continue;

                consumableQueryCache.Add(new ConsumableQueryEntry
                {
                    bagIndex = i,
                    stack = stack,
                    definition = consumable
                });
            }

            return consumableQueryCache;
        }

        public bool TryConsumeAt(int bagIndex, int count, UseContext context, out List<GameplayEffectSO> effects, out string message)
        {
            effects = null;
            message = null;

            if (bagIndex < 0 || bagIndex >= items.Count)
            {
                message = $"invalid index {bagIndex}";
                return false;
            }

            var stack = items[bagIndex];
            if (stack == null || stack.IsEmpty)
            {
                message = "empty slot";
                return false;
            }

            if (!TryGetDefinition(stack.definitionId, out var def) || def is not Consumable consumable)
            {
                message = "not consumable";
                return false;
            }

            if (!MatchesUseContext(consumable.useContext, context))
            {
                message = $"wrong context (need {consumable.useContext}, got {context})";
                return false;
            }

            if (count <= 0 || count > stack.count)
            {
                message = $"invalid count {count}";
                return false;
            }

            if (!TryTakePartialAt(bagIndex, count, out _, out message))
                return false;

            effects = new List<GameplayEffectSO>();
            if (consumable.useEffects != null)
            {
                for (var i = 0; i < consumable.useEffects.Count; i++)
                {
                    var fx = consumable.useEffects[i];
                    if (fx != null)
                        effects.Add(fx);
                }
            }

            return true;
        }

        static bool MatchesUseContext(UseContext itemContext, UseContext requested)
        {
            if (itemContext == UseContext.Any || requested == UseContext.Any)
                return true;

            return itemContext == requested;
        }

        static List<ItemStack> CloneStacks(List<ItemStack> source)
        {
            var list = new List<ItemStack>(source?.Count ?? 0);
            if (source == null) return list;

            for (var i = 0; i < source.Count; i++)
            {
                var s = source[i];
                if (s == null) continue;
                list.Add(new ItemStack
                {
                    definitionId = s.definitionId,
                    count = s.count,
                    instanceGuid = s.instanceGuid
                });
            }

            return list;
        }

        public bool TryAdd(ItemBase item, int count, out string message)
        {
            message = null;

            if (item == null)
            {
                message = "item 为空";
                return false;
            }

            if (count <= 0)
            {
                message = $"count={count}（必须 > 0）";
                return false;
            }

            var maxStack = item.MaxStack;
            var remaining = count;
            var mergedAny = false;

            if (maxStack > 1)
            {
                for (var i = 0; i < items.Count && remaining > 0; i++)
                {
                    var s = items[i];
                    if (s == null || s.definitionId != item.id) continue;

                    var space = maxStack - s.count;
                    if (space <= 0) continue;

                    var move = Mathf.Min(space, remaining);
                    s.count += move;
                    remaining -= move;
                    mergedAny = true;
                }
            }

            while (remaining > 0)
            {
                var grow = Mathf.Min(maxStack, remaining);
                items.Add(new ItemStack
                {
                    definitionId = item.id,
                    count = grow,
                    instanceGuid = NeedsGuid(item) ? NewGuid8() : null
                });
                remaining -= grow;
            }

            OnChanged?.Invoke();
            StoreInventoryLog.Info($"[Inventory] TryAdd ok: {item.id} +{count}, slots now={items.Count}");
            return true;
        }

        public bool TryConsume(string definitionId, int count, out string message)
        {
            message = null;

            if (string.IsNullOrEmpty(definitionId))
            {
                message = "definitionId 为空";
                return false;
            }

            if (count <= 0)
            {
                message = $"count={count}（必须 > 0）";
                return false;
            }

            var total = 0;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].definitionId == definitionId)
                    total += items[i].count;
            }

            if (total < count)
            {
                message = $"不足：拥有 {total}，需要 {count}";
                StoreInventoryLog.Info($"[Inventory] TryConsume failed: {message}");
                return false;
            }

            var remaining = count;
            for (var i = 0; i < items.Count && remaining > 0; i++)
            {
                var s = items[i];
                if (s == null || s.definitionId != definitionId) continue;

                var take = Mathf.Min(s.count, remaining);
                s.count -= take;
                remaining -= take;
            }

            for (var i = items.Count - 1; i >= 0; i--)
            {
                if (items[i] == null || items[i].IsEmpty)
                    items.RemoveAt(i);
            }

            OnChanged?.Invoke();
            StoreInventoryLog.Info($"[Inventory] TryConsume ok: {definitionId} -{count}, slots now={items.Count}");
            return true;
        }

        public bool TryTakeAt(int index, out ItemStack taken, out string message)
        {
            taken = null;
            message = null;

            if (index < 0 || index >= items.Count)
            {
                message = $"invalid index {index}";
                return false;
            }

            taken = items[index];
            if (taken == null || taken.IsEmpty)
            {
                message = "empty slot";
                taken = null;
                return false;
            }

            items.RemoveAt(index);
            OnChanged?.Invoke();
            StoreInventoryLog.Info($"[Inventory] TryTakeAt [{index}]: {Format(taken)}");
            return true;
        }

        public bool TryTakePartialAt(int index, int count, out ItemStack taken, out string message)
        {
            taken = null;
            message = null;

            if (index < 0 || index >= items.Count)
            {
                message = $"invalid index {index}";
                return false;
            }

            if (count <= 0)
            {
                message = $"count={count}（必须 > 0）";
                return false;
            }

            var s = items[index];
            if (s == null || s.IsEmpty)
            {
                message = "empty slot";
                return false;
            }

            if (count > s.count)
            {
                message = $"not enough (have {s.count}, need {count})";
                return false;
            }

            if (count == s.count)
            {
                taken = s;
                items.RemoveAt(index);
            }
            else
            {
                taken = new ItemStack
                {
                    definitionId = s.definitionId,
                    count = count,
                    instanceGuid = s.instanceGuid
                };
                s.count -= count;
            }

            OnChanged?.Invoke();
            StoreInventoryLog.Info($"[Inventory] TryTakePartialAt [{index}] -{count}: {Format(taken)}");
            return true;
        }

        public bool TryAddStack(ItemStack stack, out string message)
        {
            message = null;

            if (stack == null || stack.IsEmpty)
            {
                message = "stack empty";
                return false;
            }

            if (!TryGetDefinition(stack.definitionId, out var def) || def == null)
            {
                message = $"unknown item {stack.definitionId}";
                return false;
            }

            if (def.Category == ItemCategory.Equipment)
            {
                if (string.IsNullOrEmpty(stack.instanceGuid))
                {
                    message = "equipment missing guid";
                    return false;
                }

                items.Add(new ItemStack
                {
                    definitionId = stack.definitionId,
                    count = 1,
                    instanceGuid = stack.instanceGuid
                });
                OnChanged?.Invoke();
                StoreInventoryLog.Info($"[Inventory] TryAddStack equip: {Format(stack)}");
                return true;
            }

            return TryAdd(def, stack.count, out message);
        }

        public bool TryGetDefinition(string definitionId, out ItemBase definition)
        {
            definition = null;
            if (database == null) return false;
            return database.TryGet(definitionId, out definition);
        }

        string Format(ItemStack stack)
        {
            if (stack == null || stack.IsEmpty)
                return "(empty)";

            var guidPart = string.IsNullOrEmpty(stack.instanceGuid) ? string.Empty : $", guid={stack.instanceGuid}";

            if (database != null && database.TryGet(stack.definitionId, out var def) && def != null)
                return $"{def.id} | {def.Name} | basePrice={def.basePrice} (count={stack.count}{guidPart})";

            return $"{stack.definitionId} | <db missing> (count={stack.count}{guidPart})";
        }

        static bool NeedsGuid(ItemBase item)
        {
            return item != null && item.Category == ItemCategory.Equipment;
        }

        static string NewGuid8()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }
    }
}
