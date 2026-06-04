using UnityEngine;

/// <summary>
/// 水元素附着（纯标记，不造成伤害）
/// 最多 4 层，由特定技能施加，被反应时消耗层数触发效果
/// </summary>
public class WaterAuraBuff : Buff
{
    public WaterAuraBuff(int turns, int initialStacks = 1)
    {
        buffName = "水附着";
        durationTurns = turns;
        element = ElementType.Water;
        stacks = Mathf.Clamp(initialStacks, 1, 4); // 最多4层
        maxStacks = 4;
        description = $"水元素附着，当前{stacks}层。被反应时消耗层数触发效果。";
        icon = Resources.Load<Sprite>("UI/Buffs/Icon_Water"); // 使用专门的水附着图标
    }
}
