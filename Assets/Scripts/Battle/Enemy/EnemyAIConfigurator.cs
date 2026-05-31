using UnityEngine;

namespace Battle.Enemy
{
    /// <summary>
    /// 敌人AI配置器
    /// 用于在Inspector中配置敌人的AI行为
    /// 可以作为示例或直接使用
    /// </summary>
    public class EnemyAIConfigurator : MonoBehaviour
    {
        [Header("AI组件引用")]
        [SerializeField] private EnemyAI enemyAI;

        [Header("攻击行动配置")]
        [SerializeField] private EnemyAttackSequence attackSequence1;  // 攻击序列1
        [SerializeField] private EnemyAttackSequence attackSequence2;  // 攻击序列2（可选）
        [SerializeField] private Sprite attackIcon;                     // 攻击意图图标

        [Header("格挡行动配置")]
        [SerializeField] private int blockAmount = 20;                 // 格挡获得的护盾值
        [SerializeField] private Sprite blockIcon;                     // 格挡意图图标

        [Header("Debuff行动配置")]
        [SerializeField] private Sprite debuffIcon;                    // Debuff意图图标
        [SerializeField] private string[] debuffNames;                 // Debuff名称列表
        [SerializeField] private int[] debuffDurations;                // Debuff持续时间
        [SerializeField] private int[] debuffValues;                   // Debuff效果值

        [Header("AI权重配置")]
        [SerializeField] private float attackWeight = 5f;              // 攻击权重
        [SerializeField] private float blockWeight = 2f;               // 格挡权重
        [SerializeField] private float debuffWeight = 2f;              // Debuff权重
        [SerializeField] private float lowHealthThreshold = 0.3f;      // 低血量阈值
        [SerializeField] private float lowHealthBlockModifier = 3f;    // 低血量时格挡权重修正

        private void Start()
        {
            ConfigureAI();
        }

        /// <summary>
        /// 配置AI
        /// </summary>
        public void ConfigureAI()
        {
            if (enemyAI == null)
            {
                enemyAI = GetComponent<EnemyAI>();
                if (enemyAI == null)
                {
                    Debug.LogError("[EnemyAIConfigurator] 未找到EnemyAI组件");
                    return;
                }
            }

            // 创建行动列表
            var actions = new System.Collections.Generic.List<EnemyAction>();

            // 1. 攻击行动
            if (attackSequence1 != null)
            {
                var attackAction = EnemyAction.CreateAttack(
                    "普通攻击",
                    attackSequence1,
                    attackIcon,
                    attackWeight
                );
                actions.Add(attackAction);
            }

            // 2. 第二种攻击（如果有）
            if (attackSequence2 != null)
            {
                var attackAction2 = EnemyAction.CreateAttack(
                    "强力攻击",
                    attackSequence2,
                    attackIcon,
                    attackWeight * 0.7f  // 权重略低
                );
                actions.Add(attackAction2);
            }

            // 3. 格挡行动
            var blockAction = EnemyAction.CreateBlock(
                "防御",
                blockAmount,
                blockIcon,
                blockWeight,
                lowHealthThreshold
            );
            blockAction.healthWeightModifier = lowHealthBlockModifier;
            actions.Add(blockAction);

            // 4. Debuff行动
            if (debuffNames != null && debuffNames.Length > 0)
            {
                var debuffs = new EnemyDebuffConfig[debuffNames.Length];
                for (int i = 0; i < debuffNames.Length; i++)
                {
                    debuffs[i] = new EnemyDebuffConfig
                    {
                        buffTypeName = debuffNames[i],
                        duration = i < debuffDurations.Length ? debuffDurations[i] : 2,
                        value = i < debuffValues.Length ? debuffValues[i] : 5
                    };
                }

                var debuffAction = EnemyAction.CreateDebuff(
                    "施加负面效果",
                    debuffs,
                    debuffIcon,
                    debuffWeight
                );
                actions.Add(debuffAction);
            }

            // 设置AI的行动列表
            enemyAI.SetPossibleActions(actions.ToArray());

            Debug.Log($"[EnemyAIConfigurator] AI配置完成，共 {actions.Count} 个行动");
        }
    }
}
