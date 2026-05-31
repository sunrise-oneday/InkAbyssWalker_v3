using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 实体血条生成器：在战斗开始时创建队伍成员的 HUD 血条
/// </summary>
public class EntityHudSpawner : MonoBehaviour
{
    [Header("血条容器与预制体")]
    [SerializeField] private Transform playerHUDContainer;
    [SerializeField] private GameObject playerHUDPrefab;

    private List<GameObject> spawnedHUDs = new List<GameObject>();

    /// <summary>为队伍生成血条</summary>
    public void SpawnHUDs(List<PlayerBattleEntity> party)
    {
        ClearAll();

        if (playerHUDContainer == null || playerHUDPrefab == null) return;

        foreach (var member in party)
        {
            if (member == null) continue;

            GameObject hudObj = Instantiate(playerHUDPrefab, playerHUDContainer);
            spawnedHUDs.Add(hudObj);

            var hudScript = hudObj.GetComponent<EntityHUD>();
            if (hudScript != null)
            {
                hudScript.SetTargetStats(member.Stats);
                hudScript.RefreshAll();
            }
        }
    }

    /// <summary>刷新所有血条</summary>
    public void RefreshAll()
    {
        foreach (var hud in spawnedHUDs)
        {
            if (hud == null) continue;
            var script = hud.GetComponent<EntityHUD>();
            if (script != null) script.RefreshAll();
        }
    }

    /// <summary>清空所有血条</summary>
    public void ClearAll()
    {
        foreach (var hud in spawnedHUDs)
        {
            if (hud != null) Destroy(hud);
        }
        spawnedHUDs.Clear();
    }
}
