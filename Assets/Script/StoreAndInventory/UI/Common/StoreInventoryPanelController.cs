using UnityEngine;

namespace StoreAndInventory
{
    /// <summary>
    /// Coordinates shop and inventory panels (mutually exclusive when opening either).
    /// </summary>
    public class StoreInventoryPanelController : MonoBehaviour
    {
        [SerializeField] ShopUI shopUI;
        [SerializeField] ShopService shopService;
        [SerializeField] InventoryUI inventoryUI;

        void Awake()
        {
            if (shopUI == null) shopUI = FindObjectOfType<ShopUI>(true);
            if (shopService == null) shopService = FindObjectOfType<ShopService>();
            if (inventoryUI == null) inventoryUI = FindObjectOfType<InventoryUI>(true);
        }

        public bool IsShopOpen => shopUI != null && shopUI.IsOpen;
        public bool IsInventoryOpen => inventoryUI != null && inventoryUI.IsOpen;
        public bool IsAnyOpen => IsShopOpen || IsInventoryOpen;

        public void OpenShop()
        {
            inventoryUI?.Close();
            if (shopService != null && !shopService.IsOpen)
                shopService.Open();
            shopUI?.Open();
        }

        public void CloseShop()
        {
            CloseShopInternal();
        }

        public void ToggleShop()
        {
            if (IsShopOpen)
                CloseShop();
            else
                OpenShop();
        }

        public void OpenInventory()
        {
            CloseShopInternal();
            inventoryUI?.Open();
        }

        public void CloseInventory()
        {
            inventoryUI?.Close();
        }

        public void ToggleInventory()
        {
            if (IsInventoryOpen)
                CloseInventory();
            else
                OpenInventory();
        }

        public void CloseAll()
        {
            CloseShopInternal();
            inventoryUI?.Close();
        }

        void CloseShopInternal()
        {
            shopUI?.Close();
            shopService?.Close();
        }
    }
}
