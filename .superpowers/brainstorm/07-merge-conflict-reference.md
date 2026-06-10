# 07 — 冲突合并参考：修改函数完整清单

> 本文件记录所有被精准防御特效系统修改/新建的函数完整代码。
> 拉取最新 git 后遇到冲突时，按此文件逐函数对照合并。

---

## 1. ParryEvents.cs（新建，无冲突风险）

路径：`Assets/Scripts/PerfectParry/ParryEvents.cs`

整个文件为新建，不存在冲突。如果远程分支也在 `Assets/Scripts/Battle/Events/` 下创建了同名文件，保留 `Assets/Scripts/PerfectParry/ParryEvents.cs` 即可。

---

## 2. BattleCombatResolver.cs（修改）

路径：`Assets/Scripts/Core/BattleCombatResolver.cs`

### 修改的函数：`EvaluateParryAndApplyDamage`

**改动点**：在判定分支之前新增 eventData 构建，每个分支末尾追加一行 `ParryEvents.Fire*()` 调用。

```csharp
public void EvaluateParryAndApplyDamage(int hitIndex, EnemyAttackSequence seq)
{
    if (playerParty == null || playerParty.Count == 0 || playerParty[0] == null) return;

    PlayerBattleEntity defender = playerParty[0];
    var turn = BattleTurnManager.Instance;

    const float PerfectWindow = 0.12f;
    const float NormalWindow  = 0.30f;

    int rawDamage = seq.hitDamages[hitIndex];
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
        ParryEvents.FireParryFailed(eventData);
    }
    else if (timeDiff < 0f)
    {
        turn.allPerfectParriesInCurrentAttack = false;
        Debug.Log($"{debugHeader}<color=red>格挡失败！你按晚了 {Mathf.Abs(rawDiffMs):F0} 毫秒！</color>");
        ApplyDamageFeedback(defender, rawDamage, breakDamage, isPerfect: false, isNormal: false);
        ParryEvents.FireParryFailed(eventData);
    }
    else if (timeDiff <= PerfectWindow)
    {
        Debug.Log($"{debugHeader}<color=green>【完美格挡成功】你提前 {rawDiffMs:F0} 毫秒按下了空格</color>");
        ApplyDamageFeedback(defender, 0, 0, isPerfect: true, isNormal: false);
        ParryEvents.FirePerfectParry(eventData);
    }
    else if (timeDiff <= NormalWindow)
    {
        turn.allPerfectParriesInCurrentAttack = false;
        int reducedDamage = Mathf.RoundToInt(rawDamage * 0.3f);
        Debug.Log($"{debugHeader}<color=yellow>[普通格挡] 你提前 {rawDiffMs:F0} 毫秒按下了空格</color>");
        ApplyDamageFeedback(defender, reducedDamage, 0, isPerfect: false, isNormal: true);
        ParryEvents.FireNormalParry(eventData);
    }
    else
    {
        turn.allPerfectParriesInCurrentAttack = false;
        Debug.Log($"{debugHeader}<color=red>格挡失败！你按太早了，提前了 {rawDiffMs:F0} 毫秒！</color>");
        ApplyDamageFeedback(defender, rawDamage, breakDamage, isPerfect: false, isNormal: false);
        ParryEvents.FireParryFailed(eventData);
    }

    CheckBattleOver();
}
```

**冲突合并策略**：如果远程修改了此函数的判定逻辑（如窗口值、伤害计算），先合并远程的判定逻辑改动，再在每个分支末尾追加 `ParryEvents.Fire*()` 行。eventData 构建块放在 `debugHeader` 之后、闪避判定之前，位置固定。

---

## 3. PerfectParry.cs（新建→改为动态实例化模式→交错播放序列）

路径：`Assets/Scripts/PerfectParry/PerfectParry.cs`

**变更**：从"预实例化+事件订阅"模式改为"按需实例化+播完销毁"模式。**v2 进一步将粒子播放从"三粒子同时播放"改为"渐进式交错喷洒"**，视觉上更有层次感。

