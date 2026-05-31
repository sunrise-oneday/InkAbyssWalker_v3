using UnityEngine;

/// <summary>
/// 主角大地图行走状态（已集成相对偏移实时更新）
/// </summary>
public class PlayerWalkState : PlayerGroundedState
{
    protected override int AnimHash => PlayerController.Anim_Walk;

    public override void Enter()
    {
        base.Enter();
    }

    public override void Update()
    {
        base.Update();
        if (Mathf.Abs(owner.MoveInput.x) <= 0.01f)
        {
            stateMachine.ChangeState<PlayerIdleState>();
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        float targetSpeed = owner.MoveInput.x * owner.MoveSpeed;

        // 1. 行走速度 = 玩家速度 + 平台速度
        owner.SetHorizontalVelocity(targetSpeed + owner.PlatformVelocity.x);

        // ========================================================
        // 2. 行走中实时刷新相对偏移：
        // 这样当玩家一旦松开方向键，停下来进入 Idle 的那一瞬间，
        // 记录的偏移量是完全精准无漂移的当前坐标！
        // ========================================================
        owner.UpdatePlatformOffset();
    }
}