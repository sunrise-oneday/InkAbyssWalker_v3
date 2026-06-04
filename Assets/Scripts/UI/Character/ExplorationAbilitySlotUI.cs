using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个探索技能槽的 UI 组件
/// 未解锁：显示"???"，隐藏图标
/// 已解锁：显示图标和名称，可启用/禁用
/// </summary>
public class ExplorationAbilitySlotUI : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private int abilityIndex;

    [Header("UI 引用")]
    [SerializeField] private Image icon;
    [SerializeField] private Image emptyFrame;
    [SerializeField] private Text nameLabel;
    [SerializeField] private Text lockText;
    [SerializeField] private Button button;
    [SerializeField] private GameObject selectedHighlight;
    [SerializeField] private Toggle enableToggle;

    [Header("视觉颜色")]
    [SerializeField] private Color lockedFrameColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private Color unlockedFrameColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private Color selectedColor = new Color(0.55f, 0.85f, 1f, 0.95f);

    private ExplorationAbility ability;
    private bool isUnlocked;
    private bool isSelected;
    private bool isEnabled = true;
    private string abilityName;
    private string abilityDesc;

    public ExplorationAbility Ability => ability;
    public bool IsUnlocked => isUnlocked;
    public bool IsEnabled => isEnabled;

    public event Action<ExplorationAbilitySlotUI> OnSlotClicked;
    public event Action<ExplorationAbilitySlotUI, bool> OnToggleChanged;

    private void Awake()
    {
        ability = (ExplorationAbility)abilityIndex;

        if (button != null)
            button.onClick.AddListener(HandleClick);

        if (enableToggle != null)
            enableToggle.onValueChanged.AddListener(OnToggleValueChanged);

        if (selectedHighlight != null)
            selectedHighlight.SetActive(false);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);

        if (enableToggle != null)
            enableToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }

    /// <summary>
    /// 绑定技能数据并刷新视觉
    /// </summary>
    public void Bind(ExplorationAbility ability, bool unlocked, Sprite iconSprite, string displayName, string description)
    {
        this.ability = ability;
        this.abilityIndex = (int)ability;
        this.isUnlocked = unlocked;
        this.abilityName = displayName;
        this.abilityDesc = description;

        if (icon != null)
        {
            icon.sprite = iconSprite;
            icon.enabled = unlocked && iconSprite != null;
        }

        if (nameLabel != null)
        {
            nameLabel.text = unlocked ? displayName : "";
        }

        if (lockText != null)
        {
            lockText.text = unlocked ? "" : "???";
        }

        if (enableToggle != null)
        {
            enableToggle.gameObject.SetActive(unlocked);
            enableToggle.isOn = isEnabled;
        }

        ApplyVisual();
    }

    /// <summary>
    /// 设置启用状态
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
        if (enableToggle != null)
            enableToggle.isOn = enabled;
        ApplyVisual();
    }

    /// <summary>
    /// 设置选中状态
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplyVisual();
    }

    /// <summary>
    /// 根据解锁状态和选中状态设置最终视觉
    /// </summary>
    private void ApplyVisual()
    {
        //if (emptyFrame != null)
        //{
        //    if (isSelected)
        //        emptyFrame.color = selectedColor;
        //    else if (isUnlocked)
        //        emptyFrame.color = unlockedFrameColor;
        //    else
        //        emptyFrame.color = lockedFrameColor;
        //}

        if (selectedHighlight != null)
            selectedHighlight.SetActive(isSelected);

        // 未启用时降低图标透明度
        if (icon != null && isUnlocked)
        {
            var c = icon.color;
            c.a = isEnabled ? 1f : 0.4f;
            icon.color = c;
        }
    }

    private void OnToggleValueChanged(bool value)
    {
        isEnabled = value;
        ApplyVisual();
        OnToggleChanged?.Invoke(this, value);
    }

    private void HandleClick()
    {
        OnSlotClicked?.Invoke(this);
    }
}