```csharp
public class PerfectParry : MonoBehaviour
{
    // ---- 缓存三个粒子系统（Awake 时自动查找）----
    private ParticleSystem splashParticle;
    private ParticleSystem brustParticle;
    private ParticleSystem endParticle;

    private bool isPlaying;
    private Coroutine finishCoroutine;

    private void Awake()
    {
        splashParticle = transform.Find("SplashParticle")?.GetComponent<ParticleSystem>();
        brustParticle  = transform.Find("BrustParticle")?.GetComponent<ParticleSystem>();
        endParticle    = transform.Find("EndParticle")?.GetComponent<ParticleSystem>();

        // 强制三个粒子使用 UnscaledTime，顿帧期间照常播放
        ForceUnscaledTime(splashParticle);
        ForceUnscaledTime(brustParticle);
        ForceUnscaledTime(endParticle);

        // 确保初始不播放
        StopAllParticles(clearImmediate: false);
    }

    // ========== 播放逻辑 ==========

    /// <summary>播放一次完整粒子序列，播完后自动销毁自身</summary>
    /// <remarks>
    /// 渐进式喷洒效果：Brust(0s) → 0.15s → Splash → 0.15s → End
    /// </remarks>
    public void PlayAndDestroy()
    {
        if (isPlaying) return;
        isPlaying = true;

        // 清理旧残留粒子（立即清除），然后从头播放
        StopAllParticles(clearImmediate: true);

        if (finishCoroutine != null) StopCoroutine(finishCoroutine);
        finishCoroutine = StartCoroutine(SequenceAndDestroy());
    }

    private IEnumerator SequenceAndDestroy()
    {
        // 第 1 段：Brust 爆发
        if (brustParticle != null) brustParticle.Play();
        yield return new WaitForSecondsRealtime(0.15f);

        // 第 2 段：Splash 溅射
        if (splashParticle != null) splashParticle.Play();
        yield return new WaitForSecondsRealtime(0.15f);

        // 第 3 段：End 收尾
        if (endParticle != null) endParticle.Play();

        // 等待粒子自然消散（最长的 EndParticle 约 3.23s）
        yield return new WaitForSecondsRealtime(3.2f);

        Finish();
        Destroy(gameObject);
    }

    /// <summary>
    /// 停止发射新粒子，已生成的粒子自然消散（不强制清除）。
    /// </summary>
    private void Finish()
    {
        StopAllParticles(clearImmediate: false);
        isPlaying = false;
        finishCoroutine = null;
    }

    // ========== 工具方法 ==========

    private void StopAllParticles(bool clearImmediate) { /* ... */ }
    private static void ForceUnscaledTime(ParticleSystem ps) { /* ... */ }
}
```

**冲突合并策略**：整个文件为新建，不存在冲突。如果远程有同名文件，保留此版本（含 `SequenceAndDestroy` 交错播放版本）。

**关键设计约束**：
- 三个粒子顺序播放：Brust(0s) → Splash(+0.15s) → End(+0.30s)，总等待 3.2s 后销毁
- 全部使用 `WaitForSecondsRealtime`，不受 `TimeScale=0.01` 冻结影响
- `Finish()` 方法使用 `StopEmitting`（不清除已发射粒子），允许已有粒子自然消散
- 新增的 `Finish()` 方法负责重置 `isPlaying` 和 `finishCoroutine`，供未来可能的重播复用

---

## 4. ShaderEffectController.cs（修改）

路径：`Assets/Scripts/Battle/Base/ShaderEffectController.cs`

### 新增常量 & 字段

```csharp
private const string KW_PARRY_DEFENDER = "_PARRY_DEFENDER_ON";
private const float ParryGlowDuration = 0.3f;

// 独立协程句柄，与现有 _activeEffect（受伤闪光/死亡消融）互不干扰
private Coroutine _activeParryGlow;
```

**冲突合并策略**：这些新增行插在 `KW_DEATH` 常量和 `_activeEffect` 字段之后。如果远程新增了其他常量/字段，按声明顺序排列即可，互不干扰。

### 新增方法：`OnEnable` / `OnDisable`

```csharp
private void OnEnable()
{
    ParryEvents.OnPerfectParry += HandleParryGlow;
    ParryEvents.OnNormalParry  += HandleParryGlow;
}

private void OnDisable()
{
    ParryEvents.OnPerfectParry -= HandleParryGlow;
    ParryEvents.OnNormalParry  -= HandleParryGlow;
}
```

