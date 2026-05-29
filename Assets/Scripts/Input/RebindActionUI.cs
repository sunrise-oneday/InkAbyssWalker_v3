using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// 挂载在单个改键控制项 UGUI 上的组件，负责单个按键的重绑与文本渲染
/// </summary>
public class RebindActionUI : MonoBehaviour
{
    [Header("绑定的动作配置")]
    [SerializeField] private string actionMapName = "GamePlayer"; // 要改键的动作表，如 "GamePlayer" 或 "Battle"
    [SerializeField] private string actionName = "Jump";          // 对应的动作名称，如 "Jump", "Dash", "Parry"
    [SerializeField] private int bindingIndex = 0;               // 绑定的索引（单键一般为 0，复合键可增加）

    [Header("UGUI 关联组件")]
    [SerializeField] private Text actionLabel;               // 动作描述文本（如：“跳跃”、“格挡”）
    [SerializeField] private Text bindingText;               // 显示当前按键键名的文本（如：“SPACE”）
    [SerializeField] private Button rebindButton;                // 点击触发监听改键的 UGUI 按钮
    [SerializeField] private GameObject listeningOverlay;        // 遮罩提示（当开启改键时，显示“请按下任意键...”）

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
        // 防止面板被关闭时，非正常挂起的重绑操作内存泄漏
        rebindOperation?.Dispose();
    }

    private void Start()
    {
        InitializeAction();
        UpdateUI();
    }

    /// <summary>
    /// 初始化并从 InputManager 底层抓取动作对象
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
    /// 获取当前绑定的键名，并刷新 UGUI 文本显示
    /// </summary>
    public void UpdateUI()
    {
        if (targetAction == null)
        {
            InitializeAction();
        }

        if (targetAction != null && bindingText != null)
        {
            // 利用新版输入系统内置接口，自动抓取对人类友好的字符串（如 "Left Shift"、"Space"）
            string displayString = targetAction.GetBindingDisplayString(bindingIndex);
            bindingText.text = displayString.ToUpper(); // 转化为大写以符合多数游戏 UI 规范
        }

        if (listeningOverlay != null)
        {
            listeningOverlay.SetActive(false); // 默认关闭遮罩
        }
    }

    /// <summary>
    /// 点击按钮触发：启动交互式改键监听
    /// </summary>
    private void StartRebinding()
    {
        if (targetAction == null) return;

        // 1. 改键期间，必须暂时停用整个 InputAssets 的信号分发，防止操作冲突
        InputManager.Instance.Controls.Disable();

        // 2. 显示“请按任意键”遮罩
        if (listeningOverlay != null)
        {
            listeningOverlay.SetActive(true);
        }

        // 3. 构建高精度交互式绑定操作
        rebindOperation = targetAction.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse")               // 排除鼠标（防止误将鼠标滑动、点击绑定为按键）
            .WithCancelingThrough("<Keyboard>/escape")    // 设置按下 ESC 键可以直接退出/取消改键
            .OnMatchWaitForAnother(0.1f)                  // 等待 0.1s 防止按键物理弹起时引发多重判定
            .OnComplete(operation => CleanUpRebind(true))
            .OnCancel(operation => CleanUpRebind(false));

        rebindOperation.Start();
    }

    /// <summary>
    /// 结束改键后的清理与保存逻辑
    /// </summary>
    private void CleanUpRebind(bool success)
    {
        // 1. 强制清理与释放重绑句柄
        rebindOperation?.Dispose();
        rebindOperation = null;

        // 2. 隐藏按任意键的提示遮罩
        if (listeningOverlay != null)
        {
            listeningOverlay.SetActive(false);
        }

        // 3. 恢复全局输入
        InputManager.Instance.Controls.Enable();

        if (success)
        {
            // 4. 调用您在 InputManager 里准备好的数据持久化接口，写入 PlayerPrefs
            InputManager.Instance.SaveBindingOverrides();
            Debug.Log($"<color=green>[改键成功] 动作 {actionName} 已绑定新按键！</color>");
        }
        else
        {
            Debug.Log($"<color=yellow>[改键取消] {actionName} 交互式改键已安全取消。</color>");
        }

        // 5. 立即重画自身 UI 
        UpdateUI();
    }
}