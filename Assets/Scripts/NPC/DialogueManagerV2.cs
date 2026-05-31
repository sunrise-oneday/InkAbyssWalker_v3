using System;
using UnityEngine;
using DialogueSystem;

/// <summary>
/// 对话管理器 V2
/// 纯C#单例，管理新对话系统的全局状态
/// 提供简单的API供其他系统调用
/// </summary>
public class DialogueManagerV2 : MonoBehaviour
{
    // ===== 单例 =====
    public static DialogueManagerV2 Instance { get; private set; }

    [Header("依赖")]
    [SerializeField] private InkDialogueInputBridge inputBridge;
    [SerializeField] private InkGameConditionProvider conditionProvider;
    [SerializeField] private InkGameActionExecutor actionExecutor;
    [SerializeField] private InkDialogueSaveBridge saveBridge;
    [SerializeField] private DialoguePanelV2 dialoguePanel;

    // ===== 对话引擎 =====
    private DialogueGraphRunner runner;

    // ===== 事件 =====
    /// <summary>对话开始</summary>
    public event Action OnDialogueStarted;

    /// <summary>对话结束</summary>
    public event Action OnDialogueEnded;

    /// <summary>进入新节点</summary>
    public event Action<DialogueNode, string> OnNodeEntered;

    /// <summary>显示选项</summary>
    public event Action<DialogueChoiceOption[]> OnChoicePresented;

    /// <summary>玩家选择了选项</summary>
    public event Action<int> OnChoiceSelected;

    // ===== 属性 =====
    /// <summary>对话引擎实例</summary>
    public DialogueGraphRunner Runner => runner;

    /// <summary>是否正在对话中</summary>
    public bool IsDialogueActive => runner != null && runner.IsActive;

    /// <summary>当前变量状态</summary>
    public DialogueVariableState Variables => runner?.Variables;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    // ===== 初始化 =====

    private void Initialize()
    {
        // 创建对话引擎
        runner = new DialogueGraphRunner(conditionProvider, actionExecutor);

        // 设置UI提供者
        if (dialoguePanel != null)
        {
            runner.SetUIProvider(dialoguePanel);
        }
    }

    // ===== 事件订阅 =====

    private void SubscribeEvents()
    {
        if (runner != null)
        {
            runner.OnDialogueStarted += HandleDialogueStarted;
            runner.OnDialogueEnded += HandleDialogueEnded;
            runner.OnNodeEntered += HandleNodeEntered;
            runner.OnChoicePresented += HandleChoicePresented;
            runner.OnChoiceSelected += HandleChoiceSelected;
        }

        if (inputBridge != null)
        {
            inputBridge.OnConfirmPressed += HandleConfirmPressed;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.OnClickAdvance += HandleClickAdvance;
        }
    }

    private void UnsubscribeEvents()
    {
        if (runner != null)
        {
            runner.OnDialogueStarted -= HandleDialogueStarted;
            runner.OnDialogueEnded -= HandleDialogueEnded;
            runner.OnNodeEntered -= HandleNodeEntered;
            runner.OnChoicePresented -= HandleChoicePresented;
            runner.OnChoiceSelected -= HandleChoiceSelected;
        }

        if (inputBridge != null)
        {
            inputBridge.OnConfirmPressed -= HandleConfirmPressed;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.OnClickAdvance -= HandleClickAdvance;
        }
    }

    // ===== 公共API =====

    /// <summary>
    /// 开始对话
    /// </summary>
    public void StartDialogue(DialogueGraph graph, string startNodeId = null)
    {
        if (runner == null)
        {
            Debug.LogError("[DialogueManagerV2] Runner未初始化");
            return;
        }

        if (runner.IsActive)
        {
            Debug.LogWarning("[DialogueManagerV2] 已有对话进行中，先结束当前对话");
            runner.EndDialogue();
        }

        runner.StartDialogue(graph, startNodeId);
    }

