# 06 — 时序图 & 集成注意事项

## 完美格挡时序图

```
t=0.000s  敌人动画事件 TriggerDamage(hitIndex)
          → BattleCombatResolver 判定完美格挡
          → ApplyDamageFeedback（现有：FlashColor 青色 + ShakeCamera 0.2s + HitStop 0.06s）
          → ParryEvents.FirePerfectParry(eventData)  ← SafeInvoke 逐个 try-catch
              │
              ├─ BattleEffectManager.HandlePerfectParryFreeze()
              │    → 中断旧 CameraShake 协程（_activeCameraShake）   ★ P0 修正
              │    → StopAllTimeScaleCoroutines()    // 中断刚才的 HitStop 0.06s（不重置 timeScale）★ P1 修正
              │    → Time.timeScale = 0.01            // 极限顿帧
              │    → PerfectParryShake(0.15f, 0.08f)  // unscaledDeltaTime 微抖，不受冻结影响 ★ P0 修正
              │
              ├─ PerfectParry.HandlePerfectParry()
              │    → StopAllParticles(clearImmediate: true)  // 清除旧残留粒子 ★ P2 修正
              │    → SplashParticle.Play()   ─┐
              │    → BrustParticle.Play()     ├─ useUnscaledTime=true，不受冻结影响
              │    → EndParticle.Play()      ─┘
              │    → StartCoroutine(FinishAfterRealtime(3.5s))  // 等 EndParticle(3.23s) 消散
              │
              └─ ShaderEffectController.HandleParryGlow()
                   → EnableKeyword("_PARRY_DEFENDER_ON")
                   → StartCoroutine(WaitForSecondsRealtime(0.3s))

t=0.150s（真实时间）
  PerfectParryShake 完成（unscaledDeltaTime 驱动，0.15s 后准时结束）

t=0.300s（真实时间）
  ShaderEffectController → DisableKeyword("_PARRY_DEFENDER_ON")
  发光结束

t=0.400s（真实时间）
  BattleEffectManager → Time.timeScale = 1.0f
  游戏恢复正常速度
  FlashColor(Color.cyan, 0.5f) 继续播放剩余 ~0.496s（被 WaitForSeconds 冻住后恢复）→ 青色淡出

t=3.500s（真实时间）
  PerfectParry → Finish() → StopEmitting（自然消散，不清除已生成粒子）
  粒子控制器释放 isPlaying 锁
```

### 镜头抖动"延迟爆发"已消除的设计验证

- **原隐患**：ShakeCamera(0.2f, 0.25f) 使用 Time.deltaTime，0.01x 下只推进 0.004s，恢复后剩余 0.196s 集中爆发 → 已通过中断 `_activeCameraShake` 协程消除
- **新方案**：PerfectParryShake(0.15f, 0.08f) 使用 unscaledDeltaTime，0.15s 准时完成，在顿帧解除前已经结束
- **结果**：玩家恢复操作时画面完全稳定，0 额外抖动

## 普通格挡时序图

```
t=0.000s  BattleCombatResolver 判定普通格挡
          → ApplyDamageFeedback（现有：受伤 30% + FlashColor 灰色 + ShakeCamera 0.12s）
          → ParryEvents.FireNormalParry(eventData)
              │
              └─ ShaderEffectController.HandleParryGlow()
                   → EnableKeyword("_PARRY_DEFENDER_ON")
                   → StartCoroutine(WaitForSecondsRealtime(0.3s))

t=0.300s（真实时间）
  ShaderEffectController → DisableKeyword("_PARRY_DEFENDER_ON")
  发光结束

（无粒子、无顿帧、无额外镜头微抖）
```

## 格挡失败时序图

```
t=0.000s  BattleCombatResolver 判定失败
          → ApplyDamageFeedback（现有：全额伤害 + 削韧 + ShakeCamera 0.3s）
          → ParryEvents.FireParryFailed(eventData)
              │
              └─ （当前无订阅者，预留扩展点）
```

## Unity Editor 必须手动确认的事项

### PerfectParry 预制体

