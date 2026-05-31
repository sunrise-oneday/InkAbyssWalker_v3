using UnityEngine;

/// <summary>
/// 速度下降Debuff - 降低目标速度（影响行动顺序）
/// </summary>
public class SlowBuff : Buff
{
    public int speedReduction; // 速度降低值

    public SlowBuff(int duration, int reduction)
    {
        buffName = "速度下降";
        description = $"降低速度 {reduction} 点";
        durationTurns = duration;
        speedReduction = reduction;
        element = ElementType.None;
        icon = Resources.Load<Sprite>("UI/Buffs/Icon_Slow");
    }

    public override void OnApply()
    {
        // 速度属性可能不存在于CharacterStats中，这里只记录效果
        // 实际的速度影响需要在战斗回合管理器中实现
        Debug.Log($"[速度下降] {owner.gameObject.name} 速度降低 {speedReduction}");
    }

    public override void OnRemove()
    {
        Debug.Log($"[速度下降] {owner.gameObject.name} 速度恢复正常");
    }
}
