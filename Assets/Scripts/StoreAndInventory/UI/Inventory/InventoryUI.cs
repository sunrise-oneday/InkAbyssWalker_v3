using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StoreAndInventory
{
    /// <summary>
    /// 背包面板控制器。
    /// 左侧：物品列表（支持分类筛选 + 无限滚动）。
    /// 右侧：技能槽（形态切换联动）。
    /// 底部：详情面板（物品/技能描述 + 装备/卸下）。
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        // ── 服务依赖 ──
        [SerializeField] Inventory inventory;
        [SerializeField] ItemDatabase database;
        [SerializeField] EquipmentService equipmentService;
        [SerializeField] PlayerBattleEntity playerBattleEntity;

        // ── 左侧物品列表 ──
        [Header("左侧物品列表")]
        [SerializeField] Transform leftContent;
        [SerializeField] InventoryItemSlot itemSlotPrefab;

        // ── 技能槽（playerInfo 下的 5 个按钮） ──
        [Header("技能槽按钮")]
        [SerializeField] Button[] skillButtons;       // btnSkill1~4 + btnSkilUlt
        [SerializeField] Image[] skillIcons;           // 技能图标
        [SerializeField] Text[] txtEquips;             // 装备显示文本

        // ── 技能图标配置 ──
        [Header("技能图标（按 SkillRuneSlotConfig.AllSlotIds 顺序配置）")]
        [SerializeField] Sprite[] skillSprites;        // 13 个技能图标（12 技能 + 1 大招）

        // ── 形态切换 ──
        [Header("形态切换")]
        [SerializeField] Dropdown dropdownForm;
        [SerializeField] Image[] imgPlayers;           // 形态角色图片 imgPlayer1/2/3

        // ── 详情面板 ──
        [Header("详情面板")]
        [SerializeField] GameObject detailPanel;
        [SerializeField] Text txtDetailName;
        [SerializeField] Text txtDetailDesc;
        [SerializeField] Image imgDetailIcon;
        [SerializeField] Button btnEquip;
        [SerializeField] Button btnUnequip;

        // ── 关闭按钮 ──
        [Header("关闭按钮")]
        [SerializeField] Button closeButton;

        // ── 内部状态 ──
        int selectedBagIndex = -1;
        int selectedSkillIndex = -1;
        int currentFormIndex = 0;
        bool lastInteractionWasSkill; // true=技能, false=物品
        readonly List<InventoryItemSlot> spawnedSlots = new();

        /// <summary>背包面板关闭时触发。</summary>
        public event Action Closed;

        // ── 技能槽映射 ──
        // skillButtons[0~3] 对应当前形态的 4 个技能
        // skillButtons[4] 对应大招槽
        // 通过 currentFormIndex 从 SkillRuneSlotConfig.FormSlotIds 获取技能 ID

        public bool IsOpen => gameObject.activeSelf;

        void Awake()
        {
            if (inventory == null) inventory = FindObjectOfType<Inventory>();
            if (database == null) database = FindObjectOfType<ItemDatabase>();
            if (equipmentService == null) equipmentService = FindObjectOfType<EquipmentService>();
            if (playerBattleEntity == null) playerBattleEntity = FindObjectOfType<PlayerBattleEntity>();
        }

        void OnEnable()
        {
            if (inventory != null) inventory.OnChanged += RefreshAll;
            if (equipmentService != null)
            {
                equipmentService.OnEquipped += OnEquipChanged;
                equipmentService.OnUnequipped += OnEquipChanged;
            }

            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (btnEquip != null) btnEquip.onClick.AddListener(OnClickEquip);
            if (btnUnequip != null) btnUnequip.onClick.AddListener(OnClickUnequip);

            // 技能按钮绑定
            for (var i = 0; i < skillButtons.Length; i++)
            {
                var idx = i;
                if (skillButtons[i] != null)
                    skillButtons[i].onClick.AddListener(() => OnSkillClicked(idx));
            }

            // 形态下拉框绑定
            if (dropdownForm != null)
            {
                dropdownForm.onValueChanged.RemoveAllListeners();
                dropdownForm.onValueChanged.AddListener(OnFormChanged);
            }

            // 初始化形态下拉框选项
            InitFormDropdown();

            // 自动配置滚动布局
            EnsureScrollLayout();

            RefreshAll();
        }

        void OnDisable()
        {
            if (inventory != null) inventory.OnChanged -= RefreshAll;
            if (equipmentService != null)
            {
                equipmentService.OnEquipped -= OnEquipChanged;
                equipmentService.OnUnequipped -= OnEquipChanged;
            }

            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (btnEquip != null) btnEquip.onClick.RemoveListener(OnClickEquip);
            if (btnUnequip != null) btnUnequip.onClick.RemoveListener(OnClickUnequip);

            for (var i = 0; i < skillButtons.Length; i++)
            {
                if (skillButtons[i] != null)
                    skillButtons[i].onClick.RemoveAllListeners();
            }

            if (dropdownForm != null)
                dropdownForm.onValueChanged.RemoveListener(OnFormChanged);

            ClearSlots();
        }

        // ── 公共 API ──

        public void Open()
        {
            gameObject.SetActive(true);
            currentFormIndex = 0;
            if (dropdownForm != null) dropdownForm.value = 0;
            RefreshAll();
        }

        public void Close()
        {
            gameObject.SetActive(false);
            selectedBagIndex = -1;
            selectedSkillIndex = -1;
            lastInteractionWasSkill = false;
            Closed?.Invoke();
        }

        // ── 形态切换 ──

        void InitFormDropdown()
        {
            if (dropdownForm == null) return;
            dropdownForm.ClearOptions();
            dropdownForm.AddOptions(new List<string>(SkillRuneSlotConfig.FormNames));
        }

        void OnFormChanged(int index)
        {
            currentFormIndex = index;
            RefreshFormImage();
            RefreshSkillButtons();
            RefreshDetailPanel();
        }

        /// <summary>切换形态角色图片显示。</summary>
        void RefreshFormImage()
        {
            if (imgPlayers == null) return;
            for (var i = 0; i < imgPlayers.Length; i++)
            {
                if (imgPlayers[i] != null)
                    imgPlayers[i].gameObject.SetActive(i == currentFormIndex);
            }
        }

        // ── 滚动布局自动配置 ──

        void EnsureScrollLayout()
        {
            if (leftContent == null) return;

            // Content 已有 GridLayoutGroup 或其他 LayoutGroup 时，不再添加新的 LayoutGroup
            // 只确保有 ContentSizeFitter，让 Content 高度随子项数量自动扩展，从而启用 ScrollView 滚动
            var fitter = leftContent.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = leftContent.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
        }

        // ── 物品列表 ──

        void RefreshItems()
        {
            ClearSlots();
            if (leftContent == null || itemSlotPrefab == null || inventory == null) return;

            var items = inventory.Items;
            for (var i = 0; i < items.Count; i++)
            {
                var stack = items[i];
                if (stack == null || stack.IsEmpty) continue;

                if (!inventory.TryGetDefinition(stack.definitionId, out var def)) continue;
                if (def == null || def.Category != ItemCategory.Equipment) continue;

                var slot = Instantiate(itemSlotPrefab, leftContent);
                slot.Bind(i, stack, def);
                slot.OnClicked += OnItemClicked;
                slot.SetSelected(i == selectedBagIndex);
                spawnedSlots.Add(slot);
            }

            // 修正选中索引（装备后物品可能已从背包移除）
            if (selectedBagIndex < 0 || selectedBagIndex >= items.Count)
            {
                selectedBagIndex = -1;
            }
            else
            {
                var stack = items[selectedBagIndex];
                if (stack == null || stack.IsEmpty || !inventory.TryGetDefinition(stack.definitionId, out var def2) || def2 == null || def2.Category != ItemCategory.Equipment)
                {
                    selectedBagIndex = -1;
                }
            }

        }

        void ClearSlots()
        {
            for (var i = spawnedSlots.Count - 1; i >= 0; i--)
            {
                if (spawnedSlots[i] != null)
                    Destroy(spawnedSlots[i].gameObject);
            }
            spawnedSlots.Clear();
        }

        void OnItemClicked(int bagIndex)
        {
            selectedBagIndex = bagIndex;
            lastInteractionWasSkill = false;
            RefreshItemSelection();

            if (inventory == null || bagIndex < 0 || bagIndex >= inventory.Count) return;

            var stack = inventory.Items[bagIndex];
            if (stack == null || stack.IsEmpty) return;

            if (!inventory.TryGetDefinition(stack.definitionId, out var def) || def == null) return;

            ShowItemDetail(def);
        }

        void RefreshItemSelection()
        {
            for (var i = 0; i < spawnedSlots.Count; i++)
            {
                if (spawnedSlots[i] != null)
                    spawnedSlots[i].SetSelected(spawnedSlots[i].BagIndex == selectedBagIndex);
            }
        }

        // ── 技能槽 ──

        void RefreshSkillButtons()
        {
            if (skillButtons == null || equipmentService == null) return;

            var slotIds = GetFormSlotIds();

            for (var i = 0; i < skillButtons.Length; i++)
            {
                if (skillButtons[i] == null) continue;

                // 更新装备显示
                var slotIndex = GetSlotIndexForSkillButton(i);
                var equipped = equipmentService.GetEquippedItem(slotIndex);

                if (txtEquips != null && i < txtEquips.Length && txtEquips[i] != null)
                {
                    txtEquips[i].text = equipped != null ? equipped.Name : "";
                }

                // 设置技能图标
                if (skillIcons != null && i < skillIcons.Length && skillIcons[i] != null)
                {
                    var slotId = equipmentService.GetSlotId(slotIndex);
                    var sprite = GetSkillSprite(slotId);
                    skillIcons[i].sprite = sprite;
                    skillIcons[i].enabled = sprite != null;
                }
            }
        }

        /// <summary>根据技能 ID 获取对应的图标。</summary>
        Sprite GetSkillSprite(string slotId)
        {
            if (string.IsNullOrEmpty(slotId) || skillSprites == null) return null;

            var allSlotIds = SkillRuneSlotConfig.AllSlotIds;
            for (var i = 0; i < allSlotIds.Length; i++)
            {
                if (allSlotIds[i] == slotId && i < skillSprites.Length)
                    return skillSprites[i];
            }
            return null;
        }

        /// <summary>获取技能描述。</summary>
        string GetSkillDescription(string slotId)
        {
            if (string.IsNullOrEmpty(slotId) || playerBattleEntity == null) return "";

            var allForms = playerBattleEntity.availableForms;
            if (allForms == null) return "";

            // 遍历所有形态查找技能
            for (var f = 0; f < allForms.Count; f++)
            {
                var skills = allForms[f].availableSkills;
                if (skills == null) continue;

                for (var s = 0; s < skills.Count; s++)
                {
                    if (skills[s].skillName == slotId)
                        return skills[s].description;
                }
            }

            // 大招
            if (slotId == SkillRuneSlotConfig.UltimateSlotId && playerBattleEntity.equippedUltimate != null)
            {
                return playerBattleEntity.equippedUltimate.description;
            }

            return "";
        }

        /// <summary>获取当前形态的 4 个技能槽 ID，外加大招。</summary>
        string[] GetFormSlotIds()
        {
            if (currentFormIndex < SkillRuneSlotConfig.FormSlotIds.Length)
                return SkillRuneSlotConfig.FormSlotIds[currentFormIndex];
            return SkillRuneSlotConfig.FormSlotIds[0];
        }

        /// <summary>技能按钮索引 → 全局槽位索引。</summary>
        int GetSlotIndexForSkillButton(int buttonIndex)
        {
            // 0~3 = 当前形态的 4 个技能槽
            if (buttonIndex < 4)
            {
                var formSlots = GetFormSlotIds();
                if (buttonIndex < formSlots.Length)
                    return equipmentService.GetSlotIndexBySkillId(formSlots[buttonIndex]);
            }
            // 4 = 大招槽
            return equipmentService.GetSlotIndexBySkillId(SkillRuneSlotConfig.UltimateSlotId);
        }

        void OnSkillClicked(int index)
        {
            selectedSkillIndex = index;
            lastInteractionWasSkill = true;
            RefreshItemSelection();

            // 显示技能详情
            var slotIndex = GetSlotIndexForSkillButton(index);
            var equipped = equipmentService.GetEquippedItem(slotIndex);
            var slotId = equipmentService.GetSlotId(slotIndex);

            ShowSkillDetail(slotId, equipped);
        }

        // ── 详情面板 ──

        void RefreshDetailPanel()
        {
            // 优先级：技能槽选中 > 物品选中 > 隐藏
            if (selectedSkillIndex >= 0)
            {
                var slotIndex = GetSlotIndexForSkillButton(selectedSkillIndex);
                var equipped = equipmentService.GetEquippedItem(slotIndex);
                var slotId = equipmentService.GetSlotId(slotIndex);
                ShowSkillDetail(slotId, equipped);
            }
            else if (selectedBagIndex >= 0 && selectedBagIndex < inventory.Count)
            {
                var stack = inventory.Items[selectedBagIndex];
                if (stack != null && inventory.TryGetDefinition(stack.definitionId, out var def) && def != null)
                    ShowItemDetail(def);
            }
            else
            {
                HideDetail();
            }
        }

        void ShowItemDetail(ItemBase def)
        {
            if (detailPanel == null) return;
            detailPanel.SetActive(true);

            if (txtDetailName != null) txtDetailName.text = def.Name;

            if (txtDetailDesc != null)
            {
                var desc = def.description;

                // 显示属性加成
                if (def is Equipment equip)
                {
                    if (equip.statMods != null && equip.statMods.Count > 0)
                    {
                        desc += "\n\n属性加成:";
                        foreach (var mod in equip.statMods)
                            desc += $"\n  {StatDisplayUtil.Label(mod.stat)} +{mod.flat:0.#}";
                    }
                    if (equip.skillMods != null && equip.skillMods.Count > 0)
                    {
                        desc += "\n技能效果:";
                        foreach (var mod in equip.skillMods)
                            desc += $"\n  {mod.targetId} {mod.modType} +{mod.value}";
                    }
                }

                if (selectedSkillIndex >= 0)
                {
                    var slotIndex = GetSlotIndexForSkillButton(selectedSkillIndex);
                    var slotId = equipmentService.GetSlotId(slotIndex);
                    desc += $"\n\n<color=#4CAF50>将装备到: {slotId}</color>";
                }
                else
                {
                    desc += "\n\n<color=#888888>点击右侧技能槽选择装备位置</color>";
                }

                txtDetailDesc.text = desc;
            }

            if (imgDetailIcon != null)
            {
                imgDetailIcon.sprite = def.icon;
                imgDetailIcon.enabled = def.icon != null;
            }

            // 装备按钮：同时选中物品和技能槽时才可用
            if (btnEquip != null) btnEquip.gameObject.SetActive(selectedSkillIndex >= 0);
            if (btnUnequip != null) btnUnequip.gameObject.SetActive(false);
        }

        void ShowSkillDetail(string skillId, Equipment equippedRune)
        {
            if (detailPanel == null) return;
            detailPanel.SetActive(true);

            if (txtDetailName != null) txtDetailName.text = skillId ?? "未知技能";

            if (txtDetailDesc != null)
            {
                // 获取技能描述
                var skillDesc = GetSkillDescription(skillId);
                var desc = "";

                if (!string.IsNullOrEmpty(skillDesc))
                {
                    desc = skillDesc;
                }

                if (equippedRune != null)
                {
                    desc += $"\n\n<color=#FFD700>已装备符文: {equippedRune.Name}</color>";
                    if (equippedRune.statMods != null && equippedRune.statMods.Count > 0)
                    {
                        desc += "\n属性加成:";
                        foreach (var mod in equippedRune.statMods)
                            desc += $"\n  {StatDisplayUtil.Label(mod.stat)} +{mod.flat:0.#}";
                    }
                    if (equippedRune.skillMods != null && equippedRune.skillMods.Count > 0)
                    {
                        desc += "\n技能效果:";
                        foreach (var mod in equippedRune.skillMods)
                            desc += $"\n  {mod.targetId} {mod.modType} +{mod.value}";
                    }
                }
                else
                {
                    desc += "\n\n<color=#888888>未装备符文 - 选择左侧符文并点击装备</color>";
                }

                txtDetailDesc.text = desc;
            }

            if (imgDetailIcon != null)
            {
                // 显示技能图标
                var sprite = GetSkillSprite(skillId);
                imgDetailIcon.sprite = sprite;
                imgDetailIcon.enabled = sprite != null;
            }

            // 如果同时选中了物品，显示装备按钮
            if (btnEquip != null) btnEquip.gameObject.SetActive(selectedBagIndex >= 0);
            if (btnUnequip != null) btnUnequip.gameObject.SetActive(equippedRune != null && selectedBagIndex < 0);
        }

        void HideDetail()
        {
            if (detailPanel != null) detailPanel.SetActive(false);
        }

        // ── 装备/卸下 ──

        void OnClickEquip()
        {
            if (selectedBagIndex < 0) return;

            // 如果没有选中技能槽，提示用户
            if (selectedSkillIndex < 0)
            {
                if (txtDetailDesc != null)
                    txtDetailDesc.text = "<color=red>请先点击右侧技能槽选择要装备的位置</color>";
                return;
            }

            if (equipmentService == null || inventory == null) return;

            var slotIndex = GetSlotIndexForSkillButton(selectedSkillIndex);
            if (slotIndex < 0) return;

            if (!equipmentService.TryEquipFromBag(selectedBagIndex, slotIndex, out var msg))
            {
                Debug.LogWarning($"[InventoryUI] Equip failed: {msg}");
                // 在详情面板提示错误
                if (txtDetailDesc != null)
                    txtDetailDesc.text = $"<color=red>无法装备: {msg}</color>";
                return;
            }

            // 装备成功，刷新
            selectedBagIndex = -1;
            RefreshAll();
        }

        void OnClickUnequip()
        {
            if (selectedSkillIndex < 0) return;
            if (equipmentService == null) return;

            var slotIndex = GetSlotIndexForSkillButton(selectedSkillIndex);
            if (slotIndex < 0) return;

            if (!equipmentService.TryUnequip(slotIndex, out var msg))
            {
                Debug.LogWarning($"[InventoryUI] Unequip failed: {msg}");
                return;
            }

            RefreshAll();
        }

        void OnEquipChanged(int slotIndex, ItemStack stack)
        {
            RefreshAll();
        }

        // ── 刷新 ──

        void RefreshAll()
        {
            RefreshFormImage();
            RefreshItems();
            RefreshSkillButtons();
            RefreshDetailPanel();
        }
    }
}
