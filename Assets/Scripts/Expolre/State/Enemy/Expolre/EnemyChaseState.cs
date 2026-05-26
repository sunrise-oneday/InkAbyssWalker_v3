using UnityEngine;

public class EnemyChaseState : OverworldEnemyState
{
    protected override int AnimHash => OverworldEnemy.Anim_Chase;

    public override void Enter()
    {
        base.Enter();
        Debug.Log($"<color=red>[警报] {owner.gameObject.name} 发现了玩家！切换为追击状态！</color>");
    }

    public override void Update()
    {
        base.Update();

        // ========================================================
        // 1. 核心新增：如果追到了玩家（进入了大地图扑击距离）
        // 立刻强行切入【大世界扑人攻击】状态！给玩家来一下狠的！ [6]
        // ========================================================
        if (owner.IsPlayerInAttackRange())
        {
            stateMachine.ChangeState<EnemyAttackState>();
            return;
        }

        // 玩家跑远了，放弃追击
        if (!owner.IsPlayerInRange())
        {
            stateMachine.ChangeState<EnemyPatrolState>();
            return;
        }

        float directionToPlayer = owner.PlayerTransform.position.x - owner.transform.position.x;
        owner.AdjustFacingDirection(directionToPlayer);
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        owner.SetHorizontalVelocity(owner.FacingDirection * owner.ChaseSpeed);
    }
}