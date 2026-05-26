using UnityEngine;

/// <summary>
/// 大世界怪物的待机状态（在边缘站着歇脚） [6]
/// </summary>
public class EnemyIdleState : OverworldEnemyState
{
    protected override int AnimHash => OverworldEnemy.Anim_Idle;

    public override void Enter()
    {
        base.Enter(); // 状态计时器重置，并播放待机动画
        owner.SetHorizontalVelocity(0f); // 待机时静止
    }

    public override void Update()
    {
        base.Update(); // 累加 stateTimer

        // 1. 警戒：即使在打瞌睡，一旦发现玩家，立刻转为追击
        if (owner.IsPlayerInRange())
        {
            stateMachine.ChangeState<EnemyChaseState>();
            return;
        }

        // 2. 歇脚时间结束：在退出前，自动调用 Flip 转身，并切回巡逻状态
        if (stateTimer >= owner.IdleDuration)
        {
            owner.Flip(); // 转身！
            stateMachine.ChangeState<EnemyPatrolState>();
        }
    }
}