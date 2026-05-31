using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 输入上下文切换器（静态工具类）
/// 统一管理探索态/UI 态的 ActionMap 切换、光标控制、PlayerController 暂停
/// 消除 UIManager、StoreInventoryInputBridge、SettingsController 中的重复逻辑
/// </summary>
public static class InputContextSwitcher
{
    private static InputAssets controls;
    private static PlayerController cachedPlayerController;
    private static bool exploreInputPaused;
    private static bool cursorUnlocked;

    /// <summary>
    /// 检查当前是否处于战斗状态（不可切换 UI）
    /// </summary>
    public static bool CanUseExploreInput()
    {
        if (BattleManager.Instance != null &&
            BattleTurnManager.Instance.currentPhase != BattlePhase.None)
            return false;

        return true;
    }

    /// <summary>
    /// 暂停探索输入，切换到 UI 输入模式
    /// </summary>
    /// <param name="keepAliveActions">需要保持启用的 GamePlayer Action 名称（如 OpenInventory、OpenShop）</param>
    public static void PauseExploreInput(params string[] keepAliveActions)
    {
        if (controls == null && InputManager.Instance != null)
            controls = InputManager.Instance.Controls;

        if (controls == null) return;

        // 缓存并禁用 PlayerController
        if (cachedPlayerController == null)
            cachedPlayerController = Object.FindObjectOfType<PlayerController>();

        if (cachedPlayerController != null)
            cachedPlayerController.enabled = false;

        // 禁用 GamePlayer，只保留指定的 Action
        controls.GamePlayer.Disable();
        foreach (var actionName in keepAliveActions)
        {
            var action = controls.asset.FindAction(actionName, true);
            if (action != null)
                action.Enable();
        }

        UnlockCursor();
        exploreInputPaused = true;
    }

    /// <summary>
    /// 恢复探索输入，禁用 UI 输入
    /// </summary>
    public static void RestoreExploreInput()
    {
        if (!exploreInputPaused) return;

        if (controls != null)
        {
            if (CanUseExploreInput())
                controls.GamePlayer.Enable();
        }

        if (cachedPlayerController != null)
        {
            cachedPlayerController.enabled = true;
            cachedPlayerController = null;
        }

        RelockCursor();
        exploreInputPaused = false;
    }

    /// <summary>
    /// 解锁光标（UI 模式）
    /// </summary>
    public static void UnlockCursor()
    {
        if (cursorUnlocked) return;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        cursorUnlocked = true;
    }

    /// <summary>
    /// 锁定光标（探索模式）
    /// </summary>
    public static void RelockCursor()
    {
        if (!cursorUnlocked) return;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        cursorUnlocked = false;
    }

    /// <summary>
    /// 强制重置状态（用于场景切换等特殊情况）
    /// </summary>
    public static void Reset()
    {
        if (exploreInputPaused)
            RestoreExploreInput();

        controls = null;
        cachedPlayerController = null;
    }
}
