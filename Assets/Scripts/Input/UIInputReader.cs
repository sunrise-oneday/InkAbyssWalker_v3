using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// UI 输入读取器 - 纯 C# 类，由 InputManager 统一管理
/// </summary>
public class UIInputReader : InputAssets.IUIActions
{
    // UI 事件广播
    public event Action<Vector2> OnNavigateInput = delegate { };
    public event Action OnSubmitPressed = delegate { };
    public event Action OnCancelPressed = delegate { };
    public event Action<Vector2> OnPointInput = delegate { };
    public event Action OnClickPressed = delegate { };
    public event Action OnClickReleased = delegate { };
    public event Action<Vector2> OnScrollInput = delegate { };
    public event Action OnMiddleClickPressed = delegate { };
    public event Action OnRightClickPressed = delegate { };
    public event Action OnNextTabPressed = delegate { };
    public event Action OnPreviousTabPressed = delegate { };

    // ==========================================
    // IUIActions 接口实现
    // ==========================================

    public void OnNavigate(InputAction.CallbackContext context)
    {
        OnNavigateInput.Invoke(context.ReadValue<Vector2>());
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnSubmitPressed.Invoke();
        }
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnCancelPressed.Invoke();
        }
    }

    public void OnPoint(InputAction.CallbackContext context)
    {
        OnPointInput.Invoke(context.ReadValue<Vector2>());
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnClickPressed.Invoke();
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            OnClickReleased.Invoke();
        }
    }

    public void OnScroll(InputAction.CallbackContext context)
    {
        OnScrollInput.Invoke(context.ReadValue<Vector2>());
    }

    public void OnMiddleClick(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnMiddleClickPressed.Invoke();
        }
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnRightClickPressed.Invoke();
        }
    }

    public void OnTrackedDevicePosition(InputAction.CallbackContext context) { }
    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context) { }
    public void OnScrollWheel(InputAction.CallbackContext context) { }

    public void OnNextTab(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnNextTabPressed.Invoke();
        }
    }

    public void OnPreviousTab(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnPreviousTabPressed.Invoke();
        }
    }
}
