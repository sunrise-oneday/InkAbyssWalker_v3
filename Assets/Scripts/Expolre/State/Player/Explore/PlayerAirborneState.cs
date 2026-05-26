using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 空中状态基类（Jump 和 Fall 状态的共同父类）
/// </summary>
public abstract class PlayerAirborneState : PlayerState
{
    public override void Enter()
    {
        base.Enter();
    }

    public override void Update()
    {
        base.Update();

        // 1. 统一空中/冲刺检测（避免在 Jump 和 Fall 状态中重复写）
        if (owner.DashInputBuffered)
        {
            // ========================================================
            // 核心修复：在空中冲刺，需要同时检查"能量可用"且"不在 CD 中"
            // ========================================================
            if (owner.IsDashAvailable && !owner.IsDashOnCooldown)
            {
                owner.UseDashInput();

                stateMachine.ChangeState<PlayerDashState>();
                return;
            }
            else
            {        
                owner.UseDashInput();
            }
        }

        // 2. 统一跳跃/攻击输入缓冲，防止连跳和自动连击 Bug
        if (owner.JumpInputBuffered)
        {
            owner.UseJumpInput();
        }
        if (owner.AttackInputBuffered)
        {
            owner.UseAttackInput();
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        // 3. 统一应用空中水平微调（保留操控感）
        //    确保角色无论是"起跳"还是"下坠"，都拥有一致的空中转向操控
        float targetSpeed = owner.MoveInput.x * owner.MoveSpeed;
        owner.SetHorizontalVelocity(targetSpeed);
    }
}
