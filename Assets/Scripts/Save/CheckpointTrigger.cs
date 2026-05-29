using UnityEngine;

/// <summary>
/// 大世界篝火/存档点触发器组件（碰撞即激活存档，并原地恢复状态）
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CheckpointTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            // 1. 保存当前篝火的物理世界坐标到硬盘！ [3]
            SaveManager.Instance.SaveCheckpoint(transform.position);

            // 2. 篝火激活表现：将主角的血蓝全部充满！
            CharacterStats stats = player.GetComponent<CharacterStats>();
            if (stats != null)
            {
                stats.currentHP = stats.maxHP;
                stats.currentMP = stats.maxMP;

                // 刷新 UI 进度条
                stats.OnHPChanged?.Invoke();
            }

            // 可以在此实例化一个精美的篝火点燃粒子效果，或者播放神圣的音效
            Debug.Log("<color=green>[篝火存档] 存档点已激活！角色状态已全部回满！</color>");
        }
    }
}