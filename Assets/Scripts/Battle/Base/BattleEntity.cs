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

    public virtual int ReceiveAttack(int damage, int breakDamage)
    {
        // 使用带护盾的伤害计算（护盾优先抵挡伤害）
        int finalDamage = Stats.TakeDamageWithShield(damage, breakDamage);
        if (Stats.currentHP <= 0)
        {
            Die();
        }
        else
        {
            // ========================================================
            // �����ع���ֻҪ������δս�ܣ�ȫ�Զ����޷첥�š����� + �ֲ��ܻ������������� [2, 5]
            // ========================================================
            PlayHitFeedback();
        }

        return finalDamage;
    }

    /// <summary>
    /// ����ͨ�õ��ܻ��������� [2]
    /// </summary>
    public void PlayHitFeedback()
    {
        // 1. ����ֲ����궶�� 0.15 �루���� 0.12����Ӱ�츸��������꣩ [2]
        StartCoroutine(ShakeSpriteRoutine(0.15f, 0.12f));
        // 2. ������˸��� 0.12 �� [2]
        StartCoroutine(FlashColorRoutine(Color.red, 0.12f));
    }

    /// <summary>
    /// ͨ�ñ�ɫ�ӿڣ����Ⱪ¶���������мܳɹ�ʱ��˸��⡢�����������ȵ��ã�
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
            sprite.color = Color.white; // �Զ���ԭΪ������ɫ
        }
    }

    private IEnumerator ShakeSpriteRoutine(float duration, float magnitude)
    {
        if (sprite == null) yield break;

        Vector3 originalLocalPos = sprite.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // �����������ƫ����
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            sprite.transform.localPosition = originalLocalPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;

            yield return null; // �ȴ�һ֡
        }

        sprite.transform.localPosition = originalLocalPos; // ����������������λ
    }

    protected virtual void Die()
    {
        Debug.Log($"{gameObject.name} ս���ˣ�");
    }

}