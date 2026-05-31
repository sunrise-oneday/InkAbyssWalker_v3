using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 万能 UGUI 提示触发器（支持：MP/AP 资源消耗传递判定 + 动态伤害/护盾预览） [1]
/// </summary>
public class UITooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("描述面板配置")]
    [SerializeField] private string title;
    [SerializeField] private string cost;
    [TextArea(3, 5)]
    [SerializeField] private string description;

    // 暂存该技能/形态实际需要的资源，用于大面板动态检测红字 [2, 3]
    private int requiredMP;
    private int requiredAP;

    // 动态数值预览相关（技能伤害/护盾预览）
    private Skill linkedSkill;           // 关联的技能（用于动态伤害计算）
    private int blockAmount;             // 护盾值（用于格挡类技能预览）
    private bool isBlockSkill;           // 是否为格挡类技能

    /// <summary>
    /// 提供给 UI 动态生成器：在运行时一键注入该卡牌的数据，并带入消耗数值 [1, 2]
    /// </summary>
    public void SetTooltipData(string newTitle, string newCost, string newDesc, int reqMp = 0, int reqAp = 0)
    {
        this.title = newTitle;
        this.cost = newCost;
        this.description = newDesc;
        this.requiredMP = reqMp;
        this.requiredAP = reqAp;
        this.linkedSkill = null;
        this.blockAmount = 0;
        this.isBlockSkill = false;
    }

    /// <summary>
    /// 设置技能关联数据（启用动态伤害预览）
    /// </summary>
    public void SetSkillData(string newTitle, string newCost, string baseDesc,
        Skill skill, int reqMp = 0, int reqAp = 0)
    {
        this.title = newTitle;
        this.cost = newCost;
        this.description = baseDesc;
        this.requiredMP = reqMp;
        this.requiredAP = reqAp;
        this.linkedSkill = skill;
        this.blockAmount = 0;
        this.isBlockSkill = false;
    }

    /// <summary>
    /// 设置格挡类技能数据（启用动态护盾预览）
    /// </summary>
    public void SetBlockData(string newTitle, string newCost, string baseDesc,
        int shieldValue, int reqMp = 0, int reqAp = 0)
    {
        this.title = newTitle;
        this.cost = newCost;
        this.description = baseDesc;
        this.requiredMP = reqMp;
        this.requiredAP = reqAp;
        this.linkedSkill = null;
        this.blockAmount = shieldValue;
        this.isBlockSkill = true;
    }

    /// <summary>
    /// 构建动态描述文本（含颜色标记）
    /// </summary>
    private string BuildDynamicDescription()
    {
        string result = description;

        // 技能类：动态伤害预览
        if (linkedSkill != null && !isBlockSkill)
        {
            var turnMgr = BattleTurnManager.Instance;
            var uiCtrl = BattleUIController.Instance;

            if (turnMgr != null && turnMgr.selectedEnemy != null && uiCtrl?.MainPlayer != null)
            {
                CharacterStats targetStats = turnMgr.selectedEnemy.Stats;

                if (targetStats != null)
                {
                    // 计算实际伤害（只反映目标 buff 链的修正，不包含攻击力 stat 和防御）
                    int previewDamage = targetStats.PreviewEffectiveDamage(linkedSkill.baseDamage);
                    int baseTotal = linkedSkill.baseDamage;

                    // 只在实际有 buff 影响时才变色，否则显示白字基准
                    if (previewDamage > baseTotal)
                    {
                        string pctStr = baseTotal > 0 ? $" (<color=green>+{Mathf.RoundToInt((float)(previewDamage - baseTotal) / baseTotal * 100f)}%</color>)" : "";
                        result = $"<color=green>{previewDamage}</color> 点伤害{pctStr}";
                    }
                    else if (previewDamage < baseTotal && previewDamage > 0)
                    {
                        string pctStr = baseTotal > 0 ? $" (<color=red>-{Mathf.RoundToInt((float)(baseTotal - previewDamage) / baseTotal * 100f)}%</color>)" : "";
                        result = $"<color=red>{previewDamage}</color> 点伤害{pctStr}";
                    }
                    else
                    {
                        result = $"{baseTotal} 点伤害";
                    }

                    result += $"，削韧 {linkedSkill.breakDamage}";
                }
            }
            else
            {
                // 没有选中目标时显示基础伤害
                result = $"{linkedSkill.baseDamage} 点伤害，削韧 {linkedSkill.breakDamage}";
            }
        }

        // 格挡类技能：动态护盾预览
        if (isBlockSkill && blockAmount > 0)
        {
            var uiCtrl = BattleUIController.Instance;
            if (uiCtrl?.MainPlayer != null)
            {
                CharacterStats playerStats = uiCtrl.MainPlayer.Stats;
                int previewBlock = playerStats.PreviewBlockGain(blockAmount);

                if (previewBlock < blockAmount)
                {
                    int diff = blockAmount - previewBlock;
                    float pct = (float)diff / blockAmount * 100f;
                    result = $"获得 <color=red>{previewBlock}</color> 护盾 (<color=red>-{Mathf.RoundToInt(pct)}%</color>)";
                }
                else
                {
                    result = $"获得 {previewBlock} 护盾";
                }
            }
        }

        return result;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (BattleUIController.Instance != null && !string.IsNullOrEmpty(title))
        {
            // 核心修改：动态构建描述文本（包含实时伤害/护盾预览）
            string dynamicDesc = BuildDynamicDescription();
            BattleUIController.Instance.ShowFixedTooltip(title, cost, dynamicDesc, requiredMP, requiredAP);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.HideFixedTooltip();
        }
    }

    private void OnDisable()
    {
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.HideFixedTooltip();
        }
    }
}