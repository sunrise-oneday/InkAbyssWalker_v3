using UnityEngine;
using UnityEngine.EventSystems;
using Battle.UI;

/// <summary>
/// Buff 悬浮提示触发器
/// 挂在 Buff 图标上，鼠标悬停时显示 Buff 描述
/// </summary>
public class BuffTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("引用")]
    [SerializeField] private BuffTooltipPanel tooltipPanel;

    private Buff assignedBuff;

    /// <summary>
    /// 设置关联的 Buff 数据
    /// </summary>
    public void Setup(Buff buff, BuffTooltipPanel panel)
    {
        assignedBuff = buff;
        tooltipPanel = panel;
    }

    /// <summary>
    /// 鼠标进入时显示悬浮提示
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipPanel != null && assignedBuff != null)
        {
            tooltipPanel.ShowBuffTooltip(assignedBuff, transform.position);
        }
    }

    /// <summary>
    /// 鼠标离开时隐藏悬浮提示
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.HideBuffTooltip();
        }
    }
}
