using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能初始化器：用代码初始化三形态的所有技能
/// 参考明日方舟终末地的元素反应系统设计
/// 注意：只有元素反应才回复终结技能量，普通技能不回复
/// </summary>
public static class SkillInitializer
{
    // 动画状态名常量
    private const string ANIM_ATTACK_1 = "Player_Attack_1";
    private const string ANIM_ATTACK_2 = "Player_Attack_2";
    private const string ANIM_ATTACK_3 = "Player_Attack_3";

    /// <summary>
    /// 获取初临形态描述
    /// </summary>
    public static string GetChuLinDescription()
    {
        return "均衡型形态，擅长施加元素附着\n" +
               "炎墨施加火附着，寒墨施加冰附着\n" +
               "为其他形态触发元素反应创造条件\n" +
               "<color=#FFD700>元素反应可恢复终结技能量！</color>\n\n" +
               "<color=#87CEEB>当前形态按键操作：\n" +
               "Q：瞄准 | 鼠标左键：点射\n" +
               "快捷键 1/2/3：切换形态</color>";
    }

    /// <summary>
    /// 获取流墨形态描述
    /// </summary>
    public static string GetLiuMoDescription()
    {
        return "连击型形态，擅长触发元素反应\n" +
               "焰追和冰追施加水元素\n" +
               "触发燃烧/冻结/蒸发反应并恢复终结技能量\n" +
               "<color=#FFD700>元素反应可恢复终结技能量</color>\n\n" +
               "<color=#87CEEB>回合结束当前形态按键操作：\n" +
               "Shift：闪避敌人攻击,回复AP\n" +
               "快捷键 1/2/3：切换形态</color>";
    }

    /// <summary>
    /// 获取守墨形态描述
    /// </summary>
    public static string GetShouMoDescription()
    {
        return "防御型形态，擅长防御和控制\n" +
               "墨壁和墨甲提供护盾，\n" +
               "墨缚施加冰附着并减速敌人\n" +
               "<color=#FFD700>元素反应可恢复终结技能量</color>\n\n" +
               "<color=#87CEEB>回合结束当前形态按键操作：\n" +
               "E：格挡敌人攻击,造成反击\n" +
               "快捷键 1/2/3：切换形态</color>";
    }

    /// <summary>
    /// 初始化初临形态的技能（均衡型 - 施加附着）
    /// 注意：普通技能不回复终结技能量
    /// </summary>
    public static List<Skill> InitChuLinSkills()
    {
        return new List<Skill>
        {
            new Skill
            {
                skillName = "墨刺",
                description = "基础刺击，造成8点伤害，削韧2。",
                mpCost = 2,
                baseDamage = 8,
                breakDamage = 2,
                animationState = ANIM_ATTACK_1,
                applyElement = ElementType.None,
                auraStacks = 0,
                buffDuration = 0,
                ultChargeValue = 0 // 普通技能不回复能量
            },
            new Skill
            {
                skillName = "炎墨",
                description = "施加火附着2层，持续3回合。\n触发燃烧：火+水，每层8点持续伤害。\n触发融化：火+冰，每层12点爆发伤害。\n<color=#87CEEB>元素反应可恢复终结技能量！</color>",
                mpCost = 3,
                baseDamage = 10,
                breakDamage = 1,
                animationState = ANIM_ATTACK_2,
                applyElement = ElementType.Fire,
                auraStacks = 2,
                buffDuration = 3,
                ultChargeValue = 0 // 普通技能不回复能量
            },
            new Skill
            {
                skillName = "寒墨",
                description = "施加冰附着2层，持续3回合。\n触发冻结：冰+水，眩晕1回合+每层5点伤害。\n触发融化：冰+火，每层12点爆发伤害。\n<color=#87CEEB>元素反应可恢复终结技能量！</color>",
                mpCost = 3,
                baseDamage = 10,
                breakDamage = 1,
                animationState = ANIM_ATTACK_2,
                applyElement = ElementType.Ice,
                auraStacks = 2,
                buffDuration = 3,
                ultChargeValue = 0 // 普通技能不回复能量
            },
            new Skill
            {
                skillName = "墨涌",
                description = "重击，造成15点伤害，削韧3。\n对有元素附着的目标额外造成50%伤害。",
                mpCost = 5,
                baseDamage = 15,
                breakDamage = 3,
                animationState = ANIM_ATTACK_3,
                applyElement = ElementType.None,
                auraStacks = 0,
                buffDuration = 0,
                ultChargeValue = 0 // 普通技能不回复能量
            }
        };
    }

