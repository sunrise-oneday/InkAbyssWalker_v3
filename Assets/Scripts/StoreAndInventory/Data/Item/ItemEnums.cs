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
        // ---- 基础五维 ----
        Attack,
        MaxHp,
        Defense,
        MaxMp,
        MaxBreak,
        // ---- 战斗扩展属性 ----
        CritRate,           // 暴击率 (0.05 = 5%)
        CritDamage,         // 暴击伤害倍率 (0.3 = 额外30%)
        DamageReduction,    // 伤害减免百分比 (0.1 = 10%)
        FireDamageBonus,    // 火元素伤害加成
        IceDamageBonus,     // 冰元素伤害加成
        WaterDamageBonus,   // 水元素伤害加成
        ShieldBonus,        // 护盾量加成百分比
        HealBonus,          // 治疗量加成百分比
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
        CooldownReduce,
        // ---- 新增 ----
        MpCostReduce,       // MP 消耗减少（固定值）
        DamageBonusLowHp,   // 目标低血量时伤害加成（百分比）
        ShieldOnHit,        // 命中时获得护盾（固定值）
        UltChargeBonus,     // 大招充能额外加成（百分比）
    }

    /// <summary>
    /// 商店顶部筛选标签（按效果类型筛选）。策划在 ItemBase Inspector 中勾选。
    /// </summary>
    [System.Flags]
    public enum ShopTag
    {
        None    = 0,
        AP      = 1 << 0,  // 回复/增强 AP
        MP      = 1 << 1,  // 回复/增强 MP
        HP      = 1 << 2,  // 回复/增强 HP
        KO      = 1 << 3,  // 回复大招能量
        Crit    = 1 << 4,  // 暴击相关
        Def     = 1 << 5,  // 防御/减伤相关
        Elem    = 1 << 6,  // 元素相关
        Special = 1 << 7,  // 特殊效果
    }
}
