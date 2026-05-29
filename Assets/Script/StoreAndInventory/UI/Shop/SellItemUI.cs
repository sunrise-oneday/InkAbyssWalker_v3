using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StoreAndInventory
{
    public class SellItemUI : MonoBehaviour
    {
        [SerializeField] Image iconImage;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI countText;
        [SerializeField] TextMeshProUGUI priceText;
        [SerializeField] Button sellButton;

        int bagIndex = -1;
        ShopService shopService;

        void Awake()
        {
            if (sellButton != null)
                sellButton.onClick.AddListener(OnClickSell);
        }

        void OnDestroy()
        {
            if (sellButton != null)
                sellButton.onClick.RemoveListener(OnClickSell);
        }

        public void Bind(int index, ItemStack stack, ItemBase definition, ShopService service)
        {
            bagIndex = index;
            shopService = service;

            if (iconImage != null)
            {
                iconImage.sprite = definition != null ? definition.icon : null;
                iconImage.enabled = definition != null && definition.icon != null;
            }

            if (nameText != null)
                nameText.text = definition != null ? definition.Name : stack?.definitionId ?? "?";

            if (countText != null)
            {
                var showCount = stack != null && stack.count > 1;
                countText.enabled = showCount;
                countText.text = showCount ? stack.count.ToString() : string.Empty;
            }

            var sellable = definition != null && definition.canSell;
            var price = definition != null ? definition.basePrice : 0;

            if (priceText != null)
                priceText.text = sellable ? $"Sell {price}" : "Cannot sell";

            if (sellButton != null)
                sellButton.interactable = sellable && shopService != null;
        }

        void OnClickSell()
        {
            if (shopService == null || bagIndex < 0) return;

            var result = shopService.TrySell(bagIndex, 1, out var msg);
            StoreInventoryLog.Info($"[SellItemUI] Sell index={bagIndex} -> {result} {msg}");
        }
    }
}
