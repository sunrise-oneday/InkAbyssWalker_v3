using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buff工厂类
/// 使用反射自动注册所有Buff类型，通过类名动态创建Buff实例
/// </summary>
public static class BuffFactory
{
    // Buff类型注册表：类名 -> 类型
    private static Dictionary<string, Type> buffTypes = new Dictionary<string, Type>();

    // 是否已初始化
    private static bool isInitialized = false;

    /// <summary>
    /// 初始化工厂，自动注册所有Buff类型
    /// </summary>
    private static void Initialize()
    {
        if (isInitialized) return;

        var buffType = typeof(Buff);
        var assembly = buffType.Assembly;

        // 扫描程序集中所有继承自Buff的非抽象类
        foreach (var type in assembly.GetTypes())
        {
            if (buffType.IsAssignableFrom(type) && !type.IsAbstract)
            {
                buffTypes[type.Name] = type;
            }
        }

        isInitialized = true;
    }

    /// <summary>
    /// 创建Buff实例
    /// </summary>
    /// <param name="typeName">Buff类名（如"ArmorBreakBuff"）</param>
    /// <param name="args">构造函数参数</param>
    /// <returns>Buff实例，如果类型不存在则返回null</returns>
    public static Buff Create(string typeName, params object[] args)
    {
        Initialize();

        if (string.IsNullOrEmpty(typeName))
        {
            return null;
        }

        if (buffTypes.TryGetValue(typeName, out var type))
        {
            try
            {
                return (Buff)Activator.CreateInstance(type, args);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BuffFactory] 创建Buff实例失败: {typeName}, 错误: {e.Message}");
                return null;
            }
        }

        Debug.LogWarning($"[BuffFactory] 未找到Buff类型: {typeName}");
        return null;
    }

    /// <summary>
    /// 检查Buff类型是否存在
    /// </summary>
    public static bool HasType(string typeName)
    {
        Initialize();
        return buffTypes.ContainsKey(typeName);
    }

    /// <summary>
    /// 获取所有已注册的Buff类型名称
    /// </summary>
    public static string[] GetRegisteredTypes()
    {
        Initialize();
        var types = new string[buffTypes.Count];
        buffTypes.Keys.CopyTo(types, 0);
        return types;
    }
}
