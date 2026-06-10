# 01 — 事件系统：ParryEvents

> 新建文件：`Assets/Scripts/Battle/Events/ParryEvents.cs`

## 职责

纯数据载体 + 安全广播。不含任何游戏逻辑，不引用任何 MonoBehaviour。
BattleCombatResolver 是唯一的发射方，特效组件是订阅方。

## ParryEventData 结构体

```csharp
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
```

### 设计决策

- **readonly struct**：值类型零 GC，每帧发射不产生堆分配
- **字段只读**：构造后不可变，订阅者之间无法互相篡改数据
- **预留字段**：Attacker / HitPoint / HitIndex / RawDamage 当前订阅者未全部使用，但未来扩展（火花方向、伤害缩放强度）无需修改签名

## ParryEvents 静态事件类

```csharp
using System;
using UnityEngine;

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
```

### SafeInvoke 要点

- `GetInvocationList()` 将多播委托拆为单个委托逐一调用
- 每个订阅者独立 try-catch，任何一个抛异常只记录日志不影响其他订阅者
- `handler == null` 短路：无订阅者时零开销

## 强制规范（P0 生命周期）

所有订阅 ParryEvents 的 MonoBehaviour **必须**：

```
OnEnable()  中  += 订阅
OnDisable() 中  -= 解绑
```

违反此规范会导致：
- MonoBehaviour 销毁后静态事件仍持有其引用
- `MissingReferenceException` 或内存泄漏
