# 07 — 冲突合并参考：修改函数完整清单

> 本文件记录所有被精准防御特效系统 **和战斗音效系统** 修改/新建的函数完整代码。
> 拉取最新 git 后遇到冲突时，按此文件逐函数对照合并。

---

## 1. ParryEvents.cs（新建，无冲突风险）

路径：`Assets/Scripts/PerfectParry/ParryEvents.cs`

整个文件为新建，不存在冲突。如果远程分支也在 `Assets/Scripts/Battle/Events/` 下创建了同名文件，保留 `Assets/Scripts/PerfectParry/ParryEvents.cs` 即可。

---

## 2. BattleCombatResolver.cs（修改）

路径：`Assets/Scripts/Core/BattleCombatResolver.cs`

### 修改的函数：`EvaluateParryAndApplyDamage`

**改动点**：在判定分支之前新增 eventData 构建，每个分支末尾追加一行 `ParryEvents.Fire*()` 调用。闪避判定调用 `HandleDodge` 时新增 `rawDamage` 参数。

**第二次修改（2026-06-10）**：闪避事件总线——`HandleDodge` 签名新增 `int rawDamage` 参数，内部新增 `DodgeEvents.FirePerfectDodge / FireNormalDodge` 发射。

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
        HandleDodge(defender, turn, rawDamage);
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

### 修改的函数：`HandleDodge`

**改动点**：2026-06-10 新增闪避事件总线——签名新增 `int rawDamage`，内部构建 `DodgeEventData`，完美/普通闪避分支各自发射 `DodgeEvents.Fire*()`。

```csharp
private void HandleDodge(PlayerBattleEntity defender, BattleTurnManager turn, int rawDamage)
{
    turn.allPerfectParriesInCurrentAttack = false;

    float dodgeTimeDiff = Time.time - defender.GetDodgePressTime();
    float dodgeDiffMs = dodgeTimeDiff * 1000f;
    const float PerfectDodgeWindow = 0.12f;

    // 构建事件数据
    var attacker = turn.CurrentAttacker;
    Vector3 hitPoint = attacker != null ? attacker.transform.position : defender.transform.position;
    var eventData = new DodgeEventData(defender, attacker, hitPoint, isPerfect: false, rawDamage);

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

        DodgeEvents.FirePerfectDodge(eventData);
    }
    else
    {
        Debug.Log($"[攻防判定] 第 X 次攻击   ———————————————\n<color=cyan>[普通闪避] 成功免疫伤害。时间差: {dodgeDiffMs:F0} 毫秒。</color>");
        defender.FlashColor(new Color(1f, 1f, 1f, 0.4f), 0.12f);

        DodgeEvents.FireNormalDodge(eventData);
    }

    defender.UseDodgeInput();
}
```

**冲突合并策略**：如果远程也修改了 `HandleDodge`（如调整闪避窗口值、视觉反馈），先合并远程的判定逻辑改动，再保留最末尾的 `defender.UseDodgeInput()` 和两个 `DodgeEvents.Fire*()` 发射行。如果远程新加了第三种闪避结果（如"完美闪避返还 AP"变体），在新增分支中补 `DodgeEvents.FirePerfectDodge` 即可。

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

// ---- 闪避 (Dodge) ----
private static readonly int ID_DodgeProgress = Shader.PropertyToID("_DodgeProgress");
private const string KW_DODGE = "_DODGE_ON";
private const float DodgeGlowDuration = 0.25f;

// 独立协程句柄，与现有 _activeEffect（受伤闪光/死亡消融）互不干扰
private Coroutine _activeParryGlow;
private Coroutine _activeDodgeGlow;
```

**冲突合并策略**：这些新增行插在 `KW_DEATH` 常量和 `_activeEffect` 字段之后。如果远程新增了其他常量/字段，按声明顺序排列即可，互不干扰。

### 新增方法：`OnEnable` / `OnDisable`

```csharp
private void OnEnable()
{
    ParryEvents.OnPerfectParry += HandleParryGlow;
    ParryEvents.OnNormalParry  += HandleParryGlow;

    DodgeEvents.OnPerfectDodge += HandlePerfectDodge;
    DodgeEvents.OnNormalDodge  += HandleNormalDodge;
}

