using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局存档管理器（纯 C# 单例类，无 MonoBehaviour 额外开销，不挂载物体）
/// </summary>
public class SaveManager
{
    private static SaveManager instance;
    public static SaveManager Instance
    {
        get
        {
            if (instance == null) instance = new SaveManager();
            return instance;
        }
    }

    public Vector3 LastCheckpointPosition { get; private set; }

    private const string CheckpointXKey = "CheckpointX";
    private const string CheckpointYKey = "CheckpointY";
    private const string CheckpointZKey = "CheckpointZ";
    private const string HasSavedKey = "HasSavedCheckpoint";
    private const string AbilityEnabledPrefix = "AbilityEnabled_";
    private const string EquippedUltimateKey = "EquippedUltimate";

    private SaveManager()
    {
        LoadCheckpoint();
    }

    /// <summary>
    /// 激活篝火/存档点时调用，自动写入本地硬盘
    /// </summary>
    public void SaveCheckpoint(Vector3 position)
    {
        if (position.x > 1500f && position.y > 1500f)
        {
            Debug.LogWarning($"[存档系统] 警告:拦截到错误的战斗场景坐标 {position} 写入请求!已安全放弃此次存盘!");
            return;
        }

        LastCheckpointPosition = position;
        PlayerPrefs.SetFloat(CheckpointXKey, position.x);
        PlayerPrefs.SetFloat(CheckpointYKey, position.y);
        PlayerPrefs.SetFloat(CheckpointZKey, position.z);
        PlayerPrefs.SetInt(HasSavedKey, 1);
        PlayerPrefs.Save();
        Debug.Log($"[存档系统] 硬盘存档完毕!当前最新激活复活点: {position}");
    }

    public void LoadCheckpoint()
    {
        if (PlayerPrefs.GetInt(HasSavedKey, 0) == 1)
        {
            float x = PlayerPrefs.GetFloat(CheckpointXKey);
            float y = PlayerPrefs.GetFloat(CheckpointYKey);
            float z = PlayerPrefs.GetFloat(CheckpointZKey);
            LastCheckpointPosition = new Vector3(x, y, z);
        }
        else
        {
            LastCheckpointPosition = new Vector3(0f, 0f, 0f);
        }

        Debug.Log($"<color=red><b>[存档自检] 游戏刚刚启动!从硬盘载入的篝火复活点为: {LastCheckpointPosition}</b></color>");
    }

    // ============================================
    // 探索技能启用状态
    // ============================================

    public void SaveAbilityEnabled(ExplorationAbility ability, bool enabled)
    {
        PlayerPrefs.SetInt(AbilityEnabledPrefix + ability.ToString(), enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool LoadAbilityEnabled(ExplorationAbility ability, bool defaultValue = true)
    {
        string key = AbilityEnabledPrefix + ability.ToString();
        if (!PlayerPrefs.HasKey(key)) return defaultValue;
        return PlayerPrefs.GetInt(key) == 1;
    }

    public Dictionary<ExplorationAbility, bool> LoadAllAbilityEnabledStates()
    {
        var states = new Dictionary<ExplorationAbility, bool>();
        foreach (ExplorationAbility ability in System.Enum.GetValues(typeof(ExplorationAbility)))
        {
            states[ability] = LoadAbilityEnabled(ability);
        }
        return states;
    }

    // ============================================
    // 大招装备
    // ============================================

    public void SaveEquippedUltimate(string ultimateName)
    {
        PlayerPrefs.SetString(EquippedUltimateKey, ultimateName ?? "");
        PlayerPrefs.Save();
    }

    public string LoadEquippedUltimate()
    {
        return PlayerPrefs.GetString(EquippedUltimateKey, "");
    }
}