using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂载在改键设置菜单主面板上，负责面板内所有按键的宏观重置与调度
/// </summary>
public class RebindMenuController : MonoBehaviour
{
    [Header("改键子项列表")]
    [SerializeField] private RebindActionUI[] rebindUIs; // 拖入挂载了 RebindActionUI 的子物体列表

    [Header("设置交互按钮")]
    [SerializeField] private Button defaultResetButton;  // “恢复默认设置” UGUI 按钮
    [SerializeField] private Button closePanelButton;    // “关闭改键设置” UGUI 按钮

    private void OnEnable()
    {
        if (defaultResetButton != null)
        {
            defaultResetButton.onClick.AddListener(ResetAllToDefault);
        }

        if (closePanelButton != null)
        {
            closePanelButton.onClick.AddListener(CloseMenu);
        }

        RefreshAllUIs();
    }

    private void OnDisable()
    {
        if (defaultResetButton != null)
        {
            defaultResetButton.onClick.RemoveListener(ResetAllToDefault);
        }

        if (closePanelButton != null)
        {
            closePanelButton.onClick.RemoveListener(CloseMenu);
        }
    }

    /// <summary>
    /// 刷新面板下所有按键的状态显示
    /// </summary>
    public void RefreshAllUIs()
    {
        if (rebindUIs == null || rebindUIs.Length == 0)
        {
            rebindUIs = GetComponentsInChildren<RebindActionUI>(true);
        }

        foreach (var ui in rebindUIs)
        {
            if (ui != null)
            {
                ui.UpdateUI();
            }
        }
    }

    /// <summary>
    /// 调用全局重置：清除所有玩家改键缓存，还原为最初的配置
    /// </summary>
    private void ResetAllToDefault()
    {
        if (InputManager.Instance != null)
        {
            // 调用 InputManager 里的 ResetBindings 清除所有覆盖文件
            InputManager.Instance.ResetBindings();

            // 重新刷新整个界面的文本
            RefreshAllUIs();
            Debug.Log("<color=cyan>[改键系统] 已还原为系统默认按键布局。</color>");
        }
    }

    /// <summary>
    /// 关闭该 UGUI 界面
    /// </summary>
    private void CloseMenu()
    {
        gameObject.SetActive(false);
    }
}