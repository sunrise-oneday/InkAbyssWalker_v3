using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // 使用您自动生成的 C# 类: InputAssets
    public InputAssets Controls { get; private set; }

    private const string SaveKey = "InputBindingsOverrides";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 实例化自动生成类
            Controls = new InputAssets();

            // 在激活前加载本地保存的改键数据
            LoadBindingOverrides();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable() => Controls?.Enable();
    private void OnDisable() => Controls?.Disable();

    public void SaveBindingOverrides()
    {
        if (Controls == null) return;
        string rebinds = Controls.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(SaveKey, rebinds);
        PlayerPrefs.Save();
    }

    public void LoadBindingOverrides()
    {
        if (Controls == null) return;
        if (PlayerPrefs.HasKey(SaveKey))
        {
            string rebinds = PlayerPrefs.GetString(SaveKey);
            Controls.LoadBindingOverridesFromJson(rebinds);
        }
    }

    public void ResetBindings()
    {
        if (Controls == null) return;
        Controls.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(SaveKey);
        SaveBindingOverrides();
    }
}