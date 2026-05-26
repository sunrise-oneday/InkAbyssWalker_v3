using UnityEngine;

/// <summary>
/// 大世界怪物的扑人攻击状态（支持：精确击中检测、自动对齐动画时长、空刀硬直后摇） [6]
/// </summary>
public class EnemyAttackState : OverworldEnemyState
{
    protected override int AnimHash => OverworldEnemy.Anim_Attack;

    private float attackDuration;      // 伤害判定落下的那一瞬间（判定点）
    private float totalAttackDuration; // 招式总持续时间（与你的动画剪辑真实长度 100% 自动对齐）
    private bool hasCheckedHit;        // 是否已经执行过击中判定

    public override void Enter()
    {
        base.Enter(); // 状态计时器自动重置为 0
        hasCheckedHit = false;

        // 1. 扑击：给予怪物一个顺着当前朝向的前冲惯性
        owner.SetHorizontalVelocity(owner.FacingDirection * (owner.ChaseSpeed * 0.8f));
        Debug.Log($"[大世界攻击] {owner.gameObject.name} 发起飞扑斩击！");

        // ========================================================
        // 2. 核心修复（动画对齐）：动态获取怪物攻击动画的真实总长度！
        // 这样能确保怪物必须在原地“老老实实播完完整动画（后摇）”才能退出，绝不会被拦腰切断！
        // ========================================================
        if (owner.anim != null)
        {
            owner.anim.Update(0f); // 强行让动画机立即刷新
            AnimatorStateInfo stateInfo = owner.anim.GetCurrentAnimatorStateInfo(0);
            totalAttackDuration = stateInfo.length; // 获取整段攻击动画的真实秒数

            // 假设在前摇进行到 45% 进度时发生伤害判定
            attackDuration = totalAttackDuration * 0.45f;
        }
        else
        {
            // 防错兜底时间
            totalAttackDuration = 1.0f;
            attackDuration = 0.45f;
        }
    }

    public override void Update()
    {
        base.Update(); // 累加 stateTimer

        // ========================================================
        // 3. 伤害落点/扑击落点判定（在动作进行到前摇结束时进行检测）
        // ========================================================
        if (stateTimer >= attackDuration && !hasCheckedHit)
        {
            hasCheckedHit = true;

            // 检测玩家是否依然在攻击范围内（也就是玩家没有通过冲刺或跳跃成功躲开）
            if (owner.IsPlayerInAttackRange())
            {
                Debug.Log($"<color=red>[大世界击中！] {owner.gameObject.name} 成功击中玩家！强行拉入战斗！</color>");

                // 核心安全保护：确保 BattleManager 存在，防止空引用报错导致物体无法销毁
                if (BattleManager.Instance != null)
                {
                    // ========================================================
                    // 核心修改：将这只小怪配置的关卡组索引传递过去！
                    // ========================================================
                    BattleManager.Instance.StartBattle(owner.EnemyGroupIndex, false);
                }
                else
                {
                    Debug.LogWarning("[大世界攻击] 场景中缺少 BattleManager 实例！请确保已挂载该组件。");
                }

                Object.Destroy(owner.gameObject);
                return;
            }
            else
            {
                // 空刀：玩家通过冲刺或跳跃成功躲开了攻击！
                Debug.Log($"<color=green>[大世界挥空] 玩家成功躲开了 {owner.gameObject.name} 的攻击！怪物进入硬直后摇...</color>");

                // 扑空时，清除怪物的飞扑速度，使其在收招硬直中由于阻力自然停下，原地罚站
                owner.SetHorizontalVelocity(0f);
            }
        }

        // ========================================================
        // 4. 核心修复：必须等整段攻击动画播放完毕（到达 totalAttackDuration）后，才允许判定退出！
        // ========================================================
        if (stateTimer >= totalAttackDuration)
        {
            // 如果玩家还在怪物的视野范围内，怪物收招后“转过身来”继续发起追击
            if (owner.IsPlayerInRange())
            {
                stateMachine.ChangeState<EnemyChaseState>();
            }
            // 如果玩家已经跑远了，怪物恢复到待机状态重新巡逻
            else
            {
                stateMachine.ChangeState<EnemyIdleState>();
            }
        }
    }
}