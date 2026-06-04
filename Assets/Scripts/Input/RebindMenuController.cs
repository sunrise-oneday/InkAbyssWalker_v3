using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 按键重绑定菜单控制器
/// </summary>
public class RebindMenuController : MonoBehaviour
{
    [Header("重绑定 UI 列表")]
    [SerializeField] private RebindActionUI[] rebindUIs;

    [Header("控制按钮")]
    [SerializeField] private Button defaultResetButton;
    [SerializeField] private Button closePanelButton;

    private void OnEnable()
    {
        if (defaultResetButton != null)
            defaultResetButton.onClick.AddListener(ResetAllToDefault);

        if (closePanelButton != null)
            closePanelButton.onClick.AddListener(CloseMenu);

        RefreshAllUIs();
    }

    private void OnDisable()
    {
        if (defaultResetButton != null)
            defaultResetButton.onClick.RemoveListener(ResetAllToDefault);

        if (closePanelButton != null)
            closePanelButton.onClick.RemoveListener(CloseMenu);
    }

    public void RefreshAllUIs()
    {
        if (rebindUIs == null || rebindUIs.Length == 0)
            rebindUIs = GetComponentsInChildren<RebindActionUI>(true);

        foreach (var ui in rebindUIs)
        {
            if (ui != null)
                ui.UpdateUI();
        }
    }

    private void ResetAllToDefault()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.ResetBindings();
            Debug.Log("[按键系统] 已还原为默认按键绑定");
        }

        // 延迟一帧刷新，确保绑定重置完成后再更新 UI 文本
        StartCoroutine(RefreshNextFrame());
    }

    private IEnumerator RefreshNextFrame()
    {
        yield return null;
        RefreshAllUIs();
    }

    private void CloseMenu()
    {
        gameObject.SetActive(false);
    }
}
