#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using StoreAndInventory.Test;

namespace StoreAndInventory.Editor
{
    public static class PanelControllerSetup
    {
        public static StoreInventoryPanelController Ensure(
            Canvas canvas,
            ShopUI shopUI,
            ShopService shopService,
            InventoryUI inventoryUI,
            ItemTestController testController)
        {
            if (canvas == null) return null;

            RemoveQuickSwitchButton(canvas.transform);
            SceneCleanupUtility.TryRemoveDuplicatePanelControllers();
            SceneCleanupUtility.TryRemoveDuplicateItemTestControllers();

            var existing = Object.FindObjectOfType<StoreInventoryPanelController>();
            GameObject go;

            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject("StoreInventoryPanelController");
                go.transform.SetParent(canvas.transform, false);
                existing = go.AddComponent<StoreInventoryPanelController>();
            }

            var so = new SerializedObject(existing);
            so.FindProperty("shopUI").objectReferenceValue = shopUI;
            so.FindProperty("shopService").objectReferenceValue = shopService;
            so.FindProperty("inventoryUI").objectReferenceValue = inventoryUI;
            so.ApplyModifiedPropertiesWithoutUndo();

            WireAllItemTestControllers(existing);

            if (testController != null)
            {
                var testSo = new SerializedObject(testController);
                testSo.FindProperty("panelController").objectReferenceValue = existing;
                testSo.ApplyModifiedPropertiesWithoutUndo();
            }

            return existing;
        }

        static void WireAllItemTestControllers(StoreInventoryPanelController controller)
        {
            var all = Object.FindObjectsOfType<ItemTestController>(true);
            for (var i = 0; i < all.Length; i++)
            {
                var test = all[i];
                if (test == null) continue;

                var testSo = new SerializedObject(test);
                testSo.FindProperty("panelController").objectReferenceValue = controller;
                testSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void RemoveQuickSwitchButton(Transform canvas)
        {
            var existing = canvas.Find("QuickSwitchButton");
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);
        }
    }
}
#endif
