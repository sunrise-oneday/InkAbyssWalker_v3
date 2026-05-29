using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace StoreAndInventory
{
    /// <summary>
    /// Conditional logging for store/inventory hot paths.
    /// Define DEBUG_STORE_INVENTORY in Player Settings to enable verbose logs.
    /// </summary>
    internal static class StoreInventoryLog
    {
        [Conditional("DEBUG_STORE_INVENTORY")]
        public static void Info(string message)
        {
            Debug.Log(message);
        }
    }
}
