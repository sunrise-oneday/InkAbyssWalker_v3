# 敌人战斗系统增强文档

## 概述

本系统为敌人添加了更丰富的战斗行为，包括：
- **护盾系统**：参考杀戮尖塔，敌人可以有护盾值抵挡伤害
- **意图图标**：显示敌人下一次行动的类型图标
- **Debuff能力**：敌人可以给玩家施加负面效果
- **AI决策**：基于随机权重的决策系统，让敌人行动更加灵活
- **视觉效果**：护盾破碎、格挡、回血等视觉效果
- **战斗对话**：敌人在战斗中根据行动说出不同的话

## 新增功能

### 0. 敌人AI类型

**预设AI类型：**
- `Aggressive`（攻击型）：偏好攻击，高伤害
- `Defensive`（防御型）：偏好格挡和回血，注重生存
- `Supportive`（辅助型）：偏好施加debuff和强化
- `Balanced`（均衡型）：各种行动均衡

**使用方法：**
```csharp
// 添加EnemyAITypes组件
var aiTypes = gameObject.AddComponent<EnemyAITypes>();

// 配置AI类型
// 在Inspector中设置aiType为Aggressive/Defensive/Supportive/Balanced
```

### 1. 护盾系统

**机制说明：**
- 敌人可以有护盾值（`shield`）
- 受到伤害时，先扣护盾，护盾扣完再扣血
- 护盾条显示在血条旁边（蓝色条）
- 没有护盾时，护盾条自动隐藏

**使用方法：**
```csharp
// 给敌人添加护盾
enemy.Stats.AddShield(20);

// 清除敌人护盾
enemy.Stats.ClearShield();
```

**在Inspector中配置：**
1. 打开敌人预制件
2. 在`CharacterStats`组件中设置`maxShield`（最大护盾值）
3. 在`EntityHUD`组件中拖入`shieldSlider`（护盾条UI）

### 2. 意图系统

**意图类型：**
- `Attack` - 攻击
- `Block` - 格挡（获得护盾）
- `BuffSelf` - 增益自己
- `DebuffPlayer` - 减益玩家
- `MultiAttack` - 多段攻击
- `Heal` - 回血
- `Summon` - 召唤
- `Strengthen` - 强化（提升攻击/防御）
- `SpecialAttack` - 特殊攻击

**使用方法：**
```csharp
// 获取敌人当前意图
EnemyIntent intent = enemy.GetCurrentIntent();

// 设置敌人意图
enemy.SetCurrentIntent(intent);
```

**在Inspector中配置：**
1. 打开敌人预制件
2. 在`EntityHUD`组件中配置意图图标显示区域
3. 拖入`intentIcon`（意图图标Image）
4. 拖入`intentContainer`（意图容器GameObject）

### 3. Debuff系统

**新增Debuff类型：**

| Debuff名称 | 类名 | 效果 | 持续时间 |
|------------|------|------|----------|
| 破甲 | `ArmorBreakBuff` | 降低防御力 | 2-3回合 |
| 虚弱 | `WeakenBuff` | 降低攻击力 | 2-3回合 |
| 诅咒 | `CurseBuff` | 每回合造成固定伤害 | 3-4回合 |
| 中毒 | `PoisonBuff` | 每回合造成递增伤害 | 持续到被净化 |
| 速度下降 | `SlowBuff` | 降低速度（影响行动顺序） | 2回合 |

**使用BuffFactory创建Buff：**
```csharp
// 使用BuffFactory创建Buff实例
Buff armorBreak = BuffFactory.Create("ArmorBreakBuff", 3, 5);  // 持续3回合，降低5点防御
player.Stats.AddBuff(armorBreak);

// 检查Buff类型是否存在
bool exists = BuffFactory.HasType("ArmorBreakBuff");

// 获取所有已注册的Buff类型
string[] types = BuffFactory.GetRegisteredTypes();
```

