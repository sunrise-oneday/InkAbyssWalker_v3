using UnityEngine;

/// <summary>
/// 玩家战斗待机状态
/// </summary>
public class PlayerBattleIdleState : PlayerBattleState
{
    protected override int AnimHash => Animator.StringToHash("Player_BattleIdle");

    public override void Enter()
    {
        base.Enter();

        // 此时 owner 直接就是 PlayerBattleEntity，不需要 GetComponent 了，极度干净！
        owner.SetHorizontalVelocity(0f);

        if (BattleInputReader.Instance != null)
        {
            BattleInputReader.Instance.OnQuickFormPressed += HandleQuickForm;
        }
    }

    public override void Update()
    {
        base.Update();

        // 监听 Q 键瞄准
        if (owner.AimInputBuffered)
        {
            owner.UseAimInput();

            // ========================================================
            // 核心修改：只有在 正常状态 (形态 0) 下，才允许进入瞄准点射！ [2]
            // ========================================================
            if (owner.currentFormIndex == 0)
            {
                stateMachine.ChangeState<PlayerBattleAimState>();
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[战术限制] 当前处于非正常形态下，无法进入瞄准点射！</color>");
            }
            return;
        }

    }

    public override void Exit()
    {
        base.Exit();
        if (BattleInputReader.Instance != null)
        {
            BattleInputReader.Instance.OnQuickFormPressed -= HandleQuickForm;
        }
    }

    private void HandleQuickForm(int formIndex)
    {
        owner.SwitchForm(formIndex); // 直接通过 owner 调用变身方法！
    }
}