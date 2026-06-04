using UnityEngine;

/// <summary>
/// 脆弱Debuff - 降低角色获得护盾的效果（杀戮尖塔风格）
/// 默认降低 25% 护盾获取量
/// </summary>
public class FrailBuff : Buff
{
    private float shieldMultiplier; // 护盾获取倍率（默认0.75 = 减少25%）

    /// <param name="duration">持续回合数</param>
    /// <param name="percentReduction">护盾减少百分比，如 25 表示减少 25%（默认 25）</param>
    public FrailBuff(int duration, int percentReduction = 25)
    {
        buffName = "脆弱";
        durationTurns = duration;
        shieldMultiplier = 1f - percentReduction / 100f;
        description = $"获得的护盾降低至 {Mathf.RoundToInt(shieldMultiplier * 100)}%";
        element = ElementType.None;
        icon = Resources.Load<Sprite>("UI/Buffs/Icon_Frail");
    }

    /// <summary>
    /// 护盾增益拦截器：降低护盾获取量
    /// </summary>
    public override int OnBeforeGainShield(int baseShield)
    {
        return Mathf.Max(Mathf.RoundToInt(baseShield * shieldMultiplier), 0);
    }
}
