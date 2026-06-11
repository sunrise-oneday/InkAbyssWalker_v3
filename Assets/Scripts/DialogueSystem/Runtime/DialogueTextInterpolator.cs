using System.Text;
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// 对话文本变量插值器
    /// 支持 {var:variableName} 语法在文本中插入变量值
    /// </summary>
    public class DialogueTextInterpolator
    {
        private readonly StringBuilder sb = new StringBuilder();

        /// <summary>
        /// 插值处理文本
        /// </summary>
        public string Interpolate(string text, DialogueVariableState vars)
        {
            if (string.IsNullOrEmpty(text) || vars == null)
                return text;

            sb.Clear();
            int lastIndex = 0;

            while (lastIndex < text.Length)
            {
                // 查找 {var: 开头
                int varStart = text.IndexOf("{var:", lastIndex);
                if (varStart < 0)
                {
                    // 没有更多变量，追加剩余文本
                    sb.Append(text, lastIndex, text.Length - lastIndex);
                    break;
                }

                // 追加变量前的文本
                if (varStart > lastIndex)
                {
                    sb.Append(text, lastIndex, varStart - lastIndex);
                }

                // 查找结束的 }
                int varEnd = text.IndexOf('}', varStart);
                if (varEnd < 0)
                {
                    // 没有找到结束符，当作普通文本
                    sb.Append(text, varStart, text.Length - varStart);
                    break;
                }

                // 提取变量名
                string varContent = text.Substring(varStart + 5, varEnd - varStart - 5);
                string varName = varContent;
                string format = null;

                // 检查是否有格式说明符（用:分隔）
                int formatIndex = varContent.IndexOf(':');
                if (formatIndex >= 0)
                {
                    varName = varContent.Substring(0, formatIndex);
                    format = varContent.Substring(formatIndex + 1);
                }

                // 获取变量值
                string value = GetVariableValue(vars, varName, format);
                sb.Append(value);

                lastIndex = varEnd + 1;
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取变量值并应用格式
        /// </summary>
        private string GetVariableValue(DialogueVariableState vars, string varName, string format)
        {
            if (!vars.GetRawValue(varName, out string rawValue, out string type))
            {
                Debug.LogWarning($"[DialogueTextInterpolator] 变量 '{varName}' 不存在");
                return $"[{varName}]";
            }

            // 根据类型和格式返回值
            switch (type)
            {
                case "bool":
                    bool boolVal = rawValue == "true" || rawValue == "1";
                    return boolVal.ToString();

                case "int":
                    if (int.TryParse(rawValue, out int intVal))
                    {
                        // 应用格式
                        if (!string.IsNullOrEmpty(format))
                        {
                            try
                            {
                                return intVal.ToString(format);
                            }
                            catch (System.FormatException)
                            {
                                Debug.LogWarning($"[DialogueTextInterpolator] 无效的格式: {format}");
                                return intVal.ToString();
                            }
                        }
                        return intVal.ToString();
                    }
                    return rawValue;

                case "string":
                    return rawValue;

                default:
                    return rawValue;
            }
        }

        /// <summary>
        /// 检查文本是否包含变量插值
        /// </summary>
        public static bool HasInterpolation(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.Contains("{var:");
        }
    }
}
