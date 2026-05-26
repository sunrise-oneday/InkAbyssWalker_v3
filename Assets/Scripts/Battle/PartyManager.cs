using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 队伍管理器（纯 C# 单例类，无 MonoBehaviour 额外开销，不挂载物体） [3]
/// </summary>
public class PartyManager
{
    private static PartyManager instance;

    // 强类型单例获取器
    public static PartyManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new PartyManager();
            }
            return instance;
        }
    }

    // 当前出战的队友预制体（在运行时由大地图收集品或剧情动态增删）
    public List<GameObject> activeCompanionPrefabs { get; private set; }

    // 构造函数私有化，防止外部 new 产生多例
    private PartyManager()
    {
        activeCompanionPrefabs = new List<GameObject>();
    }

    /// <summary>
    /// 招募新队友入队 (通常在关卡收集品触发时，由触发器传入队友战斗预制体)
    /// </summary>
    public void AddCompanion(GameObject companionPrefab)
    {
        if (companionPrefab == null) return;
        if (!activeCompanionPrefabs.Contains(companionPrefab))
        {
            activeCompanionPrefabs.Add(companionPrefab);
            Debug.Log($"[队伍系统] 纯 C# 管理器：新队友 {companionPrefab.name} 成功加入出战队伍！");
        }
    }

    /// <summary>
    /// 队友离队
    /// </summary>
    public void RemoveCompanion(GameObject companionPrefab)
    {
        if (companionPrefab == null) return;
        if (activeCompanionPrefabs.Contains(companionPrefab))
        {
            activeCompanionPrefabs.Remove(companionPrefab);
            Debug.Log($"[队伍系统] 纯 C# 管理器：队友 {companionPrefab.name} 已离开队伍。");
        }
    }
}