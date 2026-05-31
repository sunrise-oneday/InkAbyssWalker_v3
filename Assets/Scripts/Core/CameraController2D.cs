using UnityEngine;

/// <summary>
/// 2D 类银河城大地图摄像机跟随控制器。
/// LateUpdate 中零多余方法调用，性能敏感路径已优化。
/// </summary>
public class CameraController2D : MonoBehaviour
{
    [Header("追踪目标")]
    [Tooltip("摄像机追踪的目标。若未指定则自动按 Tag 查找 Player")]
    [SerializeField] private Transform target;
    [Tooltip("自动搜索玩家时使用的场景 Tag")]
    [SerializeField] private string playerTag = "Player";

    [Header("平滑参数")]
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);

    [Header("大地图边界限制")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    [Header("大地图鼠标指针配置")]
    [SerializeField] private bool hideCursorAtStart = true;

    // ---- 运行时缓存 ----
    private Vector3 currentVelocity = Vector3.zero;
    // 场景重载后仅尝试一次自动恢复，避免每帧执行 FindXXX
    private bool hasAttemptedRecovery = false;

    // ============================================
    // Unity 生命周期
    // ============================================

    private void Start()
    {
        BindTarget();
        ApplyCursorState();
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            // 场景重载等导致引用丢失时，仅在第一帧尝试一次自动恢复
            if (!hasAttemptedRecovery)
            {
                hasAttemptedRecovery = true;
                TryAutoRecover();
            }

            // 恢复成功则继续跟随，失败则跳过本帧
            if (target == null) return;
        }

        // 计算目标位置（含偏移）
        Vector3 targetPosition = target.position + offset;

        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        }

        transform.position = Vector3.SmoothDamp(
            transform.position, targetPosition,
            ref currentVelocity, smoothTime);
    }

    // ============================================
    // 公开方法
    // ============================================

    /// <summary>
    /// 强制刷新目标引用并立即对齐。
    /// 战斗结束返回大地图后由 BattleManager 调用。
    /// </summary>
    public void RefreshTarget()
    {
        target = null;
        hasAttemptedRecovery = false;  // 允许自动恢复再次触发
        TryAutoRecover();
        // 重置阻尼速度防止对齐时抖动
        currentVelocity = Vector3.zero;
        SnapToTarget();
    }

    // ============================================
    // 内部方法
    // ============================================

    /// <summary>在 Start 中绑定目标（仅一次）</summary>
    private void BindTarget()
    {
        if (target != null) return;

        var playerObj = GameObject.FindWithTag(playerTag);
        if (playerObj != null)
        {
            target = playerObj.transform;
            return;
        }

        var controller = FindObjectOfType<PlayerController>();
        if (controller != null)
            target = controller.transform;
    }

    /// <summary>场景重载后自动恢复（最多执行一次）</summary>
    private void TryAutoRecover()
    {
        var playerObj = GameObject.FindWithTag(playerTag);
        if (playerObj != null)
        {
            target = playerObj.transform;
            return;
        }

        var controller = FindObjectOfType<PlayerController>();
        if (controller != null)
            target = controller.transform;
    }

    private void ApplyCursorState()
    {
        if (hideCursorAtStart)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    /// <summary>立即对齐到目标位置（跳过平滑）</summary>
    private void SnapToTarget()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + offset;
        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        }
        transform.position = targetPosition;
    }

    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;

        Gizmos.color = Color.green;
        Vector3 center = new Vector3(
            (minBounds.x + maxBounds.x) * 0.5f,
            (minBounds.y + maxBounds.y) * 0.5f,
            transform.position.z);
        Vector3 size = new Vector3(
            maxBounds.x - minBounds.x,
            maxBounds.y - minBounds.y,
            1f);
        Gizmos.DrawWireCube(center, size);
    }
}
