using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : EntityBase
{
    private StateMachine<PlayerController> stateMachine;

    [Header("输入通道引用")]
    [SerializeField] private GameplayInputReader inputReader;

    [Header("移动参数")]
    public float MoveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("冲刺参数")]
    public float dashSpeed = 16f;
    public float dashDuration = 0.18f;
    public float DashCooldown = 1.0f;
    public float LastDashTime { get; set; } = -99f;
    public bool IsDashOnCooldown => (Time.time - LastDashTime) < DashCooldown;
    public bool IsDashAvailable { get; set; } = true;

    [Header("连击设置 (参数可在面板无缝调整)")]
    public int MaxComboCount = 3;
    public float ComboWindowTime = 1.0f;
    public int CurrentComboIndex = 0;
    public float LastAttackTime = 0f;

    public int[] AttackAnimHashes { get; private set; }
    public float[] AttackDurations = { 0.35f, 0.35f, 0.5f };

    [Header("大地图攻击判定配置 (参数可在面板实时微调) [6]")]
    public float overworldAttackHitTime = 0.8f; // 刀光挥出、伤害产生判定点的时间（相对动画开始）
    public float overworldAttackRange = 1.2f;   // 攻击圆心距离玩家身体的水平偏移距离
    public float overworldAttackRadius = 0.8f;  // 伤害判定的圆形半径大

    [Header("物理检测")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    #region 输入相关与技能防呆锁
    public Vector2 MoveInput { get; private set; }
    private float jumpPressTime = -99f;
    private float attackPressTime = -99f;
    private float dashPressTime = -99f;

    private const float InputBufferTime = 0.15f;

    // ========================================================
    // 核心重构：在输入缓存属性中直接植入【技能解锁判定】！
    // 这样在相关能力未解锁时，输入信号会被物理静默拦截，完美锁死跳跃、冲刺、大地图攻击！
    // ========================================================
    public bool JumpInputBuffered => IsAbilityUnlocked(ExplorationAbility.Jump) && (Time.time - jumpPressTime) < InputBufferTime;
    public bool AttackInputBuffered => IsAbilityUnlocked(ExplorationAbility.RangedAttack) && (Time.time - attackPressTime) < InputBufferTime;
    public bool DashInputBuffered => IsAbilityUnlocked(ExplorationAbility.Dash) && (Time.time - dashPressTime) < InputBufferTime;

    public void UseJumpInput() => jumpPressTime = -99f;
    public void UseAttackInput() => attackPressTime = -99f;
    public void UseDashInput() => dashPressTime = -99f;
    #endregion

    public static readonly int Anim_Idle = Animator.StringToHash("Player_Idle");
    public static readonly int Anim_Walk = Animator.StringToHash("Player_Walk");
    public static readonly int Anim_Jump = Animator.StringToHash("Player_Jump");
    public static readonly int Anim_Fall = Animator.StringToHash("Player_Fall");
    public static readonly int Anim_Dash = Animator.StringToHash("Player_Dash");

    [Header("技能解锁数据")]
    private System.Collections.Generic.HashSet<ExplorationAbility> unlockedAbilities =
        new System.Collections.Generic.HashSet<ExplorationAbility>();

    // 辅助物理参数：当前在空中已跳跃的次数（用于二段跳判定）
    public int CurrentJumpCount { get; set; }

    // ========================================================
    // 核心重构：动态判定跳跃上限。
    // 1. 如果基础跳跃没解锁，上限为 0；
    // 2. 解锁了基础跳跃但未解锁二段跳，上限为 1；
    // 3. 基础跳跃和二段跳均解锁，上限为 2。
    // ========================================================
    public int MaxJumps
    {
        get
        {
            if (!IsAbilityUnlocked(ExplorationAbility.Jump)) return 0;
            return IsAbilityUnlocked(ExplorationAbility.DoubleJump) ? 2 : 1;
        }
    }

    public StateMachine<PlayerController> GetStateMachine() => stateMachine;

    protected override void Awake()
    {
        base.Awake();

        AttackAnimHashes = new int[]
        {
            Animator.StringToHash("Player_Attack_1"),
            Animator.StringToHash("Player_Attack_2"),
            Animator.StringToHash("Player_Attack_3")
        };

        inputReader = GetComponent<GameplayInputReader>();

        stateMachine = new StateMachine<PlayerController>(this);
        stateMachine.RegisterState(new PlayerIdleState());
        stateMachine.RegisterState(new PlayerWalkState());
        stateMachine.RegisterState(new PlayerJumpState());
        stateMachine.RegisterState(new PlayerFallState());
        stateMachine.RegisterState(new PlayerAttackState());
        stateMachine.RegisterState(new PlayerDashState());

        stateMachine.ChangeState<PlayerIdleState>();

        LoadAbilities();
    }

    private void OnEnable()
    {
        if (inputReader != null)
        {
            inputReader.OnMoveInput += OnMoveInputReceived;
            inputReader.OnJumpPressed += OnJumpPressedReceived;
            inputReader.OnFirePressed += OnFirePressedReceived;
            inputReader.OnDashPressed += OnDashPressedReceived;
        }
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.OnMoveInput -= OnMoveInputReceived;
            inputReader.OnJumpPressed -= OnJumpPressedReceived;
            inputReader.OnFirePressed -= OnFirePressedReceived;
            inputReader.OnDashPressed -= OnDashPressedReceived;
        }
    }

    private void Update()
    {
        stateMachine.Update();

        AdjustFacingDirection(MoveInput.x);
    }

    private void FixedUpdate() => stateMachine.FixedUpdate();

    #region 输入相关
    private void OnMoveInputReceived(Vector2 dir) => MoveInput = dir;
    private void OnJumpPressedReceived() => jumpPressTime = Time.time;
    private void OnFirePressedReceived() => attackPressTime = Time.time;
    private void OnDashPressedReceived() => dashPressTime = Time.time;
    #endregion

    public bool CheckIsGrounded() => groundCheckPoint == null ? true : Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);

    public void RefillDash(bool isResetCooldown = false)
    {
        IsDashAvailable = true;
        if (isResetCooldown)
        {
            LastDashTime = -99f;
        }
    }

    public bool IsAbilityUnlocked(ExplorationAbility ability) => unlockedAbilities.Contains(ability);

    public void UnlockAbility(ExplorationAbility ability)
    {
        if (!unlockedAbilities.Contains(ability))
        {
            unlockedAbilities.Add(ability);
            SaveAbilities();
            Debug.Log($"<color=cyan>[技能解锁] 恭喜！您成功解锁了新能力: {ability}</color>");
        }
    }

    public void SaveAbilities()
    {
        foreach (ExplorationAbility ability in System.Enum.GetValues(typeof(ExplorationAbility)))
        {
            string key = "Ability_" + ability.ToString();
            PlayerPrefs.SetInt(key, unlockedAbilities.Contains(ability) ? 1 : 0);
        }
        PlayerPrefs.Save();
    }

    public void LoadAbilities()
    {
        unlockedAbilities.Clear();
        foreach (ExplorationAbility ability in System.Enum.GetValues(typeof(ExplorationAbility)))
        {
            string key = "Ability_" + ability.ToString();
            if (PlayerPrefs.GetInt(key, 0) == 1)
            {
                unlockedAbilities.Add(ability);
            }
        }
    }

    // ========================================================
    // 研发特供：新增右键 Inspector 组件菜单进行一键调试，方便测试
    // ========================================================
#if UNITY_EDITOR
    [ContextMenu("Debug: 锁定所有技能")]
    public void ResetAllAbilities()
    {
        PlayerPrefs.DeleteAll();
        unlockedAbilities.Clear();
        SaveAbilities();
        Debug.Log("<color=red>[Debug] 所有技能已被重置锁定！玩家现在只能进行大地图左右移动。</color>");
    }

    [ContextMenu("Debug: 解锁所有技能")]
    public void UnlockAllAbilities()
    {
        unlockedAbilities.Clear();
        foreach (ExplorationAbility ability in System.Enum.GetValues(typeof(ExplorationAbility)))
        {
            unlockedAbilities.Add(ability);
        }
        SaveAbilities();
        Debug.Log("<color=green>[Debug] 已一键解锁所有大地图探索技能！</color>");
    }
#endif

    private void OnDrawGizmos()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }

        // 绘制大地图攻击判定圆形线框（青色）
        Gizmos.color = Color.cyan;
        Vector2 attackCheckPos = (Vector2)transform.position + new Vector2(FacingDirection * overworldAttackRange, -0.2f);
        Gizmos.DrawWireSphere(attackCheckPos, overworldAttackRadius);
    }
}