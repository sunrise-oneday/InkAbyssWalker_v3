namespace DialogueSystem
{
    /// <summary>
    /// 对话UI提供者接口
    /// 解耦：引擎不关心具体UI实现（UGUI/TMP/UI Toolkit）
    /// 每个项目自行实现此接口，桥接自己的UI系统
    /// </summary>
    public interface IDialogueUIProvider
    {
        /// <summary>显示对话行</summary>
        /// <param name="speaker">说话人名称</param>
        /// <param name="text">对话文本</param>
        /// <param name="portraitKey">头像资源键（可选）</param>
        /// <param name="speed">打字机速度（秒/字符）</param>
        void ShowLine(string speaker, string text, string portraitKey, float speed);

        /// <summary>显示选项列表</summary>
        /// <param name="prompt">提示文本（可选）</param>
        /// <param name="optionTexts">选项文本数组</param>
        void ShowChoices(string prompt, string[] optionTexts);

        /// <summary>显示对话UI</summary>
        void ShowDialogue();

        /// <summary>隐藏对话UI</summary>
        void HideDialogue();

        /// <summary>打字机是否已完成</summary>
        bool IsTypewriterComplete { get; set; }

        /// <summary>强制显示完整文本（跳过打字机效果）</summary>
        void ShowFullText();
    }
}
