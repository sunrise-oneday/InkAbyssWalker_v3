using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public abstract class BattleEntity : MonoBehaviour
{
    public CharacterStats Stats { get; private set; }
    public Animator Anim { get; private set; }

    // ========================================================
    // 补齐：在战斗实体基类中缓存物理刚体组件
    // ========================================================
    public Rigidbody2D Rb { get; private set; }

    protected virtual void Awake()
    {
        Stats = GetComponent<CharacterStats>();
        Anim = GetComponent<Animator>();
        Rb = GetComponent<Rigidbody2D>(); // 缓存刚体组件
    }

    /// <summary>
    /// 补齐：通用的设置物理水平速度方法，方便在战斗中强制物理定身
    /// </summary>
    public void SetHorizontalVelocity(float xVelocity)
    {
        if (Rb != null)
        {
            Rb.velocity = new Vector2(xVelocity, Rb.velocity.y);
        }
    }

    public virtual void ReceiveAttack(int damage, int breakDamage)
    {
        Stats.TakeDamage(damage, breakDamage);
        if (Stats.currentHP <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Debug.Log($"{gameObject.name} 战败了！");
    }
}