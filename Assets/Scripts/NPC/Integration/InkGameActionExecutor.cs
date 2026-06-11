using UnityEngine;
using DialogueSystem;
using StoreAndInventory;

/// <summary>
/// 游戏动作执行桥：将 Inventory/WalletService 适配到 IGameActionExecutor
/// </summary>
public class InkGameActionExecutor : MonoBehaviour, IGameActionExecutor
{
    [Header("依赖")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private ItemDatabase database;
    [SerializeField] private AudioManager audioManager;

    public bool AddItem(string itemId, int count)
    {
        if (inventory == null || database == null)
        {
            Debug.LogWarning("[InkGameActionExecutor] Inventory或Database未设置");
            return false;
        }

        if (!database.TryGet(itemId, out var def))
        {
            Debug.LogWarning($"[InkGameActionExecutor] 物品 '{itemId}' 不存在于数据库中");
            return false;
        }

        return inventory.TryAdd(def, count, out _);
    }

    public bool RemoveItem(string itemId, int count)
    {
        if (inventory == null)
        {
            Debug.LogWarning("[InkGameActionExecutor] Inventory未设置");
            return false;
        }

        return inventory.TryConsume(itemId, count, out _);
    }

    public void SetQuestFlag(string flagKey, bool value)
    {
        PlayerPrefs.SetInt($"QuestFlag_{flagKey}", value ? 1 : 0);
    }

    public void PlaySFX(string sfxKey)
    {
        if (audioManager == null)
        {
            Debug.LogWarning("[InkGameActionExecutor] AudioManager未设置");
            return;
        }

        // TODO: 根据sfxKey加载并播放音效
        // audioManager.PlaySFX(clip);
        Debug.Log($"[InkGameActionExecutor] 播放音效: {sfxKey}");
    }

    public void TriggerCustomEvent(string eventName)
    {
        Debug.Log($"[InkGameActionExecutor] 触发自定义事件: {eventName}");
        // 可扩展：通过事件系统广播
    }
}
