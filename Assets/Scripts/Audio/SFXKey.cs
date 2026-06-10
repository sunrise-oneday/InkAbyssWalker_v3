/// <summary>
/// 战斗音效键。显式赋值 + 类别步长 100，保证序列化后增删不导致数据错乱。
/// </summary>
public enum SFXKey
{
    None = 0,

    // ---- 格挡 (100+) ----
    PerfectParry = 100,
    NormalParry  = 101,
    ParryFailed  = 102,

    // ---- 闪避 (200+) ----
    PerfectDodge = 200,  // P1 启用
    NormalDodge  = 201,  // P1 启用

    // ---- 攻击 (300+) ----
    SkillCast    = 300,
    UltimateCast = 301,

    // ---- 击杀 (400+) ----
    EnemyDeath = 400,

    // ---- 流程 (500+) ----
    Victory = 500,
    Defeat  = 501
}