**冲突合并策略**：如果远程也新增了 `OnEnable`/`OnDisable`（如订阅其他事件），合并两个订阅列表到同一个方法中。

### 新增方法：`HandleParryGlow` / `PlayParryDefenderGlow` / `ParryGlowRoutine`

```csharp
/// <summary>
/// 事件响应：精准防御/普通防御共用同一个发光 handler。
/// 安全校验用 GetComponentInParent<PlayerBattleEntity>()，无视层级差异。
/// </summary>
private void HandleParryGlow(ParryEventData data)
{
    var myEntity = GetComponentInParent<PlayerBattleEntity>();
    if (myEntity == null || myEntity != data.Defender) return;

    PlayParryDefenderGlow();
}

/// <summary>启用精准防御发光，0.3s 真实时间后自动关闭</summary>
public void PlayParryDefenderGlow()
{
    if (_activeParryGlow != null) StopCoroutine(_activeParryGlow);
    _activeParryGlow = StartCoroutine(ParryGlowRoutine());
}

private IEnumerator ParryGlowRoutine()
{
    _materialInstance.EnableKeyword(KW_PARRY_DEFENDER);

    // 用 WaitForSecondsRealtime，不受顿帧 TimeScale=0.01 影响
    yield return new WaitForSecondsRealtime(ParryGlowDuration);

    _materialInstance.DisableKeyword(KW_PARRY_DEFENDER);
    _activeParryGlow = null;
}
```

**冲突合并策略**：这三个方法是全新增的，插在 `HitFlashRoutine` 之前。如果远程新增了其他方法，按功能分区排列即可。

### 修改方法：`ResetEffect`

```csharp
public void ResetEffect()
{
    if (_activeEffect != null)
    {
        StopCoroutine(_activeEffect);
        _activeEffect = null;
    }
    _materialInstance.DisableKeyword(KW_INJURED);
    _materialInstance.DisableKeyword(KW_DEATH);
    _materialInstance.SetFloat(ID_DissolveProgress, 0f);
    _materialInstance.SetFloat(ID_FlashIntensity, 0f);

    // ★ 新增：清理精准防御发光
    _materialInstance.DisableKeyword(KW_PARRY_DEFENDER);
    if (_activeParryGlow != null)
    {
        StopCoroutine(_activeParryGlow);
        _activeParryGlow = null;
    }
}
```

**冲突合并策略**：如果远程也修改了 `ResetEffect`（如新增其他 keyword 清理），先合并远程的清理逻辑，再在末尾追加 `_PARRY_DEFENDER_ON` 清理块。

---

## 5. BattleEffectManager.cs（修改）

路径：`Assets/Scripts/Core/BattleEffectManager.cs`

### 新增字段

```csharp
// ---- 协程句柄缓存 ----
private Coroutine _activeCameraShake;
private Coroutine _activeWitchTime;
private Coroutine _activeHitStop;
private Coroutine _activePerfectParryFreeze;
```

**冲突合并策略**：插在 `Awake` 方法之后。如果远程新增了其他字段，按声明顺序排列即可。

### 新增方法：`OnEnable` / `OnDisable`

```csharp
private void OnEnable()
{
    ParryEvents.OnPerfectParry += HandlePerfectParryFreeze;
}

private void OnDisable()
{
    ParryEvents.OnPerfectParry -= HandlePerfectParryFreeze;
}
```

**冲突合并策略**：如果远程也新增了 `OnEnable`/`OnDisable`，合并订阅列表到同一个方法中。

### 修改方法：`WitchTime` / `WitchTimeRoutine`

```csharp
public void WitchTime(float duration)
{
    StopAllTimeScaleCoroutines();
    _activeWitchTime = StartCoroutine(WitchTimeRoutine(duration));
}

private IEnumerator WitchTimeRoutine(float duration)
{
    Time.timeScale = 0.2f;
    yield return new WaitForSecondsRealtime(duration);
    Time.timeScale = 1.0f;
    _activeWitchTime = null;
}
```

**冲突合并策略**：如果远程修改了 WitchTime 的 duration 或 timeScale 值，保留远程的数值改动，但必须保留 `StopAllTimeScaleCoroutines()` 调用和 `_activeWitchTime` 句柄缓存 + 末尾 `_activeWitchTime = null` 清理。

