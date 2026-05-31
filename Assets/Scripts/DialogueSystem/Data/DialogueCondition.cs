using System;

namespace DialogueSystem
{
    /// <summary>
    /// 对话条件定义
    /// 用于条件分支和选项显示条件
    /// </summary>
    [Serializable]
    public class DialogueCondition
    {
        /// <summary>变量名或查询键</summary>
        public string variable;

        /// <summary>条件操作符</summary>
        public ConditionOperator op;

        /// <summary>比较值（统一用字符串，运行时根据变量类型转换）</summary>
        public string value;

        /// <summary>条件为真时跳转的节点ID（仅用于Condition节点）</summary>
        public string nextIfTrue;

        /// <summary>条件为假时跳转的节点ID（仅用于Condition节点）</summary>
        public string nextIfFalse;
    }
}
