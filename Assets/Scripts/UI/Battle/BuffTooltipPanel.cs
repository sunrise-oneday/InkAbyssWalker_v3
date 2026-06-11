using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 浮动 Buff 提示面板：鼠标悬停 Buff 图标时显示
/// </summary>
public class BuffTooltipPanel : BasePanel
{
    [Header("浮动提示")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private Transform tooltipListContainer;
    [SerializeField] private GameObject tooltipRowPrefab;

    /// <summary>显示 Buff 列表</summary>
    public void Show(List<Buff> activeBuffs)
    {
        if (tooltipPanel == null || tooltipListContainer == null || tooltipRowPrefab == null) return;
        if (activeBuffs == null || activeBuffs.Count == 0) return;

        // 清空旧条目
        foreach (Transform child in tooltipListContainer)
            Destroy(child.gameObject);

        // 填充 Buff 列表
        foreach (Buff buff in activeBuffs)
        {
            GameObject rowObj = Instantiate(tooltipRowPrefab, tooltipListContainer);

            var iconImg = rowObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImg != null) iconImg.sprite = buff.icon;

            var nameTxt = rowObj.transform.Find("txtName")?.GetComponent<Text>();
            if (nameTxt != null) nameTxt.text = buff.buffName;

            var descTxt = rowObj.transform.Find("txtDesc")?.GetComponent<Text>();
            if (descTxt != null)
                descTxt.text = $"{buff.description} <color=yellow>(剩余 {buff.durationTurns} 回合)</color>";
        }

        // 阻止射线穿透
        var cg = tooltipPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = tooltipPanel.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        tooltipPanel.SetActive(true);
        UpdatePosition();
    }

    /// <summary>隐藏 Buff 提示</summary>
    public void Hide()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    /// <summary>显示单个 Buff 的悬浮提示（用于鼠标悬停在单个图标上）</summary>
    public void ShowBuffTooltip(Buff buff, Vector3 position)
    {
        if (tooltipPanel == null || buff == null) return;

        // 清空旧条目
        foreach (Transform child in tooltipListContainer)
            Destroy(child.gameObject);

        // 创建单个 Buff 详情
        GameObject rowObj = Instantiate(tooltipRowPrefab, tooltipListContainer);

        var iconImg = rowObj.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImg != null) iconImg.sprite = buff.icon;

        var nameTxt = rowObj.transform.Find("txtName")?.GetComponent<Text>();
        if (nameTxt != null) nameTxt.text = buff.buffName;

        var descTxt = rowObj.transform.Find("txtDesc")?.GetComponent<Text>();
        if (descTxt != null)
        {
            string desc = buff.description;
            if (buff.stacks > 1)
                desc += $" <color=yellow>(x{buff.stacks})</color>";
            desc += $" <color=yellow>(剩余 {buff.durationTurns} 回合)</color>";
            descTxt.text = desc;
        }

        // 阻止射线穿透
        var cg = tooltipPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = tooltipPanel.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        tooltipPanel.SetActive(true);

        // 设置位置（在图标上方）
        tooltipPanel.transform.position = position + new Vector3(0, 50f, 0);
    }

    /// <summary>隐藏单个 Buff 提示</summary>
    public void HideBuffTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    /// <summary>跟随鼠标位置</summary>
    private void UpdatePosition()
    {
        Vector2 mousePos = Input.mousePosition;
        tooltipPanel.transform.position = mousePos + new Vector2(-120f, 60f);
    }

    private void Update()
    {
        if (tooltipPanel != null && tooltipPanel.activeSelf)
            UpdatePosition();
    }
}