private void OnDisable()
{
    ParryEvents.OnPerfectParry -= HandleParryGlow;
    ParryEvents.OnNormalParry  -= HandleParryGlow;

    DodgeEvents.OnPerfectDodge -= HandlePerfectDodge;
    DodgeEvents.OnNormalDodge  -= HandleNormalDodge;
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

### 新增方法：`HandlePerfectDodge` / `HandleNormalDodge` / `PlayDodgeGlow` / `DodgeGlowRoutine`

**改动点（2026-06-11）**：闪避 Shader 视觉集成——订阅 `DodgeEvents`，完美闪避触发边缘发光 + 完整闪避特效（顶点膨胀、噪声故障、残影混色），普通闪避仅触发边缘发光。

```csharp
/// <summary>完美闪避：边缘发光 + 完整闪避Shader（顶点膨胀、噪声故障、残影混色、透明度衰减）</summary>
private void HandlePerfectDodge(DodgeEventData data)
{
    var myEntity = GetComponentInParent<PlayerBattleEntity>();
    if (myEntity == null || myEntity != data.Defender) return;

    PlayParryDefenderGlow();
    PlayDodgeGlow();
}

/// <summary>普通闪避：仅边缘发光（复用精准防御的发光效果）</summary>
private void HandleNormalDodge(DodgeEventData data)
{
    var myEntity = GetComponentInParent<PlayerBattleEntity>();
    if (myEntity == null || myEntity != data.Defender) return;

    PlayParryDefenderGlow();
}

/// <summary>启用 _DODGE_ON 关键字，在 DodgeGlowDuration 内将 _DodgeProgress 从 0 驱动到 1</summary>
public void PlayDodgeGlow()
{
    if (_activeDodgeGlow != null) StopCoroutine(_activeDodgeGlow);
    _activeDodgeGlow = StartCoroutine(DodgeGlowRoutine());
}

private IEnumerator DodgeGlowRoutine()
{
    _materialInstance.EnableKeyword(KW_DODGE);
    _materialInstance.SetFloat(ID_DodgeProgress, 0f);

    float elapsed = 0f;
    while (elapsed < DodgeGlowDuration)
    {
        // unscaledDeltaTime: 不受 WitchTime (timeScale=0.2) 影响
        elapsed += Time.unscaledDeltaTime;
        _materialInstance.SetFloat(ID_DodgeProgress, Mathf.Clamp01(elapsed / DodgeGlowDuration));
        yield return null;
    }

    // shader 内部 sin(progress*PI) 产生 0→峰值→0 钟形曲线
    _materialInstance.SetFloat(ID_DodgeProgress, 0f);
    _materialInstance.DisableKeyword(KW_DODGE);
    _activeDodgeGlow = null;
}
```

**冲突合并策略**：四个方法全新增，插在 `ParryGlowRoutine` 之后、`HitFlashRoutine` 之前。`_activeDodgeGlow` 句柄与 `_activeParryGlow` 独立，互不干扰。`DodgeGlowRoutine` 必须使用 `Time.unscaledDeltaTime`（不能用 `Time.deltaTime`），否则 WitchTime 期间特效会慢 5 倍。

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

    // ★ 清理闪避效果
    _materialInstance.DisableKeyword(KW_DODGE);
    _materialInstance.SetFloat(ID_DodgeProgress, 0f);
    if (_activeDodgeGlow != null)
    {
        StopCoroutine(_activeDodgeGlow);
        _activeDodgeGlow = null;
    }
}
```

**冲突合并策略**：如果远程也修改了 `ResetEffect`（如新增其他 keyword 清理），先合并远程的清理逻辑，再在末尾追加 `_PARRY_DEFENDER_ON` 和 `_DODGE_ON` 清理块。

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

> 完整版本见 §7。

```csharp
private void HandlePerfectParryFreeze(ParryEventData data)
{
    // ---- 实例化 PerfectParry 粒子到玩家 eff 子物体 ----
    SpawnPerfectParry(data.Defender);

    // ★ P0 修正：先停止现有 CameraShake（避免顿帧后“延迟爆发”）
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
/// <summary>
/// 实例化 PerfectParry 粒子到玩家的 eff 子物体，播完后自动销毁。
/// 粒子方向跟随玩家朝向——由 PlayerParryState.Enter() 确保玩家面向攻击者。
/// </summary>
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

---

## 7b. PlayerParryState.cs（修改——朝向修复）

路径：`Assets/Scripts/Battle/Player/State/PlayerParryState.cs`

### 修改位置：`Enter()` 方法，清空输入之后、动作分流之前

**BUG 修复（2026-06-10）**：进入招架状态时强制玩家面向当前攻击者，解决粒子特效跟随错误朝向的问题。

```csharp
public override void Enter()
{
    base.Enter();

    owner.UseParryInput();
    owner.UseDodgeInput();
    owner.SetHorizontalVelocity(0f);

    // ========================================================
    // BUG 修复：强制玩家面向当前攻击者。
    // 战斗开始时朝向可能被锁死为朝右，导致粒子特效跟随错误朝向。
    // 在此处修正，确保招架时玩家始终面向敌人。
    // ========================================================
    var currentAttacker = BattleTurnManager.Instance?.CurrentAttacker;
    if (currentAttacker != null)
    {
        float dirToAttacker = currentAttacker.transform.position.x - owner.transform.position.x;
        if (Mathf.Abs(dirToAttacker) > 0.01f)
            owner.AdjustFacingDirection(dirToAttacker);
    }

    // （后续动作分流逻辑不变...）
```

**冲突合并策略**：新增块在清空输入之后、动作分流 `if (owner.currentFormIndex == 2)` 之前插入。如果远程在 Enter() 中也有新增逻辑，保持朝向修复在最前面（清空输入之后）。

---

## 7c. BattleManager.cs（修改——初始朝向修复）

路径：`Assets/Scripts/Core/BattleManager.cs`

### 修改位置：`StartBattleRoutine` 步骤 8

**BUG 修复（2026-06-10）**：玩家进入战斗时面向首个敌人出生点，而非硬编码朝右。

```csharp
        // 8. 瞬移玩家到战斗舞台
        if (playerController != null && protagonistSpawn != null)
        {
            playerController.rb.velocity = Vector2.zero;
            playerController.rb.position = protagonistSpawn.position;
            Physics2D.SyncTransforms();

            // ★ BUG 修复：面向首个敌人出生点，而非硬编码朝右。
            float faceDir = 1f;
            if (enemySpawns.Length > 0)
                faceDir = enemySpawns[0].transform.position.x - protagonistSpawn.position.x;
            playerController.AdjustFacingDirection(faceDir);
        }
```

**冲突合并策略**：仅修改步骤 8 最后一行 `AdjustFacingDirection` 的参数。如果远程修改了步骤 8 的其他部分（如瞬移逻辑），保留远程改动并将 `AdjustFacingDirection(faceDir)` 放在 `Physics2D.SyncTransforms()` 之后。

---

# 战斗音效系统（08-audio-system-design）

> 以下 §8–§11 记录战斗音效子系统的新增/修改文件。

---

## 8. SFXKey.cs / SFXConfigSO.cs / BattleSFXHandler.cs（新建，无冲突风险）

| 文件 | 路径 |
|------|------|
| SFXKey.cs | `Assets/Scripts/Audio/SFXKey.cs` |
| SFXConfigSO.cs | `Assets/Scripts/Audio/SFXConfigSO.cs` |
| BattleSFXHandler.cs | `Assets/Scripts/Audio/BattleSFXHandler.cs` |

三个文件均为新建，不存在冲突。完整代码参见 `08-audio-system-design.md` §2–§4。

**第二次修改（2026-06-10）**：`BattleSFXHandler` 新增 `DodgeEvents` 订阅（`OnPerfectDodge` / `OnNormalDodge`）。

**关键设计约束**：
- `BattleSFXHandler` 使用被动式单例（Awake 赋值），必须在场景中预先放置 GameObject
- 订阅 `ParryEvents` 三事件 + `DodgeEvents` 双事件（OnEnable/OnDisable 生命周期规范）
- `PlaySFX` 音量乘算 `AudioManager.Instance.MasterVolume * SFXVolume`（只读引用，零侵入）
- 100ms 防抖使用 `Time.unscaledTime`，顿帧期间不会拉长

---

## 9. PlayerCastSkillState.cs（修改，+1 行）

路径：`Assets/Scripts/Battle/Enemy/State/PlayerCastSkillState.cs`

### 修改位置：`Enter()` 方法末尾（第 38 行后）

```csharp
        Debug.Log($"[状态机] 玩家开始施放技能: {currentSkill.skillName} | 动画总时长: {skillDuration:F2}s");
        BattleSFXHandler.Instance?.PlaySFX(SFXKey.SkillCast); // ★ 音效新增
    }
```

**冲突合并策略**：如果远程在 `Enter()` 末尾追加了其他逻辑，将音效行放在所有新增行的最末尾即可。

---

## 10. PlayerBattleUltimateState.cs（修改，+1 行）

路径：`Assets/Scripts/Battle/Player/State/PlayerBattleUltimateState.cs`

### 修改位置：`Enter()` 中 early return 之后的 Debug.Log 之后（第 37 行后）

```csharp
        Debug.Log($"<color=orange>[大招调试] ===== 正式进入 PlayerBattleUltimateState 奥义释放！ =====\n" +
                  $"奥义名称: {currentUlt.ultimateName} | 动画状态: {currentUlt.animationState} | 配置时间: {currentUlt.duration}s | 判定进度: {currentUlt.hitProgress}</color>");
        BattleSFXHandler.Instance?.PlaySFX(SFXKey.UltimateCast); // ★ 音效新增
    }
```

**冲突合并策略**：必须放在 early return 守护之后（确保未配大招时不播音效）。如果远程在 early return 后也追加了逻辑，将音效行放在最后。

---

## 11. EnemyBattleEntity.cs（修改，+1 行）

路径：`Assets/Scripts/Battle/Base/EnemyBattleEntity.cs`

### 修改位置：`Die()` 方法开头，`ChangeState` 之前（第 238 行前）

```csharp
    protected override void Die()
    {
        BattleSFXHandler.Instance?.PlaySFX(SFXKey.EnemyDeath, transform.position); // ★ 音效新增
        battleStateMachine.ChangeState<EnemyBattleDieState>();
    }
```

**冲突合并策略**：如果远程修改了 `Die()` 方法（如新增死亡奖励、动画回调），将音效行放在 `ChangeState` 之前。

---

## 12. BattleManager.cs — 音效系统修改（+2 行）

路径：`Assets/Scripts/Core/BattleManager.cs`

> 此文件同时被格挡视觉系统（§6）和音效系统修改。两处改动位于不同方法，互不干扰。

### 修改位置 A：`EndBattleRoutine` 胜利分支（第 288 行后）

```csharp
            BattleUIController.Instance?.ShowVictoryPanel(true);
            BattleSFXHandler.Instance?.PlaySFX(SFXKey.Victory); // ★ 音效新增
            yield return new WaitForSeconds(3.0f);
```

### 修改位置 B：`EndBattleRoutine` 战败分支（第 341 行后）

```csharp
            BattleUIController.Instance?.ShowDefeatPanel(true);
            BattleSFXHandler.Instance?.PlaySFX(SFXKey.Defeat); // ★ 音效新增
            yield return new WaitForSeconds(3.0f);
```

**冲突合并策略**：两处均在 `ShowXxxPanel` 之后、`WaitForSeconds` 之前插入。与 §6 的 `StartBattleRoutine` 改动位于不同方法，不会冲突。如果远程修改了 `EndBattleRoutine` 的结算流程，将音效行保持在 `ShowXxxPanel` 之后即可。

---

## 13. DodgeEvents.cs（新建，无冲突风险）

路径：`Assets/Scripts/Battle/Events/DodgeEvents.cs`

整个文件为新建，仿照 `ParryEvents` 模式。如果远程有同名文件，保留此版本。

```csharp
public readonly struct DodgeEventData
{
    public readonly PlayerBattleEntity Defender;
    public readonly EnemyBattleEntity  Attacker;
    public readonly Vector3            HitPoint;
    public readonly bool               IsPerfect;
    public readonly int                RawDamage;
    // 构造函数省略（见完整代码）
}

public static class DodgeEvents
{
    public static event Action<DodgeEventData> OnPerfectDodge;
    public static event Action<DodgeEventData> OnNormalDodge;

    public static void FirePerfectDodge(DodgeEventData data) => SafeInvoke(OnPerfectDodge, data);
    public static void FireNormalDodge(DodgeEventData data)  => SafeInvoke(OnNormalDodge, data);
}
```

**冲突合并策略**：整个文件为新建，不存在冲突。如果远程有同名文件，比较双方接口保留完整参数集。

---

## BattleSFXHandler.cs — 闪避事件订阅补充

> 此节附属于 §8。在 `BattleSFXHandler` 的 `OnEnable` / `OnDisable` 中新增 `DodgeEvents` 订阅对。

```csharp
private void OnEnable()
{
    // 原格挡订阅...
    ParryEvents.OnPerfectParry += HandlePerfectParry;
    ParryEvents.OnNormalParry  += HandleNormalParry;
    ParryEvents.OnParryFailed  += HandleParryFailed;

    // ★ 新增：闪避订阅
    DodgeEvents.OnPerfectDodge += HandlePerfectDodge;
    DodgeEvents.OnNormalDodge  += HandleNormalDodge;
}

private void OnDisable()
{
    // 原格挡解绑...
    ParryEvents.OnPerfectParry -= HandlePerfectParry;
    ParryEvents.OnNormalParry  -= HandleNormalParry;
    ParryEvents.OnParryFailed  -= HandleParryFailed;

    // ★ 新增：闪避解绑
    DodgeEvents.OnPerfectDodge -= HandlePerfectDodge;
    DodgeEvents.OnNormalDodge  -= HandleNormalDodge;
}

private void HandlePerfectDodge(DodgeEventData data) => PlaySFX(SFXKey.PerfectDodge, data.HitPoint);
private void HandleNormalDodge(DodgeEventData data)  => PlaySFX(SFXKey.NormalDodge,  data.HitPoint);
```

**冲突合并策略**：如果远程也在 `OnEnable`/`OnDisable` 中新增了其他事件订阅，将 DodgeEvents 订阅对与 ParryEvents 订阅对并列排列即可。

---

# 战斗转场音效系统（transition-audio）

> 以下 §14–§15 记录战斗转场音效子系统的新增/修改文件。
> 设计文档：`.superpowers/brainstorm/transition-audio/00-transition-audio-overview.md`

---

## 14. SFXKey.cs（修改——新增转场音效枚举值）

路径：`Assets/Scripts/Audio/SFXKey.cs`

> 此文件已在 §8 中新建。本次在现有 500+ 流程类下追加两个转场音效键。

### 修改位置：枚举末尾，`Defeat = 501` 之后

```csharp
    // ---- 流程 (500+) ----
    Victory = 500,
    Defeat  = 501,

    // ---- 转场 (502+) ----
    BattleEncounter     = 502,  // 转场冲击（顿帧阶段，短促定格音）
    BattleEncounterRise = 503,  // 转场上升（撕裂阶段，持续渐强音）
}
```

**冲突合并策略**：如果远程在 500+ 区间也新增了其他流程音效（如 `BattleStart = 504`），保留远程新增项，将转场枚举值紧接 `Defeat = 501` 之后排列。枚举值显式赋值，顺序不影响序列化。

---

## 15. BattleTransitionController.cs（修改，+2 行）

路径：`Assets/Scripts/CustomPostProcessing/BattleTransitionController.cs`

### 修改位置：`EncounterRoutine` 协程内，两个阶段各 +1 行

**改动点**：在顿帧阶段和空间撕裂阶段各插入一行 `BattleSFXHandler.Instance?.PlaySFX()` 调用，替代原有的占位注释。

```csharp
    private IEnumerator EncounterRoutine(System.Action onComplete = null)
    {
        // 1. 顿帧阶段 (Hit-stop)
        Time.timeScale = 0.05f;
        BattleSFXHandler.Instance?.PlaySFX(SFXKey.BattleEncounter); // ★ 新增：转场冲击音效（短促定格音）

        // 等待现实时间度过顿帧期
        yield return new WaitForSecondsRealtime(hitStopTime);

        // 2. 空间撕裂/拉取阶段 (驱动后处理，全程保持低流速)
        BattleSFXHandler.Instance?.PlaySFX(SFXKey.BattleEncounterRise); // ★ 新增：转场上升音效（持续渐强音）
        float elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            // ... 现有逻辑不变 ...
        }

        // ... 后续逻辑不变 ...
    }
```

**冲突合并策略**：
- 第一行替换了原有占位注释 `// 播放破碎/拔剑音效...`，如果远程在此位置也替换了该注释（如改为其他音效调用），保留远程实现并在其后追加 `BattleEncounterRise` 调用。
- 第二行在 `float elapsedTime = 0f;` 之前插入，如果远程在此处也有新增逻辑，将音效行保持在循环之前。
- 两行调用均使用 `?.` 安全调用，`BattleSFXHandler` 为 DontDestroyOnLoad 单例，探索场景中始终可用。