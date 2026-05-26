using UnityEngine;

/// <summary>
/// 玩家时机格挡状态
/// </summary>
public class PlayerParryState : PlayerBattleState
{
    protected override int AnimHash => Animator.StringToHash("Player_Parry_Loop");

    private EnemyAttackSequence currentSequence;
    private int currentHitIndex;
    private bool allPerfectSoFar;
    private float lastParryPressTime;

    public void SetAttackSequence(EnemyAttackSequence sequence)
    {
        this.currentSequence = sequence;
    }

    public override void Enter()
    {
        base.Enter();

        currentHitIndex = 0;
        allPerfectSoFar = true;
        lastParryPressTime = -99f;

        // 统一直接由 owner (PlayerBattleEntity) 消费格挡输入 [5]
        owner.UseParryInput();
        owner.SetHorizontalVelocity(0f);

        Debug.Log("[招架时机] 敌人发动了连击！请在落点时机按下招架键进行格挡！");
    }

    public override void Update()
    {
        base.Update();

        if (currentSequence == null) return;

        // 直接读取 owner 战斗实体里的格挡缓存
        if (owner.ParryInputBuffered)
        {
            owner.UseParryInput();
            lastParryPressTime = stateTimer;
            Debug.Log($"[格挡状态] 玩家架招！按下时间戳: {lastParryPressTime:F2}s");
        }

        // 检测判定
        if (currentHitIndex < currentSequence.hitTimings.Length)
        {
            float targetHitTime = currentSequence.hitTimings[currentHitIndex];
            if (stateTimer >= targetHitTime)
            {
                EvaluateHitResult(targetHitTime);
                currentHitIndex++;
            }
        }

        // 结算跳转
        if (currentHitIndex >= currentSequence.hitTimings.Length)
        {
            float lastHitTime = currentSequence.hitTimings[currentSequence.hitTimings.Length - 1];
            if (stateTimer >= lastHitTime + 0.3f)
            {
                if (allPerfectSoFar)
                {
                    // 状态切换直接在战斗状态机中进行，保持完美的强类型安全！
                    stateMachine.ChangeState<PlayerCounterAttackState>();
                }
                else
                {
                    stateMachine.ChangeState<PlayerBattleIdleState>();
                }
            }
        }
    }

    private void EvaluateHitResult(float hitTime)
    {
        const float PerfectWindow = 0.12f;
        const float NormalWindow = 0.30f;

        float timeDiff = Mathf.Abs(hitTime - lastParryPressTime);

        // 1. 获取怪物当前这一击对应的配置伤害和削韧值 [6]
        int rawDamage = currentSequence.hitDamages[currentHitIndex];
        int breakDamage = currentSequence.hitBreakDamages[currentHitIndex];

        if (timeDiff <= PerfectWindow)
        {
            // 完美招架：免疫 100% 伤害和削韧，播放火花与清脆的金属招架特效！ [6]
            Debug.Log($"<color=green>[完美招架！] 成功挡下第 {currentHitIndex + 1} 击！免除伤害！时间差: {timeDiff:F3}s</color>");
        }
        else if (timeDiff <= NormalWindow)
        {
            // 普通招架：免疫削韧（破防条不减），但承受 30% 的格挡削弱伤害 [6]
            allPerfectSoFar = false;
            int reducedDamage = Mathf.RoundToInt(rawDamage * 0.3f);

            // 玩家战斗实体接收伤害
            owner.ReceiveAttack(reducedDamage, 0);
            Debug.Log($"<color=yellow>[普通招架] 成功格挡第 {currentHitIndex + 1} 击，但承受了 {reducedDamage} 点格挡减免伤害！时间差: {timeDiff:F3}s</color>");
        }
        else
        {
            // 格挡失败/被击中：扣除 100% 全额伤害和 100% 削韧！ [6]
            allPerfectSoFar = false;

            owner.ReceiveAttack(rawDamage, breakDamage);
            Debug.Log($"<color=red>[未挡下/受击！] 第 {currentHitIndex + 1} 击格挡失败！承受 {rawDamage} 点全额伤害，破防条被削减了 {breakDamage}！时间差: {timeDiff:F3}s</color>");
        }

        lastParryPressTime = -99f;
    }
}