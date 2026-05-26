using UnityEngine;

[System.Serializable]
public class EnemyAttackSequence
{
    public string attackName = "三连暴风斩";

    [Tooltip("每一击落下的精确时间（从进入格挡状态开始计时，单位：秒）")]
    public float[] hitTimings = { 0.8f, 1.4f, 2.0f };

    [Tooltip("每一击对应的原始伤害")]
    public int[] hitDamages = { 15, 15, 30 };

    [Tooltip("每一击对玩家施加的削韧值（如果玩家格挡失败，会削减玩家的破防条） [6]")]
    public int[] hitBreakDamages = { 5, 5, 12 };

    // ========================================================
    // 核心新增：每一击对应的独立动画名字（支持招式变招，如左砍、右砍、下重劈） [6]
    // 动作名字必须与 Unity Animator 里的方块名字 100% 一致！
    // ========================================================
    [Tooltip("每一击对应的动画名字（必须与动画机里的方块名字完全一致）")]
    public string[] hitAnimations = { "Enemy_Attack_1", "Enemy_Attack_2", "Enemy_Attack_3" };
}