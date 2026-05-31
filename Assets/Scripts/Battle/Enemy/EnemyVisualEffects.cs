using System.Collections;
using UnityEngine;

namespace Battle.Enemy
{
    /// <summary>
    /// 敌人视觉效果管理器
    /// 管理护盾、Debuff、意图等视觉效果
    /// </summary>
    public class EnemyVisualEffects : MonoBehaviour
    {
        [Header("效果预制件")]
        [SerializeField] private GameObject shieldBreakEffectPrefab;  // 护盾破碎效果
        [SerializeField] private GameObject blockEffectPrefab;        // 格挡效果
        [SerializeField] private GameObject healEffectPrefab;         // 回血效果
        [SerializeField] private GameObject buffEffectPrefab;         // 增益效果

        [Header("组件引用")]
        [SerializeField] private EnemyBattleEntity enemyEntity;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("颜色设置")]
        [SerializeField] private Color shieldBreakColor = new Color(0.5f, 0.8f, 1f);  // 护盾破碎颜色
        [SerializeField] private Color blockColor = new Color(0.7f, 0.7f, 0.7f);      // 格挡颜色
        [SerializeField] private Color healColor = new Color(0.3f, 1f, 0.3f);          // 回血颜色
        [SerializeField] private Color buffColor = new Color(1f, 0.8f, 0.2f);          // 增益颜色

        private CharacterStats stats;

        private void Awake()
        {
            if (enemyEntity == null)
                enemyEntity = GetComponent<EnemyBattleEntity>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            stats = GetComponent<CharacterStats>();
        }

        private void OnEnable()
        {
            if (stats != null)
            {
                stats.OnShieldChanged += OnShieldChanged;
                stats.OnHPChanged += OnHPChanged;
            }
        }

        private void OnDisable()
        {
            if (stats != null)
            {
                stats.OnShieldChanged -= OnShieldChanged;
                stats.OnHPChanged -= OnHPChanged;
            }
        }

        /// <summary>
        /// 护盾改变时触发效果
        /// </summary>
        private void OnShieldChanged()
        {
            if (stats == null) return;

            // 如果护盾减少了，播放护盾破碎效果
            if (stats.shield <= 0)
            {
                PlayShieldBreakEffect();
            }
        }

        /// <summary>
        /// 血量改变时触发效果
        /// </summary>
        private void OnHPChanged()
        {
            // 受伤闪烁效果由BattleEntity基类处理
        }

        /// <summary>
        /// 播放护盾破碎效果
        /// </summary>
        public void PlayShieldBreakEffect()
        {
            if (shieldBreakEffectPrefab != null)
            {
                GameObject effect = Instantiate(shieldBreakEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }

            // 护盾破碎时闪蓝光
            StartCoroutine(FlashColorRoutine(shieldBreakColor, 0.3f));

            Debug.Log($"[视觉效果] {gameObject.name} 护盾破碎！");
        }

        /// <summary>
        /// 播放格挡效果
        /// </summary>
        public void PlayBlockEffect()
        {
            if (blockEffectPrefab != null)
            {
                GameObject effect = Instantiate(blockEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 1.5f);
            }

            // 格挡时闪灰光
            StartCoroutine(FlashColorRoutine(blockColor, 0.2f));

            Debug.Log($"[视觉效果] {gameObject.name} 格挡！");
        }

        /// <summary>
        /// 播放回血效果
        /// </summary>
        public void PlayHealEffect()
        {
            if (healEffectPrefab != null)
            {
                GameObject effect = Instantiate(healEffectPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                Destroy(effect, 1.5f);
            }

            // 回血时闪绿光
            StartCoroutine(FlashColorRoutine(healColor, 0.3f));

            Debug.Log($"[视觉效果] {gameObject.name} 回血！");
        }

        /// <summary>
        /// 播放增益效果
        /// </summary>
        public void PlayBuffEffect()
        {
            if (buffEffectPrefab != null)
            {
                GameObject effect = Instantiate(buffEffectPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                Destroy(effect, 1.5f);
            }

            // 增益时闪金光
            StartCoroutine(FlashColorRoutine(buffColor, 0.3f));

            Debug.Log($"[视觉效果] {gameObject.name} 增益！");
        }

        /// <summary>
        /// 播放意图改变效果
        /// </summary>
        public void PlayIntentChangeEffect()
        {
            // 意图改变时闪白光
            StartCoroutine(FlashColorRoutine(Color.white, 0.15f));
        }

        /// <summary>
        /// 颜色闪烁协程
        /// </summary>
        private IEnumerator FlashColorRoutine(Color color, float duration)
        {
            if (spriteRenderer == null) yield break;

            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = color;

            yield return new WaitForSeconds(duration);

            spriteRenderer.color = originalColor;
        }
    }
}
