using UnityEngine;

namespace StoreAndInventory
{
    [CreateAssetMenu(fileName = "story_", menuName = "MoYuan/Item/Story")]
    public class StoryItem : ItemBase
    {
        [Header("剧情")]
        [Tooltip("持有此物品时满足的剧情/任务标识")]
        public string questFlagId;

        [TextArea]
        [Tooltip("额外剧情文本；Tooltip 有内容则显示")]
        public string extraStoryText;

        public override ItemCategory Category => ItemCategory.StoryItem;
        public override bool CanDiscard => false;

        public override int MaxStack => 1;

        protected override string FilePrefix => "story_";

        void Reset()
        {
            maxStack = 1;
            canSell = false;
        }
    }
}
