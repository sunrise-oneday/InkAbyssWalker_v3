using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBattleEntity : BattleEntity
{
    // ========================================================
    // 核心重构：声明并管理专属于战斗的状态机！
    // ========================================================
    private StateMachine<PlayerBattleEntity> battleStateMachine;

    [Header("战斗独有行动点 (AP)")]
    public int currentAP;
    public int maxAP = 5;

    [Header("多形态系统")]
    public List<PlayerForm> availableForms;
    public int currentFormIndex = 0;

    [Header("完美反击基础数值 [5]")]
    public int baseCounterDamage = 40;       // 基础反击真实伤害
    public int baseCounterBreakDamage = 35;  // 基础反击破防值

    public PlayerForm CurrentForm => availableForms[currentFormIndex];

    // ========================================================
    // 核心新增：暂存玩家准备施放的技能和选中的目标
    // ========================================================
    public Skill PendingSkill { get; private set; }
    public BattleEntity PendingTarget { get; private set; }

    EquipmentEffectRunner equipmentEffectRunner;

    public void SetEquipmentEffectRunner(EquipmentEffectRunner runner)
    {
        equipmentEffectRunner = runner;
    }

    // ========================================================
    // 核心新增：防止玩家狂按空格的物理冷却（350毫秒招架冷却） [2]
    // ========================================================
    private float lastParryPressTimeReal = -99f;
    private const float ParryCooldown = 0.35f;

    // ========================================================
    // 核心重构：将战斗特有的格挡输入缓存，全部转移到本组件注册！ [5]
    // ========================================================
    private float parryPressTime = -99f;
    private const float InputBufferTime = 0.15f; // 150毫秒格挡缓存期

    public bool ParryInputBuffered => (Time.time - parryPressTime) < InputBufferTime;
    public void UseParryInput() => parryPressTime = -99f; // 消费格挡输入 [5]


    // ========================================================
    // 核心新增：战斗闪避时间戳缓存（Shift 键）
    // ========================================================
    private float dodgePressTime = -99f;
    private float lastDodgePressTimeReal = -99f;
    private const float DodgeCooldown = 0.4f; // 防连按闪避惩罚时间
    public bool DodgeInputBuffered => (Time.time - dodgePressTime) < InputBufferTime;
    public void UseDodgeInput() => dodgePressTime = -99f;
    public float GetDodgePressTime() => dodgePressTime; // 暴露给 BattleManager 进行高精度闪避判定 [1]


    // ========================================================
    // 核心新增：战斗手动瞄准输入缓存（Q键）
    // ========================================================
    private float aimPressTime = -99f;
    public bool AimInputBuffered => (Time.time - aimPressTime) < InputBufferTime;
    public void UseAimInput() => aimPressTime = -99f;
    private void OnAimPressedReceived() => aimPressTime = Time.time;

    // ========================================================
    // 核心新增：战斗点射输入时间戳缓存（鼠标左键） [2]
    // ========================================================
    private float shootPressTime = -99f;
    public bool ShootInputBuffered => (Time.time - shootPressTime) < InputBufferTime;
    public void UseShootInput() => shootPressTime = -99f; // 消费点射输入

    // ========================================================
    // 核心重构：大招图书馆映射机制（大地图能力 ────> 战斗大招数据）
    // ========================================================
    [System.Serializable]
    public struct UltimateMapping
    {
        public ExplorationAbility requiredAbility; // 需要在大地图解锁哪个能力
        public UltimateSkill ultimateData;         // 对应解锁哪一个战斗大招的数据
    }

    [Header("大招图书馆 (大地图能力与大招数据的映射配置)")]
    [Tooltip("在此配置：解锁大地图的哪个能力，对应获得哪个大招数据")]
    public List<UltimateMapping> ultimateLibrary;

    [Header("大招装备/更换系统")]
    public UltimateSkill equippedUltimate;        // 当前玩家装备/使用的大招
    public List<UltimateSkill> unlockedUltimates; // 运行时自动加载出来的：玩家当前已解锁的大招列表 [3]

    // ========================================================

    public static readonly int Anim_Aim_Loop = Animator.StringToHash("Player_Aim_Loop"); // 瞄准持枪姿势
    public static readonly int Anim_Shoot = Animator.StringToHash("Player_Shoot");       // 射击开火动作

    protected override void Awake()
    {
        base.Awake(); // 获取 stats 和 animator

        // 1. 初始化并注册战斗专属的状态与状态机！
        battleStateMachine = new StateMachine<PlayerBattleEntity>(this);
        battleStateMachine.RegisterState(new PlayerBattleIdleState());
        battleStateMachine.RegisterState(new PlayerParryState());
        battleStateMachine.RegisterState(new PlayerCounterAttackState());
        battleStateMachine.RegisterState(new PlayerCastSkillState());
        battleStateMachine.RegisterState(new PlayerBattleDodgeState());
        battleStateMachine.RegisterState(new PlayerBattleAimState());
        battleStateMachine.RegisterState(new PlayerBattleUltimateState());
        battleStateMachine.RegisterState(new PlayerBattleDieState());

        battleStateMachine.ChangeState<PlayerBattleIdleState>();
    }

    private void Start()
    {
        // ========================================================
        // 核心新增：探索大地图时，默认禁用战斗状态机组件。
        // 只有当遭遇敌人触发 BattleManager.StartBattle 时，才由管理器统一唤醒。
        // ========================================================
        enabled = false;
    }

    private void OnEnable()
    {
        // 核心修改：每次进入战斗、启用组件时，自动根据大地图的存档，刷新可用的出战大招列表！ [3]
        LoadUnlockedUltimates();

        // 自动防空：如果玩家当前没有装备大招，且列表里有已解锁的，默认帮他装备第一个

        // ========================================================
        // 核心修复（防呆设计）：
        // 由于 Unity 序列化会将空字符串默认显示或填充为 "0"、"none"。
        // 我们将判断条件升级为：只要名字为空，或者为 "0"、"none"，都判定为“未配置”，
        // 此时系统会【全自动】强行帮你装备上已解锁的第一个大招（如：万剑归宗）！ [1, 2]
        // ========================================================
        bool isDefaultOrEmpty = string.IsNullOrEmpty(equippedUltimate.ultimateName) ||
                                equippedUltimate.ultimateName == "0" ||
                                equippedUltimate.ultimateName == "none";

        if (isDefaultOrEmpty && unlockedUltimates.Count > 0)
        {
            equippedUltimate = unlockedUltimates[0]; // 全自动装备第一个解锁的大招
            Debug.Log($"<color=orange>[大招系统] 检测到大招未配置或为默认值 '0'，已为您自动穿戴第一奥义: {equippedUltimate.ultimateName}</color>");
        }

        if (BattleInputReader.Instance != null)
        {
            BattleInputReader.Instance.OnParryPressed += OnParryPressedReceived;  
            BattleInputReader.Instance.OnDodgePressed += OnDodgePressedReceived;
            // 订阅 Q 键瞄准事件！
            BattleInputReader.Instance.OnAimPressed += OnAimPressedReceived;
            // 核心：在战斗组件中订阅战斗专属的左键点射事件！ [2]
            BattleInputReader.Instance.OnShootPressed += OnShootPressedReceived;
        }
    }

    private void OnDisable()
    {
        if (BattleInputReader.Instance != null)
        {
            BattleInputReader.Instance.OnParryPressed -= OnParryPressedReceived;
            BattleInputReader.Instance.OnDodgePressed -= OnDodgePressedReceived;
            // ???? Q ??????????
            BattleInputReader.Instance.OnAimPressed -= OnAimPressedReceived;
            // ??????????????????????????????????????? [2]
            BattleInputReader.Instance.OnShootPressed -= OnShootPressedReceived;
        }
    }

    // 2. ?????????? BattleManager ????
    public StateMachine<PlayerBattleEntity> GetBattleStateMachine() => battleStateMachine;

    public float GetParryPressTime() => parryPressTime;

    /// <summary>
    /// ???????????????????????????????????????????????????????? [3]
    /// </summary>
    public void LoadUnlockedUltimates()
    {
        unlockedUltimates.Clear();

        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController == null) return;

        foreach (var mapping in ultimateLibrary)
        {
            // ???????????????????????????????????????????????????????????????? [3]
            if (playerController.IsAbilityUnlocked(mapping.requiredAbility))
            {
                unlockedUltimates.Add(mapping.ultimateData);
            }
        }
    }

    /// <summary>
    /// 提供给您未来的 UI 更换面板调用：更换当前装备的大招
    /// </summary>
    public void EquipUltimate(UltimateSkill skill)
    {
        // 只有当前已解锁的大招，才允许被装备
        if (unlockedUltimates.Contains(skill))
        {
            equippedUltimate = skill;
            Debug.Log($"[大招更换] 成功装备奥义: {skill.ultimateName}");
        }
        else
        {
            Debug.LogWarning("[大招更换] 该奥义尚未解锁，无法装备！");
        }
    }

    /// <summary>
    /// 核心接口：动态计算当前实际的反击物理伤害（支持未来符文、状态加深、装备加成） [2]
    /// </summary>
    public int GetFinalCounterDamage()
    {
        int finalDamage = baseCounterDamage;

        // 示例：未来在这里读取你的符文/背包系统进行加算或乘算 [2]
        // if (RuneManager.Instance.IsRuneEquipped(RuneType.CounterMaster)) { finalDamage = Mathf.RoundToInt(finalDamage * 1.5f); }

        return finalDamage;
    }

    /// <summary>
    /// 核心接口：动态计算当前实际的反击削韧破防值 [2]
    /// </summary>
    public int GetFinalCounterBreakDamage()
    {
        int finalBreak = baseCounterBreakDamage;

        // 示例：未来在这里读取符文加成 [2]
        // if (RuneManager.Instance.IsRuneEquipped(RuneType.BreakMaster)) { finalBreak += 15; }

        return finalBreak;
    }

    // 3. 战斗状态机必须要在 Update 里更新！
    // 这样当大地图的 PlayerController 组件被整个 disable 禁用时，这里的战斗状态机依然能在战斗中存活并更新！
    private void Update()
    {
        battleStateMachine.Update();
    }

    private void FixedUpdate()
    {
        battleStateMachine.FixedUpdate();
    }

    private void OnShootPressedReceived()
    {
        shootPressTime = Time.time; // 记录左键按下时间戳 [2]
    }

    private void OnParryPressedReceived()
    {
        // 核心防呆：如果两次按键时间差小于 350 毫秒，判定为胡乱连点，直接冷冻惩罚！ [2]
        if (Time.time - lastParryPressTimeReal < ParryCooldown)
        {
            Debug.Log("<color=red>[招架惩罚] 检测到玩家在疯狂胡乱连点！招架判定处于冷却硬直中！</color>");
            return;
        }

        // 正常记录时间戳
        lastParryPressTimeReal = Time.time;
        parryPressTime = Time.time;
    }

    private void OnDodgePressedReceived()
    {
        // 闪避防连按抖动惩罚
        if (Time.time - lastDodgePressTimeReal < DodgeCooldown)
        {
            Debug.Log("<color=red>[闪避惩罚] 警告：请勿连续狂按闪避键！</color>");
            return;
        }

        lastDodgePressTimeReal = Time.time;
        dodgePressTime = Time.time;
    }

    /// <summary>
    /// 消耗共享 AP 切换形态 [3]
    /// </summary>
    public void SwitchForm(int targetFormIndex)
    {
        if (targetFormIndex < 0 || targetFormIndex >= availableForms.Count) return;
        if (targetFormIndex == currentFormIndex) return;

        PlayerForm targetForm = availableForms[targetFormIndex];

        // 核心修改：扣除并判定 BattleManager 中的共享 AP [3]
        if (BattleManager.Instance.sharedAP >= targetForm.apCostToSwitch)
        {
            BattleManager.Instance.sharedAP -= targetForm.apCostToSwitch;
            currentFormIndex = targetFormIndex;

            anim.CrossFade(PlayerForm.Anim_FormChange, 0.1f);
            Debug.Log($"[变身] 消耗 {targetForm.apCostToSwitch} 共享 AP，切换至: {targetForm.formName}");

            // 变身完后，立刻命令 UI 重新刷新技能卡牌按钮 [1]
            BattleUIController.Instance.RefreshUI();
        }
        else
        {
            Debug.LogWarning("[变身] 共享 AP 不足！");
        }
    }


    /// <summary>
    /// UGUI 按钮点击时触发此施法指令（安全切入施法状态） [3]
    /// </summary>
    public void CastSkill(Skill skill, BattleEntity target)
    {
        if (BattleManager.Instance.sharedMP >= skill.mpCost)
        {
            BattleManager.Instance.sharedMP -= skill.mpCost;

            PendingSkill = skill;
            PendingTarget = target;


            // ========================================================
            // 核心修改：施放技能时，为队伍的公共大招槽充能！ [3, 5]
            // ========================================================
            BattleManager.Instance.ChargeUltimate(skill.ultChargeValue);


            // 1. 如果技能配置了附着元素，施法时自动挂载给目标的属性 Stats 上！ [3]
            if (skill.applyElement != ElementType.None)
            {
                if (skill.applyElement == ElementType.Fire)
                {
                    target.Stats.AddBuff(new FireAuraBuff(skill.buffDuration));
                }
                else if (skill.applyElement == ElementType.Ice)
                {
                    target.Stats.AddBuff(new IceAuraBuff(skill.buffDuration));
                }
            }

            battleStateMachine.ChangeState<PlayerCastSkillState>();
        }
    }

    /// <summary>
    /// UGUI 按钮点击时触发大招释放 [3]
    /// </summary>
    public void CastUltimate()
    {
        if (equippedUltimate == null)
        {
            Debug.LogWarning("[大招释放] 玩家未装备任何大招！");
            return;
        }

        if (BattleManager.Instance.sharedUltimateEnergy >= BattleManager.Instance.maxSharedUltimateEnergy)
        {
            // 1. 扣除共享大招能量
            BattleManager.Instance.sharedUltimateEnergy = 0;

            // ========================================================
            // 核心修复：一扣除能量，在第一帧立即刷新 UI！
            // 这样左上角的大招进度条会瞬间清空，且底部的“终结奥义”按钮会立刻安全变暗！
            // ========================================================
            if (BattleUIController.Instance != null)
            {
                BattleUIController.Instance.RefreshUI();
            }

            // 3. 开启特写子弹时间
            BattleManager.Instance.WitchTime(0.4f);

            // 4. 切换至大招释放状态
            battleStateMachine.ChangeState<PlayerBattleUltimateState>();
        }
    }

    /// <summary>
    /// 核心：由 PlayerCastSkillState 在伤害判定帧时调用，对怪物造成真实伤害和削韧 [3, 5]
    /// </summary>
    public void ExecutePendingSkillDamage()
    {
        if (PendingSkill == null || PendingTarget == null)
            return;

        var finalDamage = PendingTarget.ReceiveAttack(PendingSkill.baseDamage, PendingSkill.breakDamage);

        equipmentEffectRunner?.OnAfterDealDamage(this, PendingSkill, finalDamage);

        if (BattleManager.Instance != null)
            BattleManager.Instance.CheckBattleOver();
    }

}