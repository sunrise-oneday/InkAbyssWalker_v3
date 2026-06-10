# 05 — 订阅者 C：BattleEffectManager.cs（顿帧 & 镜头）

> 修改文件：`Assets/Scripts/Core/BattleEffectManager.cs`
> 改动量：+65 行，现有 ShakeCamera/WitchTime/HitStop 均加协程缓存

## 职责

新增 `PerfectParryFreeze`：TimeScale=0.01，持续 0.4s 真实时间。
新增 `PerfectParryShake`：基于 `Time.unscaledDeltaTime` 的短促微抖（替代受 TimeScale 影响的 ShakeCamera）。
同时解决现有 TimeScale 协程之间的互斥问题。

## 核心代码逻辑

### P0 生命周期管理（新增）

```csharp
private void OnEnable()
{
    ParryEvents.OnPerfectParry += HandlePerfectParryFreeze;
}

private void OnDisable()
{
    ParryEvents.OnPerfectParry -= HandlePerfectParryFreeze;
}
```

### 事件响应（新增）

```csharp
private void HandlePerfectParryFreeze(ParryEventData data)
{
    // ★ P0 修正：先停止现有 CameraShake（避免顿帧后"延迟爆发"）
    // ApplyDamageFeedback 中调用的 ShakeCamera(0.2f, 0.25f) 使用 Time.deltaTime 计时，
    // 在 0.01x TimeScale 下几乎不推进，0.4s 真时后剩余进度集中爆发会导致剧烈抖动。
    // 此处中断旧的抖动协程，改用 unscaledDeltaTime 驱动的短促微抖。
    if (_activeCameraShake != null)
    {
        StopCoroutine(_activeCameraShake);
        _activeCameraShake = null;
    }

    PerfectParryFreeze(0.4f);
    PerfectParryShake(0.15f, 0.08f); // unscaledDeltaTime 驱动，不受顿帧影响
}
```

### 精准防御极限顿帧（新增 region）

```csharp
#region 精准防御顿帧（unscaledDeltaTime 微抖 + TimeScale 顿帧）

private Coroutine _activePerfectParryFreeze;

/// <summary>
/// 精准防御专用顿帧：TimeScale=0.01，持续指定真实时长后恢复。
/// 与 HitStop/WitchTime 互斥——先中断正在进行的时间缩放再覆盖。
/// </summary>
public void PerfectParryFreeze(float realDuration)
{
    StopAllTimeScaleCoroutines();
    _activePerfectParryFreeze = StartCoroutine(PerfectParryFreezeRoutine(realDuration));
}

private IEnumerator PerfectParryFreezeRoutine(float realDuration)
{
    Time.timeScale = 0.01f;
    yield return new WaitForSecondsRealtime(realDuration);
    Time.timeScale = 1.0f;
    _activePerfectParryFreeze = null;
}

/// <summary>
/// 完美格挡专用镜头微抖，使用 unscaledDeltaTime 驱动，不受 TimeScale=0.01 冻结影响。
/// 替代 ShakeCamera（其使用 Time.deltaTime，在顿帧期间被冻住会导致延迟爆发）。
/// </summary>
public void PerfectParryShake(float duration, float magnitude)
{
    _activeCameraShake = StartCoroutine(PerfectParryShakeRoutine(duration, magnitude));
}

private Coroutine _activeCameraShake; // ★ 新增字段，缓存 CameraShake 协程句柄

private IEnumerator PerfectParryShakeRoutine(float duration, float magnitude)
{
    Camera battleCam = Camera.main;
    if (battleCam == null) yield break;

    // ★ 注意：originalPos 在协程开始时快照一次。
    // 如果相机在顿帧期间还有动态跟随逻辑，originalPos 会是抖动开始时的位置，
    // 结束后会硬 snap 回去。当前战斗相机无动态跟随，不受影响。
    Vector3 originalPos = battleCam.transform.position;
    float elapsed = 0f;

    while (elapsed < duration)
    {
        float x = Random.Range(-1f, 1f) * magnitude;
        float y = Random.Range(-1f, 1f) * magnitude;
        battleCam.transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
        elapsed += Time.unscaledDeltaTime; // ★ 关键：不受 TimeScale 影响
        yield return null;
    }

    battleCam.transform.position = originalPos;
    _activeCameraShake = null;
}

#endregion
```

### TimeScale 互斥机制（新增）

```csharp
private Coroutine _activeWitchTime;   // ★ 新增缓存
private Coroutine _activeHitStop;     // ★ 新增缓存

/// <summary>
/// 中断所有正在进行的时间缩放协程。
/// ⚠️ P1 修正：绝对不在此处重置 Time.timeScale。
/// 原因：重置为 1.0 会导致 0.05→1.0→0.01 的瞬时闪回，
/// FixedUpdate（物理）和 Animator（动画状态机）对 TimeScale 跳变敏感，
/// 可能引发碰撞穿透或动画采样跳帧。让新协程直接覆盖 timeScale 即可。
/// </summary>
private void StopAllTimeScaleCoroutines()
{
    if (_activeWitchTime != null)
    {
        StopCoroutine(_activeWitchTime);
        _activeWitchTime = null;
    }
    if (_activeHitStop != null)
    {
        StopCoroutine(_activeHitStop);
        _activeHitStop = null;
    }
    if (_activePerfectParryFreeze != null)
    {
        StopCoroutine(_activePerfectParryFreeze);
        _activePerfectParryFreeze = null;
    }
    // ★ P1 修正：删除 Time.timeScale = 1.0f
}
```

