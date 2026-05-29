using UnityEngine;

/// <summary>
/// 强力控制：眩晕状态（纯状态数据类，符合单一职责原则，不参与任何状态机切换逻辑） [1]
/// </summary>
public class StunBuff : Buff
{
    public StunBuff(int turns)
    {
        buffName = "眩晕";
        durationTurns = turns;
        element = ElementType.None; // 属于物理控制异常，无元素
        icon = Resources.Load<Sprite>("UI/Buffs/Icon_Stun"); // 水晶图片
        description = "被强力控制！处于眩晕状态，本大回合内无法执行任何行动。";
    }
}