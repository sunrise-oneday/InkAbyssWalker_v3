using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Battle.Enemy;

/// <summary>
/// 战斗属性与状态图标显示器（全事件驱动、自响应式 UI） [1]
/// </summary>
public class EntityHUD : MonoBehaviour
{
    [Header("数据源绑定")]
    [SerializeField] private CharacterStats targetStats;
    [SerializeField] private EnemyBattleEntity targetEnemy; // 目标敌人（用于获取意图）

    [Header("基础进度条 UI")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider breakSlider; // 破防条 (可选)
    [SerializeField] private Slider shieldSlider; // 护盾条 (可选)

    [Header("意图图标显示")]
    [SerializeField] private Image intentIcon;              // 意图图标
    [SerializeField] private GameObject intentContainer;    // 意图容器
    [SerializeField] private TMP_Text intentValueText;      // 意图数值文本（可选）
    [SerializeField] private GameObject intentTooltip;      // 意图悬浮提示面板
    [SerializeField] private TMP_Text intentTooltipText;    // 意图描述文本

    [Header("Buff 状态栏配置 [1]")]
    [SerializeField] private Transform buffContainer;
    [SerializeField] private GameObject buffIconPrefab;

    [Header("选中视觉表现 [可选]")]
    [SerializeField] private GameObject selectionIndicator; // 拖入一个作为"选定红圈"或"向下箭头"的子物体
    [Header("世界空间缩放（直接填数值）")]
    [SerializeField] private float hudScale = 0.02f;
    [SerializeField] private float hudScaleFactor = 0.022f;

    private void Start()
    {
        // 直接用你填的数值，不做任何倍率计算
        transform.localScale = new Vector3(hudScale, hudScale, 1f);

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

        // 自动获取父级（BattleEnemy根节点）上的敌人实体
        if (targetEnemy == null)
        {
            targetEnemy = GetComponentInParent<EnemyBattleEntity>();
        }

        // 隐藏意图悬浮提示面板
        if (intentTooltip != null)
            intentTooltip.SetActive(false);

        // 绑定敌人意图改变事件
        if (targetEnemy != null)
        {
            targetEnemy.OnIntentChanged += OnIntentChanged;
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

        // 2. 选中变大用 hudScaleFactor，不选中用 hudScale
        float s = isSelected ? hudScaleFactor : hudScale;
        transform.localScale = new Vector3(s, s, 1f);
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
            targetStats.OnShieldChanged += RefreshShield; // 监听护盾值改变
        }
    }

    private void UnbindEvents()
    {
        if (targetStats != null)
        {
            targetStats.OnHPChanged -= RefreshHP;
            targetStats.OnBreakChanged -= RefreshBreak;
            targetStats.OnBuffsChanged -= RefreshBuffIcons;
            targetStats.OnShieldChanged -= RefreshShield;
        }
    }

    /// <summary>
    /// 统一的主动刷新（只在刚进入战场初始化时调用一次） [5]
    /// </summary>
    public void RefreshAll()
    {
        RefreshHP();
        RefreshBreak();
        RefreshShield();
        RefreshBuffIcons();
        RefreshIntent();
    }

    // ========================================================
    // 意图图标相关方法
    // ========================================================

    /// <summary>
    /// 设置目标敌人（用于获取意图）
    /// </summary>
    public void SetTargetEnemy(EnemyBattleEntity enemy)
    {
        // 解绑旧敌人的事件
        if (targetEnemy != null)
        {
            targetEnemy.OnIntentChanged -= OnIntentChanged;
        }

        targetEnemy = enemy;

        // 绑定新敌人的事件
        if (targetEnemy != null)
        {
            targetEnemy.OnIntentChanged += OnIntentChanged;
        }

        RefreshIntent();
    }

    /// <summary>
    /// 意图改变事件处理
    /// </summary>
    private void OnIntentChanged(EnemyIntent intent)
    {
        RefreshIntent();
    }

    /// <summary>
    /// 刷新意图图标显示
    /// </summary>
    public void RefreshIntent()
    {
        if (targetEnemy == null)
        {
            targetEnemy = GetComponentInParent<EnemyBattleEntity>();
        }

        if (targetEnemy == null || intentContainer == null) return;

        EnemyIntent intent = targetEnemy.GetCurrentIntent();
        if (intent == null)
        {
            // 没有意图时隐藏意图容器
            intentContainer.SetActive(false);
            return;
        }

        // 显示意图容器
        intentContainer.SetActive(true);

        // 设置意图图标
        if (intentIcon != null)
        {
            if (intent.icon != null)
            {
                intentIcon.sprite = intent.icon;
                intentIcon.gameObject.SetActive(true);
            }
            else
            {
                // 没有自定义图标，使用默认图标
                Sprite defaultIcon = GetDefaultIntentIcon(intent.type);
                if (defaultIcon != null)
                {
                    intentIcon.sprite = defaultIcon;
                    intentIcon.gameObject.SetActive(true);
                }
                else
                {
                    intentIcon.gameObject.SetActive(false);
                }
            }
        }

        // 设置意图数值文本（可选）
        if (intentValueText != null)
        {
            switch (intent.type)
            {
                case EnemyIntentType.Attack:
                case EnemyIntentType.MultiAttack:
                case EnemyIntentType.SpecialAttack:
                    intentValueText.text = intent.value.ToString();
                    break;
                case EnemyIntentType.Block:
                    intentValueText.text = intent.value.ToString();
                    break;
                case EnemyIntentType.Heal:
                    intentValueText.text = "+" + intent.value.ToString();
                    break;
                default:
                    intentValueText.text = "";
                    break;
            }
        }

        // 设置意图描述（用于悬浮提示）
        UpdateIntentTooltip(intent);
    }

    /// <summary>
    /// 更新意图悬浮提示内容
    /// </summary>
    private void UpdateIntentTooltip(EnemyIntent intent)
    {
        if (intentTooltipText == null) return;

        string description = GetIntentDescription(intent);
        intentTooltipText.text = description;
    }

    /// <summary>
    /// 获取意图描述
    /// </summary>
    private string GetIntentDescription(EnemyIntent intent)
    {
        switch (intent.type)
        {
            case EnemyIntentType.Attack:
                return "此敌人即将攻击";

            case EnemyIntentType.MultiAttack:
                return "此敌人即将进行多段攻击";

            case EnemyIntentType.SpecialAttack:
                return "此敌人即将发动强力攻击";

            case EnemyIntentType.Block:
                return "此敌人即将防御";

            case EnemyIntentType.Heal:
                return "此敌人即将恢复生命";

            case EnemyIntentType.DebuffPlayer:
                return "此敌人即将施加负面效果";

            case EnemyIntentType.BuffSelf:
            case EnemyIntentType.Strengthen:
                return "此敌人即将强化自身";

            case EnemyIntentType.Summon:
                return "此敌人即将召唤援军";

            case EnemyIntentType.Unknown:
                return "???";

            default:
                return "未知行动";
        }
    }

    /// <summary>
    /// 获取默认意图图标（从 Resources 加载）
    /// </summary>
    private Sprite GetDefaultIntentIcon(EnemyIntentType intentType)
    {
        string iconName = intentType switch
        {
            EnemyIntentType.Attack => "Intent_Attack",
            EnemyIntentType.MultiAttack => "Intent_MultiAttack",
            EnemyIntentType.SpecialAttack => "Intent_SpecialAttack",
            EnemyIntentType.Block => "Intent_Block",
            EnemyIntentType.Heal => "Intent_Heal",
            EnemyIntentType.BuffSelf => "Intent_Buff",
            EnemyIntentType.Strengthen => "Intent_Strengthen",
            EnemyIntentType.DebuffPlayer => "Intent_Debuff",
            EnemyIntentType.Summon => "Intent_Summon",
            EnemyIntentType.Unknown => "Intent_Unknown",
            _ => "Intent_Attack"
        };

        return Resources.Load<Sprite>($"IntentIcons/{iconName}");
    }

    /// <summary>
    /// 显示意图悬浮提示（鼠标悬停时调用）
    /// </summary>
    public void ShowIntentTooltip()
    {
        if (targetEnemy == null)
        {
            targetEnemy = GetComponentInParent<EnemyBattleEntity>();
        }

        Debug.Log($"[意图悬浮] ShowIntentTooltip 被调用 - intentTooltip: {(intentTooltip != null ? "OK" : "NULL")}, targetEnemy: {(targetEnemy != null ? targetEnemy.gameObject.name : "NULL")}");

        if (intentTooltip != null && targetEnemy != null)
        {
            EnemyIntent intent = targetEnemy.GetCurrentIntent();
            Debug.Log($"[意图悬浮] 意图: {(intent != null ? intent.type.ToString() : "NULL")}, icon: {(intent?.icon != null ? "OK" : "NULL")}");

            if (intent != null)
            {
                UpdateIntentTooltip(intent);
                intentTooltip.SetActive(true);
                Debug.Log($"[意图悬浮] 显示悬浮提示成功");
            }
        }
        else
        {
            Debug.LogWarning($"[意图悬浮] 无法显示 - intentTooltip: {(intentTooltip != null ? "OK" : "NULL")}, targetEnemy: {(targetEnemy != null ? "OK" : "NULL")}");
        }
    }

    /// <summary>
    /// 隐藏意图悬浮提示（鼠标离开时调用）
    /// </summary>
    public void HideIntentTooltip()
    {
        if (intentTooltip != null)
        {
            intentTooltip.SetActive(false);
        }
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

    private void RefreshShield()
    {
        if (targetStats == null || shieldSlider == null) return;

        // 护盾条最大值设为角色最大HP（参考杀戮尖塔）
        shieldSlider.maxValue = targetStats.maxHP;
        shieldSlider.value = targetStats.shield;

        // 如果没有护盾，隐藏护盾条
        shieldSlider.gameObject.SetActive(targetStats.shield > 0);
    }

    private void RefreshBuffIcons()
    {
        if (buffContainer == null || buffIconPrefab == null || targetStats == null) return;

        // 清空旧的
        foreach (Transform child in buffContainer)
        {
            Destroy(child.gameObject);
        }

        // 获取 BuffTooltipPanel（如果有）
        BuffTooltipPanel tooltipPanel = FindObjectOfType<BuffTooltipPanel>();

        // 动态克隆新的状态图标，水平布局组全自动对齐
        foreach (Buff buff in targetStats.activeBuffs)
        {
            GameObject iconObj = Instantiate(buffIconPrefab, buffContainer);

            // 1. 设置 Buff 的精灵图片
            BuffIcon iconScript = iconObj.GetComponent<BuffIcon>();
            if (iconScript != null)
            {
                iconScript.Setup(buff);
            }

            // 2. 自动抓取子物体上的 Text，动态刷入"剩余回合数"与"层数（x2）"
            Text turnText = iconObj.GetComponentInChildren<Text>();
            if (turnText != null)
            {
                turnText.text = buff.stacks > 1 ? $"{buff.durationTurns}\n<size=10>x{buff.stacks}</size>" : buff.durationTurns.ToString();
            }

            // 3. 添加 Buff 悬浮提示触发器
            if (tooltipPanel != null)
            {
                // 确保有 Raycast Target（才能接收鼠标事件）
                Image iconImage = iconObj.GetComponent<Image>();
                if (iconImage != null)
                    iconImage.raycastTarget = true;

                // 添加触发器组件
                BuffTooltipTrigger trigger = iconObj.AddComponent<BuffTooltipTrigger>();
                trigger.Setup(buff, tooltipPanel);
            }
        }
    }
}