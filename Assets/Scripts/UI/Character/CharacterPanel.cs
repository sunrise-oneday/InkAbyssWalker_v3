using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using StoreAndInventory;

/// <summary>
/// 角色面板
/// 继承 BasePanel，通过 UIManager 统一管理
/// 管理探索技能显示/启用切换、大招装备切换
/// </summary>
public class CharacterPanel : BasePanel
{
    [Header("UI 引用")]
    [SerializeField] private ExplorationAbilitySlotUI[] explorationSlots;
    [SerializeField] private Transform ultimateContainer;
    [SerializeField] private UltimateSlotUI ultimateSlotPrefab;
    [SerializeField] private SkillDetailPanel detailPanel;
    [SerializeField] private Button closeButton;

    [Header("配置")]
    [SerializeField] private AbilityDisplayConfig abilityDisplayConfig;

    private PlayerController playerController;
    private PlayerBattleEntity playerBattleEntity;
    private ExplorationAbilitySlotUI selectedAbilitySlot;
    private UltimateSlotUI selectedUltSlot;
    private readonly List<UltimateSlotUI> spawnedUltSlots = new List<UltimateSlotUI>();
    private readonly Dictionary<ExplorationAbility, bool> abilityEnabledState = new Dictionary<ExplorationAbility, bool>();
    private GameplayInputReader gameplayInput;
    private bool inputSubscribed;

    /// <summary>获取指定探索技能是否启用</summary>
    public bool IsAbilityEnabled(ExplorationAbility ability)
    {
        return abilityEnabledState.TryGetValue(ability, out bool enabled) && enabled;
    }

    // ============================================
    // 生命周期
    // ============================================

    private void Awake()
    {
        this.gameObject.SetActive(false);

        TrySubscribeInput();
    }

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    private void TrySubscribeInput()
    {
        if (inputSubscribed) return;

        if (gameplayInput == null)
            gameplayInput = InputManager.Instance?.Gameplay;

        if (gameplayInput != null)
        {
            gameplayInput.OnOpenCharacterPanelPressed += OnOpenCharacterPanelInput;
            inputSubscribed = true;
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[CharacterPanel] gameplayInput=null, InputManager.Instance={InputManager.Instance != null}</color>");
        }
    }

    private void Update()
    {
        if (!inputSubscribed)
            TrySubscribeInput();
    }

