using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗 UI 管理器（Facade 模式）
/// 统一管理战斗场景中的所有面板组件。
///
/// 【职责】
/// 1. 持有所有战斗面板的引用
/// 2. 对外暴露统一的 API（外部代码通过 BattleUIController.Instance 调用）
/// 3. 内部逻辑委托给具体的面板类处理
///
/// 【扩展现有面板】
/// 在 Inspector 中拖拽赋值即可。
///
/// 【添加新面板】
/// 1. 创建继承 BasePanel 的新类，写好独立逻辑和 SerializeField
/// 2. 在此类中添加 [SerializeField] 引用
/// 3. 在需要的地方调用 panel.OnOpen/OnClose/OnRefresh
/// </summary>
public class BattleUIController : MonoBehaviour
{
    public static BattleUIController Instance { get; private set; }

    [Header("Canvas 根节点")]
    [SerializeField] private GameObject battlePanel;

    [Header("面板组件")]
    [SerializeField] private BattleInfoPanel infoPanel;
    [SerializeField] private BattleActionPanel actionPanel;
    [SerializeField] private BuffTooltipPanel buffTooltip;
    [SerializeField] private FixedTooltipPanel fixedTooltip;
    [SerializeField] private BattleResultPanel resultPanel;
    [SerializeField] private EntityHudSpawner hudSpawner;

    private PlayerBattleEntity mainPlayer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (battlePanel != null) battlePanel.SetActive(false);
    }

    private void Start()
    {
        // 绑定全局按钮事件
        actionPanel?.BindEndTurnButton(() => BattleTurnManager.Instance.EnterEnemyTurn());
    }

    // ============================================
    // Facade API — 对外暴露，内部委托给面板
    // ============================================

    /// <summary>初始化战斗 UI（在战斗开始时调用）</summary>
    public void InitializeUI(List<PlayerBattleEntity> party, List<EnemyBattleEntity> enemies)
    {
        if (party.Count == 0 || enemies.Count == 0) return;
        mainPlayer = party[0];

        if (battlePanel != null) battlePanel.SetActive(true);

        hudSpawner?.SpawnHUDs(party);
        infoPanel?.SetPlayer(mainPlayer);
        actionPanel?.SetPlayer(mainPlayer);
        infoPanel?.BindUltimateButton(() => mainPlayer?.CastUltimate());

        RefreshUI();
    }

    /// <summary>刷新所有面板数据</summary>
    public void RefreshUI()
    {
        infoPanel?.OnRefresh();
        actionPanel?.OnRefresh();
    }

    /// <summary>刷新所有实体血条</summary>
    public void RefreshHUDs()
    {
        hudSpawner?.RefreshAll();
    }

    /// <summary>启用/禁用操作面板（敌人回合禁用）</summary>
    public void SetActionPanelActive(bool active)
    {
        actionPanel?.SetInteractable(active);
    }

    /// <summary>显示胜利面板</summary>
    public void ShowVictoryPanel(bool _) => resultPanel?.ShowVictory();

    /// <summary>显示战败面板</summary>
    public void ShowDefeatPanel(bool _) => resultPanel?.ShowDefeat();

    /// <summary>显示 Buff 浮动提示</summary>
    public void ShowTooltipList(List<Buff> activeBuffs) => buffTooltip?.Show(activeBuffs);

    /// <summary>隐藏 Buff 浮动提示</summary>
    public void HideTooltip() => buffTooltip?.Hide();

    /// <summary>显示技能详情提示（固定位置）</summary>
    public void ShowFixedTooltip(string title, string cost, string desc, int reqMp, int reqAp)
        => fixedTooltip?.Show(title, cost, desc, reqMp, reqAp);

    /// <summary>隐藏技能详情提示</summary>
    public void HideFixedTooltip() => fixedTooltip?.Hide();

    /// <summary>关闭所有战斗 UI</summary>
    public void CloseUI()
    {
        hudSpawner?.ClearAll();
        resultPanel?.HideAll();
        if (battlePanel != null) battlePanel.SetActive(false);
    }
}
