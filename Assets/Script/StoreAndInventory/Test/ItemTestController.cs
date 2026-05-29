using UnityEngine;
using UnityEngine.UI;

namespace StoreAndInventory.Test
{
    public class ItemTestController : MonoBehaviour
    {
        static ItemTestController inputInstance;

        [Header("Panel Controller")]
        [SerializeField] StoreInventoryPanelController panelController;
        [SerializeField] KeyCode toggleShopKey = KeyCode.B;
        [SerializeField] KeyCode toggleInventoryKey = KeyCode.I;

        [Header("Services")]
        [SerializeField] Inventory inventory;
        [SerializeField] EquipmentService equipmentService;
        [SerializeField] StoreSaveService storeSaveService;

        [Header("Debug (optional)")]
        [SerializeField] WalletService wallet;
        [SerializeField] TestCharacter testCharacter;
        [SerializeField] Button walletQueryButton;
        [SerializeField] Button characterQueryButton;
        [SerializeField] Button externalQueryButton;
        [SerializeField] Button saveRoundTripButton;

        [SerializeField] bool handlePanelInput = true;

        void Awake()
        {
            ResolveInputInstance();

            if (panelController == null)
                panelController = FindObjectOfType<StoreInventoryPanelController>();

            if (inventory == null) inventory = FindObjectOfType<Inventory>();
            if (equipmentService == null) equipmentService = FindObjectOfType<EquipmentService>();
            if (storeSaveService == null) storeSaveService = FindObjectOfType<StoreSaveService>();
        }

        void OnDestroy()
        {
            if (inputInstance == this)
                inputInstance = null;
        }

        void ResolveInputInstance()
        {
            if (inputInstance != null && inputInstance != this)
            {
                if (IsPreferredOver(inputInstance))
                {
                    inputInstance.handlePanelInput = false;
                    inputInstance = this;
                    handlePanelInput = true;
                }
                else
                {
                    handlePanelInput = false;
                }

                return;
            }

            inputInstance = this;
            handlePanelInput = true;
        }

        bool IsPreferredOver(ItemTestController other)
        {
            if (other == null) return true;
            if (panelController != null && other.panelController == null) return true;
            if (panelController != null && other.panelController != null
                && gameObject.name == "Test" && other.gameObject.name == "TestController")
                return true;

            return false;
        }

        void OnEnable()
        {
            if (walletQueryButton != null)
                walletQueryButton.onClick.AddListener(OnClickWalletQuery);
            if (characterQueryButton != null)
                characterQueryButton.onClick.AddListener(OnClickCharacterQuery);
            if (externalQueryButton != null)
                externalQueryButton.onClick.AddListener(OnClickExternalQuery);
            if (saveRoundTripButton != null)
                saveRoundTripButton.onClick.AddListener(OnClickSaveRoundTrip);
        }

        void OnDisable()
        {
            if (walletQueryButton != null)
                walletQueryButton.onClick.RemoveListener(OnClickWalletQuery);
            if (characterQueryButton != null)
                characterQueryButton.onClick.RemoveListener(OnClickCharacterQuery);
            if (externalQueryButton != null)
                externalQueryButton.onClick.RemoveListener(OnClickExternalQuery);
            if (saveRoundTripButton != null)
                saveRoundTripButton.onClick.RemoveListener(OnClickSaveRoundTrip);
        }

        void Update()
        {
            if (!handlePanelInput || panelController == null) return;

            if (Input.GetKeyDown(toggleShopKey))
            {
                var wasOpen = panelController.IsShopOpen;
                panelController.ToggleShop();
                Debug.Log($"[ItemTest] Toggle Shop -> {(wasOpen ? "Close" : "Open")}");
            }

            if (Input.GetKeyDown(toggleInventoryKey))
            {
                var wasOpen = panelController.IsInventoryOpen;
                panelController.ToggleInventory();
                Debug.Log($"[ItemTest] Toggle Inventory -> {(wasOpen ? "Close" : "Open")}");
            }
        }

        public void OnClickWalletQuery()
        {
            if (wallet == null)
            {
                Debug.LogWarning("[ItemTest] WalletQuery failed: wallet is null.");
                return;
            }

            Debug.Log($"[ItemTest] Wallet Ink={wallet.Get(CurrencyId.Ink)}");
        }

        public void OnClickCharacterQuery()
        {
            if (testCharacter == null)
            {
                Debug.LogWarning("[ItemTest] CharacterQuery failed: testCharacter is null.");
                return;
            }

            Debug.Log("[ItemTest] Query character stats:");
            testCharacter.LogAll();
        }

        public void OnClickExternalQuery()
        {
            if (storeSaveService != null)
            {
                storeSaveService.LogExternalQuerySnapshot();
                return;
            }

            if (equipmentService != null)
            {
                Debug.Log($"[ItemTest] GetAllStatMods count={equipmentService.GetAllStatMods().Count}");
                Debug.Log($"[ItemTest] GetAllSkillMods count={equipmentService.GetAllSkillMods().Count}");
                Debug.Log($"[ItemTest] GetAllExtraEffects count={equipmentService.GetAllExtraEffects().Count}");
            }

            if (inventory != null)
                Debug.Log($"[ItemTest] GetConsumables count={inventory.GetConsumables(null).Count}");
        }

        public void OnClickSaveRoundTrip()
        {
            if (storeSaveService == null)
            {
                Debug.LogWarning("[ItemTest] SaveRoundTrip failed: storeSaveService is null.");
                return;
            }

            var ok = storeSaveService.RoundTripSelfTest(out var error);
            Debug.Log(ok
                ? "[ItemTest] Save round-trip OK"
                : $"[ItemTest] Save round-trip FAILED: {error}");
        }
    }
}
