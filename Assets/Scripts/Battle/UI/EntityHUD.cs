using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 战斗属性与状态图标显示器（全事件驱动、自响应式 UI） [1]
/// </summary>
public class EntityHUD : MonoBehaviour
{
    [Header("数据源绑定")]
    [SerializeField] private CharacterStats targetStats;

    [Header("基础进度条 UI")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider breakSlider; // 破防条 (可选)

    [Header("Buff 状态栏配置 [1]")]
    [SerializeField] private Transform buffContainer;
    [SerializeField] private GameObject buffIconPrefab;

    [Header("选中视觉表现 [可选]")]
    [SerializeField] private GameObject selectionIndicator; // 拖入一个作为“选定红圈”或“向下箭头”的子物体
    private Vector3 defaultWorldScale = new Vector3(0.005f, 0.005f, 1f); // 默认的世界空间缩放值

    private void Start()
    {
        // ========================================================
        // 核心修复：必须先安全获取 Canvas，并【判断不为空】才进行操作！
        // 因为玩家底部的 HUD 面板是 Screen-Space（屏幕空间），它的根节点上是没有 Canvas 组件的。
        // 如果不加判断直接 GetComponent<Canvas>().worldCamera，就会触发 MissingComponentException 报错并直接卡死后面的初始化！
        // ========================================================
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                if (Camera.main != null)
                {
                    canvas.worldCamera = Camera.main; // 自动绑定大地图相机为事件相机
                }
            }
        }

        // ========================================================
        // 只要前面的物理相机绑定没有报错，这里的初始化和事件绑定就能百分之百安全执行！
        // 这样玩家底部的血条和怪物头顶的 Buff 系统就会全部恢复正常！
        // ========================================================
        if (targetStats != null)
        {
            BindEvents();
            RefreshAll();
        }
    }

    /// <summary>
    /// 核心新增：被玩家点击选中时的视觉高亮放大表现
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        // 1. 显示/隐藏选中的红色箭头/光圈
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(isSelected);
        }

        // 2. 动态放大头顶血条 1.25 倍，给玩家极其明显的视觉回馈！
        transform.localScale = isSelected ? defaultWorldScale * 2.0f : defaultWorldScale;
    }

    /// <summary>
    /// 核心重构：用于在克隆生成后动态绑定不同的出战角色属性 [1]
    /// </summary>
    public void SetTargetStats(CharacterStats stats)
    {
        // 1. 防漏：先安全注销旧的数据源事件绑定
        UnbindEvents();

        targetStats = stats;

        // 2. 绑定新数据源的生命、破防、Buff 改变事件！ [1, 5]
        BindEvents();
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    private void BindEvents()
    {
        if (targetStats != null)
        {
            targetStats.OnHPChanged += RefreshHP;       // 监听生命值改变 [5]
            targetStats.OnBreakChanged += RefreshBreak; // 监听破防值改变 [5]
            targetStats.OnBuffsChanged += RefreshBuffIcons; // 监听 Buff 改变 [1]
        }
    }

    private void UnbindEvents()
    {
        if (targetStats != null)
        {
            targetStats.OnHPChanged -= RefreshHP;
            targetStats.OnBreakChanged -= RefreshBreak;
            targetStats.OnBuffsChanged -= RefreshBuffIcons;
        }
    }

    /// <summary>
    /// 统一的主动刷新（只在刚进入战场初始化时调用一次） [5]
    /// </summary>
    public void RefreshAll()
    {
        RefreshHP();
        RefreshBreak();
        RefreshBuffIcons();
    }

    // ========================================================
    // 3. 事件驱动的分流刷新函数：数据一变，瞬间定向自重画，性能极佳！ [1, 5]
    // ========================================================

    private void RefreshHP()
    {
        if (targetStats == null || hpSlider == null) return;
        hpSlider.maxValue = targetStats.maxHP;
        hpSlider.value = targetStats.currentHP;
    }

    private void RefreshBreak()
    {
        if (targetStats == null || breakSlider == null) return;
        breakSlider.maxValue = targetStats.maxBreakValue;
        breakSlider.value = targetStats.currentBreakValue;
    }

    private void RefreshBuffIcons()
    {
        if (buffContainer == null || buffIconPrefab == null || targetStats == null) return;

        // 清空旧的
        foreach (Transform child in buffContainer)
        {
            Destroy(child.gameObject);
        }

        // 动态克隆新的状态图标，水平布局组全自动对齐
        foreach (Buff buff in targetStats.activeBuffs)
        {
            GameObject iconObj = Instantiate(buffIconPrefab, buffContainer);

            // 1. 设置 Buff 的精灵图片 [1]
            // ========================================================
            // 核心修改：改用获取我们高级的 BuffIcon 脚本并一键注入数据！
            // ========================================================
            BuffIcon iconScript = iconObj.GetComponent<BuffIcon>();
            if (iconScript != null)
            {
                iconScript.Setup(buff);
            }

            // ========================================================
            // 2. 核心新增：自动抓取子物体上的 Text，动态刷入“剩余回合数”与“层数（x2）” [1]
            // ========================================================
            Text turnText = iconObj.GetComponentInChildren<Text>();
            if (turnText != null)
            {
                // 如果层数大于 1，则同时换行显示层数，例如显示 "3 \n x2" (3回合，2层) [1]
                turnText.text = buff.stacks > 1 ? $"{buff.durationTurns}\n<size=10>x{buff.stacks}</size>" : buff.durationTurns.ToString();
            }
        }
    }
}