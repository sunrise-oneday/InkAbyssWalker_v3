# 04 — 订阅者 B：ShaderEffectController.cs（Shader 发光）

> 修改文件：`Assets/Scripts/Battle/Base/ShaderEffectController.cs`
> 改动量：+40 行，不改动现有方法逻辑

## 职责

管理 `GeneralCharacterEffect.shader` 的 `_PARRY_DEFENDER_ON` keyword。
订阅 `OnPerfectParry` 和 `OnNormalParry`，发光 0.3s（真实时间）后自动关闭。

## Shader 端现状（无需修改）

`GeneralCharacterEffect.shader` 已内置完整的精准防御发光逻辑：

```hlsl
// 第 27 行：keyword 声明
[Toggle(_PARRY_DEFENDER_ON)] _ParryDefenderKeyword ("防御者发光效果", Float) = 0

// 第 85 行：编译开关
#pragma shader_feature_local _PARRY_DEFENDER_ON

// 第 123-137 行：发光计算
#ifdef _PARRY_DEFENDER_ON
    // 四方向采样 → 内发光因子 → 叠加 _ParryGlowColor → 全局荧光覆盖
#endif
```

C# 端只需要在正确时机 EnableKeyword / DisableKeyword 即可。

## 核心代码逻辑（新增部分）

```csharp
// ================ 新增常量 ================
private const string KW_PARRY_DEFENDER = "_PARRY_DEFENDER_ON";
private const float ParryGlowDuration = 0.3f;

// ================ 新增运行时状态 ================
// 独立协程句柄，与现有 _activeEffect（受伤闪光/死亡消融）互不干扰
private Coroutine _activeParryGlow;

// ================ P0 生命周期管理（新增方法）================

private void OnEnable()
{
    ParryEvents.OnPerfectParry += HandleParryGlow;
    ParryEvents.OnNormalParry  += HandleParryGlow;
}

private void OnDisable()
{
    ParryEvents.OnPerfectParry -= HandleParryGlow;
    ParryEvents.OnNormalParry  -= HandleParryGlow;
}

// ================ 事件响应（新增方法）================

private void HandleParryGlow(ParryEventData data)
{
    // 安全校验：确认事件中的 defender 身上的 sprite 子物体就是自己
    // ShaderEffectController 挂在 EntityBase.sprite（SpriteRenderer）所在的 GameObject 上
    if (data.Defender == null) return;
    if (data.Defender.sprite == null || data.Defender.sprite.gameObject != gameObject) return;

    PlayParryDefenderGlow();
}

// ================ 核心发光逻辑（新增方法）================

/// <summary>启用精准防御发光，0.3s 真实时间后自动关闭</summary>
public void PlayParryDefenderGlow()
{
    if (_activeParryGlow != null) StopCoroutine(_activeParryGlow);
    _activeParryGlow = StartCoroutine(ParryGlowRoutine());
}

private IEnumerator ParryGlowRoutine()
{
    _materialInstance.EnableKeyword(KW_PARRY_DEFENDER);

    // 用 WaitForSecondsRealtime，不受顿帧 TimeScale=0.01 影响
    yield return new WaitForSecondsRealtime(ParryGlowDuration);

    _materialInstance.DisableKeyword(KW_PARRY_DEFENDER);
    _activeParryGlow = null;
}

// ================ ResetEffect() 追加清理（修改现有方法）================

public void ResetEffect()
{
    // ... 现有清理逻辑保持不变 ...

    // ★ 新增：清理精准防御发光
    _materialInstance.DisableKeyword(KW_PARRY_DEFENDER);
    if (_activeParryGlow != null)
    {
        StopCoroutine(_activeParryGlow);
        _activeParryGlow = null;
    }
}
```

## 设计要点

### 完美和普通共用同一个 handler

两种格挡触发相同的发光效果（shader 逻辑一样），所以 `HandleParryGlow` 同时订阅 `OnPerfectParry` 和 `OnNormalParry`。

### 独立协程句柄

`_activeParryGlow` 与现有的 `_activeEffect`（受伤闪光/死亡消融）完全独立。
完美格挡发光和受伤闪光可以同时存在，互不干扰。

### 身份匹配

通过 `data.Defender.sprite.gameObject != gameObject` 确认事件对应的是自己所在的实体。
`EntityBase.sprite` 是 `public get; protected set;`（EntityBase.cs:13），可以安全读取。

### WaitForSecondsRealtime

与粒子控制器同理，顿帧期间发光计时不被冻住，0.3s 真实时间后准时关闭。
