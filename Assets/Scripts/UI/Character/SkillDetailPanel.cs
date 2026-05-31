using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 共享的技能详情展示面板
/// 点击探索技能槽或大招槽时显示详细信息
/// </summary>
public class SkillDetailPanel : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text statsText;
    [SerializeField] private Image previewIcon;
    [SerializeField] private Button equipButton;
    [SerializeField] private GameObject equipButtonRoot;

    private UltimateSkill selectedUltimate;
    private PlayerBattleEntity boundPlayerBattleEntity;
    private Action onEquipCallback;

    private void Awake()
    {
        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipClicked);
    }

    private void OnDestroy()
    {
        if (equipButton != null)
            equipButton.onClick.RemoveListener(OnEquipClicked);
    }

    /// <summary>
    /// 绑定 PlayerBattleEntity 引用（用于装备大招）
    /// </summary>
    public void BindPlayerBattleEntity(PlayerBattleEntity entity)
    {
        boundPlayerBattleEntity = entity;
    }

    /// <summary>
    /// 设置装备回调（装备成功后刷新列表）
    /// </summary>
    public void SetEquipCallback(Action callback)
    {
        onEquipCallback = callback;
    }

    /// <summary>
    /// 显示探索技能详情
    /// </summary>
    public void ShowExplorationAbility(ExplorationAbility ability, bool unlocked, Sprite icon, string name, string desc)
    {
        selectedUltimate = null;

        if (titleText != null)
            titleText.text = unlocked ? name : "???";

        if (descriptionText != null)
            descriptionText.text = unlocked ? desc : "技能尚未解锁";

        if (statsText != null)
            statsText.text = "";

        if (previewIcon != null)
        {
            previewIcon.sprite = icon;
            previewIcon.enabled = unlocked && icon != null;
        }

        HideEquipButton();
    }

    /// <summary>
    /// 显示大招详情
    /// </summary>
    public void ShowUltimateSkill(UltimateSkill skill, bool isEquipped)
    {
        selectedUltimate = skill;

        if (titleText != null)
            titleText.text = skill != null ? skill.ultimateName : "";

        if (descriptionText != null)
            descriptionText.text = skill != null ? skill.description : "";

        if (statsText != null && skill != null)
        {
            string typeName = GetUltimateTypeName(skill.ultimateType);
            statsText.text = $"类型: {typeName}\n伤害: {skill.baseDamage}\n削韧: {skill.breakDamage}";
            if (skill.ultimateType == UltimateSkill.UltimateType.Control)
                statsText.text += $"\n眩晕: {skill.stunTurns} 回合";
        }

        if (previewIcon != null)
            previewIcon.enabled = false;

        // 显示装备按钮（只在未装备时显示）
        if (equipButtonRoot != null)
            equipButtonRoot.SetActive(!isEquipped && skill != null);
    }

    /// <summary>
    /// 显示锁定状态
    /// </summary>
    public void ShowLocked()
    {
        selectedUltimate = null;

        if (titleText != null)
            titleText.text = "选择技能查看详情";

        if (descriptionText != null)
            descriptionText.text = "";

        if (statsText != null)
            statsText.text = "";

        if (previewIcon != null)
            previewIcon.enabled = false;

        HideEquipButton();
    }

    private void HideEquipButton()
    {
        if (equipButtonRoot != null)
            equipButtonRoot.SetActive(false);
    }

    private void OnEquipClicked()
    {
        if (selectedUltimate == null || boundPlayerBattleEntity == null) return;

        boundPlayerBattleEntity.EquipUltimate(selectedUltimate);
        onEquipCallback?.Invoke();

        // 装备后隐藏按钮
        HideEquipButton();
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
