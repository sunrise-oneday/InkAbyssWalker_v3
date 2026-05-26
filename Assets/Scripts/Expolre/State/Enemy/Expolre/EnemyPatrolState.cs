using UnityEngine;

public class EnemyPatrolState : OverworldEnemyState
{
    protected override int AnimHash => OverworldEnemy.Anim_Patrol;

    public override void Enter()
    {
        base.Enter();
    }

    public override void Update()
    {
        base.Update();

        // 1. 发现玩家，切入追击
        if (owner.IsPlayerInRange())
        {
            stateMachine.ChangeState<EnemyChaseState>();
            return;
        }

        if (!owner.CheckGroundAhead())
        {
            stateMachine.ChangeState<EnemyIdleState>();
        }

    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        owner.SetHorizontalVelocity(owner.FacingDirection * owner.PatrolSpeed);
    }
}