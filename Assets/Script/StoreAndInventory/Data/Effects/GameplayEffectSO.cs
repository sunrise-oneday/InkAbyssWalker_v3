using UnityEngine;

namespace StoreAndInventory
{
    [CreateAssetMenu(fileName = "effect_", menuName = "MoYuan/Effect/Gameplay")]
    public class GameplayEffectSO : ScriptableObject
    {
        [Header("身份")]
        public string effectId;
        public string effectTag;

        [Header("文本（本期仅展示）")]
        public string displayNameKey;

        [TextArea]
        public string descriptionKey;

        [Header("战斗执行（测试装备效果）")]
        [Tooltip("留空则匹配当前形态 availableSkills[0]")]
        public string targetSkillId;

        [Tooltip("按实际造成伤害的比例回血，0.1 = 10%")]
        public float lifestealPercent = 0.1f;
    }
}
