using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using StoreAndInventory;

/// <summary>
/// 操作按钮面板：技能按钮、形态切换按钮、结束回合按钮
/// </summary>
public class BattleActionPanel : BasePanel
{
    [Header("操作面板 CanvasGroup")]
    [SerializeField] private CanvasGroup actionPanelGroup;

    [Header("按钮列表")]
    [SerializeField] private Button endTurnButton;
    [SerializeField] private List<Button> skillButtons;
    [SerializeField] private List<Button> formButtons;

    [Header("技能图标")]
    [SerializeField] private List<Image> skillIcons;           // 4 个技能图标 Image
    [SerializeField] private Sprite[] skillSprites;            // 12 个技能图标（按 SkillRuneSlotConfig 顺序）

    private PlayerBattleEntity mainPlayer;

    public void SetPlayer(PlayerBattleEntity player)
    {
        mainPlayer = player;
    }

    /// <summary>启用/禁用操作面板交互</summary>
    public void SetInteractable(bool active)
    {
        if (actionPanelGroup == null) return;

        if (active && !actionPanelGroup.gameObject.activeSelf)
            actionPanelGroup.gameObject.SetActive(true);

        actionPanelGroup.alpha = active ? 1.0f : 0.45f;
        actionPanelGroup.interactable = active;
        actionPanelGroup.blocksRaycasts = active;
    }

    /// <summary>绑定结束回合按钮</summary>
    public void BindEndTurnButton(System.Action onClick)
    {
        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveAllListeners();
            endTurnButton.onClick.AddListener(() => onClick?.Invoke());
        }
    }

    public override void OnRefresh()
    {
        SetupFormButtons();
        SetupSkillButtons();
    }

    private void SetupFormButtons()
    {
        if (mainPlayer == null) return;
        var forms = mainPlayer.availableForms;

        Debug.Log($"[BattleActionPanel] SetupFormButtons: 形态数量={forms?.Count ?? 0}");

        for (int i = 0; i < formButtons.Count; i++)
        {
            Button btn = formButtons[i];
            if (btn == null) continue;

            if (i < forms.Count)
            {
                btn.gameObject.SetActive(true);
                btn.interactable = true;
                PlayerForm form = forms[i];

                var btnText = btn.GetComponentInChildren<Text>();
                if (btnText != null)
                    btnText.text = $"{form.formName}\n({form.apCostToSwitch} AP)";

                var trigger = btn.GetComponent<UITooltipTrigger>();
                if (trigger == null) trigger = btn.gameObject.AddComponent<UITooltipTrigger>();
                // 使用形态的描述，如果没有则使用默认描述
                string formDesc = !string.IsNullOrEmpty(form.description)
                    ? form.description
                    : "切换为该形态后，将全自动刷新匹配该形态的技能控件。";
                trigger.SetTooltipData(form.formName, $"消耗: {form.apCostToSwitch} AP",
                    formDesc, 0, form.apCostToSwitch);

                Debug.Log($"[BattleActionPanel] 形态按钮 {i}: {form.formName}, 描述长度={formDesc.Length}");

                int index = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    mainPlayer.SwitchForm(index);
                    BattleUIController.Instance.RefreshUI();
                    BattleUIController.Instance.RefreshHUDs();
                });
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }

    private void SetupSkillButtons()
    {
        if (mainPlayer == null || mainPlayer.CurrentForm == null) return;

        var activeSkills = mainPlayer.CurrentForm.availableSkills;

        Debug.Log($"[BattleActionPanel] SetupSkillButtons: 当前形态={mainPlayer.CurrentForm.formName}, 技能数量={activeSkills?.Count ?? 0}");

        for (int i = 0; i < skillButtons.Count; i++)
        {
            Button btn = skillButtons[i];
            if (btn == null) continue;

            if (i < activeSkills.Count)
            {
                btn.gameObject.SetActive(true);
                btn.interactable = true;
                Skill skill = activeSkills[i];

                var btnText = btn.GetComponentInChildren<Text>();
                if (btnText != null)
                    btnText.text = $"{skill.skillName}\n({skill.mpCost} MP / {skill.breakDamage} 削韧)";

                // 设置技能图标
                if (skillIcons != null && i < skillIcons.Count && skillIcons[i] != null)
                {
                    var sprite = GetSkillSprite(skill.skillName);
                    skillIcons[i].sprite = sprite;
                    skillIcons[i].enabled = sprite != null;
                }

                var trigger = btn.GetComponent<UITooltipTrigger>();
                if (trigger == null) trigger = btn.gameObject.AddComponent<UITooltipTrigger>();
                // 使用技能的描述，如果没有则使用默认描述
                string skillDesc = !string.IsNullOrEmpty(skill.description)
                    ? skill.description
                    : $"{skill.baseDamage}点伤害，削韧 {skill.breakDamage}";

                // 护盾技能使用 SetBlockData 显示护盾预览
                int shieldValue = GetSkillShieldValue(skill.skillName);
                if (shieldValue > 0)
                {
                    trigger.SetBlockData(skill.skillName, $"消耗: {skill.mpCost} MP",
                        $"获得 {shieldValue} 护盾", shieldValue, skill.mpCost, 0);
                }
                else
                {
                    trigger.SetSkillData(skill.skillName, $"消耗: {skill.mpCost} MP",
                        skillDesc, skill, skill.mpCost, 0, skill.skillName);
                }

                Debug.Log($"[BattleActionPanel] 技能按钮 {i}: {skill.skillName}, 描述长度={skillDesc.Length}");

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    var target = BattleTurnManager.Instance.selectedEnemy;
                    if (target != null)
                    {
                        mainPlayer.CastSkill(skill, target);
                        BattleUIController.Instance.RefreshUI();
                        BattleUIController.Instance.RefreshHUDs();
                    }
                    else
                    {
                        Debug.LogWarning("[UI 提示] 请先点击选中场上的怪物作为技能目标！");
                    }
                });
            }
            else
            {
                btn.gameObject.SetActive(false);
                // 隐藏对应的图标
                if (skillIcons != null && i < skillIcons.Count && skillIcons[i] != null)
                    skillIcons[i].enabled = false;
            }
        }
    }

    /// <summary>根据技能名称获取护盾值（无护盾返回 0）。</summary>
    private int GetSkillShieldValue(string skillName)
    {
        return skillName switch
        {
            "墨壁" => 15,
            "墨甲" => 10,
            _ => 0
        };
    }

    /// <summary>根据技能名称获取对应的图标。</summary>
    private Sprite GetSkillSprite(string skillName)
    {
        if (string.IsNullOrEmpty(skillName) || skillSprites == null) return null;

        var allSlotIds = SkillRuneSlotConfig.AllSlotIds;
        for (var i = 0; i < allSlotIds.Length; i++)
        {
            if (allSlotIds[i] == skillName && i < skillSprites.Length)
                return skillSprites[i];
        }
        return null;
    }
}