### 修改方法：`ShakeCamera` / `CameraShakeRoutine`

```csharp
public void ShakeCamera(float duration, float magnitude)
{
    if (_activeCameraShake != null) StopCoroutine(_activeCameraShake);
    _activeCameraShake = StartCoroutine(CameraShakeRoutine(duration, magnitude));
}

private IEnumerator CameraShakeRoutine(float duration, float magnitude)
{
    Camera battleCam = Camera.main;
    if (battleCam == null) { _activeCameraShake = null; yield break; }

    Vector3 originalPos = battleCam.transform.position;
    float elapsed = 0f;

    while (elapsed < duration)
    {
        float x = Random.Range(-1f, 1f) * magnitude;
        float y = Random.Range(-1f, 1f) * magnitude;
        battleCam.transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
        elapsed += Time.deltaTime;
        yield return null;
    }

    battleCam.transform.position = originalPos;
    _activeCameraShake = null;
}
```

**冲突合并策略**：如果远程修改了 CameraShakeRoutine 的抖动算法，保留远程的算法改动，但必须保留 `_activeCameraShake` 句柄缓存 + `yield break` 时 `_activeCameraShake = null` + 末尾 `_activeCameraShake = null` 清理。`ShakeCamera` 入口处的 `if (_activeCameraShake != null) StopCoroutine(_activeCameraShake)` 也必须保留。

### 修改方法：`HitStop` / `HitStopRoutine`

```csharp
public void HitStop(float duration)
{
    StopAllTimeScaleCoroutines();
    _activeHitStop = StartCoroutine(HitStopRoutine(duration));
}

private IEnumerator HitStopRoutine(float duration)
{
    Time.timeScale = 0.05f;
    yield return new WaitForSecondsRealtime(duration);
    Time.timeScale = 1.0f;
    _activeHitStop = null;
}
```

**冲突合并策略**：同 WitchTime，保留远程数值改动，但必须保留 `StopAllTimeScaleCoroutines()` 调用和 `_activeHitStop` 句柄缓存 + 末尾 `_activeHitStop = null` 清理。

### 新增方法：`HandlePerfectParryFreeze`

```csharp
private void HandlePerfectParryFreeze(ParryEventData data)
{
    // ★ P0 修正：先停止现有 CameraShake（避免顿帧后"延迟爆发"）
    if (_activeCameraShake != null)
    {
        StopCoroutine(_activeCameraShake);
        _activeCameraShake = null;
    }

    PerfectParryFreeze(0.4f);
    PerfectParryShake(0.15f, 0.08f);
}
```

### 新增方法：`PerfectParryFreeze` / `PerfectParryFreezeRoutine`

```csharp
public void PerfectParryFreeze(float realDuration)
{
    StopAllTimeScaleCoroutines();
    _activePerfectParryFreeze = StartCoroutine(PerfectParryFreezeRoutine(realDuration));
}

private IEnumerator PerfectParryFreezeRoutine(float realDuration)
{
    Time.timeScale = 0.01f;
    yield return new WaitForSecondsRealtime(realDuration);
    Time.timeScale = 1.0f;
    _activePerfectParryFreeze = null;
}
```

### 新增方法：`PerfectParryShake` / `PerfectParryShakeRoutine`

```csharp
public void PerfectParryShake(float duration, float magnitude)
{
    _activeCameraShake = StartCoroutine(PerfectParryShakeRoutine(duration, magnitude));
}

private IEnumerator PerfectParryShakeRoutine(float duration, float magnitude)
{
    Camera battleCam = Camera.main;
    if (battleCam == null) { _activeCameraShake = null; yield break; }

    // ★ 注意：originalPos 在协程开始时快照一次。
    // 如果相机在顿帧期间还有动态跟随逻辑，originalPos 会是抖动开始时的位置，
    // 结束后会硬 snap 回去。当前战斗相机无动态跟随，不受影响。
    Vector3 originalPos = battleCam.transform.position;
    float elapsed = 0f;

    while (elapsed < duration)
    {
        float x = Random.Range(-1f, 1f) * magnitude;
        float y = Random.Range(-1f, 1f) * magnitude;
        battleCam.transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
        elapsed += Time.unscaledDeltaTime; // ★ 关键：不受 TimeScale 影响
        yield return null;
    }

    battleCam.transform.position = originalPos;
    _activeCameraShake = null;
}
```

