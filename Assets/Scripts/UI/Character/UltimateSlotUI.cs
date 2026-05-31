using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个大招槽的 UI 组件
/// 显示大招名称、类型、伤害描述，以及是否为当前装备状态
/// </summary>
public class UltimateSlotUI : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Image icon;
    [SerializeField] private Image equippedFrame;
    [SerializeField] private Text nameLabel;
    [SerializeField] private Text typeLabel;
    [SerializeField] private Text damageLabel;
    [SerializeField] private Button button;
    [SerializeField] private GameObject equippedBadge;

    [Header("视觉颜色")]
    [SerializeField] private Color normalFrameColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
    [SerializeField] private Color equippedFrameColor = new Color(1f, 0.85f, 0.2f, 0.95f);

    private UltimateSkill ultimateSkill;
    private bool isEquipped;

    public UltimateSkill UltimateSkill => ultimateSkill;
    public bool IsEquipped => isEquipped;

    public event Action<UltimateSlotUI> OnSlotClicked;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    /// <summary>
    /// 绑定大招数据并刷新视觉
    /// </summary>
    public void Bind(UltimateSkill skill, bool equipped)
    {
        ultimateSkill = skill;
        isEquipped = equipped;

        if (nameLabel != null)
            nameLabel.text = skill != null ? skill.ultimateName : "";

        if (typeLabel != null)
            typeLabel.text = skill != null ? GetUltimateTypeName(skill.ultimateType) : "";

        if (damageLabel != null)
            damageLabel.text = skill != null ? $"伤害: {skill.baseDamage} / 削韧: {skill.breakDamage}" : "";

        if (icon != null)
            icon.enabled = skill != null;

        ApplyVisual();
    }

    /// <summary>
    /// 设置选中状态
    /// </summary>
    public void SetSelected(bool selected)
    {
        // 大招槽的选中效果通过 equippedFrame 颜色变化实现
        // 如果需要额外的选中高亮，可以在这里扩展
    }

    /// <summary>
    /// 设置装备状态
    /// </summary>
    public void SetEquipped(bool equipped)
    {
        isEquipped = equipped;
        ApplyVisual();
    }

    /// <summary>
    /// 根据装备状态设置视觉
    /// </summary>
    private void ApplyVisual()
    {
        if (equippedFrame != null)
            equippedFrame.color = isEquipped ? equippedFrameColor : normalFrameColor;

        if (equippedBadge != null)
            equippedBadge.SetActive(isEquipped);
    }

    private void HandleClick()
    {
        OnSlotClicked?.Invoke(this);
    }

    private string GetUltimateTypeName(UltimateSkill.UltimateType type)
    {
        switch (type)
        {
            case UltimateSkill.UltimateType.SingleTarget: return "单体爆发";
            case UltimateSkill.UltimateType.AoE: return "群体轰炸";
            case UltimateSkill.UltimateType.Control: return "强力控制";
            default: return "未知";
        }
    }
}
