using UnityEngine;
using System;

namespace Battle.Enemy
{
    /// <summary>
    /// 敌人Debuff配置
    /// </summary>
    [System.Serializable]
    public class EnemyDebuffConfig
    {
        public string buffTypeName;     // Buff类名（如"ArmorBreakBuff"、"WeakenBuff"等）
        public int duration;            // 持续回合数
        public int value;               // 效果值（如伤害值、降低值等）
        public Sprite icon;             // Debuff图标
    }

    /// <summary>
    /// 敌人行动配置
    /// 定义敌人可能执行的各种行动
    /// </summary>
    [System.Serializable]
    public class EnemyAction
    {
        [Header("基本信息")]
        public string actionName;               // 行动名称
        public EnemyIntentType intentType;       // 意图类型
        public Sprite intentIcon;                // 意图图标
        public Sprite debuffIcon;                // Debuff图标（如果施加debuff）

        [Header("攻击相关")]
        public EnemyAttackSequence attackSequence;  // 攻击序列（如果是攻击类型）

        [Header("护盾相关")]
        public int shieldAmount;                // 获得的护盾值（如果是格挡类型）

        [Header("Debuff相关")]
        public EnemyDebuffConfig[] debuffs;     // 施加的debuff列表（如果是debuff类型）

        [Header("回血相关")]
        public int healAmount;                  // 回血量（如果是回血类型）

        [Header("强化相关")]
        public int strengthenAmount;            // 强化值（如果是强化类型）
        public string strengthenType;           // 强化类型："attack"或"defense"

        [Header("召唤相关")]
        public GameObject summonPrefab;         // 召唤物预制件
        public int summonCount;                 // 召唤数量

        [Header("AI权重")]
        public float baseWeight = 1f;           // 基础权重
        public float healthThreshold = 0.5f;    // 血量阈值（低于此值时权重变化）
        public float healthWeightModifier = 1f; // 血量低于阈值时的权重修正

        /// <summary>
        /// 创建攻击行动
        /// </summary>
        public static EnemyAction CreateAttack(string name, EnemyAttackSequence sequence, Sprite icon, float weight = 1f)
        {
            return new EnemyAction
            {
                actionName = name,
                intentType = EnemyIntentType.Attack,
                intentIcon = icon,
                attackSequence = sequence,
                baseWeight = weight
            };
        }

        /// <summary>
        /// 创建格挡行动
        /// </summary>
        public static EnemyAction CreateBlock(string name, int shield, Sprite icon, float weight = 1f, float healthThreshold = 0.5f)
        {
            return new EnemyAction
            {
                actionName = name,
                intentType = EnemyIntentType.Block,
                intentIcon = icon,
                shieldAmount = shield,
                baseWeight = weight,
                healthThreshold = healthThreshold,
                healthWeightModifier = 2f // 低血量时更倾向于格挡
            };
        }

        /// <summary>
        /// 创建Debuff行动
        /// </summary>
        public static EnemyAction CreateDebuff(string name, EnemyDebuffConfig[] debuffs, Sprite icon, float weight = 1f)
        {
            return new EnemyAction
            {
                actionName = name,
                intentType = EnemyIntentType.DebuffPlayer,
                intentIcon = icon,
                debuffs = debuffs,
                baseWeight = weight
            };
        }

        /// <summary>
        /// 创建回血行动
        /// </summary>
        public static EnemyAction CreateHeal(string name, int heal, Sprite icon, float weight = 1f, float healthThreshold = 0.5f)
        {
            return new EnemyAction
            {
                actionName = name,
                intentType = EnemyIntentType.Heal,
                intentIcon = icon,
                healAmount = heal,
                baseWeight = weight,
                healthThreshold = healthThreshold,
                healthWeightModifier = 3f // 低血量时更倾向于回血
            };
        }

        /// <summary>
        /// 创建召唤行动
        /// </summary>
        public static EnemyAction CreateSummon(string name, GameObject prefab, int count, Sprite icon, float weight = 1f)
        {
            return new EnemyAction
            {
                actionName = name,
                intentType = EnemyIntentType.Summon,
                intentIcon = icon,
                summonPrefab = prefab,
                summonCount = count,
                baseWeight = weight
            };
        }

        /// <summary>
        /// 创建强化行动
        /// </summary>
        public static EnemyAction CreateStrengthen(string name, int amount, string type, Sprite icon, float weight = 1f)
        {
            return new EnemyAction
            {
                actionName = name,
                intentType = EnemyIntentType.Strengthen,
                intentIcon = icon,
                strengthenAmount = amount,
                strengthenType = type,
                baseWeight = weight
            };
        }
    }
}
