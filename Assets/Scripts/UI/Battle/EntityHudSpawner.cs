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

    [Header("操作面板 HP 条（Shader 血条，独立于完整 HUD）")]
    [SerializeField] private Transform actionPanelHPContainer;
    [SerializeField] private GameObject actionPanelHPBarPrefab;

    private List<GameObject> spawnedHUDs = new List<GameObject>();
    private EntityHUD actionPanelHUD; // 操作面板内的 HP 条实例

    /// <summary>为队伍生成血条</summary>
    public void SpawnHUDs(List<PlayerBattleEntity> party)
    {
        ClearAll();

        // 1. 生成完整玩家 HUD（放在原容器）
        if (playerHUDContainer != null && playerHUDPrefab != null)
        {
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

        // 2. 生成操作面板内的 Shader HP 条（独立于完整 HUD）
        if (actionPanelHPContainer != null && actionPanelHPBarPrefab != null && party.Count > 0 && party[0] != null)
        {
            // 先实例化到预制体自身坐标空间，再挂到父容器下，防止 LayoutGroup 等改变参数
            GameObject hpObj = Instantiate(actionPanelHPBarPrefab);
            RectTransform rt = hpObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                // 记录预制体原始参数
                Vector2 anchorMin = rt.anchorMin;
                Vector2 anchorMax = rt.anchorMax;
                Vector2 anchoredPos = rt.anchoredPosition;
                Vector2 sizeDelta = rt.sizeDelta;
                Vector3 localScale = rt.localScale;
                Vector2 pivot = rt.pivot;

                rt.SetParent(actionPanelHPContainer, false);

                // 恢复预制体原始参数，不受父容器 Layout 影响
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.anchoredPosition = anchoredPos;
                rt.sizeDelta = sizeDelta;
                rt.localScale = localScale;
                rt.pivot = pivot;
            }
            actionPanelHUD = hpObj.GetComponentInChildren<EntityHUD>(); // EntityHUD 可能在子物体上
            if (actionPanelHUD != null)
            {
                actionPanelHUD.SetTargetStats(party[0].Stats);
                actionPanelHUD.RefreshAll();
                Debug.Log($"[EntityHudSpawner] 操作面板 HP 条已生成: {actionPanelHUD.name}");
            }
            else
            {
                Debug.LogWarning($"[EntityHudSpawner] actionPanelHPBarPrefab 上没有找到 EntityHUD 组件（包括子物体）");
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
        if (actionPanelHUD != null)
            actionPanelHUD.RefreshAll();
    }

    /// <summary>清空所有血条</summary>
    public void ClearAll()
    {
        foreach (var hud in spawnedHUDs)
        {
            if (hud != null) Destroy(hud);
        }
        spawnedHUDs.Clear();

        if (actionPanelHUD != null)
        {
            Destroy(actionPanelHUD.gameObject);
            actionPanelHUD = null;
        }
    }
}
