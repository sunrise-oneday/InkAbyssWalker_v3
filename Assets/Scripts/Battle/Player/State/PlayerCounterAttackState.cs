using UnityEngine;

/// <summary>
/// 完美反击状态
/// </summary>
public class PlayerCounterAttackState : PlayerBattleState
{
    protected override int AnimHash => Animator.StringToHash("Player_CounterAttack");

    private float counterDuration = 0.8f;

    public override void Enter()
    {
        base.Enter();
        owner.SetHorizontalVelocity(0f);
        Debug.Log("[反击动作] 执行完美反击！对敌方造成巨额真实伤害与削韧破防！");
    }

    public override void Update()
    {
        base.Update();
        if (stateTimer >= counterDuration)
        {
            stateMachine.ChangeState<PlayerBattleIdleState>();
        }
    }
}