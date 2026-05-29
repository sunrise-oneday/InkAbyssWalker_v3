using UnityEngine;

/// <summary>
/// 完美格挡后的自动反击状态（动态对齐真实动画时间，反击结束后交还回合控制权） [2]
/// </summary>
public class PlayerCounterAttackState : PlayerBattleState
{
    protected override int AnimHash => Animator.StringToHash("Player_CounterAttack");

    private float counterDuration; // 自动读取反击动画的真实长度，杜绝硬编码 0.8s [2]
    private bool hasAppliedDamage; // 防止在 40% 进度帧重复判定伤害

    public override void Enter()
    {
        base.Enter(); // 状态计时器自动归零，并开始播放反击动画
        owner.SetHorizontalVelocity(0f);
        hasAppliedDamage = false; // 重置
        // ========================================================
        // 核心修改：动态获取反击动画（Player_CounterAttack）的真实时间长度！ [2]
        // ========================================================
        if (owner.anim != null)
        {
            owner.anim.Update(0f); // 强行让动画机立即刷新
            AnimatorStateInfo stateInfo = owner.anim.GetCurrentAnimatorStateInfo(0);
            counterDuration = stateInfo.length; // 自动获取整段动画的秒数
        }
        else
        {
            counterDuration = 0.8f; // 默认防错时间
        }

        Debug.Log($"[反击动作] 开启完美反击！动画真实时长: {counterDuration:F2}s");
    }

    public override void Update()
    {
        base.Update();

        // ========================================================
        // 核心修改（伤害结算点）：
        // 在反击动画进行到 40% 进度时（刀刃刚好劈在怪物脸上），结算高额真实伤害与破防！
        // ========================================================
        if (stateTimer >= counterDuration * 0.40f && !hasAppliedDamage)
        {
            hasAppliedDamage = true;

            // ========================================================
            // 核心修复：反击的目标必须是当前正在攻击你的那个怪物（CurrentAttacker）！
            // 绝对不能用玩家自己选择的目标（selectedEnemy），因为那代表玩家自己回合要打的怪！ [2, 3]
            // ========================================================
            var target = BattleManager.Instance.CurrentAttacker;

            if (target != null)
            {
                // 动态向战斗实体索要经过符文、装备、状态修饰后的最终数值！ [2]
                int dmg = owner.GetFinalCounterDamage();
                int brk = owner.GetFinalCounterBreakDamage();

                // 狠狠砸在正在打你的那个怪物的脸上！ [5]
                target.ReceiveAttack(dmg, brk);

                // 刷新 UGUI 战场属性条
                if (BattleUIController.Instance != null)
                {
                    BattleUIController.Instance.RefreshUI();
                }
            }
        }


        if (stateTimer >= counterDuration)
        {
            // 1. 反击结束，玩家自动退回战斗待机状态（彻底解决没有退回待机的问题） [2]
            stateMachine.ChangeState<PlayerBattleIdleState>();

            // 2. 核心：反击结束，正式恢复敌方回合队列的推进！
            BattleManager.Instance.ProceedEnemyTurn();
        }
    }
}