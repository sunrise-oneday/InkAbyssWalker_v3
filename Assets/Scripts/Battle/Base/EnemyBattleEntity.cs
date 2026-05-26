using UnityEngine;

public class EnemyBattleEntity : BattleEntity
{
    [Header("敌人多段连击配置")]
    [SerializeField] private EnemyAttackSequence attackSequence;

    // ========================================================
    // 核心重构：声明并运行属于怪物自己的【战斗状态机】！
    // ========================================================
    private StateMachine<EnemyBattleEntity> battleStateMachine;

    // ========================================================
    // 核心重构：一次性缓存怪物的变招 Hash [2]
    // ========================================================
    public int[] HitAnimHashes { get; private set; }

    protected override void Awake()
    {
        base.Awake(); // 自动获取 stats, anim, rb

        // 注册怪物自己的战斗状态
        battleStateMachine = new StateMachine<EnemyBattleEntity>(this);
        battleStateMachine.RegisterState(new EnemyBattleIdleState());
        battleStateMachine.RegisterState(new EnemyBattleState());

        battleStateMachine.ChangeState<EnemyBattleIdleState>(); // 默认待机

        // 运行时一次性将名字转换为 Hash 缓存 [2]
        if (attackSequence != null && attackSequence.hitAnimations != null)
        {
            HitAnimHashes = new int[attackSequence.hitAnimations.Length];
            for (int i = 0; i < attackSequence.hitAnimations.Length; i++)
            {
                HitAnimHashes[i] = Animator.StringToHash(attackSequence.hitAnimations[i]);
            }
        }
    }

    // ========================================================
    // 核心补齐：必须在 Update 和 FixedUpdate 里驱动怪物的战斗状态机！
    // 漏掉这两个方法，怪物的状态切换后就会原地“断电冰冻”，无法倒计时和结束回合！ [2]
    // ========================================================
    private void Update()
    {
        if (battleStateMachine != null)
        {
            battleStateMachine.Update(); // 驱动怪物状态机工作！ [2]
        }
    }

    private void FixedUpdate()
    {
        if (battleStateMachine != null)
        {
            battleStateMachine.FixedUpdate();
        }
    }

    // 提供接口供外部切换状态
    public StateMachine<EnemyBattleEntity> GetBattleStateMachine() => battleStateMachine;
    public EnemyAttackSequence GetAttackSequence() => attackSequence;

    // ========================================================
    // 核心新增：接收动画事件，全自动完成攻击变招与伤害落点！ [1]
    // ========================================================

    /// <summary>
    /// 动画事件：刀光伤害落点时触发（在动画 timeline 判定帧右键添加） [1]
    /// </summary>
    public void TriggerDamage(int hitIndex)
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.EvaluateParryAndApplyDamage(hitIndex, attackSequence);
        }
    }

    /// <summary>
    /// 动画事件：招式变招，在动作即将结束前的一帧上右键添加，自动平滑切入下一斩！ [1, 2]
    /// </summary>
    /// <param name="nextIndex">下一斩的索引（例如：第一斩末尾事件传 1，第二斩末尾事件传 2）</param>
    public void TriggerNextAttack(int nextIndex)
    {
        if (Anim != null && HitAnimHashes != null && nextIndex < HitAnimHashes.Length)
        {
            // 直接以 0.1s 极速融接下一斩，动作表现丝滑流畅！ [2]
            Anim.CrossFade(HitAnimHashes[nextIndex], 0.1f);
            Debug.Log($"[敌人出招] 动画事件触发！变招切入第 {nextIndex + 1} 击: {attackSequence.hitAnimations[nextIndex]}");
        }
    }

    /// <summary>
    /// 动画事件：最后一击（Enemy_Attack_3）的最后一帧打上此事件，代表大招收招完毕 [1]
    /// </summary>
    public void TriggerAttackFinished()
    {
        // 自动回到待机，并将控制权安全还给玩家 [3]
        battleStateMachine.ChangeState<EnemyBattleIdleState>();

        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnEnemyTurnFinished();
        }
    }

    /// <summary>
    /// 被玩家点击选中时，命令头顶的血条 HUD 进行高亮和放大！
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        EntityHUD hud = GetComponentInChildren<EntityHUD>();
        if (hud != null)
        {
            hud.SetSelected(isSelected);
        }
    }
}