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

    public enum StatType
    {
        Attack,
        MaxHp,
        Defense,
        Speed,
        CritRate
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
