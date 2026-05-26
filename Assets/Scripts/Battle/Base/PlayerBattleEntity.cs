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

    public PlayerForm CurrentForm => availableForms[currentFormIndex];

    // ========================================================
    // 核心新增：暂存玩家准备施放的技能和选中的目标
    // ========================================================
    public Skill PendingSkill { get; private set; }
    public BattleEntity PendingTarget { get; private set; }


    // ========================================================
    // 核心重构：将战斗特有的格挡输入缓存，全部转移到本组件注册！ [5]
    // ========================================================
    private float parryPressTime = -99f;
    private const float InputBufferTime = 0.15f; // 150毫秒格挡缓存期

    public bool ParryInputBuffered => (Time.time - parryPressTime) < InputBufferTime;
    public void UseParryInput() => parryPressTime = -99f; // 消费格挡输入 [5]

    protected override void Awake()
    {
        base.Awake(); // 获取 stats 和 animator

        // 1. 初始化并注册战斗专属的状态与状态机！
        battleStateMachine = new StateMachine<PlayerBattleEntity>(this);
        battleStateMachine.RegisterState(new PlayerBattleIdleState());
        battleStateMachine.RegisterState(new PlayerParryState());
        battleStateMachine.RegisterState(new PlayerCounterAttackState());
        battleStateMachine.RegisterState(new PlayerCastSkillState());
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
        if (BattleInputReader.Instance != null)
        {
            BattleInputReader.Instance.OnParryPressed += OnParryPressedReceived;
        }
    }

    private void OnDisable()
    {
        if (BattleInputReader.Instance != null)
        {
            BattleInputReader.Instance.OnParryPressed -= OnParryPressedReceived;
        }
    }

    // 2. 提供驱动接口供 BattleManager 调用
    public StateMachine<PlayerBattleEntity> GetBattleStateMachine() => battleStateMachine;

    public float GetParryPressTime() => parryPressTime;

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

    private void OnParryPressedReceived()
    {
        parryPressTime = Time.time; // 记录格挡输入时间戳
    }
    // ========================================================

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

            Anim.CrossFade(PlayerForm.Anim_FormChange, 0.1f);
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
    /// 核心：由 PlayerCastSkillState 在伤害判定帧时调用，对怪物造成真实伤害和削韧 [3, 5]
    /// </summary>
    public void ExecutePendingSkillDamage()
    {
        if (PendingSkill != null && PendingTarget != null)
        {
            PendingTarget.ReceiveAttack(PendingSkill.baseDamage, PendingSkill.breakDamage);
        }
    }
}