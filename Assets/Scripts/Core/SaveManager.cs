using UnityEngine;

/// <summary>
/// 全局存档管理器（纯 C# 单例类，无 MonoBehaviour 额外开销，不挂载物体） [3]
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

    private SaveManager()
    {
        LoadCheckpoint();
    }

    /// <summary>
    /// 激活篝火/存档点时调用，自动写入本地硬盘 [3]
    /// </summary>
    public void SaveCheckpoint(Vector3 position)
    {
        // ========================================================
        // 核心安全防火墙（防呆/防物理穿透保护）：
        // 我们的战斗擂台在 X=2000, Y=2000 的物理隔离空域。
        // 如果传入要保存的存档点 X 和 Y 坐标大于 1500，说明绝对是战斗中意外触发或数据覆盖产生的“脏坐标”，
        // 必须由系统直接拦截，绝对禁止写入硬盘，从而保护玩家的存档不被污染！ [3]
        // ========================================================
        if (position.x > 1500f && position.y > 1500f)
        {
            Debug.LogWarning($"[存档系统] 警告：拦截到错误的战斗场景坐标 {position} 写入请求！已安全放弃此次存盘！");
            return;
        }

        LastCheckpointPosition = position;
        PlayerPrefs.SetFloat(CheckpointXKey, position.x);
        PlayerPrefs.SetFloat(CheckpointYKey, position.y);
        PlayerPrefs.SetFloat(CheckpointZKey, position.z);
        PlayerPrefs.SetInt(HasSavedKey, 1);
        PlayerPrefs.Save();
        Debug.Log($"[存档系统] 硬盘存档完毕！当前最新激活复活点: {position}");
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
            // 默认初始起点坐标（第一关出生点）
            LastCheckpointPosition = new Vector3(0f, 0f, 0f);
        }

        // ========================================================
        // 核心新增：启动自检！看看一按下 Play 运行时，你硬盘里加载出的到底是什么坐标！ [3]
        // ========================================================
        Debug.Log($"<color=red><b>[存档自检] 游戏刚刚启动！从硬盘载入的篝火复活点为: {LastCheckpointPosition}</b></color>");
    }
}