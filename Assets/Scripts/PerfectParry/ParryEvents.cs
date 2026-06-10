using System;
using UnityEngine;

/// <summary>
/// 格挡判定事件的数据载体。
/// readonly struct 零 GC，按值传递，订阅者按需取字段。
/// </summary>
public readonly struct ParryEventData
{
    public readonly PlayerBattleEntity Defender;   // 防御方（用于定位子物体/材质）
    public readonly EnemyBattleEntity  Attacker;   // 攻击方（用于计算火花飞溅方向）
    public readonly Vector3            HitPoint;   // 受击世界坐标（粒子生成位置备用）
    public readonly int                HitIndex;   // 当前连段第几击
    public readonly int                RawDamage;  // 原始伤害（特效可按伤害缩放强度）

    public ParryEventData(
        PlayerBattleEntity defender,
        EnemyBattleEntity  attacker,
        Vector3            hitPoint,
        int                hitIndex,
        int                rawDamage)
    {
        Defender  = defender;
        Attacker  = attacker;
        HitPoint  = hitPoint;
        HitIndex  = hitIndex;
        RawDamage = rawDamage;
    }
}

/// <summary>
/// 格挡判定事件总线。
/// 发射方：BattleCombatResolver（唯一）
/// 订阅方：PerfectParry / ShaderEffectController / BattleEffectManager 等
/// </summary>
public static class ParryEvents
{
    // ---- 三种格挡结果事件 ----
    public static event Action<ParryEventData> OnPerfectParry;
    public static event Action<ParryEventData> OnNormalParry;
    public static event Action<ParryEventData> OnParryFailed;

    // ---- 安全广播（P2 异常阻断防护）----

    private static void SafeInvoke(Action<ParryEventData> handler, ParryEventData data)
    {
        if (handler == null) return;

        foreach (var d in handler.GetInvocationList())
        {
            try
            {
                ((Action<ParryEventData>)d).Invoke(data);
            }
            catch (Exception e)
            {
                // 某个订阅者爆炸只记 log，不阻断后续订阅者
                Debug.LogException(e);
            }
        }
    }

    // ---- 发射方法（仅 BattleCombatResolver 调用）----

    public static void FirePerfectParry(ParryEventData data) => SafeInvoke(OnPerfectParry, data);
    public static void FireNormalParry(ParryEventData data)  => SafeInvoke(OnNormalParry, data);
    public static void FireParryFailed(ParryEventData data)  => SafeInvoke(OnParryFailed, data);
}