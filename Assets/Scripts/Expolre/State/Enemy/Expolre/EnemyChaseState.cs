using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

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

        // 1. 如果追到了玩家（进入了大地图扑击距离）
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

        // ========================================================
        // 核心修改：引入转向“死区”阈值（例如 0.25f）
        // 只有当玩家与怪物的水平距离大于 0.25f 时，怪物才会去改变朝向。
        // 如果玩家在怪物头顶（水平距离极小），怪物会保持当前朝向，不作无意义的频繁翻转。
        // ========================================================
        float turnThreshold = 0.25f;
        if (Mathf.Abs(directionToPlayer) > turnThreshold)
        {
            owner.AdjustFacingDirection(directionToPlayer);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        owner.SetHorizontalVelocity(owner.FacingDirection * owner.ChaseSpeed);
    }
}