using System.Collections;
using UnityEngine;

/// <summary>
/// 特殊方块（蔚蓝风格）：
/// - 只有冲刺状态才能撞开
/// - 撞开时玩家被弹开一定距离
/// - 方块碎裂成多个碎片飞散并渐隐消失
/// - 可选是否经过一段时间自动复原
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SpecialWall : MonoBehaviour
{
    [Header("弹开设置")]
    [SerializeField] private float bounceForce = 12f;       // 冲刺撞墙后弹开的水平力
    [SerializeField] private float bounceUpForce = 6f;      // 弹开时向上的附加力
    [SerializeField] private float bounceDuration = 0.2f;   // 弹开持续时间（秒）

    [Header("碎裂效果")]
    [SerializeField] private int fragmentRows = 3;           // 碎片网格行数
    [SerializeField] private int fragmentCols = 3;           // 碎片网格列数
    [SerializeField] private float fragmentSpeed = 8f;       // 碎片飞散初速度
    [SerializeField] private float fragmentLifetime = 0.6f;  // 碎片存活时间（秒）
    [SerializeField] private float fragmentRotationSpeed = 360f; // 碎片旋转速度（度/秒）

    [Header("复原设置")]
    [SerializeField] private bool canRespawn = true;         // 是否允许自动复原
    [SerializeField] private float respawnTime = 5f;         // 复原倒计时（秒）

    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private Sprite originalSprite;
    private bool isBroken = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        originalSprite = spriteRenderer.sprite;
        // 使用物理碰撞（非 Trigger），这样冲刺撞墙时有碰撞感
        col.isTrigger = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isBroken) return;

        PlayerController player = collision.gameObject.GetComponentInParent<PlayerController>();
        if (player == null) return;

        // 检查是否处于冲刺状态
        var stateMachine = player.GetStateMachine();
        if (stateMachine == null || stateMachine.currentState == null) return;

        if (stateMachine.currentState is PlayerDashState)
        {
            // 1. 弹开玩家
            BouncePlayer(player, collision);

            // 2. 碎裂方块
            BreakWall();
        }
    }

    /// <summary>
    /// 弹开玩家：计算碰撞方向并施加反向冲量（远离墙的方向）
    /// </summary>
    private void BouncePlayer(PlayerController player, Collision2D collision)
    {
        // 计算弹开方向：从墙指向玩家（远离墙）
        Vector2 wallCenter = (Vector2)transform.position;
        Vector2 playerPos = (Vector2)player.transform.position;
        Vector2 bounceDir = (playerPos - wallCenter).normalized;

        // 如果方向接近零（正好在墙中心），使用玩家面朝方向
        if (bounceDir.sqrMagnitude < 0.01f)
        {
            bounceDir = Vector2.right * player.FacingDirection;
        }

        // 启动弹开协程：等待冲刺状态结束后再施加弹开力
        StartCoroutine(ApplyBounceAfterDash(player, bounceDir));
    }

    /// <summary>
    /// 等待冲刺状态结束后施加弹开力（冲刺期间速度被锁定，外部力无效）
    /// </summary>
    private IEnumerator ApplyBounceAfterDash(PlayerController player, Vector2 bounceDir)
    {
        // 等待冲刺状态结束（冲刺期间 FixedUpdate 持续覆盖速度）
        var stateMachine = player.GetStateMachine();
        while (stateMachine.currentState is PlayerDashState)
        {
            yield return null;
        }

        // 冲刺结束，施加弹开力（水平 + 向上）
        if (player != null && player.gameObject.activeInHierarchy)
        {
            player.rb.velocity = Vector2.zero;
            Vector2 force = bounceDir * bounceForce + Vector2.up * bounceUpForce;
            player.rb.AddForce(force, ForceMode2D.Impulse);

            // 短暂锁定输入，防止弹开期间立即行动
            player.enabled = false;
            yield return new WaitForSeconds(bounceDuration);
            if (player != null)
            {
                player.enabled = true;
            }
        }
    }

    /// <summary>
    /// 碎裂方块：生成碎片 + 隐藏原方块 + 触发复原
    /// </summary>
    private void BreakWall()
    {
        isBroken = true;

        // 生成碎片
        SpawnFragments();

        // 隐藏原方块
        spriteRenderer.enabled = false;
        col.enabled = false;

        // 复原流程
        if (canRespawn)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    /// <summary>
    /// 将原 Sprite 切割成网格碎片，每个碎片有随机飞散方向和旋转
    /// </summary>
    private void SpawnFragments()
    {
        if (originalSprite == null) return;

        Bounds bounds = spriteRenderer.bounds;
        float cellWidth = bounds.size.x / fragmentCols;
        float cellHeight = bounds.size.y / fragmentRows;

        // 原始 sprite 的像素尺寸
        float texWidth = originalSprite.texture.width;
        float texHeight = originalSprite.texture.height;
        float ppu = originalSprite.pixelsPerUnit;

        for (int row = 0; row < fragmentRows; row++)
        {
            for (int col = 0; col < fragmentCols; col++)
            {
                // 计算碎片在世界空间的位置
                float x = bounds.min.x + col * cellWidth + cellWidth * 0.5f;
                float y = bounds.min.y + row * cellHeight + cellHeight * 0.5f;
                Vector3 fragPos = new Vector3(x, y, bounds.center.z);

                // 计算碎片在原 sprite 中的像素区域
                float pixelX = col * (texWidth / fragmentCols);
                float pixelY = row * (texHeight / fragmentRows);
                float pixelW = texWidth / fragmentCols;
                float pixelH = texHeight / fragmentRows;

                // 从原 sprite 裁切出碎片 sprite
                Rect rect = new Rect(pixelX, pixelY, pixelW, pixelH);
                Sprite fragSprite = Sprite.Create(
                    originalSprite.texture,
                    rect,
                    new Vector2(0.5f, 0.5f),
                    ppu
                );

                // 创建碎片 GameObject
                GameObject frag = new GameObject($"Fragment_{row}_{col}");
                frag.transform.position = fragPos;
                frag.transform.localScale = transform.lossyScale;

                SpriteRenderer sr = frag.AddComponent<SpriteRenderer>();
                sr.sprite = fragSprite;
                sr.sortingLayerID = spriteRenderer.sortingLayerID;
                sr.sortingOrder = spriteRenderer.sortingOrder + 1;

                // 添加物理组件
                Rigidbody2D rb = frag.AddComponent<Rigidbody2D>();
                rb.gravityScale = 1.5f; // 轻微重力，碎片会下落

                // 计算飞散方向：从中心向外 + 随机偏移
                Vector2 fragDir = ((Vector2)fragPos - (Vector2)bounds.center).normalized;
                fragDir += Random.insideUnitCircle * 0.3f; // 加入随机扰动
                rb.velocity = fragDir * fragmentSpeed;

                // 随机旋转
                float rotSpeed = Random.Range(-fragmentRotationSpeed, fragmentRotationSpeed);
                frag.AddComponent<FragmentRotator>().speed = rotSpeed;

                // 渐隐 + 自动销毁
                StartCoroutine(FadeAndDestroy(fr: sr, obj: frag));
            }
        }
    }

    /// <summary>
    /// 碎片渐隐后销毁
    /// </summary>
    private IEnumerator FadeAndDestroy(SpriteRenderer fr, GameObject obj)
    {
        float elapsed = 0f;
        Color originalColor = fr.color;

        while (elapsed < fragmentLifetime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fragmentLifetime);
            fr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        Destroy(obj);
    }

    /// <summary>
    /// 复原倒计时协程
    /// </summary>
    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);

        isBroken = false;
        spriteRenderer.enabled = true;
        col.enabled = true;
    }

    /// <summary>
    /// 手动复原方块（供外部调用，如检查点重置）
    /// </summary>
    public void ResetWall()
    {
        StopAllCoroutines();
        isBroken = false;
        spriteRenderer.enabled = true;
        col.enabled = true;
    }

    /// <summary>
    /// 碎片旋转组件
    /// </summary>
    private class FragmentRotator : MonoBehaviour
    {
        public float speed;

        private void Update()
        {
            transform.Rotate(0f, 0f, speed * Time.deltaTime);
        }
    }
}
