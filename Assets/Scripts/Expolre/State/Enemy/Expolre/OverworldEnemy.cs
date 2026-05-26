using UnityEngine;

/// <summary>
/// 大世界巡逻怪（继承自 EntityBase，完美复用物理、朝向和翻转代码）
/// </summary>
public class OverworldEnemy : EntityBase
{
    [Header("大世界巡逻")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private Transform ledgeCheckPoint;
    [SerializeField] private float checkRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("警戒与追击")]
    [SerializeField] private float chaseSpeed = 4.5f;
    [SerializeField] private float detectRange = 5.5f;

    [Header("大世界待机与攻击设置 [6]")]
    [SerializeField] private float idleDuration = 1.2f;    // 悬崖边缘待机的时间 (秒)
    [SerializeField] private float attackRange = 1.3f;     // 大地图触发扑击的攻击距离

    [Header("战斗关卡组配置")]
    [SerializeField] private int enemyGroupIndex = 0;

    private StateMachine<OverworldEnemy> stateMachine;
    public Transform PlayerTransform { get; private set; } // 缓存主角坐标

    // 静态只读的怪物探索期 4 个动画 Hash 
    public static readonly int Anim_Idle = Animator.StringToHash("Enemy_Idle");      // 待机 [6]
    public static readonly int Anim_Patrol = Animator.StringToHash("Enemy_Walk");    // 巡逻
    public static readonly int Anim_Chase = Animator.StringToHash("Enemy_Chase");     // 追击
    public static readonly int Anim_Attack = Animator.StringToHash("Enemy_Attack");   // 大地图扑击 [6]

    // 安全属性暴露
    public float PatrolSpeed => patrolSpeed;
    public float ChaseSpeed => chaseSpeed;
    public float IdleDuration => idleDuration; // 暴露待机时间 [6]
    public Transform LedgeCheckPoint => ledgeCheckPoint;
    public float CheckRadius => checkRadius;
    public LayerMask GroundLayer => groundLayer;

    // 核心新增：向外暴露当前怪物配置的战斗组索引属性
    public int EnemyGroupIndex => enemyGroupIndex;

    protected override void Awake()
    {
        base.Awake();

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        var playerObj = FindObjectOfType<PlayerController>();
        if (playerObj != null)
        {
            PlayerTransform = playerObj.transform;
        }

        // ========================================================
        // 核心重构：在状态机中完整注册 4 个大世界探索状态！ [6]
        // ========================================================
        stateMachine = new StateMachine<OverworldEnemy>(this);
        stateMachine.RegisterState(new EnemyIdleState());   // 待机 [6]
        stateMachine.RegisterState(new EnemyPatrolState()); // 巡逻
        stateMachine.RegisterState(new EnemyChaseState());  // 追击
        stateMachine.RegisterState(new EnemyAttackState()); // 大地图扑人 [6]

        stateMachine.ChangeState<EnemyIdleState>(); // 默认巡逻
    }

    private void Update() => stateMachine.Update();
    private void FixedUpdate() => stateMachine.FixedUpdate();

    public bool IsPlayerInRange()
    {
        if (PlayerTransform == null) return false;
        return Vector2.Distance(transform.position, PlayerTransform.position) < detectRange;
    }

    /// <summary>
    /// 检测玩家是否进入了大地图扑击距离 [6]
    /// </summary>
    public bool IsPlayerInAttackRange()
    {
        if (PlayerTransform == null) return false;
        return Vector2.Distance(transform.position, PlayerTransform.position) < attackRange;
    }

    // 碰撞玩家触发战斗不变
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //玩家碰到玩家就直接进入战斗，不用攻击
        //PlayerController player = collision.gameObject.GetComponentInParent<PlayerController>();
        //if (player != null)
        //{
        //    BattleManager.Instance.StartBattle();
        //    Destroy(gameObject);
        //}
    }

    public void OnHitByOverworldAttack(bool isPreemptive)
    {
        BattleManager.Instance.StartBattle(enemyGroupIndex, isPreemptive);
        if (isPreemptive)
        {
            Debug.Log("<color=green>[先制偷袭！] 玩家大地图远程偷袭怪成功！</color>");
        }
        Destroy(gameObject);
    }

    // ========================================================
    // 核心补齐：物理检测方法（防止状态类中直接进行复杂的物理碰撞调用） [5]
    // ========================================================
    /// <summary>
    /// 物理检测：检测怪物前方脚下是否有地面（返回 true 代表有路，false 代表悬崖踩空） [5]
    /// </summary>
    public bool CheckGroundAhead()
    {
        if (ledgeCheckPoint == null) return true;

        // 这一步就是您之前写的 OverlapCircle 检测代码，我们将其安全封装在这里！ [5]
        return Physics2D.OverlapCircle(ledgeCheckPoint.position, checkRadius, groundLayer);
    }
    // ========================================================

    private void OnDrawGizmos()
    {
        if (ledgeCheckPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(ledgeCheckPoint.position, checkRadius);
        }

        // 绘制大世界攻击判定红圈 [6]
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // ========================================================
        // 3. 核心新增：绘制警戒/追击玩家的蓝色大圆圈
        // 这样你在编辑器里可以非常直观地调整并查看怪物的“仇恨视野”范围！
        // ========================================================
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}