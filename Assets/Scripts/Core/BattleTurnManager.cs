using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 回合管理器
/// 管理战斗阶段切换、目标选择、回合推进逻辑。
/// playerParty 和 activeEnemies 由 BattleFlowController 在战斗开始时注入。
/// </summary>
public class BattleTurnManager : MonoBehaviour
{
    private static BattleTurnManager _instance;
    public static BattleTurnManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BattleTurnManager>();
                if (_instance == null)
                {
                    var go = new GameObject("[BattleTurnManager]");
                    _instance = go.AddComponent<BattleTurnManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ============================================
    // 外部注入引用（由 BattleFlowController 在 StartBattle 时设置）
    // ============================================
    public List<PlayerBattleEntity> playerParty { get; set; }
    public List<EnemyBattleEntity> activeEnemies { get; set; }

    // ============================================
    // 战斗阶段状态
    // ============================================
    [Header("当前战斗阶段")]
    public BattlePhase currentPhase = BattlePhase.None;
    public int currentTurn = 1;
    public int currentEnemyTurnIndex = 0;

    // ============================================
    // 目标选择
    // ============================================
    public EnemyBattleEntity selectedEnemy { get; private set; }

    // ============================================
    // 连段格挡 / 闪避标记
    // ============================================
    public bool allPerfectParriesInCurrentAttack { get; set; } = true;
    public bool hasRestoredDodgeApThisRound { get; set; } = false;

    /// <summary>当前行动的敌人</summary>
    public EnemyBattleEntity CurrentAttacker
    {
        get
        {
            if (currentEnemyTurnIndex >= 0 && currentEnemyTurnIndex < activeEnemies?.Count)
                return activeEnemies[currentEnemyTurnIndex];
            return null;
        }
    }

    /// <summary>重置回合状态到战斗初始值</summary>
    public void Reset()
    {
        currentPhase = BattlePhase.Setup;
        currentTurn = 1;
        currentEnemyTurnIndex = 0;
        selectedEnemy = null;
        allPerfectParriesInCurrentAttack = true;
        hasRestoredDodgeApThisRound = false;
    }

    // ============================================
    // Unity 生命周期
    // ============================================
    private void Update()
    {
        if (currentPhase != BattlePhase.PlayerTurn)
            return;
        HandleTargetSelection();
        CheckAndAutoSelectNextTarget();
    }

    // ============================================
    // 目标选择
    // ============================================

    /// <summary>鼠标点击场上的存活怪物即可选中</summary>
    private void HandleTargetSelection()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null)
        {
            EnemyBattleEntity clickedEnemy = hit.collider.GetComponentInParent<EnemyBattleEntity>();
            if (clickedEnemy != null && clickedEnemy.Stats.currentHP > 0)
                SelectTarget(clickedEnemy);
        }
    }

    /// <summary>当前目标死亡后自动切换到下一个存活目标</summary>
    private void CheckAndAutoSelectNextTarget()
    {
        if (selectedEnemy == null || selectedEnemy.Stats.currentHP > 0)
            return;

        EnemyBattleEntity nextTarget = null;
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && enemy.Stats.currentHP > 0)
            {
                nextTarget = enemy;
                break;
            }
        }

        if (nextTarget != null)
        {
            SelectTarget(nextTarget);
            Debug.Log($"[自动切敌] 原目标已死亡，已自动切换到下一个存活目标：{nextTarget.gameObject.name}");
        }
        else
        {
            selectedEnemy = null;
        }
    }

    /// <summary>选中指定目标，高亮并刷新 UI</summary>
    public void SelectTarget(EnemyBattleEntity target)
    {
        if (target == null) return;
        selectedEnemy = target;

        foreach (var enemy in activeEnemies)
            enemy?.SetSelected(enemy == selectedEnemy);

        BattleUIController.Instance?.RefreshUI();
    }

    // ============================================
    // 玩家回合
    // ============================================

    /// <summary>进入玩家回合，补充资源并激活操作面板</summary>
    public void EnterPlayerTurn()
    {
        currentPhase = BattlePhase.PlayerTurn;
        hasRestoredDodgeApThisRound = false;

        // 补充公共资源
        var res = BattleResourceManager.Instance;
        res.sharedAP = Mathf.Min(res.sharedAP + 2, res.maxSharedAP);
        res.sharedMP = Mathf.Min(res.sharedMP + 10, res.maxSharedMP);

        // 各队员回合开始
        foreach (var member in playerParty)
        {
            if (member == null) continue;
            member.currentAP = Mathf.Min(member.currentAP + 2, member.maxAP);
            Debug.Log($"[回合循环] 队员 {member.gameObject.name} 回合开始，当前 AP: {member.currentAP}");
            member.Stats.TickBuffs();
        }

        // UI
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.RefreshUI();
            BattleUIController.Instance.SetActionPanelActive(true);
        }

        Debug.Log($"[回合循环] 第 {currentTurn} 回合，玩家回合开始。");
    }

    // ============================================
    // 敌人回合
    // ============================================

    /// <summary>进入敌人回合</summary>
    public void EnterEnemyTurn()
    {
        StartCoroutine(EnterEnemyTurnRoutine());
    }

    private IEnumerator EnterEnemyTurnRoutine()
    {
        currentPhase = BattlePhase.EnemyTurn;
        Debug.Log("[回合循环] 敌方回合开始，准备实时格挡！");

        BattleUIController.Instance?.SetActionPanelActive(false);
        allPerfectParriesInCurrentAttack = true;

        yield return new WaitForSeconds(2f);

        if (currentEnemyTurnIndex < 0 || currentEnemyTurnIndex >= activeEnemies.Count)
            yield break;

        EnemyBattleEntity attacker = activeEnemies[currentEnemyTurnIndex];

        // 已死亡则跳过
        if (attacker == null || attacker.Stats.currentHP <= 0)
        {
            Debug.Log($"[状态判定] 敌方 {attacker?.gameObject.name} 已经阵亡，放弃其行动权");
            OnEnemyTurnFinished();
            yield break;
        }

        // 眩晕/破防跳过
        bool isStunned = attacker.Stats.activeBuffs.Exists(b => b is StunBuff);
        if (attacker.Stats.isBroken || isStunned)
        {
            Debug.Log($"<color=yellow>[行动跳过] {attacker.gameObject.name} 正处于眩晕/破防状态中，本回合无法行动</color>");
            yield return new WaitForSeconds(1.5f);
            OnEnemyTurnFinished();
            yield break;
        }

        // 根据玩家形态决定防御状态
        if (playerParty?.Count > 0 && playerParty[0] != null)
        {
            PlayerBattleEntity defender = playerParty[0];
            var fsm = defender.GetBattleStateMachine();

            if (defender.currentFormIndex == 1 || defender.currentFormIndex == 2)
                fsm.ChangeState<PlayerParryState>();
            else
            {
                fsm.ChangeState<PlayerBattleIdleState>();
                Debug.Log("<color=red>[战斗警告] 玩家当前在非防御形态下接敌，防御形态关闭，无法使用任何格挡技能！</color>");
            }
        }

        attacker.GetBattleStateMachine().ChangeState<EnemyBattleState>();
    }

    /// <summary>怪物攻击动画结束回调，处理反击/回合推进</summary>
    public void OnEnemyTurnFinished()
    {
        Debug.Log($"[回合循环] 敌方 {activeEnemies[currentEnemyTurnIndex].gameObject.name} 行动结束。");

        // 怪物存活时恢复破防值并推进 Buff
        var currentEnemy = activeEnemies[currentEnemyTurnIndex];
        if (currentEnemy != null && currentEnemy.Stats.currentHP > 0)
        {
            currentEnemy.Stats.RecoverFromBreak();
            currentEnemy.Stats.TickBuffs();
        }

        // 全部完美格挡则进入反击，否则直接推进回合
        if (allPerfectParriesInCurrentAttack && playerParty?.Count > 0)
        {
            var fsm = playerParty[0].GetBattleStateMachine();
            fsm?.ChangeState<PlayerCounterAttackState>();
        }
        else
        {
            playerParty?[0].GetBattleStateMachine()?.ChangeState<PlayerBattleIdleState>();
            ProceedEnemyTurn();
        }
    }

    /// <summary>推进敌人回合队列：下一只怪物或进入玩家回合</summary>
    public void ProceedEnemyTurn()
    {
        currentEnemyTurnIndex++;

        if (currentEnemyTurnIndex < activeEnemies.Count)
            EnterEnemyTurn();
        else
        {
            currentEnemyTurnIndex = 0;
            currentTurn++;
            EnterPlayerTurn();
        }
    }
}
