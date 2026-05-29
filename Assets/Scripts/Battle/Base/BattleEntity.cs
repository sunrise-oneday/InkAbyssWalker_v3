using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public abstract class BattleEntity : EntityBase
{
    public CharacterStats Stats { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        Stats = GetComponent<CharacterStats>();
    }

    public virtual void ReceiveAttack(int damage, int breakDamage)
    {
        Stats.TakeDamage(damage, breakDamage);
        if (Stats.currentHP <= 0)
        {
            Die();
        }
        else
        {
            // ========================================================
            // 核心重构：只要受伤且未战败，全自动、无缝播放【闪红 + 局部受击抖动】反馈！ [2, 5]
            // ========================================================
            PlayHitFeedback();
        }
    }

    /// <summary>
    /// 播放通用的受击反馈表现 [2]
    /// </summary>
    public void PlayHitFeedback()
    {
        // 1. 精灵局部坐标抖动 0.15 秒（幅度 0.12，不影响父物体的坐标） [2]
        StartCoroutine(ShakeSpriteRoutine(0.15f, 0.12f));
        // 2. 精灵闪烁红光 0.12 秒 [2]
        StartCoroutine(FlashColorRoutine(Color.red, 0.12f));
    }

    /// <summary>
    /// 通用变色接口：向外暴露（供完美招架成功时闪烁青光、被击中闪红光等调用）
    /// </summary>
    public void FlashColor(Color color, float duration)
    {
        StartCoroutine(FlashColorRoutine(color, duration));
    }

    private IEnumerator FlashColorRoutine(Color color, float duration)
    {
        if (sprite != null)
        {
            sprite.color = color;
            yield return new WaitForSeconds(duration);
            sprite.color = Color.white; // 自动复原为正常白色
        }
    }

    private IEnumerator ShakeSpriteRoutine(float duration, float magnitude)
    {
        if (sprite == null) yield break;

        Vector3 originalLocalPos = sprite.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 计算随机抖动偏移量
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            sprite.transform.localPosition = originalLocalPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;

            yield return null; // 等待一帧
        }

        sprite.transform.localPosition = originalLocalPos; // 抖动结束，物理归位
    }

    protected virtual void Die()
    {
        Debug.Log($"{gameObject.name} 战败了！");
    }

}