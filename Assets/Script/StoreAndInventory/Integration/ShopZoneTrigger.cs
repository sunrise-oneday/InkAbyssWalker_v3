using UnityEngine;
using StoreAndInventory;

/// <summary>
/// 商店触发区：区内按 F（OpenShop）由 <see cref="StoreInventoryInputBridge"/> 调用 <see cref="TryOpenShop"/>。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShopZoneTrigger : MonoBehaviour
{
    [Header("商店数据")]
    [SerializeField] ShopTableSO shopTable;

    [Header("引用（可留空，会 Find）")]
    [SerializeField] StoreInventoryPanelController panel;
    [SerializeField] ShopService shopService;

    [Header("触发")]
    [SerializeField] string playerTag = "Player";
    [SerializeField] bool requirePlayerController = true;

    [Header("可选")]
    [SerializeField] GameObject promptRoot;
    [SerializeField] bool closeShopWhenLeavingZone;

    public static ShopZoneTrigger Current { get; private set; }

    public bool IsPlayerInside { get; private set; }

    public ShopTableSO ShopTable => shopTable;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[ShopZoneTrigger] {name} 的 Collider2D 未勾选 Is Trigger，触发器不会生效。");

        ResolveReferences();
        SetPromptVisible(false);
    }

    void OnDisable()
    {
        if (Current == this)
            Current = null;

        IsPlayerInside = false;
        SetPromptVisible(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
            return;

        IsPlayerInside = true;
        Current = this;
        SetPromptVisible(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
            return;

        IsPlayerInside = false;
        if (Current == this)
            Current = null;

        SetPromptVisible(false);

        if (closeShopWhenLeavingZone)
            TryCloseShopIfOpen();
    }

    public void TryOpenShop()
    {
        ResolveReferences();

        if (!IsPlayerInside || !CanUseExploreStore() || shopTable == null)
            return;

        if (panel == null)
        {
            Debug.LogWarning("[ShopZoneTrigger] 未找到 StoreInventoryPanelController，无法打开商店 UI。");
            return;
        }

        if (shopService == null)
        {
            Debug.LogWarning("[ShopZoneTrigger] 未找到 ShopService，无法开店。");
            return;
        }

        if (panel.IsShopOpen || panel.IsAnyOpen)
            return;

        shopService.Open(shopTable);

        var bridge = FindStoreInventoryInputBridge();
        if (bridge != null)
            bridge.OpenShopUIAndPauseExploreInput();
        else
            panel.OpenShop();
    }

    void TryCloseShopIfOpen()
    {
        ResolveReferences();
        if (panel == null || !panel.IsShopOpen)
            return;

        var bridge = FindStoreInventoryInputBridge();
        if (bridge != null)
            bridge.CloseShopAndRestoreExploreInput();
        else
            panel.CloseShop();
    }

    void ResolveReferences()
    {
        if (panel == null)
            panel = FindObjectOfType<StoreInventoryPanelController>();

        if (shopService == null)
            shopService = FindObjectOfType<ShopService>();
    }

    static StoreInventoryInputBridge FindStoreInventoryInputBridge()
    {
        if (InputManager.Instance == null)
            return null;

        return InputManager.Instance.GetComponent<StoreInventoryInputBridge>()
               ?? FindObjectOfType<StoreInventoryInputBridge>();
    }

    bool IsPlayerCollider(Collider2D other)
    {
        if (other == null)
            return false;

        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
            return false;

        if (!requirePlayerController)
            return true;

        return other.GetComponentInParent<PlayerController>() != null;
    }

    static bool CanUseExploreStore()
    {
        if (BattleManager.Instance == null)
            return true;

        return BattleManager.Instance.currentPhase == BattlePhase.None;
    }

    void SetPromptVisible(bool visible)
    {
        if (promptRoot != null)
            promptRoot.SetActive(visible);
    }
}
