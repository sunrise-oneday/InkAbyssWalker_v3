using System.Collections.Generic;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("基础生命与法力")]
    public int maxHP = 100;
    public int currentHP;
    public int maxMP = 50;
    public int currentMP;

    [Header("基础战斗属性")]
    public int attack = 15;
    public int defense = 5;

    [Header("破防条 (Break Bar)")]
    public int maxBreakValue = 50;          // 最大破防上限（白色破防条） [5]
    public int currentBreakValue;           // 当前破防值 [5]
    public bool isBroken = false;           // 当前是否处于破防状态 [5]
    public float breakDamageMultiplier = 1.5f; // 破防状态下，受到的伤害倍率 [5]

    [Header("Buff 异常状态容器")]
    // 核心修正：使用合并重构后的统一 Buff 类型列表，彻底消除隐式转换报错 [1]
    public List<Buff> activeBuffs = new List<Buff>();

    public System.Action OnBuffsChanged;
    public System.Action OnHPChanged;
    public System.Action OnBreakChanged;

    private void Awake()
    {
        currentHP = maxHP;
        currentMP = maxMP;
        currentBreakValue = maxBreakValue;
    }

    /// <summary>
    /// 接收伤害与破防值的计算 [1, 5]
    /// </summary>
    /// <summary>战斗治疗，不超过 maxHP。</summary>
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        OnHPChanged?.Invoke();
    }

    /// <summary>返回本次实际扣血 finalDamage（供装备吸血等）。</summary>
    public int TakeDamage(int rawDamage, int breakDamage)
    {
        // 1. 调用所有活跃 Buff 的伤害拦截器 [1, 5]
        int processedDamage = rawDamage;
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            processedDamage = activeBuffs[i].OnBeforeTakeDamage(processedDamage);
        }

        float finalMultiplier = isBroken ? breakDamageMultiplier : 1.0f;
        int finalDamage = Mathf.Max(Mathf.RoundToInt((processedDamage - defense) * finalMultiplier), 1);
        currentHP = Mathf.Max(currentHP - finalDamage, 0);

        OnHPChanged?.Invoke();

        if (!isBroken)
        {
            currentBreakValue = Mathf.Max(currentBreakValue - breakDamage, 0);
            OnBreakChanged?.Invoke();

            if (currentBreakValue <= 0)
            {
                TriggerBreak();
            }
        }

        return finalDamage;
    }

    private void TriggerBreak()
    {
        isBroken = true;
        OnBreakChanged?.Invoke();
        Debug.Log($"<color=red>★★ [破防！] {gameObject.name} 被强行破防！ ★★</color>");
    }

    public void RecoverFromBreak()
    {
        if (isBroken)
        {
            isBroken = false;
            currentBreakValue = maxBreakValue;
            OnBreakChanged?.Invoke();
            Debug.Log($"[破防恢复] {gameObject.name} 恢复了架势，破防条重新回满。");
        }
    }

    public bool ConsumeMP(int amount)
    {
        if (currentMP >= amount)
        {
            currentMP -= amount;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 为角色附加状态，并在底层自动进行元素反应判定 [1, 3]
    /// </summary>
    public void AddBuff(Buff newBuff)
    {
        if (newBuff == null) return;

        // 1. 进行元素反应解离判断 [1, 3]
        if (CheckElementalReaction(newBuff))
        {
            return;
        }

        // ========================================================
        // 2. 核心重构：同名 Buff 拦截（刷新回合数、叠加层数，防止图标重复生成） [2]
        // ========================================================
        Buff existingBuff = activeBuffs.Find(b => b.buffName == newBuff.buffName);
        if (existingBuff != null)
        {
            // 刷新并累加回合数（设定最大上限 5 回合）
            existingBuff.durationTurns = Mathf.Min(existingBuff.durationTurns + newBuff.durationTurns, 5);

            // 递增当前层数（设定最大叠层上限） [2]
            existingBuff.stacks = Mathf.Min(existingBuff.stacks + 1, existingBuff.maxStacks);

            OnBuffsChanged?.Invoke(); // 触发刷新（会通知 UI 更新数字）
            return; // 拦截成功，不再生成新的重复图标！
        }

        // 3. 身上没有同名 Buff，正常挂载
        newBuff.Initialize(this);
        activeBuffs.Add(newBuff);
        newBuff.OnApply();

        OnBuffsChanged?.Invoke();
    }

    /// <summary>
    /// 检测新附着的元素是否与已有元素发生化学反应 [1, 3]
    /// </summary>
    private bool CheckElementalReaction(Buff incomingBuff)
    {
        if (incomingBuff.element == ElementType.None) return false;

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            Buff activeBuff = activeBuffs[i];

            if (activeBuff.element != ElementType.None && activeBuff.element != incomingBuff.element)
            {
                // 核心：将当前附着状态（包含它的层数）喂给反应结算器！
                TriggerReaction(activeBuff, incomingBuff.element);

                activeBuff.OnRemove();
                activeBuffs.RemoveAt(i);

                OnBuffsChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 反应结算器：层数越高，威力越强！ [3]
    /// </summary>
    private void TriggerReaction(Buff activeBuff, ElementType incomingElement)
    {
        ElementType activeElement = activeBuff.element;
        int stacks = activeBuff.stacks; // 提取被引爆状态的当前叠层数！

        // 反应：火 + 冰 = 融化
        if ((activeElement == ElementType.Fire && incomingElement == ElementType.Ice) ||
            (activeElement == ElementType.Ice && incomingElement == ElementType.Fire))
        {
            // ========================================================
            // 核心修改（数值跃升）：反应伤害与破防值，直接乘以被引爆的元素层数！
            // 层数越高，反应威力越恐怖！ (1层=30破防, 3层=90破防直接干碎！) [5, 6]
            // ========================================================
            int finalBreakDamage = 30 * stacks;
            int rawDamage = 15 * stacks;

            Debug.Log($"<color=orange>★★ [元素反应：融化！] 叠层 x{stacks} 爆发！对 {gameObject.name} 造成 {rawDamage} 伤害和 {finalBreakDamage} 破防！ ★★</color>");
            TakeDamage(rawDamage, finalBreakDamage);

            // 附带的易伤倍率也随层数递增 [1, 5]
            AddBuff(new VulnerabilityBuff(2, 1.2f + (0.1f * stacks)));
        }

        // 反应：水 + 火 = 蒸发
        if ((activeElement == ElementType.Water && incomingElement == ElementType.Fire) ||
            (activeElement == ElementType.Fire && incomingElement == ElementType.Water))
        {
            int rawDamage = 40 * stacks;
            Debug.Log($"<color=blue>★★ [元素反应：蒸发！] 叠层 x{stacks} 爆发！对 {gameObject.name} 造成 {rawDamage} 点无视防御伤害！ ★★</color>");
            TakeDamage(rawDamage, 5);
        }
    }

    /// <summary>
    /// 结算 Buff 回合递减和跳毒 [1]
    /// </summary>
    public void TickBuffs()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].OnTurnStart();
        }

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].durationTurns--;
            if (activeBuffs[i].durationTurns <= 0)
            {
                activeBuffs[i].OnRemove();
                activeBuffs.RemoveAt(i);
            }
        }

        OnBuffsChanged?.Invoke();
    }
}