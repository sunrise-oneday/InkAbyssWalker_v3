using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public abstract class BattleEntity : EntityBase
{
    public CharacterStats Stats { get; private set; }

    private ShaderEffectController _effectController;

    protected override void Awake()
    {
        base.Awake();

        Stats = GetComponent<CharacterStats>();
        _effectController = sprite?.GetComponent<ShaderEffectController>();
    }

    public virtual int ReceiveAttack(int damage, int breakDamage)
    {
        int finalDamage = Stats.TakeDamage(damage, breakDamage);
        if (Stats.currentHP <= 0)
        {
            // 先播死亡视觉序列（闪 + 消融），消融结束后再执行 Die()
            PlayDeathFlashThenDie();
        }
        else
        {
            // 只要尚未战死，全自动无缝播放 抖动 + 红闪 [2, 5]
            PlayHitFeedback();
        }

        return finalDamage;
    }

    /// <summary>
    /// 通用的受击反馈接口 [2]
    /// </summary>
    public void PlayHitFeedback()
    {
        StartCoroutine(ShakeSpriteRoutine(0.15f, 0.12f));

        if (_effectController != null)
        {
            _effectController.PlayHitFlash(0.15f);
        }
        else
        {
            // 降级：没有 ShaderEffectController 时走旧 sprite.color 红闪
            StartCoroutine(FlashColorRoutine(Color.red, 0.12f));
        }
    }

    /// <summary>
    /// 死亡序列：受伤闪 → 立即 Die()，由子类状态机控制后续死亡动画和消融
    /// </summary>
    protected void PlayDeathFlashThenDie()
    {
        StartCoroutine(ShakeSpriteRoutine(0.15f, 0.12f));

        if (_effectController != null)
        {
            _effectController.PlayHitFlash(0.15f);
        }
        else
        {
            // 降级：没有 Controller 时走旧逻辑
            StartCoroutine(FlashColorRoutine(Color.red, 0.12f));
        }

        // 立即进入死亡状态，消融由子类状态机在死亡动画结束后触发
        Die();
    }

    /// <summary>
    /// 通用变色接口，暴露给外部供格挡成功时闪光、受伤闪烁等功能调用
    /// </summary>
    public void FlashColor(Color color, float duration)
    {
        if (_effectController != null)
        {
            _effectController.PlayHitFlash(duration, color);
        }
        else
        {
            StartCoroutine(FlashColorRoutine(color, duration));
        }
    }

    private IEnumerator FlashColorRoutine(Color color, float duration)
    {
        if (sprite != null)
        {
            sprite.color = color;
            yield return new WaitForSeconds(duration);
            sprite.color = Color.white;
        }
    }

    private IEnumerator ShakeSpriteRoutine(float duration, float magnitude)
    {
        if (sprite == null) yield break;

        Vector3 originalLocalPos = sprite.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            sprite.transform.localPosition = originalLocalPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;

            yield return null;
        }

        sprite.transform.localPosition = originalLocalPos;
    }

    protected virtual void Die()
    {
        Debug.Log($"{gameObject.name} 战死了。");
    }
}
