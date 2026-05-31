using UnityEngine;

namespace Battle.Enemy
{
    /// <summary>
    /// 敌人AI类型配置
    /// 提供预设的AI行为模式
    /// </summary>
    public enum EnemyAIType
    {
        Aggressive,  // 攻击型：偏好攻击，高伤害
        Defensive,   // 防御型：偏好格挡和回血，注重生存
        Supportive,  // 辅助型：偏好施加debuff和强化
        Balanced     // 均衡型：各种行动均衡
    }

    /// <summary>
    /// 敌人AI类型配置器
    /// 根据预设类型自动配置AI参数
    /// </summary>
    public class EnemyAITypes : MonoBehaviour
    {
        [Header("AI类型")]
        [SerializeField] private EnemyAIType aiType = EnemyAIType.Balanced;

        [Header("组件引用")]
        [SerializeField] private EnemyAI enemyAI;
        [SerializeField] private EnemyAttackSequence attackSequence1;
        [SerializeField] private EnemyAttackSequence attackSequence2;

        [Header("图标")]
        [SerializeField] private Sprite attackIcon;
        [SerializeField] private Sprite blockIcon;
        [SerializeField] private Sprite debuffIcon;
        [SerializeField] private Sprite healIcon;
        [SerializeField] private Sprite strengthenIcon;

        private void Start()
        {
            ConfigureByType();
        }

        /// <summary>
        /// 根据AI类型配置参数
        /// </summary>
        public void ConfigureByType()
        {
            if (enemyAI == null)
            {
                enemyAI = GetComponent<EnemyAI>();
                if (enemyAI == null)
                {
                    Debug.LogError("[EnemyAITypes] 未找到EnemyAI组件");
                    return;
                }
            }

            switch (aiType)
            {
                case EnemyAIType.Aggressive:
                    ConfigureAggressive();
                    break;
                case EnemyAIType.Defensive:
                    ConfigureDefensive();
                    break;
                case EnemyAIType.Supportive:
                    ConfigureSupportive();
                    break;
                case EnemyAIType.Balanced:
                default:
                    ConfigureBalanced();
                    break;
            }

            Debug.Log($"[EnemyAITypes] 配置完成，AI类型：{aiType}");
        }

        /// <summary>
        /// 攻击型配置
        /// 特点：高攻击权重，低血量时更激进
        /// </summary>
        private void ConfigureAggressive()
        {
            var actions = new System.Collections.Generic.List<EnemyAction>();

            // 主要攻击
            if (attackSequence1 != null)
            {
                var attack1 = EnemyAction.CreateAttack("猛攻", attackSequence1, attackIcon, 8f);
                actions.Add(attack1);
            }

            // 第二种攻击
            if (attackSequence2 != null)
            {
                var attack2 = EnemyAction.CreateAttack("重击", attackSequence2, attackIcon, 5f);
                attack2.healthThreshold = 0.5f;
                attack2.healthWeightModifier = 1.5f; // 低血量时更倾向于重击
                actions.Add(attack2);
            }

            // 格挡（较少）
            var block = EnemyAction.CreateBlock("防御", 15, blockIcon, 1.5f, 0.3f);
            block.healthWeightModifier = 2f;
            actions.Add(block);

            // Debuff
            var debuffs = new EnemyDebuffConfig[]
            {
                new EnemyDebuffConfig { buffTypeName = "Weaken", duration = 2, value = 5 }
            };
            var debuff = EnemyAction.CreateDebuff("虚弱打击", debuffs, debuffIcon, 2f);
            actions.Add(debuff);

            enemyAI.SetPossibleActions(actions.ToArray());
        }

