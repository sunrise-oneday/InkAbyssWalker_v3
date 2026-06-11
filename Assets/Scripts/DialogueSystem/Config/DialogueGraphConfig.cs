using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// 对话图配置资产
    /// ScriptableObject包装器，用于在Inspector中引用JSON文件
    /// 支持两种加载方式：
    /// 1. 直接引用：Inspector中拖入TextAsset
    /// 2. Resources.Load：放在Resources/Dialogues/下
    /// </summary>
    [CreateAssetMenu(fileName = "dialogue_", menuName = "Dialogue System/Graph Asset")]
    public class DialogueGraphConfig : ScriptableObject
    {
        [Header("对话数据")]
        [Tooltip("对话JSON文件")]
        [SerializeField] private TextAsset jsonAsset;

        [Header("配置")]
        [Tooltip("起始节点ID（留空使用JSON中的startNodeId）")]
        [SerializeField] private string startNodeId;

        /// <summary>
        /// 获取起始节点ID
        /// </summary>
        public string StartNodeId => startNodeId;

        /// <summary>
        /// 加载对话图
        /// </summary>
        public DialogueGraph LoadGraph()
        {
            if (jsonAsset == null)
            {
                Debug.LogError("[DialogueGraphConfig] jsonAsset为null");
                return null;
            }

            return DialogueGraph.FromJson(jsonAsset.text);
        }

        /// <summary>
        /// 从Resources加载对话图
        /// </summary>
        public static DialogueGraph LoadFromResources(string path)
        {
            var textAsset = Resources.Load<TextAsset>(path);
            if (textAsset == null)
            {
                Debug.LogError($"[DialogueGraphConfig] 无法从Resources加载: {path}");
                return null;
            }

            return DialogueGraph.FromJson(textAsset.text);
        }
    }
}
