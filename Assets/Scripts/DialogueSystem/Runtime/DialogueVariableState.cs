using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// 对话变量运行时状态
    /// 支持bool、int、string三种类型的变量存储
    /// 支持JSON序列化用于存档
    /// </summary>
    [Serializable]
    public class DialogueVariableState
    {
        [SerializeField] private List<string> keys = new List<string>();
        [SerializeField] private List<string> values = new List<string>();
        [SerializeField] private List<string> types = new List<string>();

        // 运行时索引缓存
        private Dictionary<string, int> indexCache;

        /// <summary>
        /// 从对话图初始化变量默认值
        /// </summary>
        public void Initialize(DialogueVariableDef[] variableDefs)
        {
            keys.Clear();
            values.Clear();
            types.Clear();
            indexCache = null;

            if (variableDefs == null) return;

            foreach (var def in variableDefs)
            {
                SetRaw(def.name, def.defaultValue, def.type.ToString().ToLower());
            }
        }

        /// <summary>
        /// 获取bool变量值
        /// </summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
            string raw = GetRaw(key);
            if (raw == null) return defaultValue;
            return raw == "true" || raw == "1";
        }

        /// <summary>
        /// 设置bool变量值
        /// </summary>
        public void SetBool(string key, bool value)
        {
            SetRaw(key, value.ToString().ToLower(), "bool");
        }

        /// <summary>
        /// 获取int变量值
        /// </summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            string raw = GetRaw(key);
            if (raw == null) return defaultValue;
            if (int.TryParse(raw, out int result))
                return result;
            return defaultValue;
        }

        /// <summary>
        /// 设置int变量值
        /// </summary>
        public void SetInt(string key, int value)
        {
            SetRaw(key, value.ToString(), "int");
        }

        /// <summary>
        /// 递增int变量
        /// </summary>
        public void IncrementInt(string key, int amount = 1)
        {
            int current = GetInt(key);
            SetInt(key, current + amount);
        }

        /// <summary>
        /// 获取string变量值
        /// </summary>
        public string GetString(string key, string defaultValue = "")
        {
            string raw = GetRaw(key);
            if (raw == null) return defaultValue;
            return raw;
        }

        /// <summary>
        /// 设置string变量值
        /// </summary>
        public void SetString(string key, string value)
        {
            SetRaw(key, value, "string");
        }

        /// <summary>
        /// 检查变量是否存在
        /// </summary>
        public bool Has(string key)
        {
            return GetIndex(key) >= 0;
        }

        /// <summary>
        /// 获取变量的原始字符串值和类型
        /// </summary>
        public bool GetRawValue(string key, out string value, out string type)
        {
            int index = GetIndex(key);
            if (index < 0)
            {
                value = null;
                type = null;
                return false;
            }
            value = values[index];
            type = types[index];
            return true;
        }

        // ===== 序列化方法 =====

        /// <summary>
        /// 序列化为JSON
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// 从JSON反序列化
        /// </summary>
        public static DialogueVariableState FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new DialogueVariableState();

            try
            {
                return JsonUtility.FromJson<DialogueVariableState>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogueVariableState] JSON解析失败: {e.Message}");
                return new DialogueVariableState();
            }
        }

        // ===== 内部方法 =====

        private string GetRaw(string key)
        {
            int index = GetIndex(key);
            if (index < 0) return null;
            return values[index];
        }

        private void SetRaw(string key, string value, string type)
        {
            int index = GetIndex(key);
            if (index >= 0)
            {
                values[index] = value;
                types[index] = type;
            }
            else
            {
                keys.Add(key);
                values.Add(value);
                types.Add(type);
                indexCache = null; // 清除缓存
            }
        }

        private int GetIndex(string key)
        {
            if (indexCache == null)
            {
                BuildIndexCache();
            }

            if (indexCache.TryGetValue(key, out int index))
                return index;

            return -1;
        }

        private void BuildIndexCache()
        {
            indexCache = new Dictionary<string, int>(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                if (!indexCache.ContainsKey(keys[i]))
                {
                    indexCache[keys[i]] = i;
                }
            }
        }
    }
}
