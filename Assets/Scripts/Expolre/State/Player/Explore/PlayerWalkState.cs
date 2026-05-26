using UnityEngine;

public class PlayerWalkState : PlayerGroundedState
{
    // 只需声明行走动画
    protected override int AnimHash => PlayerController.Anim_Walk;

    public override void Enter()
    {
        base.Enter(); // 必须调用，触发动画播放
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
        owner.SetHorizontalVelocity(targetSpeed);
    }
}