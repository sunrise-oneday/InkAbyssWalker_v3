using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StoreAndInventory
{
    public enum ShopPanelMode
    {
        Buy,
        Sell
    }

    public class ShopUI : MonoBehaviour
    {
        [SerializeField] ShopService shopService;
        [SerializeField] WalletService wallet;
        [SerializeField] Inventory inventory;
        [SerializeField] ShopItemUI itemPrefab;
        [SerializeField] SellItemUI sellItemPrefab;
        [SerializeField] Transform content;
        [SerializeField] TextMeshProUGUI inkText;
        [SerializeField] ItemTooltipUI itemInfoPanel;

        [Header("Tabs")]
        [SerializeField] Button buyTabButton;
        [SerializeField] Button sellTabButton;

        [Header("关闭按钮（可选）")]
        [SerializeField] Button closeButton;

        ShopPanelMode mode = ShopPanelMode.Buy;
        ShopEntry selectedEntry;

        void Awake()
        {
            if (shopService == null)
                shopService = FindObjectOfType<ShopService>();
            if (wallet == null)
                wallet = FindObjectOfType<WalletService>();
            if (inventory == null)
                inventory = FindObjectOfType<Inventory>();
            if (itemInfoPanel == null)
                itemInfoPanel = transform.Find("ItemInfoPanel")?.GetComponent<ItemTooltipUI>()
                                  ?? GetComponentInChildren<ItemTooltipUI>(true);
        }

        public bool IsOpen => gameObject.activeSelf;

        void OnEnable()
        {
            if (shopService != null)
            {
                shopService.OnShopOpened += HandleShopOpened;
                shopService.OnShopChanged += HandleShopInventoryChanged;
            }

            if (wallet != null)
                wallet.OnChanged += HandleWalletChanged;

            if (inventory != null)
                inventory.OnChanged += HandleShopInventoryChanged;

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (buyTabButton != null)
                buyTabButton.onClick.AddListener(() => SetMode(ShopPanelMode.Buy));

            if (sellTabButton != null)
                sellTabButton.onClick.AddListener(() => SetMode(ShopPanelMode.Sell));

            EnsureShopOpen();
            RefreshInk();
            Refresh();
        }

        void OnDisable()
        {
            if (shopService != null)
            {
                shopService.OnShopOpened -= HandleShopOpened;
                shopService.OnShopChanged -= HandleShopInventoryChanged;
            }

            if (wallet != null)
                wallet.OnChanged -= HandleWalletChanged;

            if (inventory != null)
                inventory.OnChanged -= HandleShopInventoryChanged;

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);

            if (buyTabButton != null)
                buyTabButton.onClick.RemoveAllListeners();

            if (sellTabButton != null)
                sellTabButton.onClick.RemoveAllListeners();

            itemInfoPanel?.Hide();
            selectedEntry = null;
        }

        void HandleShopOpened(ShopTableSO _) => Refresh();

        void HandleShopInventoryChanged() => Refresh();

        void HandleWalletChanged(CurrencyId _, int __)
        {
            RefreshInk();
            if (selectedEntry != null && itemInfoPanel != null && itemInfoPanel.IsVisible)
                ShowBuyInfoPanel(selectedEntry);
        }

        public void Open()
        {
            EnsureShopOpen();
            gameObject.SetActive(true);
            RefreshInk();
            Refresh();
        }

        public void Close()
        {
            itemInfoPanel?.Hide();
            selectedEntry = null;
            gameObject.SetActive(false);
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        public void SetMode(ShopPanelMode newMode)
        {
            mode = newMode;
            itemInfoPanel?.Hide();
            selectedEntry = null;
            Refresh();
        }

        void EnsureShopOpen()
        {
            if (shopService == null) return;
            if (!shopService.IsOpen)
                shopService.Open();
        }

        public void RefreshInk()
        {
            if (inkText == null || wallet == null) return;
            inkText.text = $"Ink {wallet.Get(CurrencyId.Ink)}";
        }

        public void Refresh()
        {
            if (content == null)
            {
                Debug.LogWarning("[ShopUI] Refresh failed: content is null.");
                return;
            }

            itemInfoPanel?.Hide();
            selectedEntry = null;

            for (var i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            if (mode == ShopPanelMode.Buy)
                RefreshBuy();
            else
                RefreshSell();
        }

        void RefreshBuy()
        {
            if (shopService == null || itemPrefab == null)
            {
                Debug.LogWarning("[ShopUI] Refresh Buy failed: missing refs.");
                return;
            }

            if (!shopService.IsOpen)
                return;

            var entries = shopService.GetVisibleEntries();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry?.item == null) continue;

                var view = Instantiate(itemPrefab, content);
                view.Bind(entry, shopService);
                view.OnSelected += HandleShopItemSelected;
            }
        }

        void RefreshSell()
        {
            if (inventory == null || sellItemPrefab == null || shopService == null)
            {
                Debug.LogWarning("[ShopUI] Refresh Sell failed: missing refs.");
                return;
            }

            var items = inventory.Items;
            for (var i = 0; i < items.Count; i++)
            {
                var stack = items[i];
                if (stack == null || stack.IsEmpty) continue;

                inventory.TryGetDefinition(stack.definitionId, out var def);

                var view = Instantiate(sellItemPrefab, content);
                view.Bind(i, stack, def, shopService);
            }
        }

        void HandleShopItemSelected(ShopEntry entry)
        {
            selectedEntry = entry;
            ShowBuyInfoPanel(entry);
        }

        void ShowBuyInfoPanel(ShopEntry entry)
        {
            if (itemInfoPanel == null || entry?.item == null)
                return;

            itemInfoPanel.ShowFixed(entry.item, entry, shopService);

            if (CanBuy(entry, out var reason))
            {
                var price = shopService.GetPrice(entry);
                itemInfoPanel.ConfigureAction($"Buy ({price})", OnClickBuySelected, true);
            }
            else
            {
                itemInfoPanel.ConfigureAction(reason, null, false);
            }
        }

        void OnClickBuySelected()
        {
            if (selectedEntry?.item == null || shopService == null) return;

            var itemId = selectedEntry.item.id;
            var result = shopService.TryBuy(itemId, 1);
            StoreInventoryLog.Info($"[ShopUI] Buy {itemId} → {result}");

            if (result == BuyResult.Success)
            {
                itemInfoPanel?.Hide();
                selectedEntry = null;
                Refresh();
                return;
            }

            ShowBuyInfoPanel(selectedEntry);
        }

        bool CanBuy(ShopEntry entry, out string reason)
        {
            reason = "Buy";

            if (entry?.item == null || shopService == null)
            {
                reason = "Unavailable";
                return false;
            }

            var item = entry.item;
            var remaining = shopService.GetRemainingStock(item.id);

            if (item.Category == ItemCategory.Equipment)
            {
                if (entry.stock == 0 || (remaining >= 0 && remaining <= 0))
                {
                    reason = "Sold out";
                    return false;
                }
            }

            if (shopService.CurrentShop == null)
            {
                reason = "Shop closed";
                return false;
            }

            var price = shopService.GetPrice(entry);
            if (wallet == null || !wallet.Has(CurrencyId.Ink, price))
            {
                reason = "Not enough Ink";
                return false;
            }

            return true;
        }
    }
}
