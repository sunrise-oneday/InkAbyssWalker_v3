using UnityEngine;
using Battle.Enemy;

/// <summary>
/// 分析技能 - 揭示敌人的未知意图
/// 使用后可以看到敌人的真实意图
/// </summary>
[CreateAssetMenu(fileName = "AnalyzeSkill", menuName = "Battle/Skills/Analyze Skill")]
public class AnalyzeSkill : ScriptableObject
{
    [Header("技能信息")]
    public string skillName = "分析";
    public string description = "揭示目标敌人的下一个行动意图";
    public int mpCost = 10;
    public Sprite icon;

    [Header("效果设置")]
    public float revealDuration = -1; // -1 表示永久揭示，正数表示持续回合数

    /// <summary>
    /// 执行分析技能，揭示目标敌人的意图
    /// </summary>
    /// <param name="caster">施法者（玩家）</param>
    /// <param name="target">目标敌人</param>
    /// <returns>是否成功揭示</returns>
    public bool Execute(PlayerBattleEntity caster, EnemyBattleEntity target)
    {
        if (caster == null || target == null)
        {
            Debug.LogWarning("[分析技能] 施法者或目标为空");
            return false;
        }

        // 检查 MP 消耗
        if (caster.Stats.currentMP < mpCost)
        {
            Debug.Log($"[分析技能] MP 不足，需要 {mpCost}，当前 {caster.Stats.currentMP}");
            return false;
        }

        // 消耗 MP
        caster.Stats.currentMP -= mpCost;

        // 获取当前意图
        EnemyIntent currentIntent = target.GetCurrentIntent();
        if (currentIntent == null)
        {
            Debug.Log($"[分析技能] {target.gameObject.name} 没有意图");
            return false;
        }

        // 如果是未知意图，揭示它
        if (currentIntent.type == EnemyIntentType.Unknown)
        {
            // 获取敌人 AI 重新决策
            EnemyAI ai = target.GetEnemyAI();
            if (ai != null)
            {
                CharacterStats playerStats = caster.Stats;
                EnemyIntent revealedIntent = ai.DecideNextAction(target.Stats, playerStats);
                target.SetCurrentIntent(revealedIntent);

                Debug.Log($"[分析技能] 成功揭示 {target.gameObject.name} 的意图：{revealedIntent.type}");

                // 刷新 UI
                RefreshEnemyHUD(target);

                return true;
            }
        }
        else
        {
            Debug.Log($"[分析技能] {target.gameObject.name} 的意图已经是已知的：{currentIntent.type}");
        }

        return false;
    }

    /// <summary>
    /// 刷新敌人的 HUD 显示
    /// </summary>
    private void RefreshEnemyHUD(EnemyBattleEntity enemy)
    {
        EntityHUD hud = enemy.GetComponentInChildren<EntityHUD>();
        if (hud != null)
        {
            hud.RefreshIntent();
        }
    }
}
