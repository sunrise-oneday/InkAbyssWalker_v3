using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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

    // 各 Buff 类型的颜色映射（用于生成后备占位图标）
    private static readonly Dictionary<string, Color> BuffColors = new Dictionary<string, Color>
    {
        { "虚弱", new Color(0.8f, 0.4f, 0.1f) },       // 橙色
        { "破甲", new Color(0.9f, 0.5f, 0.0f) },       // 金黄色
        { "脆弱", new Color(0.9f, 0.3f, 0.5f) },       // 粉红
        { "中毒", new Color(0.3f, 0.8f, 0.1f) },       // 绿色
        { "诅咒", new Color(0.6f, 0.1f, 0.8f) },       // 紫色
        { "速度下降", new Color(0.5f, 0.7f, 0.9f) },   // 淡蓝
        { "火元素附着", new Color(1.0f, 0.3f, 0.0f) }, // 红色
        { "冰元素附着", new Color(0.2f, 0.6f, 1.0f) }, // 蓝色
        { "眩晕", new Color(1.0f, 1.0f, 0.2f) },       // 黄色
        { "易伤", new Color(1.0f, 0.2f, 0.2f) },      // 亮红
        { "燃烧", new Color(1.0f, 0.4f, 0.1f) },       // 火焰橙
        { "冻结", new Color(0.4f, 0.7f, 1.0f) },       // 冰晶蓝
        { "水附着", new Color(0.2f, 0.5f, 0.9f) },     // 水蓝
    };

    /// <summary>
    /// 生成彩色圆形作为后备图标
    /// </summary>
    private static Sprite GenerateFallbackIcon(Color color, string label)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        // 绘制圆形
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1;
        Color transparent = new Color(0, 0, 0, 0);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dx = x + 0.5f - center.x;
                float dy = y + 0.5f - center.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= radius)
                {
                    // 边缘柔化
                    float alpha = Mathf.Clamp01((radius - dist + 1f) / 1f);
                    Color c = color;
                    c.a = alpha;
                    tex.SetPixel(x, y, c);
                }
                else
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
        }

        tex.Apply();

        Rect rect = new Rect(0, 0, size, size);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        return Sprite.Create(tex, rect, pivot, 32f);
    }

    public void Setup(Buff buff)
    {
        this.cachedBuff = buff;
        Image img = GetComponent<Image>();
        if (img != null)
        {
            if (buff.icon != null)
            {
                img.sprite = buff.icon;
            }
            else
            {
                // 没有加载到图标文件 → 生成彩色占位圆点
                Color color = Color.gray;
                if (buff.buffName != null && BuffColors.ContainsKey(buff.buffName))
                {
                    color = BuffColors[buff.buffName];
                }
                img.sprite = GenerateFallbackIcon(color, buff.buffName);
                Debug.Log($"[BuffIcon] {buff.buffName} 使用后备彩色图标");
            }
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