namespace DialogueSystem
{
    /// <summary>
    /// 对话节点类型
    /// </summary>
    public enum NodeType
    {
        Dialogue,    // 对话行（NPC或玩家台词）
        Choice,      // 选项分支（玩家选择）
        Condition,   // 条件分支（根据变量/状态跳转）
        Action,      // 动作执行（修改变量、添加物品等）
        End          // 对话结束
    }

    /// <summary>
    /// 条件操作符
    /// </summary>
    public enum ConditionOperator
    {
        Equals,              // 等于
        NotEquals,           // 不等于
        GreaterThan,         // 大于
        LessThan,            // 小于
        GreaterThanOrEquals, // 大于等于
        LessThanOrEquals,    // 小于等于
        HasItem,             // 持有物品（通过外部查询接口）
        QuestFlag            // 剧情标记已设置
    }

    /// <summary>
    /// 动作类型
    /// </summary>
    public enum ActionType
    {
        SetVariable,         // 设置变量值
        IncrementVariable,   // 递增变量值
        AddItem,             // 添加物品给玩家
        RemoveItem,          // 移除玩家物品
        SetQuestFlag,        // 设置剧情标记
        PlaySFX,             // 播放音效
        TriggerEvent         // 触发自定义事件
    }

    /// <summary>
    /// 变量类型
    /// </summary>
    public enum VariableType
    {
        Bool,    // 布尔值
        Int,     // 整数
        String   // 字符串
    }
}
