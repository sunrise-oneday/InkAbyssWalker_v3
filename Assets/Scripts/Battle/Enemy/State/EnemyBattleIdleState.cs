using UnityEngine;

/// <summary>
/// 怪物战斗待机状态
/// </summary>
public class EnemyBattleIdleState : EnemyBaseBattleState
{
    // 对应 Animator 里的怪物战斗待机状态名字
    protected override int AnimHash => Animator.StringToHash("Enemy_BattleIdle");

    public override void Enter()
    {
        base.Enter();
        owner.SetHorizontalVelocity(0f); // 战斗中定身
    }
}