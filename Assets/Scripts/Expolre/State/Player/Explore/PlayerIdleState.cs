using UnityEngine;

/// <summary>
/// 主角大地图待机状态（已集成平台物理锁死）
/// </summary>
public class PlayerIdleState : PlayerGroundedState
{
    protected override int AnimHash => PlayerController.Anim_Idle;

    public override void Enter()
    {
        base.Enter();
    }

    public override void Update()
    {
        base.Update();
        if (Mathf.Abs(owner.MoveInput.x) > 0.01f)
        {
            stateMachine.ChangeState<PlayerWalkState>();
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        // 1. 设置水平速度与平台完全同步
        owner.SetHorizontalVelocity(owner.PlatformVelocity.x);

        // ========================================================
        // 2. 核心锁死：静止时将坐标完全钉死在平台相对位置上，彻底阻断漂移！
        // ========================================================
        owner.LockToPlatform();
    }
}