using UnityEngine;

/// <summary>
/// 破甲Debuff - 降低目标防御力
/// </summary>
public class ArmorBreakBuff : Buff
{
    public int defenseReduction; // 防御力降低值

    public ArmorBreakBuff(int duration, int reduction)
    {
        buffName = "破甲";
        description = $"降低防御力 {reduction} 点";
        durationTurns = duration;
        defenseReduction = reduction;
        element = ElementType.None;
        icon = Resources.Load<Sprite>("UI/Buffs/Icon_ArmorBreak");
    }

    public override void OnApply()
    {
        if (owner != null)
        {
            owner.defense = Mathf.Max(owner.defense - defenseReduction, 0);
            Debug.Log($"[破甲] {owner.gameObject.name} 防御力降低 {defenseReduction}，当前防御: {owner.defense}");
        }
    }

    public override void OnRemove()
    {
        if (owner != null)
        {
            owner.defense += defenseReduction;
            Debug.Log($"[破甲] {owner.gameObject.name} 防御力恢复 {defenseReduction}，当前防御: {owner.defense}");
        }
    }
}
