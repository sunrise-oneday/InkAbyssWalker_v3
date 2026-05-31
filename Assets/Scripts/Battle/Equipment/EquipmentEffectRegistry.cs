using System.Collections.Generic;
using StoreAndInventory;
using UnityEngine;

/// <summary>
/// 测试装备效果注册表。当前仅：技能1 · 实际伤害百分比吸血。
/// </summary>
public static class EquipmentEffectRegistry
{
    public const string EffectIdTestSkill1Lifesteal = "test_skill1_lifesteal_10pct";

    public static void RunSkill1LifestealOnAfterDealDamage(
        IReadOnlyList<GameplayEffectSO> effects,
        EquipmentEffectContext ctx)
    {
        if (effects == null || ctx.attackerStats == null || ctx.skill == null || ctx.dealer == null)
            return;

        if (ctx.finalDamageDealt <= 0)
            return;

        var totalPercent = 0f;
        for (var i = 0; i < effects.Count; i++)
        {
            var so = effects[i];
            if (so == null || so.effectId != EffectIdTestSkill1Lifesteal)
                continue;

            if (!MatchesSkill1(ctx, so))
                continue;

            totalPercent += so.lifestealPercent > 0f ? so.lifestealPercent : 0.1f;
        }

        if (totalPercent <= 0f)
            return;

        var heal = Mathf.Max(1, Mathf.RoundToInt(ctx.finalDamageDealt * totalPercent));
        ctx.attackerStats.Heal(heal);
        Debug.Log($"[装备效果·测试] 技能1吸血 +{heal}（伤害 {ctx.finalDamageDealt} × {totalPercent:P0}）");
    }

    static bool MatchesSkill1(EquipmentEffectContext ctx, GameplayEffectSO so)
    {
        if (!string.IsNullOrEmpty(so.targetSkillId))
            return ctx.skill.skillName == so.targetSkillId;

        var form = ctx.dealer.CurrentForm;
        if (form?.availableSkills == null || form.availableSkills.Count == 0)
            return false;

        return ctx.skill.skillName == form.availableSkills[0].skillName;
    }
}
