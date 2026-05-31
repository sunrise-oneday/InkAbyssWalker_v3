using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 音频管理器
/// 负责游戏内所有音效和背景音乐的统一管理。
/// 支持主音量 / BGM / SFX 三级音量独立控制，自动持久化到 PlayerPrefs。
///
/// 使用方式：
///   AudioManager.Instance.PlayBGM(bgmClip);
///   AudioManager.Instance.PlaySFX(sfxClip);
///   AudioManager.Instance.SetMasterVolume(0.8f);
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("AudioSource 引用")]
    [SerializeField] private AudioSource bgmSource;       // 专门播放 BGM（Loop = true）
    [SerializeField] private AudioSource sfxSource;       // 专门播放 SFX（Loop = false）

    [Header("调试")]
    [SerializeField] private bool logEnabled = false;

    private const string PrefKey_Master = "Audio_MasterVolume";
    private const string PrefKey_BGM    = "Audio_BGMVolume";
    private const string PrefKey_SFX    = "Audio_SFXVolume";

    // ---- 单例 ----
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManager>();
                if (_instance == null)
                {
                    var go = new GameObject("[AudioManager]");
                    _instance = go.AddComponent<AudioManager>();
                    _instance.Initialize();
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
        Initialize();
    }

    private void Initialize()
    {
        // 确保 AudioSource 存在
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        // 从 PlayerPrefs 加载音量设置
        LoadVolumes();
    }

    // ============================================
    // 音量控制
    // ============================================

    public float MasterVolume
    {
        get => PlayerPrefs.GetFloat(PrefKey_Master, 1f);
        set
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PrefKey_Master, clamped);
            PlayerPrefs.Save();
            ApplyVolumes();
        }
    }

    public float BGMVolume
    {
        get => PlayerPrefs.GetFloat(PrefKey_BGM, 1f);
        set
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PrefKey_BGM, clamped);
            PlayerPrefs.Save();
            ApplyVolumes();
        }
    }

    public float SFXVolume
    {
        get => PlayerPrefs.GetFloat(PrefKey_SFX, 1f);
        set
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PrefKey_SFX, clamped);
            PlayerPrefs.Save();
            ApplyVolumes();
        }
    }

    private void LoadVolumes()
    {
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (bgmSource != null)
            bgmSource.volume = PlayerPrefs.GetFloat(PrefKey_Master, 1f)
                             * PlayerPrefs.GetFloat(PrefKey_BGM, 1f);
        if (sfxSource != null)
            sfxSource.volume = PlayerPrefs.GetFloat(PrefKey_Master, 1f)
                             * PlayerPrefs.GetFloat(PrefKey_SFX, 1f);
    }

    // ============================================
    // BGM
    // ============================================

    /// <summary>播放背景音乐（自动切换、淡出淡入由外部协程或 AnimationCurve 控制）</summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;

        if (bgmSource.isPlaying && bgmSource.clip == clip)
            return;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.Play();

        if (logEnabled)
            Debug.Log($"[AudioManager] 播放 BGM: {clip.name}");
    }

    /// <summary>停止背景音乐</summary>
    public void StopBGM()
    {
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    // ============================================
    // SFX
    // ============================================

    /// <summary>播放一次音效</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        sfxSource.PlayOneShot(clip);

        if (logEnabled)
            Debug.Log($"[AudioManager] 播放 SFX: {clip.name}");
    }

    /// <summary>在指定世界位置播放音效（需要 AudioSource 预制体配合）</summary>
    public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float spatialBlend = 1f)
    {
        if (clip == null) return;

        // 使用 Unity 内置 API，简单可靠
        AudioSource.PlayClipAtPoint(clip, position, sfxSource.volume * spatialBlend);
    }
}
