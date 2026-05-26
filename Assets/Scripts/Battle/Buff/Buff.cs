using UnityEngine;

/// <summary>
/// 未来如果想加草、雷、风等元素，直接在这里添加即可：
/// </summary>
public enum ElementType
{
    None,
    Fire,  // 火元素
    Ice,   // 冰元素
    Water  // 水元素（作为拓展演示）
}

/// <summary>
/// 状态异常/增益效果基类（纯 C# 类，无 MonoBehaviour 开销） [1]
/// </summary>
public abstract class Buff
{
    public string buffName;           // Buff 名字
    public Sprite icon;               // 对应的 UGUI 图标 [1]
    public int durationTurns;         // 持续回合数
    public string description;        // 悬停时显示的描述文本（合并自原有类）
    public ElementType element = ElementType.None; // 附着的元素类型

    // ========================================================
    // 核心新增：叠层属性（用于叠层和伤害倍率计算） [2]
    // ========================================================
    public int stacks = 1;         // 当前层数，默认是 1 层 [2]
    public int maxStacks = 3;      // 最大叠层上限（可在子类里自由修改，比如改成最大5层） [2]

    public CharacterStats owner { get; private set; } // 该 Buff 挂在谁身上

    public void Initialize(CharacterStats owner)
    {
        this.owner = owner;
    }

    // ==========================================
    // 状态生命周期钩子（子类按需重写）
    // ==========================================
    public virtual void OnApply() { }              // 刚刚挂载时触发 [1]
    public virtual void OnTurnStart() { }          // 角色回合开始时触发（例如：燃烧扣血）
    public virtual void OnTurnEnd() { }            // 角色回合结束时触发
    public virtual void OnRemove() { }             // 状态消失时触发

    /// <summary>
    /// 核心：伤害拦截器。在角色计算护甲和伤害前调用，
    /// 可用于实现“免伤盾”、“流血加深”或“易伤”等效果，彻底解耦伤害计算！ [1, 5]
    /// </summary>
    public virtual int OnBeforeTakeDamage(int rawDamage) => rawDamage;
}