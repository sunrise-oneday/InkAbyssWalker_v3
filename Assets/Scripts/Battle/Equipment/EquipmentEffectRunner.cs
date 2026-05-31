using StoreAndInventory;
using UnityEngine;

/// <summary>
/// 战斗内缓存装备 extraEffects；由 BattleManager 开战 Bind、收战 Clear。
/// </summary>
public class EquipmentEffectRunner : MonoBehaviour
{
    CharacterStats protagonistStats;
    PlayerBattleEntity protagonistEntity;
    System.Collections.Generic.IReadOnlyList<GameplayEffectSO> cachedEffects;

    public void BindForBattle(
        EquipmentService equipmentService,
        CharacterStats stats,
        PlayerBattleEntity battleEntity)
    {
        protagonistStats = stats;
        protagonistEntity = battleEntity;
        cachedEffects = equipmentService != null
            ? equipmentService.GetAllExtraEffects()
            : null;
    }

    public void ClearBattleCache()
    {
        protagonistStats = null;
        protagonistEntity = null;
        cachedEffects = null;
    }

    public void OnAfterDealDamage(PlayerBattleEntity dealer, Skill skill, int finalDamageDealt)
    {
        if (cachedEffects == null || protagonistStats == null || protagonistEntity == null)
            return;

        if (dealer == null || dealer.Stats != protagonistStats)
            return;

        var turn = BattleManager.Instance != null ? BattleManager.Instance.currentTurn : 0;
        var ctx = new EquipmentEffectContext(
            dealer,
            protagonistStats,
            skill,
            finalDamageDealt,
            turn);

        EquipmentEffectRegistry.RunSkill1LifestealOnAfterDealDamage(cachedEffects, ctx);
    }
}
