using UnityEngine;
using UnityEngine.UI;

namespace StoreAndInventory
{
    public class EquipmentSlotUI : MonoBehaviour
    {
        [SerializeField] int slotIndex;
        [SerializeField] Image icon;
        [SerializeField] Image emptyFrame;
        [SerializeField] Button button;

        [Header("Selection")]
        [SerializeField] Color emptyFrameNormalColor = new(1f, 1f, 1f, 0.35f);
        [SerializeField] Color emptyFrameSelectedColor = new(0.55f, 0.85f, 1f, 0.95f);

        EquipmentService equipmentService;
        ItemDatabase database;
        bool selected;

        public int SlotIndex => slotIndex;
        public event System.Action<int> OnSlotClicked;

        public void Bind(EquipmentService service, ItemDatabase db)
        {
            equipmentService = service;
            database = db;

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }

            Refresh();
        }

        public void SetSelected(bool isSelected)
        {
            selected = isSelected;
            ApplySelectionVisual();
        }

        void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        public void Refresh()
        {
            if (equipmentService == null)
            {
                SetEmpty();
                return;
            }

            var stack = equipmentService.GetEquippedStack(slotIndex);
            if (stack == null)
            {
                SetEmpty();
                return;
            }

            ItemBase def = null;
            database?.TryGet(stack.definitionId, out def);

            if (icon != null)
            {
                icon.sprite = def != null ? def.icon : null;
                icon.enabled = def != null && def.icon != null;
            }

            if (emptyFrame != null)
                emptyFrame.enabled = false;

            ApplySelectionVisual();
        }

        void SetEmpty()
        {
            if (icon != null)
            {
                icon.sprite = null;
                icon.enabled = false;
            }

            if (emptyFrame != null)
                emptyFrame.enabled = true;

            ApplySelectionVisual();
        }

        void ApplySelectionVisual()
        {
            if (emptyFrame == null || !emptyFrame.enabled)
                return;

            emptyFrame.color = selected ? emptyFrameSelectedColor : emptyFrameNormalColor;
        }

        void HandleClick()
        {
            OnSlotClicked?.Invoke(slotIndex);
        }
    }
}
