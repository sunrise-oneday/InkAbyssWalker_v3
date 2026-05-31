using UnityEngine;
using UnityEngine.InputSystem;
using StoreAndInventory;

/// <summary>
/// 探索态开/关背包（I）、商店（F）；暂停/恢复 GamePlayer 与探索移动。
/// </summary>
[DefaultExecutionOrder(100)]
public class StoreInventoryInputBridge : MonoBehaviour
{
    [Header("引用（可留空，会 Find / 运行时生成）")]
    [SerializeField] StoreInventoryPanelController panel;
    [SerializeField] ShopService shopService;

    [Header("场景缺少物品系统时自动实例化")]
    [SerializeField] bool spawnIfMissing = true;
    [SerializeField] GameObject dataPrefab;
    [SerializeField] GameObject servicePrefab;
    [SerializeField] GameObject uiPrefab;

    InputAssets controls;
    InventoryUI boundInventoryUI;
    bool exploreInputPausedByBridge;
    bool inputBound;

    void Awake()
    {
        if (spawnIfMissing)
            EnsureStoreSystemsInScene();

        ResolvePanelReference();
    }

    void OnEnable() => TryBindInput();

    void OnDisable() => UnbindInput();

    void EnsureStoreSystemsInScene()
    {
        if (FindObjectOfType<ItemDatabase>() == null && dataPrefab != null)
            Instantiate(dataPrefab);

        if (FindObjectOfType<Inventory>() == null && servicePrefab != null)
            Instantiate(servicePrefab);

        if (FindObjectOfType<StoreInventoryPanelController>() == null && uiPrefab != null)
        {
            var instance = Instantiate(uiPrefab);
            instance.name = uiPrefab.name;

            var rootRect = instance.GetComponent<RectTransform>();
            if (rootRect != null && rootRect.localScale == Vector3.zero)
                rootRect.localScale = Vector3.one;
        }
    }

    void ResolvePanelReference()
    {
        if (panel != null)
            return;

        panel = FindObjectOfType<StoreInventoryPanelController>();

        if (shopService == null)
            shopService = FindObjectOfType<ShopService>();
    }

    void TryBindInput()
    {
        if (inputBound)
            return;

        if (InputManager.Instance == null || InputManager.Instance.Controls == null)
            return;

        controls = InputManager.Instance.Controls;
        controls.GamePlayer.OpenInventory.performed += OnOpenInventoryPerformed;
        controls.GamePlayer.OpenShop.performed += OnOpenShopPerformed;
        inputBound = true;
    }

    void UnbindInput()
    {
        if (!inputBound || controls == null)
            return;

        controls.GamePlayer.OpenInventory.performed -= OnOpenInventoryPerformed;
        controls.GamePlayer.OpenShop.performed -= OnOpenShopPerformed;
        inputBound = false;
    }

    void OnOpenShopPerformed(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed)
            return;

        if (!CanUseExploreStoreInput())
            return;

        ResolvePanelReference();

        if (panel != null && panel.IsShopOpen)
        {
            CloseShopAndRestoreExploreInput();
            return;
        }

        if (ShopZoneTrigger.Current == null)
            return;

        ShopZoneTrigger.Current.TryOpenShop();
    }

    void OnOpenInventoryPerformed(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed)
            return;

        TryToggleInventory();
    }

    public void TryToggleInventory()
    {
        ResolvePanelReference();

        if (panel == null)
        {
            Debug.LogWarning(
                "[StoreInventoryInputBridge] 无法开背包：缺少 StoreInventoryPanelController。请挂 UI Prefab 或启用 spawnIfMissing。");
            return;
        }

        if (!CanOpenInventory())
            return;

        if (panel.IsInventoryOpen)
            CloseInventoryAndRestoreExploreInput();
        else
            OpenInventoryAndPauseExploreInput();
    }

    public void OpenInventoryAndPauseExploreInput()
    {
        ResolvePanelReference();
        if (panel == null || !CanOpenInventory())
            return;

        panel.OpenInventory();
        BindInventoryUiCloseHook();
        PauseExploreInputForUI();
    }

    public void CloseInventoryAndRestoreExploreInput()
    {
        if (panel == null)
            return;

        UnbindInventoryUiCloseHook();
        panel.CloseInventory();
        RestoreExploreInputIfAllowed();
    }

    void BindInventoryUiCloseHook()
    {
        UnbindInventoryUiCloseHook();

        boundInventoryUI = FindObjectOfType<InventoryUI>(true);
        if (boundInventoryUI == null)
            return;

        boundInventoryUI.Closed += HandleInventoryUiClosed;
    }

    void UnbindInventoryUiCloseHook()
    {
        if (boundInventoryUI == null)
            return;

        boundInventoryUI.Closed -= HandleInventoryUiClosed;
        boundInventoryUI = null;
    }

    void HandleInventoryUiClosed()
    {
        UnbindInventoryUiCloseHook();
        RestoreExploreInputIfAllowed();
    }

    public void CloseShopAndRestoreExploreInput()
    {
        if (panel == null)
            return;

        panel.CloseShop();
        RestoreExploreInputIfAllowed();
    }

    public void CloseAllPanelsAndRestoreExploreInput()
    {
        if (panel == null)
            return;

        panel.CloseAll();
        RestoreExploreInputIfAllowed();
    }

    public void OpenShopUIAndPauseExploreInput()
    {
        if (panel == null || !CanUseExploreStoreInput())
            return;

        panel.OpenShop();
        PauseExploreInputForUI();
    }

    bool CanOpenInventory() => CanUseExploreStoreInput() && panel != null;

    bool CanUseExploreStoreInput()
    {
        return InputContextSwitcher.CanUseExploreInput();
    }

    void PauseExploreInputForUI()
    {
        InputContextSwitcher.PauseExploreInput("OpenInventory", "OpenShop");
        exploreInputPausedByBridge = true;
    }

    void RestoreExploreInputIfAllowed()
    {
        if (!exploreInputPausedByBridge)
            return;

        if (panel != null && panel.IsAnyOpen)
            return;

        InputContextSwitcher.RestoreExploreInput();
        exploreInputPausedByBridge = false;
    }

}
