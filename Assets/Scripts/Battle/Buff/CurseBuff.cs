using UnityEngine;

/// <summary>
/// 诅咒Debuff - 每回合造成固定伤害
/// </summary>
public class CurseBuff : Buff
{
    public int damagePerTurn; // 每回合伤害

    public CurseBuff(int duration, int damage)
    {
        buffName = "诅咒";
        description = $"每回合造成 {damage} 点伤害";
        durationTurns = duration;
        damagePerTurn = damage;
        element = ElementType.None;
        icon = Resources.Load<Sprite>("UI/Buffs/Icon_Curse");
    }

    public override void OnTurnStart()
    {
        if (owner != null)
        {
            owner.TakeDamage(damagePerTurn, 0);
            Debug.Log($"[诅咒] {owner.gameObject.name} 受到 {damagePerTurn} 点诅咒伤害");
        }
    }
}
