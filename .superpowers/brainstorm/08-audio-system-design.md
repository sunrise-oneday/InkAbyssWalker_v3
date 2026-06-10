# 通用战斗音效播放系统 — 设计文档

> 日期：2026-06-10
> 参考：ParryEvents 事件总线模式、BattleEffectManager 静态单例模式

## 需求摘要

在现有视觉特效系统（ParryEvents + BattleEffectManager + ShaderEffectController）基础上，新增战斗音效子系统。P0 阶段覆盖格挡/攻击/击杀/流程音效，闪避音效搁置到 P1。

**设计原则**：
- **零侵入现有文件** — 不改动 `AudioManager.cs`、`BattleCombatResolver.cs`、`ParryEvents.cs`、`BattleEffectManager.cs`、`ShaderEffectController.cs`
- **可嵌入式架构** — 当前独立运行，未来可无缝接入完整音乐系统
- **配置表驱动** — ScriptableObject 统一管理，新增音效不需改代码

---

## 1. 文件变更清单

| 文件 | 操作 | 改动量 |
|------|------|--------|
| `Assets/Scripts/Audio/SFXKey.cs` | **新建** | ~30 行 |
| `Assets/Scripts/Audio/SFXConfigSO.cs` | **新建** | ~50 行 |
| `Assets/Scripts/Audio/BattleSFXHandler.cs` | **新建** | ~120 行 |
| `Assets/Scripts/Battle/Enemy/State/PlayerCastSkillState.cs` | 修改 | +1 行 |
| `Assets/Scripts/Battle/Player/State/PlayerBattleUltimateState.cs` | 修改 | +1 行 |
| `Assets/Scripts/Battle/Base/EnemyBattleEntity.cs` | 修改 | +1 行 |
| `Assets/Scripts/Core/BattleManager.cs` | 修改 | +2 行 |

**不改动**：AudioManager.cs、BattleCombatResolver.cs、ParryEvents.cs、BattleEffectManager.cs、ShaderEffectController.cs、PlayerParryState.cs、PlayerBattleDodgeState.cs、PlayerCounterAttackState.cs

---

## 2. SFXKey 枚举

```csharp
// SFXKey.cs
// Path: Assets/Scripts/Audio/SFXKey.cs

/// <summary>
/// 战斗音效键。显式赋值 + 类别步长 100，保证序列化后增删不导致数据错乱。
/// </summary>
public enum SFXKey
{
    None = 0,

    // ---- 格挡 (100+) ----
    PerfectParry = 100,
    NormalParry  = 101,
    ParryFailed  = 102,

    // ---- 闪避 (200+) ---- P1 启用
    PerfectDodge = 200,
    NormalDodge  = 201,

    // ---- 攻击 (300+) ----
    SkillCast    = 300,
    UltimateCast = 301,

    // ---- 击杀 (400+) ----
    EnemyDeath = 400,

    // ---- 流程 (500+) ----
    Victory = 500,
    Defeat  = 501
}
```

---

## 3. SFXConfigSO

```csharp
// SFXConfigSO.cs
// Path: Assets/Scripts/Audio/SFXConfigSO.cs

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗音效配置表（ScriptableObject）
/// 将 SFXKey 枚举映射到 AudioClip + 播放参数。
/// 参考 AbilityDisplayConfig 的 List + GetEntry 模式。
///
/// 创建: Assets > Create > InkAbyss > Audio > SFX Config
/// 推荐存放: Assets/Resources/Audio/
/// </summary>
[CreateAssetMenu(fileName = "BattleSFXConfig", menuName = "InkAbyss/Audio/SFX Config")]
public class SFXConfigSO : ScriptableObject
{
    [System.Serializable]
    public class SFXEntry
    {
        public SFXKey key;
        public AudioClip clip;

        [Header("Volume & Spatial")]
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0f, 1f)] public float spatialBlend = 0f; // 0=2D, 1=3D

        [Header("Pitch Randomization")]
        [Range(-3f, 3f)] public float pitchMin = 1f;
        [Range(-3f, 3f)] public float pitchMax = 1f;

        [Header("Priority (0=Highest, 256=Lowest)")]
        [Range(0, 256)] public int priority = 128;

#if UNITY_EDITOR
        public void Validate()
        {
            if (pitchMin > pitchMax)
            {
                pitchMax = pitchMin;
                Debug.LogWarning($"SFXEntry {key}: pitchMin > pitchMax, 已自动修正。");
            }
            if (clip == null)
                Debug.LogWarning($"SFXEntry {key}: AudioClip 缺失!");
        }
#endif
    }

    public List<SFXEntry> entries = new List<SFXEntry>();

    /// <summary>按 key 查找条目，未找到返回 null（与 AbilityDisplayConfig.GetEntry 对齐）</summary>
    public SFXEntry GetEntry(SFXKey key)
    {
        return entries.Find(e => e.key == key);
    }
}
```

