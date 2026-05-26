using UnityEngine;

[System.Serializable]
public class Skill
{
    public string skillName;       // 技能名称
    public int mpCost;             // 消耗法力 (MP)
    public int baseDamage;         // 基础伤害值
    public int breakDamage;        // 削减破防值 (Stance Damage) [5]
    public string animationState;  // 对应的动画 State 名字

    // ========================================================
    // 核心新增：技能附带的元素效果与持续时间
    // ========================================================
    [Header("大地图/战斗技能元素附着")]
    public ElementType applyElement = ElementType.None; // 释放该技能会附着什么元素
    public int buffDuration = 3;                       // 附着持续几回合
}