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