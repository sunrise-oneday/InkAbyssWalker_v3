using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuffIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("边框高亮设置 (拖入子物体 Border)")]
    [SerializeField] private CanvasGroup highlightBorderGroup;

    private Buff cachedBuff;

    private void Awake()
    {
        if (highlightBorderGroup != null)
        {
            highlightBorderGroup.alpha = 0f;
            highlightBorderGroup.blocksRaycasts = false;
        }
    }

    public void Setup(Buff buff)
    {
        this.cachedBuff = buff;
        Image img = GetComponent<Image>();
        if (img != null)
        {
            img.sprite = buff.icon;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"<color=cyan>[悬浮测试] 鼠标移入了 Buff 图标！当前焦点 Buff: {cachedBuff?.buffName ?? "空"}</color>");

        // 1. 悬浮微放大表现
        transform.localScale = new Vector3(1.22f, 1.22f, 1f);

        // 2. 显示金色的高亮边框 [1]
        if (highlightBorderGroup != null)
        {
            highlightBorderGroup.alpha = 1f;
        }

        // ========================================================
        // 3. 核心修改：不再只传自己！直接把这个宿主身上所有的 Buff 列表打包丢过去！
        // 这样左边的提示大盒子就会从上往下自动整齐排队列出所有的 Buff 信息！ [1]
        // ========================================================
        if (cachedBuff != null && cachedBuff.owner != null && BattleUIController.Instance != null)
        {
            BattleUIController.Instance.ShowTooltipList(cachedBuff.owner.activeBuffs);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("[悬浮测试] 鼠标移出了 Buff 图标。");

        transform.localScale = Vector3.one;

        if (highlightBorderGroup != null)
        {
            highlightBorderGroup.alpha = 0f;
        }

        // 隐藏大提示框
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.HideTooltip();
        }
    }

    private void OnDisable()
    {
        transform.localScale = Vector3.one;
        if (highlightBorderGroup != null)
        {
            highlightBorderGroup.alpha = 0f;
        }
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.HideTooltip();
        }
    }
}