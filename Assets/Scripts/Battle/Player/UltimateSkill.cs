using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class UltimateSkill
{
    public string ultimateName;         // 大招名称
    public string animationState;       // 大招对应的动画 State 名字（例如 Player_Ultimate_Laser）
    public string description;
    public float duration = 1.6f;       // 该大招的完整动画时长（秒）
    public float hitProgress = 0.5f;     // 伤害落点进度（0~1，例如 0.5 代表动画播放到 50% 进度时发生落地伤害判定）

    public enum UltimateType
    {
        SingleTarget, // 单体爆发（针对当前锁定的目标造成巨额伤害）
        AoE,          // 群体轰炸（对战场上所有存活的怪物造成范围伤害）
        Control       // 强力控制（高额破防，并对目标施加“眩晕”状态，使其下回合直接罚站跳过回合）
    }

    public UltimateType ultimateType;

    [Header("数值配置")]
    public int baseDamage = 120;
    public int breakDamage = 60;
    public int stunTurns = 1; // 仅对控制型大招有效，眩晕几回合
}