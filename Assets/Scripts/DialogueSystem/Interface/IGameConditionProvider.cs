namespace DialogueSystem
{
    /// <summary>
    /// 游戏状态查询接口
    /// 解耦：对话系统不依赖 Inventory/StoryItem/QuestFlag
    /// 每个游戏项目自行实现此接口，桥接自己的系统
    /// </summary>
    public interface IGameConditionProvider
    {
        /// <summary>玩家是否持有指定物品（按ID）</summary>
        /// <param name="itemId">物品定义ID</param>
        /// <param name="minCount">最少数量</param>
        bool PlayerHasItem(string itemId, int minCount = 1);

        /// <summary>查询剧情标记是否已设置</summary>
        /// <param name="flagKey">标记键</param>
        bool HasQuestFlag(string flagKey);

        /// <summary>查询自定义游戏状态（扩展用）</summary>
        /// <param name="stateKey">状态键</param>
        bool QueryCustomState(string stateKey);
    }
}
