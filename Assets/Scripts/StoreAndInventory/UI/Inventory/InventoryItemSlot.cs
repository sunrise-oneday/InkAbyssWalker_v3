using System;
using UnityEngine;
using UnityEngine.UI;

namespace StoreAndInventory
{
    /// <summary>
    /// 背包物品列表槽位：显示图标 + 拥有数量，点击触发选中事件。
    /// 用于 leftSVItem 的无限滚动列表。
    /// </summary>
    public class InventoryItemSlot : MonoBehaviour
    {
        [SerializeField] Image itemIcon;
        [SerializeField] Text txtNum;

        int bagIndex = -1;
        bool selected;
        Button button;

        public int BagIndex => bagIndex;
        public event Action<int> OnClicked;

        void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnClicked?.Invoke(bagIndex));
            }
        }

        void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }

        public void Bind(int index, ItemStack stack, ItemBase definition)
        {
            bagIndex = index;

            if (itemIcon != null)
            {
                itemIcon.sprite = definition != null ? definition.icon : null;
                itemIcon.enabled = definition != null && definition.icon != null;
            }

            if (txtNum != null)
            {
                var showCount = stack != null && stack.count >= 1;
                txtNum.text = showCount ? $"x{stack.count}" : string.Empty;
            }
        }

        public void SetSelected(bool isSelected)
        {
            selected = isSelected;
        }
    }
}
