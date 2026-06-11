using UnityEngine;

namespace StoreAndInventory
{
    [CreateAssetMenu(fileName = "effect_", menuName = "MoYuan/Effect/Gameplay")]
    public class GameplayEffectSO : ScriptableObject
    {
        [Header("身份")]
        public string effectId;
        public string effectTag;

        [Header("文本")]
        public string displayNameKey;

        [TextArea]
        public string descriptionKey;

        [Header("战斗执行")]
        [Tooltip("留空则匹配当前形态 availableSkills[0]")]
        public string targetSkillId;

        // ---- 吸血 ----
        [Header("吸血")]
        [Tooltip("按实际造成伤害的比例回血，0.1 = 10%")]
        public float lifestealPercent;

        // ---- 暴击 ----
        [Header("暴击")]
        [Tooltip("暴击率加成，0.05 = 5%")]
        public float critRateBonus;
        [Tooltip("暴击伤害加成，0.3 = 额外30%暴伤")]
        public float critDamageBonus;

        // ---- 减伤 ----
        [Header("减伤")]
        [Tooltip("伤害减免百分比，0.1 = 减10%")]
        public float damageReductionPercent;

        // ---- 元素伤害加成 ----
        [Header("元素伤害加成")]
        [Tooltip("火元素伤害加成百分比")]
        public float fireDamageBonusPercent;
        [Tooltip("冰元素伤害加成百分比")]
        public float iceDamageBonusPercent;
        [Tooltip("水元素伤害加成百分比")]
        public float waterDamageBonusPercent;

        // ---- 即时效果（消耗品用） ----
        [Header("即时效果（消耗品）")]
        [Tooltip("即时恢复 HP 量")]
        public int instantHealHp;
        [Tooltip("即时恢复 MP 量")]
        public int instantHealMp;
        [Tooltip("即时获得护盾量")]
        public int instantShield;
        [Tooltip("即时大招充能")]
        public int instantUltCharge;

        // ---- 临时 Buff（消耗品用） ----
        [Header("临时 Buff（消耗品）")]
        [Tooltip("临时攻击力加成（持续 N 回合）")]
        public float tempAttackBonus;
        [Tooltip("临时防御力加成（持续 N 回合）")]
        public float tempDefenseBonus;
        [Tooltip("临时 Buff 持续回合数")]
        public int tempBuffDuration;

        // ---- 大招增伤 ----
        [Header("大招")]
        [Tooltip("大招伤害加成百分比")]
        public float ultimateDamageBonusPercent;

        // ---- 护盾 ----
        [Header("护盾")]
        [Tooltip("护盾量加成百分比，0.2 = +20%")]
        public float shieldBonusPercent;
    }
}
