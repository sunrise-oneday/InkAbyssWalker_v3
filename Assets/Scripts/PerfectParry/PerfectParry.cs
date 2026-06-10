using System.Collections;
using UnityEngine;

/// <summary>
/// 完美格挡粒子控制器（纯特效，不含战斗逻辑）。
/// 改为按需实例化模式：由 BattleEffectManager 在完美格挡触发时实例化到玩家 eff 子物体，
/// 播放完粒子序列后自动销毁自身。
/// 子物体结构：SplashParticle / BrustParticle / EndParticle。
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

        // 确保初始不播放
        StopAllParticles(clearImmediate: false);
    }

    // ========== 播放逻辑 ==========

    /// <summary>播放一次完整粒子序列，播完后自动销毁自身</summary>
    /// <remarks>
    /// 渐进式喷洒效果：Brust(0s) → 0.15s → Splash → 0.15s → End
    /// </remarks>
    public void PlayAndDestroy()
    {
        if (isPlaying) return;
        isPlaying = true;

        // 清理旧残留粒子（立即清除），然后从头播放
        StopAllParticles(clearImmediate: true);

        if (finishCoroutine != null) StopCoroutine(finishCoroutine);
        finishCoroutine = StartCoroutine(SequenceAndDestroy());
    }

    private IEnumerator SequenceAndDestroy()
    {
        // 第 1 段：Brust 爆发
        if (brustParticle != null) brustParticle.Play();
        yield return new WaitForSecondsRealtime(0.15f);

        // 第 2 段：Splash 溅射
        if (splashParticle != null) splashParticle.Play();
        yield return new WaitForSecondsRealtime(0.15f);

        // 第 3 段：End 收尾
        if (endParticle != null) endParticle.Play();

        // 等待粒子自然消散（最长的 EndParticle 约 3.23s）
        yield return new WaitForSecondsRealtime(3.2f);

        Finish();
        Destroy(gameObject);
    }

    /// <summary>
    /// 停止发射新粒子，已生成的粒子自然消散（不强制清除）。
    /// </summary>
    private void Finish()
    {
        StopAllParticles(clearImmediate: false);
        isPlaying = false;
        finishCoroutine = null;
    }

    // ========== 工具方法 ==========

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
