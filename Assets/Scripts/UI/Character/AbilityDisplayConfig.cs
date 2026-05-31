using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 探索技能显示配置（ScriptableObject）
/// 将每个 ExplorationAbility 枚举值映射到可显示的 UI 数据（名称、描述、图标）
/// </summary>
[CreateAssetMenu(fileName = "AbilityDisplayConfig", menuName = "InkAbyss/Ability Display Config")]
public class AbilityDisplayConfig : ScriptableObject
{
    [System.Serializable]
    public class AbilityDisplayEntry
    {
        public ExplorationAbility ability;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
    }

    public List<AbilityDisplayEntry> entries = new List<AbilityDisplayEntry>();

    public AbilityDisplayEntry GetEntry(ExplorationAbility ability)
    {
        return entries.Find(e => e.ability == ability);
    }
}
