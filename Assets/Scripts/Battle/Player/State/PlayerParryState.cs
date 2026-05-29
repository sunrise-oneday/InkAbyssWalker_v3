using UnityEngine;

/// <summary>
/// 玩家时机招架状态（纯事件驱动版，免去计时器与冗余判定） [1]
/// </summary>
public class PlayerParryState : PlayerBattleState
{
    // 彻底废弃原本写死单动作的写法，改为在 Enter() 中根据形态动态交叉淡入！
    protected override int AnimHash => 0;

    public override void Enter()
    {
        base.Enter(); // 自动调用 BaseState 的进入逻辑（因为上方的 AnimHash 为 0，所以不会自动播动画）

        // 核心一：一进入招架姿势，立刻清空玩家可能残留的旧格挡与闪避缓存，防止误触 [2]
        owner.UseParryInput();
        owner.UseDodgeInput();
        owner.SetHorizontalVelocity(0f);

        // ========================================================
        // 核心新增（动作分流）：根据玩家当前的形态，自动切换不同的防守准备姿势！
        // ========================================================
        if (owner.currentFormIndex == 2)
        {
            // 如果是格挡形态 (2)，播放威严的举盾防守待机动作
            owner.anim.CrossFade(Animator.StringToHash("Player_Parry_Loop"), 0.1f);
            Debug.Log("[招架时机] 玩家处于【格挡形态】，已架起重盾！请在被劈中前按下【空格键】格挡！");
        }
        else if (owner.currentFormIndex == 1)
        {
            // 如果是闪避形态 (1)，播放轻盈的侧身闪步准备动作
            owner.anim.CrossFade(Animator.StringToHash("Player_Dodge_Prep"), 0.1f);
            Debug.Log("[招架时机] 玩家处于【闪避形态】，已屈膝准备！请在被劈中前按下【左 Shift 键】闪避！");
        }
    }

    public override void Update()
    {
        base.Update();

        // ========================================================
        // 核心新增 1：闪避打断判定（只允许【闪避形态 1】进行闪避） [2]
        // ========================================================
        if (owner.DodgeInputBuffered)
        {
            if (owner.currentFormIndex == 1)
            {
                stateMachine.ChangeState<PlayerBattleDodgeState>();
                return;
            }
            else
            {
                // 如果当前不是闪避形态，直接清空输入并拦截（防止将输入残留带到下一帧）
                owner.UseDodgeInput();
                Debug.LogWarning("[防守限制] 当前处于非【闪避形态】下，无法使用闪避按键！");
            }
        }

        // ========================================================
        // 核心新增 2：招架按键防御性清空（只允许【格挡形态 2】进行招架） [2]
        // 由于招架是动画事件触发的，如果玩家在非格挡形态下按了空格，我们需要在 Update 中将其清空，防止残留到落地
        // ========================================================
        if (owner.ParryInputBuffered)
        {
            if (owner.currentFormIndex != 2)
            {
                owner.UseParryInput(); // 强行丢弃非格挡形态下的空格输入
                Debug.LogWarning("[防守限制] 当前处于非【格挡形态】下，无法使用招架按键！");
            }
        }

        // ========================================================
        // 所有的伤害时机判定完全由怪物的动画事件 TriggerDamage 触发并交由 BattleManager 自动结算。
        // 回合结束的切回 Idle 也完全由怪物的 TriggerAttackFinished 事件在后台安全接管！ [1]
        // ========================================================
    }
}