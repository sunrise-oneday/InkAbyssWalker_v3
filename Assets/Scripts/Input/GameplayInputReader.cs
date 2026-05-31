using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 游戏玩法输入读取器 - 纯 C# 类，由 InputManager 统一管理
/// </summary>
public class GameplayInputReader : InputAssets.IGamePlayerActions
{
    // ========================================================
    // 输入事件（通过 Action 委托分发给其他模块）
    // ========================================================
    public event Action<Vector2> OnMoveInput = delegate { };
    public event Action<Vector2> OnLookInput = delegate { };
    public event Action OnFirePressed = delegate { };
    public event Action OnFireReleased = delegate { };
    public event Action OnDashPressed = delegate { };
    public event Action OnJumpPressed = delegate { };
    public event Action OnOpenCharacterPanelPressed = delegate { };
    public event Action OnInteractPressed = delegate { };

    // ========================================================
    // IGamePlayerActions 接口实现
    // ========================================================

    public void OnMove(InputAction.CallbackContext context)
    {
        OnMoveInput.Invoke(context.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        OnLookInput.Invoke(context.ReadValue<Vector2>());
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnFirePressed.Invoke();
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            OnFireReleased.Invoke();
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnJumpPressed.Invoke();
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnDashPressed.Invoke();
        }
    }

    // 以下两个方法由外部接管，此处仅保留空实现以满足接口
    public void OnOpenInventory(InputAction.CallbackContext context) { }
    public void OnOpenShop(InputAction.CallbackContext context) { }

    public void OnOpenCharacterPanel(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnOpenCharacterPanelPressed.Invoke();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnInteractPressed.Invoke();
        }
    }
}
