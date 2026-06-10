# 02 — 发射端：BattleCombatResolver 改动

> 修改文件：`Assets/Scripts/Core/BattleCombatResolver.cs`
> 改动量：+15 行，零删除

## 改动原则

- 现有 `ApplyDamageFeedback`（FlashColor / ShakeCamera / HitStop）**全部保留不动**
- 事件发射是**追加**在判定分支末尾，不替换任何现有逻辑
- BattleCombatResolver 只负责"告诉世界发生了什么"，不关心谁在听

## 核心代码逻辑

### EvaluateParryAndApplyDamage 方法改动

```csharp
public void EvaluateParryAndApplyDamage(int hitIndex, EnemyAttackSequence seq)
{
    if (playerParty == null || playerParty.Count == 0 || playerParty[0] == null) return;

    PlayerBattleEntity defender = playerParty[0];
    var turn = BattleTurnManager.Instance;

    const float PerfectWindow = 0.12f;
    const float NormalWindow  = 0.30f;

    int rawDamage   = seq.hitDamages[hitIndex];
    int breakDamage = seq.hitBreakDamages[hitIndex];

    string debugHeader = $"[攻防判定] 第 {hitIndex + 1} 次攻击   ———————————————\n";

    // ★ 新增：构建事件数据（在判定分支之前，一次性构建）
    var attacker = BattleTurnManager.Instance.CurrentAttacker;
    var eventData = new ParryEventData(
        defender:  defender,
        attacker:  attacker,
        hitPoint:  attacker != null ? attacker.transform.position : defender.transform.position,
        hitIndex:  hitIndex,
        rawDamage: rawDamage
    );

    // ---- 阶段 1：闪避判定（不变）----
    if (defender.GetBattleStateMachine().currentState is PlayerBattleDodgeState)
    {
        HandleDodge(defender, turn);
        CheckBattleOver();
        return;
    }

    // ---- 阶段 2：格挡判定 ----
    float hitTime       = Time.time;
    float parryPressTime = defender.GetParryPressTime();
    float timeDiff      = hitTime - parryPressTime;
    float rawDiffMs     = timeDiff * 1000f;

    if (parryPressTime <= -99f)
    {
        // 未按键
        turn.allPerfectParriesInCurrentAttack = false;
        Debug.Log($"{debugHeader}<color=red>警告：未检测到任何按键，直接全吃伤害！</color>");
        ApplyDamageFeedback(defender, rawDamage, breakDamage, isPerfect: false, isNormal: false);
        ParryEvents.FireParryFailed(eventData);    // ★ 新增
    }
    else if (timeDiff < 0f)
    {
        // 按晚了
        turn.allPerfectParriesInCurrentAttack = false;
        Debug.Log($"{debugHeader}<color=red>格挡失败！你按晚了 {Mathf.Abs(rawDiffMs):F0} 毫秒！</color>");
        ApplyDamageFeedback(defender, rawDamage, breakDamage, isPerfect: false, isNormal: false);
        ParryEvents.FireParryFailed(eventData);    // ★ 新增
    }
    else if (timeDiff <= PerfectWindow)
    {
        // ★ 完美格挡
        Debug.Log($"{debugHeader}<color=green>【完美格挡成功】你提前 {rawDiffMs:F0} 毫秒按下了空格</color>");
        ApplyDamageFeedback(defender, 0, 0, isPerfect: true, isNormal: false);
        ParryEvents.FirePerfectParry(eventData);   // ★ 新增
    }
    else if (timeDiff <= NormalWindow)
    {
        // ★ 普通格挡
        turn.allPerfectParriesInCurrentAttack = false;
        int reducedDamage = Mathf.RoundToInt(rawDamage * 0.3f);
        Debug.Log($"{debugHeader}<color=yellow>[普通格挡] 你提前 {rawDiffMs:F0} 毫秒按下了空格</color>");
        ApplyDamageFeedback(defender, reducedDamage, 0, isPerfect: false, isNormal: true);
        ParryEvents.FireNormalParry(eventData);    // ★ 新增
    }
    else
    {
        // 按太早
        turn.allPerfectParriesInCurrentAttack = false;
        Debug.Log($"{debugHeader}<color=red>格挡失败！你按太早了，提前了 {rawDiffMs:F0} 毫秒！</color>");
        ApplyDamageFeedback(defender, rawDamage, breakDamage, isPerfect: false, isNormal: false);
        ParryEvents.FireParryFailed(eventData);    // ★ 新增
    }

    CheckBattleOver();
}
```

## 改动总结

- `eventData` 在分支前构建一次，避免每个分支重复代码
- 每个分支末尾追加一行 `ParryEvents.Fire*()` 调用
- `hitPoint` 取攻击方坐标，未来可用于粒子偏移
- `ApplyDamageFeedback` 内部代码**零改动**
