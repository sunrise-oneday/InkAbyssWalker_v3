using UnityEngine;

/// <summary>
/// 火元素附着（纯标记，不造成伤害）
/// 最多 4 层，由特定技能施加，被反应时消耗层数触发效果
/// </summary>
public class FireAuraBuff : Buff
{
    public FireAuraBuff(int turns, int initialStacks = 1)
    {
        buffName = "火附着";
        durationTurns = turns;
        element = ElementType.Fire;
        stacks = Mathf.Clamp(initialStacks, 1, 4); // 最多4层
        maxStacks = 4;
        description = $"火元素附着，当前{stacks}层。被反应时消耗层数触发效果。";
        icon = Resources.Load<Sprite>("UI/Buffs/Icon_Fire");
    }
}
