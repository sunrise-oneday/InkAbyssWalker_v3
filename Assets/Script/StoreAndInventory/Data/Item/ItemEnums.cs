namespace StoreAndInventory
{
    public enum ItemCategory
    {
        Equipment,
        Consumable,
        StoryItem
    }

    public enum ItemRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public enum UseContext
    {
        Exploration,
        Battle,
        Any
    }

    /// <summary>
    /// 与主工程 <see cref="CharacterStats"/> 基础上限字段一一对应（不含 current* / isBroken 等运行时状态）。
    /// </summary>
    public enum StatType
    {
        Attack,
        MaxHp,
        Defense,
        MaxMp,
        MaxBreak
    }

    public enum SkillModTarget
    {
        SkillId,
        CardId,
        SkillTag
    }

    public enum SkillModType
    {
        ProjectileCount,
        AreaScale,
        DamageBonus,
        CooldownReduce
    }
}