### 新增方法：`StopAllTimeScaleCoroutines`

```csharp
private void StopAllTimeScaleCoroutines()
{
    if (_activeWitchTime != null)
    {
        StopCoroutine(_activeWitchTime);
        _activeWitchTime = null;
    }
    if (_activeHitStop != null)
    {
        StopCoroutine(_activeHitStop);
        _activeHitStop = null;
    }
    if (_activePerfectParryFreeze != null)
    {
        StopCoroutine(_activePerfectParryFreeze);
        _activePerfectParryFreeze = null;
    }
    // ★ P1 修正：删除 Time.timeScale = 1.0f
}
```

**冲突合并策略**：如果远程新增了其他 TimeScale 协程（如 SuperSlowMotion），在 `StopAllTimeScaleCoroutines` 中追加对应的句柄中断。**绝对不要**在此方法中加 `Time.timeScale = 1.0f`，这是 P1 修正的核心。

---

## 6. BattleManager.cs（修改）

路径：`Assets/Scripts/Core/BattleManager.cs`

### 修改位置：`StartBattleRoutine` 方法内，步骤 9→10 之间

**改动点**：移除 PerfectParry 预实例化（已改为动态按需生成），仅保留 ShaderEffectController 动态添加。

```csharp
        // 9. 激活战斗组件
        foreach (var member in playerParty)
        {
            if (member == null) continue;
            member.enabled = true;

            var pc = member.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;

            member.GetBattleStateMachine()?.ChangeState<PlayerBattleIdleState>();
            member.currentAP = 3;
        }

        // ★ 新增：确保玩家身上有 ShaderEffectController（挂到 sprite 子物体上）
        if (playerParty.Count > 0 && playerParty[0] != null)
        {
            if (playerParty[0].sprite != null)
            {
                var spriteGO = playerParty[0].sprite.gameObject;
                if (spriteGO.GetComponent<ShaderEffectController>() == null)
                {
                    spriteGO.AddComponent<ShaderEffectController>();
                    Debug.Log("[ShaderEffectController] 已动态添加到玩家 sprite 子物体");
                }
            }
        }

        // 10. 克隆队友
        if (PartyManager.Instance != null)
        {
            // ...
```

**冲突合并策略**：如果远程在步骤 9→10 之间也有改动，将上述 ShaderEffectController 添加块合并到远程改动之后。

---

## 7. BattleEffectManager.cs（修改——新增 SpawnPerfectParry）

路径：`Assets/Scripts/Core/BattleEffectManager.cs`

### 修改方法：`HandlePerfectParryFreeze`

```csharp
private void HandlePerfectParryFreeze(ParryEventData data)
{
    // ---- 实例化 PerfectParry 粒子到玩家 eff 子物体 ----
    SpawnPerfectParry(data.Defender);

    // ★ P0 修正：先停止现有 CameraShake
    if (_activeCameraShake != null)
    {
        StopCoroutine(_activeCameraShake);
        _activeCameraShake = null;
    }

    PerfectParryFreeze(0.4f);
    PerfectParryShake(0.15f, 0.08f);
}
```

### 新增方法：`SpawnPerfectParry`

```csharp
private void SpawnPerfectParry(PlayerBattleEntity defender)
{
    if (defender == null) return;

    Transform eff = defender.transform.Find("eff");
    if (eff == null)
    {
        Debug.LogWarning("[PerfectParry] 玩家身上未找到 eff 子物体，回退到根节点");
        eff = defender.transform;
    }

    var prefab = Resources.Load<GameObject>("Prefab/PerfectParry");
    if (prefab == null)
    {
        Debug.LogWarning("[PerfectParry] 未找到 Prefab/PerfectParry 预制体");
        return;
    }

    var go = Instantiate(prefab, eff);
    go.name = "PerfectParry_Effect";

    var controller = go.GetComponent<PerfectParry>();
    if (controller == null)
        controller = go.AddComponent<PerfectParry>();

    controller.PlayAndDestroy();
}
```

**冲突合并策略**：`SpawnPerfectParry` 是全新增方法，放在 `PerfectParryShakeRoutine` 之后、`StopAllTimeScaleCoroutines` 之前即可。