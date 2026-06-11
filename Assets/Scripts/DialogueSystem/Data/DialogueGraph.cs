using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// 对话图数据模型
    /// 顶层数据结构，包含所有节点和变量定义
    /// </summary>
    [Serializable]
    public class DialogueGraph
    {
        /// <summary>数据版本号</summary>
        public int version = 1;

        /// <summary>对话唯一ID</summary>
        public string dialogueId;

        /// <summary>变量定义列表</summary>
        public DialogueVariableDef[] variables;

        /// <summary>节点列表</summary>
        public DialogueNode[] nodes;

        /// <summary>起始节点ID（默认为"start"）</summary>
        public string startNodeId = "start";

        // ===== 运行时缓存 =====
        [NonSerialized] private Dictionary<string, DialogueNode> nodeCache;

        /// <summary>
        /// 根据节点ID查找节点
        /// </summary>
        public DialogueNode FindNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
                return null;

            // 构建缓存
            if (nodeCache == null || nodeCache.Count != nodes.Length)
            {
                BuildNodeCache();
            }

            if (nodeCache.TryGetValue(nodeId, out var node))
                return node;

            Debug.LogWarning($"[DialogueGraph] 节点 '{nodeId}' 不存在于对话 '{dialogueId}' 中");
            return null;
        }

        /// <summary>
        /// 获取起始节点
        /// </summary>
        public DialogueNode GetStartNode()
        {
            return FindNode(startNodeId);
        }

        /// <summary>
        /// 构建节点索引缓存
        /// </summary>
        private void BuildNodeCache()
        {
            nodeCache = new Dictionary<string, DialogueNode>(nodes.Length);
            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node.nodeId))
                {
                    if (!nodeCache.ContainsKey(node.nodeId))
                    {
                        nodeCache[node.nodeId] = node;
                    }
                    else
                    {
                        Debug.LogWarning($"[DialogueGraph] 重复的节点ID: '{node.nodeId}' 在对话 '{dialogueId}' 中");
                    }
                }
            }
        }

        /// <summary>
        /// 从JSON字符串加载对话图
        /// </summary>
        public static DialogueGraph FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<DialogueGraph>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogueGraph] JSON解析失败: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 转换为JSON字符串
        /// </summary>
        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }
    }
}
