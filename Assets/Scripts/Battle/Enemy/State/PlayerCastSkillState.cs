using UnityEngine;

/// <summary>
/// 玩家在战斗中释放技能的状态（动作播完全自动退回待机，彻底解决卡死问题） [3]
/// </summary>
public class PlayerCastSkillState : PlayerBattleState
{
    // 动态获取玩家正在施放的技能动画 Hash
    protected override int AnimHash => Animator.StringToHash(owner.PendingSkill.animationState);

    private Skill currentSkill;
    private BattleEntity target;
    private float skillDuration;     // 技能动画总时长
    private bool hasAppliedDamage;   // 是否已经结算过伤害

    public override void Enter()
    {
        base.Enter(); // 自动进入计时器归零，并开始播放技能动画

        currentSkill = owner.PendingSkill;
        target = owner.PendingTarget;

        stateTimer = 0f;
        hasAppliedDamage = false;

        // 动态获取当前释放技能的动画真实总长度，实现全自动收招
        if (owner.Anim != null)
        {
            owner.Anim.Update(0f); // 强制刷新
            AnimatorStateInfo stateInfo = owner.Anim.GetCurrentAnimatorStateInfo(0);
            skillDuration = stateInfo.length;
        }
        else
        {
            skillDuration = 1.0f; // 默认防错时间
        }

        Debug.Log($"[状态机] 玩家开始施放技能: {currentSkill.skillName} | 动画总时长: {skillDuration:F2}s");
    }

    public override void Update()
    {
        // 1. 手动累加计时器（因为父类没有写 Update 累加）
        stateTimer += Time.deltaTime;

        // 2. 伤害落点：在技能进行到一半（50% 进度，伤害判定点）时进行结算 [3]
        if (stateTimer >= skillDuration * 0.5f && !hasAppliedDamage)
        {
            hasAppliedDamage = true;

            // 扣除共享蓝量，并向选中的怪物施加伤害和破防！ [3, 5]
            owner.ExecutePendingSkillDamage();
        }

        // 3. 核心：施法动作和收招后摇结束，全自动切换回【战斗待机状态】，彻底避免卡死！ [3]
        if (stateTimer >= skillDuration)
        {
            stateMachine.ChangeState<PlayerBattleIdleState>();
        }
    }
}