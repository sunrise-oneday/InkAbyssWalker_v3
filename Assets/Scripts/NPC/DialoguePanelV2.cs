using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DialogueSystem;

/// <summary>
/// 对话 UI 面板 V2
/// 实现 IDialogueUIProvider 接口，支持新对话系统
/// 包含：打字机效果、文本显示、点击推进、选项显示
/// </summary>
public class DialoguePanelV2 : MonoBehaviour, IDialogueUIProvider, IPointerClickHandler
{
    [Header("UI 引用")]
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject continueIndicator;

    [Header("NPC 头像（可选）")]
    [SerializeField] private UnityEngine.UI.Image speakerPortrait;

    [Header("选项 UI")]
    [SerializeField] private GameObject optionsRoot;
    [SerializeField] private Transform optionsContainer;
    [SerializeField] private GameObject optionButtonPrefab;

    [Header("设置")]
    [SerializeField] private float defaultTypewriterSpeed = 0.03f;
    [SerializeField] private float punctuationPauseMultiplier = 4f;

    // ===== 状态 =====
    private bool isOpen;
    private Coroutine typewriterCoroutine;
    private string currentFullText;
    private bool isTypewriterComplete;
    private bool isChoiceMode;
    private Action<int> onChoiceSelected;

    // ===== IDialogueUIProvider 实现 =====
    public bool IsTypewriterComplete
    {
        get => isTypewriterComplete;
        set => isTypewriterComplete = value;
    }

    private void Awake()
    {
        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
        isOpen = false;
        SetContinueIndicator(false);

        if (optionsRoot != null)
            optionsRoot.SetActive(false);
    }

    // ===== IDialogueUIProvider 实现 =====

    public void ShowDialogue()
    {
        if (dialogueRoot != null)
            dialogueRoot.SetActive(true);
        isOpen = true;
        isChoiceMode = false;
    }

    public void HideDialogue()
    {
        StopTypewriter();
        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
        isOpen = false;
        isChoiceMode = false;

        if (speakerNameText != null) speakerNameText.text = "";
        if (dialogueText != null) dialogueText.text = "";
        SetContinueIndicator(false);

        HideOptions();
    }

    public void ShowLine(string speaker, string text, string portraitKey, float speed)
    {
        if (!isOpen) ShowDialogue();

        // 设置说话人名称
        if (speakerNameText != null)
            speakerNameText.text = speaker;

        // 设置对话文本
        if (dialogueText != null)
            dialogueText.text = "";

        currentFullText = text;

        // 确定打字机速度
        float actualSpeed = speed > 0f ? speed : defaultTypewriterSpeed;

        // 隐藏选项，显示对话
        HideOptions();
        isChoiceMode = false;
        SetContinueIndicator(false);

        // 启动打字机效果
        StartTypewriter(text, actualSpeed);
    }

    public void ShowChoices(string prompt, string[] optionTexts)
    {
        if (!isOpen) ShowDialogue();

        // 停止打字机
        StopTypewriter();

        // 隐藏对话文本，显示选项
        if (dialogueText != null)
            dialogueText.text = prompt ?? "";

        isChoiceMode = true;
        SetContinueIndicator(false);

        // 显示选项
        ShowOptions(optionTexts);
    }

    public void ShowFullText()
    {
        StopTypewriter();
        if (dialogueText != null && currentFullText != null)
            dialogueText.text = currentFullText;
    }

    // ===== IPointerClickHandler 实现 =====

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isOpen) return;

        // 如果是选项模式，不响应点击推进
        if (isChoiceMode) return;

        // 如果打字机正在运行，显示完整文本
        if (!isTypewriterComplete && typewriterCoroutine != null)
        {
            ShowFullText();
            return;
        }

        // 通知外部推进对话
        OnClickAdvance?.Invoke();
    }

    // ===== 事件 =====

    /// <summary>点击对话框推进对话</summary>
    public event Action OnClickAdvance;

    // ===== 选项处理 =====

    private void ShowOptions(string[] optionTexts)
    {
        if (optionsRoot == null || optionsContainer == null || optionButtonPrefab == null)
        {
            Debug.LogWarning("[DialoguePanelV2] 选项UI未配置");
            return;
        }

        // 清除旧选项
        foreach (Transform child in optionsContainer)
        {
            Destroy(child.gameObject);
        }

        // 创建新选项按钮
        for (int i = 0; i < optionTexts.Length; i++)
        {
            int index = i; // 捕获索引
            var buttonObj = Instantiate(optionButtonPrefab, optionsContainer);
            buttonObj.SetActive(true);

            // 设置按钮文本
            var textComponent = buttonObj.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
                textComponent.text = optionTexts[i];

            // 设置按钮点击事件
            var button = buttonObj.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnOptionClicked(index));
            }
        }

        optionsRoot.SetActive(true);
    }

    private void HideOptions()
    {
        if (optionsRoot != null)
            optionsRoot.SetActive(false);

        // 清除选项按钮
        if (optionsContainer != null)
        {
            foreach (Transform child in optionsContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void OnOptionClicked(int index)
    {
        isChoiceMode = false;
        HideOptions();
        onChoiceSelected?.Invoke(index);
    }

    /// <summary>
    /// 设置选项选择回调（由NPCDialogueTrigger调用）
    /// </summary>
    public void SetChoiceCallback(Action<int> callback)
    {
        onChoiceSelected = callback;
    }

    // ===== 打字机效果 =====

    private void StartTypewriter(string fullText, float speed)
    {
        StopTypewriter();
        currentFullText = fullText;

        typewriterCoroutine = StartCoroutine(TypewriterCoroutine(fullText, speed));
    }

    private IEnumerator TypewriterCoroutine(string text, float speed)
    {
        isTypewriterComplete = false;
        dialogueText.text = "";

        for (int i = 0; i < text.Length; i++)
        {
            dialogueText.text += text[i];

            // 标点符号额外停顿
            if (text[i] == '。' || text[i] == '！' || text[i] == '？' ||
                text[i] == '.' || text[i] == '!' || text[i] == '?')
            {
                yield return new WaitForSeconds(speed * punctuationPauseMultiplier);
            }
            else
            {
                yield return new WaitForSeconds(speed);
            }
        }

        isTypewriterComplete = true;
        typewriterCoroutine = null;

        // 显示继续指示器
        SetContinueIndicator(true);
    }

    private void StopTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        isTypewriterComplete = false;
    }

    private void SetContinueIndicator(bool visible)
    {
        if (continueIndicator != null)
            continueIndicator.SetActive(visible);
    }
}
