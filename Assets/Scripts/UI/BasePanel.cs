using UnityEngine;

/// <summary>
/// UI 面板基类
/// 所有面板（战斗面板、设置面板等）继承此类，获得统一的生命周期管理。
/// </summary>
public abstract class BasePanel : MonoBehaviour
{
    [Header("面板信息")]
    [SerializeField] private string panelTitle = "面板";
    public string PanelTitle => panelTitle;

    /// <summary>面板是否处于打开状态</summary>
    public bool IsOpen => gameObject.activeSelf;

    /// <summary>面板打开/显示时调用</summary>
    public virtual void OnOpen() { }

    /// <summary>面板关闭/隐藏时调用</summary>
    public virtual void OnClose() { }

    /// <summary>面板数据刷新时调用</summary>
    public virtual void OnRefresh() { }

    /// <summary>切换为该面板（Tab 激活）时调用</summary>
    public virtual void OnActivate() { }

    /// <summary>离开该面板（Tab 失活）时调用</summary>
    public virtual void OnDeactivate() { }

    /// <summary>设置面板显隐，自动触发 OnOpen/OnClose</summary>
    public virtual void SetActive(bool active)
    {
        if (gameObject.activeSelf == active) return;
        gameObject.SetActive(active);
        if (active) OnOpen();
        else OnClose();
    }
}
