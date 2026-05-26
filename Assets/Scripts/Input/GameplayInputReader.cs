using System;
using UnityEngine;
using UnityEngine.InputSystem;

// 实现自动生成的 IGamePlayerActions 接口
public class GameplayInputReader : MonoBehaviour, InputAssets.IGamePlayerActions
{
    // 向外广播强类型事件，供状态机或控制器订阅
    public event Action<Vector2> OnMoveInput = delegate { };
    public event Action<Vector2> OnLookInput = delegate { };
    public event Action OnFirePressed = delegate { };
    public event Action OnFireReleased = delegate { };
    public event Action OnDashPressed = delegate { };

    // 新增跳跃和事件
    public event Action OnJumpPressed = delegate { };

    private void Start()
    {
        if (InputManager.Instance != null && InputManager.Instance.Controls != null)
        {
            // 将自身注册为 GamePlayer 动作表的回调监听者
            InputManager.Instance.Controls.GamePlayer.SetCallbacks(this);
        }
    }

    // ========================================================
    // 以下方法是实现 IGamePlayerActions 自动生成的对应回调
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

    // 2. 接口自动生成的方法
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnJumpPressed.Invoke(); // 广播跳跃按下
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
       if(context.phase == InputActionPhase.Performed)
        {
            OnDashPressed.Invoke();
        }

    }
}