### 现有方法改动（仅加协程缓存 + 互斥调用）

```csharp
// ShakeCamera 改动 ★ P0 关键：
// 原实现直接用 StartCoroutine(CameraShakeRoutine(...))，不缓存句柄。
// 导致 HandlePerfectParryFreeze 中的 _activeCameraShake 中断永远无法生效。
// 修正后缓存句柄，支持 P0 镜头抖动中断。
public void ShakeCamera(float duration, float magnitude)
{
    if (_activeCameraShake != null) StopCoroutine(_activeCameraShake); // ★ 新增：中断旧抖动
    _activeCameraShake = StartCoroutine(CameraShakeRoutine(duration, magnitude)); // ★ 缓存句柄
}

private IEnumerator CameraShakeRoutine(float duration, float magnitude)
{
    Camera battleCam = Camera.main;
    if (battleCam == null) { _activeCameraShake = null; yield break; }

    Vector3 originalPos = battleCam.transform.position;
    float elapsed = 0f;

    while (elapsed < duration)
    {
        float x = Random.Range(-1f, 1f) * magnitude;
        float y = Random.Range(-1f, 1f) * magnitude;
        battleCam.transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
        elapsed += Time.deltaTime;
        yield return null;
    }

    battleCam.transform.position = originalPos;
    _activeCameraShake = null;                                         // ★ 新增清理
}

// WitchTime 改动：
public void WitchTime(float duration)
{
    StopAllTimeScaleCoroutines();                                      // ★ 新增
    _activeWitchTime = StartCoroutine(WitchTimeRoutine(duration));     // ★ 缓存句柄
}

private IEnumerator WitchTimeRoutine(float duration)
{
    Time.timeScale = 0.2f;
    yield return new WaitForSecondsRealtime(duration);
    Time.timeScale = 1.0f;
    _activeWitchTime = null;                                           // ★ 新增清理
}

// HitStop 改动：
public void HitStop(float duration)
{
    StopAllTimeScaleCoroutines();                                      // ★ 新增
    _activeHitStop = StartCoroutine(HitStopRoutine(duration));         // ★ 缓存句柄
}

private IEnumerator HitStopRoutine(float duration)
{
    Time.timeScale = 0.05f;
    yield return new WaitForSecondsRealtime(duration);
    Time.timeScale = 1.0f;
    _activeHitStop = null;                                             // ★ 新增清理
}
```

## 设计要点

### TimeScale 互斥

同一时刻只能有一个时间缩放效果生效。后触发的效果通过 `StopAllTimeScaleCoroutines` 自动中断前一个。

实际时序：完美格挡时，`ApplyDamageFeedback` 先触发 `HitStop(0.06s)`，紧接着事件触发 `PerfectParryFreeze(0.4s)`，后者自动覆盖前者。无需改动 Resolver 中的现有代码。

### P0 镜头抖动"延迟爆发"修正

**根因**：现有 `ShakeCamera(CameraShakeRoutine)` 第 76 行使用 `Time.deltaTime` 累加计时。在 0.01x TimeScale 下 deltaTime ≈ 0.00016s，0.4s 真实时间内进度仅推进 ~0.004s。TimeScale 恢复为 1.0 后，剩余 ~0.196s 的随机偏移会集中爆发。

**后果**：玩家在 0.4s 顿帧结束后、准备输入反击指令的黄金窗口内，屏幕剧烈抖动，严重破坏视觉锁定和操作手感。

**修正**：
1. `HandlePerfectParryFreeze` 中先中断现有的 `ShakeCamera` 协程（通过 `_activeCameraShake` 缓存句柄）
2. 用 `PerfectParryShake` 替代—它使用 `Time.unscaledDeltaTime` 驱动，0.15s 的微抖在顿帧期间照常完成
3. 参数 `(0.15f, 0.08f)` 比原有的 `(0.25f, 0.3f)` 更短促更微妙，不过度抢戏

### P1 TimeScale "闪回"修正

`StopAllTimeScaleCoroutines` 中删除 `Time.timeScale = 1.0f`。新协程会直接覆盖 timeScale，无需中间重置。删除后避免 `0.05→1.0→0.01` 跳变引发的 FixedUpdate 碰撞穿透和 Animator 采样跳帧。

### FlashColor 冻住是正面效果

`ApplyDamageFeedback` 中 `FlashColor(Color.cyan, 0.5f)` 调用的是 `PlayHitFlash(0.5f)`，其内部 `HitFlashRoutine`（`ShaderEffectController.cs:103`）使用 `WaitForSeconds(duration)`（受 TimeScale 影响）。在 0.01x 下实际持续约 50s。

这反而是**正面效果**：青色的完美格挡闪光在 0.4s 顿帧期间保持满强度，顿帧结束后才在约 0.5s 内自然淡出。视觉效果接近街霸 6 的 Drive Parry 定格 + 蓝色特效冻结。

> ⚠ **实现时注意**：此行为隐式依赖 `HitFlashRoutine` 使用 `WaitForSeconds`（而非 `WaitForSecondsRealtime`）。实现时应在 `HitFlashRoutine` 上方加注释标记此依赖，防止日后被人"顺手修复"为 `WaitForSecondsRealtime` 而破坏完美格挡的视觉效果。
