using UnityEngine;

/// <summary>
/// 战斗资源管理器
/// 管理共享 AP、MP、大招能量等战斗资源的数值与变更逻辑。
/// 独立单例，不依赖其他管理器。
/// </summary>
public class BattleResourceManager : MonoBehaviour
{
    private static BattleResourceManager _instance;
    public static BattleResourceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BattleResourceManager>();
                if (_instance == null)
                {
                    var go = new GameObject("[BattleResourceManager]");
                    _instance = go.AddComponent<BattleResourceManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [Header("共享战斗资源")]
    public int sharedAP;
    public int maxSharedAP = 5;
    public int sharedMP;
    public int maxSharedMP = 100;

    [Header("大招能量")]
    public int sharedUltimateEnergy = 0;
    public int maxSharedUltimateEnergy = 100;

    /// <summary>恢复战斗初始默认值</summary>
    public void Reset()
    {
        sharedAP = 3;
        sharedMP = 50;
        sharedUltimateEnergy = 0;
    }

    /// <summary>为大招充能并自动刷新 UI</summary>
    public void ChargeUltimate(int amount)
    {
        sharedUltimateEnergy = Mathf.Min(sharedUltimateEnergy + amount, maxSharedUltimateEnergy);
        Debug.Log($"[大招充能] 共享大招能量增加 {amount}%，当前充能量: {sharedUltimateEnergy}%");
        BattleUIController.Instance?.RefreshUI();
    }
}
