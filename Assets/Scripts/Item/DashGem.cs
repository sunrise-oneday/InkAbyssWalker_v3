using System.Collections;
using UnityEngine;

/// <summary>
/// 冲刺重置水晶（蔚蓝风格）
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DashGem : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private float respawnTime = 3f;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D col;
    private bool isAvailable = true;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isAvailable) return;

        // ========================================================
        // 核心修复：改用 GetComponentInParent
        // 确保无论玩家的碰撞体在哪个子层级，都能准确拿到根对象上的 PlayerController 脚本
        // ========================================================
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            // ========================================================
            // 核心修复：吃水晶时传入 true，同时恢复能量和重置 CD，获得瞬间起冲资格！
            // ========================================================
            player.RefillDash(true);

            // 触发收集表现：宝石隐藏并失效
            StartCoroutine(CollectAndRespawnRoutine());
        }
    }

    private IEnumerator CollectAndRespawnRoutine()
    {
        isAvailable = false;
        spriteRenderer.enabled = false;
        col.enabled = false;

        yield return new WaitForSeconds(respawnTime);

        isAvailable = true;
        spriteRenderer.enabled = true;
        col.enabled = true;
    }
}
