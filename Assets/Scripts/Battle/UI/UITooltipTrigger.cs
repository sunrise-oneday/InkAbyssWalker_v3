using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using StoreAndInventory;

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
    private string linkedSkillId;        // 技能 ID（用于查找装备的符文）
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
        Skill skill, int reqMp = 0, int reqAp = 0, string skillId = null)
    {
        this.title = newTitle;
        this.cost = newCost;
        this.description = baseDesc;
        this.requiredMP = reqMp;
        this.requiredAP = reqAp;
        this.linkedSkill = skill;
        this.linkedSkillId = skillId;
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

            // 查找该技能装备的符文
            int runeAttackFlat = 0;
            float runeDamageBonusPct = 0f;
            Equipment equippedRune = null;
            if (!string.IsNullOrEmpty(linkedSkillId))
            {
                equippedRune = BattleStatSyncBridge.GetEquippedRuneForSkill(linkedSkillId);
                if (equippedRune != null)
                {
                    // 累加攻击加成
                    if (equippedRune.statMods != null)
                    {
                        for (int i = 0; i < equippedRune.statMods.Count; i++)
                        {
                            var mod = equippedRune.statMods[i];
                            if (mod.stat == StatType.Attack)
                                runeAttackFlat += Mathf.RoundToInt(mod.flat);
                        }
                    }
                    // 累加技能伤害加成
                    if (equippedRune.skillMods != null)
                    {
                        for (int i = 0; i < equippedRune.skillMods.Count; i++)
                        {
                            var mod = equippedRune.skillMods[i];
                            if (mod.modType == SkillModType.DamageBonus)
                                runeDamageBonusPct += mod.value;
                        }
                    }
                }
            }

            // 基础伤害信息
            string damageText = "";
            if (turnMgr != null && turnMgr.selectedEnemy != null && uiCtrl?.MainPlayer != null)
            {
                CharacterStats targetStats = turnMgr.selectedEnemy.Stats;

                if (targetStats != null)
                {
                    // 计算实际伤害：基础 + 符文攻击加成，再经 buff 链修正
                    int baseTotal = linkedSkill.baseDamage + runeAttackFlat;
                    int afterDealt = baseTotal;
                    CharacterStats playerStats = uiCtrl.MainPlayer.Stats;
                    for (int i = playerStats.activeBuffs.Count - 1; i >= 0; i--)
                    {
                        afterDealt = playerStats.activeBuffs[i].OnBeforeDealDamage(afterDealt);
                    }
                    int previewDamage = targetStats.PreviewEffectiveDamage(afterDealt);

                    // 只在实际有 buff 影响时才变色，否则显示白字基准
                    if (previewDamage > baseTotal)
                    {
                        string pctStr = baseTotal > 0 ? $" (<color=green>+{Mathf.RoundToInt((float)(previewDamage - baseTotal) / baseTotal * 100f)}%</color>)" : "";
                        damageText = $"<color=green>{previewDamage}</color> 点伤害{pctStr}";
                    }
                    else if (previewDamage < baseTotal && previewDamage > 0)
                    {
                        string pctStr = baseTotal > 0 ? $" (<color=red>-{Mathf.RoundToInt((float)(baseTotal - previewDamage) / baseTotal * 100f)}%</color>)" : "";
                        damageText = $"<color=red>{previewDamage}</color> 点伤害{pctStr}";
                    }
                    else
                    {
                        damageText = $"{baseTotal} 点伤害";
                    }

                    damageText += $"，削韧 {linkedSkill.breakDamage}";
                }
            }
            else
            {
                // 没有选中目标时显示基础伤害
                int baseTotal = linkedSkill.baseDamage + runeAttackFlat;
                damageText = $"{baseTotal} 点伤害，削韧 {linkedSkill.breakDamage}";
            }

            // 元素附着信息
            string elementText = "";
            if (linkedSkill.applyElement != ElementType.None)
            {
                string elementName = "";
                string reactionInfo = "";

                switch (linkedSkill.applyElement)
                {
                    case ElementType.Fire:
                        elementName = "火";
                        reactionInfo = "触发燃烧(火+水)/融化(火+冰)";
                        break;
                    case ElementType.Ice:
                        elementName = "冰";
                        reactionInfo = "触发冻结(冰+水)/融化(冰+火)";
                        break;
                    case ElementType.Water:
                        elementName = "水";
                        reactionInfo = "触发蒸发(水+火)/燃烧(火+水)";
                        break;
                }

                elementText = $"\n<color=#FFD700>附着：{elementName}元素 {linkedSkill.auraStacks}层</color>";
                elementText += $"\n<color=#87CEEB>反应：{reactionInfo}</color>";
                if (linkedSkill.ultChargeValue > 0)
                    elementText += $"\n<color=#90EE90>充能：终结技+{linkedSkill.ultChargeValue}</color>";
            }
            else if (linkedSkill.ultChargeValue > 0)
            {
                // 无元素技能也显示充能（仅当有充能值时）
                elementText = $"\n<color=#90EE90>充能：终结技+{linkedSkill.ultChargeValue}</color>";
            }

            // 符文加成信息
            string runeText = "";
            if (equippedRune != null)
            {
                var parts = new System.Collections.Generic.List<string>();
                if (runeAttackFlat > 0) parts.Add($"攻击+{runeAttackFlat}");
                if (runeDamageBonusPct > 0f) parts.Add($"伤害+{Mathf.RoundToInt(runeDamageBonusPct * 100)}%");
                if (parts.Count > 0)
                    runeText = $"\n<color=#FFD700>符文加成: {string.Join(", ", parts)}</color>";
            }

            result = damageText + elementText + runeText;
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