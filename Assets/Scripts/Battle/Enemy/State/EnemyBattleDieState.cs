using UnityEngine;

/// <summary>
/// 怪物战斗死亡/战败状态
/// </summary>
public class EnemyBattleDieState : EnemyBaseBattleState, IDeathState
{
    protected override int AnimHash => Animator.StringToHash("Enemy_Die"); // 播放死亡动画

    public override void Enter()
    {
        base.Enter(); // 播放死亡动作
        owner.SetHorizontalVelocity(0f);

        // 核心安全：彻底禁用物理模拟！防止死后的尸体阻挡其他怪出招，或者产生推挤碰撞
        owner.rb.simulated = false;

        Debug.Log($"[怪物战败] {owner.gameObject.name} 被击杀，切入死亡动作。");

        // 如果在怪物预制体面板上勾选了“死后自动消失” [5]
        if (owner.destroyOnDeath)
        {
            owner.StartCoroutine(DestroyAndFadeRoutine(1.8f)); // 1.8 秒后渐隐并销毁物身
        }
    }

    private System.Collections.IEnumerator DestroyAndFadeRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        float fadeDuration = 1.0f; // 渐隐持续 1 秒
        Color originalColor = owner.sprite.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            owner.sprite.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        Object.Destroy(owner.gameObject);
    }
}