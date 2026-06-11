using System;
using UnityEngine;

/// <summary>
/// 闪避判定事件的数据载体。
/// readonly struct 零 GC，按值传递，订阅者按需取字段。
/// </summary>
public readonly struct DodgeEventData
{
    public readonly PlayerBattleEntity Defender;   // 防御方
    public readonly EnemyBattleEntity  Attacker;   // 攻击方
    public readonly Vector3            HitPoint;   // 受击世界坐标
    public readonly bool               IsPerfect;  // 是否完美闪避
    public readonly int                RawDamage;  // 原始伤害（被避免的伤害量）

    public DodgeEventData(
        PlayerBattleEntity defender,
        EnemyBattleEntity  attacker,
        Vector3            hitPoint,
        bool               isPerfect,
        int                rawDamage)
    {
        Defender   = defender;
        Attacker   = attacker;
        HitPoint   = hitPoint;
        IsPerfect  = isPerfect;
        RawDamage  = rawDamage;
    }
}

/// <summary>
/// 闪避判定事件总线。
/// 发射方：BattleCombatResolver（唯一）
/// 订阅方：BattleSFXHandler 等
/// </summary>
public static class DodgeEvents
{
    // ---- 两种闪避结果事件 ----
    public static event Action<DodgeEventData> OnPerfectDodge;
    public static event Action<DodgeEventData> OnNormalDodge;

    // ---- 安全广播（P2 异常阻断防护）----

    private static void SafeInvoke(Action<DodgeEventData> handler, DodgeEventData data)
    {
        if (handler == null) return;

        foreach (var d in handler.GetInvocationList())
        {
            try
            {
                ((Action<DodgeEventData>)d).Invoke(data);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    // ---- 发射方法（仅 BattleCombatResolver 调用）----

    public static void FirePerfectDodge(DodgeEventData data) => SafeInvoke(OnPerfectDodge, data);
    public static void FireNormalDodge(DodgeEventData data)  => SafeInvoke(OnNormalDodge, data);
}
