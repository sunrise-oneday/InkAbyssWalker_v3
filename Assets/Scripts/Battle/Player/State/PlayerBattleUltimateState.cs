using UnityEngine;

/// <summary>
/// 玩家终极奥义释放状态（全诊断日志 + 纯面板数据对齐版） [2, 3]
/// </summary>
public class PlayerBattleUltimateState : PlayerBattleState
{
    protected override int AnimHash => Animator.StringToHash(owner.equippedUltimate.animationState);

    private float localTimer;      // 纯手写高精度计时器
    private UltimateSkill currentUlt;
    private bool hasDealtDamage;

    public override void Enter()
    {
        base.Enter(); // 自动进入计时器归零

        localTimer = 0f;
        hasDealtDamage = false;
        owner.SetHorizontalVelocity(0f);
        owner.CanFlip = false; // 锁死大世界物理转头

        currentUlt = owner.equippedUltimate;

        if (currentUlt == null || string.IsNullOrEmpty(currentUlt.ultimateName) || currentUlt.ultimateName == "0")
        {
            Debug.LogWarning("[大招状态] 检测到未配置任何合法大招！直接退回待机。");
            stateMachine.ChangeState<PlayerBattleIdleState>();
            return;
        }

        // 大招开始：镜头开始震动
        BattleManager.Instance.ShakeCamera(0.4f, 0.35f);

        // 1. 抓包日志 1：
        Debug.Log($"<color=orange>[大招调试] ===== 正式进入 PlayerBattleUltimateState 奥义释放！ =====\n" +
                  $"奥义名称: {currentUlt.ultimateName} | 动画状态: {currentUlt.animationState} | 配置时间: {currentUlt.duration}s | 判定进度: {currentUlt.hitProgress}</color>");
    }

    public override void Update()
    {
        // 2. 核心修复：手动累加时间
        localTimer += Time.deltaTime;

        if (currentUlt == null) return;

        // 3. 抓包日志 2：实时看计时器是否在跑（如果没打印，说明状态机在一进入时就报错死锁了）
        // Debug.Log($"[大招调试] 倒计时: {localTimer:F2}s / 总时长: {currentUlt.duration:F2}s");

        float damageHitTime = currentUlt.duration * currentUlt.hitProgress; // 0.8 * 0.4 = 0.32秒

        // 4. 伤害判定点
        if (localTimer >= damageHitTime && !hasDealtDamage)
        {
            hasDealtDamage = true;
            ExecuteUltimateEffects();
        }

        // 5. 核心：大招动作播放完毕，自动返回待机
        if (localTimer >= currentUlt.duration)
        {
            Debug.Log($"<color=orange>[大招调试] ===== 大招动作完成，正在退回战斗待机！ =====</color>");
            stateMachine.ChangeState<PlayerBattleIdleState>();
        }
    }

    /// <summary>
    /// 根据大招类型，分流结算
    /// </summary>
    private void ExecuteUltimateEffects()
    {
        Debug.Log($"<color=orange>[大招调试] ===== 开始执行大招效果结算！类型: {currentUlt.ultimateType} =====</color>");

        BattleManager.Instance.ShakeCamera(0.25f, 0.45f);
        BattleManager.Instance.HitStop(0.12f); // 卡肉顿挫

        switch (currentUlt.ultimateType)
        {
            case UltimateSkill.UltimateType.SingleTarget:
                var target = BattleManager.Instance.selectedEnemy;
                if (target != null)
                {
                    target.ReceiveAttack(currentUlt.baseDamage, currentUlt.breakDamage);
                }
                break;

            case UltimateSkill.UltimateType.AoE:
                var enemies = BattleManager.Instance.activeEnemies;
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    if (enemies[i] != null)
                    {
                        enemies[i].ReceiveAttack(currentUlt.baseDamage, currentUlt.breakDamage);
                    }
                }
                break;

            case UltimateSkill.UltimateType.Control:
                // ========================================================
                // 核心对齐：控制型大招，只对当前选中的怪物施加眩晕 [1]
                // ========================================================
                var ctrlTarget = BattleManager.Instance.selectedEnemy;
                if (ctrlTarget != null)
                {
                    Debug.Log($"[大招调试] 正在对目标 {ctrlTarget.gameObject.name} 进行控制大招打击...");
                    ctrlTarget.ReceiveAttack(currentUlt.baseDamage, currentUlt.breakDamage);

                    // 施加眩晕 Buff（它会自动瞬间接管怪物的动画）
                    ctrlTarget.Stats.AddBuff(new StunBuff(currentUlt.stunTurns));
                }
                break;
        }

        // 刷新血条
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.RefreshUI();
        }
    }

    public override void Exit()
    {
        base.Exit();
        owner.CanFlip = true;

        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.SetActionPanelActive(true);
        }
    }
}