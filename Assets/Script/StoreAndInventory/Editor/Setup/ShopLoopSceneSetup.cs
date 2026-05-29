#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StoreAndInventory.Test;

namespace StoreAndInventory.Editor
{
    public static class ShopLoopSceneSetup
    {
        const string MenuPath = "MoYuan/Setup Shop Loop (Mid Plan A)";

        [MenuItem(MenuPath)]
        public static void SetupScene()
        {
            var shopService = Object.FindObjectOfType<ShopService>();
            var wallet = Object.FindObjectOfType<WalletService>();
            var inventory = Object.FindObjectOfType<Inventory>();
            var shopUI = Object.FindObjectOfType<ShopUI>(true);
            var testController = Object.FindObjectOfType<ItemTestController>();

            if (shopService == null || wallet == null || shopUI == null)
            {
                EditorUtility.DisplayDialog("Shop Loop Setup",
                    "Scene must contain ShopService, WalletService, and ShopUI (ShopPanel).",
                    "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(shopUI.gameObject, "Setup Shop Loop UI");

            var canvas = shopUI.GetComponentInParent<Canvas>();
            var inkText = EnsureInkHeader(shopUI.transform);
            var itemInfoPanel = TooltipUiFactory.EnsureFixedInfoPanel(shopUI.transform, canvas);

            var shopUiSo = new SerializedObject(shopUI);
            shopUiSo.FindProperty("shopService").objectReferenceValue = shopService;
            shopUiSo.FindProperty("wallet").objectReferenceValue = wallet;
            shopUiSo.FindProperty("inkText").objectReferenceValue = inkText;
            shopUiSo.FindProperty("itemInfoPanel").objectReferenceValue = itemInfoPanel;

            var closeBtn = shopUI.transform.Find("CloseButton")?.GetComponent<Button>();
            if (closeBtn != null)
                shopUiSo.FindProperty("closeButton").objectReferenceValue = closeBtn;

            shopUiSo.ApplyModifiedPropertiesWithoutUndo();

            if (testController != null)
            {
                var testSo = new SerializedObject(testController);
                testSo.FindProperty("wallet").objectReferenceValue = wallet;
                testSo.FindProperty("inventory").objectReferenceValue = inventory;
                testSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var inventoryUI = Object.FindObjectOfType<InventoryUI>(true);
            PanelControllerSetup.Ensure(canvas, shopUI, shopService, inventoryUI, testController);

            EditorUtility.SetDirty(shopUI);
            if (testController != null) EditorUtility.SetDirty(testController);
            Debug.Log("[ShopLoopSetup] Shop loop UI wired. Run Play and press B to test.");
        }

        static TextMeshProUGUI EnsureInkHeader(Transform parent)
        {
            var existing = parent.Find("InkHeader")?.GetComponent<TextMeshProUGUI>();
            if (existing != null) return existing;

            var go = new GameObject("InkHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, "Create InkHeader");
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(12f, -12f);
            rect.sizeDelta = new Vector2(240f, 32f);

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = "Ink 0";
            text.fontSize = 22;
            text.color = Color.black;
            return text;
        }

    }
}
#endif
