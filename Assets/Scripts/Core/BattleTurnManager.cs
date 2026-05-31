using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Battle.Enemy;

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

        // 各队员回合开始：触发回合开始效果（燃烧/中毒跳伤害），但不扣减回合数
        foreach (var member in playerParty)
        {
            if (member == null) continue;
            member.currentAP = Mathf.Min(member.currentAP + 2, member.maxAP);
            member.Stats.ProcTurnStartBuffs();
            Debug.Log($"[回合循环] 队员 {member.gameObject.name} 回合开始，当前 AP: {member.currentAP}");
        }

        // ===== 关键：在玩家回合开始时，为所有敌人决策意图 =====
        DecideAllEnemyIntents();

        // UI
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.RefreshUI();
            BattleUIController.Instance.SetActionPanelActive(true);
        }

        Debug.Log($"[回合循环] 第 {currentTurn} 回合，玩家回合开始。");
    }

    /// <summary>
    /// 为所有存活敌人决策下一个行动意图
    /// 在玩家回合开始时调用，让玩家提前看到敌人意图
    /// </summary>
    private void DecideAllEnemyIntents()
    {
        CharacterStats playerStats = (playerParty?.Count > 0 && playerParty[0] != null) ? playerParty[0].Stats : null;

        foreach (var enemy in activeEnemies)
        {
            if (enemy == null || enemy.Stats.currentHP <= 0) continue;

            EnemyAI ai = enemy.GetEnemyAI();
            if (ai != null)
            {
                // 如果没有配置行动列表，自动初始化默认行动
                if (ai.GetPossibleActions() == null || ai.GetPossibleActions().Length == 0)
                {
                    ai.InitializeDefaultActions();
                }

                // AI决策下一个行动
                EnemyIntent intent = ai.DecideNextAction(enemy.Stats, playerStats);

                // 部分敌人有概率显示未知意图（需要玩家使用分析技能揭示）
                if (Random.value < 0.3f)
                {
                    // 保存真实意图但显示为未知
                    EnemyIntent unknownIntent = EnemyIntent.CreateUnknown(null);
                    unknownIntent.action = intent.action; // 保存真实行动
                    enemy.SetCurrentIntent(unknownIntent);
                    Debug.Log($"[AI决策] {enemy.gameObject.name} 显示未知意图，需要分析技能揭示");
                }
                else
                {
                    enemy.SetCurrentIntent(intent);
                    Debug.Log($"[AI决策] {enemy.gameObject.name} 下一个行动：{intent?.type}，数值：{intent?.value}");
                }
            }
        }

        // 更新所有敌人的意图图标显示
        UpdateEnemyIntentUI();
    }

    // ============================================
    // 敌人回合
    // ============================================

    /// <summary>进入敌人回合</summary>
    public void EnterEnemyTurn()
    {
        // 玩家回合结束时，扣减玩家身上的 Buff 持续时间（效果已在回合开始时触发过）
        foreach (var member in playerParty)
        {
            if (member != null) member.Stats.TickBuffDurations();
        }

        // 如果是第一回合且没有决策过意图，先为所有敌人决策意图
        if (currentTurn == 1 && activeEnemies.Count > 0)
        {
            bool hasIntents = false;
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null && enemy.GetCurrentIntent() != null)
                {
                    hasIntents = true;
                    break;
                }
            }

            if (!hasIntents)
            {
                DecideAllEnemyIntents();
            }
        }

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

        // 使用玩家回合开始时决策的意图执行行动
        EnemyIntent intent = attacker.GetCurrentIntent();
        if (intent != null)
        {
            // 播放敌人对话
            EnemyBattleDialogue dialogue = attacker.GetComponent<EnemyBattleDialogue>();
            if (dialogue != null)
            {
                dialogue.PlayDialogueByIntent(intent.type);
            }

            Debug.Log($"[行动执行] {attacker.gameObject.name} 执行意图：{intent?.type}，数值：{intent?.value}");

            // 根据意图类型执行不同行动
            yield return ExecuteIntent(attacker, intent);
        }
        else
        {
            // 没有意图，使用默认攻击
            Debug.Log($"[默认行动] {attacker.gameObject.name} 没有意图，执行默认攻击");
            attacker.GetBattleStateMachine().ChangeState<EnemyBattleState>();
        }
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

        // 只有攻击类意图的完美格挡才触发反击，非攻击行动直接推进回合
        bool isAttackIntent = false;
        var activeAttacker = activeEnemies[currentEnemyTurnIndex];
        if (activeAttacker != null)
        {
            var intent = activeAttacker.GetCurrentIntent();
            if (intent != null)
            {
                isAttackIntent = intent.type == EnemyIntentType.Attack
                    || intent.type == EnemyIntentType.MultiAttack
                    || intent.type == EnemyIntentType.SpecialAttack;
            }
        }

        if (isAttackIntent && allPerfectParriesInCurrentAttack && playerParty?.Count > 0)
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

    // ============================================
    // AI意图执行
    // ============================================

    /// <summary>
    /// 根据意图类型执行行动
    /// </summary>
    private IEnumerator ExecuteIntent(EnemyBattleEntity attacker, EnemyIntent intent)
    {
        if (intent == null)
        {
            // 没有意图，执行默认攻击
            attacker.GetBattleStateMachine().ChangeState<EnemyBattleState>();
            yield break;
        }

        // 获取视觉效果组件
        EnemyVisualEffects vfx = attacker.GetComponent<EnemyVisualEffects>();

        // 如果是未知意图，执行其保存的真实行动
        EnemyIntentType actualType = intent.type;
        if (intent.type == EnemyIntentType.Unknown && intent.action != null)
        {
            actualType = intent.action.intentType;
            Debug.Log($"[行动执行] {attacker.gameObject.name} 执行未知意图，真实行动：{actualType}");
        }

        switch (actualType)
        {
            case EnemyIntentType.Attack:
            case EnemyIntentType.MultiAttack:
            case EnemyIntentType.SpecialAttack:
                // 攻击行动（包括普通攻击、多段攻击、特殊攻击）
                Debug.Log($"[行动执行] {attacker.gameObject.name} 执行攻击行动");
                attacker.GetBattleStateMachine().ChangeState<EnemyBattleState>();
                break;

            case EnemyIntentType.Block:
                // 格挡行动（获得护盾）
                int blockValue = intent.action != null ? intent.action.shieldAmount : intent.value;
                Debug.Log($"[行动执行] {attacker.gameObject.name} 执行格挡行动，获得 {blockValue} 护盾");
                attacker.ExecuteBlockAction();
                if (vfx != null) vfx.PlayBlockEffect();
                yield return new WaitForSeconds(1f);
                OnEnemyTurnFinished();
                break;

            case EnemyIntentType.DebuffPlayer:
                // Debuff行动（给玩家施加负面效果）
                Debug.Log($"[行动执行] {attacker.gameObject.name} 执行Debuff行动");
                if (playerParty?.Count > 0 && playerParty[0] != null)
                {
                    attacker.ExecuteDebuffAction(playerParty[0].Stats);
                }
                yield return new WaitForSeconds(1f);
                OnEnemyTurnFinished();
                break;

            case EnemyIntentType.BuffSelf:
            case EnemyIntentType.Strengthen:
                // 增益自己/强化行动
                Debug.Log($"[行动执行] {attacker.gameObject.name} 执行增益/强化行动");
                attacker.ExecuteStrengthenAction();
                if (vfx != null) vfx.PlayBuffEffect();
                yield return new WaitForSeconds(1f);
                OnEnemyTurnFinished();
                break;

            case EnemyIntentType.Heal:
                // 回血行动
                int healValue = intent.action != null ? intent.action.healAmount : intent.value;
                Debug.Log($"[行动执行] {attacker.gameObject.name} 执行回血行动，恢复 {healValue} HP");
                attacker.ExecuteHealAction();
                if (vfx != null) vfx.PlayHealEffect();
                yield return new WaitForSeconds(1f);
                OnEnemyTurnFinished();
                break;

            case EnemyIntentType.Summon:
                // 召唤行动
                Debug.Log($"[行动执行] {attacker.gameObject.name} 执行召唤行动");
                attacker.ExecuteSummonAction();
                yield return new WaitForSeconds(1f);
                OnEnemyTurnFinished();
                break;

            default:
                // 未知行动类型，执行默认攻击
                Debug.LogWarning($"[行动执行] 未知的行动类型：{intent.type}，执行默认攻击");
                attacker.GetBattleStateMachine().ChangeState<EnemyBattleState>();
                break;
        }
    }

    /// <summary>
    /// 更新所有敌人的意图图标显示
    /// </summary>
    private void UpdateEnemyIntentUI()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && enemy.Stats.currentHP > 0)
            {
                EntityHUD hud = enemy.GetComponentInChildren<EntityHUD>();
                if (hud != null)
                {
                    hud.RefreshIntent();
                }
            }
        }
    }
}
