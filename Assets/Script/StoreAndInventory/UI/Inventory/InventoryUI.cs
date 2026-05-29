using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StoreAndInventory
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] Inventory inventory;
        [SerializeField] ItemDatabase database;
        [SerializeField] TestCharacter testCharacter;
        [SerializeField] EquipmentService equipmentService;

        [SerializeField] InventoryItemUI itemPrefab;
        [SerializeField] Transform content;

        [SerializeField] AttributeStatLineUI[] statLines;
        [SerializeField] EquipmentSlotUI[] runeSlots;
        [SerializeField] ItemTooltipUI itemInfoPanel;
        [SerializeField] Button useButton;
        [SerializeField] Button closeButton;

        readonly List<InventoryItemUI> itemViews = new();
        int selectedBagIndex = -1;
        int selectedRuneSlotIndex = -1;
        int infoPanelRuneSlotIndex = -1;

        public bool IsOpen => gameObject.activeSelf;

        void Awake()
        {
            if (inventory == null)
                inventory = FindObjectOfType<Inventory>();
            if (database == null)
                database = FindObjectOfType<ItemDatabase>();
            if (testCharacter == null)
                testCharacter = FindObjectOfType<TestCharacter>();
            if (equipmentService == null)
                equipmentService = FindObjectOfType<EquipmentService>();
            if (itemInfoPanel == null)
                itemInfoPanel = transform.Find("ItemInfoPanel")?.GetComponent<ItemTooltipUI>()
                                  ?? GetComponentInChildren<ItemTooltipUI>(true);

            if (useButton != null)
                useButton.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (inventory != null)
                inventory.OnChanged += HandleInventoryChanged;

            if (equipmentService != null)
            {
                equipmentService.OnEquipped += HandleEquipmentChanged;
                equipmentService.OnUnequipped += HandleEquipmentChanged;
            }

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            BindRuneSlots();
            RefreshAll();
        }

        void OnDisable()
        {
            if (inventory != null)
                inventory.OnChanged -= HandleInventoryChanged;

            if (equipmentService != null)
            {
                equipmentService.OnEquipped -= HandleEquipmentChanged;
                equipmentService.OnUnequipped -= HandleEquipmentChanged;
            }

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);

            itemInfoPanel?.Hide();
        }

        void BindRuneSlots()
        {
            if (runeSlots == null) return;

            for (var i = 0; i < runeSlots.Length; i++)
            {
                var slot = runeSlots[i];
                if (slot == null) continue;

                slot.Bind(equipmentService, database);
                slot.OnSlotClicked -= HandleRuneSlotClicked;
                slot.OnSlotClicked += HandleRuneSlotClicked;
            }
        }

        public void Open()
        {
            gameObject.SetActive(true);
            RefreshAll();
        }

        public void Close()
        {
            itemInfoPanel?.Hide();
            gameObject.SetActive(false);
            selectedBagIndex = -1;
            selectedRuneSlotIndex = -1;
            infoPanelRuneSlotIndex = -1;
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        void HandleInventoryChanged()
        {
            RefreshItems();
            if (selectedBagIndex >= inventory.Count)
            {
                selectedBagIndex = -1;
                itemInfoPanel?.Hide();
            }
        }

        void HandleEquipmentChanged(int slotIndex, ItemStack stack)
        {
            RefreshRuneSlots();
            RefreshItems();
            RefreshStats();
            LogStatSnapshot($"equip change slot={slotIndex} id={stack?.definitionId}");

            if (infoPanelRuneSlotIndex == slotIndex && equipmentService.GetEquippedStack(slotIndex) == null)
                itemInfoPanel?.Hide();
        }

        void HandleRuneSlotClicked(int slotIndex)
        {
            if (equipmentService == null) return;

            SetSelectedRuneSlot(slotIndex);

            var stack = equipmentService.GetEquippedStack(slotIndex);
            if (stack == null)
            {
                itemInfoPanel?.Hide();
                infoPanelRuneSlotIndex = -1;
                return;
            }

            if (!inventory.TryGetDefinition(stack.definitionId, out var def) || def == null)
                return;

            infoPanelRuneSlotIndex = slotIndex;
            selectedBagIndex = -1;
            RefreshSelection();
            ShowEquippedRunePanel(def, slotIndex);
        }

        void HandleItemClicked(int bagIndex)
        {
            selectedBagIndex = bagIndex;
            infoPanelRuneSlotIndex = -1;
            RefreshSelection();

            if (inventory == null || bagIndex < 0 || bagIndex >= inventory.Count)
                return;

            var stack = inventory.Items[bagIndex];
            if (stack == null || stack.IsEmpty) return;

            if (!inventory.TryGetDefinition(stack.definitionId, out var def) || def == null)
                return;

            ShowBagItemPanel(def, bagIndex);
        }

        void ShowBagItemPanel(ItemBase def, int bagIndex)
        {
            if (itemInfoPanel == null) return;

            itemInfoPanel.ShowAtClickPosition(def, Input.mousePosition);

            switch (def.Category)
            {
                case ItemCategory.Equipment:
                    if (selectedRuneSlotIndex >= 0)
                        itemInfoPanel.ConfigureAction("Equip", () => OnClickEquip(bagIndex, selectedRuneSlotIndex), true);
                    else
                        itemInfoPanel.ConfigureAction("Select rune slot", null, false);
                    break;

                case ItemCategory.Consumable:
                    itemInfoPanel.ConfigureAction("Use", () => OnClickUse(bagIndex), true);
                    break;

                default:
                    itemInfoPanel.ConfigureAction(null, null, false);
                    break;
            }
        }

        void ShowEquippedRunePanel(ItemBase def, int slotIndex)
        {
            if (itemInfoPanel == null) return;

            itemInfoPanel.ShowAtClickPosition(def, Input.mousePosition);
            itemInfoPanel.ConfigureAction("Unequip", () => OnClickUnequip(slotIndex), true);
        }

        void OnClickEquip(int bagIndex, int runeSlotIndex)
        {
            if (equipmentService == null) return;

            if (!equipmentService.TryEquipFromBag(bagIndex, runeSlotIndex, out var msg))
                Debug.LogWarning($"[InventoryUI] Equip: {msg}");
            else
            {
                itemInfoPanel?.Hide();
                selectedBagIndex = -1;
                RefreshSelection();
            }
        }

        void OnClickUnequip(int runeSlotIndex)
        {
            if (equipmentService == null) return;

            if (!equipmentService.TryUnequip(runeSlotIndex, out var msg))
                Debug.LogWarning($"[InventoryUI] Unequip: {msg}");
            else
            {
                itemInfoPanel?.Hide();
                infoPanelRuneSlotIndex = -1;
            }
        }

        void OnClickUse(int bagIndex)
        {
            if (inventory == null) return;

            var stack = inventory.Items[bagIndex];
            if (stack == null || stack.IsEmpty) return;

            if (!inventory.TryGetDefinition(stack.definitionId, out var def))
                return;

            if (def.Category != ItemCategory.Consumable)
            {
                Debug.LogWarning("[InventoryUI] Use failed: not consumable.");
                return;
            }

            var consumable = def as Consumable;
            var need = consumable != null && consumable.consumeOnUse > 0 ? consumable.consumeOnUse : 1;

            if (!inventory.TryConsumeAt(bagIndex, need, UseContext.Exploration, out var effects, out var msg))
            {
                Debug.LogWarning($"[InventoryUI] Use failed: {msg}");
                return;
            }

            if (effects != null)
            {
                for (var i = 0; i < effects.Count; i++)
                {
                    var fx = effects[i];
                    var fxName = fx != null ? fx.name : "(null)";
                    StoreInventoryLog.Info($"[InventoryUI] Use effect: {fxName}");
                }
            }

            itemInfoPanel?.Hide();
            selectedBagIndex = -1;
            RefreshItems();
        }

        void SetSelectedRuneSlot(int slotIndex)
        {
            selectedRuneSlotIndex = slotIndex;
            RefreshRuneSlotSelection();
        }

        public void RefreshAll()
        {
            RefreshStats();
            RefreshItems();
            RefreshRuneSlots();
            RefreshRuneSlotSelection();
        }

        public void RefreshStats()
        {
            if (testCharacter == null || statLines == null) return;

            var mods = equipmentService != null ? equipmentService.GetAllStatMods() : null;

            for (var i = 0; i < statLines.Length; i++)
            {
                var line = statLines[i];
                if (line == null) continue;

                var stat = line.StatType;
                var baseValue = testCharacter.Get(stat);
                var bonus = StatDisplayUtil.SumEquipmentBonus(baseValue, stat, mods);
                line.Bind(baseValue, bonus);
            }
        }

        void LogStatSnapshot(string reason)
        {
            if (testCharacter == null || statLines == null) return;

            var mods = equipmentService != null ? equipmentService.GetAllStatMods() : null;
            var modCount = mods?.Count ?? 0;
            StoreInventoryLog.Info($"[InventoryUI] {reason} statMods={modCount}");

            for (var i = 0; i < statLines.Length; i++)
            {
                var line = statLines[i];
                if (line == null) continue;

                var stat = line.StatType;
                var baseValue = testCharacter.Get(stat);
                var bonus = StatDisplayUtil.SumEquipmentBonus(baseValue, stat, mods);
                var effective = baseValue + bonus;
                StoreInventoryLog.Info(
                    $"[InventoryUI]   {StatDisplayUtil.Label(stat)} base={StatDisplayUtil.FormatValue(stat, baseValue)} " +
                    $"bonus={StatDisplayUtil.FormatValue(stat, bonus)} effective={StatDisplayUtil.FormatValue(stat, effective)}");
            }
        }

        public void RefreshItems()
        {
            if (content == null || itemPrefab == null || inventory == null)
                return;

            for (var i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            itemViews.Clear();

            var items = inventory.Items;
            for (var i = 0; i < items.Count; i++)
            {
                var stack = items[i];
                if (stack == null || stack.IsEmpty) continue;

                inventory.TryGetDefinition(stack.definitionId, out var def);

                var view = Instantiate(itemPrefab, content);
                view.Bind(i, stack, def);
                view.OnClicked += HandleItemClicked;
                view.SetSelected(i == selectedBagIndex);
                itemViews.Add(view);
            }

            if (selectedBagIndex >= inventory.Count)
                selectedBagIndex = -1;
        }

        void RefreshSelection()
        {
            for (var i = 0; i < itemViews.Count; i++)
            {
                var view = itemViews[i];
                if (view != null)
                    view.SetSelected(view.BagIndex == selectedBagIndex);
            }
        }

        public void RefreshRuneSlots()
        {
            if (runeSlots == null) return;

            for (var i = 0; i < runeSlots.Length; i++)
            {
                if (runeSlots[i] != null)
                    runeSlots[i].Refresh();
            }

            RefreshRuneSlotSelection();
        }

        void RefreshRuneSlotSelection()
        {
            if (runeSlots == null) return;

            for (var i = 0; i < runeSlots.Length; i++)
            {
                if (runeSlots[i] != null)
                    runeSlots[i].SetSelected(i == selectedRuneSlotIndex);
            }
        }
    }
}
