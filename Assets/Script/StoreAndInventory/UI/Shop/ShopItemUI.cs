using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace StoreAndInventory
{
    public class ShopItemUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] Image iconImage;
        [SerializeField] TextMeshProUGUI priceText;
        [SerializeField] TextMeshProUGUI stockText;

        ShopEntry entry;
        ShopService shopService;
        int price;
        int remainingStock;

        public ShopEntry Entry => entry;
        public event Action<ShopEntry> OnSelected;

        public void Bind(ShopEntry shopEntry, ShopService service)
        {
            entry = shopEntry;
            shopService = service;

            var item = entry?.item;
            price = shopService != null && entry != null ? shopService.GetPrice(entry) : 0;
            remainingStock = shopService != null && item != null ? shopService.GetRemainingStock(item.id) : 0;

            if (iconImage != null)
            {
                iconImage.sprite = item != null ? item.icon : null;
                iconImage.enabled = item != null && item.icon != null;
            }

            if (priceText != null)
                priceText.text = price.ToString();

            if (stockText != null)
            {
                if (item != null && item.Category == ItemCategory.Equipment && remainingStock >= 0)
                {
                    stockText.text = $"Left {remainingStock}";
                    stockText.enabled = true;
                }
                else
                {
                    stockText.text = string.Empty;
                    stockText.enabled = false;
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (entry?.item == null) return;
            OnSelected?.Invoke(entry);
        }
    }
}
