using UnityEngine;

/// <summary>
/// 玩家时机招架状态（纯事件驱动版，免去计时器与冗余判定） [1]
/// </summary>
public class PlayerParryState : PlayerBattleState
{
    // 彻底废弃原本写死单动作的写法，改为在 Enter() 中根据形态动态交叉淡入！
    protected override int AnimHash => 0;

    private bool hasPlayedParryAnim; // 防止每帧重复触发举盾动画

    public override void Enter()
    {
        base.Enter(); // 自动调用 BaseState 的进入逻辑（因为上方的 AnimHash 为 0，所以不会自动播动画）

        // 核心一：一进入招架姿势，立刻清空玩家可能残留的旧格挡与闪避缓存，防止误触 [2]
        owner.UseParryInput();
        owner.UseDodgeInput();
        owner.SetHorizontalVelocity(0f);
        hasPlayedParryAnim = false;

        // ========================================================
        // 核心新增（动作分流）：根据玩家当前的形态，自动切换不同的防守准备姿势！
        // ========================================================
        if (owner.currentFormIndex == 2)
        {
            // 格挡形态：先进战斗待机，等待玩家按下 E 键后才切换到举盾动画
            owner.anim.CrossFade(Animator.StringToHash("Player_BattleIdle"), 0.1f);
            Debug.Log("[招架时机] 玩家处于【格挡形态】，请在被劈中前按下【E 键】格挡！");
        }
        else if (owner.currentFormIndex == 1)
        {
            // 闪避形态：播放轻盈的侧身闪步准备动作
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
                Debug.LogWarning($"<color=orange>[按键拦截] 当前处于【格挡形态】，左Shift闪避无效！请按【E 键】格挡！</color>");
            }
        }

        // ========================================================
        // 核心新增 2：格挡按键处理 [2]
        // 格挡形态下按 E → 播放举盾动画；非格挡形态下按 E → 丢弃输入
        // ========================================================
        if (owner.ParryInputBuffered)
        {
            if (owner.currentFormIndex == 2)
            {
                // 格挡形态：首次检测到 E 键按下时切换到举盾动画
                if (!hasPlayedParryAnim)
                {
                    owner.anim.CrossFade(Animator.StringToHash("Player_Parry_Loop"), 0.1f);
                    hasPlayedParryAnim = true;
                    Debug.Log("[招架时机] 检测到格挡按键，举起重盾！");
                }
            }
            else
            {
                owner.UseParryInput(); // 强行丢弃非格挡形态下的 E 键输入
                Debug.LogWarning($"<color=orange>[按键拦截] 当前处于【闪避形态】，E 键格挡无效！请按【左Shift键】闪避！</color>");
            }
        }

        // ========================================================
        // 所有的伤害时机判定完全由怪物的动画事件 TriggerDamage 触发并交由 BattleManager 自动结算。
        // 回合结束的切回 Idle 也完全由怪物的 TriggerAttackFinished 事件在后台安全接管！ [1]
        // ========================================================
    }
}