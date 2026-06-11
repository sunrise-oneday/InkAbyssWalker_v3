using System;
using UnityEngine;
using UnityEngine.UI;

namespace StoreAndInventory
{
    /// <summary>
    /// 商店物品列表项，自身挂 Button 组件作为点击交互。
    /// 显示图标、名称、钱币图标、价格、持有数量。
    /// </summary>
    public class ShopItemSlot : MonoBehaviour
    {
        [SerializeField] Image iconImage;
        [SerializeField] Text nameText;
        [SerializeField] Image inkIcon;
        [SerializeField] Sprite inkSprite;
        [SerializeField] Sprite spiritSprite;
        [SerializeField] Text priceText;
        [SerializeField] Text countText;

        ShopEntry entry;
        Button button;

        public ShopEntry Entry => entry;
        public event Action<ShopEntry> OnSelected;

        void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(OnClick);
        }

        void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(OnClick);
        }

        public void Bind(ShopEntry shopEntry, int remainingStock, int displayPrice)
        {
            entry = shopEntry;

            var item = entry?.item;

            if (iconImage != null)
            {
                iconImage.sprite = item != null ? item.icon : null;
                iconImage.enabled = item != null && item.icon != null;
            }

            if (nameText != null)
                nameText.text = item != null ? item.Name : string.Empty;

            if (inkIcon != null && item != null)
            {
                var sprite = item.currencyId == CurrencyId.Spirit ? spiritSprite : inkSprite;
                if (sprite != null)
                {
                    inkIcon.sprite = sprite;
                    inkIcon.enabled = true;
                }
            }

            if (priceText != null)
            {
                if (displayPrice > 0 && item != null)
                {
                    var symbol = item.currencyId == CurrencyId.Spirit ? "灵" : "墨";
                    priceText.text = displayPrice + symbol;
                }
                else
                {
                    priceText.text = string.Empty;
                }
            }

            // 显示剩余可购买数量（库存）。-1 = 无限库存，不显示
            if (countText != null)
                countText.text = remainingStock >= 0 ? $"x{remainingStock}" : string.Empty;
        }

        void OnClick()
        {
            if (entry?.item == null) return;
            OnSelected?.Invoke(entry);
        }
    }
}
