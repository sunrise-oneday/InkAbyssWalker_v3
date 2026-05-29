using UnityEngine;

/// <summary>
/// 将此脚本挂载在关卡中的任何宝箱、能量球或引导石碑上（碰撞体需勾选 Is Trigger）
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AbilityUnlockTrigger : MonoBehaviour
{
    [Header("配置要解锁的技能")]
    [SerializeField] private ExplorationAbility abilityToUnlock;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 寻找父物体或自身上的 PlayerController，确保物理安全
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            // 1. 调用解锁并自动存档
            player.UnlockAbility(abilityToUnlock);

            // 2. 物理效果表现：可以在此处播放解锁特效、音效，以及广播弹窗 UI

            // 3. 销毁触发器自身，防止重复吃
            Destroy(gameObject);
        }
    }
}