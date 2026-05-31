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

        if (InputManager.Instance?.Battle != null)
        {
            InputManager.Instance.Battle.OnQuickFormPressed += HandleQuickForm;
        }
    }

    public override void Update()
    {
        base.Update();

        // 非玩家回合时禁止任何操作
        if (BattleTurnManager.Instance.currentPhase != BattlePhase.PlayerTurn)
            return;

        // 监听 Q 键瞄准
        if (owner.AimInputBuffered)
        {
            owner.UseAimInput();

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
        if (InputManager.Instance?.Battle != null)
        {
            InputManager.Instance.Battle.OnQuickFormPressed -= HandleQuickForm;
        }
    }

    private void HandleQuickForm(int formIndex)
    {
        // 非玩家回合时禁止换形
        if (BattleTurnManager.Instance.currentPhase != BattlePhase.PlayerTurn)
            return;

        owner.SwitchForm(formIndex);
    }
}