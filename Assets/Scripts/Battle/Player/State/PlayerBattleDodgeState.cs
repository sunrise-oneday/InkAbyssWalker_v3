using UnityEngine;

/// <summary>
/// 玩家战斗特有闪避状态（继承自 PlayerBattleState，无缝防重名） [2]
/// </summary>
public class PlayerBattleDodgeState : PlayerBattleState
{
    // 对应 Animator 中的 Player_BattleDodge 动画方块名字
    protected override int AnimHash => Animator.StringToHash("Player_BattleDodge");

    private float dodgeDuration = 0.25f; // 闪避无敌帧/动作总时长

    public override void Enter()
    {
        base.Enter(); // stateTimer 自动重置为 0

        // 1. 消费闪避按键输入
        // ========================================================
        // 核心修复：在此处【绝对不要】调用 owner.UseDodgeInput(); ！！！
        // 如果在 Enter 里就把它 Use（重置为-99）了，等怪物打到你时，
        // 你的时间戳就会变成 -99，算出来的时间差就会是 110000 毫秒，导致完美闪避永远失败！
        // 放心，输入会在 BattleManager 的 EvaluateParry 里面被正确消费重置！ [1, 2]
        // ========================================================
        // owner.UseDodgeInput(); // <--- 彻底删除或注释掉这一行！
        owner.SetHorizontalVelocity(0f);

        // 2. 闪避期间，锁死身体转头控制，保持动作连贯
        owner.CanFlip = false;

        Debug.Log("[闪避动作] 玩家踩出侧身闪步！身体进入无敌阶段。");
    }

    public override void Update()
    {
        base.Update(); // 累加 stateTimer

        // 3. 闪避动作（0.25秒）结束：
        // 自动退回到 PlayerParryState（防守架势）中！
        // 这样如果怪物有下一刀伤害，玩家还可以继续选择招架（Space）或者再次闪避（Shift）！
        if (stateTimer >= dodgeDuration)
        {
            stateMachine.ChangeState<PlayerParryState>();
        }
    }

    public override void Exit()
    {
        base.Exit();
        owner.CanFlip = true; // 恢复身体转头控制
    }
}