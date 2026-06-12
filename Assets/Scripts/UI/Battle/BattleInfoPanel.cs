using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 顶部信息面板：回合数、共享 AP/MP、大招能量槽
/// </summary>
public class BattleInfoPanel : BasePanel
{
    [Header("回合指示")]
    [SerializeField] private Text turnText;

    [Header("共享资源（Shader 驱动）")]
    [SerializeField] private APSlotController apSlotController;
    [SerializeField] private MPSlotController mpSlotController;

    [Header("大招能量")]
    [SerializeField] private Slider sharedUltSlider;
    [SerializeField] private Text sharedUltText;
    [SerializeField] private Button ultimateButton;

    private PlayerBattleEntity mainPlayer;

    public void SetPlayer(PlayerBattleEntity player)
    {
        mainPlayer = player;
    }

    public Button UltimateButton => ultimateButton;

    public override void OnRefresh()
    {
        if (mainPlayer == null) return;

        // 回合
        if (turnText != null)
            turnText.text = $"回合 {BattleTurnManager.Instance.currentTurn}";

        // AP / MP 由 APSlotController / MPSlotController 自行在 LateUpdate 中轮询 BattleResourceManager 更新

        // 大招进度条
        if (sharedUltSlider != null)
        {
            sharedUltSlider.maxValue = BattleResourceManager.Instance.maxSharedUltimateEnergy;
            sharedUltSlider.value = BattleResourceManager.Instance.sharedUltimateEnergy;
        }
        if (sharedUltText != null)
            sharedUltText.text = $"{BattleResourceManager.Instance.sharedUltimateEnergy}%";

        // 大招按钮交互状态
        if (ultimateButton != null)
        {
            bool canCastUlt = (BattleResourceManager.Instance.sharedUltimateEnergy >= BattleResourceManager.Instance.maxSharedUltimateEnergy)
                              && (BattleTurnManager.Instance.currentPhase == BattlePhase.PlayerTurn);
            ultimateButton.interactable = canCastUlt;

            // Tooltip
            var trigger = ultimateButton.GetComponent<UITooltipTrigger>();
            if (trigger == null) trigger = ultimateButton.gameObject.AddComponent<UITooltipTrigger>();

            string ultName = mainPlayer.equippedUltimate != null ? mainPlayer.equippedUltimate.ultimateName : "未装备大招";
            string ultDesc = mainPlayer.equippedUltimate != null ? mainPlayer.equippedUltimate.description : "大招能量达到 100% 后，可点击此处释放。";
            trigger.SetTooltipData(ultName, "消耗: 100% 怒气", ultDesc, 0, 0);
        }
    }

    /// <summary>设置大招按钮监听</summary>
    public void BindUltimateButton(System.Action onClick)
    {
        if (ultimateButton != null)
        {
            ultimateButton.onClick.RemoveAllListeners();
            ultimateButton.onClick.AddListener(() => onClick?.Invoke());
        }
    }
}
