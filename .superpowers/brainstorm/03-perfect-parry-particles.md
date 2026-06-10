# 03 — 订阅者 A：PerfectParry.cs（粒子控制器）

> 重写文件：`Assets/Shaders/PerfectParry.cs`
> 原文件为空壳，完全重写为 ~90 行

## 职责

纯粒子控制器。仅订阅 `OnPerfectParry`，播放三个子粒子、播完自动停止。
不含任何战斗逻辑、不控制 TimeScale、不控制 Shader keyword。

## 挂载位置

```
Player (GameObject)
  └── PerfectParry (GameObject) ← 本脚本挂这里，始终 active
        ├── SplashParticle (ParticleSystem, Duration=0.5s, StartLifetime=0.5s, PlayOnAwake=false)
        ├── BrustParticle  (ParticleSystem, Duration=0.5s, StartLifetime=0.5s, PlayOnAwake=false)
        └── EndParticle    (ParticleSystem, Duration=0.5s, StartLifetime=3.23s, PlayOnAwake=false)
```

## 核心代码逻辑

```csharp
using System.Collections;
using UnityEngine;

/// <summary>
/// 完美格挡粒子控制器（纯特效，不含战斗逻辑）。
/// 挂载位置：Player → PerfectParry 子物体（始终 active，粒子不播放时自然不可见）。
/// 子物体结构：SplashParticle / BrustParticle / EndParticle。
/// 仅订阅 OnPerfectParry，普通格挡不触发粒子。
/// </summary>
public class PerfectParry : MonoBehaviour
{
    // ---- 缓存三个粒子系统（Awake 时自动查找）----
    private ParticleSystem splashParticle;
    private ParticleSystem brustParticle;
    private ParticleSystem endParticle;

    private bool isPlaying;
    private Coroutine finishCoroutine;

    private void Awake()
    {
        splashParticle = transform.Find("SplashParticle")?.GetComponent<ParticleSystem>();
        brustParticle  = transform.Find("BrustParticle")?.GetComponent<ParticleSystem>();
        endParticle    = transform.Find("EndParticle")?.GetComponent<ParticleSystem>();

        // 强制三个粒子使用 UnscaledTime，顿帧期间照常播放
        ForceUnscaledTime(splashParticle);
        ForceUnscaledTime(brustParticle);
        ForceUnscaledTime(endParticle);

        // 确保初始不播放（粒子不播放时自然不可见，无需 SetActive）
        StopAllParticles(clearImmediate: false);
    }

    // ========== P0 生命周期管理 ==========
    // GameObject 始终 active → OnEnable 在场景加载时正常触发
    // OnDisable 在场景卸载或物体销毁时正常触发
    // 严格配对，无泄漏风险

    private void OnEnable()
    {
        ParryEvents.OnPerfectParry += HandlePerfectParry;
    }

    private void OnDisable()
    {
        ParryEvents.OnPerfectParry -= HandlePerfectParry;
    }

    // ========== 事件响应 ==========

    private void HandlePerfectParry(ParryEventData data)
    {
        // 安全校验：确认事件中的 defender 就是自己的父物体
        if (data.Defender == null || data.Defender.gameObject != transform.parent?.gameObject)
            return;

        Play();
    }

    // ========== 播放逻辑 ==========

    public void Play()
    {
        if (isPlaying) return;
        isPlaying = true;

        // 清理旧残留粒子（立即清除），然后从头播放
        StopAllParticles(clearImmediate: true);

        if (splashParticle != null) splashParticle.Play();
        if (brustParticle  != null) brustParticle.Play();
        if (endParticle    != null) endParticle.Play();

        // 最大粒子 StartLifetime=3.23s（EndParticle），留余量 0.27s
        // 用 WaitForSecondsRealtime，不受 TimeScale=0.01 冻结影响
        if (finishCoroutine != null) StopCoroutine(finishCoroutine);
        finishCoroutine = StartCoroutine(FinishAfterRealtime(3.5f));
    }

    private IEnumerator FinishAfterRealtime(float realSeconds)
    {
        yield return new WaitForSecondsRealtime(realSeconds);
        Finish();
    }

    /// <summary>
    /// 停止发射新粒子，已生成的粒子自然消散（不强制清除）。
    /// P2 修正：不能用 StopEmittingAndClear 瞬间抹除已生成的粒子，
    /// EndParticle 的 StartLifetime=3.23s，强行清除会产生视觉切断。
    /// </summary>
    private void Finish()
    {
        StopAllParticles(clearImmediate: false);
        isPlaying = false;
        finishCoroutine = null;
    }

    // ========== 工具方法 ==========

    /// <summary>
    /// 停止所有粒子。
    /// </summary>
    /// <param name="clearImmediate">
    /// true  = 立即清除所有已生成粒子（Play() 开头清理旧残留用）
    /// false = 停止发射新粒子，已生成的粒子自然播放到生命周期结束（Finish() 用）
    /// </param>
    private void StopAllParticles(bool clearImmediate)
    {
        var behavior = clearImmediate
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting;

        if (splashParticle != null) splashParticle.Stop(true, behavior);
        if (brustParticle  != null) brustParticle.Stop(true, behavior);
        if (endParticle    != null) endParticle.Stop(true, behavior);
    }

    private static void ForceUnscaledTime(ParticleSystem ps)
    {
        if (ps == null) return;
        var main = ps.main;
        main.useUnscaledTime = true;
    }
}
```

## 设计要点

### 不用 SetActive，粒子即控制可见性

粒子不播放时自然不渲染任何东西，不需要通过 SetActive(true/false) 来控制可见性。
这彻底规避了 SetActive 带来的生命周期问题：

- **避免的坑**：如果在 Awake 中 SetActive(false)，Unity 不会调用 OnEnable，事件订阅永远不会发生
- **避免的坑**：反复 SetActive 切换会导致 OnEnable/OnDisable 反复触发，增加不必要的订阅/解绑开销

### P2 粒子"视觉切断"修正

**根因**：EndParticle 的 `StartLifetime = 3.23s`，远超最初设想的 0.5s。如果在 3.5s 时用 `StopEmittingAndClear`，会瞬间抹除所有半空中的粒子尾迹，产生明显视觉切断。

**修正**：
- `Play()` 开始时用 `clearImmediate: true`（`StopEmittingAndClear`）— 清理上次可能残留的粒子是合理的
- `Finish()` 结束时用 `clearImmediate: false`（`StopEmitting`）— 停止发射新粒子，已生成的粒子自然消散
- `waitTime` 从 0.6s 改为 3.5s（EndParticle StartLifetime 3.23s + 0.27s 余量），确保协程在粒子完全消散后才标记 `isPlaying = false`

### WaitForSecondsRealtime

3.5s 后的 Finish 使用 `WaitForSecondsRealtime`，不受 TimeScale=0.01 冻结影响。3.5s 真实时间后准时调用 Finish。

### isPlaying 防重入

连续两次完美格挡间隔 < 3.5s 时，第二次 Play() 会被 isPlaying 挡住。
实际战斗中连续完美格挡的间隔通常 > 0.8s（敌人连击间隔），3.5s 足够保险。

> **设计决策**：敌人多段连击（如 3 连击）时，若玩家全部完美格挡，第 2、3 段 hit 的粒子会被 isPlaying 挡掉。这不是 bug，而是**主动选择**——连续爆粒子视觉噪音过大，且连续完美格挡应靠顿帧和闪光传达，不需要粒子叠加。如需支持更短间隔的连续触发，可将 isPlaying 改为在旧协程中断后立即放开。
