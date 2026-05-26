using System;
using UnityEngine;
using UnityEngine.InputSystem;

// 实现您自动生成的 InputAssets.IUIActions 接口
public class UIInputReader : MonoBehaviour, InputAssets.IUIActions
{
    // ========================================================
    // 1. 对外暴露的 UI 事件广播者（观察者模式）
    // ========================================================
    public event Action<Vector2> OnNavigateInput = delegate { };
    public event Action OnSubmitPressed = delegate { };
    public event Action OnCancelPressed = delegate { }; // 对应 ESC 键（Cancel 动作）
    public event Action<Vector2> OnPointInput = delegate { };
    public event Action OnClickPressed = delegate { };
    public event Action OnClickReleased = delegate { };
    public event Action<Vector2> OnScrollInput = delegate { };
    public event Action OnMiddleClickPressed = delegate { };
    public event Action OnRightClickPressed = delegate { };

    private void Start()
    {
        if (InputManager.Instance != null && InputManager.Instance.Controls != null)
        {
            // 将自身注册为 UI 动作表的回调监听者
            InputManager.Instance.Controls.UI.SetCallbacks(this);
        }
    }

    // ========================================================
    // 2. 以下方法是实现 IUIActions 接口自动生成的回调方法
    // ========================================================

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

    public void OnTrackedDevicePosition(InputAction.CallbackContext context)
    {
        // 如果您的项目需要支持 VR/XR 手柄坐标，可在此添加相关事件
    }

    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context)
    {
        // 如果您的项目需要支持 VR/XR 手柄方向，可在此添加相关事件
    }

    public void OnScrollWheel(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }
}