
using UnityEngine;

/// <summary>
/// 怪物战斗状态基类，其泛型 T 严格约束为 EnemyBattleEntity
/// </summary>
public abstract class EnemyBaseBattleState : BaseState<EnemyBattleEntity>
{
    // 子类重写此属性，提供怪物特有的战斗动画 Hash
    protected virtual int AnimHash => 0;

    public override void Enter()
    {
        base.Enter(); // 自动调用 BaseState 进行计时器归零

        // 进入状态时，全自动播放怪物对应的战斗动画
        if (AnimHash != 0 && owner.anim != null)
        {
            owner.anim.CrossFade(AnimHash, 0.1f);
        }
    }

    public override void Update()
    {
        base.Update();
    }
}