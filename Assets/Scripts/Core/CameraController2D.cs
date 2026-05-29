using UnityEngine;

/// <summary>
/// 2D类银河城大地图摄像机跟随控制脚本
/// </summary>
public class CameraController2D : MonoBehaviour
{
    [Header("追踪目标")]
    [SerializeField] private Transform target;                // 摄像机需要追踪的主角目标
    [SerializeField] private string playerTag = "Player";      // 若未手动拖拽目标，则通过此 Tag 在场景中自动检索

    [Header("平滑参数")]
    [SerializeField] private float smoothTime = 0.2f;         // 平滑阻尼时间，数值越小跟随越贴身，越大越柔和
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f); // 相对主角的偏移量（Z 轴一般保持在 -10f 避免穿透 2D 平面）

    [Header("大地图边界限制")]
    [SerializeField] private bool useBounds = false;          // 是否启用区域边界限制，防止相机移出关卡范围
    [SerializeField] private Vector2 minBounds;               // 相机能达到的最小世界坐标 (Min X, Min Y)
    [SerializeField] private Vector2 maxBounds;               // 相机能达到的最大世界坐标 (Max X, Max Y)

    [Header("大地图鼠标指针配置")]
    [SerializeField] private bool hideCursorAtStart = true;   // 进入场景时是否自动隐藏并锁定鼠标指针

    private Vector3 currentVelocity = Vector3.zero;           // SmoothDamp 内部物理计算所需的速度变量缓存

    private void Start()
    {
        // 1. 自动定位主角：如果未在 Inspector 中拖拽目标，系统将自动通过 Tag 或脚本类型抓取
        if (target == null)
        {
            GameObject playerObj = GameObject.FindWithTag(playerTag);
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
            else
            {
                PlayerController controller = FindObjectOfType<PlayerController>();
                if (controller != null)
                {
                    target = controller.transform;
                }
            }
        }

        // 2. 隐藏并锁定鼠标：类银河城游戏在大地图探索期不使用鼠标，故执行隐藏并置中锁定
        if (hideCursorAtStart)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        // 3. 初始坐标对齐：防止游戏开局瞬间摄像机从极为遥远的地方缓慢滑行过来
        if (target != null)
        {
            Vector3 targetPosition = target.position + offset;
            if (useBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
                targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
            }
            transform.position = targetPosition;
        }
    }

    private void LateUpdate()
    {
        // 摄像机追踪必须放在 LateUpdate 中执行，确保在主角所有的 Update、FixedUpdate 物理位移计算完全结束之后再调整相机位置，防止出现抖动
        if (target == null) return;

        // 计算包含偏移量的相机目标点
        Vector3 targetPosition = target.position + offset;

        // 若开启了地图边界约束，则对相机的目标 X 与 Y 坐标进行强行截断，防止视野穿帮
        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        }

        // 使用高精度物理渐进平滑算法将相机移向目标点，保证相机移动拥有渐入渐出的惯性手感
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
    }

    /// <summary>
    /// 在 Unity 编辑器场景（Scene）视图中可视化绘制相机边界，方便策划调整
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            Gizmos.color = Color.green;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f, transform.position.z);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}