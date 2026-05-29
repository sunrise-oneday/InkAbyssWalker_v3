#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using StoreAndInventory.Test;

namespace StoreAndInventory.Editor
{
    /// <summary>
    /// Removes legacy scene objects that are no longer referenced by runtime scripts.
    /// Safe to run multiple times (idempotent).
    /// </summary>
    public static class SceneCleanupUtility
    {
        const string MenuPath = "MoYuan/Cleanup Legacy Scene Objects";

        [MenuItem(MenuPath)]
        public static void CleanupActiveSceneFromMenu()
        {
            var count = CleanupActiveScene();
            Debug.Log($"[SceneCleanup] Removed {count} legacy object(s). Save the scene if changes look correct.");
        }

        public static int CleanupActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return 0;

            var removed = TryRemoveLegacyShop();
            removed += TryRemoveDuplicateCanvasPanels();
            removed += TryRemoveDuplicatePanelControllers();
            removed += TryRemoveDuplicateItemTestControllers();

            if (removed > 0)
                EditorSceneManager.MarkSceneDirty(scene);

            return removed;
        }

        static int TryRemoveLegacyShop()
        {
            var removed = 0;
            var legacyShops = Object.FindObjectsOfType<Transform>(true);

            for (var i = 0; i < legacyShops.Length; i++)
            {
                var t = legacyShops[i];
                if (t == null || t.name != "Shop")
                    continue;

                if (IsUnder(t, "ShopPanel"))
                    continue;

                if (!HasMissingScripts(t.gameObject) && t.GetComponent<ShopService>() != null)
                    continue;

                if (t.parent != null && t.parent.name == "SAI")
                {
                    Undo.DestroyObjectImmediate(t.gameObject);
                    removed++;
                }
            }

            return removed;
        }

        static int TryRemoveDuplicateCanvasPanels()
        {
            var ui = GameObject.Find("UI");
            if (ui == null)
                return 0;

            if (ui.GetComponent<Canvas>() == null)
                return 0;

            var removed = 0;
            removed += TryRemoveDirectChildNamed(ui.transform, "ItemTooltipPanel", "ShopPanel");
            removed += TryRemoveDirectChildNamed(ui.transform, "ShopContextMenuPanel", "ShopPanel");
            removed += TryRemoveDirectChildNamed(ui.transform, "QuickSwitchButton");
            return removed;
        }

        public static int TryRemoveDuplicatePanelControllers()
        {
            var all = Object.FindObjectsOfType<StoreInventoryPanelController>(true);
            if (all.Length <= 1) return 0;

            var keepIndex = 0;
            var bestScore = ScorePanelController(all[0]);
            for (var i = 1; i < all.Length; i++)
            {
                var score = ScorePanelController(all[i]);
                if (score <= bestScore) continue;
                bestScore = score;
                keepIndex = i;
            }

            var removed = 0;
            for (var i = 0; i < all.Length; i++)
            {
                if (i == keepIndex || all[i] == null) continue;
                Undo.DestroyObjectImmediate(all[i].gameObject);
                removed++;
            }

            return removed;
        }

        public static int TryRemoveDuplicateItemTestControllers()
        {
            var all = Object.FindObjectsOfType<ItemTestController>(true);
            if (all.Length <= 1) return 0;

            var keepIndex = 0;
            var bestScore = ScoreItemTestController(all[0]);
            for (var i = 1; i < all.Length; i++)
            {
                var score = ScoreItemTestController(all[i]);
                if (score <= bestScore) continue;
                bestScore = score;
                keepIndex = i;
            }

            var removed = 0;
            for (var i = 0; i < all.Length; i++)
            {
                if (i == keepIndex || all[i] == null) continue;
                Undo.DestroyObjectImmediate(all[i].gameObject);
                removed++;
            }

            return removed;
        }

        static int ScorePanelController(StoreInventoryPanelController controller)
        {
            if (controller == null) return -1;

            var score = 0;
            var so = new SerializedObject(controller);
            if (so.FindProperty("shopUI").objectReferenceValue != null) score += 4;
            if (so.FindProperty("inventoryUI").objectReferenceValue != null) score += 4;
            if (so.FindProperty("shopService").objectReferenceValue != null) score += 2;
            if (controller.gameObject.activeInHierarchy) score += 1;
            return score;
        }

        static int ScoreItemTestController(ItemTestController controller)
        {
            if (controller == null) return -1;

            var score = 0;
            var so = new SerializedObject(controller);
            if (so.FindProperty("panelController").objectReferenceValue != null) score += 10;
            if (so.FindProperty("storeSaveService").objectReferenceValue != null) score += 3;
            if (so.FindProperty("externalQueryButton").objectReferenceValue != null) score += 2;
            if (controller.gameObject.name == "Test") score += 2;
            if (controller.gameObject.name == "TestController") score -= 3;
            if (controller.gameObject.activeInHierarchy) score += 1;
            return score;
        }

        static int TryRemoveDirectChildNamed(Transform canvas, string childName, string keepUnderPanelName)
        {
            var child = canvas.Find(childName);
            if (child == null)
                return 0;

            if (IsUnder(child, keepUnderPanelName))
                return 0;

            Undo.DestroyObjectImmediate(child.gameObject);
            return 1;
        }

        static int TryRemoveDirectChildNamed(Transform canvas, string childName)
        {
            var child = canvas.Find(childName);
            if (child == null)
                return 0;

            Undo.DestroyObjectImmediate(child.gameObject);
            return 1;
        }

        static bool IsUnder(Transform t, string ancestorName)
        {
            var p = t.parent;
            while (p != null)
            {
                if (p.name == ancestorName)
                    return true;
                p = p.parent;
            }

            return false;
        }

        static bool HasMissingScripts(GameObject go)
        {
            return GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0;
        }
    }
}
#endif
