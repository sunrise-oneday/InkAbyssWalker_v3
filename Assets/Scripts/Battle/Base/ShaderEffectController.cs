using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// 挂到 SpriteRenderer 所在 GameObject 上（即 EntityBase 的 sprite 子物体）。
/// 负责驱动 GeneralCharacterEffect.shader 的受伤闪光和死亡消融效果。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ShaderEffectController : MonoBehaviour
{
    // ---- 缓存 ----
    private SpriteRenderer _sprite;
    private Material _materialInstance; // sprite.material 自动复制的实例

    // ---- shader 属性 ID（避免字符串拼接开销）----
    private static readonly int ID_FlashColor = Shader.PropertyToID("_FlashColor");
    private static readonly int ID_FlashIntensity = Shader.PropertyToID("_FlashIntensity");
    private static readonly int ID_DissolveProgress = Shader.PropertyToID("_DissolveProgress");
    private static readonly int ID_DissolveTex_ST = Shader.PropertyToID("_DissolveTex_ST"); //消融缩放偏移 ID

    private const string KW_INJURED = "_INJURED_ON";
    private const string KW_DEATH = "_DEATH_ON";

    // ---- 运行时状态 ----
    private Coroutine _activeEffect; // 防止重叠调用

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
        // 首次访问 .material 会自动为这个 renderer 创建材质实例副本
        _materialInstance = _sprite.material;
    }

    private void SetMaterialVector(int propertyID, Vector4 value)
    {
        if (_materialInstance != null)
        {
            _materialInstance.SetVector(propertyID, value);
        }
    }

    // ============================================================
    // 公开 API
    // ============================================================

    /// <summary>播放受伤闪光（非致死）</summary>
    public void PlayHitFlash(float duration = 0.15f, Color? flashColor = null)
    {
        if (_activeEffect != null) StopCoroutine(_activeEffect);
        _activeEffect = StartCoroutine(HitFlashRoutine(duration, flashColor ?? Color.red));
    }

    /// <summary>播放死亡序列：先闪 → 再消融 → 回调</summary>
    public void PlayDeathSequence(
        float flashDuration = 0.15f,
        float dissolveDuration = 1.0f,
        Color? flashColor = null,
        Action onComplete = null)
    {
        if (_activeEffect != null) StopCoroutine(_activeEffect);
        _activeEffect = StartCoroutine(DeathSequenceRoutine(flashDuration, dissolveDuration, flashColor ?? Color.red, onComplete));
    }

    /// <summary>重置所有效果（战斗重置/对象池回收时调用）</summary>
    public void ResetEffect()
    {
        if (_activeEffect != null)
        {
            StopCoroutine(_activeEffect);
            _activeEffect = null;
        }
        _materialInstance.DisableKeyword(KW_INJURED);
        _materialInstance.DisableKeyword(KW_DEATH);
        _materialInstance.SetFloat(ID_DissolveProgress, 0f);
        _materialInstance.SetFloat(ID_FlashIntensity, 0f);
    }

    private void OnDestroy()
    {
        // 清理材质实例，避免内存泄漏
        if (_materialInstance != null)
            Destroy(_materialInstance);
    }

    // ============================================================
    // 协程
    // ============================================================

    private IEnumerator HitFlashRoutine(float duration, Color color)
    {
        // 确保死亡 keyword 关闭
        _materialInstance.DisableKeyword(KW_DEATH);
        _materialInstance.SetFloat(ID_DissolveProgress, 0f);

        // 设置闪光颜色和强度
        _materialInstance.SetColor(ID_FlashColor, color);
        _materialInstance.SetFloat(ID_FlashIntensity, 0.4f);

        // 开启受伤闪光
        _materialInstance.EnableKeyword(KW_INJURED);

        yield return new WaitForSeconds(duration);

        // 关闭受伤闪光
        _materialInstance.DisableKeyword(KW_INJURED);
        _activeEffect = null;
    }

    private IEnumerator DeathSequenceRoutine(float flashDuration, float dissolveDuration, Color flashColor, Action onComplete)
    {
        // ---- Phase 1：受伤闪光 ----
        _materialInstance.DisableKeyword(KW_DEATH);
        _materialInstance.SetFloat(ID_DissolveProgress, 0f);

        _materialInstance.SetColor(ID_FlashColor, flashColor);
        _materialInstance.SetFloat(ID_FlashIntensity, 0.4f);
        _materialInstance.EnableKeyword(KW_INJURED);

        yield return new WaitForSeconds(flashDuration);

        // ---- Phase 2：关闭闪光，开始消融 ----
        _materialInstance.DisableKeyword(KW_INJURED);
        _materialInstance.EnableKeyword(KW_DEATH);

        // 核心改进：在消融的第一帧，给敌人随机一个消融贴图的 Offset 偏移量
        // 这能完全避开同屏多个怪堆叠死亡时，消融边缘重合导致的“糊成一团”问题
        Vector4 randomST = new Vector4(
            UnityEngine.Random.Range(0.9f, 1.1f), // 缩放轻微变化
            UnityEngine.Random.Range(0.9f, 1.1f),
            UnityEngine.Random.Range(0f, 100f),   // 随机 X 轴偏移
            UnityEngine.Random.Range(0f, 100f)    // 随机 Y 轴偏移
        );
        SetMaterialVector(ID_DissolveTex_ST, randomST);

        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / dissolveDuration);
            _materialInstance.SetFloat(ID_DissolveProgress, progress);
            yield return null;
        }

        // 确保到达终值
        _materialInstance.SetFloat(ID_DissolveProgress, 1f);
        _activeEffect = null;

        // ---- 消融完成 → 通知调用方 ----
        onComplete?.Invoke();
    }
}