    private void OnDestroy()
    {
        if (gameplayInput != null)
        {
            gameplayInput.OnOpenCharacterPanelPressed -= OnOpenCharacterPanelInput;
        }

        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseButtonClicked);
    }

    private void OnOpenCharacterPanelInput()
    {

        if (!InputContextSwitcher.CanUseExploreInput()) return;

        if (IsOpen)
            UIManager.Instance.CloseCharacterPanel();
        else
            UIManager.Instance.OpenCharacterPanel();
    }

    private void OnCloseButtonClicked()
    {
        UIManager.Instance.CloseCharacterPanel();
    }

    // ============================================
    // BasePanel 生命周期
    // ============================================

    public override void OnOpen()
    {
        base.OnOpen();
        ResolvePlayerReferences();
        RefreshUI();
    }

    public override void OnClose()
    {
        base.OnClose();
        ClearSelection();
    }

    public override void OnRefresh()
    {
        base.OnRefresh();
        RefreshUI();
    }

    // ============================================
    // 数据刷新
    // ============================================

    private void RefreshUI()
    {
        RefreshExplorationSlots();
        RefreshUltimateSlots();

        if (detailPanel != null)
            detailPanel.ShowLocked();
    }

    private void RefreshExplorationSlots()
    {
        if (explorationSlots == null || playerController == null) return;

        // 从存档加载启用状态
        if (abilityEnabledState.Count == 0)
        {
            var saved = SaveManager.Instance.LoadAllAbilityEnabledStates();
            foreach (var kv in saved)
                abilityEnabledState[kv.Key] = kv.Value;
        }

        foreach (var slot in explorationSlots)
        {
            if (slot == null) continue;

            var ability = slot.Ability;
            bool unlocked = playerController.IsAbilityUnlocked(ability);
            var displayData = abilityDisplayConfig != null ? abilityDisplayConfig.GetEntry(ability) : null;

            // 初始化启用状态：从存档读取，默认启用
            if (!abilityEnabledState.ContainsKey(ability))
                abilityEnabledState[ability] = true;

            slot.Bind(
                ability,
                unlocked,
                displayData != null ? displayData.icon : null,
                displayData != null ? displayData.displayName : ability.ToString(),
                displayData != null ? displayData.description : ""
            );

            // 恢复启用状态并同步到 PlayerController
            bool enabled = abilityEnabledState[ability];
            if (unlocked)
            {
                slot.SetEnabled(enabled);
                playerController.SetAbilityEnabled(ability, enabled);
            }

            slot.OnSlotClicked -= OnAbilitySlotClicked;
            slot.OnSlotClicked += OnAbilitySlotClicked;

            slot.OnToggleChanged -= OnAbilityToggleChanged;
            slot.OnToggleChanged += OnAbilityToggleChanged;
        }
    }

    private void RefreshUltimateSlots()
    {
        foreach (var slot in spawnedUltSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        spawnedUltSlots.Clear();

        if (playerBattleEntity == null)
        {
            Debug.LogWarning("[角色面板] playerBattleEntity 为空，无法加载大招");
            return;
        }
        if (ultimateSlotPrefab == null)
        {
            Debug.LogWarning("[角色面板] ultimateSlotPrefab 未赋值");
            return;
        }
        if (ultimateContainer == null)
        {
            Debug.LogWarning("[角色面板] ultimateContainer 未赋值");
            return;
        }

        playerBattleEntity.LoadUnlockedUltimates();

        foreach (var skill in playerBattleEntity.unlockedUltimates)
        {
            var slot = Instantiate(ultimateSlotPrefab, ultimateContainer);
            bool isEquipped = playerBattleEntity.equippedUltimate != null
                && playerBattleEntity.equippedUltimate.ultimateName == skill.ultimateName;

            slot.Bind(skill, isEquipped);
            slot.OnSlotClicked += OnUltSlotClicked;
            spawnedUltSlots.Add(slot);
        }
    }

    // ============================================
    // 用户交互
    // ============================================

    private void OnAbilitySlotClicked(ExplorationAbilitySlotUI clickedSlot)
    {
        ClearSelection();
        selectedAbilitySlot = clickedSlot;
        clickedSlot.SetSelected(true);

        if (detailPanel != null)
        {
            var displayData = abilityDisplayConfig != null ? abilityDisplayConfig.GetEntry(clickedSlot.Ability) : null;
            detailPanel.ShowExplorationAbility(
                clickedSlot.Ability,
                clickedSlot.IsUnlocked,
                displayData != null ? displayData.icon : null,
                displayData != null ? displayData.displayName : clickedSlot.Ability.ToString(),
                displayData != null ? displayData.description : ""
            );
        }
    }

    private void OnAbilityToggleChanged(ExplorationAbilitySlotUI slot, bool enabled)
    {
        abilityEnabledState[slot.Ability] = enabled;

        // 同步到 PlayerController
        if (playerController != null)
            playerController.SetAbilityEnabled(slot.Ability, enabled);

        // 保存到存档
        SaveManager.Instance.SaveAbilityEnabled(slot.Ability, enabled);
    }

    private void OnUltSlotClicked(UltimateSlotUI clickedSlot)
    {
        ClearSelection();
        selectedUltSlot = clickedSlot;
        clickedSlot.SetSelected(true);

        if (detailPanel != null && playerBattleEntity != null)
        {
            bool isEquipped = playerBattleEntity.equippedUltimate != null
                && playerBattleEntity.equippedUltimate.ultimateName == clickedSlot.UltimateSkill.ultimateName;

            detailPanel.ShowUltimateSkill(clickedSlot.UltimateSkill, isEquipped);
        }
    }

    private void OnUltimateEquipped()
    {
        // 保存装备的大招
        if (playerBattleEntity != null && playerBattleEntity.equippedUltimate != null)
        {
            SaveManager.Instance.SaveEquippedUltimate(playerBattleEntity.equippedUltimate.ultimateName);
        }

        RefreshUltimateSlots();

        if (detailPanel != null && selectedUltSlot != null && playerBattleEntity != null)
        {
            bool isEquipped = playerBattleEntity.equippedUltimate != null
                && playerBattleEntity.equippedUltimate.ultimateName == selectedUltSlot.UltimateSkill.ultimateName;

            detailPanel.ShowUltimateSkill(selectedUltSlot.UltimateSkill, isEquipped);
        }
    }

    private void ClearSelection()
    {
        if (selectedAbilitySlot != null)
        {
            selectedAbilitySlot.SetSelected(false);
            selectedAbilitySlot = null;
        }

        if (selectedUltSlot != null)
        {
            selectedUltSlot.SetSelected(false);
            selectedUltSlot = null;
        }
    }

    // ============================================
    // 辅助方法
    // ============================================

    private void ResolvePlayerReferences()
    {
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if (playerController != null && playerBattleEntity == null)
            playerBattleEntity = playerController.GetComponent<PlayerBattleEntity>();

        if (detailPanel != null && playerBattleEntity != null)
            detailPanel.BindPlayerBattleEntity(playerBattleEntity);

        if (detailPanel != null)
            detailPanel.SetEquipCallback(OnUltimateEquipped);
    }
}
