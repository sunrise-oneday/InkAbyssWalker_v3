using System;
using UnityEngine;
using UnityEngine.InputSystem;

// ??????????? IGamePlayerActions ???
public class GameplayInputReader : MonoBehaviour, InputAssets.IGamePlayerActions
{
    // ????????????????????????????????
    public event Action<Vector2> OnMoveInput = delegate { };
    public event Action<Vector2> OnLookInput = delegate { };
    public event Action OnFirePressed = delegate { };
    public event Action OnFireReleased = delegate { };
    public event Action OnDashPressed = delegate { };

    // ????????????
    public event Action OnJumpPressed = delegate { };

    private void Start()
    {
        if (InputManager.Instance != null && InputManager.Instance.Controls != null)
        {
            // ?????????? GamePlayer ????????????????
            InputManager.Instance.Controls.GamePlayer.SetCallbacks(this);
        }
    }

    // ========================================================
    // ???¡¤???????? IGamePlayerActions ?????????????
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

    // 2. ??????????????
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnJumpPressed.Invoke(); // ?????????
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
       if(context.phase == InputActionPhase.Performed)
        {
            OnDashPressed.Invoke();
        }

    }

    // ???????? StoreInventoryInputBridge ??????????????????????? IGamePlayerActions
    public void OnOpenInventory(InputAction.CallbackContext context) { }

    public void OnOpenShop(InputAction.CallbackContext context) { }
}