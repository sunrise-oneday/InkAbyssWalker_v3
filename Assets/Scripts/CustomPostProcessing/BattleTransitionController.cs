using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class BattleTransitionController : MonoBehaviour
{
    public Volume globalVolume;//全局Volume
    [Header("转场手感调整")]
    public float hitStopTime = 0.15f;
    public float transitionDuration = 0.8f;
    // 使用非线性曲线，建议设置为"先慢后极快"的抛物线，营造吸入感
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private BattleTransitionEffect m_TransitionEffect;
    void Start()
    {
        // 从 Volume 中获取我们的自定义后处理组件
        if (globalVolume.profile.TryGet(out BattleTransitionEffect effect))
        {
            m_TransitionEffect = effect;
            m_TransitionEffect.progress.value = 0f; // 初始化为0
        }
    }

    // 可以在触发遇敌时调用此方法（无回调，内部自动调用 LoadBattleScene）
    public void TriggerEncounter()
    {
        TriggerEncounter(null);
    }

    /// <summary>触发遇敌转场，完成时回调 onComplete（可空，为空时自动调用 LoadBattleScene）</summary>
    public void TriggerEncounter(System.Action onComplete)
    {
        if (m_TransitionEffect == null) return;
        StartCoroutine(EncounterRoutine(onComplete));
    }

    private IEnumerator EncounterRoutine(System.Action onComplete = null)
    {
        // 1. 顿帧阶段 (Hit-stop)
        // 将时间流速降至接近0（不要完全0，防止某些依赖真实时间的逻辑卡死，或者直接用0但用WaitForSecondsRealtime）
        Time.timeScale = 0.05f;
        BattleSFXHandler.Instance?.PlaySFX(SFXKey.BattleEncounter); // 转场冲击音效（短促定格音）

        // 等待现实时间度过顿帧期
        yield return new WaitForSecondsRealtime(hitStopTime);

        // 2. 空间撕裂/拉取阶段 (驱动后处理，全程保持低流速)
        BattleSFXHandler.Instance?.PlaySFX(SFXKey.BattleEncounterRise); // 转场上升音效（持续渐强音）
        float elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.unscaledDeltaTime; // 依然使用不受TimeScale影响的时间
            float t = elapsedTime / transitionDuration;

            // 使用动画曲线计算当前的表现进度
            float curveValue = transitionCurve.Evaluate(t);

            // 写入 Volume 参数，这会被你的 CustomRenderFeature 自动提取并渲染
            m_TransitionEffect.progress.Override(curveValue);

            yield return null;
        }

        m_TransitionEffect.progress.Override(1f);

        // 3. 画面此时已经被完全拉伸和模糊遮蔽
        if (onComplete != null)
        {
            // 有回调时，由调用方决定后续操作
            onComplete.Invoke();
        }
        else
        {
            // 无回调时，兼容原有逻辑：自动加载战斗场景
            LoadBattleScene();
        }
    }

    public void ResetTransition()
    {
        if (m_TransitionEffect != null)
        {
            m_TransitionEffect.progress.Override(0f);
        }
    }

    private void LoadBattleScene()
    {
        Debug.Log("画面转场完毕，开始加载战斗场景...");
        // UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("BattleScene");

        // 注意：在加载新场景并初始化完毕后，记得将 m_TransitionEffect.progress 恢复为 0
    }
}
