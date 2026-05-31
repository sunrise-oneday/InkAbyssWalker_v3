using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全局设置面板控制器（ESC 打开/关闭，Q/E 切换标签页）
/// </summary>
public class SettingsController : MonoBehaviour
{
    [Header("面板根节点")]
    [SerializeField] private GameObject settingsRoot;

    [Header("Tab 按钮（按顺序，数量与 panels 一致）")]
    [SerializeField] private List<Button> tabButtons;

    [Header("设置面板列表（按顺序）")]
    [SerializeField] private List<BasePanel> panels;

    [Header("UI 输入")]
    private UIInputReader uiInput;

    // ---- 运行时 ----
    private int currentIndex = 0;
    private bool isOpen = false;
    private float lastToggleTime;
    private const float ToggleCooldown = 0.2f;
    private bool inputSubscribed;

    private void Awake()
    {
        TrySubscribeInput();
    }

    private void Start()
    {
        if (settingsRoot != null)
            settingsRoot.SetActive(false);

        for (int i = 0; i < tabButtons.Count && i < panels.Count; i++)
        {
            int index = i;
            tabButtons[i]?.onClick.AddListener(() => SwitchToPanel(index));
        }
    }

    private void TrySubscribeInput()
    {
        if (inputSubscribed) return;

        if (uiInput == null)
            uiInput = InputManager.Instance?.UI;

        if (uiInput != null)
        {
            uiInput.OnCancelPressed += Toggle;
            uiInput.OnNextTabPressed += NextPanel;
            uiInput.OnPreviousTabPressed += PreviousPanel;
            inputSubscribed = true;
        }
    }

    private void OnDestroy()
    {
        if (uiInput != null)
        {
            uiInput.OnCancelPressed -= Toggle;
            uiInput.OnNextTabPressed -= NextPanel;
            uiInput.OnPreviousTabPressed -= PreviousPanel;
        }
    }

    private void Update()
    {
        if (!inputSubscribed)
            TrySubscribeInput();
    }

    // ============================================
    // 打开 / 关闭
    // ============================================

    public void Toggle()
    {
        if (Time.time - lastToggleTime < ToggleCooldown) return;
        lastToggleTime = Time.time;

        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (settingsRoot == null) return;
        isOpen = true;
        settingsRoot.SetActive(true);

        InputContextSwitcher.PauseExploreInput();

        currentIndex = 0;
        RefreshPanels();
    }

    public void Close()
    {
        if (settingsRoot == null) return;
        isOpen = false;
        panels[currentIndex]?.OnDeactivate();
        settingsRoot.SetActive(false);

        InputContextSwitcher.RestoreExploreInput();
    }

    // ============================================
    // 面板切换
    // ============================================

    public void SwitchToPanel(int index)
    {
        if (index < 0 || index >= panels.Count || index == currentIndex) return;

        panels[currentIndex]?.OnDeactivate();
        panels[currentIndex]?.gameObject.SetActive(false);

        currentIndex = index;
        panels[currentIndex]?.gameObject.SetActive(true);
        panels[currentIndex]?.OnActivate();
        UpdateTabHighlights();
    }

    public void NextPanel()
    {
        SwitchToPanel((currentIndex + 1) % panels.Count);
    }

    public void PreviousPanel()
    {
        SwitchToPanel((currentIndex - 1 + panels.Count) % panels.Count);
    }

    private void RefreshPanels()
    {
        foreach (var p in panels)
        {
            if (p != null) { p.gameObject.SetActive(false); p.OnClose(); }
        }

        currentIndex = 0;
        if (panels.Count > 0 && panels[0] != null)
        {
            panels[0].gameObject.SetActive(true);
            panels[0].OnOpen();
            panels[0].OnActivate();
        }
        UpdateTabHighlights();
    }

    private void UpdateTabHighlights()
    {
        for (int i = 0; i < tabButtons.Count; i++)
        {
            if (tabButtons[i] == null) continue;

            var hl = tabButtons[i].transform.Find("Highlight")?.gameObject;
            if (hl != null) hl.SetActive(i == currentIndex);

            tabButtons[i].interactable = (i != currentIndex);
        }
    }

    public bool IsOpen => isOpen;
}
