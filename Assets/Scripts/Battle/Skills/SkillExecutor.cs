using UnityEngine;
using Battle.Enemy;

/// <summary>
/// 技能执行器 - 管理战斗中技能的使用
/// </summary>
public class SkillExecutor : MonoBehaviour
{
    public static SkillExecutor Instance { get; private set; }

    [Header("可用技能")]
    [SerializeField] private AnalyzeSkill analyzeSkill;

    [Header("UI 引用")]
    [SerializeField] private BattleUIController battleUI;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 使用分析技能
    /// </summary>
    public bool UseAnalyzeSkill()
    {
        // 获取当前选中的敌人
        EnemyBattleEntity target = BattleTurnManager.Instance?.selectedEnemy;
        if (target == null)
        {
            Debug.Log("[技能系统] 没有选中目标");
            return false;
        }

        // 获取玩家
        PlayerBattleEntity player = GetPlayer();
        if (player == null)
        {
            Debug.Log("[技能系统] 找不到玩家");
            return false;
        }

        // 执行分析技能
        if (analyzeSkill != null)
        {
            return analyzeSkill.Execute(player, target);
        }

        Debug.LogWarning("[技能系统] 分析技能未配置");
        return false;
    }

    /// <summary>
    /// 获取玩家实体
    /// </summary>
    private PlayerBattleEntity GetPlayer()
    {
        if (BattleTurnManager.Instance?.playerParty?.Count > 0)
        {
            return BattleTurnManager.Instance.playerParty[0];
        }
        return null;
    }
}
