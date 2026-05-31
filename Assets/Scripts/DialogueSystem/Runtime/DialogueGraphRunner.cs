using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// 对话图运行引擎
    /// 驱动对话流程：节点解析、分支选择、条件判定、动作执行
    /// 不依赖任何Unity特定类型（除Debug.Log），纯逻辑层
    /// </summary>
    public class DialogueGraphRunner
    {
        // ===== 状态 =====
        private DialogueGraph graph;
        private DialogueVariableState variableState;
        private DialogueNode currentNode;
        private bool isActive;

        // ===== 依赖注入 =====
        private readonly DialogueConditionEvaluator conditionEvaluator;
        private readonly DialogueActionExecutor actionExecutor;
        private readonly DialogueTextInterpolator textInterpolator;

        // ===== 可选依赖 =====
        private IDialogueUIProvider uiProvider;

        // ===== 事件 =====
        /// <summary>对话开始</summary>
        public event Action OnDialogueStarted;

        /// <summary>对话结束</summary>
        public event Action OnDialogueEnded;

        /// <summary>进入新节点（参数：节点, 插值后的文本）</summary>
        public event Action<DialogueNode, string> OnNodeEntered;

        /// <summary>显示选项（参数：选项列表）</summary>
        public event Action<DialogueChoiceOption[]> OnChoicePresented;

        /// <summary>玩家选择了选项（参数：选项索引）</summary>
        public event Action<int> OnChoiceSelected;

        // ===== 属性 =====
        /// <summary>是否正在对话中</summary>
        public bool IsActive => isActive;

        /// <summary>当前变量状态</summary>
        public DialogueVariableState Variables => variableState;

        /// <summary>当前节点</summary>
        public DialogueNode CurrentNode => currentNode;

        // ===== 构造函数 =====
        public DialogueGraphRunner(
            IGameConditionProvider conditionProvider,
            IGameActionExecutor actionExecutor)
        {
            this.conditionEvaluator = new DialogueConditionEvaluator(conditionProvider);
            this.actionExecutor = new DialogueActionExecutor(actionExecutor, null); // variableState稍后设置
            this.textInterpolator = new DialogueTextInterpolator();
        }

        /// <summary>
        /// 设置UI提供者（可选）
        /// </summary>
        public void SetUIProvider(IDialogueUIProvider uiProvider)
        {
            this.uiProvider = uiProvider;
        }

        // ===== 公共API =====

        /// <summary>
        /// 开始对话
        /// </summary>
        public void StartDialogue(DialogueGraph graph, string startNodeId = null)
        {
            if (graph == null)
            {
                Debug.LogError("[DialogueGraphRunner] 无法启动对话：graph为null");
                return;
            }

            if (isActive)
            {
                Debug.LogWarning("[DialogueGraphRunner] 已有对话进行中，先结束当前对话");
                EndDialogue();
            }

            this.graph = graph;
            this.variableState = new DialogueVariableState();
            this.variableState.Initialize(graph.variables);

            // 更新actionExecutor的variableState引用
            var newActionExecutor = new DialogueActionExecutor(
                actionExecutor.GetType().GetField("gameExecutor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(actionExecutor) as IGameActionExecutor,
                variableState
            );

            isActive = true;

            string nodeId = startNodeId ?? graph.startNodeId;
            EnterNode(nodeId);

            OnDialogueStarted?.Invoke();
        }

        /// <summary>
        /// 开始对话（使用已有的变量状态）
        /// </summary>
        public void StartDialogue(DialogueGraph graph, DialogueVariableState existingVars, string startNodeId = null)
        {
            if (graph == null)
            {
                Debug.LogError("[DialogueGraphRunner] 无法启动对话：graph为null");
                return;
            }

            if (isActive)
            {
                Debug.LogWarning("[DialogueGraphRunner] 已有对话进行中，先结束当前对话");
                EndDialogue();
            }

            this.graph = graph;
            this.variableState = existingVars ?? new DialogueVariableState();

            isActive = true;

            string nodeId = startNodeId ?? graph.startNodeId;
            EnterNode(nodeId);

            OnDialogueStarted?.Invoke();
        }

        /// <summary>
        /// 推进对话（对于Dialogue节点）
        /// </summary>
        public void AdvanceLine()
        {
            if (!isActive || currentNode == null) return;

            if (currentNode.type == NodeType.Dialogue)
            {
                // 执行onShowActions（如果还没执行）
                if (currentNode.onShowActions != null)
                {
                    actionExecutor.ExecuteAll(currentNode.onShowActions);
                }

                // 跳转到下一个节点
                if (!string.IsNullOrEmpty(currentNode.next))
                {
                    EnterNode(currentNode.next);
                }
                else
                {
                    EndDialogue();
                }
            }
            else if (currentNode.type == NodeType.Action)
            {
                // 执行动作
                actionExecutor.ExecuteAll(currentNode.actions);

                // 跳转到下一个节点
                if (!string.IsNullOrEmpty(currentNode.next))
                {
                    EnterNode(currentNode.next);
                }
                else
                {
                    EndDialogue();
                }
            }
        }

        /// <summary>
        /// 选择选项（对于Choice节点）
        /// </summary>
        public void SelectChoice(int optionIndex)
        {
            if (!isActive || currentNode == null) return;

            if (currentNode.type != NodeType.Choice)
            {
                Debug.LogWarning("[DialogueGraphRunner] 当前节点不是Choice节点");
                return;
            }

            if (currentNode.options == null || optionIndex < 0 || optionIndex >= currentNode.options.Length)
            {
                Debug.LogWarning($"[DialogueGraphRunner] 无效的选项索引: {optionIndex}");
                return;
            }

            var option = currentNode.options[optionIndex];

            // 检查选项条件
            if (option.condition != null && !conditionEvaluator.Evaluate(option.condition, variableState))
            {
                Debug.LogWarning($"[DialogueGraphRunner] 选项 {optionIndex} 条件不满足");
                return;
            }

            // 执行选项动作
            if (option.actions != null)
            {
                actionExecutor.ExecuteAll(option.actions);
            }

            OnChoiceSelected?.Invoke(optionIndex);

            // 跳转到目标节点
            if (!string.IsNullOrEmpty(option.next))
            {
                EnterNode(option.next);
            }
            else
            {
                EndDialogue();
            }
        }

        /// <summary>
        /// 强制结束对话
        /// </summary>
        public void EndDialogue()
        {
            if (!isActive) return;

            isActive = false;
            currentNode = null;

            OnDialogueEnded?.Invoke();
        }

        /// <summary>
        /// 加载对话历史状态
        /// </summary>
        public void LoadState(string variablesJson, string historyJson)
        {
            if (!string.IsNullOrEmpty(variablesJson))
            {
                variableState = DialogueVariableState.FromJson(variablesJson);
            }
        }

        /// <summary>
        /// 保存对话状态
        /// </summary>
        public (string variablesJson, string historyJson) SaveState()
        {
            return (variableState?.ToJson(), null);
        }

        // ===== 内部方法 =====

        private void EnterNode(string nodeId)
        {
            var node = graph.FindNode(nodeId);
            if (node == null)
            {
                Debug.LogError($"[DialogueGraphRunner] 节点 '{nodeId}' 不存在");
                EndDialogue();
                return;
            }

            currentNode = node;

            switch (node.type)
            {
                case NodeType.Dialogue:
                    HandleDialogueNode(node);
                    break;

                case NodeType.Choice:
                    HandleChoiceNode(node);
                    break;

                case NodeType.Condition:
                    HandleConditionNode(node);
                    break;

                case NodeType.Action:
                    HandleActionNode(node);
                    break;

                case NodeType.End:
                    HandleEndNode();
                    break;

                default:
                    Debug.LogWarning($"[DialogueGraphRunner] 未知的节点类型: {node.type}");
                    EndDialogue();
                    break;
            }
        }

        private void HandleDialogueNode(DialogueNode node)
        {
            // 插值处理文本
            string resolvedText = textInterpolator.Interpolate(node.text, variableState);

            // 显示对话
            if (uiProvider != null)
            {
                uiProvider.ShowDialogue();
                uiProvider.ShowLine(node.speaker, resolvedText, node.portrait, node.typewriterSpeed);
            }

            OnNodeEntered?.Invoke(node, resolvedText);
        }

        private void HandleChoiceNode(DialogueNode node)
        {
            if (node.options == null || node.options.Length == 0)
            {
                Debug.LogWarning($"[DialogueGraphRunner] Choice节点 '{node.nodeId}' 没有选项");
                EndDialogue();
                return;
            }

            // 过滤可用选项
            var availableOptions = new List<DialogueChoiceOption>();
            foreach (var option in node.options)
            {
                if (option.condition == null || conditionEvaluator.Evaluate(option.condition, variableState))
                {
                    availableOptions.Add(option);
                }
            }

            if (availableOptions.Count == 0)
            {
                Debug.LogWarning($"[DialogueGraphRunner] Choice节点 '{node.nodeId}' 没有可用选项");
                EndDialogue();
                return;
            }

            // 显示选项
            if (uiProvider != null)
            {
                string[] optionTexts = new string[availableOptions.Count];
                for (int i = 0; i < availableOptions.Count; i++)
                {
                    optionTexts[i] = textInterpolator.Interpolate(availableOptions[i].text, variableState);
                }

                uiProvider.ShowDialogue();
                uiProvider.ShowChoices(node.prompt, optionTexts);
            }

            OnChoicePresented?.Invoke(availableOptions.ToArray());
        }

        private void HandleConditionNode(DialogueNode node)
        {
            string nextNodeId;
            bool conditionMet = conditionEvaluator.EvaluateConditions(
                node.conditions, variableState, out nextNodeId, node.defaultNext);

            if (!string.IsNullOrEmpty(nextNodeId))
            {
                EnterNode(nextNodeId);
            }
            else
            {
                Debug.LogWarning($"[DialogueGraphRunner] Condition节点 '{node.nodeId}' 没有有效的跳转目标");
                EndDialogue();
            }
        }

        private void HandleActionNode(DialogueNode node)
        {
            // 执行动作
            actionExecutor.ExecuteAll(node.actions);

            // 跳转到下一个节点
            if (!string.IsNullOrEmpty(node.next))
            {
                EnterNode(node.next);
            }
            else
            {
                EndDialogue();
            }
        }

        private void HandleEndNode()
        {
            EndDialogue();
        }
    }
}
