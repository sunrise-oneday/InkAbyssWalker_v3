using UnityEngine;

/// <summary>
/// 虚弱Debuff - 降低目标攻击力
/// </summary>
public class WeakenBuff : Buff
{
    public int attackReduction; // 攻击力降低值

    public WeakenBuff(int duration, int reduction)
    {
        buffName = "虚弱";
        description = $"降低攻击力 {reduction} 点";
        durationTurns = duration;
        attackReduction = reduction;
        element = ElementType.None;
        icon = Resources.Load<Sprite>("UI/Buffs/Icon_Weaken");
    }

    public override void OnApply()
    {
        if (owner != null)
        {
            owner.attack = Mathf.Max(owner.attack - attackReduction, 0);
            Debug.Log($"[虚弱] {owner.gameObject.name} 攻击力降低 {attackReduction}，当前攻击: {owner.attack}");
        }
    }

    public override void OnRemove()
    {
        if (owner != null)
        {
            owner.attack += attackReduction;
            Debug.Log($"[虚弱] {owner.gameObject.name} 攻击力恢复 {attackReduction}，当前攻击: {owner.attack}");
        }
    }
}
