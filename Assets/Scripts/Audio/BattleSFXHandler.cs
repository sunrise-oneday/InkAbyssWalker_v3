using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗音效播放器。
/// 订阅 ParryEvents 获取格挡音效时机，通过静态单例接收其他战斗触发点（技能/大招/死亡/胜负）。
/// 不依赖 AudioManager，自行管理临时 AudioSource 实例。
///
/// 使用方式：
///   BattleSFXHandler.Instance?.PlaySFX(SFXKey.SkillCast);
/// </summary>
public class BattleSFXHandler : MonoBehaviour
{
    [SerializeField] private SFXConfigSO sfxConfig;

    private Dictionary<SFXKey, SFXEntry> _sfxLookup;
    private Dictionary<SFXKey, float> _lastPlayTime; // 防抖

    // ============================================
    // 静态单例
    // ============================================

    private static BattleSFXHandler _instance;
    public static BattleSFXHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BattleSFXHandler>();
                if (_instance == null)
                {
                    var go = new GameObject("[BattleSFXHandler]");
                    _instance = go.AddComponent<BattleSFXHandler>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        BuildLookup();
    }

    // ============================================
    // 配置表加载
    // ============================================

    private void BuildLookup()
    {
        _sfxLookup = new Dictionary<SFXKey, SFXEntry>();
        _lastPlayTime = new Dictionary<SFXKey, float>();

        if (sfxConfig == null || sfxConfig.entries == null)
        {
            Debug.LogWarning("[BattleSFXHandler] SFXConfigSO 未配置或 entries 为空！");
            return;
        }

        foreach (var entry in sfxConfig.entries)
        {
            if (entry.key == SFXKey.None) continue;
            _sfxLookup[entry.key] = entry; // 重复 key 后者覆盖
        }
    }

    // ============================================
    // 生命周期：格挡事件订阅（ParryEvents）
    // ============================================

    private void OnEnable()
    {
        ParryEvents.OnPerfectParry += HandlePerfectParry;
        ParryEvents.OnNormalParry  += HandleNormalParry;
        ParryEvents.OnParryFailed  += HandleParryFailed;

        DodgeEvents.OnPerfectDodge += HandlePerfectDodge;
        DodgeEvents.OnNormalDodge  += HandleNormalDodge;
    }

    private void OnDisable()
    {
        ParryEvents.OnPerfectParry -= HandlePerfectParry;
        ParryEvents.OnNormalParry  -= HandleNormalParry;
        ParryEvents.OnParryFailed  -= HandleParryFailed;

        DodgeEvents.OnPerfectDodge -= HandlePerfectDodge;
        DodgeEvents.OnNormalDodge  -= HandleNormalDodge;
    }

    private void HandlePerfectParry(ParryEventData data) => PlaySFX(SFXKey.PerfectParry, data.HitPoint);
    private void HandleNormalParry(ParryEventData data)  => PlaySFX(SFXKey.NormalParry,  data.HitPoint);
    private void HandleParryFailed(ParryEventData data)  => PlaySFX(SFXKey.Hit, data.HitPoint);  // 防御失败→受击音效

    private void HandlePerfectDodge(DodgeEventData data) => PlaySFX(SFXKey.PerfectDodge, data.HitPoint);
    private void HandleNormalDodge(DodgeEventData data)  => PlaySFX(SFXKey.Hit, data.HitPoint);  // 闪避失败→受击音效

    // ============================================
    // 核心播放方法
    // ============================================

    /// <summary>播放指定 key 的音效。position 为 null 时在原点播放（2D 模式）。</summary>
    public void PlaySFX(SFXKey key, Vector3? position = null)
    {
        if (!_sfxLookup.TryGetValue(key, out var entry))
        {
            Debug.LogWarning($"[BattleSFXHandler] 未找到 SFX 配置: {key}");
            return;
        }

        if (entry.clip == null)
        {
            Debug.LogWarning($"[BattleSFXHandler] AudioClip 为空: {key}");
            return;
        }

        // ★ 防抖：100ms 内同一 key 不重复播放（解决 AoE 多杀叠加）
        float now = Time.unscaledTime;
        if (_lastPlayTime.TryGetValue(key, out float last) && (now - last) < 0.1f)
            return;
        _lastPlayTime[key] = now;

        // 随机变调
        float pitch = Random.Range(entry.pitchMin, entry.pitchMax);

        // 创建临时 AudioSource，播完自动销毁
        var go = new GameObject($"SFX_{entry.key}");
        if (position.HasValue) go.transform.position = position.Value;

        var source = go.AddComponent<AudioSource>();
        source.clip         = entry.clip;
        source.volume       = entry.volume;
        source.pitch        = pitch;
        source.spatialBlend = entry.spatialBlend;
        source.priority     = entry.priority;
        source.Play();

        // clip 长度 / |pitch|，pitch 为 0 时保护
        float safePitch = Mathf.Max(0.01f, Mathf.Abs(pitch));
        float realLength = entry.clip.length / safePitch;
        Destroy(go, realLength + 0.1f);
    }
}
