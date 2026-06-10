# 精准防御特效系统 — 设计总览

> 日期：2026-06-08
> 参考：街霸6 精准防御（Drive Parry → Perfect Parry）

## 需求摘要

在玩家成功触发完美/普通格挡时，驱动以下视觉反馈：

| 条件 | _PARRY_DEFENDER_ON 发光 | 粒子特效 | 顿帧（0.01x） |
|------|:-:|:-:|:-:|
| 完美格挡（Perfect） | ✅ 0.3s | ✅ 0.5s | ✅ 0.4s |
| 普通格挡（Normal） | ✅ 0.3s | — | — |
| 格挡失败 | — | — | — |

## 设计方案

**方案 B：事件驱动解耦**

BattleCombatResolver 在判定出口发射事件，特效侧各组件自行订阅、互不耦合。
战斗逻辑未来更新不影响特效订阅者，新增特效只需加新订阅者。

## 已解决的架构隐患

| 级别 | 问题 | 解决方案 |
|------|------|----------|
| P0 致命 | 静态事件内存泄漏 | 所有订阅者强制 OnEnable 订阅 / OnDisable 解绑 |
| P0 致命 | 镜头抖动"延迟爆发" | ShakeCamera 缓存句柄 + 中断旧协程 + 改用 unscaledDeltaTime 微抖 |
| P1 高危 | 载荷扩展性瓶颈 | ParryEventData 结构体封装所有上下文 |
| P1 高危 | TimeScale "闪回"物理异常 | StopAllTimeScaleCoroutines 不重置 timeScale |
| P2 中危 | 多播委托异常阻断 | SafeInvoke + GetInvocationList + try-catch |
| P2 中危 | 粒子"视觉切断" | EndParticle(3.23s) 用 StopEmitting 自然消散 |

## 文件变更清单

| 文件 | 操作 | 改动量 |
|------|------|--------|
| `Assets/Scripts/Battle/Events/ParryEvents.cs` | **新建** | ~50 行 |
| `Assets/Scripts/Core/BattleCombatResolver.cs` | 修改 | +15 行 |
| `Assets/Shaders/PerfectParry.cs` | **重写** | ~90 行 |
| `Assets/Scripts/Battle/Base/ShaderEffectController.cs` | 修改 | +40 行 |
| `Assets/Scripts/Core/BattleEffectManager.cs` | 修改 | +65 行 |

**不改动**：GeneralCharacterEffect.shader、MoBrust.shader、PerfectParry.prefab、PlayerBattleEntity.cs、BattleTurnManager.cs

## 事件订阅矩阵

| 订阅者 | OnPerfectParry | OnNormalParry | OnParryFailed |
|--------|:-:|:-:|:-:|
| PerfectParry.cs（粒子） | ✅ | — | — |
| ShaderEffectController.cs（发光） | ✅ | ✅ | — |
| BattleEffectManager.cs（顿帧） | ✅ | — | — |
| _未来扩展（音效/UI/combo）_ | _扩展点_ | _扩展点_ | _扩展点_ |

## 文档索引

- [01-parry-events.md](01-parry-events.md) — 事件系统（ParryEventData + ParryEvents）
- [02-combat-resolver-integration.md](02-combat-resolver-integration.md) — 发射端改动
- [03-perfect-parry-particles.md](03-perfect-parry-particles.md) — 粒子控制器
- [04-shader-effect-parry-glow.md](04-shader-effect-parry-glow.md) — Shader 发光
- [05-battle-effect-freeze.md](05-battle-effect-freeze.md) — 顿帧 & 镜头
- [06-timing-and-integration.md](06-timing-and-integration.md) — 时序图 & 集成注意事项
