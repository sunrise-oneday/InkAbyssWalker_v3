using UnityEngine;

[System.Serializable]
public class Skill
{
    public string skillName;       // 技能名称
    public string description;     // 技能描述
    public int mpCost;             // 消耗法力 (MP)
    public int baseDamage;         // 基础伤害值
    public int breakDamage;        // 削减破防值 (Stance Damage) [5]
    public string animationState;  // 对应的动画 State 名字

    // ========================================================
    // 元素附着系统（参考明日方舟终末地）
    // ========================================================
    [Header("元素附着设置")]
    public ElementType applyElement = ElementType.None; // 释放该技能会附着什么元素
    public int auraStacks = 1;                         // 施加附着的层数（1-4层）
    public int buffDuration = 3;                       // 附着持续几回合

    // ========================================================
    // 大招充能设置
    // ========================================================
    [Header("大招充能设置")]
    public int ultChargeValue = 15; // 每次释放该技能，大招能量 +15
}