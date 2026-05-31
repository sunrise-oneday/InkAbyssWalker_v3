using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏设置面板
/// 退出游戏、返回标题（预留）等功能
/// </summary>
public class GameSettingsPanel : BaseSettingsPanel
{
    [Header("操作按钮")]
    [SerializeField] private Button quitButton;
    [SerializeField] private Button returnToTitleButton;

    private void Start()
    {
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        if (returnToTitleButton != null)
            returnToTitleButton.onClick.AddListener(ReturnToTitle);
    }

    /// <summary>退出游戏</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>返回标题画面（预留：需创建 TitleScene 后启用）</summary>
    public void ReturnToTitle()
    {
        Debug.Log("[GameSettings] 返回标题画面（尚未实现 TitleScene）");
        // TODO: SceneManager.LoadScene("TitleScene");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