    /// <summary>
    /// 开始对话（使用DialogueGraphConfig）
    /// </summary>
    public void StartDialogue(DialogueGraphConfig config, string startNodeId = null)
    {
        if (config == null)
        {
            Debug.LogError("[DialogueManagerV2] Config为null");
            return;
        }

        var graph = config.LoadGraph();
        if (graph == null)
        {
            Debug.LogError("[DialogueManagerV2] 无法加载对话图");
            return;
        }

        string nodeId = !string.IsNullOrEmpty(startNodeId) ? startNodeId : config.StartNodeId;
        StartDialogue(graph, nodeId);
    }

    /// <summary>
    /// 开始对话（使用TextAsset）
    /// </summary>
    public void StartDialogue(TextAsset jsonAsset, string startNodeId = null)
    {
        if (jsonAsset == null)
        {
            Debug.LogError("[DialogueManagerV2] JsonAsset为null");
            return;
        }

        var graph = DialogueGraph.FromJson(jsonAsset.text);
        if (graph == null)
        {
            Debug.LogError("[DialogueManagerV2] 无法解析对话JSON");
            return;
        }

        StartDialogue(graph, startNodeId);
    }

    /// <summary>
    /// 推进对话
    /// </summary>
    public void AdvanceLine()
    {
        if (!IsDialogueActive) return;
        runner.AdvanceLine();
    }

    /// <summary>
    /// 选择选项
    /// </summary>
    public void SelectChoice(int optionIndex)
    {
        if (!IsDialogueActive) return;
        runner.SelectChoice(optionIndex);
    }

    /// <summary>
    /// 结束对话
    /// </summary>
    public void EndDialogue()
    {
        if (!IsDialogueActive) return;
        runner.EndDialogue();
    }

    /// <summary>
    /// 获取变量值（bool）
    /// </summary>
    public bool GetVariableBool(string key, bool defaultValue = false)
    {
        return Variables?.GetBool(key, defaultValue) ?? defaultValue;
    }

    /// <summary>
    /// 获取变量值（int）
    /// </summary>
    public int GetVariableInt(string key, int defaultValue = 0)
    {
        return Variables?.GetInt(key, defaultValue) ?? defaultValue;
    }

    /// <summary>
    /// 获取变量值（string）
    /// </summary>
    public string GetVariableString(string key, string defaultValue = "")
    {
        return Variables?.GetString(key, defaultValue) ?? defaultValue;
    }

    /// <summary>
    /// 设置变量值
    /// </summary>
    public void SetVariable(string key, object value)
    {
        if (Variables == null) return;

        if (value is bool boolVal)
            Variables.SetBool(key, boolVal);
        else if (value is int intVal)
            Variables.SetInt(key, intVal);
        else if (value is string stringVal)
            Variables.SetString(key, stringVal);
        else
            Variables.SetString(key, value?.ToString() ?? "");
    }

    // ===== 事件处理 =====

    private void HandleConfirmPressed()
    {
        if (!IsDialogueActive) return;
        runner.AdvanceLine();
    }

    private void HandleClickAdvance()
    {
        if (!IsDialogueActive) return;
        runner.AdvanceLine();
    }

    private void HandleDialogueStarted()
    {
        // 暂停游戏输入
        if (inputBridge != null)
        {
            inputBridge.PauseGameInput();
            inputBridge.SetDialogueInputActive(true);
        }

        OnDialogueStarted?.Invoke();
    }

    private void HandleDialogueEnded()
    {
        // 恢复游戏输入
        if (inputBridge != null)
        {
            inputBridge.SetDialogueInputActive(false);
            inputBridge.RestoreGameInput();
        }

        OnDialogueEnded?.Invoke();
    }

    private void HandleNodeEntered(DialogueNode node, string resolvedText)
    {
        OnNodeEntered?.Invoke(node, resolvedText);
    }

    private void HandleChoicePresented(DialogueChoiceOption[] options)
    {
        // 设置选项选择回调
        if (dialoguePanel != null)
        {
            dialoguePanel.SetChoiceCallback(OnOptionSelectedInternal);
        }

        OnChoicePresented?.Invoke(options);
    }

    private void HandleChoiceSelected(int optionIndex)
    {
        OnChoiceSelected?.Invoke(optionIndex);
    }

    private void OnOptionSelectedInternal(int optionIndex)
    {
        runner.SelectChoice(optionIndex);
    }
}
