using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2D类银河城移动平台（速度广播 + 0 摩擦力物理接管模式）
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class MovingPlatform2D : MonoBehaviour
{
    [Header("移动路径配置")]
    [SerializeField] private Transform pointA;           // 移动起点 A
    [SerializeField] private Transform pointB;           // 移动终点 B
    [SerializeField] private float speed = 3f;           // 平台移动速度
    [SerializeField] private float waitTime = 1f;        // 到达端点后的停顿等待时间

    [Header("调试开关")]
    [SerializeField] private bool enableDebugLogs = true; // 是否开启控制台彩色调试日志

    private Rigidbody2D rb;
    private Vector3 targetPos;
    private float waitTimer;
    private bool isWaiting;

    // 核心新增：向外暴露平台当前的世界坐标系移动速度
    public Vector2 CurrentVelocity { get; private set; }

    // 动态创建的 0 摩擦力物理材质
    private PhysicsMaterial2D frictionlessMaterial;
    // 缓存玩家原本的物理材质，离开平台时安全还原
    private Dictionary<Collider2D, PhysicsMaterial2D> originalMaterials = new Dictionary<Collider2D, PhysicsMaterial2D>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 1. 动态生成 0 摩擦力材质并赋予平台自身，防止平台主动向玩家传导物理推力
        frictionlessMaterial = new PhysicsMaterial2D("Platform_Frictionless")
        {
            friction = 0f,
            bounciness = 0f
        };
        GetComponent<Collider2D>().sharedMaterial = frictionlessMaterial;

        if (pointB != null) targetPos = pointB.position;
        else if (pointA != null) targetPos = pointA.position;
    }

    private void FixedUpdate()
    {
        if (pointA == null || pointB == null)
        {
            CurrentVelocity = Vector2.zero;
            return;
        }

        if (isWaiting)
        {
            waitTimer += Time.fixedDeltaTime;
            if (waitTimer >= waitTime) isWaiting = false;
            CurrentVelocity = Vector2.zero;
            return;
        }

        Vector3 currentPos = transform.position;
        Vector3 newPos = Vector3.MoveTowards(currentPos, targetPos, speed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        // 计算并广播当前帧的精确速度
        CurrentVelocity = ((Vector2)newPos - (Vector2)currentPos) / Time.fixedDeltaTime;

        if (Vector3.Distance(newPos, targetPos) < 0.01f)
        {
            isWaiting = true;
            waitTimer = 0f;
            targetPos = (targetPos == pointA.position) ? pointB.position : pointA.position;
        }
    }

    // ========================================================
    // 物理材质接管：当玩家踩在平台上时，强行将其摩擦力降为 0。
    // 这能切断物理引擎自动施加的摩擦推力，让玩家的速度完全交由状态机接管！
    // ========================================================
    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f) // 确保玩家在平台上方
                {
                    player.SetCurrentPlatform(this);

                    // 接管玩家碰撞体的材质，将其设为 0 摩擦力
                    Collider2D passengerCollider = collision.collider;
                    if (passengerCollider != null && !originalMaterials.ContainsKey(passengerCollider))
                    {
                        originalMaterials[passengerCollider] = passengerCollider.sharedMaterial;
                        passengerCollider.sharedMaterial = frictionlessMaterial;
                    }

                    if (enableDebugLogs)
                    {
                        Debug.Log($"<color=green>[物理接管] 玩家 [{collision.gameObject.name}] 已踏上平台，临时将物理摩擦力归零！</color>", gameObject);
                    }
                    break;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            player.SetCurrentPlatform(null); // 通知玩家脱离平台

            // 还原玩家碰撞体原本的摩擦力材质，保证其在普通地面上的移动手感不受影响
            Collider2D passengerCollider = collision.collider;
            if (passengerCollider != null && originalMaterials.ContainsKey(passengerCollider))
            {
                passengerCollider.sharedMaterial = originalMaterials[passengerCollider];
                originalMaterials.Remove(passengerCollider);
            }

            if (enableDebugLogs)
            {
                Debug.Log($"<color=red>[物理还原] 玩家 [{collision.gameObject.name}] 已离开平台，已恢复其原始物理材质。</color>", gameObject);
            }
        }
    }
}