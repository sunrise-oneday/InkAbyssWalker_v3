using System.Collections;
using UnityEngine;

/// <summary>
/// 冲刺状态
/// </summary>
public class PlayerDashState : PlayerState
{
    protected override int AnimHash => Animator.StringToHash("Player_Dash");

    private float originalGravity;
    private float dashDirection;

    public override void Enter()
    {
        base.Enter();

        // 1. 清空冲刺输入
        owner.UseDashInput();

        // ========================================================
        // 2. 核心修复：一进入冲刺状态，立刻扣除能量防止连冲
        //    如果没有这行，冲刺结束后切回 Fall 状态后，会触发第二次冲刺。
        // ======================================================== 
        owner.IsDashAvailable = false;

        // 3. 确定冲刺方向（按住方向优先，否则使用面朝方向）
        float horizontalInput = owner.MoveInput.x;
        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            dashDirection = Mathf.Sign(horizontalInput);
        }
        else
        {
            dashDirection = owner.FacingDirection;
        }

        // 4. 关闭重力：暂存重力并清零，防止冲刺时往下掉
        originalGravity = owner.rb.gravityScale;
        owner.rb.gravityScale = 0f;

        // 5. 赋予冲刺速度：瞬间抹平垂直速度，赋予纯水平冲刺速度
        owner.rb.velocity = new Vector2(dashDirection * owner.dashSpeed, 0f);

        // 6. 锁定转向：冲刺期间禁止掉头，防止镜头乱晃
        owner.CanFlip = false;
    }

    public override void Update()
    {
        base.Update();

        // 6. 时间到，自动退出
        if (stateTimer >= owner.dashDuration)
        {
            if (owner.CheckIsGrounded())
            {
                stateMachine.ChangeState<PlayerIdleState>();
            }
            else
            {
                stateMachine.ChangeState<PlayerFallState>();
            }
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        // 7. 核心维持：在冲刺状态里，持续强制写入速度，防止被物理/碰撞干扰
        owner.SetHorizontalVelocity(dashDirection * owner.dashSpeed);
    }

    public override void Exit()
    {
        base.Exit();

        // 8. 恢复重力
        owner.rb.gravityScale = originalGravity;

        // 9. 细节：冲刺结束时，稍微衰减一下水平速度（乘以0.4f），让角色在结束时有一丝"滑停"的感觉，而不是硬生生卡住
        owner.rb.velocity = new Vector2(owner.rb.velocity.x * 0.4f, owner.rb.velocity.y);

        // 10. 恢复转向
        owner.CanFlip = true;

        if (!owner.IsDashAvailable)
        {
            owner.LastDashTime = Time.time;
        }

        //// 只有 LastDashTime 没有被外部重置（不是 -99f）的时候才更新
        //if (owner.LastDashTime > 0)
        //{
        //    owner.LastDashTime = Time.time;
        //}
    }
}
