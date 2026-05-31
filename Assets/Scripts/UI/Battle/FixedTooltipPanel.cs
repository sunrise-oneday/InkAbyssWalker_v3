using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 固定位置技能详情提示面板
/// 支持渐入渐出、弹性缩放、文字交叉淡入淡出动画
/// </summary>
public class FixedTooltipPanel : BasePanel
{
    [Header("CanvasGroup 控制")]
    [SerializeField] private CanvasGroup fixedTooltipGroup;
    [SerializeField] private CanvasGroup tooltipTextGroup;

    [Header("文本")]
    [SerializeField] private Text fixedTitleText;
    [SerializeField] private Text fixedCostText;
    [SerializeField] private Text fixedDescText;

    // 协程缓存，防止内存泄漏
    private Coroutine activeFadeRoutine;
    private Coroutine activeScaleRoutine;
    private Coroutine activeTextRoutine;

    private string currentActiveTitle = "";

    /// <summary>展示技能详情（防连点保护、淡入淡出、弹性缩放）</summary>
    public void Show(string title, string cost, string desc, int reqMp, int reqAp)
    {
        if (fixedTooltipGroup == null) return;

        // 停止旧协程
        KillActiveCoroutines();

        // 资源充足性检测
        string finalCostText = BuildCostText(cost, reqMp, reqAp);

        // 如果正在显示同一个面板且已完全透明，直接返回防连点
        if (currentActiveTitle == title && fixedTooltipGroup.alpha > 0.9f) return;

        bool isPanelActive = fixedTooltipGroup.gameObject.activeSelf;

        if (isPanelActive)
        {
            // 面板已激活 → 快速淡出再淡入切换内容
            currentActiveTitle = title;
            fixedTooltipGroup.alpha = 1f;
            activeTextRoutine = StartCoroutine(CrossFadeTextRoutine(title, finalCostText, desc));
        }
        else
        {
            // 面板首次打开
            currentActiveTitle = title;
            SetTextContent(title, finalCostText, desc);
            if (tooltipTextGroup != null) tooltipTextGroup.alpha = 1f;

            fixedTooltipGroup.gameObject.SetActive(true);
            activeFadeRoutine = StartCoroutine(FadeGroupRoutine(fixedTooltipGroup, 1f, 0.15f));
            activeScaleRoutine = StartCoroutine(ScalePopRoutine(fixedTooltipGroup.transform, 0.18f));
        }
    }

    /// <summary>关闭面板（平滑淡出）</summary>
    public void Hide()
    {
        if (fixedTooltipGroup == null) return;

        currentActiveTitle = "";

        if (!gameObject.activeInHierarchy || !enabled)
        {
            fixedTooltipGroup.alpha = 0f;
            fixedTooltipGroup.gameObject.SetActive(false);
            return;
        }

        KillActiveCoroutines();
        activeFadeRoutine = StartCoroutine(FadeGroupRoutine(fixedTooltipGroup, 0f, 0.12f, true));
    }

    // ============================================
    // 内部方法
    // ============================================

    private void KillActiveCoroutines()
    {
        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
        if (activeScaleRoutine != null) StopCoroutine(activeScaleRoutine);
        if (activeTextRoutine != null) StopCoroutine(activeTextRoutine);
        activeFadeRoutine = null;
        activeScaleRoutine = null;
        activeTextRoutine = null;
    }

    private string BuildCostText(string cost, int reqMp, int reqAp)
    {
        if (cost.Contains("怒气") && BattleResourceManager.Instance.sharedUltimateEnergy < BattleResourceManager.Instance.maxSharedUltimateEnergy)
            return $"<color=red>{cost} (怒气未满)</color>";
        if (BattleResourceManager.Instance.sharedMP < reqMp)
            return $"<color=red>{cost} (魔力不足！)</color>";
        if (BattleResourceManager.Instance.sharedAP < reqAp)
            return $"<color=red>{cost} (行动力不足！)</color>";
        return cost;
    }

    private void SetTextContent(string title, string cost, string desc)
    {
        if (fixedTitleText != null) fixedTitleText.text = title;
        if (fixedCostText != null) fixedCostText.text = cost;
        if (fixedDescText != null) fixedDescText.text = desc;
    }

    // ============================================
    // 协程动画
    // ============================================

    private IEnumerator FadeGroupRoutine(CanvasGroup group, float targetAlpha, float duration, bool deactivateOnComplete = false)
    {
        float startAlpha = group.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.SmoothStep(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        group.alpha = targetAlpha;
        if (deactivateOnComplete && targetAlpha <= 0.05f)
            group.gameObject.SetActive(false);
    }

    private IEnumerator ScalePopRoutine(Transform t, float duration)
    {
        Vector3 startScale = new Vector3(0.85f, 0.85f, 1f);
        Vector3 targetScale = Vector3.one;
        t.localScale = startScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / duration;
            float s = 1.70158f;
            float value = 1f - Mathf.Pow(1f - p, 3f) * (1f - p * (s + 1f) - s);
            t.localScale = Vector3.LerpUnclamped(startScale, targetScale, value);
            yield return null;
        }

        t.localScale = targetScale;
    }

    private IEnumerator CrossFadeTextRoutine(string title, string cost, string desc)
    {
        if (tooltipTextGroup == null) yield break;

        // 淡出
        float elapsed = 0f;
        float duration = 0.08f;
        float startAlpha = tooltipTextGroup.alpha;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tooltipTextGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            yield return null;
        }
        tooltipTextGroup.alpha = 0f;

        // 替换内容
        SetTextContent(title, cost, desc);

        // 淡入
        elapsed = 0f;
        duration = 0.12f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tooltipTextGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        tooltipTextGroup.alpha = 1f;
    }
}
