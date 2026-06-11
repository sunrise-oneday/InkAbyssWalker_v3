using System;

namespace DialogueSystem
{
    /// <summary>
    /// 对话动作定义
    /// 用于执行变量修改、物品操作、音效播放等
    /// </summary>
    [Serializable]
    public class DialogueAction
    {
        /// <summary>动作类型</summary>
        public ActionType type;

        /// <summary>字符串参数（变量名、物品ID、事件名等）</summary>
        public string key;

        /// <summary>字符串值参数（用于设置变量值）</summary>
        public string value;

        /// <summary>整数参数（物品数量、递增量等）</summary>
        public int count;
    }
}
