using System;
using UnityEngine;

namespace StoreAndInventory
{
    /// <summary>
    /// 货币（Ink）读写；供 ShopService 扣款/退款。
    /// 调用方：ShopService、StoreSaveService、UI 显示。
    /// 禁止：UI 直接改 wallet.entries。
    /// </summary>
    public class WalletService : MonoBehaviour
    {
        public const int AmountMax = int.MaxValue;

        [SerializeField] CurrencyWallet wallet = new();

        public event Action<CurrencyId, int> OnChanged;

        public CurrencyWallet Data => wallet;

        public void LoadFromData(CurrencyWallet source)
        {
            wallet.entries.Clear();
            if (source?.entries == null)
            {
                EnsureDefaultEntries();
                return;
            }

            for (var i = 0; i < source.entries.Count; i++)
            {
                var e = source.entries[i];
                if (e == null) continue;
                wallet.entries.Add(new CurrencyEntry
                {
                    id = e.id,
                    amount = e.amount
                });
            }

            EnsureDefaultEntries();
        }

        void Awake()
        {
            EnsureDefaultEntries();
        }

        public int Get(CurrencyId id)
        {
            var entry = FindEntry(id);
            return entry?.amount ?? 0;
        }

        public bool Has(CurrencyId id, int amount)
        {
            if (amount <= 0) return true;
            return Get(id) >= amount;
        }

        public void Add(CurrencyId id, int amount, string reason = null)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[Wallet] Add skipped: {id} amount={amount}");
                return;
            }

            var entry = FindOrCreateEntry(id);
            var before = entry.amount;

            if (entry.amount >= AmountMax)
            {
                Debug.LogWarning($"[Wallet] Add overflow: {id} already at max ({AmountMax}), discarded +{amount}");
                return;
            }

            var next = (long)entry.amount + amount;
            if (next > AmountMax)
            {
                var overflow = next - AmountMax;
                entry.amount = AmountMax;
                Debug.LogWarning($"[Wallet] Add overflow: {id} +{amount} capped at {AmountMax}, discarded {overflow}");
            }
            else
            {
                entry.amount = (int)next;
            }

            var reasonPart = string.IsNullOrEmpty(reason) ? string.Empty : $" ({reason})";
            StoreInventoryLog.Info($"[Wallet] Add {id} +{amount}{reasonPart} → {entry.amount} (was {before})");
            OnChanged?.Invoke(id, entry.amount);
        }

        public bool TrySpend(CurrencyId id, int amount, string reason = null)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[Wallet] TrySpend skipped: {id} amount={amount}");
                return false;
            }

            var current = Get(id);
            if (current < amount)
            {
                StoreInventoryLog.Info($"[Wallet] TrySpend failed: {id} 不足（拥有 {current}，需要 {amount}）");
                return false;
            }

            var entry = FindOrCreateEntry(id);
            entry.amount -= amount;

            var reasonPart = string.IsNullOrEmpty(reason) ? string.Empty : $" ({reason})";
            StoreInventoryLog.Info($"[Wallet] Spend {id} -{amount}{reasonPart} → {entry.amount}");
            OnChanged?.Invoke(id, entry.amount);
            return true;
        }

        void EnsureDefaultEntries()
        {
            if (FindEntry(CurrencyId.Ink) == null)
            {
                wallet.entries.Add(new CurrencyEntry
                {
                    id = CurrencyId.Ink,
                    amount = 0
                });
            }
        }

        CurrencyEntry FindEntry(CurrencyId id)
        {
            for (var i = 0; i < wallet.entries.Count; i++)
            {
                if (wallet.entries[i] != null && wallet.entries[i].id == id)
                    return wallet.entries[i];
            }
            return null;
        }

        CurrencyEntry FindOrCreateEntry(CurrencyId id)
        {
            var entry = FindEntry(id);
            if (entry != null) return entry;

            entry = new CurrencyEntry { id = id, amount = 0 };
            wallet.entries.Add(entry);
            return entry;
        }
    }
}
