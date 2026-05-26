using UnityEngine;

public class PlayerIdleState : PlayerGroundedState
{
    // 只需声明它要绑定的动画，进入该状态时动画会自动无缝播放
    protected override int AnimHash => PlayerController.Anim_Idle;

    public override void Enter()
    {
        base.Enter(); // 必须调用，触发动画播放
        owner.SetHorizontalVelocity(0);
    }

    public override void Update()
    {
        base.Update();
        if (Mathf.Abs(owner.MoveInput.x) > 0.01f)
        {
            stateMachine.ChangeState<PlayerWalkState>();
        }
    }
}