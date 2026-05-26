using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
            sharedMpText.text = $"{BattleManager.Instance.sharedMP}/{BattleManager.Instance.maxSharedMP}";
        }

        SetupFormButtons();
        SetupSkillButtons();
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
}