---

## 4. BattleSFXHandler

### 4.1 订阅事件（格挡 3 事件）

遵循 P0 生命周期规范（OnEnable 订阅 / OnDisable 解绑），三个独立 handler：

```csharp
private void OnEnable()
{
    ParryEvents.OnPerfectParry += HandlePerfectParry;
    ParryEvents.OnNormalParry  += HandleNormalParry;
    ParryEvents.OnParryFailed  += HandleParryFailed;
}

private void OnDisable()
{
    ParryEvents.OnPerfectParry -= HandlePerfectParry;
    ParryEvents.OnNormalParry  -= HandleNormalParry;
    ParryEvents.OnParryFailed  -= HandleParryFailed;
}

private void HandlePerfectParry(ParryEventData data) => PlaySFX(SFXKey.PerfectParry, data.HitPoint);
private void HandleNormalParry(ParryEventData data)  => PlaySFX(SFXKey.NormalParry,  data.HitPoint);
private void HandleParryFailed(ParryEventData data)  => PlaySFX(SFXKey.ParryFailed, data.HitPoint);
```

### 4.2 播放管线（核心）

2D/3D 统一处理，全部由 `spatialBlend` 驱动。不依赖 `AudioManager`。

```csharp
// BattleSFXHandler.cs
// Path: Assets/Scripts/Audio/BattleSFXHandler.cs
//
// 职责：战斗音效播放中枢。
//   - 订阅 ParryEvents 格挡事件（OnEnable/OnDisable 生命周期规范）
//   - 配置表驱动，新增音效只需编辑 SFXConfigSO，不改代码
//   - 独立于 AudioManager（只读引用其音量设置，零侵入）

using System.Collections.Generic;
using UnityEngine;

public class BattleSFXHandler : MonoBehaviour
{
    // ==============================
    // 被动式单例（需在场景中预先放置 GameObject）
    // 与 BattleEffectManager 的懒加载 getter 不同：
    //   Awake 赋值，不主动创建。
    //   场景缺失时所有 ?.PlaySFX() 静默跳过。
    // ==============================
    public static BattleSFXHandler Instance { get; private set; }

    [SerializeField] private SFXConfigSO sfxConfig;

    private Dictionary<SFXKey, SFXConfigSO.SFXEntry> _sfxLookup;
    private Dictionary<SFXKey, float> _lastPlayTime; // 防抖

    private void Awake()
    {
        // ★ 被动式单例（需在场景中预先放置 GameObject，与 BattleEffectManager 的懒加载 getter 不同）
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildLookup();
    }

    private void BuildLookup()
    {
        _sfxLookup = new Dictionary<SFXKey, SFXConfigSO.SFXEntry>();
        _lastPlayTime = new Dictionary<SFXKey, float>();
        if (sfxConfig == null || sfxConfig.entries == null) return;

        foreach (var entry in sfxConfig.entries)
        {
            if (entry.key == SFXKey.None) continue;
            _sfxLookup[entry.key] = entry; // 重复 key 后者覆盖
        }
    }

    // ========== P0 生命周期管理（格挡事件订阅）==========

    private void OnEnable()
    {
        ParryEvents.OnPerfectParry += HandlePerfectParry;
        ParryEvents.OnNormalParry  += HandleNormalParry;
        ParryEvents.OnParryFailed  += HandleParryFailed;
    }

    private void OnDisable()
    {
        ParryEvents.OnPerfectParry -= HandlePerfectParry;
        ParryEvents.OnNormalParry  -= HandleNormalParry;
        ParryEvents.OnParryFailed  -= HandleParryFailed;
    }

    // ---- 事件处理（直接转发到播放管线）----

    private void HandlePerfectParry(ParryEventData data) => PlaySFX(SFXKey.PerfectParry, data.HitPoint);
    private void HandleNormalParry(ParryEventData data)  => PlaySFX(SFXKey.NormalParry,  data.HitPoint);
    private void HandleParryFailed(ParryEventData data)  => PlaySFX(SFXKey.ParryFailed, data.HitPoint);

    // ========== 播放管线（核心）==========

    /// <summary>播放指定 key 的音效。position 为 null 时在原点播放（2D 模式）。</summary>
    public void PlaySFX(SFXKey key, Vector3? position = null)
    {
        if (_sfxLookup == null || !_sfxLookup.TryGetValue(key, out var entry))
        {
            Debug.LogWarning($"[BattleSFXHandler] 未找到 SFX 配置: {key}");
            return;
        }

        if (entry.clip == null)
        {
            Debug.LogWarning($"[BattleSFXHandler] AudioClip 为空: {key}");
            return;
        }

        // ★ 防抖：100ms 内同一 key 不重复播放（解决 AoE 多杀叠加）
        float now = Time.unscaledTime;
        if (_lastPlayTime.TryGetValue(key, out float last) && (now - last) < 0.1f)
            return;
        _lastPlayTime[key] = now;

        // 随机变调
        float pitch = Random.Range(entry.pitchMin, entry.pitchMax);

        // 创建临时 AudioSource，播完自动销毁
        var go = new GameObject($"SFX_{entry.key}");
        if (position.HasValue) go.transform.position = position.Value;

        var source = go.AddComponent<AudioSource>();
        source.clip = entry.clip;
        // 音效音量 = 配置表基础音量 × 全局 SFX 音量 × 主音量（只读 AudioManager，零侵入）
        float globalSFXVol = AudioManager.Instance != null
            ? AudioManager.Instance.MasterVolume * AudioManager.Instance.SFXVolume
            : 1f;
        source.volume       = entry.volume * globalSFXVol;
        source.pitch        = pitch;
        source.spatialBlend = entry.spatialBlend;
        source.priority     = entry.priority;
        source.Play();

        // clip 长度 / |pitch|，pitch 为 0 时保护
        float safePitch = Mathf.Max(0.01f, Mathf.Abs(pitch));
        float realLength = entry.clip.length / safePitch;
        Destroy(go, realLength + 0.1f);
    }

    // ========== 调试辅助 ==========

    /// <summary>Editor 用：列出所有已配置的 SFX 条目</summary>
    public void DebugPrintAllEntries()
    {
        if (_sfxLookup == null || _sfxLookup.Count == 0)
        {
            Debug.Log("[BattleSFXHandler] 配置表为空");
            return;
        }
        foreach (var kv in _sfxLookup)
            Debug.Log($"[BattleSFXHandler] {kv.Key} -> {kv.Value.clip?.name ?? "(null)"} vol={kv.Value.volume}");
    }
}
```

