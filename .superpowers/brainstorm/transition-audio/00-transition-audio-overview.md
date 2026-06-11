# 战斗转场音效系统 — 设计文档

> 日期：2026-06-11
> 参考：08-audio-system-design.md（战斗音效子系统）、BattleTransitionController 转场协程、崩铁转场音效风格

## 需求摘要

在玩家于主场景（探索场景）触发战斗转场时，配合视觉后处理效果（崩铁式空间撕裂）播放对应音效，增强打击感与沉浸感。

**设计原则**：
- **复用现有音效管线** — 直接调用 `BattleSFXHandler.Instance?.PlaySFX(SFXKey.XXX)`，与 Victory/Defeat/SkillCast 等触发方式一致
- **零新文件** — 仅修改 `SFXKey.cs` 和 `BattleTransitionController.cs`，不引入新的 C# 文件
- **配置表驱动** — 新增 SFXKey 枚举值后，只需在 `SFXConfigSO` 资源中配置对应 AudioClip

---

## 1. 文件变更清单

| 文件 | 操作 | 改动量 |
|------|------|--------|
| `Assets/Scripts/Audio/SFXKey.cs` | 修改 | +2 行（新增枚举值） |
| `Assets/Scripts/CustomPostProcessing/BattleTransitionController.cs` | 修改 | +3 行（音效调用） |

**不改动**：BattleSFXHandler.cs、SFXConfigSO.cs、AudioManager.cs、BattleManager.cs、BattleTransitionEffect.cs、CustomRenderFeature.cs

---

## 2. 转场时间线与音效关键帧

基于 `BattleTransitionController.EncounterRoutine` 协程的时间线：

```
t=0                t=hitStopTime(~0.15s)         t=hitStopTime+transitionDuration(~0.95s)
 │                       │                              │
 ▼                       ▼                              ▼
┌─────────────────┐ ┌──────────────────────┐  ┌──────────────────┐
│  阶段1：顿帧     │ │  阶段2：空间撕裂       │  │  转场完成          │
│  TimeScale→0.05 │ │  progress: 0→1       │  │  progress=1       │
│                 │ │                      │  │  回调/加载场景      │
│  ★ 冲击音效      │ │  ★ 上升音效            │  │                    │
│  (BattleEncounter)│ │  (BattleEncounterRise)│  │                    │
└─────────────────┘ └──────────────────────┘  └──────────────────┘
```

### 各阶段音效说明

| 阶段 | SFXKey | 触发时机 | 音效风格建议 | spatialBlend |
|------|--------|----------|-------------|:---:|
| 顿帧开始 | `BattleEncounter` | `EncounterRoutine` 第一行（TimeScale 降低之后） | 短促金属撞击 / 空间定格冲击音，参考崩铁遇敌时的"咔嚓"碎裂感 | 0 (2D) |
| 空间撕裂 | `BattleEncounterRise` | 顿帧结束、撕裂动画开始前 | 持续上升音，低频嗡鸣渐强 / 空间扭曲拉伸音，营造被吸入感 | 0 (2D) |

> 两阶段音效均为 2D 播放（`spatialBlend = 0`），因为转场是全屏后处理效果，音效应从"画面中央"传来，无需空间定位。

---

## 3. SFXKey 枚举扩展

```csharp
// SFXKey.cs
// Path: Assets/Scripts/Audio/SFXKey.cs
//
// 在现有 500+ 流程类下追加：

public enum SFXKey
{
    // ... 现有枚举值不变 ...

    // ---- 流程 (500+) ----
    Victory = 500,
    Defeat  = 501,

    // ---- 转场 (502+) ----
    BattleEncounter     = 502,  // 转场冲击（顿帧阶段，短促定格音）
    BattleEncounterRise = 503,  // 转场上升（撕裂阶段，持续渐强音）
}
```

---

## 4. SFXConfigSO 配置表新增条目

在 `BattleSFXConfig` ScriptableObject 资源中追加两条：

| key | clip 建议 | volume | spatialBlend | pitchMin | pitchMax | priority |
|-----|----------|:------:|:---:|:---:|:---:|:---:|
| `BattleEncounter` | 金属撞击/玻璃碎裂短音（~0.3s） | 1.0 | 0 | 0.95 | 1.05 | 64 |
| `BattleEncounterRise` | 低频嗡鸣渐强/空间扭曲拉伸音（~1.0s） | 0.9 | 0 | 0.9 | 1.1 | 64 |

> `priority` 设为 64（较高），确保转场音效不会被同时触发的其他音效抢占。
> `pitchMin`/`pitchMax` 微调范围比战斗音效略窄，转场音效需要更稳定的听感。

---

## 5. BattleTransitionController 改动

### 5.1 EncounterRoutine — 顿帧阶段插入冲击音效

> 文件: `Assets/Scripts/CustomPostProcessing/BattleTransitionController.cs`
> 插入位置: `EncounterRoutine` 内，`Time.timeScale = 0.05f` 之后（第 43-44 行）

