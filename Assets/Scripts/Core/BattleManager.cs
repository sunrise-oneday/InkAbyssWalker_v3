using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 确保引入场景管理命名空间

public enum BattlePhase
{
    None,        // 大世界探索中（非战斗状态，核心新增！）
    Setup,       // 战斗初始化
    PlayerTurn,  // 玩家回合
    EnemyTurn,   // 敌人回合
    Win,         // 胜利结算
    Lose         // 败北结算
}


public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [System.Serializable]
    public struct EnemyGroup
    {
        public string groupName;
        public List<GameObject> enemyPrefabs; // 挂载了 EnemyBattleEntity 的战斗场景怪物预制体
    }

    [Header("敌人关卡数据库")]
    public List<EnemyGroup> enemyDatabase;

    [Header("运行期战场角色引用 (代码全自动抓取，无需手动拖拽)")]
    public List<PlayerBattleEntity> playerParty = new List<PlayerBattleEntity>();
    public List<EnemyBattleEntity> activeEnemies = new List<EnemyBattleEntity>();

    [Header("当前战斗阶段")]
    public BattlePhase currentPhase = BattlePhase.None;
    public int currentTurn = 1; // 记录当前是第几回合

    // ========================================================
    // 核心新增：记录当前正在出招的怪物索引，实现群怪排队轮流攻击！ [3]
    // ========================================================
    private int currentEnemyTurnIndex = 0;

    // ========================================================
    // 核心重构：共享战斗资源池（队伍共用） [3]
    // ========================================================
    [Header("共享战斗资源")]
    public int sharedAP;
    public int maxSharedAP = 5;
    public int sharedMP;
    public int maxSharedMP = 100;
    // ========================================================

    [Header("共享大招资源 [共用]")]
    public int sharedUltimateEnergy = 0;           // 当前队伍共享的大招能量值（百分比 0 ~ 100） [3]
    public int maxSharedUltimateEnergy = 100;      // 大招能量上限

    // ========================================================
    // 核心修复：定义类级别的私有变量，彻底消除“上下文不存在”的编译报错 [1]
    // ========================================================
    private PlayerController playerController; // 缓存大地图的主角移动脚本 [1]
    //private PlayerBattleEntity playerBattleEntity; // 缓存大地图的主角移动脚本 [1]
    private Vector3 savedExplorePosition;      // 暂存领队主角在大地图碰怪时的物理坐标 [1]

      // ========================================================
    // 核心新增：在类级别缓存大地图摄像机，防止其被 disable 禁用后 Camera.main 无法获取！ [1]
    // ========================================================
    private Camera exploreCamera;

    // ========================================================
    // 核心重构：记录玩家当前选中的主目标怪物，支持随时点击切换！
    // ========================================================
    public EnemyBattleEntity selectedEnemy { get; private set; }

    // ========================================================
    // 核心新增：记录当前这轮怪物连斩中，玩家是否全部达成了“完美格挡”
    // ========================================================
    public bool allPerfectParriesInCurrentAttack { get; set; } = true;

    // ========================================================
    // 核心新增：限制一回合内只能通过完美闪避恢复 1 点 AP 的状态位 [3]
    // ========================================================
    public bool hasRestoredDodgeApThisRound { get; set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 确保在叠加场景时常驻内存 [3]
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // ========================================================
        // 核心新增：游戏启动时，自动在场景中寻找主角的移动控制器并缓存，
        // 彻底解放双手，无需在 Inspector 面板里手动拖拽主角了！
        // ========================================================
        playerController = FindObjectOfType<PlayerController>();
    }

    private void Update()
    {
        // ========================================================
        // 核心修改：判断玩家当前是否正处于“瞄准状态（PlayerBattleAimState）”
        // ========================================================
        bool isAiming = playerParty.Count > 0 &&
                        playerParty[0].GetBattleStateMachine().currentState is PlayerBattleAimState;

        // 只有在玩家回合，且没有进入瞄准状态时，BattleManager 的通用鼠标检测才生效
        // 这样可以彻底避免“点击切换锁定目标”与“开枪点射”发生按键冲突！
        if (currentPhase == BattlePhase.PlayerTurn && !isAiming)
        {
            HandleTargetSelection();
        }
    }

    /// <summary>
    /// 提供给玩家施法时调用：为大招充能并自动更新 UI [3, 5]
    /// </summary>
    public void ChargeUltimate(int amount)
    {
        sharedUltimateEnergy = Mathf.Min(sharedUltimateEnergy + amount, maxSharedUltimateEnergy);
        Debug.Log($"[大招充能] 队伍大招能量恢复了 {amount}%！当前总能量: {sharedUltimateEnergy}%");

        // 数据改变，立即通知 UI 重新计算大招按钮的亮起状态 [3]
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.RefreshUI();
        }
    }

    // ========================================================
    // 核心新增：获取当前回合中正在向玩家发起进攻的怪物实体（当前进攻者） [3]
    // ========================================================
    public EnemyBattleEntity CurrentAttacker
    {
        get
        {
            if (currentEnemyTurnIndex >= 0 && currentEnemyTurnIndex < activeEnemies.Count)
            {
                return activeEnemies[currentEnemyTurnIndex];
            }
            return null;
        }
    }

    /// <summary>
    /// 鼠标射线探测：点击场景中的怪物即可完成选定 [2]
    /// </summary>
    private void HandleTargetSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 从大地图的主相机发射 2D 射线（因为大地图相机虽然 disabled 了，但它的坐标和场景完全没变） [3]
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                EnemyBattleEntity clickedEnemy = hit.collider.GetComponentInParent<EnemyBattleEntity>();
                // 核心修复：只有当怪物不为空，且生命值大于 0（存活）时，才允许用鼠标选中它！
                if (clickedEnemy != null && clickedEnemy.Stats.currentHP > 0)
                {
                    SelectTarget(clickedEnemy);
                }
            }
        }
    }

    /// <summary>
    /// 核心：选定主目标，并将其他怪物的血条高亮和放大全部复位
    /// </summary>
    public void SelectTarget(EnemyBattleEntity target)
    {
        if (target == null) return;

        selectedEnemy = target;

        // 遍历所有活怪：被选中的放大发光，未选中的恢复原样
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.SetSelected(enemy == selectedEnemy);
            }
        }

        // 选中后自动刷新 UGUI 技能卡牌按钮（因为技能的目标现在对准了新选中的怪）
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.RefreshUI();
        }
    }

    /// <summary>
    /// 开始战斗
    /// </summary>
    public void StartBattle(int groupIndex, bool isPreemptive)
    {
        // ========================================================
        // 核心安全防线：只有在大地图探索状态（None）下，才允许启动战斗！
        // 如果已经开战，直接拦截重复的开战请求，绝对防止 savedExplorePosition 被二次覆盖！ [3]
        // ========================================================
        if (currentPhase == BattlePhase.None)
        {
            StartCoroutine(StartBattleRoutine(groupIndex, isPreemptive));
        }
    }

    private IEnumerator StartBattleRoutine(int groupIndex, bool isPreemptive)
    {
        currentPhase = BattlePhase.Setup;
        currentTurn = 1; // 回合重置为 1
        currentEnemyTurnIndex = 0; // 重置怪物出手顺序 [3]

        // 强行启动并激活战斗输入通道！
        //var battleReader = BattleInputReader.Instance;

        Debug.Log("[战斗系统] 遭遇敌人！开始加载战斗场景...");

        // ========================================================
        // 1. 核心：在战斗开始时，强行显示并解除鼠标锁定！（鼠标开始工作）
        // ========================================================
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 1. 切换输入动作表 
        var controls = InputManager.Instance.Controls.asset;
        if (controls != null)
        {
            controls.FindActionMap("GamePlayer")?.Disable();
            controls.FindActionMap("Battle")?.Enable();
        }

        // 2. 核心：如果大地图主角存在，暂存位置并将其物理控制器禁用（定身）
        if (playerController != null)
        {
            savedExplorePosition = playerController.transform.position; // 暂存大地图碰怪坐标 [1]
            Debug.Log($"<color=orange><b>[物理抓包 1：碰怪开战] 此时玩家尚未传送！" +
                      $"大地图坐标: {playerController.transform.position} | 刚体坐标: {playerController.rb.position} | " +
                      $"记录下的 savedExplorePosition: {savedExplorePosition}</b></color>");
            playerController.enabled = false; // 禁用大地图物理与跑跳
        }

        // 3. 禁用大地图主相机与耳朵，清除双监听器警告
        // ========================================================
        // 核心修改：在禁用前，直接用变量记录大地图相机！此时它还是亮着的，能 100% 成功抓取！ [1]
        // ========================================================
        exploreCamera = Camera.main;
        if (exploreCamera != null)
        {
            exploreCamera.enabled = false;
            var listener = exploreCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }

        // 4. 异步叠加加载战斗场景 [3]
        yield return SceneManager.LoadSceneAsync("BattleScene", LoadSceneMode.Additive);

        // 5. 寻找战斗场景中做好的传送出生点
        GameObject[] playerSpawns = GameObject.FindGameObjectsWithTag("PlayerBattleSpawn");
        Transform protagonistSpawn = playerSpawns.Length > 0 ? playerSpawns[0].transform : null;
        GameObject[] enemySpawns = GameObject.FindGameObjectsWithTag("EnemyBattleSpawn");

        // 6. 清空旧的出战列表，并将主角的战斗组件（PlayerBattleEntity）加为出战队伍的 0 号位
        playerParty.Clear();
        if (playerController != null)
        {
            PlayerBattleEntity pEntity = playerController.GetComponent<PlayerBattleEntity>();
            if (pEntity != null)
            {
                playerParty.Add(pEntity);
            }
        }

        // 7. 物理安全传送主角到战斗擂台（修改刚体，并强行同步，完美解决传送失效 Bug！） [3]
        if (playerController != null && protagonistSpawn != null)
        {
            playerController.rb.velocity = Vector2.zero;          // 抹平传送惯性速度
            playerController.rb.position = protagonistSpawn.position; // 直接修改刚体坐标 [3]
            Physics2D.SyncTransforms();                            // 强制命令 Unity 物理引擎更新 [3]
            //playerController.transform.position = protagonistSpawn.position;
            playerController.AdjustFacingDirection(1);
        }

        // ========================================================
        // 8. 核心修复：遍历队伍，唤醒并开启所有出战队员的【战斗脑区组件】！ [2]
        // ========================================================
        foreach (var member in playerParty)
        {
            if (member == null) continue;

            // 唤醒战斗状态机，使其 Update() 开始运转！ [2]
            member.enabled = true;

            // 禁用其对应的大地图移动脚本
            var pController = member.GetComponent<PlayerController>();
            if (pController != null)
            {
                savedExplorePosition = pController.transform.position; // 暂存大地图坐标
                pController.enabled = false; // 禁用大地图物理
            }

            // 启动战斗状态机
            var battleStateMachine = member.GetBattleStateMachine();
            if (battleStateMachine != null)
            {
                battleStateMachine.ChangeState<PlayerBattleIdleState>();
            }

            member.currentAP = 3;
        }

        // 8. 动态在战场上克隆在 PartyManager 里配置的归队队友!
        if (PartyManager.Instance != null)
        {
            var companionPrefabs = PartyManager.Instance.activeCompanionPrefabs;
            for (int i = 0; i < companionPrefabs.Count; i++)
            {
                // 队伍人数超过出生点上限就停止（1个给主角，其余给队友）
                if (i + 1 >= playerSpawns.Length) break;

                GameObject companionPrefab = companionPrefabs[i];
                Transform spawnPoint = playerSpawns[i + 1].transform; // 队友从第 2 个点开始站位

                // 动态克隆队友!
                GameObject spawnedCompanion = Instantiate(companionPrefab, spawnPoint.position, Quaternion.identity);

                // 将克隆出来的队友移入战斗场景，确保卸载场景时能全自动被卸载销毁
                SceneManager.MoveGameObjectToScene(spawnedCompanion, SceneManager.GetSceneByName("BattleScene"));

                PlayerBattleEntity companionEntity = spawnedCompanion.GetComponent<PlayerBattleEntity>();
                if (companionEntity != null)
                {
                    playerParty.Add(companionEntity);
                    companionEntity.currentAP = 3;
                    companionEntity.GetBattleStateMachine().ChangeState<PlayerBattleIdleState>();
                }
            }
        }

        // 9. 动态克隆怪物 
        activeEnemies.Clear();
        if (groupIndex >= 0 && groupIndex < enemyDatabase.Count)
        {
            EnemyGroup group = enemyDatabase[groupIndex];
            for (int i = 0; i < group.enemyPrefabs.Count; i++)
            {
                if (i >= enemySpawns.Length) break;

                GameObject enemyPrefab = group.enemyPrefabs[i];
                Transform spawnPoint = enemySpawns[i].transform;

                GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
                SceneManager.MoveGameObjectToScene(spawnedEnemy, SceneManager.GetSceneByName("BattleScene"));

                EnemyBattleEntity enemyEntity = spawnedEnemy.GetComponent<EnemyBattleEntity>();
                if (enemyEntity != null)
                {
                    activeEnemies.Add(enemyEntity);
                }
            }
        }

        // ========================================================
        // 核心：战斗开始时，初始化队伍共用的 AP 和 MP [3]
        // ========================================================
        sharedAP = 3;   // 初始 3 点 AP
        sharedMP = 50;  // 初始 50 点 MP

        if (BattleUIController.Instance != null && playerParty.Count > 0 && activeEnemies.Count > 0)
        {          
            BattleUIController.Instance.InitializeUI(playerParty, activeEnemies);
        }

        // 核心：开局自动挑选第一个【存活】的怪为默认目标
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && enemy.Stats.currentHP > 0)
            {
                SelectTarget(enemy);
                break;
            }
        }

        Debug.Log("[战斗系统] 场景加载与实体生成完毕，战斗准备就绪！");
        // ========================================================
        // 核心修改：根据大世界的触发方式，判定是谁的回合！ [3]
        // ========================================================
        if (isPreemptive)
        {
            // 玩家主动击中：开局进入玩家回合（先手优势） [3]
            EnterPlayerTurn();
        }
        else
        {
            // 玩家被怪物扑中：开局立刻直接切入敌人回合（玩家瞬间亮出招架状态！） [3]
            EnterEnemyTurn();
        }

        sharedUltimateEnergy = 0;
    }

    /// <summary>
    /// 结束战斗：清理战场并返回大地图 [3]
    /// </summary>
    public void EndBattle(bool isWin)
    {
        StartCoroutine(EndBattleRoutine(isWin));
    }

     private IEnumerator EndBattleRoutine(bool isWin)
    {
        currentPhase = isWin ? BattlePhase.Win : BattlePhase.Lose;

        // ========================================================
        // 抓包点 2（战后结算前）：观察在卸载场景前，savedExplorePosition 是否被篡改
        // ========================================================
        Debug.Log($"<color=orange><b>[物理抓包 2：准备卸载场景] 此时大招打完准备结算！" +
                  $"当前记录的 savedExplorePosition: {savedExplorePosition} | 玩家当前肉身真实坐标: {playerController.transform.position}</b></color>");

        if (isWin)
        {
            // ----------------------------------------------------
            // 胜利分支（原路安全回返大地图，并自动存档） [3]
            // ----------------------------------------------------
            Debug.Log("[战斗结束] 胜利！正在播放胜利结算面板...");

            // 1. 弹出胜利 UGUI 动画面板（停留 3 秒给玩家看特效）
            if (BattleUIController.Instance != null)
            {
                BattleUIController.Instance.ShowVictoryPanel(true);
            }
            yield return new WaitForSeconds(3.0f);

            // 2. 关闭战斗 UI
            if (BattleUIController.Instance != null)
            {
                BattleUIController.Instance.CloseUI();
            }

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            // 3. 恢复动作表 [2]
            var controls = InputManager.Instance.Controls.asset;
            if (controls != null)
            {
                controls.FindActionMap("GamePlayer")?.Enable();  
                controls.FindActionMap("Battle")?.Disable();     
            }

            // 4. 叠加场景卸载：克隆出的战斗怪物和小伙伴在这里会被全自动干净释放！ [3]
            yield return SceneManager.UnloadSceneAsync("BattleScene");
            activeEnemies.Clear(); 

            // 销毁克隆队友
            foreach (var member in playerParty)
            {
                if (member == null) continue;
                if (member.GetComponent<PlayerController>() == null)
                {
                    Destroy(member.gameObject);
                }
            }
            playerParty.Clear();

            // 5. 恢复主相机
            // ========================================================
            // 核心修复：直接使用我们缓存好的 exploreCamera 重新开启它！
            // 彻底绕过了 Camera.main 无法寻找已禁用相机的 Unity 底层陷阱，100% 成功睁开眼睛！ [1]
            // ========================================================
            if (exploreCamera != null)
            {
                exploreCamera.enabled = true;
                var listener = exploreCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true; // 恢复大地图的耳朵
            }

            // 6. 传送主角原位，开启移动
            if (playerController != null)
            {
                playerController.enabled = true; 
                playerController.rb.velocity = Vector2.zero;
                playerController.rb.position = savedExplorePosition; // 原地物理传送
                Physics2D.SyncTransforms();

                Debug.Log($"<color=orange><b>[物理抓包 3：传送归位后] 玩家已经执行物理回归大地图！" +
                     $"回归目标坐标(即savedExplorePosition): {savedExplorePosition} | 刚体当前实际坐标: {playerController.rb.position}</b></color>");


                playerController.GetStateMachine().ChangeState<PlayerIdleState>();
                
                // 胜利后，顺手在大地图上自动存档，保存血蓝状态！
                SaveManager.Instance.SaveCheckpoint(savedExplorePosition);
            }
        }
        else
        {
            // ----------------------------------------------------
            // 败北分支：重载整个大世界场景，在最后一个存档点复活！ [3]
            // ----------------------------------------------------
            Debug.Log("[战斗结束] 败北。正在播放战败面板...");

            // 1. 弹出战败面板
            if (BattleUIController.Instance != null)
            {
                BattleUIController.Instance.ShowDefeatPanel(true);
            }
            yield return new WaitForSeconds(3.0f);

            if (BattleUIController.Instance != null)
            {
                BattleUIController.Instance.CloseUI();
            }

            // 清理缓存
            activeEnemies.Clear();
            playerParty.Clear();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            // 恢复大地图控制 [2]
            var controls = InputManager.Instance.Controls.asset;
            if (controls != null)
            {
                controls.FindActionMap("GamePlayer")?.Enable();  
                controls.FindActionMap("Battle")?.Disable();     
            }

            // ========================================================
            // 2. 核心重载大世界（Single 模式）：
            // 直接重新读取整个大地图关卡。这样大地图上所有刚才打碎的瓶子、
            // 已经死亡的小怪都会得到最完美的底层全刷新复活！
            // ========================================================
            yield return SceneManager.LoadSceneAsync("ExploreScene", LoadSceneMode.Single);

            // 3. 场景重新加载后，获取新生成的主角脚本引用（因为旧的主角随原场景一起被销毁重置了）
            playerController = FindObjectOfType<PlayerController>();
            //playerBattleEntity = playerController?.GetComponent<PlayerBattleEntity>();

            // 4. 读取存档点数据：将满血复活的全新主角，物理传送到最后一个篝火激活点坐标上！
            if (playerController != null)
            {
                // 回满血蓝状态
                var stats = playerController.GetComponent<CharacterStats>();
                if (stats != null)
                {
                    stats.currentHP = stats.maxHP;
                    stats.currentMP = stats.maxMP;
                }

                playerController.rb.velocity = Vector2.zero;
                
                // 物理定位：传送至篝火存档位置 [3]
                playerController.rb.position = SaveManager.Instance.LastCheckpointPosition;
                Physics2D.SyncTransforms();

                playerController.enabled = true;
                playerController.GetStateMachine().ChangeState<PlayerIdleState>();
            }

            // ========================================================
            // 6. 核心重置：完全回到大地图，将当前战斗阶段重置为 None，解锁下一次碰怪开战！
            // ========================================================
            currentPhase = BattlePhase.None;

            Debug.Log("[战斗结束] 玩家已在最后一个篝火存档点安全复活！怪物和场景完全恢复刷新。");
        }
    }

    /// <summary>
    /// 进入玩家回合
    /// </summary>
    public void EnterPlayerTurn()
    {
        currentPhase = BattlePhase.PlayerTurn;

        // 核心修复：在新大回合开始时，重置“完美闪避恢复 AP”的限制标志！ [3]
        hasRestoredDodgeApThisRound = false;

        // ========================================================
        // 核心重构：回合开始时，为队伍补充公共资源（共用） [3]
        // ========================================================
        sharedAP = Mathf.Min(sharedAP + 2, maxSharedAP); // 队伍回复 2 AP [3]
        sharedMP = Mathf.Min(sharedMP + 10, maxSharedMP); // 队伍回复 10 MP [3]


        foreach (var member in playerParty)
        {
            if (member != null)
            {
                member.currentAP = Mathf.Min(member.currentAP + 2, member.maxAP);
                Debug.Log($"[回合循环] 队员 {member.gameObject.name} 回合开始！当前 AP: {member.currentAP}");
                // 推动全队 Buff 生命周期（燃烧扣血在此触发） [1]
                member.Stats.TickBuffs();
            }
        }

        // ========================================================
        // 2. 核心：玩家回合开启，将 UGUI 行动面板完全显示出来！
        // 玩家此时可以用鼠标自由点击技能、换形态、结束回合
        // ========================================================
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.RefreshUI(); // 刷新顶部的 TURN 数据和 AP 条
            BattleUIController.Instance.SetActionPanelActive(true); // 显示控制面板
        }

        Debug.Log($"[回合循环] 第 {currentTurn} 回合：玩家回合开始！");
    }

    /// <summary>
    /// 进入敌人回合（改为启动协程，实现呼吸感延迟） [3]
    /// </summary>
    public void EnterEnemyTurn()
    {
        StartCoroutine(EnterEnemyTurnRoutine());
    }

    private IEnumerator EnterEnemyTurnRoutine()
    {
        currentPhase = BattlePhase.EnemyTurn;
        Debug.Log("[回合循环] 敌方回合开始！请准备时机格挡！");

        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.SetActionPanelActive(false);
        }

        allPerfectParriesInCurrentAttack = true;

        // 1. 2 秒的对峙呼吸时间 [3]
        yield return new WaitForSeconds(2f);

        if (currentEnemyTurnIndex >= 0 && currentEnemyTurnIndex < activeEnemies.Count)
        {
            EnemyBattleEntity attacker = activeEnemies[currentEnemyTurnIndex];

            // ========================================================
            // 核心修复：死怪不行动！ [3]
            // 如果轮到这只怪出手时，它的血量已经归零（已经死了），直接跳过出招，顺延到下一只怪！
            // ========================================================
            if (attacker == null || attacker.Stats.currentHP <= 0)
            {
                Debug.Log($"[状态判定] 敌方 {attacker?.gameObject.name} 已经死亡战败，跳过其行动。");
                OnEnemyTurnFinished();
                yield break;
            }

            // 2. 核心重构（支持眩晕、破防跳过回合！）
            bool isStunned = attacker.Stats.activeBuffs.Exists(b => b is StunBuff);

            if (attacker.Stats.isBroken || isStunned)
            {
                Debug.Log($"<color=yellow>[行动受控] {attacker.gameObject.name} 正处于眩晕/破防状态中！本回合无法行动！</color>");

                // ========================================================
                // 核心修改（极致解耦）：这里不需要手动调用 CrossFade 播放眩晕动画了！
                // 因为通过观察者模式，在玩家放完控制大招的一瞬间，怪物自己就已经切入了 EnemyBattleStunState 播放眩晕动画了！
                // 此时它已经处于眩晕动作中，我们只需要让它在这里罚站 1.5 秒，然后直接切入下一回合即可！ [3, 5]
                // ========================================================
                yield return new WaitForSeconds(1.5f); // 原地罚站 1.5 秒表现 [3]

                // 手动调用出招完毕，推动下一个怪出手或交还玩家回合！
                OnEnemyTurnFinished();
                yield break; // 提前结束协程，不再出招 [3]
            }

            // ========================================================
            // 2. 招架者选定：根据玩家当前的战斗形态，决定其进入什么防御姿态！ [2, 3]
            // ========================================================
            if (playerParty.Count > 0 && playerParty[0] != null)
            {
                PlayerBattleEntity defender = playerParty[0];
                var defenderFSM = defender.GetBattleStateMachine();

                // 如果是闪避形态 (1) 或者 格挡形态 (2)，才允许切入格挡监听状态进行时机防守！ [3]
                if (defender.currentFormIndex == 1 || defender.currentFormIndex == 2)
                {
                    defenderFSM.ChangeState<PlayerParryState>();
                }
                else
                {
                    // 如果玩家是在正常形态 (0) 下结束回合的，强制退回到普通的【战斗待机状态】。
                    // 由于他不处于防守姿态下，按空格/Shift 将完全被屏蔽，只能硬生生挨怪物的暴打！ [3]
                    defenderFSM.ChangeState<PlayerBattleIdleState>();
                    Debug.Log("<color=red>[战术警报] 主角当前处于正常形态下结束回合！防守姿态关闭，无法进行任何格挡与闪避！</color>");
                }
            }

            // 4. 驱使当前怪物进入攻击状态（使用您确定的类名：EnemyBattleState！） [2]
            attacker.GetBattleStateMachine().ChangeState<EnemyBattleState>();
        }
    }

    // 核心重构补齐：当怪物出招完毕后调用本函数
    public void OnEnemyTurnFinished()
    {
        Debug.Log($"[回合循环] 敌方 {activeEnemies[currentEnemyTurnIndex].gameObject.name} 出招完毕。");


        // 核心修复：只有怪还活着，才给它恢复破防条
        if (activeEnemies[currentEnemyTurnIndex] != null && activeEnemies[currentEnemyTurnIndex].Stats.currentHP > 0)
        {
            activeEnemies[currentEnemyTurnIndex].Stats.RecoverFromBreak();
            // 核心修复：只有在单只怪物完成行动（或罚站结束）后，才精准结算它的 Buff 倒计时！
            // 这样能确保控制技能在完美生效（跳过它的行动）后，才在回合尾部安全扣减并解除！
            activeEnemies[currentEnemyTurnIndex].Stats.TickBuffs(); // 核心新增：在此处推动怪物的 Buff 倒计时！ [1]
        }

        // 核心判定：这一段怪物的连击，玩家是否【全部完美格挡】了？
        if (allPerfectParriesInCurrentAttack)
        {
            // 完美无瑕！暂缓回合推进，驱使玩家的【战斗状态机】切入【反击状态】！ [2]
            var playerStateMachine = playerParty[0].GetBattleStateMachine();
            if (playerStateMachine != null)
            {
                playerStateMachine.ChangeState<PlayerCounterAttackState>();
            }
            // 注意：由于玩家正在执行反击，我们绝对不能在此处调用 ProceedEnemyTurn()！
            // 反击完毕后，PlayerCounterAttackState.cs 会主动调用 ProceedEnemyTurn() 恢复回合运转！
        }
        else
        {
            // 没能全部完美格挡：安全将玩家切回战斗待机，并直接进行下一只怪的出手或回合交替
            var playerStateMachine = playerParty[0].GetBattleStateMachine();
            if (playerStateMachine != null)
            {
                playerStateMachine.ChangeState<PlayerBattleIdleState>();
            }

            // ========================================================
            // 核心修复：此处直接调用 ProceedEnemyTurn 推进回合即可！
            // 彻底删除了下方原本残留的重复自增代码，彻底杜绝多重并行协程导致的鬼畜死锁！ [2, 3]
            // ========================================================
            ProceedEnemyTurn();
        }
    }

    /// <summary>
    /// 核心新增：由于反击结束、或没有触发反击，正式恢复敌人回合序列的推进 [2]
    /// </summary>
    public void ProceedEnemyTurn()
    {
        currentEnemyTurnIndex++;

        if (currentEnemyTurnIndex < activeEnemies.Count)
        {
            EnterEnemyTurn(); // 轮到下一只怪出招
        }
        else
        {
            currentEnemyTurnIndex = 0;
            currentTurn++;
            EnterPlayerTurn(); // 所有人出完招，开启下一回合
        }
    }

    /// 核心修复（对齐接口）：由怪物的动画事件直接触发，
    /// 自动在内部抓取当前物理帧的时间戳与玩家的按键时间进行高精度格挡判定 [1, 5]
    /// </summary>
    public void EvaluateParryAndApplyDamage(int hitIndex, EnemyAttackSequence seq)
    {
        if (playerParty.Count == 0 || playerParty[0] == null) return;

        PlayerBattleEntity defender = playerParty[0];

        const float PerfectWindow = 0.12f; // 完美招架时间窗口 (120毫秒)
        const float NormalWindow = 0.30f;  // 普通招架时间窗口 (300毫秒)

        int rawDamage = seq.hitDamages[hitIndex];
        int breakDamage = seq.hitBreakDamages[hitIndex];

        string debugHeader = $"[受击判定] 第 {hitIndex + 1} 击落点 ────────────────\n";

        // ========================================================
        // 核心判定 1：如果这一击落下的瞬间，玩家正处于【战斗闪避状态】中！
        // 伤害和削韧会被直接 100% 绝对免疫（不产生任何受击抖动和闪红）！
        // ========================================================
        // ========================================================
        // 核心判定 1：如果这一击落下的瞬间，玩家正处于【战斗闪避状态】中！
        // 伤害和削韧会被直接 100% 绝对免疫（不产生任何受击物理解算）！
        // ========================================================
        if (defender.GetBattleStateMachine().currentState is PlayerBattleDodgeState)
        {
            // ========================================================
            // 核心修复：由于玩家选择的是“闪避保命”而不是“硬碰硬的完美格挡架招”，
            // 所以只要触发了闪避（不论是普通还是完美），都必须将“全招架”标记设为 false，直接剥夺反击资格！ [2]
            // ========================================================
            allPerfectParriesInCurrentAttack = false;

            // 获取闪避的时间差
            float dodgeTimeDiff = Time.time - defender.GetDodgePressTime();
            float dodgeDiffMs = dodgeTimeDiff * 1000f;

            const float PerfectDodgeWindow = 0.12f; // 完美闪避的时间窗口

            if (dodgeTimeDiff >= 0f && dodgeTimeDiff <= PerfectDodgeWindow)
            {
                // A. 完美闪避（Witch Time 慢动作爆发）
                Debug.Log($"{debugHeader}<color=lime>★【完美闪避！】伤害免疫！时间差: {dodgeDiffMs:F0} 毫秒。</color>");

                WitchTime(0.25f);
                defender.FlashColor(new Color(0.2f, 1.0f, 0.4f), 0.15f);
                ShakeCamera(0.12f, 0.08f);

                // 行动点恢复（单回合最高只能 +1 AP） [3]
                if (!hasRestoredDodgeApThisRound)
                {
                    hasRestoredDodgeApThisRound = true;
                    sharedAP = Mathf.Min(sharedAP + 1, maxSharedAP);
                    if (BattleUIController.Instance != null) BattleUIController.Instance.RefreshUI();
                }
            }
            else
            {
                // B. 普通闪避：全额免伤，但不恢复 AP [3]
                Debug.Log($"{debugHeader}<color=cyan>[普通闪避] 成功躲避伤害！时间差: {dodgeDiffMs:F0} 毫秒。</color>");

                // 普通闪避反馈：闪烁半透明幽灵白光
                defender.FlashColor(new Color(1f, 1f, 1f, 0.4f), 0.12f);
            }

            // 消费闪避输入
            defender.UseDodgeInput();
            // 每次格挡结算完，立即在后台检测战斗是否结束！
            CheckBattleOver();
            return; // 核心：闪避成功直接拦截，完全不吃任何伤害和削韧！ [5]
        }

        //2格挡判断
        // 1. 获取怪物动画判定事件落在当前物理帧的系统时间戳
        float hitTime = Time.time;

        // 2. 直接读取战斗实体里缓存的玩家按下空格键的时间戳 [1, 2]
        float parryPressTime = defender.GetParryPressTime();

        // 3. 精准时间差：怪物的砍中时间 减去 玩家的按键时间
        float timeDiff = hitTime - parryPressTime;
        float rawDiffMs = timeDiff * 1000f; // 转化为毫秒

        if (parryPressTime <= -99f)
        {
            // 未按键：格挡不完美
            allPerfectParriesInCurrentAttack = false; // <--- 核心标记：招架连段失败！
            Debug.Log($"{debugHeader}<color=red>结果：未检测到任何按键！直接全额受击！</color>");
            ApplyDamageFeedback(defender, rawDamage, breakDamage, isPerfect: false, isNormal: false);
        }
        else if (timeDiff < 0f)
        {
            // 按晚了：格挡不完美
            allPerfectParriesInCurrentAttack = false; // <--- 核心标记：招架连段失败！
            float lateMs = Mathf.Abs(rawDiffMs);
            Debug.Log($"{debugHeader}<color=red>结果：格挡失败！你按晚了 {lateMs:F0} 毫秒！(必须在劈中前按)</color>");
            ApplyDamageFeedback(defender, rawDamage, breakDamage, isPerfect: false, isNormal: false);
        }
        else
        {
            if (timeDiff <= PerfectWindow)
            {
                // 完美格挡：保持 allPerfectParriesInCurrentAttack 为 true（不打断完美）
                Debug.Log($"{debugHeader}<color=green>★【完美招架成功！】你在劈中前 {rawDiffMs:F0} 毫秒按下了空格！(完美区间: 0 ~ 120毫秒)</color>");
                ApplyDamageFeedback(defender, 0, 0, isPerfect: true, isNormal: false);
            }
            else if (timeDiff <= NormalWindow)
            {
                // 普通格挡：虽然免除部分伤害，但判定不完美！
                allPerfectParriesInCurrentAttack = false; // <--- 核心标记：招架连段失败！
                int reducedDamage = Mathf.RoundToInt(rawDamage * 0.3f);
                Debug.Log($"{debugHeader}<color=yellow>结果：普通格挡。你在劈中前 {rawDiffMs:F0} 毫秒按下了空格。(普通区间: 120 ~ 300毫秒)</color>");
                ApplyDamageFeedback(defender, reducedDamage, 0, isPerfect: false, isNormal: true);
            }
            else
            {
                // 按得太早了：格挡不完美
                allPerfectParriesInCurrentAttack = false; // <--- 核心标记：招架连段失败！
                Debug.Log($"{debugHeader}<color=red>结果：按键失败！你按得太早了！提前了 {rawDiffMs:F0} 毫秒！(超过了 300毫秒安全期)</color>");
                ApplyDamageFeedback(defender, rawDamage, breakDamage, isPerfect: false, isNormal: false);
            }
        }

        // 每次招架判定完毕后，立即检测战场胜负！
        CheckBattleOver();
    }

    /// <summary>
    /// 检测战场上的存活状况，实时判断胜负！
    /// </summary>
    public void CheckBattleOver()
    {
        bool isPlayerDead = CheckPlayerDead();
        bool isAllEnemiesDead = CheckAllEnemiesDead();

        Debug.Log($"[胜负自检] 检查战场生命状态 | 玩家是否阵亡: {isPlayerDead} | 怪物是否全灭: {isAllEnemiesDead}");

        if (isPlayerDead)
        {
            Debug.Log("<color=red>[胜负结算] 玩家生命归零，判定为：战斗败北！</color>");

            // ========================================================
            // 核心修改：在宣布败北、弹出 UI 前，先驱使玩家的【战斗状态机】切入【死亡状态（PlayerBattleDieState）】！
            // 这样主角就会在被怪物打空血的一瞬间，当场抱头倒地死亡！ [2, 3]
            // ========================================================
            if (playerParty.Count > 0 && playerParty[0] != null)
            {
                var playerStateMachine = playerParty[0].GetBattleStateMachine();
                if (playerStateMachine != null && !(playerStateMachine.currentState is PlayerBattleDieState))
                {
                    playerStateMachine.ChangeState<PlayerBattleDieState>();
                }
            }

            EndBattle(isWin: false); // 进入败北结算协程
        }
        else if (isAllEnemiesDead)
        {
            Debug.Log("<color=green>[胜负结算] 敌方全员生命归零，判定为：战斗胜利！</color>");
            EndBattle(isWin: true);  // 怪物全灭，胜利！
        }
    }

    private bool CheckPlayerDead()
    {
        if (playerParty.Count > 0 && playerParty[0] != null)
        {
            return playerParty[0].Stats.currentHP <= 0;
        }
        return true;
    }

    private bool CheckAllEnemiesDead()
    {
        foreach (var enemy in activeEnemies)
        {
            // 只要还有任何一只怪活着，就不能算全灭
            if (enemy != null && enemy.Stats.currentHP > 0) return false;
        }
        return true;
    }

    // ========================================================
    // 物理表现引擎：魔女时间/子弹时间（Witch Time）
    // ========================================================
    public void WitchTime(float duration)
    {
        StartCoroutine(WitchTimeRoutine(duration));
    }

    private IEnumerator WitchTimeRoutine(float duration)
    {
        Time.timeScale = 0.2f; // 画面在瞬间慢放 5 倍，凸显极限闪避的终极快感！
        yield return new WaitForSecondsRealtime(duration); // 使用真实世界不受影响的物理秒数
        Time.timeScale = 1.0f; // 极速恢复正常
    }


    /// <summary>
    /// 处理动作反馈（屏幕震动、卡肉顿挫、身体闪光）
    /// </summary>
    private void ApplyDamageFeedback(PlayerBattleEntity defender, int finalDamage, int breakDamage, bool isPerfect, bool isNormal)
    {
        if (isPerfect)
        {
            // 完美反馈：
            defender.FlashColor(Color.cyan, 0.5f); // 玩家身体闪烁亮青色防卫光芒
            ShakeCamera(0.2f, 0.25f);                     // 屏幕猛烈震动 0.2 秒
            HitStop(0.06f);                               // 画面硬直卡肉 0.06 秒（打击感极强）
        }
        else if (isNormal)
        {
            // 普通反馈：
            defender.ReceiveAttack(finalDamage, breakDamage);
            defender.FlashColor(new Color(0.8f, 0.8f, 0.8f), 0.1f); // 身体闪烁灰色暗示不完美
            ShakeCamera(0.12f, 0.08f);                                    // 屏幕轻微抖动
        }
        else
        {
            // 未格挡反馈：
            defender.ReceiveAttack(finalDamage, breakDamage);
            // 未格挡反馈：自动触发 ReceiveAttack 里的【闪红+身体抖动】表现 [5]
            //defender.FlashColor(Color.red, 0.2f); // 玩家受重创闪烁红色
            ShakeCamera(0.3f, 0.15f);                   // 屏幕长时间挫败震动
        }
    }


    // ==========================================
    // 物理表现引擎（Camera Shake & Hit Stop）
    // ==========================================
    public void ShakeCamera(float duration, float magnitude)
    {
        StartCoroutine(CameraShakeRoutine(duration, magnitude));
    }

    private IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        Camera battleCam = Camera.main; // 寻找战斗场景主相机
        if (battleCam == null) yield break;

        Vector3 originalPos = battleCam.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            battleCam.transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;

            yield return null;
        }

        battleCam.transform.position = originalPos; // 物理归位
    }

    public void HitStop(float duration)
    {
        StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.05f; // 画面几近完全静止
        yield return new WaitForSecondsRealtime(duration); // 使用不受时间缩放影响的真实秒数
        Time.timeScale = 1.0f;  // 恢复正常
    }
}