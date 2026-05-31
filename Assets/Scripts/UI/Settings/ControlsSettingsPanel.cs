using UnityEngine;

/// <summary>
/// 按键设置面板
/// 包装 RebindMenuController，将其嵌入设置面板系统
/// </summary>
public class ControlsSettingsPanel : BaseSettingsPanel
{
    [Header("改键面板引用")]
    [SerializeField] private RebindMenuController rebindMenu;

    public override void OnOpen()
    {
        rebindMenu?.RefreshAllUIs();
    }

    public override void OnActivate()
    {
        if (rebindMenu != null)
            rebindMenu.gameObject.SetActive(true);
    }

    public override void OnDeactivate()
    {
        if (rebindMenu != null)
            rebindMenu.gameObject.SetActive(false);
    }
}
