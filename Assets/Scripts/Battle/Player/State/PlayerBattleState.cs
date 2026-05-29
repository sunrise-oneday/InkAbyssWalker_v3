using UnityEngine;

/// <summary>
/// 战斗状态基类，其泛型 T 直接约束为 PlayerBattleEntity
/// </summary>
public abstract class PlayerBattleState : BaseState<PlayerBattleEntity>
{
    protected virtual int AnimHash => 0;
    protected virtual float CrossFadeDuration => 0f;

    public override void Enter()
    {
        base.Enter(); // 自动调用 BaseState 的计时器清零

        // 直接通过 owner (PlayerBattleEntity) 访问动画机，播放战斗动画
        if (AnimHash != 0 && owner.anim != null)
        {
            owner.anim.CrossFade(AnimHash, CrossFadeDuration);
        }
    }

    public override void Update()
    {
        base.Update(); // 自动调用 BaseState 的时间累加
    }
}