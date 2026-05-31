using System.Collections.Generic;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("基础生命与法力")]
    public int maxHP = 100;
    public int currentHP;
    public int maxMP = 50;
    public int currentMP;

    [Header("护盾系统（杀戮尖塔风格）")]
    public int shield;                    // 当前护盾值
    public int maxShield = 999;           // 最大护盾值

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
    public System.Action OnShieldChanged;

    private void Awake()
    {
        currentHP = maxHP;
        currentMP = maxMP;
        currentBreakValue = maxBreakValue;
        shield = 0;
    }

    // ============================================
    // 护盾系统方法
    // ============================================

    /// <summary>
    /// 添加护盾值（经过 buff 链拦截修正）
    /// </summary>
    public void AddShield(int amount)
    {
        if (amount <= 0) return;

        // 调用所有活跃 Buff 的护盾增益拦截器
        int processedAmount = amount;
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            processedAmount = activeBuffs[i].OnBeforeGainShield(processedAmount);
        }

        shield = Mathf.Min(shield + processedAmount, maxShield);
        OnShieldChanged?.Invoke();
        Debug.Log($"[护盾] {gameObject.name} 获得 {processedAmount} 护盾(原始{amount})，当前护盾: {shield}");
    }

    /// <summary>
    /// 清除所有护盾
    /// </summary>
    public void ClearShield()
    {
        if (shield <= 0) return;
        shield = 0;
        OnShieldChanged?.Invoke();
    }

    /// <summary>
    /// 带护盾的伤害计算（护盾优先抵挡伤害）
    /// </summary>
    public int TakeDamageWithShield(int rawDamage, int breakDamage)
    {
        // 1. 调用所有活跃 Buff 的伤害拦截器
        int processedDamage = rawDamage;
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            processedDamage = activeBuffs[i].OnBeforeTakeDamage(processedDamage);
        }

        float finalMultiplier = isBroken ? breakDamageMultiplier : 1.0f;
        int damageAfterDefense = Mathf.Max(Mathf.RoundToInt((processedDamage - defense) * finalMultiplier), 1);

        // 2. 护盾优先抵挡伤害
        int damageToShield = Mathf.Min(damageAfterDefense, shield);
        shield -= damageToShield;
        int remainingDamage = damageAfterDefense - damageToShield;

        // 3. 剩余伤害扣血
        int finalDamage = remainingDamage;
        if (remainingDamage > 0)
        {
            currentHP = Mathf.Max(currentHP - remainingDamage, 0);
        }

        // 4. 触发事件
        if (damageToShield > 0)
        {
            OnShieldChanged?.Invoke();
            Debug.Log($"[护盾] {gameObject.name} 护盾抵挡了 {damageToShield} 伤害，剩余护盾: {shield}");
        }

        if (remainingDamage > 0 || damageToShield > 0)
        {
            OnHPChanged?.Invoke();
        }

        // 5. 破防值计算（护盾不影响破防）
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

    /// <summary>
    /// 预览对此目标造成的最终伤害（用于工具提示的动态数值显示）
    /// 只反映目标身上的 buff 链（如易伤、破防状态）对伤害的修正，
    /// 不包含攻击力 stat 和防御减免，让玩家看到纯粹的 buff/debuff 影响
    /// </summary>
    /// <param name="baseDamage">技能基础伤害</param>
    /// <returns>经过目标 buff 链修正后的预估伤害</returns>
    public int PreviewEffectiveDamage(int baseDamage)
    {
        // 只经过目标的 Buff 拦截器链（如 VulnerabilityBuff 放大伤害）
        int processedDamage = baseDamage;
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            processedDamage = activeBuffs[i].OnBeforeTakeDamage(processedDamage);
        }
        return processedDamage;
    }

    /// <summary>
    /// 预览此目标获得护盾的最终值（用于工具提示的动态数值显示）
    /// </summary>
    /// <param name="baseShield">基础护盾值</param>
    /// <returns>经过 buff 链拦截后的预估护盾值</returns>
    public int PreviewBlockGain(int baseShield)
    {
        int processedAmount = baseShield;
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            processedAmount = activeBuffs[i].OnBeforeGainShield(processedAmount);
        }
        return processedAmount;
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
    /// 结算 Buff 回合效果（燃烧/中毒跳伤害等），在回合开始时调用
    /// </summary>
    public void ProcTurnStartBuffs()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].OnTurnStart();
        }
    }

    /// <summary>
    /// 扣减 Buff 持续回合数并移除过期 Buff，在回合结束时调用
    /// </summary>
    public void TickBuffDurations()
    {
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

    /// <summary>
    /// 完整的 Buff 结算（效果 + 扣回合），敌方回合结束时使用
    /// </summary>
    public void TickBuffs()
    {
        ProcTurnStartBuffs();
        TickBuffDurations();
    }
}