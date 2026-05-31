using System.Collections.Generic;
using UnityEngine;

namespace Battle.Enemy
{
    /// <summary>
    /// 敌人AI决策组件
    /// 基于随机权重的决策系统，让敌人行动更加灵活
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        [Header("行动配置")]
        [SerializeField] private EnemyAction[] possibleActions;      // 可能的行动列表
        [SerializeField] private EnemyAction[] lowHealthActions;     // 低血量时的特殊行动（可选）

        [Header("AI设置")]
        [SerializeField] private float lowHealthThreshold = 0.3f;    // 低血量阈值（30%以下）
        [SerializeField] private bool enableLowHealthActions = true; // 是否启用低血量特殊行动

        // 当前意图
        private EnemyIntent currentIntent;

        /// <summary>获取当前意图</summary>
        public EnemyIntent CurrentIntent => currentIntent;

        /// <summary>
        /// 决定下一个行动
        /// </summary>
        /// <param name="enemyStats">敌人属性</param>
        /// <param name="playerStats">玩家属性（可选，用于更智能的决策）</param>
        /// <returns>选择的意图</returns>
        public EnemyIntent DecideNextAction(CharacterStats enemyStats, CharacterStats playerStats = null)
        {
            if (possibleActions == null || possibleActions.Length == 0)
            {
                Debug.LogWarning("[EnemyAI] 没有配置可选行动");
                return null;
            }

            // 计算敌人血量百分比
            float healthPercentage = (float)enemyStats.currentHP / enemyStats.maxHP;

            // 选择行动池
            EnemyAction[] actionPool = possibleActions;

            // 如果启用低血量特殊行动，且血量低于阈值
            if (enableLowHealthActions && lowHealthActions != null && lowHealthActions.Length > 0
                && healthPercentage < lowHealthThreshold)
            {
                // 合并行动池
                actionPool = MergeActionPools(possibleActions, lowHealthActions);
            }

            // 计算权重并随机选择
            EnemyAction selectedAction = SelectActionByWeight(actionPool, healthPercentage);

            // 创建意图
            currentIntent = CreateIntentFromAction(selectedAction);

            Debug.Log($"[EnemyAI] {gameObject.name} 决策：血量 {healthPercentage:P0}，选择行动：{selectedAction.actionName}，意图类型：{currentIntent.type}");

            return currentIntent;
        }

        /// <summary>
        /// 根据权重随机选择行动
        /// </summary>
        private EnemyAction SelectActionByWeight(EnemyAction[] actions, float healthPercentage)
        {
            // 计算所有行动的权重
            float totalWeight = 0f;
            List<float> weights = new List<float>();

            foreach (var action in actions)
            {
                float weight = CalculateWeight(action, healthPercentage);
                weights.Add(weight);
                totalWeight += weight;
            }

            // 如果总权重为0，随机选择一个
            if (totalWeight <= 0f)
            {
                return actions[Random.Range(0, actions.Length)];
            }

            // 根据权重随机选择
            float random = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < actions.Length; i++)
            {
                cumulative += weights[i];
                if (random <= cumulative)
                {
                    return actions[i];
                }
            }

            // 理论上不会执行到这里，但作为保底
            return actions[actions.Length - 1];
        }

        /// <summary>
        /// 计算行动权重
        /// </summary>
        private float CalculateWeight(EnemyAction action, float healthPercentage)
        {
            float weight = action.baseWeight;

            // 血量低于阈值时调整权重
            if (healthPercentage < action.healthThreshold)
            {
                weight *= action.healthWeightModifier;
            }

            // 确保权重不为负数
            return Mathf.Max(weight, 0.01f);
        }

        /// <summary>
        /// 合并两个行动池
        /// </summary>
        private EnemyAction[] MergeActionPools(EnemyAction[] pool1, EnemyAction[] pool2)
        {
            EnemyAction[] merged = new EnemyAction[pool1.Length + pool2.Length];
            pool1.CopyTo(merged, 0);
            pool2.CopyTo(merged, pool1.Length);
            return merged;
        }

        /// <summary>
        /// 根据行动创建意图
        /// </summary>
        private EnemyIntent CreateIntentFromAction(EnemyAction action)
        {
            switch (action.intentType)
            {
                case EnemyIntentType.Attack:
                case EnemyIntentType.MultiAttack:
                case EnemyIntentType.SpecialAttack:
                    return EnemyIntent.CreateAttack(action, action.intentIcon);

                case EnemyIntentType.Block:
                    return EnemyIntent.CreateBlock(action, action.intentIcon);

                case EnemyIntentType.DebuffPlayer:
                    return EnemyIntent.CreateDebuff(action, action.intentIcon);

                case EnemyIntentType.Heal:
                    return EnemyIntent.CreateHeal(action, action.intentIcon);

                case EnemyIntentType.Summon:
                    return EnemyIntent.CreateSummon(action, action.intentIcon);

                case EnemyIntentType.Strengthen:
                case EnemyIntentType.BuffSelf:
                    return EnemyIntent.CreateStrengthen(action, action.intentIcon);

                default:
                    // 默认作为攻击处理
                    return EnemyIntent.CreateAttack(action, action.intentIcon);
            }
        }

        /// <summary>
        /// 获取所有可选行动
        /// </summary>
        public EnemyAction[] GetPossibleActions()
        {
            return possibleActions;
        }

        /// <summary>
        /// 设置可选行动
        /// </summary>
        public void SetPossibleActions(EnemyAction[] actions)
        {
            possibleActions = actions;
        }

        /// <summary>
        /// 初始化默认行动列表（用于测试）
        /// </summary>
        public void InitializeDefaultActions()
        {
            if (possibleActions != null && possibleActions.Length > 0) return;

            var actions = new System.Collections.Generic.List<EnemyAction>();

            // 加载默认意图图标
            Sprite attackIcon = Resources.Load<Sprite>("IntentIcons/Intent_Attack");
            Sprite blockIcon = Resources.Load<Sprite>("IntentIcons/Intent_Block");
            Sprite healIcon = Resources.Load<Sprite>("IntentIcons/Intent_Heal");
            Sprite debuffIcon = Resources.Load<Sprite>("IntentIcons/Intent_Debuff");

            // 获取攻击序列（如果有的话）
            var entity = GetComponent<EnemyBattleEntity>();
            var attackSequence = entity?.GetAttackSequence();

            // 默认攻击行动
            if (attackSequence != null)
            {
                var attack = EnemyAction.CreateAttack("攻击", attackSequence, attackIcon, 5f);
                actions.Add(attack);
            }

            // 默认格挡行动
            var block = EnemyAction.CreateBlock("格挡", 20, blockIcon, 2f, 0.4f);
            block.healthWeightModifier = 3f;
            actions.Add(block);

            // 默认回血行动
            var heal = EnemyAction.CreateHeal("恢复", 15, healIcon, 1.5f, 0.3f);
            heal.healthWeightModifier = 3f;
            actions.Add(heal);

            // 默认Debuff行动
            var debuffs = new EnemyDebuffConfig[]
            {
                new EnemyDebuffConfig { buffTypeName = "WeakenBuff", duration = 2, value = 3 }
            };
            var debuff = EnemyAction.CreateDebuff("虚弱打击", debuffs, debuffIcon, 2f);
            actions.Add(debuff);

            possibleActions = actions.ToArray();
            Debug.Log($"[EnemyAI] 自动初始化了 {actions.Count} 个默认行动");
        }
    }
}