**在Inspector中配置Debuff：**
```
buffTypeName: "ArmorBreakBuff"  // Buff类名
duration: 3                     // 持续回合数
value: 5                        // 效果值
```

### 4. AI决策系统

**决策机制：**
- 基于随机权重的决策系统
- 根据血量百分比调整行动权重
- 低血量时可以触发特殊行动

**权重计算示例：**
```
行动池：
- 普通攻击：基础权重 5，血量<50%时权重*0.5
- 格挡：基础权重 2，血量<30%时权重*3
- 施加debuff：基础权重 2，血量<60%时权重*1.5
- 回血：基础权重 1，血量<25%时权重*5

血量70%时的权重：
- 普通攻击：5 * 1 = 5
- 格挡：2 * 1 = 2
- 施加debuff：2 * 1 = 2
- 回血：1 * 1 = 1
总权重：10

血量30%时的权重：
- 普通攻击：5 * 0.5 = 2.5
- 格挡：2 * 3 = 6
- 施加debuff：2 * 1.5 = 3
- 回血：1 * 5 = 5
总权重：16.5
```

### 5. 视觉效果系统

**效果类型：**
- 护盾破碎效果：护盾被击破时的视觉反馈
- 格挡效果：敌人格挡时的视觉反馈
- 回血效果：敌人回血时的视觉反馈
- 增益效果：敌人强化自己时的视觉反馈

**使用方法：**
```csharp
// 添加EnemyVisualEffects组件
var vfx = gameObject.AddComponent<EnemyVisualEffects>();

// 在Inspector中配置效果预制件
// shieldBreakEffectPrefab - 护盾破碎效果
// blockEffectPrefab - 格挡效果
// healEffectPrefab - 回血效果
// buffEffectPrefab - 增益效果
```

### 6. 战斗对话系统

**对话类型：**
- 攻击时的台词
- 格挡时的台词
- 回血时的台词
- 施加debuff时的台词
- 低血量时的台词
- 死亡时的台词

**使用方法：**
```csharp
// 添加EnemyBattleDialogue组件
var dialogue = gameObject.AddComponent<EnemyBattleDialogue>();

// 在Inspector中配置对话内容
// attackLines - 攻击时的台词
// blockLines - 格挡时的台词
// healLines - 回血时的台词
// debuffLines - 施加debuff时的台词
// lowHealthLines - 低血量时的台词
// deathLines - 死亡时的台词
```

## 配置步骤

### 步骤1：添加AI组件

1. 打开敌人预制件
2. 添加`EnemyAI`组件
3. 或者添加`EnemyAIConfigurator`组件（更方便的配置方式）

### 步骤2：配置攻击序列

1. 在`EnemyAI`组件中配置`possibleActions`数组
2. 为每个行动配置：
   - `actionName` - 行动名称
   - `intentType` - 意图类型
   - `intentIcon` - 意图图标
   - `attackSequence` - 攻击序列（如果是攻击类型）
   - `shieldAmount` - 护盾值（如果是格挡类型）
   - `debuffs` - Debuff配置（如果是debuff类型）
   - `baseWeight` - 基础权重
   - `healthThreshold` - 血量阈值
   - `healthWeightModifier` - 权重修正

### 步骤3：配置低血量行动（可选）

1. 在`EnemyAI`组件中配置`lowHealthActions`数组
2. 这些行动只在敌人血量低于`lowHealthThreshold`时才会被考虑

### 步骤4：配置UI显示

1. 打开敌人预制件的子物体HUD
2. 在`EntityHUD`组件中配置：
   - `intentIcon` - 意图图标Image
   - `intentContainer` - 意图容器GameObject
   - `intentValueText` - 意图数值文本（可选）

## 示例配置

### 简单敌人配置

```csharp
// 使用EnemyAIConfigurator快速配置
var configurator = GetComponent<EnemyAIConfigurator>();
configurator.ConfigureAI();
```

