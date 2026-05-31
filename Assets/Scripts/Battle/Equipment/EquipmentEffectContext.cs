/// <summary>
/// 装备特殊效果执行上下文（测试：技能1 吸血）。
/// </summary>
public readonly struct EquipmentEffectContext
{
    public readonly PlayerBattleEntity dealer;
    public readonly CharacterStats attackerStats;
    public readonly Skill skill;
    public readonly int finalDamageDealt;
    public readonly int currentTurn;

    public EquipmentEffectContext(
        PlayerBattleEntity dealer,
        CharacterStats attackerStats,
        Skill skill,
        int finalDamageDealt,
        int currentTurn)
    {
        this.dealer = dealer;
        this.attackerStats = attackerStats;
        this.skill = skill;
        this.finalDamageDealt = finalDamageDealt;
        this.currentTurn = currentTurn;
    }
}
