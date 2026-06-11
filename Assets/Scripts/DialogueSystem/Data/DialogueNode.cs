using System;

namespace DialogueSystem
{
    /// <summary>
    /// 对话节点定义
    /// 统一的节点结构，通过type字段区分不同类型
    /// JsonUtility不支持多态，所以使用单一类 + type字段分发
    /// </summary>
    [Serializable]
    public class DialogueNode
    {
        /// <summary>节点唯一ID</summary>
        public string nodeId;

        /// <summary>节点类型</summary>
        public NodeType type;

        // ===== Dialogue节点字段 =====
        /// <summary>说话人名称</summary>
        public string speaker;

        /// <summary>头像资源键（可选）</summary>
        public string portrait;

        /// <summary>对话文本（支持 {var:variableName} 插值）</summary>
        public string text;

        /// <summary>打字机速度（秒/字符，0使用全局默认值）</summary>
        public float typewriterSpeed;

        /// <summary>进入此节点时执行的动作（可选）</summary>
        public DialogueAction[] onShowActions;

        // ===== Choice节点字段 =====
        /// <summary>选项提示文本（可选）</summary>
        public string prompt;

        /// <summary>选项列表</summary>
        public DialogueChoiceOption[] options;

        // ===== Condition节点字段 =====
        /// <summary>条件列表（支持多条件，按顺序检查，第一个满足的生效）</summary>
        public DialogueCondition[] conditions;

        /// <summary>所有条件都不满足时的默认跳转节点</summary>
        public string defaultNext;

        // ===== Action节点字段 =====
        /// <summary>要执行的动作列表</summary>
        public DialogueAction[] actions;

        // ===== 通用跳转字段 =====
        /// <summary>下一个节点ID（用于Dialogue和Action节点）</summary>
        public string next;

        // ===== End节点无额外字段 =====
    }
}
