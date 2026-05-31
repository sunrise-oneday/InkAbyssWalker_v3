using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 对话 UI 面板
/// 纯 UI 渲染层：打字机效果、文本显示、点击推进。
/// 不继承 BasePanel，对话生命周期由 DialogueManager 驱动。
/// </summary>
public class DialoguePanel : MonoBehaviour, IPointerClickHandler
{
    [Header("UI 引用")]
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject continueIndicator;

    [Header("NPC 头像（可选）")]
    [SerializeField] private UnityEngine.UI.Image speakerPortrait;

    private bool isOpen;
    private Coroutine typewriterCoroutine;
    private string currentFullText;
    private bool isTypewriterRunning;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
        isOpen = false;
        SetContinueIndicator(false);
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    // ============================================
    // 事件订阅
    // ============================================

    private void SubscribeEvents()
    {
        DialogueManager.Instance.OnDialogueStarted += HandleDialogueStarted;
        DialogueManager.Instance.OnLineChanged += HandleLineChanged;
        DialogueManager.Instance.OnLineCompleted += HandleLineCompleted;
        DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
    }

    private void UnsubscribeEvents()
    {
        DialogueManager.Instance.OnDialogueStarted -= HandleDialogueStarted;
        DialogueManager.Instance.OnLineChanged -= HandleLineChanged;
        DialogueManager.Instance.OnLineCompleted -= HandleLineCompleted;
        DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
    }

    // ============================================
    // DialogueManager 事件处理
    // ============================================

    private void HandleDialogueStarted(DialogueData data)
    {
        Show();
    }

    private void HandleLineChanged(DialogueData.DialogueLine line, int index)
    {
        if (speakerNameText != null)
            speakerNameText.text = line.SpeakerName;

        if (dialogueText != null)
            dialogueText.text = "";

        currentFullText = line.Text;

        float speed = DialogueManager.Instance.GetCurrentLine().TypewriterSpeed;
        if (speed <= 0f)
            speed = 0.03f;

        StartTypewriter(line.Text, speed);
        SetContinueIndicator(false);
    }

    private void HandleLineCompleted()
    {
        if (isTypewriterRunning)
        {
            StopTypewriter();
            ShowFullText();
        }

        bool isLastLine = DialogueManager.Instance.CurrentLineIndex
                          >= DialogueManager.Instance.TotalLines - 1;
        SetContinueIndicator(!isLastLine);
    }

    private void HandleDialogueEnded()
    {
        StopTypewriter();
        Hide();
    }

    // ============================================
    // 点击处理
    // ============================================

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isOpen) return;
        if (!DialogueManager.Instance.IsDialogueActive) return;

        DialogueManager.Instance.AdvanceLine();
    }

    // ============================================
    // 打字机效果
    // ============================================

    private void StartTypewriter(string fullText, float speed)
    {
        StopTypewriter();
        currentFullText = fullText;

        if (DialogueManager.Instance.IsCurrentLineComplete)
        {
            ShowFullText();
            return;
        }

        typewriterCoroutine = StartCoroutine(TypewriterCoroutine(fullText, speed));
    }

    private IEnumerator TypewriterCoroutine(string text, float speed)
    {
        isTypewriterRunning = true;
        dialogueText.text = "";

        for (int i = 0; i < text.Length; i++)
        {
            dialogueText.text += text[i];

            if (DialogueManager.Instance.IsCurrentLineComplete)
            {
                ShowFullText();
                break;
            }

            if (text[i] == '\u3002' || text[i] == '\uff01' || text[i] == '\uff1f' ||
                text[i] == '.' || text[i] == '!' || text[i] == '?')
            {
                yield return new WaitForSeconds(speed * 4f);
            }
            else
            {
                yield return new WaitForSeconds(speed);
            }
        }

        isTypewriterRunning = false;

        if (!DialogueManager.Instance.IsCurrentLineComplete)
        {
            DialogueManager.Instance.NotifyTypewriterComplete();
        }
    }

    private void StopTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        isTypewriterRunning = false;
    }

    private void ShowFullText()
    {
        StopTypewriter();
        if (dialogueText != null && currentFullText != null)
            dialogueText.text = currentFullText;

        bool isLastLine = DialogueManager.Instance != null &&
                          DialogueManager.Instance.CurrentLineIndex
                          >= DialogueManager.Instance.TotalLines - 1;
        SetContinueIndicator(!isLastLine);
    }

    // ============================================
    // 显示/隐藏
    // ============================================

    private void Show()
    {
        if (dialogueRoot != null)
            dialogueRoot.SetActive(true);
        isOpen = true;
    }

    private void Hide()
    {
        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
        isOpen = false;

        if (speakerNameText != null) speakerNameText.text = "";
        if (dialogueText != null) dialogueText.text = "";
        SetContinueIndicator(false);
    }

    private void SetContinueIndicator(bool visible)
    {
        if (continueIndicator != null)
            continueIndicator.SetActive(visible);
    }
}
