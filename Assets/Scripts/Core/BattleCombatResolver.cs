using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻防判定解析器
/// 负责格挡/闪避判定、伤害结算、胜负裁定。
/// 纯逻辑模块，不持有持久状态，所有状态由 BattleTurnManager 管理。
/// </summary>
public class BattleCombatResolver : MonoBehaviour
{
    private static BattleCombatResolver _instance;
    public static BattleCombatResolver Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BattleCombatResolver>();
                if (_instance == null)
                {
                    var go = new GameObject("[BattleCombatResolver]");
                    _instance = go.AddComponent<BattleCombatResolver>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 外部注入引用（由 BattleManager 在 StartBattle 时设置）
    public List<PlayerBattleEntity> playerParty { get; set; }
    public List<EnemyBattleEntity> activeEnemies { get; set; }

    // ============================================
    // 攻防判定主入口
    // ============================================

    /// <summary>
    /// 供怪物动画事件调用。根据当前动画帧与玩家按键的时间差进行格挡/闪避判定。
    /// </summary>
    public void EvaluateParryAndApplyDamage(int hitIndex, EnemyAttackSequence seq)
    {
        if (playerParty == null || playerParty.Count == 0 || playerParty[0] == null) return;

        PlayerBattleEntity defender = playerParty[0];
        var turn = BattleTurnManager.Instance;

        const float PerfectWindow = 0.12f;
        const float NormalWindow = 0.30f;

        int rawDamage = seq.hitDamages[hitIndex];
        int breakDamage = seq.hitBreakDamages[hitIndex];

        string debugHeader = $"[攻防判定] 第 {hitIndex + 1} 次攻击   ———————————————\n";

        // ---- 阶段 1：闪避判定 ----
        if (defender.GetBattleStateMachine().currentState is PlayerBattleDodgeState)
        {
            HandleDodge(defender, turn);
            CheckBattleOver();
            return;
        }

        // ---- 阶段 2：格挡判定 ----
        float hitTime = Time.time;
        float parryPressTime = defender.GetParryPressTime();
        float timeDiff = hitTime - parryPressTime;
        float rawDiffMs = timeDiff * 1000f;

        if (parryPressTime <= -99f)
        {
            turn.allPerfectParriesInCurrentAttack = false;
            Debug.Log($"{debugHeader}<color=red>警告：未检测到任何按键，直接全吃伤害！</color>");
            ApplyDamageFeedback(defender, rawDamage, breakDamage, isPerfect: false, isNormal: false);
        }
        else if (timeDiff < 0f)
        {
            turn.allPerfectParriesInCurrentAttack = false;
            Debug.Log($"{debugHeader}<color=red>格挡失败！你按晚了 {Mathf.Abs(rawDiffMs):F0} 毫秒！</color>");
            ApplyDamageFeedback(defender, rawDamage, breakDamage, isPerfect: false, isNormal: false);
        }
        else if (timeDiff <= PerfectWindow)
        {
            Debug.Log($"{debugHeader}<color=green>【完美格挡成功】你提前 {rawDiffMs:F0} 毫秒按下了空格</color>");
            ApplyDamageFeedback(defender, 0, 0, isPerfect: true, isNormal: false);
        }
        else if (timeDiff <= NormalWindow)
        {
            turn.allPerfectParriesInCurrentAttack = false;
            int reducedDamage = Mathf.RoundToInt(rawDamage * 0.3f);
            Debug.Log($"{debugHeader}<color=yellow>[普通格挡] 你提前 {rawDiffMs:F0} 毫秒按下了空格</color>");
            ApplyDamageFeedback(defender, reducedDamage, 0, isPerfect: false, isNormal: true);
        }
        else
        {
            turn.allPerfectParriesInCurrentAttack = false;
            Debug.Log($"{debugHeader}<color=red>格挡失败！你按太早了，提前了 {rawDiffMs:F0} 毫秒！</color>");
            ApplyDamageFeedback(defender, rawDamage, breakDamage, isPerfect: false, isNormal: false);
        }

        CheckBattleOver();
    }

    // ============================================
    // 闪避处理
    // ============================================

    private void HandleDodge(PlayerBattleEntity defender, BattleTurnManager turn)
    {
        turn.allPerfectParriesInCurrentAttack = false;

        float dodgeTimeDiff = Time.time - defender.GetDodgePressTime();
        float dodgeDiffMs = dodgeTimeDiff * 1000f;
        const float PerfectDodgeWindow = 0.12f;

        if (dodgeTimeDiff >= 0f && dodgeTimeDiff <= PerfectDodgeWindow)
        {
            Debug.Log($"[攻防判定] 第 X 次攻击   ———————————————\n<color=lime>【完美闪避】免疫伤害！时间差: {dodgeDiffMs:F0} 毫秒。</color>");

            BattleEffectManager.Instance.WitchTime(0.25f);
            defender.FlashColor(new Color(0.2f, 1.0f, 0.4f), 0.15f);
            BattleEffectManager.Instance.ShakeCamera(0.12f, 0.08f);

            if (!turn.hasRestoredDodgeApThisRound)
            {
                turn.hasRestoredDodgeApThisRound = true;
                var res = BattleResourceManager.Instance;
                res.sharedAP = Mathf.Min(res.sharedAP + 1, res.maxSharedAP);
                BattleUIController.Instance?.RefreshUI();
            }
        }
        else
        {
            Debug.Log($"[攻防判定] 第 X 次攻击   ———————————————\n<color=cyan>[普通闪避] 成功免疫伤害。时间差: {dodgeDiffMs:F0} 毫秒。</color>");
            defender.FlashColor(new Color(1f, 1f, 1f, 0.4f), 0.12f);
        }

        defender.UseDodgeInput();
    }

    // ============================================
    // 伤害反馈
    // ============================================

    private void ApplyDamageFeedback(PlayerBattleEntity defender, int finalDamage, int breakDamage, bool isPerfect, bool isNormal)
    {
        var effect = BattleEffectManager.Instance;

        if (isPerfect)
        {
            defender.FlashColor(Color.cyan, 0.5f);
            effect.ShakeCamera(0.2f, 0.25f);
            effect.HitStop(0.06f);
        }
        else if (isNormal)
        {
            defender.ReceiveAttack(finalDamage, breakDamage);
            defender.FlashColor(new Color(0.8f, 0.8f, 0.8f), 0.1f);
            effect.ShakeCamera(0.12f, 0.08f);
        }
        else
        {
            defender.ReceiveAttack(finalDamage, breakDamage);
            effect.ShakeCamera(0.3f, 0.15f);
        }
    }

    // ============================================
    // 胜负裁定
    // ============================================

    /// <summary>检查场上存活状态，实时判定胜负</summary>
    public void CheckBattleOver()
    {
        bool isPlayerDead = CheckPlayerDead();
        bool isAllEnemiesDead = CheckAllEnemiesDead();

        Debug.Log($"[胜负自检] 战场存活状态 | 是否阵亡: {isPlayerDead} | 敌人全灭: {isAllEnemiesDead}");

        if (isPlayerDead)
        {
            Debug.Log("<color=red>[胜负裁定] 玩家生命归零，判定为：战斗失败</color>");

            if (playerParty?.Count > 0 && playerParty[0] != null)
            {
                var fsm = playerParty[0].GetBattleStateMachine();
                if (fsm != null && !(fsm.currentState is PlayerBattleDieState))
                    fsm.ChangeState<PlayerBattleDieState>();
            }

            BattleManager.Instance.EndBattle(isWin: false);
        }
        else if (isAllEnemiesDead)
        {
            Debug.Log("<color=green>[胜负裁定] 敌方全员阵亡，判定为：战斗胜利</color>");
            BattleManager.Instance.EndBattle(isWin: true);
        }
    }

    private bool CheckPlayerDead()
    {
        return playerParty == null || playerParty.Count == 0 || playerParty[0] == null || playerParty[0].Stats.currentHP <= 0;
    }

    private bool CheckAllEnemiesDead()
    {
        if (activeEnemies == null) return true;
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && enemy.Stats.currentHP > 0) return false;
        }
        return true;
    }
}
