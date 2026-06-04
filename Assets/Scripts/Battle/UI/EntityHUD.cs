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
    [SerializeField] private Image hpFillImage;    // 自定义 Shader HP（自动查找子物体中名为"HP"的Image）
    private Material hpFillMaterial;                 // 深拷贝材质实例，防止多敌人血条串值
    [SerializeField] private Slider breakSlider; // 破防条 (可选)

    [Header("HP 数值显示（杀戮尖塔风格）")]
    [SerializeField] private TMP_Text hpValueText;  // HP 数值文本（可选，不拖则自动创建）
    [SerializeField] private Vector2 hpTextOffset = new Vector2(0f, 2f);   // HP 文本相对于血条的偏移
    [SerializeField] private float hpTextFontSize = 14f;                    // HP 文本字号

    [Header("护盾显示（图标+数字）")]
    [SerializeField] private GameObject shieldContainer;    // 护盾容器（可选，不拖则自动创建）
    [SerializeField] private Image shieldIcon;              // 护盾图标
    [SerializeField] private TMP_Text shieldValueText;      // 护盾数值文本
    [SerializeField] private Vector2 shieldOffset = new Vector2(-5f, 0f);  // 护盾容器相对于血条左侧的偏移
    [SerializeField] private float shieldIconSize = 24f;                    // 护盾图标大小
    [SerializeField] private float shieldFontSize = 18f;                    // 护盾数字字号

    [Header("意图图标显示")]
    [SerializeField] private Image intentIcon;              // 意图图标
    [SerializeField] private GameObject intentContainer;    // 意图容器
    [SerializeField] private TMP_Text intentValueText;      // 意图数值文本（可选）
    [SerializeField] private GameObject intentTooltip;      // 意图悬浮提示面板
    [SerializeField] private Text intentTooltipText;        // 意图描述文本

    [Header("Buff 状态栏配置 [1]")]
    [SerializeField] private Transform buffContainer;
    [SerializeField] private GameObject buffIconPrefab;

    [Header("选中视觉表现 [可选]")]
    [SerializeField] private GameObject selectionIndicator; // 拖入一个作为"选定红圈"或"向下箭头"的子物体

    [Header("当前行动指示器")]
    [SerializeField] private GameObject currentAttackerIndicator;  // ▼向下箭头，标记当前行动的敌人
    [SerializeField] private Color attackerIndicatorColor = new Color(1f, 0.85f, 0f, 1f); // 金黄色
    [SerializeField] private Vector3 attackerIndicatorOffset = new Vector3(0, 80f, 0);    // 头顶偏移

    [Header("世界空间缩放（直接填数值）")]
    [SerializeField] private float hudScale = 0.02f;
    [SerializeField] private float hudScaleFactor = 0.022f;

    private void Start()
    {
        // 自动查找 HP Image（HPbar_Billboard Shader 血条）
        // 策略：1) 优先搜子物体 "HP" 2) 次之搜自身（EntityHUD 直接挂在 HP GameObject 上的情况）
        bool hpIsOnSelf = false;
        if (hpFillImage == null)
        {
            var hpChild = transform.Find("HP");
            if (hpChild != null)
                hpFillImage = hpChild.GetComponent<Image>();
        }
        if (hpFillImage == null)
        {
            hpFillImage = GetComponent<Image>(); // 自身就是 HP
            hpIsOnSelf = hpFillImage != null;
        }

        // 缩放：只在 HP 是子物体时对父节点等比缩放。EntityHUD 直接挂在 HP 上时不覆盖 localScale
        if (!hpIsOnSelf)
            transform.localScale = new Vector3(hudScale, hudScale, 1f);

        if (hpFillImage != null)
            Debug.Log($"[EntityHUD] HP Image 就绪: {hpFillImage.name} (transform={transform.name})");

        // 深拷贝材质，每个敌人独立一份，SetFloat 不会串值
        if (hpFillImage != null && hpFillMaterial == null)
        {
            hpFillMaterial = new Material(hpFillImage.material);
            hpFillImage.material = hpFillMaterial;
        }
        if (hpFillImage == null)
        {
            Debug.LogWarning($"[EntityHUD] 未找到 HP Image，血条只走 Slider。" +
                           $"transform={transform.name}, childCount={transform.childCount}");
        }

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

        // 确保当前行动箭头初始隐藏
        if (currentAttackerIndicator != null)
            currentAttackerIndicator.SetActive(false);

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
    /// 设置当前是否为正在行动的敌人（显示/隐藏向下箭头▼指示器）
    /// 与 SetSelected 完全独立，二者可以共存
    /// </summary>
    public void SetCurrentAttacker(bool isAttacking)
    {
        if (currentAttackerIndicator == null && isAttacking)
        {
            CreateAttackerIndicator();
        }

        if (currentAttackerIndicator != null)
        {
            currentAttackerIndicator.SetActive(isAttacking);
        }
    }

    /// <summary>
    /// 代码动态创建向下箭头指示器（fallback，当 Inspector 未手动配置时使用）
    /// </summary>
    private void CreateAttackerIndicator()
    {
        GameObject arrowObj = new GameObject("CurrentAttackerArrow");
        arrowObj.transform.SetParent(transform, false);
        arrowObj.transform.localPosition = attackerIndicatorOffset;
        arrowObj.transform.localScale = Vector3.one;

        var tmp = arrowObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "\u25BC";  // ▼
        tmp.fontSize = 28;
        tmp.color = attackerIndicatorColor;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontStyle = TMPro.FontStyles.Bold;

        var fitter = arrowObj.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        currentAttackerIndicator = arrowObj;
        Debug.Log($"[EntityHUD] 动态创建 CurrentAttackerArrow (GameObject={gameObject.name})");
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

        // 确保文本可见
        intentTooltipText.color = Color.white;
        intentTooltipText.fontSize = 14;
        intentTooltipText.alignment = TextAnchor.MiddleCenter;
        intentTooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
        intentTooltipText.verticalOverflow = VerticalWrapMode.Overflow;
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
            targetEnemy = GetComponentInParent<EnemyBattleEntity>();

        if (targetEnemy == null || intentTooltip == null) return;

        EnemyIntent intent = targetEnemy.GetCurrentIntent();
        if (intent == null) return;

        UpdateIntentTooltip(intent);
        intentTooltip.SetActive(true);
    }

    /// <summary>
    /// 隐藏意图悬浮提示（鼠标离开时调用）
    /// </summary>
    public void HideIntentTooltip()
    {
        if (intentTooltip != null)
            intentTooltip.SetActive(false);
    }

    // ========================================================
    // 3. 事件驱动的分流刷新函数：数据一变，瞬间定向自重画，性能极佳！ [1, 5]
    // ========================================================

    private void RefreshHP()
    {
        if (targetStats == null) return;

        // 更新 Slider（如果存在且启用）
        if (hpSlider != null && hpSlider.gameObject.activeInHierarchy)
        {
            hpSlider.maxValue = targetStats.maxHP;
            hpSlider.value = targetStats.currentHP;
        }

        // 更新自定义 Image HP 条（HPbar_Billboard Shader 用 _CurrentHP 控制进度）
        if (hpFillMaterial != null)
        {
            float ratio = targetStats.maxHP > 0
                ? (float)targetStats.currentHP / targetStats.maxHP
                : 0f;
            hpFillMaterial.SetFloat("_CurrentHP", ratio);
        }

        // 确保 HP 文本存在并更新
        EnsureHPText();
        if (hpValueText != null)
        {
            hpValueText.text = $"{targetStats.currentHP}/{targetStats.maxHP}";
        }
    }

    private void RefreshBreak()
    {
        if (targetStats == null || breakSlider == null) return;
        breakSlider.maxValue = targetStats.maxBreakValue;
        breakSlider.value = targetStats.currentBreakValue;
    }

    private void RefreshShield()
    {
        if (targetStats == null) return;

        // 确保护盾 UI 元素存在（动态创建 fallback）
        EnsureShieldUI();

        bool hasShield = targetStats.shield > 0;
        if (shieldContainer != null)
        {
            shieldContainer.SetActive(hasShield);
            if (hasShield && shieldValueText != null)
            {
                shieldValueText.text = targetStats.shield.ToString();
            }
        }
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

    // ========================================================
    // 动态创建 UI 元素（当 Inspector 未配置时自动创建）
    // ========================================================

    /// <summary>
    /// 确保护盾 UI 元素存在（图标+数字），如果 Inspector 未配置则动态创建
    /// </summary>
    private void EnsureShieldUI()
    {
        if (shieldContainer != null) return;

        // 动态创建护盾容器
        GameObject container = new GameObject("ShieldDisplay");
        container.transform.SetParent(transform, false);

        RectTransform containerRect = container.AddComponent<RectTransform>();
        // 放在血条左侧
        containerRect.anchorMin = new Vector2(0f, 0.5f);
        containerRect.anchorMax = new Vector2(0f, 0.5f);
        containerRect.pivot = new Vector2(1f, 0.5f);
        containerRect.anchoredPosition = shieldOffset;  // 使用 Inspector 可调参数
        containerRect.sizeDelta = new Vector2(60f, 30f);

        // 水平布局
        var layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 2f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // 护盾图标
        GameObject iconObj = new GameObject("ShieldIcon");
        iconObj.transform.SetParent(container.transform, false);
        shieldIcon = iconObj.AddComponent<Image>();
        Sprite shieldSprite = Resources.Load<Sprite>("UI/Buffs/Icon_Shield");
        if (shieldSprite != null)
        {
            shieldIcon.sprite = shieldSprite;
        }
        shieldIcon.color = new Color(0.4f, 0.7f, 1f); // 淡蓝色调
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(shieldIconSize, shieldIconSize);  // 使用 Inspector 可调参数

        // 护盾数值文本（两位数字 99 需要足够宽度）
        GameObject textObj = new GameObject("ShieldValue");
        textObj.transform.SetParent(container.transform, false);
        shieldValueText = textObj.AddComponent<TextMeshProUGUI>();
        shieldValueText.text = "0";
        shieldValueText.fontSize = shieldFontSize;  // 使用 Inspector 可调参数
        shieldValueText.color = new Color(0.6f, 0.85f, 1f); // 淡蓝色
        shieldValueText.fontStyle = FontStyles.Bold;
        shieldValueText.alignment = TextAlignmentOptions.MidlineRight;
        shieldValueText.enableWordWrapping = false;  // 禁止换行
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(50f, 28f);  // 足够容纳两位数字

        shieldContainer = container;
        Debug.Log($"[EntityHUD] 动态创建护盾显示 (transform={transform.name})");
    }

    /// <summary>
    /// 确保 HP 文本存在，如果 Inspector 未配置则动态创建
    /// </summary>
    private void EnsureHPText()
    {
        if (hpValueText != null) return;

        // 在 HP 条上方创建 HP 文本
        GameObject textObj = new GameObject("HPValueText");
        textObj.transform.SetParent(transform, false);

        hpValueText = textObj.AddComponent<TextMeshProUGUI>();
        hpValueText.text = "0/0";
        hpValueText.fontSize = hpTextFontSize;  // 使用 Inspector 可调参数
        hpValueText.color = Color.white;
        hpValueText.fontStyle = FontStyles.Bold;
        hpValueText.alignment = TextAlignmentOptions.Center;

        // 添加黑色描边/阴影增加可读性
        var shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(1f, -1f);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        // 居中放置在血条上方
        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.anchoredPosition = hpTextOffset;  // 使用 Inspector 可调参数
        textRect.sizeDelta = new Vector2(150f, 20f);

        Debug.Log($"[EntityHUD] 动态创建 HP 数值文本 (transform={transform.name})");
    }
}