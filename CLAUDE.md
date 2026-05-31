# CLAUDE.md

本文件为 eva (0ldm0s.net/eva-cli) 在此仓库中工作时提供指导。

## 项目概述

InkAbyssWalker 是一个 Unity 2D 类银河城（Metroidvania）回合制动作 RPG。核心玩法：2D 平台探索 + 回合制战斗，带有元素反应、Buff 系统和装备/背包系统。

## 技术栈

- Unity 2022.3 LTS + C# (.NET Standard 2.1)
- Universal Render Pipeline (URP) 12.x
- Unity InputSystem 1.7
- TextMesh Pro

## 常用命令

- 在 Unity Editor 中打开项目：通过 Unity Hub 打开 `InkAbyssWalker-Integration` 文件夹
- 构建：Unity Editor → File → Build Settings
- 项目入口场景：`Assets/Scenes/`

## 项目结构

```
Assets/
├── Scripts/           # 主工程代码（Core/Battle/Expolre/Input/Item/Save/UI/Audio）
├── Script/            # StoreAndInventory 独立子系统
├── Scenes/            # 场景文件
├── Resources/         # 动态加载资源
├── prefab/            # 预制体
├── Animation/         # 动画
├── Graphics/          # 图形资源
├── Material/          # 材质
├── Settings/          # URP 等渲染设置
├── Editor/            # 编辑器扩展
├── Document/          # 文档
└── TextMesh Pro/      # TMP 资源
```

## 核心架构

### 状态机驱动
所有玩家/敌人行为由 `StateMachine<T>` 泛型状态机驱动。探索和战斗各有一套独立状态机：
- 玩家探索状态机（7 状态）→ `PlayerController` 拥有
- 玩家战斗状态机（8 状态）→ `PlayerBattleEntity` 拥有
- 敌人探索状态机（4 状态）→ `OverworldEnemy` 拥有
- 敌人战斗状态机（4 状态）→ `EnemyBattleEntity` 拥有

`IDeathState` 接口标记死亡状态，切入后状态机永久锁定。

### 输入系统
事件驱动架构：`InputAssets`（3 个 Action Map）→ `InputReader`（事件广播）→ `Controller`（响应）。
三个 Action Map：GamePlayer（探索）、Battle（战斗）、UI。
`StoreInventoryInputBridge` 负责 I/F 键的背包/商店输入拦截。

### 战斗系统
`BattleManager` 统一调度 → `BattleTurnManager` 回合控制 → `BattleCombatResolver` 攻防判定。
伤害计算链：动画事件 → 闪避/格挡判定 → `CharacterStats.TakeDamage()` → Buff 拦截器链 → 破防倍率 → 扣血。
Buff 系统使用策略模式多态 + 责任链模式（`OnBeforeTakeDamage` 拦截修改伤害）。

### 商店与背包子系统
命名空间 `StoreAndInventory`，位于 `Assets/Script/StoreAndInventory/`。
严格 MVC 分层：数据层（Data/）→ 服务层（Services/）→ UI 层（UI/），UI 禁止直接操作数据层。
`BattleStatSyncBridge` 在开战/收战时同步装备属性到 `CharacterStats`。

## 设计模式

| 模式 | 主要使用位置 |
|------|------------|
| 状态模式 | StateMachine<T> 驱动全部实体行为 |
| 单例模式 | 所有 Manager 类（BattleManager, InputManager, SaveManager 等） |
| 观察者模式 | CharacterStats 事件 → EntityHUD 刷新；InputReader 事件 → Controller 响应 |
| Facade 模式 | BattleUIController 统一管理战斗面板 |
| 策略模式 | Buff 多态；效果系统 |
| 责任链模式 | Buff.OnBeforeTakeDamage 伤害拦截链 |

## 开发规范

- 新功能优先在现有框架基础上扩展（状态机、事件系统、Service 层）
- 资源放置遵循现有目录约定
- 脚本文件保存为 UTF-8 with BOM（Unity 要求）
- 遵循现有命名规范和代码风格

@.eva/docs/战斗系统.md
@.eva/docs/状态机系统.md
@.eva/docs/商店背包系统.md
@.eva/docs/输入系统.md
@.eva/docs/探索系统.md
@.eva/docs/UI与音频系统.md

refine-tags
type: code
domain: 2D类银河城回合制动作RPG
keywords: Unity, C#, URP, 状态机, 回合制战斗, 元素反应, Buff系统, 背包, 商店, InputSystem
