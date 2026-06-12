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
    private const string KW_PARRY_DEFENDER = "_PARRY_DEFENDER_ON";
    private const float ParryGlowDuration = 0.3f;

    // ---- 闪避 (Dodge) ----
    private static readonly int ID_DodgeProgress = Shader.PropertyToID("_DodgeProgress");
    private static readonly int ID_ParryGlowColor = Shader.PropertyToID("_ParryGlowColor");
    private const string KW_DODGE = "_DODGE_ON";
    private const float DodgeGlowDuration = 0.25f;
    // 普通闪避发光颜色（区别于精准防御的蓝色）
    private static readonly Color NormalDodgeGlowColor = new Color(0.2f, 1f, 0.4f, 1f);

    // ---- 运行时状态 ----
    private Coroutine _activeEffect; // 防止重叠调用
    // 独立协程句柄，与现有 _activeEffect（受伤闪光/死亡消融）互不干扰
    private Coroutine _activeParryGlow;
    private Coroutine _activeDodgeGlow;

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
        // 首次访问 .material 会自动为这个 renderer 创建材质实例副本
        _materialInstance = _sprite.material;
    }

    // ========== P0 生命周期管理（格挡发光事件订阅）==========

    private void OnEnable()
    {
        ParryEvents.OnPerfectParry += HandleParryGlow;
        ParryEvents.OnNormalParry  += HandleParryGlow;

        DodgeEvents.OnPerfectDodge += HandlePerfectDodge;
        DodgeEvents.OnNormalDodge  += HandleNormalDodge;
    }

    private void OnDisable()
    {
        ParryEvents.OnPerfectParry -= HandleParryGlow;
        ParryEvents.OnNormalParry  -= HandleParryGlow;

        DodgeEvents.OnPerfectDodge -= HandlePerfectDodge;
        DodgeEvents.OnNormalDodge  -= HandleNormalDodge;
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

        // ★ 新增：清理精准防御发光
        _materialInstance.DisableKeyword(KW_PARRY_DEFENDER);
        if (_activeParryGlow != null)
        {
            StopCoroutine(_activeParryGlow);
            _activeParryGlow = null;
        }

        // ★ 清理闪避效果
        _materialInstance.DisableKeyword(KW_DODGE);
        _materialInstance.SetFloat(ID_DodgeProgress, 0f);
        if (_activeDodgeGlow != null)
        {
            StopCoroutine(_activeDodgeGlow);
            _activeDodgeGlow = null;
        }
        if (_normalDodgeColorRoutine != null)
        {
            StopCoroutine(_normalDodgeColorRoutine);
            _normalDodgeColorRoutine = null;
        }
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

    // ========== 格挡发光（新增）==========

    /// <summary>事件响应：精准防御/普通防御共用同一个发光 handler</summary>
    private void HandleParryGlow(ParryEventData data)
    {
        // 安全校验：确认这个 ShaderEffectController 属于防御方实体
        var myEntity = GetComponentInParent<PlayerBattleEntity>();
        if (myEntity == null || myEntity != data.Defender) return;

        PlayParryDefenderGlow();
    }

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

    // ========== 闪避效果 (Dodge) ==========

    /// <summary>完美闪避：边缘发光 + 完整闪避Shader（顶点膨胀、噪声故障、残影混色、透明度衰减）</summary>
    private void HandlePerfectDodge(DodgeEventData data)
    {
        var myEntity = GetComponentInParent<PlayerBattleEntity>();
        if (myEntity == null || myEntity != data.Defender) return;

        PlayParryDefenderGlow();
        PlayDodgeGlow();
    }

    /// <summary>普通闪避：仅边缘发光，使用独立的绿色以区别于精准防御蓝色</summary>
    private void HandleNormalDodge(DodgeEventData data)
    {
        var myEntity = GetComponentInParent<PlayerBattleEntity>();
        if (myEntity == null || myEntity != data.Defender) return;

        if (_normalDodgeColorRoutine != null) StopCoroutine(_normalDodgeColorRoutine);
        _normalDodgeColorRoutine = StartCoroutine(NormalDodgeColorRoutine());
    }

    private Coroutine _normalDodgeColorRoutine;

    private IEnumerator NormalDodgeColorRoutine()
    {
        Color originalColor = _materialInstance.GetColor(ID_ParryGlowColor);
        _materialInstance.SetColor(ID_ParryGlowColor, NormalDodgeGlowColor);

        PlayParryDefenderGlow();

        yield return new WaitForSecondsRealtime(ParryGlowDuration);

        _materialInstance.SetColor(ID_ParryGlowColor, originalColor);
        _normalDodgeColorRoutine = null;
    }

    /// <summary>启用 _DODGE_ON 关键字，在 DodgeGlowDuration 内将 _DodgeProgress 从 0 驱动到 1</summary>
    public void PlayDodgeGlow()
    {
        if (_activeDodgeGlow != null) StopCoroutine(_activeDodgeGlow);
        _activeDodgeGlow = StartCoroutine(DodgeGlowRoutine());
    }

    private IEnumerator DodgeGlowRoutine()
    {
        _materialInstance.EnableKeyword(KW_DODGE);
        _materialInstance.SetFloat(ID_DodgeProgress, 0f);

        float elapsed = 0f;
        while (elapsed < DodgeGlowDuration)
        {
            // unscaledDeltaTime: 不受 WitchTime (timeScale=0.2) 影响
            elapsed += Time.unscaledDeltaTime;
            _materialInstance.SetFloat(ID_DodgeProgress, Mathf.Clamp01(elapsed / DodgeGlowDuration));
            yield return null;
        }

        // shader 内部 sin(progress*PI) 产生 0→峰值→0 钟形曲线
        _materialInstance.SetFloat(ID_DodgeProgress, 0f);
        _materialInstance.DisableKeyword(KW_DODGE);
        _activeDodgeGlow = null;
    }

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
        // ---- Phase 1：受伤闪光 + 消融预启动 ----
        // 从第一帧就启用 _DEATH_ON，确保消融全程可见（不被死亡动画的 alpha 曲线盖住）
        _materialInstance.EnableKeyword(KW_DEATH);
        _materialInstance.SetFloat(ID_DissolveProgress, 0f);

        _materialInstance.SetColor(ID_FlashColor, flashColor);
        _materialInstance.SetFloat(ID_FlashIntensity, 0.4f);
        _materialInstance.EnableKeyword(KW_INJURED);

        yield return new WaitForSecondsRealtime(flashDuration);

        // ---- Phase 2：关闭闪光，开始消融 ----
        _materialInstance.DisableKeyword(KW_INJURED);

        Vector4 randomST = new Vector4(
            UnityEngine.Random.Range(0.9f, 1.1f),
            UnityEngine.Random.Range(0.9f, 1.1f),
            UnityEngine.Random.Range(0f, 100f),
            UnityEngine.Random.Range(0f, 100f)
        );
        SetMaterialVector(ID_DissolveTex_ST, randomST);

        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / dissolveDuration);
            _materialInstance.SetFloat(ID_DissolveProgress, progress);
            yield return null;
        }

        _materialInstance.SetFloat(ID_DissolveProgress, 1f);
        _activeEffect = null;

        onComplete?.Invoke();
    }
}
