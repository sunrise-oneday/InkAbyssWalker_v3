using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 确保引入场景管理命名空间

public enum BattlePhase
{
    Setup,       // 战斗初始化（角色站位、加载数据）
    PlayerTurn,  // 玩家回合（选择技能、换形态）
    EnemyTurn,   // 敌人回合（格挡与防御阶段）
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
    public BattlePhase currentPhase = BattlePhase.Setup;
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

    // ========================================================
    // 核心修复：定义类级别的私有变量，彻底消除“上下文不存在”的编译报错 [1]
    // ========================================================
    private PlayerController playerController; // 缓存大地图的主角移动脚本 [1]
    private Vector3 savedExplorePosition;      // 暂存领队主角在大地图碰怪时的物理坐标 [1]

    // ========================================================
    // 核心重构：记录玩家当前选中的主目标怪物，支持随时点击切换！
    // ========================================================
    public EnemyBattleEntity selectedEnemy { get; private set; }

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
        // 核心新增：在玩家回合，每帧监听鼠标点击，进行“点击射线选怪”！
        // ========================================================
        if (currentPhase == BattlePhase.PlayerTurn)
        {
            HandleTargetSelection();
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
                if (clickedEnemy != null)
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
    /// 开始战斗：异步加载战斗场景并传送 [3]
    /// </summary>
    public void StartBattle(int groupIndex, bool isPreemptive)
    {
        StartCoroutine(StartBattleRoutine(groupIndex, isPreemptive));
    }

    private IEnumerator StartBattleRoutine(int groupIndex, bool isPreemptive)
    {
        currentPhase = BattlePhase.Setup;
        currentTurn = 1; // 回合重置为 1
        currentEnemyTurnIndex = 0; // 重置怪物出手顺序 [3]

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
            playerController.enabled = false; // 禁用大地图物理与跑跳
        }

        // 3. 禁用大地图主相机与耳朵
        Camera exploreCamera = Camera.main;
        if (exploreCamera != null)
        {
            exploreCamera.enabled = false;
            var listener = exploreCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false; // 禁用耳朵，消除双 Listener 警告
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
            //playerController.rb.velocity = Vector2.zero;          // 抹平传送惯性速度
            //playerController.rb.position = protagonistSpawn.position; // 直接修改刚体坐标 [3]
            //Physics2D.SyncTransforms();                            // 强制命令 Unity 物理引擎更新 [3]
            playerController.transform.position = protagonistSpawn.position;
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

        // ========================================================
        // 核心：开局默认自动将“第 1 只怪物”设为首选目标，确保玩家有默认攻击靶子！
        // ========================================================
        if (activeEnemies.Count > 0)
        {
            SelectTarget(activeEnemies[0]);
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
        Debug.Log(isWin ? "[战斗系统] 胜利！正在返回大地图..." : "[战斗系统] 败北。正在返回原位...");

               // 1. 关闭战斗 UI
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.CloseUI();
        }

        // ========================================================
        // 2. 核心：大地图战斗彻底结束，将鼠标重新隐藏锁定，恢复探索模式！
        // ========================================================
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 1. 恢复大地图控制：开启 GamePlayer，关闭 Battle 动作表 [2]
        var controls = InputManager.Instance.Controls.asset;
        if (controls != null)
        {
            controls.FindActionMap("GamePlayer")?.Enable();
            controls.FindActionMap("Battle")?.Disable();
        }

        // 2. 异步卸载战斗场景（克隆出来的队友和怪物会自动随场景一并销毁！） [3]
        yield return SceneManager.UnloadSceneAsync("BattleScene");
        activeEnemies.Clear();

        // 3. 销毁大出战列表（playerParty）里的克隆队友，只保留传送的主角 [3]
        foreach (var member in playerParty)
        {
            if (member == null) continue;

            // 核心修复：战斗结束，禁用这名角色的战斗脑区，防止与探索脚本打架
            member.enabled = false;

            // 如果它不是主角（身上没有大地图控制器），说明它是克隆出来的临时队友，销毁之！ [3]
            if (member.GetComponent<PlayerController>() == null)
            {
                Destroy(member.gameObject);
            }
        }
        playerParty.Clear(); // 清空缓存

        // 4. 恢复主相机与耳朵
        Camera exploreCamera = Camera.main;
        if (exploreCamera != null)
        {
            exploreCamera.enabled = true;
            var listener = exploreCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
        }

        // ========================================================
        // 5. 核心修复：物理传送主角回原位，并重新启用大地图跑跳物理 [3]
        // 由于我们使用了全局缓存变量 playerController，此处绝不会再报任何“上下文不存在”的错误！
        // ========================================================
        if (playerController != null)
        {
            playerController.enabled = true; // 恢复物理移动组件

            playerController.rb.velocity = Vector2.zero; // 清空物理残留速度
            playerController.rb.position = savedExplorePosition; // 修改刚体传送 [3]
            Physics2D.SyncTransforms();

            playerController.GetStateMachine().ChangeState<PlayerIdleState>();
        }

        Debug.Log("[战斗系统] 已成功结束战斗，返回大地图。");
    }

    public void EnterPlayerTurn()
    {
        currentPhase = BattlePhase.PlayerTurn;

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

        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.SetActionPanelActive(false);
        }

        // ========================================================
        // 核心修改（手感细节）：敌方回合开始时，强制等待 1.2 秒给玩家缓冲反应时间！
        // 多个怪排队轮流攻击时，也会在两只怪攻击间产生 1.2 秒间隔，表现非常高级！ [3]
        // ========================================================
        yield return new WaitForSeconds(1.2f);

        // 检测当前出手顺序的怪物是否合法、是否存活
        if (currentEnemyTurnIndex >= 0 && currentEnemyTurnIndex < activeEnemies.Count)
        {
            EnemyBattleEntity attacker = activeEnemies[currentEnemyTurnIndex]; // 依次呼叫怪物出战！ [3]

            if (attacker != null)
            {
                EnemyAttackSequence seq = attacker.GetAttackSequence();

                // 招架方选定：主角接招
                if (playerParty.Count > 0 && playerParty[0] != null)
                {
                    PlayerBattleEntity defender = playerParty[0];
                    var battleStateMachine = defender.GetBattleStateMachine();

                    PlayerParryState parryState = battleStateMachine.GetState<PlayerParryState>();
                    if (parryState != null)
                    {
                        parryState.SetAttackSequence(seq);
                        battleStateMachine.ChangeState<PlayerParryState>();
                    }
                }

                // 驱使当前排队的怪物切入攻击状态！
                attacker.GetBattleStateMachine().ChangeState<EnemyBattleState>();
            }
        }
    }