---

## 5. P0 触发点集成（4 文件，共 6 行）

### 5.1 PlayerCastSkillState.cs — 技能释放

> 文件: `Assets/Scripts/Battle/Enemy/State/PlayerCastSkillState.cs`
> 插入位置: `Enter()` 方法末尾，Debug.Log 之后（第 38 行后）

```csharp
// ... 现有逻辑不变（line 38）...
Debug.Log($"[状态机] 玩家开始施放技能: {currentSkill.skillName} | 动画总时长: {skillDuration:F2}s");
BattleSFXHandler.Instance?.PlaySFX(SFXKey.SkillCast); // ★ 新增：技能释放音效
```

### 5.2 PlayerBattleUltimateState.cs — 大招释放

> 文件: `Assets/Scripts/Battle/Player/State/PlayerBattleUltimateState.cs`
> 插入位置: `Enter()` 中 early return 之后的 Debug.Log 之后（第 37 行后）

```csharp
// ... early return 之后的多行 Debug.Log（line 36-37）...
Debug.Log($"<color=orange>[大招调试] ===== 正式进入 PlayerBattleUltimateState 奥义释放！ =====\n" +
          $"奥义名称: {currentUlt.ultimateName} | 动画状态: {currentUlt.animationState} | 配置时间: {currentUlt.duration}s | 判定进度: {currentUlt.hitProgress}</color>");
BattleSFXHandler.Instance?.PlaySFX(SFXKey.UltimateCast); // ★ 新增：奥义释放音效
```

### 5.3 EnemyBattleEntity.cs — 敌人死亡

> 文件: `Assets/Scripts/Battle/Base/EnemyBattleEntity.cs`
> 插入位置: `Die()` 方法开头，`ChangeState` 之前（第 238 行前）

```csharp
protected override void Die()
{
    BattleSFXHandler.Instance?.PlaySFX(SFXKey.EnemyDeath, transform.position); // ★ 新增：死亡音效，3D 空间位置
    battleStateMachine.ChangeState<EnemyBattleDieState>();
}
```

