using UnityEngine;
using DialogueSystem;

/// <summary>
/// NPC对话触发器 V2
/// 使用接口化设计，支持新对话系统
/// 支持：JSON对话数据、分支对话、条件判断、变量系统
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NPCDialogueTriggerV2 : MonoBehaviour
{
    [Header("对话数据")]
    [Tooltip("对话JSON文件（TextAsset）")]
    [SerializeField] private TextAsset dialogueJson;

    [Tooltip("或使用DialogueGraphConfig资产")]
    [SerializeField] private DialogueGraphConfig dialogueConfig;

    [Tooltip("起始节点ID（留空使用JSON中的startNodeId）")]
    [SerializeField] private string startNodeId;

    [Header("依赖（场景中的对象）")]
    [SerializeField] private DialogueGraphRunner dialogueRunner;
    [SerializeField] private DialoguePanelV2 dialoguePanel;
    [SerializeField] private InkDialogueInputBridge inputBridge;
    [SerializeField] private InkGameConditionProvider conditionProvider;
    [SerializeField] private InkGameActionExecutor actionExecutor;
    [SerializeField] private InkDialogueSaveBridge saveBridge;

    [Header("触发设置")]
    [Tooltip("玩家标签")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("交互提示UI（如'按E对话'）")]
    [SerializeField] private GameObject promptRoot;

    [Header("存档设置")]
    [Tooltip("是否自动保存/加载对话状态")]
    [SerializeField] private bool autoSaveState = true;

    // ===== 状态 =====
    private DialogueGraph currentGraph;
    private bool isPlayerInside;
    private bool isDialogueActive;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[NPCDialogueTriggerV2] {name} 的 Collider2D 未勾选 Is Trigger");

        SetPromptVisible(false);
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        isPlayerInside = false;
        SetPromptVisible(false);
    }

    private void Start()
    {
        // 初始化对话引擎
        InitializeDialogueRunner();
    }

    // ===== 初始化 =====

    private void InitializeDialogueRunner()
    {
        if (dialogueRunner == null)
        {
            Debug.LogError($"[NPCDialogueTriggerV2] {name} 的 DialogueRunner 未设置");
            return;
        }

        // 设置UI提供者
        if (dialoguePanel != null)
        {
            dialogueRunner.SetUIProvider(dialoguePanel);
        }
    }

    // ===== 事件订阅 =====

    private void SubscribeEvents()
    {
        if (inputBridge != null)
        {
            inputBridge.OnConfirmPressed += HandleConfirmPressed;
        }

        if (dialogueRunner != null)
        {
            dialogueRunner.OnDialogueStarted += HandleDialogueStarted;
            dialogueRunner.OnDialogueEnded += HandleDialogueEnded;
            dialogueRunner.OnChoicePresented += HandleChoicePresented;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.OnClickAdvance += HandleClickAdvance;
        }
    }

    private void UnsubscribeEvents()
    {
        if (inputBridge != null)
        {
            inputBridge.OnConfirmPressed -= HandleConfirmPressed;
        }

        if (dialogueRunner != null)
        {
            dialogueRunner.OnDialogueStarted -= HandleDialogueStarted;
            dialogueRunner.OnDialogueEnded -= HandleDialogueEnded;
            dialogueRunner.OnChoicePresented -= HandleChoicePresented;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.OnClickAdvance -= HandleClickAdvance;
        }
    }

    // ===== 对话启动 =====

    private void TryStartDialogue()
    {
        if (isDialogueActive)
        {
            Debug.LogWarning($"[NPCDialogueTriggerV2] {name} 已有对话进行中");
            return;
        }

        // 加载对话数据
        if (!LoadDialogueData())
        {
            Debug.LogError($"[NPCDialogueTriggerV2] {name} 无法加载对话数据");
            return;
        }

        // 加载保存的状态
        if (autoSaveState && saveBridge != null)
        {
            var (varsJson, histJson) = saveBridge.LoadDialogueState(currentGraph.dialogueId);
            if (!string.IsNullOrEmpty(varsJson))
            {
                dialogueRunner.LoadState(varsJson, histJson);
            }
        }

        // 暂停游戏输入
        if (inputBridge != null)
        {
            inputBridge.PauseGameInput();
            inputBridge.SetDialogueInputActive(true);
        }

        // 隐藏交互提示
        SetPromptVisible(false);

        // 启动对话
        string nodeId = !string.IsNullOrEmpty(startNodeId) ? startNodeId : null;
        dialogueRunner.StartDialogue(currentGraph, nodeId);
    }

    private bool LoadDialogueData()
    {
        // 尝试从DialogueGraphConfig加载
        if (dialogueConfig != null)
        {
            currentGraph = dialogueConfig.LoadGraph();
            if (currentGraph != null) return true;
        }

        // 尝试从TextAsset加载
        if (dialogueJson != null)
        {
            currentGraph = DialogueGraph.FromJson(dialogueJson.text);
            if (currentGraph != null) return true;
        }

        Debug.LogError($"[NPCDialogueTriggerV2] {name} 没有配置对话数据");
        return false;
    }

    // ===== 事件处理 =====

    private void HandleConfirmPressed()
    {
        if (!isDialogueActive) return;
        if (!isPlayerInside) return;

        // 推进对话
        dialogueRunner.AdvanceLine();
    }

    private void HandleClickAdvance()
    {
        if (!isDialogueActive) return;

        // 推进对话
        dialogueRunner.AdvanceLine();
    }

    private void HandleDialogueStarted()
    {
        isDialogueActive = true;
    }

    private void HandleDialogueEnded()
    {
        isDialogueActive = false;

        // 保存对话状态
        if (autoSaveState && saveBridge != null && currentGraph != null)
        {
            var (varsJson, histJson) = dialogueRunner.SaveState();
            saveBridge.SaveDialogueState(currentGraph.dialogueId, varsJson, histJson);
        }

        // 恢复游戏输入
        if (inputBridge != null)
        {
            inputBridge.SetDialogueInputActive(false);
            inputBridge.RestoreGameInput();
        }

        // 显示交互提示（如果仍在触发区域内）
        if (isPlayerInside)
        {
            SetPromptVisible(true);
        }
    }

    private void HandleChoicePresented(DialogueChoiceOption[] options)
    {
        // 设置选项选择回调
        if (dialoguePanel != null)
        {
            dialoguePanel.SetChoiceCallback(OnOptionSelected);
        }
    }

    private void OnOptionSelected(int optionIndex)
    {
        // 选择选项
        dialogueRunner.SelectChoice(optionIndex);
    }

    // ===== 碰撞检测 =====

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerCollider(other)) return;

        isPlayerInside = true;

        if (!isDialogueActive)
        {
            SetPromptVisible(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other)) return;

        isPlayerInside = false;
        SetPromptVisible(false);
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null) return false;

        // 检查标签
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
            return false;

        // 检查是否有PlayerController组件（可选）
        // 如果项目中没有PlayerController，可以移除这个检查
        // return other.GetComponentInParent<PlayerController>() != null;

        return true;
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptRoot != null)
            promptRoot.SetActive(visible);
    }
}
