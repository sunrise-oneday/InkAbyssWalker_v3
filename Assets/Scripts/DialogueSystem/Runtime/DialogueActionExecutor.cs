using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// 对话动作执行器
    /// 内部动作（变量操作）直接处理；外部动作（物品/标记/音效）委托给接口
    /// </summary>
    public class DialogueActionExecutor
    {
        private readonly IGameActionExecutor gameExecutor;
        private readonly DialogueVariableState variableState;

        public DialogueActionExecutor(IGameActionExecutor gameExecutor, DialogueVariableState variableState)
        {
            this.gameExecutor = gameExecutor;
            this.variableState = variableState;
        }

        /// <summary>
        /// 执行单个动作
        /// </summary>
        public void Execute(DialogueAction action)
        {
            if (action == null) return;

            switch (action.type)
            {
                case ActionType.SetVariable:
                    ExecuteSetVariable(action);
                    break;

                case ActionType.IncrementVariable:
                    ExecuteIncrementVariable(action);
                    break;

                case ActionType.AddItem:
                    ExecuteAddItem(action);
                    break;

                case ActionType.RemoveItem:
                    ExecuteRemoveItem(action);
                    break;

                case ActionType.SetQuestFlag:
                    ExecuteSetQuestFlag(action);
                    break;

                case ActionType.PlaySFX:
                    ExecutePlaySFX(action);
                    break;

                case ActionType.TriggerEvent:
                    ExecuteTriggerEvent(action);
                    break;

                default:
                    Debug.LogWarning($"[DialogueActionExecutor] 未知的动作类型: {action.type}");
                    break;
            }
        }

        /// <summary>
        /// 执行动作数组
        /// </summary>
        public void ExecuteAll(DialogueAction[] actions)
        {
            if (actions == null) return;

            foreach (var action in actions)
            {
                Execute(action);
            }
        }

        private void ExecuteSetVariable(DialogueAction action)
        {
            if (string.IsNullOrEmpty(action.key))
            {
                Debug.LogWarning("[DialogueActionExecutor] SetVariable缺少key");
                return;
            }

            // 尝试解析为int
            if (int.TryParse(action.value, out int intValue))
            {
                variableState.SetInt(action.key, intValue);
            }
            // 尝试解析为bool
            else if (action.value == "true" || action.value == "false" || action.value == "1" || action.value == "0")
            {
                variableState.SetBool(action.key, action.value == "true" || action.value == "1");
            }
            // 否则作为string
            else
            {
                variableState.SetString(action.key, action.value);
            }
        }

        private void ExecuteIncrementVariable(DialogueAction action)
        {
            if (string.IsNullOrEmpty(action.key))
            {
                Debug.LogWarning("[DialogueActionExecutor] IncrementVariable缺少key");
                return;
            }

            variableState.IncrementInt(action.key, action.count);
        }

        private void ExecuteAddItem(DialogueAction action)
        {
            if (gameExecutor == null)
            {
                Debug.LogWarning("[DialogueActionExecutor] IGameActionExecutor未设置，无法添加物品");
                return;
            }

            if (string.IsNullOrEmpty(action.key))
            {
                Debug.LogWarning("[DialogueActionExecutor] AddItem缺少itemId");
                return;
            }

            int count = action.count > 0 ? action.count : 1;
            bool success = gameExecutor.AddItem(action.key, count);
            if (!success)
            {
                Debug.LogWarning($"[DialogueActionExecutor] 添加物品失败: {action.key} x{count}");
            }
        }

        private void ExecuteRemoveItem(DialogueAction action)
        {
            if (gameExecutor == null)
            {
                Debug.LogWarning("[DialogueActionExecutor] IGameActionExecutor未设置，无法移除物品");
                return;
            }

            if (string.IsNullOrEmpty(action.key))
            {
                Debug.LogWarning("[DialogueActionExecutor] RemoveItem缺少itemId");
                return;
            }

            int count = action.count > 0 ? action.count : 1;
            bool success = gameExecutor.RemoveItem(action.key, count);
            if (!success)
            {
                Debug.LogWarning($"[DialogueActionExecutor] 移除物品失败: {action.key} x{count}");
            }
        }

        private void ExecuteSetQuestFlag(DialogueAction action)
        {
            if (gameExecutor == null)
            {
                Debug.LogWarning("[DialogueActionExecutor] IGameActionExecutor未设置，无法设置剧情标记");
                return;
            }

            if (string.IsNullOrEmpty(action.key))
            {
                Debug.LogWarning("[DialogueActionExecutor] SetQuestFlag缺少flagKey");
                return;
            }

            bool value = action.value == "true" || action.value == "1";
            gameExecutor.SetQuestFlag(action.key, value);
        }

        private void ExecutePlaySFX(DialogueAction action)
        {
            if (gameExecutor == null)
            {
                Debug.LogWarning("[DialogueActionExecutor] IGameActionExecutor未设置，无法播放音效");
                return;
            }

            if (string.IsNullOrEmpty(action.key))
            {
                Debug.LogWarning("[DialogueActionExecutor] PlaySFX缺少sfxKey");
                return;
            }

            gameExecutor.PlaySFX(action.key);
        }

        private void ExecuteTriggerEvent(DialogueAction action)
        {
            if (gameExecutor == null)
            {
                Debug.LogWarning("[DialogueActionExecutor] IGameActionExecutor未设置，无法触发事件");
                return;
            }

            if (string.IsNullOrEmpty(action.key))
            {
                Debug.LogWarning("[DialogueActionExecutor] TriggerEvent缺少eventName");
                return;
            }

            gameExecutor.TriggerCustomEvent(action.key);
        }
    }
}
