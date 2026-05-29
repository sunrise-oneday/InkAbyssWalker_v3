using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BattleUIController : MonoBehaviour
{
    public static BattleUIController Instance { get; private set; }

    [Header("Canvas 整体控制")]
    [SerializeField] private GameObject battleCanvas;

    [Header("回合制顶层全局数据")]
    [SerializeField] private Text turnText;

    [Header("队伍共享资源 UI [共用]")]
    [SerializeField] private Text sharedApText;
    [SerializeField] private Slider sharedMpSlider;
    [SerializeField] private Text sharedMpText;

    [Header("玩家队伍属性容器 (底部的 Horizontal Layout Group)")]
    [SerializeField] private Transform playerHUDContainer;
    [SerializeField] private GameObject playerHUDPrefab;

    // ========================================================
    // 核心重构：将原本的 GameObject 隐藏改为用 CanvasGroup 进行“半透明交互锁死”控制！
    // ========================================================
    [Header("行动面板 CanvasGroup 控制")]
    [SerializeField] private CanvasGroup actionPanelGroup;  // 拖入挂载了 CanvasGroup 组件的行动大面板物体 [2]

    [SerializeField] private Button endTurnButton;
    [SerializeField] private List<Button> skillButtons;
    [SerializeField] private List<Button> formButtons;
    // ========================================================

    // ========================================================
    // 核心重构：杀戮尖塔式多 Buff 排队提示面板 [1]
    // ========================================================
    [Header("杀戮尖塔式 提示面板 [1]")]
    [SerializeField] private GameObject tooltipPanel;         // 提示面板大盒子物体
    [SerializeField] private Transform tooltipListContainer;  // 大盒子里的垂直布局组容器（从上往下排队）
    [SerializeField] private GameObject tooltipRowPrefab;     // 单个 Buff 描述的单行预制体（左图右文） [1]

    // ========================================================
    // 核心重构：大招 UGUI 数据绑定 [3]
    // ========================================================
    [Header("队伍共享大招 UI [左上角]")]
    [SerializeField] private Slider sharedUltSlider;       // 全局公共大招能量条 [3]
    [SerializeField] private Text sharedUltText;           // 显示公共大招百分比，如 "100%" [3]
    [SerializeField] private Button ultimateButton;        // 大招释放按钮
                                                           // ========================================================

    [Header("固定位置描述面板 [新增]")]
    [SerializeField] private CanvasGroup fixedTooltipGroup; // 描述大面板的 CanvasGroup [1]
    [SerializeField] private CanvasGroup tooltipTextGroup;  // 内部所有文本的子 CanvasGroup（用于文字极速渐变）
    [SerializeField] private Text fixedTitleText;          // 提示名字 [1]
    [SerializeField] private Text fixedCostText;           // 提示消耗 [1]
    [SerializeField] private Text fixedDescText;           // 提示描述 [1]

    // 核心新增：用于控制纯原生协程动画的指针缓存，防抖打断 [2]
    private Coroutine activeFadeRoutine;
    private Coroutine activeScaleRoutine;
    private Coroutine activeTextRoutine;

    private string currentActiveTitle = ""; // 记录当前亮起的描述标题，用作防重判断

    [Header("胜负面板 [新增]")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    public void ShowVictoryPanel(bool isShow) => victoryPanel?.SetActive(isShow);
    public void ShowDefeatPanel(bool isShow) => defeatPanel?.SetActive(isShow);

    private List<GameObject> spawnedHUDs = new List<GameObject>();
    private PlayerBattleEntity mainPlayer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (battleCanvas != null) battleCanvas.SetActive(false);
        if (tooltipPanel != null) tooltipPanel.SetActive(false); // 默认隐藏提示框
    }

    private void Start()
    {
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
        }

        // ========================================================
        // 核心：绑定大招按钮的 onClick 点击事件！
        // ========================================================
        if (ultimateButton != null)
        {
            ultimateButton.onClick.AddListener(OnUltimateButtonClicked);
        }
    }

    private void Update()
    {
        // 提示：如果提示面板正处于显示状态，每帧命令它跟着鼠标移动
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            UpdateTooltipPosition();
        }
    }

    /// <summary>
    /// 提供给外部调用：显示 Buff 描述面板 [1]
    /// </summary>
    /// <summary>
    /// 核心重构：展示杀戮尖塔式的多 Buff 排队提示框 [1]
    /// </summary>
    /// <param name="activeBuffs">当前角色身上正在挂载的全部 Buff 列表</param>
    public void ShowTooltipList(List<Buff> activeBuffs)
    {
        if (tooltipPanel == null || tooltipListContainer == null || tooltipRowPrefab == null) return;
        if (activeBuffs == null || activeBuffs.Count == 0) return;

        // 1. 清空旧的提示条（打扫战场）
        foreach (Transform child in tooltipListContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. 遍历该角色身上所有的 Buff，动态克隆并生成一行行的描述条！ [1]
        foreach (Buff buff in activeBuffs)
        {
            GameObject rowObj = Instantiate(tooltipRowPrefab, tooltipListContainer);

            // 自动寻找并渲染左侧的 Buff 图像 [1]
            Image img = rowObj.transform.Find("Icon")?.GetComponent<Image>();
            if (img != null) img.sprite = buff.icon;

            // 自动寻找并渲染右侧的名字 [1]
            Text nameTxt = rowObj.transform.Find("txtName")?.GetComponent<Text>();
            if (nameTxt != null) nameTxt.text = buff.buffName;

            // 自动寻找并渲染右侧的描述 [1]
            Text descTxt = rowObj.transform.Find("txtDesc")?.GetComponent<Text>();
            if (descTxt != null)
            {
                descTxt.text = $"{buff.description} <color=yellow>(剩 {buff.durationTurns} 回合)</color>";
            }
        }

        // ========================================================
        // 3. 核心修复：用代码强制对提示大面板挂载并设置 CanvasGroup！
        // 强行关闭 blocksRaycasts 阻挡。这样鼠标射线会完全穿透它，一闪一闪的 Bug 永久被消除！ [2]
        // ========================================================
        CanvasGroup cg = tooltipPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = tooltipPanel.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        tooltipPanel.SetActive(true);
        UpdateTooltipPosition();
    }

    /// <summary>
    /// 提供给外部调用：隐藏 Buff 描述面板
    /// </summary>
    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 让面板跟着鼠标走的位移逻辑（微调偏移，防止挡住指针） [1]
    /// </summary>
    private void UpdateTooltipPosition()
    {
        // 获取鼠标当前的屏幕像素坐标，并稍微往左上方进行像素微调
        Vector2 mousePos = Input.mousePosition;
        tooltipPanel.transform.position = mousePos + new Vector2(-120f, 60f); // 偏左 120 像素，偏上 60 像素
    }

    public void InitializeUI(List<PlayerBattleEntity> party, List<EnemyBattleEntity> enemies)
    {
        if (party.Count == 0 || enemies.Count == 0) return;

        mainPlayer = party[0];

        if (battleCanvas != null) battleCanvas.SetActive(true);

        ClearOldHUDs();

        if (playerHUDContainer != null && playerHUDPrefab != null)
        {
            foreach (var member in party)
            {
                if (member == null) continue;

                GameObject hudObj = Instantiate(playerHUDPrefab, playerHUDContainer);
                spawnedHUDs.Add(hudObj);

                EntityHUD hudScript = hudObj.GetComponent<EntityHUD>();
                if (hudScript != null)
                {
                    hudScript.SetTargetStats(member.Stats);
                    hudScript.RefreshAll();
                }
            }
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (mainPlayer == null) return;

        if (turnText != null)
        {
            turnText.text = $"回合 {BattleManager.Instance.currentTurn}";
        }

        if (sharedApText != null)
        {
            sharedApText.text = $"AP: {BattleManager.Instance.sharedAP}/{BattleManager.Instance.maxSharedAP}";
        }

        if (sharedMpSlider != null)
        {
            sharedMpSlider.maxValue = BattleManager.Instance.maxSharedMP;
            sharedMpSlider.value = BattleManager.Instance.sharedMP;
        }

        if (sharedMpText != null)
        {
            sharedMpText.text = $"MP:{BattleManager.Instance.sharedMP}/{BattleManager.Instance.maxSharedMP}";
        }

        // ========================================================
        // 核心新增：更新左上角大招进度条，并动态计算大招按钮的亮起状态！ [3]
        // ========================================================
        if (sharedUltSlider != null)
        {
            sharedUltSlider.maxValue = BattleManager.Instance.maxSharedUltimateEnergy;
            sharedUltSlider.value = BattleManager.Instance.sharedUltimateEnergy;
        }

        if (sharedUltText != null)
        {
            sharedUltText.text = $"{BattleManager.Instance.sharedUltimateEnergy}%";
        }

        if (ultimateButton != null)
        {
            // 亮起规则：只有当大招能量满了（>=100），并且当前处于玩家自己的回合时，按钮才允许高亮亮起！ [3]
            bool canCastUlt = (BattleManager.Instance.sharedUltimateEnergy >= BattleManager.Instance.maxSharedUltimateEnergy)
                              && (BattleManager.Instance.currentPhase == BattlePhase.PlayerTurn);

            ultimateButton.interactable = canCastUlt; // 自动控制置灰与亮起

            // ========================================================
            // 核心新增：自动为大招（终结奥义）按钮动态注入并更新提示数据！
            // ========================================================
            UITooltipTrigger trigger = ultimateButton.GetComponent<UITooltipTrigger>();
            if (trigger == null) trigger = ultimateButton.gameObject.AddComponent<UITooltipTrigger>();

            string ultName = mainPlayer.equippedUltimate != null ? mainPlayer.equippedUltimate.ultimateName : "未装备大招";
            string ultDesc = mainPlayer.equippedUltimate != null ? mainPlayer.equippedUltimate.description : "大招能量满 100% 后，可点击爆裂释放。";
            trigger.SetTooltipData(ultName, "消耗: 100% 怒气", ultDesc, 0, 0); // 传入 0 消耗，大招怒气另外单独判断
        }

        SetupFormButtons();
        SetupSkillButtons();
    }

    private void OnUltimateButtonClicked()
    {
        // 触发大招释放
        if (mainPlayer != null)
        {
            mainPlayer.CastUltimate();
        }
    }

    private void SetupFormButtons()
    {
        if (mainPlayer == null) return;
        var forms = mainPlayer.availableForms;

        for (int i = 0; i < formButtons.Count; i++)
        {
            Button btn = formButtons[i];
            if (btn == null) continue;

            if (i < forms.Count)
            {
                btn.gameObject.SetActive(true);
                PlayerForm form = forms[i];

                Text btnText = btn.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.text = $"{form.formName}\n({form.apCostToSwitch} AP)";
                }

                // ========================================================
                // 全自动：为形态切换按钮动态注入并更新提示数据！
                // ========================================================
                UITooltipTrigger trigger = btn.GetComponent<UITooltipTrigger>();
                if (trigger == null) trigger = btn.gameObject.AddComponent<UITooltipTrigger>();
                // 传入 AP 实际消耗参数，用于红字判断 [3]
                trigger.SetTooltipData(form.formName, $"消耗: {form.apCostToSwitch} AP", "变身为此形态，并全自动刷新匹配该形态的技能卡牌。", 0, form.apCostToSwitch);

                int index = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    mainPlayer.SwitchForm(index);
                    RefreshUI();
                    UpdateAllHUDs();
                });
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }

    private void SetupSkillButtons()
    {
        if (mainPlayer == null || mainPlayer.CurrentForm == null) return;

        var activeSkills = mainPlayer.CurrentForm.availableSkills;

        for (int i = 0; i < skillButtons.Count; i++)
        {
            Button btn = skillButtons[i];
            if (btn == null) continue;

            if (i < activeSkills.Count)
            {
                btn.gameObject.SetActive(true);
                Skill skill = activeSkills[i];

                Text btnText = btn.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.text = $"{skill.skillName}\n({skill.mpCost} MP / {skill.breakDamage} 削韧)";
                }

                // ========================================================
                // 全自动：为当前形态下的技能按钮动态注入并更新提示数据！
                // ========================================================
                UITooltipTrigger trigger = btn.GetComponent<UITooltipTrigger>();
                if (trigger == null) trigger = btn.gameObject.AddComponent<UITooltipTrigger>();
                // 传入 MP 实际消耗参数，用于红字判断 [3]
                trigger.SetTooltipData(skill.skillName, $"消耗: {skill.mpCost} MP", $"{skill.baseDamage}点威力，对目标造成 {skill.breakDamage}点白色削韧伤害。", skill.mpCost, 0);

                btn.onClick.RemoveAllListeners();

                // ========================================================
                // 核心修改：点击释放技能时，目标对准我们在 BattleManager 中点击选中的怪物！
                // ========================================================
                btn.onClick.AddListener(() =>
                {
                    var target = BattleManager.Instance.selectedEnemy; // 动态寻找选中的怪！ [2]
                    if (target != null)
                    {
                        mainPlayer.CastSkill(skill, target);
                        RefreshUI();
                        UpdateAllHUDs();
                    }
                    else
                    {
                        Debug.LogWarning("[UI 提示] 请用鼠标先点击场景中的怪物作为攻击目标！ [2]");
                    }
                });
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateAllHUDs()
    {
        foreach (var hud in spawnedHUDs)
        {
            if (hud != null)
            {
                EntityHUD script = hud.GetComponent<EntityHUD>();
                if (script != null) script.RefreshAll();
            }
        }
    }

    private void ClearOldHUDs()
    {
        foreach (var hud in spawnedHUDs)
        {
            if (hud != null) Destroy(hud);
        }
        spawnedHUDs.Clear();
    }

    public void CloseUI()
    {
        ClearOldHUDs();
        if (battleCanvas != null) battleCanvas.SetActive(false);
        if(victoryPanel != null) victoryPanel.SetActive(false);
        if(defeatPanel != null) defeatPanel.SetActive(false);
    }

    // ========================================================
    // 核心重构：使用 CanvasGroup 代对整个面板进行“置灰、锁死交互”
    // ========================================================
    public void SetActionPanelActive(bool isActive)
    {
        if (actionPanelGroup != null)
        {
            // 激活时：不透明度为 1，开启鼠标射线阻挡（可正常点击）
            // 锁死时（敌人回合）：半透明（置灰表现），关闭鼠标射线，使其完全无法点击！
            actionPanelGroup.alpha = isActive ? 1.0f : 0.45f;
            actionPanelGroup.blocksRaycasts = isActive;
        }
    }

    private void OnEndTurnClicked()
    {
        BattleManager.Instance.EnterEnemyTurn();
    }

    // ========================================================================
    // 纯 Unity 协程缓动引擎 (支持极速无缝滑动切换)
    // ========================================================================

    /// <summary>
    /// 核心重构：展现固定位置的描述面板（支持：红字警告、防一关一开瞬闪、极速对齐） [1, 2, 3]
    /// </summary>
    /// <summary>
    /// 核心重构：展现固定位置的描述面板（用 activeSelf 代替 alpha 判断，彻底破解逻辑闭环陷阱） [1, 2, 3]
    /// </summary>
    public void ShowFixedTooltip(string title, string cost, string desc, int reqMp, int reqAp)
    {
        if (fixedTooltipGroup == null) return;

        // 1. 极致滑手感：如果上一次的“隐藏/淡出”协程正在运行，在第一时间将其立刻掐断！
        if (activeFadeRoutine != null)
        {
            StopCoroutine(activeFadeRoutine);
            activeFadeRoutine = null;
        }

        // 处理资源不足时的红字警告
        string finalCostText = cost;
        bool isMpEnough = BattleManager.Instance.sharedMP >= reqMp;
        bool isApEnough = BattleManager.Instance.sharedAP >= reqAp;

        if (cost.Contains("怒气") && BattleManager.Instance.sharedUltimateEnergy < BattleManager.Instance.maxSharedUltimateEnergy)
        {
            finalCostText = $"<color=red>{cost} (怒气未满！)</color>";
        }
        else if (!isMpEnough)
        {
            finalCostText = $"<color=red>{cost} (法力不足！)</color>";
        }
        else if (!isApEnough)
        {
            finalCostText = $"<color=red>{cost} (行动点不足！)</color>";
        }

        // 如果是当前正在显示的，且不透明度是满的，直接返回
        if (currentActiveTitle == title && fixedTooltipGroup.alpha > 0.9f) return;

        // ========================================================
        // 核心修复：用 activeSelf（大面板物体是否在层级树中处于激活状态）来代替 alpha 判定！
        // 这样可以彻底避免“因为提前强行将 alpha 设为 1，导致系统误以为面板已经亮起，
        // 从而跳过了 SetActive(true) 激活步骤并导致协程闪退”的致命逻辑陷阱！
        // ========================================================
        bool isPanelAlreadyActive = fixedTooltipGroup.gameObject.activeSelf;

        if (isPanelAlreadyActive)
        {
            // 【分支 A】：如果面板已经在显示（由于快速滑动，从一个键扫到另一个键）
            currentActiveTitle = title;

            // 既然是通过快速滑动重新唤醒了面板，我们在这里强行将其不透明度恢复到 1.0f 正常颜色！
            fixedTooltipGroup.alpha = 1f;

            if (activeTextRoutine != null) StopCoroutine(activeTextRoutine);

            // 极速淡入淡出文字
            activeTextRoutine = StartCoroutine(CrossFadeTextRoutine(title, finalCostText, desc));
        }
        else
        {
            // 【分支 B】：如果是第一次从无到有亮起（activeSelf 为 false）
            currentActiveTitle = title;

            // 写入初始文字
            if (fixedTitleText != null) fixedTitleText.text = title;
            if (fixedCostText != null) fixedCostText.text = finalCostText;
            if (fixedDescText != null) fixedDescText.text = desc;
            if (tooltipTextGroup != null) tooltipTextGroup.alpha = 1f;

            if (activeScaleRoutine != null) StopCoroutine(activeScaleRoutine);

            // 核心：在第一帧首先物理激活大面板！
            fixedTooltipGroup.gameObject.SetActive(true);

            // 开启原生协程渐入与回弹缩放过渡
            activeFadeRoutine = StartCoroutine(FadeGroupRoutine(fixedTooltipGroup, 1f, 0.15f));
            activeScaleRoutine = StartCoroutine(ScalePopRoutine(fixedTooltipGroup.transform, 0.18f));
        }
    }

    /// <summary>
    /// 新增：淡出隐藏描述大面板（带安全保护）
    /// </summary>
    public void HideFixedTooltip()
    {
        if (fixedTooltipGroup == null) return;

        currentActiveTitle = "";

        // ========================================================
        // 核心安全修复：如果整个 Canvas 已经被隐藏（例如战斗结算完毕），
        // 绝对不允许在此刻启动任何 StartCoroutine（否则 Unity 会报致命的 activeInHierarchy 闪退警告）！ [3]
        // ========================================================
        if (!gameObject.activeInHierarchy || !enabled)
        {
            fixedTooltipGroup.alpha = 0f;
            fixedTooltipGroup.gameObject.SetActive(false);
            return;
        }

        // 杀掉所有活动动画
        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
        if (activeScaleRoutine != null) StopCoroutine(activeScaleRoutine);
        if (activeTextRoutine != null) StopCoroutine(activeTextRoutine);

        // 0.12秒平滑淡出并关闭
        activeFadeRoutine = StartCoroutine(FadeGroupRoutine(fixedTooltipGroup, 0f, 0.12f, true));
    }

    private IEnumerator FadeGroupRoutine(CanvasGroup group, float targetAlpha, float duration, bool deactivateOnComplete = false)
    {
        float startAlpha = group.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            group.alpha = Mathf.SmoothStep(startAlpha, targetAlpha, t);
            yield return null;
        }

        group.alpha = targetAlpha;
        if (deactivateOnComplete && targetAlpha <= 0.05f)
        {
            group.gameObject.SetActive(false);
        }
    }

    private IEnumerator ScalePopRoutine(Transform t, float duration)
    {
        Vector3 startScale = new Vector3(0.85f, 0.85f, 1f);
        Vector3 targetScale = Vector3.one;
        t.localScale = startScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / duration;

            float s = 1.70158f;
            float value = 1f - Mathf.Pow(1f - p, 3f) * (1f - p * (s + 1f) - s);

            t.localScale = Vector3.LerpUnclamped(startScale, targetScale, value);
            yield return null;
        }

        t.localScale = targetScale;
    }

    private IEnumerator CrossFadeTextRoutine(string title, string cost, string desc)
    {
        if (tooltipTextGroup == null) yield break;

        // 1. 文字极速淡出 (0.08 秒)
        float elapsed = 0f;
        float duration = 0.08f;
        float startAlpha = tooltipTextGroup.alpha;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tooltipTextGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            yield return null;
        }
        tooltipTextGroup.alpha = 0f;

        // 2. 极速换字
        if (fixedTitleText != null) fixedTitleText.text = title;
        if (fixedCostText != null) fixedCostText.text = cost;
        if (fixedDescText != null) fixedDescText.text = desc;

        // 3. 文字极速淡入 (0.12 秒)
        elapsed = 0f;
        duration = 0.12f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tooltipTextGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        tooltipTextGroup.alpha = 1f;
    }

}