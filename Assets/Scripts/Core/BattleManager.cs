using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // ȷ�����볡�����������ռ�

public enum BattlePhase
{
    None,        // ������̽���У���ս��״̬��������������
    Setup,       // ս����ʼ��
    PlayerTurn,  // ��һغ�
    EnemyTurn,   // ���˻غ�
    Win,         // ʤ������
    Lose         // �ܱ�����
}


public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [System.Serializable]
    public struct EnemyGroup
    {
        public string groupName;
        public List<GameObject> enemyPrefabs; // ������ EnemyBattleEntity ��ս����������Ԥ����
    }

    [Header("���˹ؿ����ݿ�")]
    public List<EnemyGroup> enemyDatabase;

    [Header("������ս����ɫ���� (����ȫ�Զ�ץȡ�������ֶ���ק)")]
    public List<PlayerBattleEntity> playerParty = new List<PlayerBattleEntity>();
    public List<EnemyBattleEntity> activeEnemies = new List<EnemyBattleEntity>();

    [Header("��ǰս���׶�")]
    public BattlePhase currentPhase = BattlePhase.None;
    public int currentTurn = 1; // ��¼��ǰ�ǵڼ��غ�

    // ========================================================
    // ������������¼��ǰ���ڳ��еĹ���������ʵ��Ⱥ���Ŷ����������� [3]
    // ========================================================
    private int currentEnemyTurnIndex = 0;

    // ========================================================
    // �����ع�������ս����Դ�أ����鹲�ã� [3]
    // ========================================================
    [Header("����ս����Դ")]
    public int sharedAP;
    public int maxSharedAP = 5;
    public int sharedMP;
    public int maxSharedMP = 100;
    // ========================================================

    [Header("����������Դ [����]")]
    public int sharedUltimateEnergy = 0;           // ��ǰ���鹲���Ĵ�������ֵ���ٷֱ� 0 ~ 100�� [3]
    public int maxSharedUltimateEnergy = 100;      // ������������

    // ========================================================
    // �����޸��������༶���˽�б��������������������Ĳ����ڡ��ı��뱨�� [1]
    // ========================================================
    private PlayerController playerController; // ������ͼ�������ƶ��ű� [1]
    //private PlayerBattleEntity playerBattleEntity; // ������ͼ�������ƶ��ű� [1]
    private Vector3 savedExplorePosition;      // �ݴ���������ڴ��ͼ����ʱ���������� [1]

    // ========================================================
    // �������������༶�𻺴���ͼ���������ֹ�䱻 disable ���ú� Camera.main �޷���ȡ�� [1]
    // ========================================================
    private Camera exploreCamera;

    // ========================================================
    // �����ع�����¼��ҵ�ǰѡ�е���Ŀ����֧����ʱ����л���
    // ========================================================
    public EnemyBattleEntity selectedEnemy { get; private set; }

    // ========================================================
    // ������������¼��ǰ���ֹ�����ն�У�����Ƿ�ȫ������ˡ������񵲡�
    // ========================================================
    public bool allPerfectParriesInCurrentAttack { get; set; } = true;

    // ========================================================
    // ��������������һ�غ���ֻ��ͨ���������ָܻ� 1 �� AP ��״̬λ [3]
    // ========================================================
    public bool hasRestoredDodgeApThisRound { get; set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ȷ���ڵ��ӳ���ʱ��פ�ڴ� [3]
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // ========================================================
        // ������������Ϸ����ʱ���Զ��ڳ�����Ѱ�����ǵ��ƶ������������棬
        // ���׽��˫�֣������� Inspector ������ֶ���ק�����ˣ�
        // ========================================================
        playerController = FindObjectOfType<PlayerController>();
    }

    private void Update()
    {
        // ========================================================
    /// �����������Զ���ѡĿ���߼�������ǰ�����Ĺ������������Զ�ָ����һ�����Ĺ�
    /// </summary>
    private void CheckAndAutoSelectNextTarget()
    {
        // �����ǰû��ѡ��Ŀ�꣬���ߵ�ǰĿ����Ȼ��������ִ���κ��߼�
        if (selectedEnemy == null || selectedEnemy.Stats.currentHP > 0)
        {
            return;
        }

        // Ѱ��ս���ϵ�һ��Ѫ������ 0 �Ĵ���
        EnemyBattleEntity nextTarget = null;
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && enemy.Stats.currentHP > 0)
            {
                nextTarget = enemy;
                break;
            }
        }

        if (nextTarget != null)
        {
            // �ҵ����ŵĹ֣��Զ��л�����
            SelectTarget(nextTarget);
            Debug.Log($"[�Զ�����] ԭĿ�� {selectedEnemy.gameObject.name} �����������Զ�������һ�����Ŀ�꣺{nextTarget.gameObject.name}");
        }
        else
        {
            // ���ȫ�����ﶼ�����ˣ������ѡ�񣬵ȴ��غ��Լ촥��ʤ��
            selectedEnemy = null;
        }
    }

    /// <summary>    /// �ṩ�����ʩ��ʱ���ã�Ϊ���г��ܲ��Զ����� UI [3, 5]
    /// </summary>
    public void ChargeUltimate(int amount)
    {
        sharedUltimateEnergy = Mathf.Min(sharedUltimateEnergy + amount, maxSharedUltimateEnergy);
        Debug.Log($"[���г���] ������������ָ��� {amount}%����ǰ������: {sharedUltimateEnergy}%");

        // ���ݸı䣬����֪ͨ UI ���¼�����а�ť������״̬ [3]
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.RefreshUI();
        }
    }

    // ========================================================
    // ������������ȡ��ǰ�غ�����������ҷ�������Ĺ���ʵ�壨��ǰ�����ߣ� [3]
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
    /// �������̽�⣺��������еĹ��Ｔ�����ѡ�� [2]
    /// </summary>
    private void HandleTargetSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // �Ӵ��ͼ����������� 2D ���ߣ���Ϊ���ͼ�����Ȼ disabled �ˣ�����������ͳ�����ȫû�䣩 [3]
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                EnemyBattleEntity clickedEnemy = hit.collider.GetComponentInParent<EnemyBattleEntity>();
                // �����޸���ֻ�е����ﲻΪ�գ�������ֵ���� 0����ʱ�������������ѡ������
                if (clickedEnemy != null && clickedEnemy.Stats.currentHP > 0)
                {
                    SelectTarget(clickedEnemy);
                }
            }
        }
    }

    /// <summary>
    /// ���ģ�ѡ����Ŀ�꣬�������������Ѫ�������ͷŴ�ȫ����λ
    /// </summary>
    public void SelectTarget(EnemyBattleEntity target)
    {
        if (target == null) return;

        selectedEnemy = target;

        // �������л�֣���ѡ�еķŴ󷢹⣬δѡ�еĻָ�ԭ��
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.SetSelected(enemy == selectedEnemy);
            }
        }

        // ѡ�к��Զ�ˢ�� UGUI ���ܿ��ư�ť����Ϊ���ܵ�Ŀ�����ڶ�׼����ѡ�еĹ֣�
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.RefreshUI();
        }
    }

    /// <summary>
    /// ��ʼս��
    /// </summary>
    public void StartBattle(int groupIndex, bool isPreemptive)
    {
        // ========================================================
        // ���İ�ȫ���ߣ�ֻ���ڴ��ͼ̽��״̬��None���£�����������ս����
        // ����Ѿ���ս��ֱ�������ظ��Ŀ�ս���󣬾��Է�ֹ savedExplorePosition �����θ��ǣ� [3]
        // ========================================================
        if (currentPhase == BattlePhase.None)
        {
            StartCoroutine(StartBattleRoutine(groupIndex, isPreemptive));
        }
    }

    private IEnumerator StartBattleRoutine(int groupIndex, bool isPreemptive)
    {
        currentPhase = BattlePhase.Setup;
        currentTurn = 1; // �غ�����Ϊ 1
        currentEnemyTurnIndex = 0; // ���ù������˳�� [3]

        // ǿ������������ս������ͨ����
        //var battleReader = BattleInputReader.Instance;

        Debug.Log("[ս��ϵͳ] �������ˣ���ʼ����ս������...");

        // ========================================================
        // 1. ���ģ���ս����ʼʱ��ǿ����ʾ������������������꿪ʼ������
        // ========================================================
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 1. �л����붯���� 
        var controls = InputManager.Instance.Controls.asset;
        if (controls != null)
        {
            controls.FindActionMap("GamePlayer")?.Disable();
            controls.FindActionMap("Battle")?.Enable();
        }

        // 2. ���ģ�������ͼ���Ǵ��ڣ��ݴ�λ�ò������������������ã�������
        if (playerController != null)
        {
            savedExplorePosition = playerController.transform.position; // �ݴ���ͼ�������� [1]
            Debug.Log($"<color=orange><b>[����ץ�� 1�����ֿ�ս] ��ʱ�����δ���ͣ�" +
                      $"���ͼ����: {playerController.transform.position} | ��������: {playerController.rb.position} | " +
                      $"��¼�µ� savedExplorePosition: {savedExplorePosition}</b></color>");
            playerController.enabled = false; // ���ô��ͼ����������
        }

        // 3. ���ô��ͼ���������䣬���˫����������
        // ========================================================
        // �����޸ģ��ڽ���ǰ��ֱ���ñ�����¼���ͼ�������ʱ���������ŵģ��� 100% �ɹ�ץȡ�� [1]
        // ========================================================
        exploreCamera = Camera.main;
        if (exploreCamera != null)
        {
            exploreCamera.enabled = false;
            var listener = exploreCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }

        // 4. �첽���Ӽ���ս������ [3]
        yield return SceneManager.LoadSceneAsync("BattleScene", LoadSceneMode.Additive);

        // 5. Ѱ��ս�����������õĴ��ͳ�����
        GameObject[] playerSpawns = GameObject.FindGameObjectsWithTag("PlayerBattleSpawn");
        Transform protagonistSpawn = playerSpawns.Length > 0 ? playerSpawns[0].transform : null;
        GameObject[] enemySpawns = GameObject.FindGameObjectsWithTag("EnemyBattleSpawn");

        // 6. ��վɵĳ�ս�б����������ǵ�ս�������PlayerBattleEntity����Ϊ��ս����� 0 ��λ
        playerParty.Clear();
        if (playerController != null)
        {
            PlayerBattleEntity pEntity = playerController.GetComponent<PlayerBattleEntity>();
            if (pEntity != null)
            {
                playerParty.Add(pEntity);
            }
        }

        // 7. ������ȫ�������ǵ�ս����̨���޸ĸ��壬��ǿ��ͬ���������������ʧЧ Bug���� [3]
        if (playerController != null && protagonistSpawn != null)
        {
            playerController.rb.velocity = Vector2.zero;          // Ĩƽ���͹����ٶ�
            playerController.rb.position = protagonistSpawn.position; // ֱ���޸ĸ������� [3]
            Physics2D.SyncTransforms();                            // ǿ������ Unity ����������� [3]
            //playerController.transform.position = protagonistSpawn.position;
            playerController.AdjustFacingDirection(1);
        }

        // ========================================================
        // 8. �����޸����������飬���Ѳ��������г�ս��Ա�ġ�ս������������� [2]
        // ========================================================
        foreach (var member in playerParty)
        {
            if (member == null) continue;

            // ����ս��״̬����ʹ�� Update() ��ʼ��ת�� [2]
            member.enabled = true;

            // �������Ӧ�Ĵ��ͼ�ƶ��ű�
            var pController = member.GetComponent<PlayerController>();
            if (pController != null)
            {
                savedExplorePosition = pController.transform.position; // �ݴ���ͼ����
                pController.enabled = false; // ���ô��ͼ����
            }

            // ����ս��״̬��
            var battleStateMachine = member.GetBattleStateMachine();
            if (battleStateMachine != null)
            {
                battleStateMachine.ChangeState<PlayerBattleIdleState>();
            }

            member.currentAP = 3;
        }

        // 8. ��̬��ս���Ͽ�¡�� PartyManager �����õĹ�Ӷ���!
        if (PartyManager.Instance != null)
        {
            var companionPrefabs = PartyManager.Instance.activeCompanionPrefabs;
            for (int i = 0; i < companionPrefabs.Count; i++)
            {
                // ���������������������޾�ֹͣ��1�������ǣ���������ѣ�
                if (i + 1 >= playerSpawns.Length) break;

                GameObject companionPrefab = companionPrefabs[i];
                Transform spawnPoint = playerSpawns[i + 1].transform; // ���Ѵӵ� 2 ���㿪ʼվλ

                // ��̬��¡����!
                GameObject spawnedCompanion = Instantiate(companionPrefab, spawnPoint.position, Quaternion.identity);

                // ����¡�����Ķ�������ս��������ȷ��ж�س���ʱ��ȫ�Զ���ж������
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

        // 9. ��̬��¡���� 
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
        // ���ģ�ս����ʼʱ����ʼ�����鹲�õ� AP �� MP [3]
        // ========================================================
        sharedAP = 3;   // ��ʼ 3 �� AP
        sharedMP = 50;  // ��ʼ 50 �� MP

        if (BattleUIController.Instance != null && playerParty.Count > 0 && activeEnemies.Count > 0)
        {            BattleUIController.Instance.InitializeUI(playerParty, activeEnemies);
        }

        // ���ģ������Զ���ѡ��һ�������Ĺ�ΪĬ��Ŀ��
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && enemy.Stats.currentHP > 0)
            {
                SelectTarget(enemy);
                break;
            }
        }

        Debug.Log("[ս��ϵͳ] ����������ʵ��������ϣ�ս��׼��������");
        // ========================================================
        // �����޸ģ����ݴ�����Ĵ�����ʽ���ж���˭�Ļغϣ� [3]
        // ========================================================
        if (isPreemptive)
        {
            // ����������У����ֽ�����һغϣ��������ƣ� [3]
            EnterPlayerTurn();
        }
        else
        {
            // ��ұ��������У���������ֱ��������˻غϣ����˲�������м�״̬���� [3]
            EnterEnemyTurn();
        }

        sharedUltimateEnergy = 0;
    }

    /// <summary>
    /// ����ս��������ս�������ش��ͼ [3]
    /// </summary>
    public void EndBattle(bool isWin)
    {
        StartCoroutine(EndBattleRoutine(isWin));
    }

    private IEnumerator EndBattleRoutine(bool isWin)    {
        currentPhase = isWin ? BattlePhase.Win : BattlePhase.Lose;

        // ========================================================
        // ץ���� 2��ս�����ǰ�����۲���ж�س���ǰ��savedExplorePosition �Ƿ񱻴۸�
        // ========================================================
        Debug.Log($"<color=orange><b>[����ץ�� 2��׼��ж�س���] ��ʱ���д���׼�����㣡" +
                  $"��ǰ��¼�� savedExplorePosition: {savedExplorePosition} | ��ҵ�ǰ������ʵ����: {playerController.transform.position}</b></color>");

        if (isWin)
        {
            // ----------------------------------------------------
            // ʤ����֧��ԭ·��ȫ�ط����ͼ�����Զ��浵�� [3]
            // ----------------------------------------------------
            Debug.Log("[ս������] ʤ�������ڲ���ʤ���������...");

            // 1. ����ʤ�� UGUI ������壨ͣ�� 3 �����ҿ���Ч��
            if (BattleUIController.Instance != null)
            {
                BattleUIController.Instance.ShowVictoryPanel(true);
            }
            yield return new WaitForSeconds(3.0f);

            // 2. �ر�ս�� UI
            if (BattleUIController.Instance != null)
            {
                BattleUIController.Instance.CloseUI();
            }

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            // 3. �ָ������� [2]
            var controls = InputManager.Instance.Controls.asset;
            if (controls != null)
            {
                controls.FindActionMap("GamePlayer")?.Enable();
                controls.FindActionMap("Battle")?.Disable();            }

            // 4. ���ӳ���ж�أ���¡����ս�������С���������ᱻȫ�Զ��ɾ��ͷţ� [3]
            yield return SceneManager.UnloadSceneAsync("BattleScene");
            activeEnemies.Clear();
            // ���ٿ�¡����
            foreach (var member in playerParty)
            {
                if (member == null) continue;
                if (member.GetComponent<PlayerController>() == null)
                {
                    Destroy(member.gameObject);
                }
            }
            playerParty.Clear();

            // 5. �ָ������
            // ========================================================
            // �����޸���ֱ��ʹ�����ǻ���õ� exploreCamera ���¿�������
            // �����ƹ��� Camera.main �޷�Ѱ���ѽ�������� Unity �ײ����壬100% �ɹ������۾��� [1]
            // ========================================================
            if (exploreCamera != null)
            {
                exploreCamera.enabled = true;
                var listener = exploreCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true; // �ָ����ͼ�Ķ���
            }

            // 6. ��������ԭλ�������ƶ�
            if (playerController != null)
            {
                playerController.enabled = true;                playerController.rb.velocity = Vector2.zero;
                playerController.rb.position = savedExplorePosition; // ԭ����������
                Physics2D.SyncTransforms();

                Debug.Log($"<color=orange><b>[����ץ�� 3�����͹�λ��] ����Ѿ�ִ�������ع���ͼ��" +
                     $"�ع�Ŀ������(��savedExplorePosition): {savedExplorePosition} | ���嵱ǰʵ������: {playerController.rb.position}</b></color>");


                playerController.GetStateMachine().ChangeState<PlayerIdleState>();
                // ʤ����˳���ڴ��ͼ���Զ��浵������Ѫ��״̬��
                SaveManager.Instance.SaveCheckpoint(savedExplorePosition);
            }
        }
        else
        {
            // ----------------------------------------------------
            // �ܱ���֧���������������糡���������һ���浵�㸴� [3]
            // ----------------------------------------------------
            Debug.Log("[ս������] �ܱ������ڲ���ս�����...");

            // 1. ����ս�����
            if (BattleUIController.Instance != null)
            {
                BattleUIController.Instance.ShowDefeatPanel(true);
            }
            yield return new WaitForSeconds(3.0f);

            if (BattleUIController.Instance != null)
            {
                BattleUIController.Instance.CloseUI();
            }

            // ��������
            activeEnemies.Clear();
            playerParty.Clear();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            // �ָ����ͼ���� [2]
            var controls = InputManager.Instance.Controls.asset;
            if (controls != null)
            {
                controls.FindActionMap("GamePlayer")?.Enable();
                controls.FindActionMap("Battle")?.Disable();            }

            // ========================================================
            // 2. �������ش����磨Single ģʽ����
            // ֱ�����¶�ȡ�������ͼ�ؿ����������ͼ�����иղŴ����ƿ�ӡ�
            // �Ѿ�������С�ֶ���õ��������ĵײ�ȫˢ�¸��
            // ========================================================
            yield return SceneManager.LoadSceneAsync("ExploreScene", LoadSceneMode.Single);

            // 3. �������¼��غ󣬻�ȡ�����ɵ����ǽű����ã���Ϊ�ɵ�������ԭ����һ�����������ˣ�
            playerController = FindObjectOfType<PlayerController>();
            //playerBattleEntity = playerController?.GetComponent<PlayerBattleEntity>();

            // 4. ��ȡ�浵�����ݣ�����Ѫ�����ȫ�����ǣ��������͵����һ�����𼤻�������ϣ�
            if (playerController != null)
            {
                // ����Ѫ��״̬
                var stats = playerController.GetComponent<CharacterStats>();
                if (stats != null)
                {
                    stats.currentHP = stats.maxHP;
                    stats.currentMP = stats.maxMP;
                }

                playerController.rb.velocity = Vector2.zero;
                // ������λ������������浵λ�� [3]
                playerController.rb.position = SaveManager.Instance.LastCheckpointPosition;
                Physics2D.SyncTransforms();

                playerController.enabled = true;
                playerController.GetStateMachine().ChangeState<PlayerIdleState>();
            }

            // ========================================================
            // 6. �������ã���ȫ�ص����ͼ������ǰս���׶�����Ϊ None��������һ�����ֿ�ս��
            // ========================================================
            currentPhase = BattlePhase.None;

            Debug.Log("[ս������] ����������һ������浵�㰲ȫ�������ͳ�����ȫ�ָ�ˢ�¡�");
        }
    }

    /// <summary>
    /// ������һغ�
    /// </summary>
    public void EnterPlayerTurn()
    {
        currentPhase = BattlePhase.PlayerTurn;

        // �����޸������´�غϿ�ʼʱ�����á��������ָܻ� AP�������Ʊ�־�� [3]
        hasRestoredDodgeApThisRound = false;

        // ========================================================
        // �����ع����غϿ�ʼʱ��Ϊ���鲹�乫����Դ�����ã� [3]
        // ========================================================
        sharedAP = Mathf.Min(sharedAP + 2, maxSharedAP); // ����ظ� 2 AP [3]
        sharedMP = Mathf.Min(sharedMP + 10, maxSharedMP); // ����ظ� 10 MP [3]


        foreach (var member in playerParty)
        {
            if (member != null)
            {
                member.currentAP = Mathf.Min(member.currentAP + 2, member.maxAP);
                Debug.Log($"[�غ�ѭ��] ��Ա {member.gameObject.name} �غϿ�ʼ����ǰ AP: {member.currentAP}");
                // �ƶ�ȫ�� Buff �������ڣ�ȼ�տ�Ѫ�ڴ˴����� [1]
                member.Stats.TickBuffs();
            }
        }

        // ========================================================
        // 2. ���ģ���һغϿ������� UGUI �ж������ȫ��ʾ������
        // ��Ҵ�ʱ������������ɵ�����ܡ�����̬�������غ�
        // ========================================================
        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.RefreshUI(); // ˢ�¶����� TURN ���� and AP ��            BattleUIController.Instance.SetActionPanelActive(true); // ��ʾ�������
        }

        Debug.Log($"[�غ�ѭ��] �� {currentTurn} �غϣ���һغϿ�ʼ��");
    }

    /// <summary>
    /// ������˻غϣ���Ϊ����Э�̣�ʵ�ֺ������ӳ٣� [3]
    /// </summary>
    public void EnterEnemyTurn()
    {
        StartCoroutine(EnterEnemyTurnRoutine());
    }

    private IEnumerator EnterEnemyTurnRoutine()
    {
        currentPhase = BattlePhase.EnemyTurn;
        Debug.Log("[�غ�ѭ��] �з��غϿ�ʼ����׼��ʱ���񵲣�");

        if (BattleUIController.Instance != null)
        {
            BattleUIController.Instance.SetActionPanelActive(false);
        }

        allPerfectParriesInCurrentAttack = true;

        // 1. 2 ��Ķ��ź���ʱ�� [3]
        yield return new WaitForSeconds(2f);

        if (currentEnemyTurnIndex >= 0 && currentEnemyTurnIndex < activeEnemies.Count)
        {
            EnemyBattleEntity attacker = activeEnemies[currentEnemyTurnIndex];

            // ========================================================
            // �����޸������ֲ��ж��� [3]
            // ����ֵ���ֻ�ֳ���ʱ������Ѫ���Ѿ����㣨�Ѿ����ˣ���ֱ���������У�˳�ӵ���һֻ�֣�
            // ========================================================
            if (attacker == null || attacker.Stats.currentHP <= 0)
            {
                Debug.Log($"[״̬�ж�] �з� {attacker?.gameObject.name} �Ѿ�����ս�ܣ��������ж���");
                OnEnemyTurnFinished();
                yield break;
            }

            // 2. �����ع���֧��ѣ�Ρ��Ʒ������غϣ���
            bool isStunned = attacker.Stats.activeBuffs.Exists(b => b is StunBuff);

            if (attacker.Stats.isBroken || isStunned)
            {
                Debug.Log($"<color=yellow>[�ж��ܿ�] {attacker.gameObject.name} ������ѣ��/�Ʒ�״̬�У����غ��޷��ж���</color>");

                // ========================================================
                // �����޸ģ����½�������ﲻ��Ҫ�ֶ����� CrossFade ����ѣ�ζ����ˣ�
                // ��Ϊͨ���۲���ģʽ������ҷ�����ƴ��е�һ˲�䣬�����Լ����Ѿ������� EnemyBattleStunState ����ѣ�ζ����ˣ�
                // ��ʱ���Ѿ�����ѣ�ζ����У�����ֻ��Ҫ���������﷣վ 1.5 �룬Ȼ��ֱ��������һ�غϼ��ɣ� [3, 5]
                // ========================================================
                yield return new WaitForSeconds(1.5f); // ԭ�ط�վ 1.5 ����� [3]

                // �ֶ����ó�����ϣ��ƶ���һ���ֳ��ֻ򽻻���һغϣ�
                OnEnemyTurnFinished();
                yield break; // ��ǰ����Э�̣����ٳ��� [3]
            }

            // ========================================================
            // 2. �м���ѡ����������ҵ�ǰ��ս����̬�����������ʲô������̬�� [2, 3]
            // ========================================================
            if (playerParty.Count > 0 && playerParty[0] != null)
            {
                PlayerBattleEntity defender = playerParty[0];
                var defenderFSM = defender.GetBattleStateMachine();

                // �����������̬ (1) ���� ����̬ (2)������������񵲼���״̬����ʱ�����أ� [3]
                if (defender.currentFormIndex == 1 || defender.currentFormIndex == 2)
                {
                    defenderFSM.ChangeState<PlayerParryState>();
                }
                else
                {
                    // ����������������̬ (0) �½����غϵģ�ǿ���˻ص���ͨ�ġ�ս������״̬����
                    // �����������ڷ�����̬�£����ո�/Shift ����ȫ�����Σ�ֻ��Ӳ����������ı��� [3]
                    defenderFSM.ChangeState<PlayerBattleIdleState>();
                    Debug.Log("<color=red>[ս������] ���ǵ�ǰ����������̬�½����غϣ�������̬�رգ��޷������κθ������ܣ�</color>");
                }
            }

            // 4. ��ʹ��ǰ������빥��״̬��ʹ����ȷ����������EnemyBattleState���� [2]
            attacker.GetBattleStateMachine().ChangeState<EnemyBattleState>();
        }
    }

    // �����ع����룺�����������Ϻ���ñ�����
    public void OnEnemyTurnFinished()
    {
        Debug.Log($"[�غ�ѭ��] �з� {activeEnemies[currentEnemyTurnIndex].gameObject.name} ������ϡ�");


        // �����޸���ֻ�йֻ����ţ��Ÿ����ָ��Ʒ���
        if (activeEnemies[currentEnemyTurnIndex] != null && activeEnemies[currentEnemyTurnIndex].Stats.currentHP > 0)
        {
            activeEnemies[currentEnemyTurnIndex].Stats.RecoverFromBreak();
            // �����޸���ֻ���ڵ�ֻ��������ж�����վ�������󣬲ž�׼�������� Buff ����ʱ��
            // ������ȷ�����Ƽ�����������Ч�����������ж����󣬲��ڻغ�β����ȫ�ۼ��������
            activeEnemies[currentEnemyTurnIndex].Stats.TickBuffs(); // �����������ڴ˴��ƶ������ Buff ����ʱ�� [1]
        }

        // �����ж�����һ�ι��������������Ƿ�ȫ�������񵲡��ˣ�
        if (allPerfectParriesInCurrentAttack)
        {
            // ������覣��ݻ��غ��ƽ�����ʹ��ҵġ�ս��״̬�������롾����״̬���� [2]
            var playerStateMachine = playerParty[0].GetBattleStateMachine();
            if (playerStateMachine != null)
            {
                playerStateMachine.ChangeState<PlayerCounterAttackState>();
            }
            // ע�⣺�����������ִ�з��������Ǿ��Բ����ڴ˴����� ProceedEnemyTurn()��
            // ������Ϻ�PlayerCounterAttackState.cs ���������� ProceedEnemyTurn() �ָ��غ���ת��
        }
        else
        {
            // û��ȫ�������񵲣���ȫ������л�ս����������ֱ�ӽ�����һֻ�ֵĳ��ֻ�غϽ���
            var playerStateMachine = playerParty[0].GetBattleStateMachine();
            if (playerStateMachine != null)
            {
                playerStateMachine.ChangeState<PlayerBattleIdleState>();
            }

            // ========================================================
            // �����޸����˴�ֱ�ӵ��� ProceedEnemyTurn �ƽ��غϼ��ɣ�
            // ����ɾ�����·�ԭ���������ظ��������룬���׶ž����ز���Э�̵��µĹ��������� [2, 3]
            // ========================================================
            ProceedEnemyTurn();
        }
    }

    /// <summary>
    /// �������������ڷ�����������û�д�����������ʽ�ָ����˻غ����е��ƽ� [2]
    /// </summary>
    public void ProceedEnemyTurn()
    {
        currentEnemyTurnIndex++;

        if (currentEnemyTurnIndex < activeEnemies.Count)
        {
            EnterEnemyTurn(); // �ֵ���һֻ�ֳ���
        }
        else
        {
            currentEnemyTurnIndex = 0;
            currentTurn++;
            EnterPlayerTurn(); // �����˳����У�������һ�غ�
        }
    }

    /// �����޸�������ӿڣ����ɹ���Ķ����¼�ֱ�Ӵ�����
    /// �Զ����ڲ�ץȡ��ǰ����֡��ʱ�������ҵİ���ʱ����и߾��ȸ��ж� [1, 5]
    /// </summary>
    public void EvaluateParryAndApplyDamage(int hitIndex, EnemyAttackSequence seq)
    {
        if (playerParty.Count == 0 || playerParty[0] == null) return;

        PlayerBattleEntity defender = playerParty[0];

        const float PerfectWindow = 0.12f; // �����м�ʱ�䴰�� (120����)
        const float NormalWindow = 0.30f;  // ��ͨ�м�ʱ�䴰�� (300����)

        int rawDamage = seq.hitDamages[hitIndex];
        int breakDamage = seq.hitBreakDamages[hitIndex];

        string debugHeader = $"[�ܻ��ж�] �� {hitIndex + 1} ����� ��������������������������������\n";

        // ========================================================
        // �����ж� 1�������һ�����µ�˲�䣬��������ڡ�ս������״̬���У�
        // �˺������ͻᱻֱ�� 100% �������ߣ��������κ��ܻ����������죩��
        // ========================================================
        // ========================================================
        // �����ж� 1�������һ�����µ�˲�䣬��������ڡ�ս������״̬���У�
        // �˺������ͻᱻֱ�� 100% �������ߣ��������κ��ܻ��������㣩��
        // ========================================================
        if (defender.GetBattleStateMachine().currentState is PlayerBattleDodgeState)
        {
            // ========================================================
            // �����޸����������ѡ����ǡ����ܱ����������ǡ�Ӳ��Ӳ�������񵲼��С���
            // ����ֻҪ���������ܣ���������ͨ�����������������뽫��ȫ�мܡ������Ϊ false��ֱ�Ӱ��ᷴ���ʸ� [2]
            // ========================================================
            allPerfectParriesInCurrentAttack = false;

            // ��ȡ���ܵ�ʱ���
            float dodgeTimeDiff = Time.time - defender.GetDodgePressTime();
            float dodgeDiffMs = dodgeTimeDiff * 1000f;

            const float PerfectDodgeWindow = 0.12f; // �������ܵ�ʱ�䴰��

            if (dodgeTimeDiff >= 0f && dodgeTimeDiff <= PerfectDodgeWindow)
            {
                // A. �������ܣ�Witch Time ������������
                Debug.Log($"{debugHeader}<color=lime>��������ܣ����˺����ߣ�ʱ���: {dodgeDiffMs:F0} ���롣</color>");

                WitchTime(0.25f);
                defender.FlashColor(new Color(0.2f, 1.0f, 0.4f), 0.15f);
                ShakeCamera(0.12f, 0.08f);

                // �ж���ָ������غ����ֻ�� +1 AP�� [3]
                if (!hasRestoredDodgeApThisRound)
                {
                    hasRestoredDodgeApThisRound = true;
                    sharedAP = Mathf.Min(sharedAP + 1, maxSharedAP);
                    if (BattleUIController.Instance != null) BattleUIController.Instance.RefreshUI();
                }
            }
            else
            {
                // B. ��ͨ���ܣ�ȫ�����ˣ������ָ� AP [3]
                Debug.Log($"{debugHeader}<color=cyan>[��ͨ����] �ɹ�����˺���ʱ���: {dodgeDiffMs:F0} ���롣</color>");

                // ��ͨ���ܷ�������˸��͸������׹�
                defender.FlashColor(new Color(1f, 1f, 1f, 0.4f), 0.12f);
            }

            // ������������
            defender.UseDodgeInput();
            // ÿ�θ񵲽����꣬�����ں�̨���ս���Ƿ������
            CheckBattleOver();
            return; // ���ģ����ܳɹ�ֱ�����أ���ȫ�����κ��˺������ͣ� [5]
        }

        //2���ж�
        // 1. ��ȡ���ﶯ���ж��¼����ڵ�ǰ����֡��ϵͳʱ���
        float hitTime = Time.time;

        // 2. ֱ�Ӷ�ȡս��ʵ���ﻺ�����Ұ��¿ո����ʱ��� [1, 2]
        float parryPressTime = defender.GetParryPressTime();

        // 3. ��׼ʱ������Ŀ���ʱ�� ��ȥ ��ҵİ���ʱ��
        float timeDiff = hitTime - parryPressTime;
        float rawDiffMs = timeDiff * 1000f; // ת��Ϊ����

        if (parryPressTime <= -99f)
        {
            // δ�������񵲲�����
            allPerfectParriesInCurrentAttack = false; // <--- ���ı�ǣ��м�����ʧ�ܣ�
            Debug.Log($"{debugHeader}<color=red>�����δ��⵽�κΰ�����ֱ��ȫ���ܻ���</color>");
            ApplyDamageFeedback(defender, rawDamage, breakDamage, isPerfect: false, isNormal: false);
        }
        else if (timeDiff < 0f)
        {
            // �����ˣ��񵲲�����
            allPerfectParriesInCurrentAttack = false; // <--- ���ı�ǣ��м�����ʧ�ܣ�
            float lateMs = Mathf.Abs(rawDiffMs);
            Debug.Log($"{debugHeader}<color=red>�������ʧ�ܣ��㰴���� {lateMs:F0} ���룡(����������ǰ��)</color>");
            ApplyDamageFeedback(defender, rawDamage, breakDamage, isPerfect: false, isNormal: false);
        }
        else
        {
            if (timeDiff <= PerfectWindow)
            {
                // �����񵲣����� allPerfectParriesInCurrentAttack Ϊ true�������������
                Debug.Log($"{debugHeader}<color=green>������мܳɹ�������������ǰ {rawDiffMs:F0} ���밴���˿ո�(��������: 0 ~ 120����)</color>");
                ApplyDamageFeedback(defender, 0, 0, isPerfect: true, isNormal: false);
            }
            else if (timeDiff <= NormalWindow)
            {
                // ��ͨ�񵲣���Ȼ��������˺������ж���������
                allPerfectParriesInCurrentAttack = false; // <--- ���ı�ǣ��м�����ʧ�ܣ�
                int reducedDamage = Mathf.RoundToInt(rawDamage * 0.3f);
                Debug.Log($"{debugHeader}<color=yellow>�������ͨ�񵲡���������ǰ {rawDiffMs:F0} ���밴���˿ո�(��ͨ����: 120 ~ 300����)</color>");
                ApplyDamageFeedback(defender, reducedDamage, 0, isPerfect: false, isNormal: true);
            }
            else
            {
                // ����̫���ˣ��񵲲�����
                allPerfectParriesInCurrentAttack = false; // <--- ���ı�ǣ��м�����ʧ�ܣ�
                Debug.Log($"{debugHeader}<color=red>���������ʧ�ܣ��㰴��̫���ˣ���ǰ�� {rawDiffMs:F0} ���룡(������ 300���밲ȫ��)</color>");
                ApplyDamageFeedback(defender, rawDamage, breakDamage, isPerfect: false, isNormal: false);
            }
        }

        // ÿ���м��ж���Ϻ��������ս��ʤ����
        CheckBattleOver();
    }

    /// <summary>
    /// ���ս���ϵĴ��״����ʵʱ�ж�ʤ����
    /// </summary>
    public void CheckBattleOver()
    {
        bool isPlayerDead = CheckPlayerDead();
        bool isAllEnemiesDead = CheckAllEnemiesDead();

        Debug.Log($"[ʤ���Լ�] ���ս������״̬ | ����Ƿ�����: {isPlayerDead} | �����Ƿ�ȫ��: {isAllEnemiesDead}");

        if (isPlayerDead)
        {
            Debug.Log("<color=red>[ʤ������] ����������㣬�ж�Ϊ��ս���ܱ���</color>");

            // ========================================================
            // �����޸ģ��������ܱ������� UI ǰ������ʹ��ҵġ�ս��״̬�������롾����״̬��PlayerBattleDieState������
            // �������Ǿͻ��ڱ�������Ѫ��һ˲�䣬������ͷ���������� [2, 3]
            // ========================================================
            if (playerParty.Count > 0 && playerParty[0] != null)
            {
                var playerStateMachine = playerParty[0].GetBattleStateMachine();
                if (playerStateMachine != null && !(playerStateMachine.currentState is PlayerBattleDieState))
                {
                    playerStateMachine.ChangeState<PlayerBattleDieState>();
                }
            }

            EndBattle(isWin: false); // ����ܱ�����Э��
        }
        else if (isAllEnemiesDead)
        {
            Debug.Log("<color=green>[ʤ������] �з�ȫԱ�������㣬�ж�Ϊ��ս��ʤ����</color>");
            EndBattle(isWin: true);  // ����ȫ��ʤ����
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
            // ֻҪ�����κ�һֻ�ֻ��ţ��Ͳ�����ȫ��
            if (enemy != null && enemy.Stats.currentHP > 0) return false;
        }
        return true;
    }

    // ========================================================
    // �����������棺ħŮʱ��/�ӵ�ʱ�䣨Witch Time��
    // ========================================================
    public void WitchTime(float duration)
    {
        StartCoroutine(WitchTimeRoutine(duration));
    }

    private IEnumerator WitchTimeRoutine(float duration)
    {
        Time.timeScale = 0.2f; // ������˲������ 5 ����͹�Լ������ܵ��ռ���У�
        yield return new WaitForSecondsRealtime(duration); // ʹ����ʵ���粻��Ӱ�����������
        Time.timeScale = 1.0f; // ���ٻָ�����
    }


    /// <summary>
    /// ����������������Ļ�𶯡�����ٴ졢�������⣩
    /// </summary>
    private void ApplyDamageFeedback(PlayerBattleEntity defender, int finalDamage, int breakDamage, bool isPerfect, bool isNormal)
    {
        if (isPerfect)
        {
            // ����������
            defender.FlashColor(Color.cyan, 0.5f); // ���������˸����ɫ������â
            ShakeCamera(0.2f, 0.25f);                     // ��Ļ������ 0.2 ��
            HitStop(0.06f);                               // ����Ӳֱ���� 0.06 �루����м�ǿ��
        }
        else if (isNormal)
        {
            // ��ͨ������
            defender.ReceiveAttack(finalDamage, breakDamage);
            defender.FlashColor(new Color(0.8f, 0.8f, 0.8f), 0.1f); // ������˸��ɫ��ʾ������
            ShakeCamera(0.12f, 0.08f);                                    // ��Ļ��΢����
        }
        else
        {
            // δ�񵲷�����
            defender.ReceiveAttack(finalDamage, breakDamage);
            // δ�񵲷������Զ����� ReceiveAttack ��ġ�����+���嶶�������� [5]
            //defender.FlashColor(Color.red, 0.2f); // ������ش���˸��ɫ
            ShakeCamera(0.3f, 0.15f);                   // ��Ļ��ʱ������
        }
    }


    // ==========================================
    // �����������棨Camera Shake & Hit Stop��
    // ==========================================
    public void ShakeCamera(float duration, float magnitude)
    {
        StartCoroutine(CameraShakeRoutine(duration, magnitude));
    }

    private IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        Camera battleCam = Camera.main; // Ѱ��ս�����������
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

        battleCam.transform.position = originalPos; // ������λ
    }

    public void HitStop(float duration)
    {
        StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.05f; // ���漸����ȫ��ֹ
        yield return new WaitForSecondsRealtime(duration); // ʹ�ò���ʱ������Ӱ�����ʵ����
        Time.timeScale = 1.0f;  // �ָ�����
    }
}