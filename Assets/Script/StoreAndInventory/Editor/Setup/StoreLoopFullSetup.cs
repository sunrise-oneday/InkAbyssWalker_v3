#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace StoreAndInventory.Editor
{
    public static class StoreLoopFullSetup
    {
        const string MenuPath = "MoYuan/Setup Full Store Loop (A+B+C)";

        [MenuItem(MenuPath)]
        public static void SetupFullScene()
        {
            SceneCleanupUtility.CleanupActiveScene();
            ShopLoopSceneSetup.SetupScene();
            InventoryLoopSceneSetup.SetupScene();
            Debug.Log("[StoreLoopFullSetup] Shop + Inventory + Save/Query wired. Legacy scene objects cleaned. Play: B=Shop, I=Inventory, ExtQuery, SaveTest.");
        }
    }
}
#endif
