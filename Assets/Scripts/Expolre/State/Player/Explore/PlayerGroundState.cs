using UnityEngine;

public abstract class PlayerGroundedState : PlayerState
{
    public override void Enter()
    {
        base.Enter();

        // ========================================================
        // 核心修复：每次踏上地面时，只恢复能量为 true，不重置 CD 时间限制
        //    保证即使踩在地面上，也必须完整等待 1.0 秒的冲刺冷却
        // ========================================================
        owner.RefillDash(false);
    }

    public override void Update()
    {
        // 强防御：如果检测到离开了地面（原因为跳离了地面），
        // 则在底层统一强制切换至下坠（Fall）状态，此处代码几乎不会触发
        if (!owner.CheckIsGrounded())
        {
            stateMachine.ChangeState<PlayerFallState>();
            return;
        }

        // 通用跳跃检测（也可以写在这里，如：从 Jump 状态切换回 JumpState）
        // 检测到玩家按下了跳跃键
        if (owner.JumpInputBuffered)
        {
            // 关键：消费掉跳跃缓冲（设为 false），防止落地时再次自动跳跃
            owner.UseJumpInput();

            stateMachine.ChangeState<PlayerJumpState>();
            return;
        }

        if (owner.DashInputBuffered)
        {
            // 核心修复：只有在不在冷却时才能冲刺
            if (!owner.IsDashOnCooldown)
            {
                owner.UseDashInput();
                stateMachine.ChangeState<PlayerDashState>();
                return;
            }
            else
            {
                // 细节：如果冲刺在 CD 中，直接清空本次按键输入！
                // 这样可以防止：在 CD 时提前 0.1 秒按了 Shift，CD 一结束角色自动冲出去
                owner.UseDashInput();
            }
        }

        // 连击输入检测 (Fire 键)
        if (owner.AttackInputBuffered)
        {
            // 核心：是否超出了连击窗口期
            // 如果距离上一次攻击动作结束的时间超出了连击窗口（如 1 秒），则强制重置回第一斩
            if (Time.time - owner.LastAttackTime > owner.ComboWindowTime)
            {
                owner.CurrentComboIndex = 0;
            }

            stateMachine.ChangeState<PlayerAttackState>();
        }
    }

}
