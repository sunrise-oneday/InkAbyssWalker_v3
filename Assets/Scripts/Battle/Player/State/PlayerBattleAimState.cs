using UnityEngine;

/// <summary>
/// 玩家战斗特有瞄准开火状态（首击锁定，次击开火确认版） [1, 2]
/// </summary>
public class PlayerBattleAimState : PlayerBattleState
{
    // 默认播放持枪待机的瞄准循环姿态
    protected override int AnimHash => PlayerBattleEntity.Anim_Aim_Loop;

    private float shootTimer;
    private float shootDuration = 0.42f; // 单次射击的后坐力恢复时间（在此期间屏蔽按键）
    private bool isShooting;

    // 点射的基础物理伤害和削韧数值
    private int shootDamage = 25;
    private int shootBreakDamage = 15;

    public override void Enter()
    {
        base.Enter(); // 状态计时器自动归零，并开始播放瞄准循环动画

        // 1. 核心：通过 owner (PlayerBattleEntity) 消费瞄准和点射缓存输入 [2]
        owner.UseAimInput();
        owner.UseShootInput();

        isShooting = false;
        owner.SetHorizontalVelocity(0f);
        owner.CanFlip = false; // 瞄准期间锁死大世界的物理转头

        // 自动将身体和武器朝向对准当前被选中的怪物
        FaceTarget();

        // 表现设计：进入瞄准镜后，隐藏整个底部的 UGUI 技能面板
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.SetActionPanelActive(false);
        }

        Debug.Log("<color=cyan>[瞄准模式] 已进入瞄准姿态！按下【鼠标左键】消耗 1 AP 进行点射。再次按下【Q 键】收枪退出。</color>");
    }

    public override void Update()
    {
        // 1. 如果正在射击的后坐力硬直中
        if (isShooting)
        {
            shootTimer += Time.deltaTime;
            if (shootTimer >= shootDuration)
            {
                isShooting = false;
                // 射击完毕，平滑退回到【瞄准待机动画】
                owner.anim.CrossFade(PlayerBattleEntity.Anim_Aim_Loop, 0.1f);
                Debug.Log("[瞄准模式] 后坐力恢复，继续处于瞄准中。");
            }
            return; // 射击硬直中，屏蔽任何其他操作，保证动画不会鬼畜
        }

        // 实时让玩家面朝他选中的怪物
        FaceTarget();

        // ========================================================
        // 核心重构：双重点击判定（左键触发） [1, 2]
        // ========================================================
        if (owner.ShootInputBuffered)
        {
            owner.UseShootInput(); // 消费输入 [2]

            // 1. 发射 2D 射线，探测玩家鼠标点中的是哪个怪物
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                EnemyBattleEntity clickedEnemy = hit.collider.GetComponentInParent<EnemyBattleEntity>();
                if (clickedEnemy != null)
                {
                    // 2. 核心：判断点击的怪，是不是已经是当前“已锁定”的怪？ [1, 2]
                    if (clickedEnemy == BattleManager.Instance.selectedEnemy)
                    {
                        // 判定 A：如果是同一个怪，执行【开枪点射】！
                        if (BattleManager.Instance.sharedAP >= 1)
                        {
                            ExecuteShoot();
                        }
                        else
                        {
                            Debug.LogWarning("[瞄准模式] 共享 AP 不足，无法开火！");
                        }
                    }
                    else
                    {
                        // 判定 B：如果是新的怪，仅仅执行【更新锁定目标】，不准开枪！ [1, 2]
                        BattleManager.Instance.SelectTarget(clickedEnemy);
                        Debug.Log($"[瞄准模式] 目标已切换，成功锁定新敌人: {clickedEnemy.gameObject.name}。再次点击它即可开枪！");
                    }
                }
            }
        }

        // 3. 退出指令：按下 Q 键（OnAimPressed）
        if (owner.AimInputBuffered)
        {
            owner.UseAimInput();

            // 退出瞄准，安全回到正常的【战斗待机状态】
            stateMachine.ChangeState<PlayerBattleIdleState>();
        }
    }

    private void ExecuteShoot()
    {
        isShooting = true;
        shootTimer = 0f;

        // 1. 扣除 1 点队伍公共 AP，并即时刷新 UGUI 界面 [3]
        BattleManager.Instance.sharedAP -= 1;
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.RefreshUI();
        }

        // 2. 播放射击开火动画
        if (owner.anim != null)
        {
            owner.anim.CrossFade(PlayerBattleEntity.Anim_Shoot, 0.05f);
        }

        // 3. 对准玩家当前用鼠标锁定的那个怪物，扣除其血量和白色破防值 [2, 5]
        var target = BattleManager.Instance.selectedEnemy;
        if (target != null)
        {
            target.ReceiveAttack(shootDamage, shootBreakDamage);

            // 物理反馈：开枪时屏幕微震
            BattleManager.Instance.ShakeCamera(0.12f, 0.18f);
        }

        Debug.Log($"[瞄准模式] 开枪点射！消耗 1 AP，剩余 AP: {BattleManager.Instance.sharedAP}");
    }

    private void FaceTarget()
    {
        var target = BattleManager.Instance.selectedEnemy;
        if (target != null)
        {
            // 通过继承自 EntityBase 的属性，计算朝向
            float dirToTarget = target.transform.position.x - owner.transform.position.x;
            owner.AdjustFacingDirection(dirToTarget);
        }
    }

    public override void Exit()
    {
        base.Exit();
        owner.CanFlip = true; // 恢复物理转头

        // 退出瞄准状态，恢复显示 UGUI 控制大面板
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.SetActionPanelActive(true);
        }
        Debug.Log("[瞄准模式] 退出瞄准，恢复普通战斗姿势。");
    }
}