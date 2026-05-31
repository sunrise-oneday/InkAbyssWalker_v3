using UnityEngine;
using DialogueSystem;
using StoreAndInventory;

/// <summary>
/// 游戏条件查询桥：将 Inventory 服务适配到 IGameConditionProvider
/// </summary>
public class InkGameConditionProvider : MonoBehaviour, IGameConditionProvider
{
    [Header("依赖")]
    [SerializeField] private Inventory inventory;

    public bool PlayerHasItem(string itemId, int minCount = 1)
    {
        if (inventory == null)
        {
            Debug.LogWarning("[InkGameConditionProvider] Inventory未设置");
            return false;
        }

        int total = 0;
        foreach (var stack in inventory.Items)
        {
            if (stack.definitionId == itemId)
                total += stack.count;
        }
        return total >= minCount;
    }

    public bool HasQuestFlag(string flagKey)
    {
        return PlayerPrefs.GetInt($"QuestFlag_{flagKey}", 0) == 1;
    }

    public bool QueryCustomState(string stateKey)
    {
        // 可扩展：根据 stateKey 查询不同系统
        return false;
    }
}
