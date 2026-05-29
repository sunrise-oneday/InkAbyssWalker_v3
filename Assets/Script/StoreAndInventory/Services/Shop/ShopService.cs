using System;
using System.Collections.Generic;
using UnityEngine;

namespace StoreAndInventory
{
    /// <summary>
    /// 商店营业、购买、卖出、限购归档与货架查询。
    /// 调用方：ShopUI、地图逻辑（Open/Close）、StoreSaveService。
    /// 禁止：UI 绕过 TryBuy/TrySell 直接改背包或货币。
    /// </summary>
    public class ShopService : MonoBehaviour
    {
        [SerializeField] WalletService wallet;
        [SerializeField] Inventory inventory;
        [SerializeField] ShopTableSO defaultTable;

        ShopTableSO currentTable;
        ShopRuntimeData runtime = new();
        readonly List<ShopRuntimeData> runtimeArchive = new();
        Dictionary<string, ShopEntry> entryById = new();
        Dictionary<string, StockEntry> stockById = new();

        public ShopTableSO CurrentShop => currentTable;
        public ShopRuntimeData CurrentRuntime => runtime;
        public ShopTableSO DefaultTable => defaultTable;
        public bool IsOpen => currentTable != null;

        public event Action<ShopTableSO> OnShopOpened;
        public event Action OnShopClosed;
        public event Action OnShopChanged;

        public void Open(ShopTableSO table = null)
        {
            var target = table != null ? table : defaultTable;
            if (target == null)
            {
                Debug.LogWarning("[ShopService] Open failed: no ShopTableSO assigned.");
                return;
            }

            currentTable = target;
            runtime = new ShopRuntimeData { shopId = target.shopId };
            entryById.Clear();
            stockById.Clear();

            for (var i = 0; i < target.fixedStock.Count; i++)
            {
                var entry = target.fixedStock[i];
                if (entry?.item == null || string.IsNullOrEmpty(entry.item.id)) continue;

                entryById[entry.item.id] = entry;

                if (entry.item.Category != ItemCategory.Equipment || entry.stock <= 0)
                    continue;

                var stockEntry = new StockEntry
                {
                    itemId = entry.item.id,
                    remaining = entry.stock
                };
                runtime.stockOverrides.Add(stockEntry);
                stockById[entry.item.id] = stockEntry;
            }

            ApplyArchivedRuntime(target.shopId);

            StoreInventoryLog.Info($"[ShopService] Open: {target.shopId} {target.displayName}");
            OnShopOpened?.Invoke(target);
        }

        public void Close()
        {
            if (currentTable == null) return;

            ArchiveCurrentRuntime();

            currentTable = null;
            runtime = new ShopRuntimeData();
            entryById.Clear();
            stockById.Clear();
            StoreInventoryLog.Info("[ShopService] Close");
            OnShopClosed?.Invoke();
        }

        public BuyResult TryBuy(string itemId, int count = 1)
        {
            if (!IsOpen)
            {
                StoreInventoryLog.Info("[ShopService] TryBuy → ShopClosed");
                return BuyResult.ShopClosed;
            }

            if (count <= 0 || string.IsNullOrEmpty(itemId))
            {
                StoreInventoryLog.Info($"[ShopService] TryBuy → InvalidItem (itemId={itemId}, count={count})");
                return BuyResult.InvalidItem;
            }

            if (!entryById.TryGetValue(itemId, out var entry) || entry?.item == null)
            {
                StoreInventoryLog.Info($"[ShopService] TryBuy → InvalidItem (not in fixedStock: {itemId})");
                return BuyResult.InvalidItem;
            }

            var item = entry.item;
            var totalPrice = GetPrice(entry) * count;

            if (!HasStockByCategory(item, entry, count))
            {
                StoreInventoryLog.Info($"[ShopService] TryBuy → OutOfStock ({itemId} x{count})");
                return BuyResult.OutOfStock;
            }

            if (wallet == null || !wallet.Has(CurrencyId.Ink, totalPrice))
            {
                StoreInventoryLog.Info($"[ShopService] TryBuy → NoCurrency ({itemId}, need Ink={totalPrice})");
                return BuyResult.NoCurrency;
            }

            if (!wallet.TrySpend(CurrencyId.Ink, totalPrice, $"buy {itemId} x{count}"))
                return BuyResult.NoCurrency;

            if (inventory == null)
            {
                wallet.Add(CurrencyId.Ink, totalPrice, $"rollback buy {itemId} x{count}");
                Debug.LogWarning($"[ShopService] TryBuy → NoSpace (rollback Ink={totalPrice}): inventory is null");
                return BuyResult.NoSpace;
            }

            if (!inventory.TryAdd(item, count, out var addMsg))
            {
                wallet.Add(CurrencyId.Ink, totalPrice, $"rollback buy {itemId} x{count}");
                Debug.LogWarning($"[ShopService] TryBuy → NoSpace (rollback Ink={totalPrice}): {addMsg}");
                return BuyResult.NoSpace;
            }

            ConsumeStockIfEquipment(item, entry, count);
            StoreInventoryLog.Info($"[ShopService] TryBuy → Success ({itemId} x{count}, Ink -{totalPrice})");
            OnShopChanged?.Invoke();
            return BuyResult.Success;
        }