> 使用 `transform.position` 作为 3D 空间位置。AoE 多杀时 100ms 防抖限制叠加。

### 5.4 BattleManager.cs — 胜负结算

> 文件: `Assets/Scripts/Core/BattleManager.cs`

胜利分支（`EndBattleRoutine` 内，第 288 行后）：

```csharp
// ... line 288 ...
            BattleUIController.Instance?.ShowVictoryPanel(true);
            BattleSFXHandler.Instance?.PlaySFX(SFXKey.Victory); // ★ 新增：胜利音效
            yield return new WaitForSeconds(3.0f);
```

战败分支（`EndBattleRoutine` 内，第 341 行后）：

```csharp
// ... line 341 ...
            BattleUIController.Instance?.ShowDefeatPanel(true);
            BattleSFXHandler.Instance?.PlaySFX(SFXKey.Defeat); // ★ 新增：战败音效
            yield return new WaitForSeconds(3.0f);
```

---

## 6. 配置表使用流程

0. 在场景中创建空 GameObject（命名 `[BattleSFXHandler]`），挂载 `BattleSFXHandler` 脚本并拖入 `sfxConfig` 配置表
1. Unity Editor → Project 窗口 → 右键 → Create → Audio → SFX Config
2. 在 Inspector 中为每个 SFXKey 添加条目，拖入 AudioClip
3. 将配置表拖入 `BattleSFXHandler` 的 `sfxConfig` 字段
4. 未配置 key 或 clip 为空的条目：播放时静默跳过 + console warning

---

## 7. 技术债务 & P1 规划

| 级别 | 事项 | 说明 |
|------|------|------|
| P1 | 闪避音效 | 需在 `BattleCombatResolver.HandleDodge` 内新增事件发射（或 `DodgeEvents.cs`） |
| P1 | AudioSource 对象池 | 当前 `new GameObject + Destroy`，高频场景（技能连放）上对象池 |
| P1 | P0 剩余 key | `PlayerHit`、`PerfectCounter`、`EnemyBreak`、`BattleStart`、`PlayerTurnStart`、`PlayerDeath` |
| P1 | AudioMixer 集成 | 将临时 AudioSource 路由到 AudioMixer Group，接入全局音量和效果链 |
| P2 | SFXConfigSO Editor 校验按钮 | 在 Inspector 上添加 "Validate All" 按钮，批量调用 `SFXEntry.Validate()` |

---

## 8. 防抖逻辑说明

**触发条件**：同一 `SFXKey` 在 100ms 真实时间内（`Time.unscaledTime`）重复调用 `PlaySFX` 时，后续调用静默丢弃。

**主要场景**：AoE 大招同时击杀多只怪物，`EnemyBattleEntity.Die()` 在同一帧内连续触发多次。

**计时基准**：使用 `unscaledTime` 而非 `Time.time`，确保顿帧（TimeScale=0.01）期间防抖窗口不会被人为拉长。

---

## 9. 事件订阅矩阵

| 订阅者 | OnPerfectParry | OnNormalParry | OnParryFailed |
|--------|:-:|:-:|:-:|
| PerfectParry.cs（粒子） | ✅ | — | — |
| ShaderEffectController.cs（发光） | ✅ | ✅ | — |
| BattleEffectManager.cs（顿帧） | ✅ | — | — |
| **BattleSFXHandler.cs（音效）** | **✅** | **✅** | **✅** |

---

## 10. 与其他系统的关系

```
                    BattleCombatResolver.FirePerfectParry/NormalParry/Failed
                              │
                              ▼
                         ParryEvents (static event)
                              │
         ┌────────────────────┼────────────────────┐
         ▼                    ▼                    ▼
  BattleEffectManager   ShaderEffectController   BattleSFXHandler  【新增】
    (顿帧+粒子+微抖)       (PARRY_DEFENDER 发光)    (音效播放)

★ BattleSFXHandler.PlaySFX 音量乘算 AudioManager.MasterVolume × SFXVolume
  （只读引用，不修改 AudioManager 任何代码）
```

**后续接入完整音乐系统时**：
- `PlaySFX` 方法内的临时 AudioSource 创建逻辑可替换为从对象池获取
- `SFXConfigSO` 可扩展 `outputAudioMixerGroup` 字段，将音效路由到 Mixer
- 枚举可按类别新增（600+ BGM、700+ UI 等），不破坏现有数据
- 当前通过代码乘算的音量可迁移至 AudioMixer Group 统一接管
