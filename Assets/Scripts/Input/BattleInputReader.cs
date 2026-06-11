using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 战斗输入读取器 - 纯 C# 类，由 InputManager 统一管理
/// </summary>
public class BattleInputReader : InputAssets.IBattleActions
{
    // 战斗输入事件
    public event Action OnParryPressed = delegate { };
    public event Action OnDodgePressed = delegate { };
    public event Action OnAimPressed = delegate { };
    public event Action OnShootPressed = delegate { };
    public event Action<int> OnQuickFormPressed = delegate { };

    // ==========================================
    // IBattleActions 接口实现
    // ==========================================

    public void OnParry(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnParryPressed.Invoke();
        }
    }

    public void OnQuickForm1(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnQuickFormPressed.Invoke(0);
        }
    }

    public void OnQuickForm2(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnQuickFormPressed.Invoke(1);
        }
    }

    public void OnQuickForm3(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnQuickFormPressed.Invoke(2);
        }
    }

    public void OnDodge(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnDodgePressed.Invoke();
        }
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnAimPressed.Invoke();
        }
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnShootPressed.Invoke();
        }
    }
}