        public SellResult TrySell(int bagSlotIndex, int count, out string message)
        {
            message = null;

            if (inventory == null)
            {
                message = "inventory missing";
                return SellResult.InvalidSlot;
            }

            if (bagSlotIndex < 0 || bagSlotIndex >= inventory.Count)
            {
                message = $"invalid bag index {bagSlotIndex}";
                return SellResult.InvalidSlot;
            }

            if (count <= 0)
            {
                message = $"count={count}";
                return SellResult.InvalidCount;
            }

            var preview = inventory.Items[bagSlotIndex];
            if (preview == null || preview.IsEmpty)
            {
                message = "empty slot";
                return SellResult.InvalidSlot;
            }

            if (!inventory.TryGetDefinition(preview.definitionId, out var def) || def == null)
            {
                message = $"unknown item {preview.definitionId}";
                return SellResult.InvalidItem;
            }

            if (!def.canSell)
            {
                message = "not sellable";
                StoreInventoryLog.Info($"[ShopService] TrySell → NotSellable ({def.id})");
                return SellResult.NotSellable;
            }

            if (count > preview.count)
            {
                message = $"not enough (have {preview.count})";
                return SellResult.InvalidCount;
            }

            if (!inventory.TryTakePartialAt(bagSlotIndex, count, out var taken, out message))
                return SellResult.InvalidCount;

            var totalPrice = def.basePrice * count;

            if (wallet != null)
                wallet.Add(CurrencyId.Ink, totalPrice, $"sell {def.id} x{count}");

            StoreInventoryLog.Info($"[ShopService] TrySell → Success ({def.id} x{count}, Ink +{totalPrice})");
            OnShopChanged?.Invoke();
            return SellResult.Success;
        }

        public IReadOnlyList<ShopEntry> GetVisibleEntries()
        {
            if (currentTable == null)
                return Array.Empty<ShopEntry>();
            return currentTable.fixedStock;
        }

        public int GetPrice(ShopEntry entry)
        {
            if (entry?.item == null || currentTable == null)
                return 0;

            var baseUnit = entry.priceOverride > 0 ? entry.priceOverride : entry.item.basePrice;
            return Mathf.RoundToInt(baseUnit * Mathf.Max(0f, currentTable.priceMultiplier));
        }

        public int GetRemainingStock(string itemId)
        {
            if (!IsOpen || string.IsNullOrEmpty(itemId))
                return 0;

            if (!entryById.TryGetValue(itemId, out var entry) || entry?.item == null)
                return 0;

            return GetRemainingStockForEntry(entry);
        }

        public void Refresh()
        {
            // 预留：随机货架刷新
        }

        int GetRemainingStockForEntry(ShopEntry entry)
        {
            var item = entry.item;

            if (item.Category == ItemCategory.Consumable || item.Category == ItemCategory.StoryItem)
                return -1;

            if (item.Category != ItemCategory.Equipment)
                return -1;

            if (entry.stock == -1)
                return -1;

            if (entry.stock == 0)
                return 0;

            if (stockById.TryGetValue(item.id, out var stockEntry))
                return stockEntry.remaining;

            return entry.stock;
        }