    /// <summary>
    /// 初始化流墨形态的技能（连击型 - 触发反应）
    /// 注意：只有元素反应才回复终结技能量
    /// </summary>
    public static List<Skill> InitLiuMoSkills()
    {
        return new List<Skill>
        {
            new Skill
            {
                skillName = "墨影斩",
                description = "快速二连击，每次造成6点伤害。",
                mpCost = 2,
                baseDamage = 6,
                breakDamage = 1,
                animationState = ANIM_ATTACK_1,
                applyElement = ElementType.None,
                auraStacks = 0,
                buffDuration = 0,
                ultChargeValue = 0 // 普通技能不回复能量
            },
            new Skill
            {
                skillName = "焰追",
                description = "施加水附着1层，触发元素反应。\n火附着+水=燃烧，每层8点持续伤害3回合。\n水附着+火=蒸发，每层15点爆发伤害。\n<color=#87CEEB>反应后恢复15-25终结技能量！</color>",
                mpCost = 4,
                baseDamage = 12,
                breakDamage = 2,
                animationState = ANIM_ATTACK_2,
                applyElement = ElementType.Water,
                auraStacks = 1,
                buffDuration = 3,
                ultChargeValue = 0 // 普通技能不回复能量，只有反应才回复
            },
            new Skill
            {
                skillName = "冰追",
                description = "施加水附着1层，触发冻结反应。\n冰附着+水=冻结，眩晕1回合+每层5点伤害。\n<color=#87CEEB>反应后恢复15终结技能量！</color>",
                mpCost = 4,
                baseDamage = 12,
                breakDamage = 2,
                animationState = ANIM_ATTACK_2,
                applyElement = ElementType.Water,
                auraStacks = 1,
                buffDuration = 3,
                ultChargeValue = 0 // 普通技能不回复能量，只有反应才回复
            },
            new Skill
            {
                skillName = "残墨",
                description = "终结技，造成20点伤害，削韧4。\n消耗目标所有元素附着，每层额外造成8点伤害。",
                mpCost = 6,
                baseDamage = 20,
                breakDamage = 4,
                animationState = ANIM_ATTACK_3,
                applyElement = ElementType.None,
                auraStacks = 0,
                buffDuration = 0,
                ultChargeValue = 0 // 普通技能不回复能量
            }
        };
    }

    /// <summary>
    /// 初始化守墨形态的技能（防御型 - 防御控制）
    /// 注意：普通技能不回复终结技能量
    /// </summary>
    public static List<Skill> InitShouMoSkills()
    {
        return new List<Skill>
        {
            new Skill
            {
                skillName = "墨壁",
                description = "获得15点护盾，持续整场战斗。\n护盾优先抵挡伤害。",
                mpCost = 3,
                baseDamage = 0,
                breakDamage = 0,
                animationState = ANIM_ATTACK_1,
                applyElement = ElementType.None,
                auraStacks = 0,
                buffDuration = 0,
                ultChargeValue = 0 // 普通技能不回复能量
            },
            new Skill
            {
                skillName = "墨缚",
                description = "施加冰附着2层，持续3回合。\n造成8点伤害，削韧3。\n触发冻结：冰+水，眩晕1回合。\n<color=#87CEEB>元素反应可恢复终结技能量！</color>",
                mpCost = 4,
                baseDamage = 8,
                breakDamage = 3,
                animationState = ANIM_ATTACK_2,
                applyElement = ElementType.Ice,
                auraStacks = 2,
                buffDuration = 3,
                ultChargeValue = 0 // 普通技能不回复能量
            },
            new Skill
            {
                skillName = "反墨",
                description = "造成10点伤害，削韧2。\n格挡成功后反击，伤害+100%。",
                mpCost = 3,
                baseDamage = 10,
                breakDamage = 2,
                animationState = ANIM_ATTACK_2,
                applyElement = ElementType.None,
                auraStacks = 0,
                buffDuration = 0,
                ultChargeValue = 0 // 普通技能不回复能量
            },
            new Skill
            {
                skillName = "墨甲",
                description = "全队获得10点护盾。\n护盾优先抵挡伤害，保护队友。",
                mpCost = 5,
                baseDamage = 0,
                breakDamage = 0,
                animationState = ANIM_ATTACK_1,
                applyElement = ElementType.None,
                auraStacks = 0,
                buffDuration = 0,
                ultChargeValue = 0 // 普通技能不回复能量
            }
        };
    }

    /// <summary>
    /// 初始化所有形态的技能列表
    /// </summary>
    public static List<List<Skill>> InitAllForms()
    {
        return new List<List<Skill>>
        {
            InitChuLinSkills(),
            InitLiuMoSkills(),
            InitShouMoSkills()
        };
    }
}
