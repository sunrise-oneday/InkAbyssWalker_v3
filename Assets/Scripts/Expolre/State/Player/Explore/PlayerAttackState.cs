using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttackState : PlayerState
{
    // 核心解耦：进入状态时，自动获取当前连招步数对应的动画 Hash
    protected override int AnimHash => owner.AttackAnimHashes[owner.CurrentComboIndex];

    private bool wantsNextCombo;
    private bool hasCheckedHit; // 防止一次挥刀在多帧内重复判定击中

    public override void Enter()
    {
        base.Enter(); // 全自动播放当前连招动画

        // ========================================================
        // 关键修复：一旦进入攻击状态，立刻把控制器的攻击输入标记重置为 false！
        // 这样挥完刀后，状态机就不会因为旧的残留信号而自动重新攻击。
        // ========================================================
        owner.UseAttackInput(); 
        wantsNextCombo = false;
        hasCheckedHit = false; // <--- 核心新增：每次挥刀开始时重置标记

        // 攻击开始时，清除水平速度防止滑行（也可以保留一丁点位移作为前冲，在此可自由微调）
        owner.SetHorizontalVelocity(0f);
    }

    public override void Update()
    {
        base.Update();

        // 1. 输入预录：在挥刀期间，只要玩家再次按下攻击键，暂存“下一击”请求
        if (owner.AttackInputBuffered)
        {
            owner.UseAttackInput();// 消费输入，避免残留 
            wantsNextCombo = true;
        }

        // ========================================================
        // 核心修改：读取从大地图 PlayerController 统一配置并暴露的物理参数
        // 这样不仅支持随时调参，还支持在 Scene 视图里实时看到青色的攻击判定圈
        // ========================================================
        if (stateTimer >= owner.overworldAttackHitTime && !hasCheckedHit)
        {
            hasCheckedHit = true;

            // 计算玩家正前方的探测起点（从面板属性动态读取 range）
            Vector2 checkPos = (Vector2)owner.transform.position + new Vector2(owner.FacingDirection * owner.overworldAttackRange, -0.2f);

            // 探测前方圆形范围内有没有怪（从面板属性动态读取 radius）
            Collider2D hitCollider = Physics2D.OverlapCircle(checkPos, owner.overworldAttackRadius, LayerMask.GetMask("Enemy"));

            if (hitCollider != null)
            {
                OverworldEnemy enemy = hitCollider.GetComponentInParent<OverworldEnemy>();
                if (enemy != null)
                {
                    Debug.Log("<color=green>[主动偷袭！] 玩家成功抢先挥刀击中了怪物！进入先制战斗！</color>");
                    enemy.OnHitByOverworldAttack(isPreemptive: true);
                    return;
                }
            }
        }

        // ========================================================
        // 核心新增：主动取消后摇（Jump Cancel）
        // 假设攻击开始 0.15 秒后，刀光伤害判定已经发生，
        // 此时只要玩家按下跳跃键，就可以强行“掐断”当前攻击，重置连击并直接跳起！
        // ========================================================
        if (stateTimer > 0.15f)
        {
            if (owner.JumpInputBuffered)
            {
                // ========================================================
                // 核心修复：在这里打断切换状态时，必须立刻将跳跃输入消费掉！
                // 否则这个信号在空中会一直保持为 true，导致落地时再次起跳！ [2]
                // ========================================================
                owner.UseJumpInput();

                owner.CurrentComboIndex = 0; // 掐断时，重置连击
                stateMachine.ChangeState<PlayerJumpState>(); // 强行切入跳跃状态，中断攻击动画
                return;
            }

            // 提示：如果你以后做了闪避/冲刺动作，也可以在这里写：
            if (owner.DashInputBuffered)
            {
                owner.UseDashInput();
                stateMachine.ChangeState<PlayerDashState>(); 
                return; 
            }
        }

        // 获取当前这一击应该持续的时间
        float currentAttackDuration = owner.AttackDurations[owner.CurrentComboIndex];

        // 2. 当前这一斩的挥刀时间结束，进行结算
        if (stateTimer >= currentAttackDuration)
        {
            if (wantsNextCombo)
            {
                // 狂按按键：立刻进入下一击
                owner.CurrentComboIndex = (owner.CurrentComboIndex + 1) % owner.MaxComboCount;
                stateMachine.ChangeState<PlayerAttackState>();
            }
            else
            {
                // 核心修复：即使动画自然播完退回 Idle，我们也把连击步数“准备好”指向下一击！
                // 如果玩家在接下来的 1 秒内按键，就能顺利打出第二斩；如果超时，GroundedState 会自动清零。
                owner.CurrentComboIndex = (owner.CurrentComboIndex + 1) % owner.MaxComboCount;
                stateMachine.ChangeState<PlayerIdleState>();
            }
            
        }
    }

    public override void Exit()
    {
        base.Exit();
        // 记录这一次连招结束的时间戳，用于下一次按键判断是否超时
        owner.LastAttackTime = Time.time;
    }
}