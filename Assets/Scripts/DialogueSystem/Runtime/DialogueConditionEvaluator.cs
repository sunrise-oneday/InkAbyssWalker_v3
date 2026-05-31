using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// 对话条件求值器
    /// 支持变量比较和外部查询（物品、标记）
    /// </summary>
    public class DialogueConditionEvaluator
    {
        private readonly IGameConditionProvider conditionProvider;

        public DialogueConditionEvaluator(IGameConditionProvider conditionProvider)
        {
            this.conditionProvider = conditionProvider;
        }

        /// <summary>
        /// 求值单个条件
        /// </summary>
        public bool Evaluate(DialogueCondition condition, DialogueVariableState vars)
        {
            if (condition == null) return true;

            // 对于HasItem和QuestFlag，使用外部查询接口
            if (condition.op == ConditionOperator.HasItem)
            {
                return conditionProvider != null && conditionProvider.PlayerHasItem(condition.value);
            }

            if (condition.op == ConditionOperator.QuestFlag)
            {
                return conditionProvider != null && conditionProvider.HasQuestFlag(condition.variable);
            }

            // 对于变量比较，从变量状态获取值
            if (!vars.GetRawValue(condition.variable, out string rawValue, out string type))
            {
                Debug.LogWarning($"[DialogueConditionEvaluator] 变量 '{condition.variable}' 不存在");
                return false;
            }

            // 根据类型进行比较
            return EvaluateComparison(rawValue, type, condition.op, condition.value);
        }

        /// <summary>
        /// 求值条件节点的所有条件（按顺序检查，第一个满足的生效）
        /// </summary>
        public bool EvaluateConditions(DialogueCondition[] conditions, DialogueVariableState vars, out string nextNodeId, string defaultNext)
        {
            if (conditions == null || conditions.Length == 0)
            {
                nextNodeId = defaultNext;
                return true;
            }

            foreach (var condition in conditions)
            {
                if (Evaluate(condition, vars))
                {
                    nextNodeId = condition.nextIfTrue;
                    return true;
                }
                else
                {
                    nextNodeId = condition.nextIfFalse;
                    // 如果有nextIfFalse，说明这个条件分支有明确的false路径
                    if (!string.IsNullOrEmpty(condition.nextIfFalse))
                        return false;
                }
            }

            // 所有条件都不满足，使用默认跳转
            nextNodeId = defaultNext;
            return false;
        }

        /// <summary>
        /// 执行比较操作
        /// </summary>
        private bool EvaluateComparison(string leftRaw, string type, ConditionOperator op, string rightRaw)
        {
            switch (type)
            {
                case "bool":
                    return EvaluateBoolComparison(leftRaw, op, rightRaw);
                case "int":
                    return EvaluateIntComparison(leftRaw, op, rightRaw);
                case "string":
                    return EvaluateStringComparison(leftRaw, op, rightRaw);
                default:
                    Debug.LogWarning($"[DialogueConditionEvaluator] 未知的变量类型: {type}");
                    return false;
            }
        }

        private bool EvaluateBoolComparison(string leftRaw, ConditionOperator op, string rightRaw)
        {
            bool left = leftRaw == "true" || leftRaw == "1";
            bool right = rightRaw == "true" || rightRaw == "1";

            switch (op)
            {
                case ConditionOperator.Equals: return left == right;
                case ConditionOperator.NotEquals: return left != right;
                default:
                    Debug.LogWarning($"[DialogueConditionEvaluator] Bool类型不支持操作符: {op}");
                    return false;
            }
        }

        private bool EvaluateIntComparison(string leftRaw, ConditionOperator op, string rightRaw)
        {
            if (!int.TryParse(leftRaw, out int left))
            {
                Debug.LogWarning($"[DialogueConditionEvaluator] 无法解析整数: {leftRaw}");
                return false;
            }

            if (!int.TryParse(rightRaw, out int right))
            {
                Debug.LogWarning($"[DialogueConditionEvaluator] 无法解析整数: {rightRaw}");
                return false;
            }

            switch (op)
            {
                case ConditionOperator.Equals: return left == right;
                case ConditionOperator.NotEquals: return left != right;
                case ConditionOperator.GreaterThan: return left > right;
                case ConditionOperator.LessThan: return left < right;
                case ConditionOperator.GreaterThanOrEquals: return left >= right;
                case ConditionOperator.LessThanOrEquals: return left <= right;
                default:
                    Debug.LogWarning($"[DialogueConditionEvaluator] Int类型不支持操作符: {op}");
                    return false;
            }
        }

        private bool EvaluateStringComparison(string leftRaw, ConditionOperator op, string rightRaw)
        {
            switch (op)
            {
                case ConditionOperator.Equals: return leftRaw == rightRaw;
                case ConditionOperator.NotEquals: return leftRaw != rightRaw;
                default:
                    Debug.LogWarning($"[DialogueConditionEvaluator] String类型不支持操作符: {op}");
                    return false;
            }
        }
    }
}
