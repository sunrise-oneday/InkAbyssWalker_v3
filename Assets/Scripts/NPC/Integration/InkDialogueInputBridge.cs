using System;
using UnityEngine;
using DialogueSystem;

/// <summary>
/// 对话输入桥：将项目输入系统适配到 IDialogueInputProvider
/// 可移植时替换此文件即可
/// </summary>
public class InkDialogueInputBridge : MonoBehaviour, IDialogueInputProvider
{
#pragma warning disable CS0067 // 事件从未被使用
    public event Action OnConfirmPressed;
    public event Action<int> OnOptionSelected;
#pragma warning restore CS0067

    private GameplayInputReader gameplayInput;
    private bool inputActive;
    private bool isSubscribed;

    private void OnEnable()
    {
        TrySubscribeInput();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
    }

    private void Update()
    {
        if (!isSubscribed)
            TrySubscribeInput();
    }

    private void TrySubscribeInput()
    {
        if (isSubscribed) return;
        if (InputManager.Instance == null) return;

        gameplayInput = InputManager.Instance.Gameplay;
        if (gameplayInput == null) return;

        gameplayInput.OnInteractPressed += HandleInteractPressed;
        isSubscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (!isSubscribed) return;
        if (gameplayInput != null)
            gameplayInput.OnInteractPressed -= HandleInteractPressed;
        isSubscribed = false;
    }

    private void HandleInteractPressed()
    {
        if (!inputActive) return;
        OnConfirmPressed?.Invoke();
    }

    public void PauseGameInput()
    {
        InputContextSwitcher.PauseExploreInput();
    }

    public void RestoreGameInput()
    {
        InputContextSwitcher.RestoreExploreInput();
    }

    public void SetDialogueInputActive(bool active)
    {
        inputActive = active;
    }
}
