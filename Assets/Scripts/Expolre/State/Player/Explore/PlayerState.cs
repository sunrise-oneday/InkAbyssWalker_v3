using UnityEngine;

public abstract class PlayerState : BaseState<PlayerController>
{
    // 子类如果有关联的动画，只需重写（Override）此属性并返回对应的 Hash 值。默认返回 0（不播放动画）
    protected virtual int AnimHash => 0;

    // 动画过渡时间。如果是传统 2D 帧动画，设为 0 即可立即切换。
    // 如果是 2D 骨骼动画（如 Spine ），可以设为 0.1f 获得丝滑的过渡效果。
    protected virtual float CrossFadeDuration => 0f;

    public override void Enter()
    {      
        base.Enter();
        // 核心解耦点：进入任何状态时，全自动播放对应的动画，彻底解放子类
        if (AnimHash != 0 && owner.anim != null)
        {
            owner.anim.CrossFade(AnimHash, CrossFadeDuration);
        }
    }
}