using UnityEngine;

/// <summary>
/// 设置面板基类
/// 统一继承 BasePanel，语义别名便于理解。
/// 直接使用 BasePanel 的生命周期：
///   OnOpen()    → 面板显示时
///   OnClose()   → 面板隐藏时
///   OnActivate()  → 切换为当前 Tab 时
///   OnDeactivate() → 离开当前 Tab 时
///   PanelTitle  → Tab 按钮文字
/// </summary>
public abstract class BaseSettingsPanel : BasePanel
{
    // 完全复用 BasePanel，此处仅为语义别名
}
