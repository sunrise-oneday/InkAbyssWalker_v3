using System;
using System.Collections.Generic;

namespace StoreAndInventory
{
    [Serializable]
    public class RuneSlotEntry
    {
        /// <summary>槽位标识：技能名（如"墨刺"）或"ultimate"表示大招槽。</summary>
        public string slotId;
        public string instanceGuid;
    }

    [Serializable]
    public class EquipmentData
    {
        public List<RuneSlotEntry> runes = new();
    }

    [Serializable]
    public class EquippedCacheEntry
    {
        public string instanceGuid;
        public ItemStack stack;
    }

    [Serializable]
    public class EquippedCacheData
    {
        public List<EquippedCacheEntry> entries = new();
    }

    /// <summary>
    /// 预定义的技能符文槽配置。每个槽位绑定一个技能或大招。
    /// </summary>
    public static class SkillRuneSlotConfig
    {
        public const string UltimateSlotId = "ultimate";

        /// <summary>所有技能槽 ID（12 个技能 + 1 个大招 = 13 槽）。</summary>
        public static readonly string[] AllSlotIds =
        {
            // 初临形态
            "墨刺", "炎墨", "寒墨", "墨涌",
            // 流墨形态
            "墨影斩", "焰追", "冰追", "残墨",
            // 守墨形态
            "墨壁", "墨缚", "反墨", "墨甲",
            // 大招
            UltimateSlotId
        };

        /// <summary>每个形态包含的技能槽 ID。</summary>
        public static readonly string[][] FormSlotIds =
        {
            new[] { "墨刺", "炎墨", "寒墨", "墨涌" },
            new[] { "墨影斩", "焰追", "冰追", "残墨" },
            new[] { "墨壁", "墨缚", "反墨", "墨甲" },
        };

        /// <summary>形态名称。</summary>
        public static readonly string[] FormNames = { "初临形态", "流墨形态", "守墨形态" };
    }
}
