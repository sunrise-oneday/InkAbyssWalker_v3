using System;
using UnityEngine;

/// <summary>
/// 对话流程管理器（纯 C# 单例）
/// 管理当前对话状态、行推进、结束。
/// 不引用 UI，通过事件广播驱动 UI 层。
/// </summary>
public class DialogueManager
{
    private static DialogueManager instance;
    public static DialogueManager Instance
    {
        get
        {
            if (instance == null) instance = new DialogueManager();
            return instance;
        }
    }

    private DialogueData currentData;
    private int currentLineIndex;
    private bool isTypewriterComplete;

    /// <summary>当前是否有对话在进行</summary>
    public bool IsDialogueActive => currentData != null;

    /// <summary>当前行索引</summary>
    public int CurrentLineIndex => currentLineIndex;

    /// <summary>当前总行数</summary>
    public int TotalLines => currentData != null ? currentData.Lines.Count : 0;

    /// <summary>当前行是否已完全显示</summary>
    public bool IsCurrentLineComplete => isTypewriterComplete;

    // ============================================
    // 事件
    // ============================================

    /// <summary>对话开始（参数: DialogueData）</summary>
    public event Action<DialogueData> OnDialogueStarted = delegate { };

    /// <summary>对话结束</summary>
    public event Action OnDialogueEnded = delegate { };

    /// <summary>切换到新行（参数: 行内容, 行索引）</summary>
    public event Action<DialogueData.DialogueLine, int> OnLineChanged = delegate { };

    /// <summary>当前行打字完成</summary>
    public event Action OnLineCompleted = delegate { };

    private DialogueManager() { }

    // ============================================
    // 公共 API
    // ============================================

    /// <summary>开始一段对话</summary>
    public void StartDialogue(DialogueData data)
    {
        if (data == null || data.Lines.Count == 0)
        {
            Debug.LogWarning("[DialogueManager] 尝试开始空对话，已忽略。");
            return;
        }

        if (IsDialogueActive)
        {
            Debug.LogWarning("[DialogueManager] 已有对话进行中，先结束当前对话。");
            EndDialogue();
        }

        currentData = data;
        currentLineIndex = 0;
        isTypewriterComplete = false;

        OnDialogueStarted.Invoke(data);
        EmitCurrentLine();
    }

    /// <summary>推进到下一行。
    /// 如果当前行未完成打字，先完成当前行；如果已完成，推进到下一行。</summary>
    public void AdvanceLine()
    {
        if (!IsDialogueActive) return;

        if (!isTypewriterComplete)
        {
            isTypewriterComplete = true;
            OnLineCompleted.Invoke();
            return;
        }

        currentLineIndex++;

        if (currentLineIndex >= currentData.Lines.Count)
        {
            EndDialogue();
            return;
        }

        isTypewriterComplete = false;
        EmitCurrentLine();
    }

    /// <summary>强制结束当前对话</summary>
    public void EndDialogue()
    {
        if (!IsDialogueActive) return;

        currentData = null;
        currentLineIndex = 0;
        isTypewriterComplete = false;

        OnDialogueEnded.Invoke();
    }

    /// <summary>由 DialoguePanel 在打字动画完成时调用</summary>
    public void NotifyTypewriterComplete()
    {
        isTypewriterComplete = true;
        OnLineCompleted.Invoke();
    }

    /// <summary>获取当前行数据</summary>
    public DialogueData.DialogueLine GetCurrentLine()
    {
        if (!IsDialogueActive) return null;
        if (currentLineIndex < 0 || currentLineIndex >= currentData.Lines.Count) return null;
        return currentData.Lines[currentLineIndex];
    }

    // ============================================
    // 内部
    // ============================================

    private void EmitCurrentLine()
    {
        var line = GetCurrentLine();
        if (line != null)
        {
            OnLineChanged.Invoke(line, currentLineIndex);
            line.OnLineShow?.Invoke();
        }
    }
}
