using UnityEngine;

/// <summary>
/// 虚弱Debuff - 降低角色出战伤害（通过拦截器链，不直接修改属性）
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

    /// <summary>
    /// 出战伤害拦截器：降低角色对外造成的伤害
    /// </summary>
    public override int OnBeforeDealDamage(int rawDamage)
    {
        return Mathf.Max(rawDamage - attackReduction, 0);
    }
}