    // ========================================================
    // 核心重构补齐：当怪物出招完毕后调用本函数
    // ========================================================
    public void OnEnemyTurnFinished()
    {
        Debug.Log($"[回合循环] 敌方 {activeEnemies[currentEnemyTurnIndex].gameObject.name} 出招完毕。");

        // 怪物破防条恢复 [5]
        if (activeEnemies[currentEnemyTurnIndex] != null)
        {
            activeEnemies[currentEnemyTurnIndex].Stats.RecoverFromBreak();
        }

        // 1. 核心：增加出手队列索引（轮到下一个怪物出招） [3]
        currentEnemyTurnIndex++;

        // 2. 如果还有存活的怪物没有出过手，继续执行敌人回合！ [3]
        if (currentEnemyTurnIndex < activeEnemies.Count)
        {
            EnterEnemyTurn(); // 继续下一个怪的攻击回合 [3]
        }
        else
        {
            // 3. 所有怪物全部攻击完毕，重置出手队列，增加回合数，无缝交还给玩家！ [3]
            currentEnemyTurnIndex = 0;
            currentTurn++;
            EnterPlayerTurn();
        }
    }

    /// <summary>
    /// 核心：由怪物的动画事件触发，进行高精度时间差格挡判定 [1]
    /// </summary>
    /// <param name="hitIndex">当前是第几击</param>
    /// <param name="seq">怪物的攻击数据源</param>
    public void EvaluateParryAndApplyDamage(int hitIndex, EnemyAttackSequence seq)
    {
        if (playerParty.Count == 0 || playerParty[0] == null) return;

        PlayerBattleEntity defender = playerParty[0];

        // 完美格挡窗口（玩家在被砍中前 120 毫秒内按了招架）
        const float PerfectWindow = 0.12f;
        // 普通格挡窗口（玩家在被砍中前 300 毫秒内按了招架）
        const float NormalWindow = 0.30f;

        // 计算时间差：怪物砍中你的瞬间（Time.time）与你按下空格的系统时间（parryPressTime）之差 [1]
        // 提示：defender.GetParryPressTime() 需在 PlayerBattleEntity 中暴露 parryPressTime 的只读属性
        float timeDiff = Time.time - defender.GetParryPressTime();

        int rawDamage = seq.hitDamages[hitIndex];
        int breakDamage = seq.hitBreakDamages[hitIndex];

        // 注意：因为是玩家提前格挡，所以 Time.time 必定大于 parryPressTime，差值必定是正数
        if (timeDiff >= 0f && timeDiff <= PerfectWindow)
        {
            // 完美招架
            Debug.Log($"<color=green>[完美招架！] 成功招架第 {hitIndex + 1} 击！时间差: {timeDiff:F3}s</color>");
        }
        else if (timeDiff >= 0f && timeDiff <= NormalWindow)
        {
            // 普通招架：承受 30% 减免伤害
            int reducedDamage = Mathf.RoundToInt(rawDamage * 0.3f);
            defender.ReceiveAttack(reducedDamage, 0);
            Debug.Log($"<color=yellow>[普通招架] 成功格挡第 {hitIndex + 1} 击！承受 {reducedDamage} 减伤。时间差: {timeDiff:F3}s</color>");
        }
        else
        {
            // 没挡住/受击：承受全额伤害和削韧
            defender.ReceiveAttack(rawDamage, breakDamage);
            Debug.Log($"<color=red>[未挡下/受击！] 第 {hitIndex + 1} 击招架失败！承受全额伤害 {rawDamage}！时间差: {timeDiff:F3}s</color>");
        }

        // 判定完后，重置玩家格挡缓存
        defender.UseParryInput();
    }
}