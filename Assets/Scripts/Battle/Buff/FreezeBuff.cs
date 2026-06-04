using UnityEngine;

/// <summary>
/// 冻结 Buff（由冰+水反应触发）
/// 眩晕效果，无法行动
/// </summary>
public class FreezeBuff : Buff
{
    public FreezeBuff(int turns, int freezeStacks)
    {
        buffName = "冻结";
        durationTurns = turns;
        stacks = Mathf.Clamp(freezeStacks, 1, 4);
        maxStacks = 4;
        description = $"冻结中，无法行动，{stacks}层";
        element = ElementType.None; // 冻结是反应结果，不是元素附着
        icon = Resources.Load<Sprite>("UI/Buffs/Icon_Freeze"); // 使用专门的冻结图标
    }

    // 冻结效果由状态机检查（类似 StunBuff）
    // 敌人回合时检查是否有 FreezeBuff，有则跳过行动
}