1. **层级关系**：`Player → PerfectParry → SplashParticle / BrustParticle / EndParticle`
2. **子物体名称**：必须与脚本中 `transform.Find()` 的参数完全一致
3. **Play On Awake**：三个 ParticleSystem 的 `Play On Awake` 必须**关闭**（由脚本控制）
4. **粒子材质**：确认 Renderer 材质已指定 `ShaderMoBrust.mat` 或 `BrustMo.mat`
5. **PerfectParry.cs 挂载**：确认脚本挂在 PerfectParry 子物体上（不是 Player 根物体）
6. **默认状态**：预制体在 Hierarchy 中保持 **active**（不用 SetActive 控制可见性）
7. **EndParticle StartLifetime**：当前为 3.23s，协程等待 3.5s 后标记 isPlaying=false。如果之后修改粒子 Lifetime，需要同步修改 `FinishAfterRealtime` 的等待时间

### GeneralCharacterEffect.shader

1. **_PARRY_DEFENDER_ON**：Inspector 中默认 off（当前已是，无需改动）
2. **_ParryGlowColor**：可在 Inspector 中调整发光颜色（默认蓝色 HDR）
3. **_ParryGlowWidth**：可在 Inspector 中调整发光边缘宽度（默认 2.0）

### MoBrust.shader

无需任何修改，粒子系统通过材质（ShaderMoBrust.mat / BrustMo.mat）引用此 shader。

## P0-P2 隐患修正汇总

| 级别 | 隐患 | 根因 | 修正 |
|------|------|------|------|
| P0 | 镜头抖动"延迟爆发" | ShakeCamera 用 `Time.deltaTime`，顿帧期间冻结，恢复后集中爆发 | 中断旧协程 + 改用 `PerfectParryShake(unscaledDeltaTime)` |
| P0 | ShakeCamera 句柄未缓存 | 现有 `ShakeCamera` 未将协程句柄赋给 `_activeCameraShake`，中断逻辑形同虚设 | `ShakeCamera` 增加句柄缓存 + 进入前中断旧协程 |
| P1 | TimeScale "闪回"物理异常 | `StopAllTimeScaleCoroutines` 中 `Time.timeScale=1.0f` 导致 0.05→1.0→0.01 跳变 | 删除中间重置，新协程直接覆盖 |
| P2 | 粒子"视觉切断" | `StopEmittingAndClear` 瞬间抹除 StartLifetime=3.23s 的 EndParticle | `Finish` 改用 `StopEmitting`（自然消散）|

## 类型兼容性确认

`BattleCombatResolver.EvaluateParryAndApplyDamage` 中构建 eventData 时：

```csharp
var attacker = BattleTurnManager.Instance.CurrentAttacker;
```

- `BattleTurnManager.CurrentAttacker` 声明位于 `BattleTurnManager.cs:68`：`public EnemyBattleEntity CurrentAttacker`
- `ParryEventData.Attacker` 声明位于 `ParryEvents.cs`：`public readonly EnemyBattleEntity Attacker`
- 两者返回类型一致，无需任何类型转换或防御性 null 检查（`CurrentAttacker` 在攻击回合中必然非 null）

因此 `BattleTurnManager.cs` 列为"不改动"文件是正确的——仅被引用读取，不修改任何代码。

## 未来扩展点

无需改任何现有代码，只需新建脚本订阅事件：

| 扩展需求 | 做法 |
|----------|------|
| 精准防御音效 | 新建 `ParrySFXHandler.cs`，订阅 `OnPerfectParry` 播放打击音效 |
| "PERFECT!" UI 文字 | 新建 `ParryUIHandler.cs`，订阅 `OnPerfectParry` 显示飘字 |
| 连续完美格挡 combo | 新建 `ParryComboTracker.cs`，订阅三个事件统计连段 |
| 格挡失败受击音效 | 订阅 `OnParryFailed` 播放受击音效 |
| 根据伤害缩放粒子强度 | 读取 `data.RawDamage`，按比例调整粒子 startSize |
| 火花飞溅方向 | 读取 `data.Attacker.transform.position` 计算方向向量 |
