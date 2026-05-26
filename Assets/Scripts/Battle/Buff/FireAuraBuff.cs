using UnityEngine;

public class FireAuraBuff : Buff
{
    private int burnDamage = 8; // 每回合燃烧伤害

    public FireAuraBuff(int turns)
    {
        buffName = "火元素附着";
        durationTurns = turns;
        description = "每回合开始时受到 8 点火焰灼烧伤害。";
        element = ElementType.Fire; // 标记为火元素
        icon = Resources.Load<Sprite>("UI/Buffs/Icon_Fire"); // 读取你的图标
    }

    public override void OnTurnStart()
    {
        // 回合开始时，自动执行灼烧扣血（不扣除破防值） [5]
        owner.TakeDamage(burnDamage, 0);
        Debug.Log($"{owner.gameObject.name} 受到火元素灼烧，损失了 {burnDamage} 点生命！");
    }
}