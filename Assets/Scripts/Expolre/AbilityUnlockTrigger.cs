using UnityEngine;

/// <summary>
/// 将此脚本挂载在关卡中的任何宝箱或能量球上（碰撞体需勾选 Is Trigger
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AbilityUnlockTrigger : MonoBehaviour
{
    [Header("配置要解锁的技能")]
    [SerializeField] private ExplorationAbility abilityToUnlock;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 寻找父物体上的 PlayerController 确保物理安全
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            // 1. 解锁能力
            player.UnlockAbility(abilityToUnlock);

            // 2. 提示：在此处可以触发 UIManager 弹窗展示 “获得：二段跳能力！” 

            // 3. 销毁自身，防止重复吃
            Destroy(gameObject);
        }
    }
}