using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace StoreAndInventory
{
    public class InventoryItemUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] Image iconImage;
        [SerializeField] TextMeshProUGUI countText;
        [SerializeField] Image backgroundImage;

        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color selectedColor = new(0.75f, 0.9f, 1f, 1f);

        int bagIndex = -1;
        bool selected;

        public int BagIndex => bagIndex;
        public event Action<int> OnClicked;

        public void Bind(int index, ItemStack stack, ItemBase definition)
        {
            bagIndex = index;

            if (iconImage != null)
            {
                iconImage.sprite = definition != null ? definition.icon : null;
                iconImage.enabled = definition != null && definition.icon != null;
            }

            if (countText != null)
            {
                var showCount = stack != null && stack.count > 1;
                countText.enabled = showCount;
                countText.text = showCount ? stack.count.ToString() : string.Empty;
            }

            ApplySelectedVisual();
        }

        public void SetSelected(bool isSelected)
        {
            selected = isSelected;
            ApplySelectedVisual();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (bagIndex < 0) return;
            OnClicked?.Invoke(bagIndex);
        }

        void ApplySelectedVisual()
        {
            if (backgroundImage == null) return;
            backgroundImage.color = selected ? selectedColor : normalColor;
        }
    }
}
