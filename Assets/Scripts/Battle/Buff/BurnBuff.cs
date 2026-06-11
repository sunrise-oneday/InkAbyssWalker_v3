using UnityEngine;

/// <summary>
/// 燃烧 Buff（由火+水反应触发）
/// 每回合造成伤害，伤害基于触发时的火附着层数
/// </summary>
public class BurnBuff : Buff
{
    private int damagePerTurn;

    public BurnBuff(int turns, int burnStacks)
    {
        buffName = "燃烧";
        durationTurns = turns;
        stacks = Mathf.Clamp(burnStacks, 1, 4);
        maxStacks = 4;
        damagePerTurn = 8 * stacks; // 每层 8 点伤害
        description = $"燃烧中，每回合受到{damagePerTurn}伤害，{stacks}层";
        element = ElementType.None; // 燃烧是反应结果，不是元素附着
        icon = Resources.Load<Sprite>("UI/Buffs/Icon_Burn"); // 使用专门的燃烧图标
    }

    public override void OnTurnStart()
    {
        // 回合开始时造成燃烧伤害
        owner.TakeDamage(damagePerTurn, 0);
        Debug.Log($"<color=orange>[燃烧] {owner.gameObject.name} 受到 {damagePerTurn} 点燃烧伤害！</color>");
    }
}
