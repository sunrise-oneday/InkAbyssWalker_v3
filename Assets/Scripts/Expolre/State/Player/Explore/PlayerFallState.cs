using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家下落状态（下降段）
/// </summary>
public class PlayerFallState : PlayerAirborneState // 继承自空中基类
{
    protected override int AnimHash => PlayerController.Anim_Fall;

    public override void Enter()
    {
        base.Enter();
    }

    public override void Update()
    {
        base.Update(); // 执行基类的空中冲刺和输入丢弃检测

        // 核心跳转点：当下落检测到踩在地面上时
        if (owner.CheckIsGrounded())
        {
            // 根据玩家当前有没有按 A/D 键，决定落地是直接静止(Idle)还是继续走路(Walk)
            if (Mathf.Abs(owner.MoveInput.x) > 0.01f)
            {
                stateMachine.ChangeState<PlayerWalkState>();
            }
            else
            {
                stateMachine.ChangeState<PlayerIdleState>();
            }
        }
    }

}