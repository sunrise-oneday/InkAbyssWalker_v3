namespace DialogueSystem
{
    /// <summary>
    /// 游戏动作执行接口
    /// 解耦：对话系统不直接操作 Inventory/Wallet/剧情系统
    /// 每个游戏项目自行实现此接口，桥接自己的系统
    /// </summary>
    public interface IGameActionExecutor
    {
        /// <summary>添加物品给玩家</summary>
        /// <param name="itemId">物品定义ID</param>
        /// <param name="count">数量</param>
        /// <returns>是否成功</returns>
        bool AddItem(string itemId, int count);

        /// <summary>移除玩家物品</summary>
        /// <param name="itemId">物品定义ID</param>
        /// <param name="count">数量</param>
        /// <returns>是否成功</returns>
        bool RemoveItem(string itemId, int count);

        /// <summary>设置剧情标记</summary>
        /// <param name="flagKey">标记键</param>
        /// <param name="value">标记值</param>
        void SetQuestFlag(string flagKey, bool value);

        /// <summary>播放音效</summary>
        /// <param name="sfxKey">音效资源键</param>
        void PlaySFX(string sfxKey);

        /// <summary>触发自定义事件（扩展用）</summary>
        /// <param name="eventName">事件名</param>
        void TriggerCustomEvent(string eventName);
    }
}
