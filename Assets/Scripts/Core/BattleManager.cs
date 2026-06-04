using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum BattlePhase
{
    None,        // 大地图探索中，非战斗状态
    Setup,       // 战斗开始初始化
    PlayerTurn,  // 玩家回合
    EnemyTurn,   // 敌人回合
    Win,         // 胜利结算
    Lose         // 战败结算
}

/// <summary>
/// 战斗流程控制器
/// 负责战斗的开启/结束、场景加载/卸载、实体生成/清理、摄像机/输入切换。
/// 回合管理、目标选择、攻防判定、资源管理分别委托给独立模块。
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [System.Serializable]
    public struct EnemyGroup
    {
        public string groupName;
        public List<GameObject> enemyPrefabs;
    }

    [Header("敌人关卡数据库")]
    public List<EnemyGroup> enemyDatabase;

    [Header("转场效果")]
    [SerializeField] private BattleTransitionController transitionController;

    [Header("参战角色列表（支持全自动抓取，无需手动拖拽）")]
    public List<PlayerBattleEntity> playerParty = new List<PlayerBattleEntity>();
    public List<EnemyBattleEntity> activeEnemies = new List<EnemyBattleEntity>();

    // 流程缓存
    private PlayerController playerController;
    private Vector3 savedExplorePosition;
    private Camera exploreCamera;

    // 战斗结束标志位，防止结束期间重复进入战斗
    private bool isEndingBattle = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[BattleManager] Awake - 首次初始化，DontDestroyOnLoad");

            // 强制重置 BattleTurnManager 状态，防止编辑器中残留上次运行的状态
            var turn = BattleTurnManager.Instance;
            if (turn != null)
            {
                turn.Reset();
                Debug.Log($"[BattleManager] 强制重置 BattleTurnManager，currentPhase={turn.currentPhase}");
            }
        }
        else
        {
            Debug.Log($"[BattleManager] Awake - 已存在实例，销毁自身");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
    }

    // ============================================
    // 开始战斗
    // ============================================

    /// <summary>开始战斗（仅在大地图探索状态下允许，防止重复开战）</summary>
    public void StartBattle(int groupIndex, bool isPreemptive, System.Action onTransitionComplete = null)
    {
        // 调试日志：帮助定位战斗重入问题
        var turn = BattleTurnManager.Instance;
        Debug.Log($"[战斗调试] ========== StartBattle 被调用 ==========" +
                  $"\n  groupIndex={groupIndex}, isPreemptive={isPreemptive}" +
                  $"\n  currentPhase={turn?.currentPhase}" +
                  $"\n  isEndingBattle={isEndingBattle}");

        // 检查是否正在结束战斗
        if (isEndingBattle)
        {
            Debug.LogWarning("[战斗调试] ✗ 拒绝进入战斗！正在执行战败流程");
            return;
        }

        if (turn != null && turn.currentPhase == BattlePhase.None)
        {
            Debug.Log("[战斗调试] ✓ currentPhase == None，允许进入战斗，启动协程...");
            StartCoroutine(StartBattleRoutine(groupIndex, isPreemptive, onTransitionComplete));
        }
        else
        {
            Debug.LogWarning($"[战斗调试] ✗ 拒绝进入战斗！currentPhase={turn?.currentPhase}，不是 None");
        }
    }

    private IEnumerator StartBattleRoutine(int groupIndex, bool isPreemptive, System.Action onTransitionComplete = null)
    {
        // 0. 自动查找转场控制器（如果 Inspector 没拖的话）
        if (transitionController == null)
        {
            transitionController = FindAnyObjectByType<BattleTransitionController>();
        }

        if (transitionController != null)
        {
            Debug.Log("[转场] 开始播放遇敌转场动画...");
            bool transitionDone = false;
            transitionController.TriggerEncounter(() => transitionDone = true);
            yield return new WaitUntil(() => transitionDone);
            // 转场结束后恢复正常时间流速
            Time.timeScale = 1.0f;
            Debug.Log("[转场] 转场动画完成，开始加载战斗场景。");
        }
        else
        {
            Debug.LogWarning("[转场] 未找到 BattleTransitionController，跳过转场效果");
        }

        var turn = BattleTurnManager.Instance;
        turn.Reset();
        turn.currentPhase = BattlePhase.Setup;

        Debug.Log("[战斗系统] 检测到敌人，开始初始化战斗场景...");

        // 1. 显示鼠标光标
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 2. 切换输入映射
        var controls = InputManager.Instance.Controls.asset;
        controls?.FindActionMap("GamePlayer")?.Disable();
        controls?.FindActionMap("Battle")?.Enable();

        // 3. 冻结大地图角色
        if (playerController != null)
        {
            savedExplorePosition = playerController.transform.position;
            Debug.Log($"<color=orange><b>[调试抓取 1：准备开战] 即将冻结大地图！" +
                      $"大地图坐标: {playerController.transform.position} | 刚体位置: {playerController.rb.position}</b></color>");
            playerController.enabled = false;
        }

        // 4. 禁用大地图摄像机（禁用前缓存引用，避免 Camera.main 失效）
        exploreCamera = Camera.main;
        if (exploreCamera != null)
        {
            exploreCamera.enabled = false;
            var listener = exploreCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }

        // 5. 异步加载战斗场景
        yield return SceneManager.LoadSceneAsync("BattleScene", LoadSceneMode.Additive);

        // 6. 查找出生点
        GameObject[] playerSpawns = GameObject.FindGameObjectsWithTag("PlayerBattleSpawn");
        Transform protagonistSpawn = playerSpawns.Length > 0 ? playerSpawns[0].transform : null;
        GameObject[] enemySpawns = GameObject.FindGameObjectsWithTag("EnemyBattleSpawn");

        // 7. 构建玩家队伍
        playerParty.Clear();
        if (playerController != null)
        {
            var pEntity = playerController.GetComponent<PlayerBattleEntity>();
            if (pEntity != null) playerParty.Add(pEntity);
        }

        // 8. 瞬移玩家到战斗舞台
        if (playerController != null && protagonistSpawn != null)
        {
            playerController.rb.velocity = Vector2.zero;
            playerController.rb.position = protagonistSpawn.position;
            Physics2D.SyncTransforms();
            playerController.AdjustFacingDirection(1);
        }

        // 9. 激活战斗组件
        foreach (var member in playerParty)
        {
            if (member == null) continue;
            member.enabled = true;

            var pc = member.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;

            member.GetBattleStateMachine()?.ChangeState<PlayerBattleIdleState>();
            member.currentAP = 3;
        }

        // 10. 克隆队友
        if (PartyManager.Instance != null)
        {
            var prefabs = PartyManager.Instance.activeCompanionPrefabs;
            for (int i = 0; i < prefabs.Count; i++)
            {
                if (i + 1 >= playerSpawns.Length) break;

                var spawned = Instantiate(prefabs[i], playerSpawns[i + 1].transform.position, Quaternion.identity);
                SceneManager.MoveGameObjectToScene(spawned, SceneManager.GetSceneByName("BattleScene"));

                var entity = spawned.GetComponent<PlayerBattleEntity>();
                if (entity != null)
                {
                    playerParty.Add(entity);
                    entity.currentAP = 3;
                    entity.GetBattleStateMachine().ChangeState<PlayerBattleIdleState>();
                }
            }
        }

        // 11. 装备效果绑定
        ApplyEquipmentStatsToProtagonist();
        BindEquipmentEffectsForBattle();

        // 12. 生成敌人
        activeEnemies.Clear();
        if (groupIndex >= 0 && groupIndex < enemyDatabase.Count)
        {
            var group = enemyDatabase[groupIndex];
            for (int i = 0; i < group.enemyPrefabs.Count && i < enemySpawns.Length; i++)
            {
                var spawned = Instantiate(group.enemyPrefabs[i], enemySpawns[i].transform.position, Quaternion.identity);
                SceneManager.MoveGameObjectToScene(spawned, SceneManager.GetSceneByName("BattleScene"));

                var entity = spawned.GetComponent<EnemyBattleEntity>();
                if (entity != null) activeEnemies.Add(entity);
            }
        }

        // 13. 为子管理器注入实体引用并初始化
        turn.playerParty = playerParty;
        turn.activeEnemies = activeEnemies;
        var resolver = BattleCombatResolver.Instance;
        resolver.playerParty = playerParty;
        resolver.activeEnemies = activeEnemies;
        BattleResourceManager.Instance.Reset();

        // 14. 初始化 UI
        if (BattleUIController.Instance != null && playerParty.Count > 0 && activeEnemies.Count > 0)
            BattleUIController.Instance.InitializeUI(playerParty, activeEnemies);

        // 15. 默认选中第一个存活的敌人
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && enemy.Stats.currentHP > 0)
            {
                turn.SelectTarget(enemy);
                break;
            }
        }

        Debug.Log("[战斗系统] 敌我双方实体生成完毕，战斗准备完成！");

        // 通知调用方战斗已准备好（转场完成，可以销毁大地图敌人等）
        onTransitionComplete?.Invoke();

        // 16. 根据先制攻击决定先手
        if (isPreemptive)
            turn.EnterPlayerTurn();
        else
            turn.EnterEnemyTurn();
    }

    // ============================================
    // 结束战斗
    // ============================================

    /// <summary>结束战斗，执行胜利/战败结算</summary>
    public void EndBattle(bool isWin)
    {
        StartCoroutine(EndBattleRoutine(isWin));
    }

    private IEnumerator EndBattleRoutine(bool isWin)
    {
        // 设置标志位，阻止结束期间进入新战斗
        isEndingBattle = true;

        var turn = BattleTurnManager.Instance;
        turn.currentPhase = isWin ? BattlePhase.Win : BattlePhase.Lose;

        Debug.Log($"<color=orange><b>[调试抓取 2：准备卸载场景] 即将执行大地图回传！" +
                  $"当前记录的 savedExplorePosition: {savedExplorePosition}</b></color>");

        if (isWin)
        {
            // ---------- 胜利流程 ----------
            Debug.Log("[战斗结算] 胜利！正在执行胜利结算流程...");

            BattleUIController.Instance?.ShowVictoryPanel(true);
            yield return new WaitForSeconds(3.0f);
            BattleUIController.Instance?.CloseUI();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            var controls = InputManager.Instance.Controls.asset;
            controls?.FindActionMap("GamePlayer")?.Enable();
            controls?.FindActionMap("Battle")?.Disable();

            yield return SceneManager.UnloadSceneAsync("BattleScene");
            activeEnemies.Clear();

            // 清理克隆队友
            foreach (var member in playerParty)
            {
                if (member == null) continue;
                if (member.GetComponent<PlayerController>() == null)
                    Destroy(member.gameObject);
            }
            playerParty.Clear();

            // 恢复大地图摄像机
            if (exploreCamera != null)
            {
                exploreCamera.enabled = true;
                var listener = exploreCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
            }

            // 送回原位并保存
            if (playerController != null)
            {
                RestoreEquipmentStatsToProtagonist();
                playerController.enabled = true;
                playerController.rb.velocity = Vector2.zero;
                playerController.rb.position = savedExplorePosition;
                Physics2D.SyncTransforms();

                Debug.Log($"<color=orange><b>[调试抓取 3：大地图归位] 回归坐标: {savedExplorePosition}</b></color>");

                playerController.GetStateMachine().ChangeState<PlayerIdleState>();
                // 刷新摄像机目标引用（防止摄像机引用失效）
                FindObjectOfType<CameraController2D>()?.RefreshTarget();
                SaveManager.Instance.SaveCheckpoint(savedExplorePosition);
            }

            // 重置战斗阶段，允许再次进入战斗（使用 Instance 而非局部变量）
            BattleTurnManager.Instance.currentPhase = BattlePhase.None;
            isEndingBattle = false;
        }
        else
        {
            // ---------- 战败流程 ----------
            Debug.Log("[战斗结算] 战败！正在执行战败结算...");
            Debug.Log($"[战斗调试] 战败前状态 | currentPhase={BattleTurnManager.Instance.currentPhase} | 当前场景={SceneManager.GetActiveScene().name}");

            // 立即重置战斗阶段，防止等待期间玩家再次触发战斗
            BattleTurnManager.Instance.currentPhase = BattlePhase.None;
            Debug.Log($"[战斗调试] 立即重置 currentPhase={BattleTurnManager.Instance.currentPhase}");

            BattleUIController.Instance?.ShowDefeatPanel(true);
            yield return new WaitForSeconds(3.0f);
            BattleUIController.Instance?.CloseUI();

            activeEnemies.Clear();
            playerParty.Clear();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            var controls = InputManager.Instance.Controls.asset;
            controls?.FindActionMap("GamePlayer")?.Enable();
            controls?.FindActionMap("Battle")?.Disable();

            Debug.Log("[战斗调试] 开始加载 ExploreScene...");
            yield return SceneManager.LoadSceneAsync("ExploreScene", LoadSceneMode.Single);
            Debug.Log($"[战斗调试] ExploreScene 加载完成 | 当前场景={SceneManager.GetActiveScene().name}");

            playerController = FindObjectOfType<PlayerController>();
            Debug.Log($"[战斗调试] 场景重载后 playerController={(playerController != null ? playerController.gameObject.name : "NULL")}");

            if (playerController != null)
            {
                RestoreEquipmentStatsToProtagonist();

                var stats = playerController.GetComponent<CharacterStats>();
                if (stats != null)
                {
                    stats.currentHP = stats.maxHP;
                    stats.currentMP = stats.maxMP;
                    Debug.Log($"[战斗调试] 玩家 HP/MP 已恢复 | HP={stats.currentHP}/{stats.maxHP} | MP={stats.currentMP}/{stats.maxMP}");
                }

                playerController.rb.velocity = Vector2.zero;
                playerController.rb.position = SaveManager.Instance.LastCheckpointPosition;
                Physics2D.SyncTransforms();
                playerController.enabled = true;
                playerController.GetStateMachine().ChangeState<PlayerIdleState>();
                Debug.Log($"[战斗调试] 玩家已传送到检查点: {SaveManager.Instance.LastCheckpointPosition}");
            }
            else
            {
                Debug.LogError("[战斗调试] 场景重载后找不到 PlayerController！");
            }

            // 刷新摄像机目标引用（场景重载后确保摄像机跟随玩家）
            FindObjectOfType<CameraController2D>()?.RefreshTarget();

            // 重置战斗阶段，允许再次进入战斗（使用 Instance 而非局部变量，防止场景重载后引用失效）
            BattleTurnManager.Instance.currentPhase = BattlePhase.None;
            isEndingBattle = false;

            // 详细调试日志
            Debug.Log($"[战斗调试] ========== 战败流程完成 ==========" +
                      $"\n  currentPhase 已重置为: {BattleTurnManager.Instance.currentPhase}" +
                      $"\n  isEndingBattle={isEndingBattle}");
            Debug.Log("[战斗结算] 已回到最近一个存档点安全复活！");
        }
    }

    // ============================================
    // 装备效果绑定
    // ============================================

    private void ApplyEquipmentStatsToProtagonist()
    {
        if (playerController == null) return;
        var stats = playerController.GetComponent<CharacterStats>();
        if (stats == null) return;
        StoreAndInventory.BattleStatSyncBridge.ApplyToCharacterStats(stats);
    }

    private void RestoreEquipmentStatsToProtagonist()
    {
        if (playerController == null) return;
        var stats = playerController.GetComponent<CharacterStats>();
        if (stats == null) return;
        StoreAndInventory.BattleStatSyncBridge.RestoreCharacterStats(stats);
        ClearEquipmentEffectsForBattle();
    }

    private void BindEquipmentEffectsForBattle()
    {
        if (playerController == null) return;
        var stats = playerController.GetComponent<CharacterStats>();
        var battleEntity = playerController.GetComponent<PlayerBattleEntity>();
        if (stats == null || battleEntity == null) return;

        var runner = playerController.GetComponent<EquipmentEffectRunner>();
        if (runner == null)
            runner = playerController.gameObject.AddComponent<EquipmentEffectRunner>();

        var equipmentService = FindObjectOfType<StoreAndInventory.EquipmentService>();
        runner.BindForBattle(equipmentService, stats, battleEntity);
        battleEntity.SetEquipmentEffectRunner(runner);
    }

    private void ClearEquipmentEffectsForBattle()
    {
        if (playerController == null) return;
        var battleEntity = playerController.GetComponent<PlayerBattleEntity>();
        var runner = playerController.GetComponent<EquipmentEffectRunner>();
        runner?.ClearBattleCache();
        battleEntity?.SetEquipmentEffectRunner(null);
    }
}
