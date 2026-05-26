using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家跳跃状态（上升段）
/// </summary>
public class PlayerJumpState : PlayerAirborneState // 继承自空中基类
{
    protected override int AnimHash => PlayerController.Anim_Jump;

    public override void Enter()
    {
        base.Enter(); // 执行基类的 Enter 重置

        // 安全防线：确保进入跳跃状态时清空可能残留的跳跃输入
        owner.UseJumpInput();

        // 直接强制将 Y 轴速度设为跳跃速度，抹平下落物理势能，保证跳跃高度绝对恒定
        owner.rb.velocity = new Vector2(owner.rb.velocity.x, owner.jumpForce);
    }

    public override void Update()
    {
        base.Update(); // 执行基类的空中冲刺和输入丢弃检测

        // 核心跳转：抛物线顶点检测。只要 Y 轴速度变负，转为下落状态
        if (owner.rb.velocity.y < -0.1f)
        {
            stateMachine.ChangeState<PlayerFallState>();
        }
    }
}