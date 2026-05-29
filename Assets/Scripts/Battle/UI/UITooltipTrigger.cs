using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 万能 UGUI 提示触发器（支持：MP/AP 资源消耗传递判定） [1]
/// </summary>
public class UITooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("描述面板配置")]
    [SerializeField] private string title;
    [SerializeField] private string cost;
    [TextArea(3, 5)]
    [SerializeField] private string description;

    // 暂存该技能/形态实际需要的资源，用于大面板动态检测红字 [2, 3]
    private int requiredMP;
    private int requiredAP;

    /// <summary>
    /// 提供给 UI 动态生成器：在运行时一键注入该卡牌的数据，并带入消耗数值 [1, 2]
    /// </summary>
    public void SetTooltipData(string newTitle, string newCost, string newDesc, int reqMp = 0, int reqAp = 0)
    {
        this.title = newTitle;
        this.cost = newCost;
        this.description = newDesc;
        this.requiredMP = reqMp;
        this.requiredAP = reqAp;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (BattleUIController.Instance != null && !string.IsNullOrEmpty(title))
        {
            // 核心修改：将名字、消耗、描述以及【实际数值消耗】一并喂给主控制板！ [1, 3]
            BattleUIController.Instance.ShowFixedTooltip(title, cost, description, requiredMP, requiredAP);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.HideFixedTooltip();
        }
    }

    private void OnDisable()
    {
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.HideFixedTooltip();
        }
    }
}