using UnityEngine;
using StoreAndInventory;

/// <summary>
/// UI 管理器（单例）
/// 统一管理所有面板的打开/关闭、互斥检查
/// ActionMap 切换委托给 InputContextSwitcher
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("面板引用")]
    [SerializeField] private CharacterPanel characterPanel;
    [SerializeField] private SettingsController settingsController;

    [Header("互斥面板引用（避免 FindObjectOfType）")]
    [SerializeField] private StoreInventoryPanelController storePanel;
    [SerializeField] private DialoguePanel dialoguePanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>当前是否有任何面板打开</summary>
    public bool IsAnyPanelOpen =>
        (characterPanel != null && characterPanel.IsOpen) ||
        (settingsController != null && settingsController.IsOpen) ||
        (storePanel != null && storePanel.IsAnyOpen) ||
        (dialoguePanel != null && dialoguePanel.IsOpen);

    // ============================================
    // 面板打开/关闭 API（由各面板自行调用）
    // ============================================

    public void ToggleCharacterPanel()
    {
        if (characterPanel == null) return;

        if (characterPanel.IsOpen)
            CloseCharacterPanel();
        else
            OpenCharacterPanel();
    }

    public void OpenCharacterPanel()
    {
        if (characterPanel == null || characterPanel.IsOpen) return;

        // 互斥检查
        if (storePanel != null && storePanel.IsAnyOpen) return;
        if (settingsController != null && settingsController.IsOpen) return;

        InputContextSwitcher.PauseExploreInput("OpenCharacterPanel");
        characterPanel.SetActive(true);
    }

    public void CloseCharacterPanel()
    {
        if (characterPanel == null || !characterPanel.IsOpen) return;

        characterPanel.SetActive(false);
        if (!IsAnyPanelOpen)
            InputContextSwitcher.RestoreExploreInput();
    }

    public void CloseAllPanels()
    {
        if (characterPanel != null && characterPanel.IsOpen)
            characterPanel.SetActive(false);

        if (settingsController != null && settingsController.IsOpen)
            settingsController.Close();

        if (dialoguePanel != null && dialoguePanel.IsOpen)
            DialogueManager.Instance.EndDialogue();

        InputContextSwitcher.RestoreExploreInput();
    }
}
