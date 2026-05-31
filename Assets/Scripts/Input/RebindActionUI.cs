using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 单个按键重绑定 UI 组件。
/// 负责管理一个动作的绑定显示、交互式重绑定流程及结果文本渲染。
/// </summary>
public class RebindActionUI : MonoBehaviour
{
    [Header("绑定的动作配置")]
    [SerializeField] private string actionMapName = "GamePlayer"; // 要修改的动作地图，如 "GamePlayer" 或 "Battle"
    [SerializeField] private string actionName = "Jump";          // 对应的动作名称，如 "Jump", "Dash", "Parry"
    [SerializeField] private int bindingIndex = 0;               // 绑定的索引（一般为 0，复合键需添加）

    [Header("UGUI 组件引用")]
    [SerializeField] private Text actionLabel;               // 动作文本（如：跳跃、冲刺、格挡）
    [SerializeField] private Text bindingText;               // 显示当前按键文本（如：SPACE）
    [SerializeField] private Button rebindButton;            // 触发重绑定的 UGUI 按钮
    [SerializeField] private GameObject listeningOverlay;    // 监听状态遮罩（显示"请按键..."）

    private InputAction targetAction;
    private InputActionRebindingExtensions.RebindingOperation rebindOperation;

    private void OnEnable()
    {
        if (rebindButton != null)
        {
            rebindButton.onClick.AddListener(StartRebinding);
        }
        UpdateUI();
    }

    private void OnDisable()
    {
        if (rebindButton != null)
        {
            rebindButton.onClick.RemoveListener(StartRebinding);
        }
        // 防止面板被关闭时重绑定操作句柄内存泄漏
        rebindOperation?.Dispose();
    }

    private void Start()
    {
        InitializeAction();
        UpdateUI();
    }

    /// <summary>
    /// 初始化时从 InputManager 底层抓取目标动作
    /// </summary>
    private void InitializeAction()
    {
        if (InputManager.Instance == null || InputManager.Instance.Controls == null) return;

        var asset = InputManager.Instance.Controls.asset;
        if (asset != null)
        {
            targetAction = asset.FindActionMap(actionMapName)?.FindAction(actionName);
        }
    }

    /// <summary>
    /// 读取当前绑定的按键名称并刷新 UGUI 文本显示
    /// </summary>
    public void UpdateUI()
    {
        if (targetAction == null)
        {
            InitializeAction();
        }

        if (targetAction != null && bindingText != null)
        {
            // 新版绑定系统的友好接口，自动抓取格式化好的字符串（如 "Left Shift"、"Space"）
            string displayString = targetAction.GetBindingDisplayString(bindingIndex);
            bindingText.text = displayString.ToUpper(); // 转为大写以符合游戏 UI 规范
        }

        if (listeningOverlay != null)
        {
            listeningOverlay.SetActive(false); // 默认关闭遮罩
        }
    }

    /// <summary>
    /// 点击按钮后启动交互式重绑定流程
    /// </summary>
    private void StartRebinding()
    {
        if (targetAction == null) return;

        // 1. 重绑定期间，暂时停用整个 InputAssets 信号分发，防止玩家动作冲突
        InputManager.Instance.Controls.Disable();

        // 2. 显示"请按键..."遮罩
        if (listeningOverlay != null)
        {
            listeningOverlay.SetActive(true);
        }

        // 3. 启动高精度交互式绑定操作
        rebindOperation = targetAction.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse")               // 排除鼠标（防止滚轮/滑动被当作按键）
            .WithCancelingThrough("<Keyboard>/escape")    // 按下 ESC 可随时退出/取消重绑定
            .OnMatchWaitForAnother(0.1f)                  // 等待 0.1s 防止同一次按压时误判冲突
            .OnComplete(operation => CleanUpRebind(true))
            .OnCancel(operation => CleanUpRebind(false));

        rebindOperation.Start();
    }

    /// <summary>
    /// 重绑定完成/取消后的收尾与保存逻辑
    /// </summary>
    private void CleanUpRebind(bool success)
    {
        // 1. 强制结束并释放操作句柄
        rebindOperation?.Dispose();
        rebindOperation = null;

        // 2. 隐藏按键遮罩
        if (listeningOverlay != null)
        {
            listeningOverlay.SetActive(false);
        }

        // 3. 只恢复 UI 输入，不启用 GamePlayer/Battle（保持设置面板状态）
        InputManager.Instance.Controls.UI.Enable();

        if (success)
        {
            // 4. 调用 InputManager 暴露的数据持久化接口，写入 PlayerPrefs
            InputManager.Instance.SaveBindingOverrides();
            Debug.Log($"<color=green>[重绑定成功] 动作 {actionName} 已更新绑定。</color>");
        }
        else
        {
            Debug.Log($"<color=yellow>[重绑定取消] {actionName} 的交互式重绑定已安全取消。</color>");
        }

        // 5. 刷新当前按钮 UI
        UpdateUI();
    }
}
