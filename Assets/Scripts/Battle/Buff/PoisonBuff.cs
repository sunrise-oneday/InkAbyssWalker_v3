using UnityEngine;

/// <summary>
/// 中毒Debuff - 每回合造成递增伤害
/// </summary>
public class PoisonBuff : Buff
{
    public int baseDamage; // 基础伤害
    public int currentDamage; // 当前伤害（每回合递增）

    public PoisonBuff(int duration, int damage)
    {
        buffName = "中毒";
        description = $"每回合造成递增伤害，起始 {damage} 点";
        durationTurns = duration;
        baseDamage = damage;
        currentDamage = damage;
        element = ElementType.None;
        maxStacks = 5; // 最多叠加5层
        icon = Resources.Load<Sprite>("UI/Buffs/Icon_Poison");
    }

    public override void OnTurnStart()
    {
        if (owner != null)
        {
            owner.TakeDamage(currentDamage, 0);
            Debug.Log($"[中毒] {owner.gameObject.name} 受到 {currentDamage} 点中毒伤害");

            // 中毒伤害每回合递增
            currentDamage += baseDamage;
        }
    }

    public override void OnApply()
    {
        // 中毒叠加时，重置伤害递增
        currentDamage = baseDamage * stacks;
    }
}
