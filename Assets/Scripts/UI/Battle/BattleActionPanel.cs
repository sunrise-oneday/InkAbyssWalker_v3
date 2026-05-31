using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
                trigger.SetTooltipData(form.formName, $"消耗: {form.apCostToSwitch} AP",
                    "切换为该形态后，将全自动刷新匹配该形态的技能控件。", 0, form.apCostToSwitch);

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

                var trigger = btn.GetComponent<UITooltipTrigger>();
                if (trigger == null) trigger = btn.gameObject.AddComponent<UITooltipTrigger>();
                trigger.SetTooltipData(skill.skillName, $"消耗: {skill.mpCost} MP",
                    $"{skill.baseDamage}点伤害，对目标造成 {skill.breakDamage}点白色削韧伤害。", skill.mpCost, 0);

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
            }
        }
    }
}
