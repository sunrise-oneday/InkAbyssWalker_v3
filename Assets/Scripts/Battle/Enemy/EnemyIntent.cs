using UnityEngine;

namespace Battle.Enemy
{
    /// <summary>
    /// 敌人意图类型
    /// </summary>
    public enum EnemyIntentType
    {
        Attack,         // 攻击
        Block,          // 格挡（获得护盾）
        BuffSelf,       // 增益自己
        DebuffPlayer,   // 减益玩家
        MultiAttack,    // 多段攻击
        Heal,           // 回血
        Summon,         // 召唤
        Strengthen,     // 强化（提升攻击/防御）
        SpecialAttack,  // 特殊攻击（如蓄力攻击）
        Unknown         // 未知意图（需要特定技能才能揭示）
    }

    /// <summary>
    /// 敌人意图数据
    /// 定义敌人下一次行动的类型和相关信息
    /// </summary>
    [System.Serializable]
    public class EnemyIntent
    {
        [Header("意图基本信息")]
        public EnemyIntentType type;        // 意图类型
        public Sprite icon;                 // 意图图标
        public string description;          // 意图描述（可选）

        [Header("数值信息")]
        public int value;                   // 数值（伤害/护盾量/buff效果值）
        public int hitCount;                // 攻击次数（仅攻击类型）

        [Header("关联行动")]
        public EnemyAction action;          // 关联的行动配置

        /// <summary>
        /// 创建攻击意图
        /// </summary>
        public static EnemyIntent CreateAttack(EnemyAction action, Sprite icon)
        {
            var intent = new EnemyIntent
            {
                type = EnemyIntentType.Attack,
                icon = icon,
                value = action.attackSequence.hitDamages[0],
                hitCount = action.attackSequence.hitAnimations.Length,
                action = action
            };
            return intent;
        }

        /// <summary>
        /// 创建格挡意图
        /// </summary>
        public static EnemyIntent CreateBlock(EnemyAction action, Sprite icon)
        {
            var intent = new EnemyIntent
            {
                type = EnemyIntentType.Block,
                icon = icon,
                value = action.shieldAmount,
                action = action
            };
            return intent;
        }

        /// <summary>
        /// 创建Debuff意图
        /// </summary>
        public static EnemyIntent CreateDebuff(EnemyAction action, Sprite icon)
        {
            var intent = new EnemyIntent
            {
                type = EnemyIntentType.DebuffPlayer,
                icon = icon,
                action = action
            };
            return intent;
        }

        /// <summary>
        /// 创建回血意图
        /// </summary>
        public static EnemyIntent CreateHeal(EnemyAction action, Sprite icon)
        {
            var intent = new EnemyIntent
            {
                type = EnemyIntentType.Heal,
                icon = icon,
                value = action.healAmount,
                action = action
            };
            return intent;
        }

        /// <summary>
        /// 创建召唤意图
        /// </summary>
        public static EnemyIntent CreateSummon(EnemyAction action, Sprite icon)
        {
            var intent = new EnemyIntent
            {
                type = EnemyIntentType.Summon,
                icon = icon,
                action = action
            };
            return intent;
        }

        /// <summary>
        /// 创建强化意图
        /// </summary>
        public static EnemyIntent CreateStrengthen(EnemyAction action, Sprite icon)
        {
            var intent = new EnemyIntent
            {
                type = EnemyIntentType.Strengthen,
                icon = icon,
                value = action.strengthenAmount,
                action = action
            };
            return intent;
        }

        /// <summary>
        /// 创建特殊攻击意图
        /// </summary>
        public static EnemyIntent CreateSpecialAttack(EnemyAction action, Sprite icon)
        {
            var intent = new EnemyIntent
            {
                type = EnemyIntentType.SpecialAttack,
                icon = icon,
                value = action.attackSequence.hitDamages[0],
                hitCount = action.attackSequence.hitAnimations.Length,
                action = action
            };
            return intent;
        }

        /// <summary>
        /// 创建未知意图（玩家需要使用特定技能才能揭示）
        /// </summary>
        public static EnemyIntent CreateUnknown(Sprite unknownIcon)
        {
            var intent = new EnemyIntent
            {
                type = EnemyIntentType.Unknown,
                icon = unknownIcon,
                description = "???"
            };
            return intent;
        }
    }
}
