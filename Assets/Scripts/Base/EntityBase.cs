using UnityEngine;

/// <summary>
/// 所有移动实体的抽象基类 (Player 和 Enemy 都会继承它)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public abstract class EntityBase : MonoBehaviour
{
    // 对子类暴露基础组件引用
    public Rigidbody2D rb { get; protected set; }
    public Animator anim { get; protected set; }
    public SpriteRenderer sprite { get; protected set; }

    [Header("朝向设置")]
    public bool IsFacingRight = true; // 当前是否朝右
    public bool CanFlip = true;       // 是否允许翻转 (方便外部状态如释放特定技能、受击时临时锁死朝向)
    // 如果朝右，返回 1；如果朝左，返回 -1。
    // 这样在任何代码（如冲刺、开枪、受击击退）中，都可以直接乘以这个数值来获得方向向量
    public int FacingDirection => IsFacingRight ? 1 : -1;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>(); // 获取自身或子物体的渲染器
    }

    /// <summary>
    /// 根据水平输入调整朝向 (提供给外部或子类调用)
    /// </summary>
    /// <param name="horizontalInput">当前的水平输入或移动方向速度 (x轴)</param>
    public virtual void AdjustFacingDirection(float horizontalInput)
    {
        if (!CanFlip) return;

        // 只有当有明确的向右输入，且当前朝左时，才翻转
        if (horizontalInput > 0.01f && !IsFacingRight)
        {
            Flip();
        }
        // 只有当有明确的向左输入，且当前朝右时，才翻转
        else if (horizontalInput < -0.01f && IsFacingRight)
        {
            Flip();
        }
    }

    /// <summary>
    /// 执行翻转逻辑 (通过修改 localScale)
    /// </summary>
    public virtual void Flip()
    {
        IsFacingRight = !IsFacingRight;

        Vector3 localScale = transform.localScale;
        localScale.x *= -1f; // 反转 X 轴缩放
        transform.localScale = localScale;
    }

    /// <summary>
    /// 通用的设置水平速度方法
    /// </summary>
    public void SetHorizontalVelocity(float xVelocity)
    {
        if (rb != null)
        {
            rb.velocity = new Vector2(xVelocity, rb.velocity.y);
        }
    }
}