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
    }
}
