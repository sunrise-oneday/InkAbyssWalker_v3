using UnityEngine;

/// <summary>
/// 怪物释放多段连击的状态
/// </summary>
public class EnemyBattleState : EnemyBaseBattleState
{
    // 废弃写死单动作，动画变招由动画事件全自动流转 [1]
    protected override int AnimHash => 0;

    public override void Enter()
    {
        // 自动调用 BaseState 的 Enter 重置
        base.Enter();

        // 1. 核心：进入状态时，只负责引爆第一斩的动作启动！ [1, 2]
        if (owner.Anim != null && owner.HitAnimHashes != null && owner.HitAnimHashes.Length > 0)
        {
            // 播放第一段攻击动画（如 Enemy_Attack_1） [2]
            owner.Anim.CrossFade(owner.HitAnimHashes[0], 0.1f);
            Debug.Log($"[状态机] 敌人进入攻击状态，启动第一击: {owner.GetAttackSequence().hitAnimations[0]}");
        }
    }

    // ========================================================
    // 彻底解放！这里不需要重写 Update()，不需要 FixedUpdate()！
    // 所有的变招过渡（TriggerNextAttack）、落点判定（TriggerDamage）、
    // 以及回合结束退回（TriggerAttackFinished）全部由 Unity 的动画事件全自动搞定！ [1]
    // ========================================================
}