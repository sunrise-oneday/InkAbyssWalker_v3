using System.Collections;
using UnityEngine;
using TMPro;

namespace Battle.Enemy
{
    /// <summary>
    /// 敌人战斗对话系统
    /// 让敌人在战斗中根据行动说出不同的话
    /// </summary>
    public class EnemyBattleDialogue : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] private GameObject dialogueBubble;          // 对话气泡
        [SerializeField] private TMP_Text dialogueText;              // 对话文本
        [SerializeField] private float dialogueDisplayDuration = 2f; // 对话显示时间

        [Header("组件引用")]
        [SerializeField] private EnemyBattleEntity enemyEntity;
        [SerializeField] private EnemyVisualEffects visualEffects;

        [Header("对话配置")]
        [SerializeField] private EnemyDialogueLine[] attackLines;    // 攻击时的台词
        [SerializeField] private EnemyDialogueLine[] blockLines;     // 格挡时的台词
        [SerializeField] private EnemyDialogueLine[] healLines;      // 回血时的台词
        [SerializeField] private EnemyDialogueLine[] debuffLines;    // 施加debuff时的台词
        [SerializeField] private EnemyDialogueLine[] lowHealthLines; // 低血量时的台词
        [SerializeField] private EnemyDialogueLine[] deathLines;     // 死亡时的台词

        private Coroutine dialogueCoroutine;

        private void Awake()
        {
            if (enemyEntity == null)
                enemyEntity = GetComponent<EnemyBattleEntity>();

            if (visualEffects == null)
                visualEffects = GetComponent<EnemyVisualEffects>();

            // 隐藏对话气泡
            if (dialogueBubble != null)
                dialogueBubble.SetActive(false);

            // 如果没有配置台词，初始化默认台词
            InitializeDefaultLines();
        }

        /// <summary>
        /// 初始化默认台词（用于测试）
        /// </summary>
        private void InitializeDefaultLines()
        {
            if (attackLines == null || attackLines.Length == 0)
            {
                attackLines = new EnemyDialogueLine[]
                {
                    new EnemyDialogueLine { text = "看招！", duration = 1.5f },
                    new EnemyDialogueLine { text = "受死吧！", duration = 1.5f },
                    new EnemyDialogueLine { text = "接招！", duration = 1.5f }
                };
            }

            if (blockLines == null || blockLines.Length == 0)
            {
                blockLines = new EnemyDialogueLine[]
                {
                    new EnemyDialogueLine { text = "没用的！", duration = 1.5f },
                    new EnemyDialogueLine { text = "休想！", duration = 1.5f }
                };
            }

            if (healLines == null || healLines.Length == 0)
            {
                healLines = new EnemyDialogueLine[]
                {
                    new EnemyDialogueLine { text = "恢复中...", duration = 1.5f },
                    new EnemyDialogueLine { text = "还没结束！", duration = 1.5f }
                };
            }

            if (debuffLines == null || debuffLines.Length == 0)
            {
                debuffLines = new EnemyDialogueLine[]
                {
                    new EnemyDialogueLine { text = "尝尝这个！", duration = 1.5f },
                    new EnemyDialogueLine { text = "诅咒你！", duration = 1.5f }
                };
            }

            if (lowHealthLines == null || lowHealthLines.Length == 0)
            {
                lowHealthLines = new EnemyDialogueLine[]
                {
                    new EnemyDialogueLine { text = "可恶...", duration = 2f },
                    new EnemyDialogueLine { text = "别小看我！", duration = 2f }
                };
            }

            if (deathLines == null || deathLines.Length == 0)
            {
                deathLines = new EnemyDialogueLine[]
                {
                    new EnemyDialogueLine { text = "不...", duration = 2f }
                };
            }
        }

        /// <summary>
        /// 根据行动类型播放对话
        /// </summary>
        public void PlayDialogueByIntent(EnemyIntentType intentType)
        {
            EnemyDialogueLine line = null;

            switch (intentType)
            {
                case EnemyIntentType.Attack:
                case EnemyIntentType.MultiAttack:
                case EnemyIntentType.SpecialAttack:
                    line = GetRandomLine(attackLines);
                    break;

                case EnemyIntentType.Block:
                    line = GetRandomLine(blockLines);
                    break;

                case EnemyIntentType.Heal:
                    line = GetRandomLine(healLines);
                    break;

                case EnemyIntentType.DebuffPlayer:
                    line = GetRandomLine(debuffLines);
                    break;
            }

            if (line != null)
            {
                PlayDialogue(line.text, line.duration);
            }
        }

        /// <summary>
        /// 播放低血量对话
        /// </summary>
        public void PlayLowHealthDialogue()
        {
            EnemyDialogueLine line = GetRandomLine(lowHealthLines);
            if (line != null)
            {
                PlayDialogue(line.text, line.duration);
            }
        }

        /// <summary>
        /// 播放死亡对话
        /// </summary>
        public void PlayDeathDialogue()
        {
            EnemyDialogueLine line = GetRandomLine(deathLines);
            if (line != null)
            {
                PlayDialogue(line.text, line.duration);
            }
        }

        /// <summary>
        /// 播放对话
        /// </summary>
        public void PlayDialogue(string text, float duration = -1f)
        {
            if (dialogueBubble == null || dialogueText == null) return;

            if (duration <= 0f)
                duration = dialogueDisplayDuration;

            // 停止之前的对话
            if (dialogueCoroutine != null)
            {
                StopCoroutine(dialogueCoroutine);
            }

            dialogueCoroutine = StartCoroutine(ShowDialogueRoutine(text, duration));
        }

        /// <summary>
        /// 显示对话协程
        /// </summary>
        private IEnumerator ShowDialogueRoutine(string text, float duration)
        {
            // 显示对话气泡
            dialogueBubble.SetActive(true);
            dialogueText.text = text;

            // 播放视觉效果
            if (visualEffects != null)
            {
                visualEffects.PlayIntentChangeEffect();
            }

            yield return new WaitForSeconds(duration);

            // 隐藏对话气泡
            dialogueBubble.SetActive(false);
        }

        /// <summary>
        /// 随机获取一行对话
        /// </summary>
        private EnemyDialogueLine GetRandomLine(EnemyDialogueLine[] lines)
        {
            if (lines == null || lines.Length == 0) return null;
            return lines[Random.Range(0, lines.Length)];
        }
    }

    /// <summary>
    /// 敌人对话行
    /// </summary>
    [System.Serializable]
    public class EnemyDialogueLine
    {
        [TextArea(1, 3)]
        public string text;              // 对话文本
        public float duration = 2f;      // 显示时间
        public float weight = 1f;        // 出现权重
    }
}
