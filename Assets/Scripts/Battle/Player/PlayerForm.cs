using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerForm
{
    public string formName;            // 形态名称（例如：狂战士、冰魔法师）
    public int apCostToSwitch = 1;     // 切换到该形态需要消耗的行动点 (AP)
    public static readonly int Anim_FormChange = Animator.StringToHash("Player_FormChange"); // 变身动画

    public List<Skill> availableSkills; // 该形态下可用的技能列表
}