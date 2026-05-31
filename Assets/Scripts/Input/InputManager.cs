using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // InputAssets 实例
    public InputAssets Controls { get; private set; }

    // InputReader 实例
    public GameplayInputReader Gameplay { get; private set; }
    public BattleInputReader Battle { get; private set; }
    public UIInputReader UI { get; private set; }

    private const string SaveKey = "InputBindingsOverrides";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 创建 InputAssets
            Controls = new InputAssets();

            // 创建 InputReader 并注册回调
            Gameplay = new GameplayInputReader();
            Battle = new BattleInputReader();
            UI = new UIInputReader();

            Controls.GamePlayer.SetCallbacks(Gameplay);
            Controls.Battle.SetCallbacks(Battle);
            Controls.UI.SetCallbacks(UI);

            // 启用探索和 UI Action Map
            Controls.GamePlayer.Enable();
            Controls.UI.Enable();

            // 加载按键绑定覆盖
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
