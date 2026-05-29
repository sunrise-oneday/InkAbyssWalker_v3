using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 专门处理战斗中键盘输入与改键的输入通道组件
/// </summary>
public class BattleInputReader : MonoBehaviour, InputAssets.IBattleActions
{
    public static BattleInputReader Instance { get; private set; }

    // 战斗特有输入事件（支持动态改键！）
    public event Action OnParryPressed = delegate { };
    public event Action OnDodgePressed = delegate { };
    public event Action OnAimPressed = delegate { };
    public event Action OnShootPressed = delegate { };
    public event Action<int> OnQuickFormPressed = delegate { }; // 传入要切换的形态索引

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (InputManager.Instance != null && InputManager.Instance.Controls != null)
        {
            // 绑定至底层自动生成的 Battle 动作表回调
            InputManager.Instance.Controls.Battle.SetCallbacks(this);
        }
    }

    // ==========================================
    // 实现 IBattleActions 接口自动生成的方法
    // ==========================================

    public void OnParry(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnParryPressed.Invoke(); // 广播招架按下
        }
    }

    public void OnQuickForm1(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnQuickFormPressed.Invoke(0); // 快捷切换形态 1
        }
    }

    public void OnQuickForm2(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnQuickFormPressed.Invoke(1); // 快捷切换形态 2
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