using UnityEngine;

public class IceAuraBuff : Buff
{
    private int defenseReduction = 4; // 减防数值

    public IceAuraBuff(int turns)
    {
        buffName = "冰元素附着";
        durationTurns = turns;
        description = "身体被严寒冻结，物理防御力降低 4 点。";
        element = ElementType.Ice; // 标记为冰元素
        icon = Resources.Load<Sprite>("UI/Buffs/Icon_Ice");
    }

    public override void OnApply()
    {
        // 挂载时，降低角色的物理防御力
        owner.defense = Mathf.Max(owner.defense - defenseReduction, 0);
        Debug.Log($"{owner.gameObject.name} 身体被冻僵，防御力降低了 {defenseReduction} 点！");
    }

    public override void OnRemove()
    {
        // 状态消失时，必须将削减的防御力还回去！这也是 OnRemove 的核心作用
        owner.defense += defenseReduction;
    }
}