```csharp
private IEnumerator EncounterRoutine(System.Action onComplete = null)
{
    // 1. 顿帧阶段 (Hit-stop)
    Time.timeScale = 0.05f;
    BattleSFXHandler.Instance?.PlaySFX(SFXKey.BattleEncounter); // ★ 新增：转场冲击音效

    // 等待现实时间度过顿帧期
    yield return new WaitForSecondsRealtime(hitStopTime);

    // 2. 空间撕裂/拉取阶段
    BattleSFXHandler.Instance?.PlaySFX(SFXKey.BattleEncounterRise); // ★ 新增：转场上升音效

    float elapsedTime = 0f;
    while (elapsedTime < transitionDuration)
    {
        // ... 现有逻辑不变 ...
    }

    // ... 后续逻辑不变 ...
}
```

### 5.2 代码改动量统计

| 位置 | 改动 |
|------|------|
| `EncounterRoutine` L44（TimeScale 赋值后） | +1 行 `BattleSFXHandler.Instance?.PlaySFX(SFXKey.BattleEncounter);` |
| `EncounterRoutine` L49（顿帧 yield 后、撕裂循环前） | +1 行 `BattleSFXHandler.Instance?.PlaySFX(SFXKey.BattleEncounterRise);` |

---

## 6. TimeScale 兼容性分析

### 问题

转场期间 `Time.timeScale = 0.05`，`BattleSFXHandler.PlaySFX` 内部使用 `source.Play()` 启动 AudioSource。Unity 中 AudioSource 播放**受 TimeScale 影响**，理论上 0.05x 时间流速下音频会被极度拉伸甚至无法正常播放。

### 分析

实际上 Unity 的 `AudioSource.Play()` 调用后，音频引擎在**混音器层面**独立运行，**不受 TimeScale 影响**。AudioSource 的 `pitch` 参数控制播放速度，与 `Time.timeScale` 无关。

验证依据：
1. `BattleSFXHandler` 的防抖机制使用 `Time.unscaledTime`（已正确）
2. Unity 音频引擎在 `AudioSettings.dspTime` 上运行，独立于游戏循环的 `Time.time`
3. 项目中 `BattleEffectManager` 的镜头抖动协程已使用 `Time.unscaledDeltaTime`，转场音效播放与之一致

### 结论

**无需特殊处理**。`BattleSFXHandler.PlaySFX` 在低 TimeScale 下可正常播放音效。`WaitForSecondsRealtime` 保证协程等待使用真实时间，音效引擎独立运行不受影响。

---

## 7. 音效风格建议

参考**崩坏：星穹铁道**遇敌转场音效特征：

| 阶段 | 听感特征 | 实现建议 |
|------|---------|---------|
| 顿帧冲击 | 清脆短促的"碎裂/撞击"音，类似玻璃破碎+金属共鸣，给人"时间被冻结"的感觉 | 选取 ~0.2-0.4s 的短促音效，含高频金属泛音 |
| 空间撕裂 | 低频嗡鸣持续上升，伴随空间扭曲的电子音效，营造"现实被撕裂并吸入"的紧张感 | 选取 ~0.8-1.2s 的渐强音效，频率从低向高攀升 |

> 音效素材可后续在 Unity Editor 中通过 `SFXConfigSO` Inspector 直接拖入 AudioClip 调整，无需改代码。

---

## 8. 与其他系统的关系

```
探索场景
  │
  ▼
EnemyAttackState（敌人击中玩家）
  │
  ▼
BattleManager.StartBattle()
  │
  ├─► BattleTransitionController.TriggerEncounter()
  │     │
  │     ├─► EncounterRoutine 协程
  │     │     ├─► Time.timeScale = 0.05
  │     │     ├─► ★ BattleSFXHandler.PlaySFX(BattleEncounter)     ← 新增
  │     │     ├─► WaitForSecondsRealtime(hitStopTime)
  │     │     ├─► ★ BattleSFXHandler.PlaySFX(BattleEncounterRise) ← 新增
  │     │     └─► 驱动 BattleTransitionEffect.progress (0→1)
  │     │
  │     └─► onComplete 回调
  │
  └─► StartBattleRoutine（加载战斗场景、初始化实体...）
```

**音效播放链路**：`BattleTransitionController` → `BattleSFXHandler.Instance?.PlaySFX()` → 临时 AudioSource → AudioListener（探索摄像机）→ 扬声器

---

## 9. 技术债务 & 后续规划

| 级别 | 事项 | 说明 |
|------|------|------|
| P1 | AudioSource 对象池 | 当前 PlaySFX 使用 `new GameObject + Destroy`，高频调用下可优化为对象池 |
| P1 | 转场完成收束音 | 可选第三阶段音效（progress=1 时的最终冲击），对应 SFXKey `BattleEncounterEnd = 504` |
| P1 | AudioMixer 集成 | 将 BattleSFXHandler 的临时 AudioSource 路由到 AudioMixer Group |
| P2 | 音量跟随全局设置 | 当前 `PlaySFX` 未乘算 `AudioManager.MasterVolume * SFXVolume`（现有 BattleSFXHandler 实现已包含此逻辑） |
| P2 | 不同敌人类型差异化转场音效 | 可在 `BattleEncounter` 基础上按 `EnemyGroup` 类型选择不同音效 key |