        bool HasStockByCategory(ItemBase item, ShopEntry entry, int count)
        {
            if (item.Category == ItemCategory.Consumable || item.Category == ItemCategory.StoryItem)
                return true;

            if (item.Category != ItemCategory.Equipment)
                return true;

            if (entry.stock == -1)
                return true;

            if (entry.stock == 0)
                return false;

            return GetRemainingStockForEntry(entry) >= count;
        }

        void ConsumeStockIfEquipment(ItemBase item, ShopEntry entry, int count)
        {
            if (item.Category != ItemCategory.Equipment)
                return;

            if (entry.stock <= 0)
                return;

            if (!stockById.TryGetValue(item.id, out var stockEntry))
                return;

            stockEntry.remaining = Mathf.Max(0, stockEntry.remaining - count);
        }

        public void ArchiveCurrentRuntime()
        {
            if (string.IsNullOrEmpty(runtime.shopId))
                return;

            UpsertArchivedRuntime(CloneRuntime(runtime));
        }

        public ShopRuntimeArchive ExportRuntimeArchive()
        {
            ArchiveCurrentRuntime();
            var archive = new ShopRuntimeArchive();
            for (var i = 0; i < runtimeArchive.Count; i++)
                archive.shops.Add(CloneRuntime(runtimeArchive[i]));
            return archive;
        }

        public void ImportRuntimeArchive(ShopRuntimeArchive archive)
        {
            runtimeArchive.Clear();
            if (archive?.shops == null) return;

            for (var i = 0; i < archive.shops.Count; i++)
            {
                var shop = archive.shops[i];
                if (shop == null || string.IsNullOrEmpty(shop.shopId)) continue;
                runtimeArchive.Add(CloneRuntime(shop));
            }
        }

        void ApplyArchivedRuntime(string shopId)
        {
            var archived = FindArchivedRuntime(shopId);
            if (archived == null) return;

            runtime.lastRefreshTick = archived.lastRefreshTick;
            runtime.randomSnapshot = CloneStringList(archived.randomSnapshot);

            for (var i = 0; i < archived.stockOverrides.Count; i++)
            {
                var saved = archived.stockOverrides[i];
                if (saved == null || string.IsNullOrEmpty(saved.itemId)) continue;

                if (!stockById.TryGetValue(saved.itemId, out var live))
                    continue;

                live.remaining = saved.remaining;
            }
        }

        ShopRuntimeData FindArchivedRuntime(string shopId)
        {
            for (var i = 0; i < runtimeArchive.Count; i++)
            {
                var r = runtimeArchive[i];
                if (r != null && r.shopId == shopId)
                    return r;
            }

            return null;
        }

        void UpsertArchivedRuntime(ShopRuntimeData snapshot)
        {
            for (var i = 0; i < runtimeArchive.Count; i++)
            {
                if (runtimeArchive[i]?.shopId != snapshot.shopId) continue;
                runtimeArchive[i] = snapshot;
                return;
            }

            runtimeArchive.Add(snapshot);
        }

        static ShopRuntimeData CloneRuntime(ShopRuntimeData source)
        {
            if (source == null) return new ShopRuntimeData();

            var clone = new ShopRuntimeData
            {
                shopId = source.shopId,
                lastRefreshTick = source.lastRefreshTick,
                randomSnapshot = CloneStringList(source.randomSnapshot)
            };

            for (var i = 0; i < source.stockOverrides.Count; i++)
            {
                var s = source.stockOverrides[i];
                if (s == null) continue;
                clone.stockOverrides.Add(new StockEntry
                {
                    itemId = s.itemId,
                    remaining = s.remaining
                });
            }

            return clone;
        }

        static List<string> CloneStringList(List<string> source)
        {
            var list = new List<string>();
            if (source == null) return list;
            for (var i = 0; i < source.Count; i++)
                list.Add(source[i]);
            return list;
        }
    }
}
