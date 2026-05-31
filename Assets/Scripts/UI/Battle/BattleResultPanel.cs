using UnityEngine;

/// <summary>
/// 战斗结果面板：胜利/战败显示
/// </summary>
public class BattleResultPanel : BasePanel
{
    [Header("胜负面板")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;

    public void ShowVictory()
    {
        victoryPanel?.SetActive(true);
        defeatPanel?.SetActive(false);
    }

    public void ShowDefeat()
    {
        victoryPanel?.SetActive(false);
        defeatPanel?.SetActive(true);
    }

    public void HideAll()
    {
        victoryPanel?.SetActive(false);
        defeatPanel?.SetActive(false);
    }
}