        /// <summary>
        /// 防御型配置
        /// 特点：高格挡和回血权重，注重生存
        /// </summary>
        private void ConfigureDefensive()
        {
            var actions = new System.Collections.Generic.List<EnemyAction>();

            // 攻击（较少）
            if (attackSequence1 != null)
            {
                var attack = EnemyAction.CreateAttack("反击", attackSequence1, attackIcon, 3f);
                actions.Add(attack);
            }

            // 格挡（主要）
            var block = EnemyAction.CreateBlock("铁壁防御", 30, blockIcon, 6f, 0.5f);
            block.healthWeightModifier = 3f; // 低血量时更倾向于格挡
            actions.Add(block);

            // 回血
            var heal = EnemyAction.CreateHeal("生命恢复", 25, healIcon, 4f, 0.4f);
            heal.healthWeightModifier = 4f; // 低血量时更倾向于回血
            actions.Add(heal);

            // 强化防御
            var strengthen = EnemyAction.CreateStrengthen("硬化", 5, "defense", strengthenIcon, 2f);
            actions.Add(strengthen);

            enemyAI.SetPossibleActions(actions.ToArray());
        }

        /// <summary>
        /// 辅助型配置
        /// 特点：偏好施加debuff和强化自己
        /// </summary>
        private void ConfigureSupportive()
        {
            var actions = new System.Collections.Generic.List<EnemyAction>();

            // 攻击（较少）
            if (attackSequence1 != null)
            {
                var attack = EnemyAction.CreateAttack("诅咒攻击", attackSequence1, attackIcon, 3f);
                actions.Add(attack);
            }

            // 施加多种debuff
            var debuff1 = EnemyAction.CreateDebuff("破甲诅咒",
                new EnemyDebuffConfig[] { new EnemyDebuffConfig { buffTypeName = "ArmorBreak", duration = 3, value = 8 } },
                debuffIcon, 4f);
            actions.Add(debuff1);

            var debuff2 = EnemyAction.CreateDebuff("虚弱诅咒",
                new EnemyDebuffConfig[] { new EnemyDebuffConfig { buffTypeName = "Weaken", duration = 2, value = 6 } },
                debuffIcon, 3f);
            actions.Add(debuff2);

            var debuff3 = EnemyAction.CreateDebuff("中毒诅咒",
                new EnemyDebuffConfig[] { new EnemyDebuffConfig { buffTypeName = "Poison", duration = 4, value = 5 } },
                debuffIcon, 3.5f);
            actions.Add(debuff3);

            // 强化自己
            var strengthen = EnemyAction.CreateStrengthen("黑暗强化", 8, "attack", strengthenIcon, 2.5f);
            actions.Add(strengthen);

            enemyAI.SetPossibleActions(actions.ToArray());
        }

        /// <summary>
        /// 均衡型配置
        /// 特点：各种行动均衡
        /// </summary>
        private void ConfigureBalanced()
        {
            var actions = new System.Collections.Generic.List<EnemyAction>();

            // 攻击
            if (attackSequence1 != null)
            {
                var attack1 = EnemyAction.CreateAttack("普通攻击", attackSequence1, attackIcon, 5f);
                actions.Add(attack1);
            }

            if (attackSequence2 != null)
            {
                var attack2 = EnemyAction.CreateAttack("强力攻击", attackSequence2, attackIcon, 3f);
                actions.Add(attack2);
            }

            // 格挡
            var block = EnemyAction.CreateBlock("防御", 20, blockIcon, 2.5f, 0.4f);
            block.healthWeightModifier = 2.5f;
            actions.Add(block);

            // Debuff
            var debuffs = new EnemyDebuffConfig[]
            {
                new EnemyDebuffConfig { buffTypeName = "Weaken", duration = 2, value = 4 }
            };
            var debuff = EnemyAction.CreateDebuff("虚弱打击", debuffs, debuffIcon, 2f);
            actions.Add(debuff);

            // 回血
            var heal = EnemyAction.CreateHeal("恢复", 20, healIcon, 1.5f, 0.3f);
            heal.healthWeightModifier = 3f;
            actions.Add(heal);

            enemyAI.SetPossibleActions(actions.ToArray());
        }
    }
}
