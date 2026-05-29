using UnityEngine;

/// <summary>
/// 敌方战斗眩晕/受控状态（在大世界被大招砸中时，瞬间切入本状态罚站） [5]
/// </summary>
public class EnemyBattleStunState : EnemyBaseBattleState
{
    // 对应 Animator 里的怪物眩晕/受击循环动画（比如 Enemy_Dizzy） [5]
    protected override int AnimHash => Animator.StringToHash("Enemy_Dizzy");

    public override void Enter()
    {
        base.Enter(); // 播放眩晕动画
        owner.SetHorizontalVelocity(0f); // 物理定身
    }
}