### 手动配置

```csharp
// 创建行动列表
var actions = new EnemyAction[]
{
    EnemyAction.CreateAttack("普通攻击", attackSequence, attackIcon, 5f),
    EnemyAction.CreateBlock("防御", 20, blockIcon, 2f, 0.3f),
    EnemyAction.CreateDebuff("施加debuff", debuffs, debuffIcon, 2f),
    EnemyAction.CreateHeal("回血", 30, healIcon, 1f, 0.25f),
    EnemyAction.CreateSummon("召唤小兵", summonPrefab, 2, summonIcon, 1f),
    EnemyAction.CreateStrengthen("强化", 5, "attack", strengthenIcon, 1.5f)
};

// 设置AI
enemyAI.SetPossibleActions(actions);
```

## 注意事项

1. **护盾值清零**：护盾值每回合开始时清零（除非有特殊buff保持护盾）
2. **意图更新**：意图图标在敌人回合结束时立即更新，供玩家在下一回合决策
3. **权重平衡**：AI权重参数需要在测试中反复调整，确保敌人行为既有随机性又不会过于离谱
4. **低血量行为**：低血量时敌人的行为应该更加激进（如更多格挡、回血）

## 文件结构

```
Assets/Scripts/Battle/Enemy/
├── EnemyAI.cs              # AI决策组件
├── EnemyAction.cs          # 行动配置
├── EnemyIntent.cs          # 意图数据
├── EnemyAITypes.cs         # 预设AI类型配置
├── EnemyAIConfigurator.cs  # AI配置器（示例）
├── EnemyVisualEffects.cs   # 视觉效果管理
├── EnemyBattleDialogue.cs  # 战斗对话系统
└── README.md               # 本文档

Assets/Scripts/Battle/Buff/
├── Buff.cs                 # Buff基类
├── BuffFactory.cs          # Buff工厂（自动注册+动态创建）
├── ArmorBreakBuff.cs       # 破甲debuff
├── WeakenBuff.cs           # 虚弱debuff
├── CurseBuff.cs            # 诅咒debuff
├── PoisonBuff.cs           # 中毒debuff
└── SlowBuff.cs             # 速度下降debuff

Assets/Scripts/Battle/Buff/
├── ArmorBreakBuff.cs       # 破甲debuff
├── WeakenBuff.cs           # 虚弱debuff
├── CurseBuff.cs            # 诅咒debuff
├── PoisonBuff.cs           # 中毒debuff
└── SlowBuff.cs             # 速度下降debuff

Assets/Scripts/Battle/Base/
├── CharacterStats.cs       # 添加了护盾系统
├── BattleEntity.cs         # 修改了伤害计算
└── EnemyBattleEntity.cs    # 添加了AI和意图

Assets/Scripts/Battle/UI/
└── EntityHUD.cs            # 添加了护盾条和意图图标

Assets/Scripts/Core/
└── BattleTurnManager.cs    # 集成了AI决策
```

## 更新日志

### v1.3
- 重构Buff创建方式，使用BuffFactory工厂模式
- 新增BuffFactory类，自动注册所有Buff类型
- 修改EnemyDebuffConfig，使用buffTypeName替代debuffName
- 修改EnemyBattleEntity，使用BuffFactory创建Buff实例

### v1.2
- 添加了预设AI类型配置（攻击型、防御型、辅助型、均衡型）
- 添加了视觉效果系统（护盾破碎、格挡、回血、增益效果）
- 添加了战斗对话系统（敌人在战斗中说话）
- 集成视觉效果和对话到战斗流程

### v1.1
- 添加了回血、召唤、强化、特殊攻击等新的行动类型
- 更新了AI决策系统支持新的行动类型
- 更新了使用文档

### v1.0
- 初始版本
- 实现了护盾系统
- 实现了意图系统
- 实现了Debuff系统
- 实现了AI决策系统
- 集成到BattleTurnManager
