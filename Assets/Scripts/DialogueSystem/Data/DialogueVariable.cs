using System;

namespace DialogueSystem
{
    /// <summary>
    /// 对话变量定义
    /// 用于在对话中存储和修改状态
    /// </summary>
    [Serializable]
    public class DialogueVariableDef
    {
        /// <summary>变量名</summary>
        public string name;

        /// <summary>变量类型</summary>
        public VariableType type;

        /// <summary>默认值（字符串格式，运行时根据类型转换）</summary>
        public string defaultValue;
    }

    /// <summary>
    /// 选项定义（用于Choice节点）
    /// </summary>
    [Serializable]
    public class DialogueChoiceOption
    {
        /// <summary>选项显示文本</summary>
        public string text;

        /// <summary>选中后跳转的节点ID</summary>
        public string next;

        /// <summary>选项显示条件（可选，null表示始终显示）</summary>
        public DialogueCondition condition;

        /// <summary>选中时执行的动作（可选）</summary>
        public DialogueAction[] actions;
    }
}
