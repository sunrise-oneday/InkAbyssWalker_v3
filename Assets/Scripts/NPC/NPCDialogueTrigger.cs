using UnityEngine;

/// <summary>
/// NPC 对话触发器
/// 挂在 NPC GameObject 上，检测玩家靠近后按 E 键开始对话。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NPCDialogueTrigger : MonoBehaviour
{
    [Header("对话数据")]
    [SerializeField] private DialogueData dialogueData;

    [Header("触发设置")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool requirePlayerController = true;

    [Header("UI 反馈")]
    [Tooltip("靠近时显示的交互提示（如 '按 E 对话'）")]
    [SerializeField] private GameObject promptRoot;

    public bool IsPlayerInside { get; private set; }
    public DialogueData DialogueData => dialogueData;

    private GameplayInputReader gameplayInput;
    private bool inputSubscribed;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[NPCDialogueTrigger] {name} 的 Collider2D 未勾选 Is Trigger。");

        SetPromptVisible(false);
    }

    private void OnEnable()
    {
        TrySubscribeInput();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
        IsPlayerInside = false;
        SetPromptVisible(false);
    }

    private void Update()
    {
        if (!inputSubscribed)
            TrySubscribeInput();
    }

    // ============================================
    // 输入订阅
    // ============================================

    private void TrySubscribeInput()
    {
        if (inputSubscribed) return;
        if (InputManager.Instance == null) return;

        gameplayInput = InputManager.Instance.Gameplay;
        if (gameplayInput == null) return;

        gameplayInput.OnInteractPressed += OnInteractPressed;
        inputSubscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (!inputSubscribed) return;
        if (gameplayInput != null)
            gameplayInput.OnInteractPressed -= OnInteractPressed;
        inputSubscribed = false;
    }

    private void OnInteractPressed()
    {
        if (!InputContextSwitcher.CanUseExploreInput()) return;
        if (UIManager.Instance != null && UIManager.Instance.IsAnyPanelOpen) return;
        if (!IsPlayerInside) return;

        if (dialogueData == null || dialogueData.Lines.Count == 0)
        {
            Debug.LogWarning($"[NPCDialogueTrigger] {name} 的 dialogueData 为空或无内容。");
            return;
        }

        TryStartDialogue();
    }

    // ============================================
    // 对话启停
    // ============================================

    private void TryStartDialogue()
    {
        DialogueManager.Instance.StartDialogue(dialogueData);
        DialogueManager.Instance.OnDialogueEnded += OnDialogueEnded;

        InputContextSwitcher.PauseExploreInput();
        SetPromptVisible(false);
    }

    private void OnDialogueEnded()
    {
        DialogueManager.Instance.OnDialogueEnded -= OnDialogueEnded;

        if (!UIManager.Instance.IsAnyPanelOpen)
            InputContextSwitcher.RestoreExploreInput();

        if (IsPlayerInside)
            SetPromptVisible(true);
    }

    // ============================================
    // 碰撞检测
    // ============================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerCollider(other)) return;

        IsPlayerInside = true;
        SetPromptVisible(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other)) return;

        IsPlayerInside = false;
        SetPromptVisible(false);
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null) return false;

        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
            return false;

        if (!requirePlayerController) return true;

        return other.GetComponentInParent<PlayerController>() != null;
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptRoot != null)
            promptRoot.SetActive(visible);
    }
}
