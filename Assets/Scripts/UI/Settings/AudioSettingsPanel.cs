using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 音频设置面板
/// 主音量 / BGM / SFX 三级滑动条 + 静音开关
/// </summary>
public class AudioSettingsPanel : BasePanel
{
    [Header("音量滑动条")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("音量数值文本")]
    [SerializeField] private Text masterValueText;
    [SerializeField] private Text bgmValueText;
    [SerializeField] private Text sfxValueText;

    [Header("静音开关")]
    [SerializeField] private Toggle muteMasterToggle;
    [SerializeField] private Toggle muteBGMToggle;
    [SerializeField] private Toggle muteSFXToggle;

    private bool isInitializing = true;
    private float preMuteMasterVol = 1f; // 静音前音量缓存
    private float preMuteBGMVol = 1f;
    private float preMuteSFXVol = 1f;

    private void Start()
    {
        // 从 AudioManager 同步初始值
        var am = AudioManager.Instance;

        // Slider + Text
        InitSlider(masterSlider, masterValueText, am.MasterVolume, v =>
        {
            am.MasterVolume = v;
            if (!isInitializing) PlayTick();
            UpdateValueText(masterValueText, v);
        });

        InitSlider(bgmSlider, bgmValueText, am.BGMVolume, v =>
        {
            am.BGMVolume = v;
            UpdateValueText(bgmValueText, v);
        });

        InitSlider(sfxSlider, sfxValueText, am.SFXVolume, v =>
        {
            am.SFXVolume = v;
            if (!isInitializing) PlayTick();
            UpdateValueText(sfxValueText, v);
        });

        // Toggle
        if (muteMasterToggle != null)
            muteMasterToggle.onValueChanged.AddListener(OnMuteMasterChanged);
        if (muteBGMToggle != null)
            muteBGMToggle.onValueChanged.AddListener(OnMuteBGMChanged);
        if (muteSFXToggle != null)
            muteSFXToggle.onValueChanged.AddListener(OnMuteSFXChanged);

        isInitializing = false;
    }

    public override void OnOpen()
    {
        SyncFromManager();
    }

    // ============================================
    // 初始化辅助
    // ============================================

    private void InitSlider(Slider slider, Text text, float value, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null) return;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(callback);
        UpdateValueText(text, value);
    }

    private void SyncFromManager()
    {
        var am = AudioManager.Instance;
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(am.MasterVolume);
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(am.BGMVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(am.SFXVolume);

        // 同步静音状态时，初始化静音前的音量缓存
        bool isMasterMuted = am.MasterVolume <= 0.01f;
        bool isBGMMuted = am.BGMVolume <= 0.01f;
        bool isSFXMuted = am.SFXVolume <= 0.01f;

        if (isMasterMuted) preMuteMasterVol = 1f;
        if (isBGMMuted) preMuteBGMVol = 1f;
        if (isSFXMuted) preMuteSFXVol = 1f;

        if (muteMasterToggle != null) muteMasterToggle.SetIsOnWithoutNotify(isMasterMuted);
        if (muteBGMToggle != null) muteBGMToggle.SetIsOnWithoutNotify(isBGMMuted);
        if (muteSFXToggle != null) muteSFXToggle.SetIsOnWithoutNotify(isSFXMuted);

        UpdateAllTexts();
    }

    // ============================================
    // Toggle 逻辑
    // ============================================

    private void OnMuteMasterChanged(bool isOn)
    {
        var am = AudioManager.Instance;
        if (isOn)
        {
            preMuteMasterVol = am.MasterVolume > 0.01f ? am.MasterVolume : 1f;
            am.MasterVolume = 0f;
        }
        else
        {
            am.MasterVolume = preMuteMasterVol > 0.01f ? preMuteMasterVol : 1f;
        }
        PlayTick();
    }

    private void OnMuteBGMChanged(bool isOn)
    {
        var am = AudioManager.Instance;
        if (isOn)
        {
            preMuteBGMVol = am.BGMVolume > 0.01f ? am.BGMVolume : 1f;
            am.BGMVolume = 0f;
        }
        else
        {
            am.BGMVolume = preMuteBGMVol > 0.01f ? preMuteBGMVol : 1f;
        }
    }

    private void OnMuteSFXChanged(bool isOn)
    {
        var am = AudioManager.Instance;
        if (isOn)
        {
            preMuteSFXVol = am.SFXVolume > 0.01f ? am.SFXVolume : 1f;
            am.SFXVolume = 0f;
        }
        else
        {
            am.SFXVolume = preMuteSFXVol > 0.01f ? preMuteSFXVol : 1f;
        }
    }

    // ============================================
    // 辅助
    // ============================================

    private void PlayTick() { AudioManager.Instance?.PlaySFX(null); }

    private void UpdateAllTexts()
    {
        var am = AudioManager.Instance;
        UpdateValueText(masterValueText, am.MasterVolume);
        UpdateValueText(bgmValueText, am.BGMVolume);
        UpdateValueText(sfxValueText, am.SFXVolume);
    }

    private void UpdateValueText(Text t, float v)
    {
        if (t != null) t.text = $"{Mathf.RoundToInt(v * 100)}%";
    }
}
