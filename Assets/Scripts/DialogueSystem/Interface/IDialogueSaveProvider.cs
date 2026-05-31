namespace DialogueSystem
{
    /// <summary>
    /// 对话存档提供者接口
    /// 解耦：对话系统不依赖 StoreSaveService
    /// 每个项目自行实现此接口，桥接自己的存档系统
    /// </summary>
    public interface IDialogueSaveProvider
    {
        /// <summary>保存对话状态</summary>
        /// <param name="dialogueId">对话ID</param>
        /// <param name="variablesJson">变量状态JSON</param>
        /// <param name="historyJson">对话历史JSON</param>
        void SaveDialogueState(string dialogueId, string variablesJson, string historyJson);

        /// <summary>加载对话状态</summary>
        /// <param name="dialogueId">对话ID</param>
        /// <returns>(变量状态JSON, 对话历史JSON)，如果不存在则返回(null, null)</returns>
        (string variablesJson, string historyJson) LoadDialogueState(string dialogueId);
    }
}
