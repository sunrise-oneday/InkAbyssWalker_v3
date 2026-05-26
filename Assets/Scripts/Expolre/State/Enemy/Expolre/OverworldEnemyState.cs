using UnityEngine;

/// <summary>
/// 大世界怪物状态基类（用于全自动驱动怪物身上的动画机，解决怪物没有主角动画的报错）
/// </summary>
public abstract class OverworldEnemyState : BaseState<OverworldEnemy>
{
    // 子类重写此属性，提供怪物自己特有的动画 Hash 变量
    protected virtual int AnimHash => 0;

    public override void Enter()
    {
        // 自动重置计时器为 0
        base.Enter();

        // 核心解耦：进入任何状态时，自动触发播放怪物自己的动画！
        if (AnimHash != 0 && owner.anim != null)
        {
            owner.anim.CrossFade(AnimHash, 0f);
        }
    }

    public override void Update()
    {
        base.Update();
    }
}