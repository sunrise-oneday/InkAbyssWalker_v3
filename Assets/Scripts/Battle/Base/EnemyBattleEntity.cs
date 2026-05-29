using System.Collections;
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

    [Header("死亡可选项 [5]")]
    [Tooltip("勾选后，怪物死亡播放完动画 2 秒后会自动渐隐消失；不勾选则永远留在场上")]
    public bool destroyOnDeath = false; // <--- 核心补齐：必须加上这一行！

    private void OnEnable()
    {
        // ========================================================
        // 核心重构（观察者模式）：监听自己身上的 Buff 改变事件！ [1]
        // ========================================================
        if (Stats != null)
        {
            Stats.OnBuffsChanged += HandleBuffsChanged;
            Stats.OnBreakChanged += HandleBreakChanged; // 核心新增：监听白色破防条改变！ [5]
        }
    }

    private void OnDisable()
    {
        if (Stats != null)
        {
            Stats.OnBuffsChanged -= HandleBuffsChanged;
            Stats.OnBreakChanged -= HandleBreakChanged;
        }
    }

    /// <summary>
    /// 核心重构（观察者模式）：
    /// 当白色破防条（Break Bar）发生改变时，怪物自主调整自己的动作状态！ [5]
    /// </summary>
    private void HandleBreakChanged()
    {
        if (Stats == null || battleStateMachine == null) return;

        if (Stats.isBroken)
        {
            // 如果自己被玩家打至“破防”（白色进度条归零），
            // 在玩家的回合内，身体瞬间切入【战斗眩晕状态（EnemyBattleStunState）】！ [5]
            if (!(battleStateMachine.currentState is EnemyBattleStunState))
            {
                battleStateMachine.ChangeState<EnemyBattleStunState>();
                Debug.Log($"<color=red>[物理反馈] {gameObject.name} 破防条被打空！身体瞬间进入眩晕！</color>");
            }
        }
        else
        {
            // 如果破防恢复了（通常在它的回合结束、RecoverFromBreak 被调用时触发）
            if (battleStateMachine.currentState is EnemyBattleStunState)
            {
                // 细节防错：必须确保身上此时也没有其他“眩晕 Buff”挂着，才允许恢复到正常的战斗待机！
                bool hasStun = Stats.activeBuffs.Exists(b => b is StunBuff);
                if (!hasStun)
                {
                    battleStateMachine.ChangeState<EnemyBattleIdleState>();
                    Debug.Log($"[物理反馈] {gameObject.name} 破防恢复，重回正常战斗待机动作。");
                }
            }
        }
    }

    /// <summary>
    /// 核心重构（观察者模式）：
    /// 当 Buff 发生改变时，怪物自主调整自己的动作状态！
    /// </summary>
    private void HandleBuffsChanged()
    {
        if (Stats == null || battleStateMachine == null) return;

        bool hasStun = Stats.activeBuffs.Exists(b => b is StunBuff);

        if (hasStun)
        {
            // 中了眩晕，瞬间进入眩晕抱头动作
            if (!(battleStateMachine.currentState is EnemyBattleStunState))
            {
                battleStateMachine.ChangeState<EnemyBattleStunState>();
            }
        }
        else
        {
            // 眩晕时间结束消失了：
            if (battleStateMachine.currentState is EnemyBattleStunState)
            {
                // 细节防错：只有在身上【既没有眩晕 Buff】，【也没有处于破防状态】时，才恢复待机！
                // 这能完美避免“眩晕 Buff 消失了，但怪依然在破防状态下，结果怪自动站起来”的严重逻辑 Bug！ [5]
                if (!Stats.isBroken)
                {
                    battleStateMachine.ChangeState<EnemyBattleIdleState>();
                }
            }
        }
    }

    protected override void Awake()
    {
        base.Awake(); // 自动获取 stats, anim, rb

        // 注册怪物自己的战斗状态
        battleStateMachine = new StateMachine<EnemyBattleEntity>(this);
        battleStateMachine.RegisterState(new EnemyBattleIdleState());
        battleStateMachine.RegisterState(new EnemyBattleState());
        battleStateMachine.RegisterState(new EnemyBattleStunState());
        battleStateMachine.RegisterState(new EnemyBattleDieState());  // 死亡


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
        if (anim != null && HitAnimHashes != null && nextIndex < HitAnimHashes.Length)
        {
            // 直接以 0.1s 极速融接下一斩，动作表现丝滑流畅！ [2]
            anim.CrossFade(HitAnimHashes[nextIndex], 0.1f);
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

    /// <summary>
    /// 动画事件：【时机闪红警告】！在伤害落点前的 0.2 ~ 0.25 秒处的帧上，右键添加该事件。
    /// 调用后，怪物身上会闪烁刺眼的红色光芒，作为玩家按下空格键的“视觉哨兵”！
    /// </summary>
    public void TriggerParryIndicator()
    {
        StartCoroutine(FlashColorRoutine(Color.red, 0.15f));
    }

    // ========================================================
    // 物理/视觉表现通用协程
    // ========================================================
    private IEnumerator FlashColorRoutine(Color color, float duration)
    {
        if (sprite != null)
        {
            sprite.color = color;            
            yield return new WaitForSeconds(duration);
            sprite.color = Color.white; // 恢复正常白色
        }
    }

    /// <summary>
    /// 重写死亡方法，使怪物切入死亡状态并禁用物理碰撞 [5]
    /// </summary>
    protected override void Die()
    {
        battleStateMachine.ChangeState<EnemyBattleDieState>();
    }
}