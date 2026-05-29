using UnityEngine;

/// <summary>
/// 玩家战斗死亡状态（战斗失败时触发，播放死亡动画并锁死物理刚体） [5]
/// </summary>
public class PlayerBattleDieState : PlayerBattleState, IDeathState
{
    // 指定播放你 Animator 里的 Player_BattleDie 动画状态名字
    protected override int AnimHash => Animator.StringToHash("Player_BattleDie");

    public override void Enter()
    {
        base.Enter(); // 状态计时器重置，并播放死亡倒地动画

        owner.SetHorizontalVelocity(0f);

        Debug.Log($"[玩家死亡] {owner.gameObject.name} 播放死亡动作！");
    }
}