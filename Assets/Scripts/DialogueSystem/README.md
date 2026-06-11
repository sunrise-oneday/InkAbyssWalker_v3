# NPC对话系统 V2

一个可独立移植、支持分支对话和条件判断的NPC对话系统。

## 特性

- **JSON配置**：对话数据存储为JSON文件，可用任何文本编辑器编辑
- **分支对话**：支持玩家选择不同选项，NPC回答相应变化
- **条件判断**：可根据变量值、物品持有、剧情标记动态切换对话
- **变量系统**：支持bool、int、string三种类型变量
- **文本插值**：支持在对话文本中插入变量值
- **可移植**：核心系统不依赖项目特定代码，通过接口抽象解耦

## 目录结构

```
Assets/Scripts/DialogueSystem/           # 核心引擎（可独立移植）
  Data/                                  # 数据模型
  Runtime/                               # 运行时引擎
  Interface/                             # 接口定义
  Config/                                # 配置加载

Assets/Scripts/NPC/Integration/          # 项目级集成桥（不可移植）
  InkDialogueInputBridge.cs              # 输入桥接
  InkGameConditionProvider.cs            # 条件查询桥接
  InkGameActionExecutor.cs               # 动作执行桥接
  InkDialogueSaveBridge.cs               # 存档桥接

Assets/Scripts/NPC/                      # NPC相关脚本
  DialogueManagerV2.cs                   # 对话管理器单例
  DialoguePanelV2.cs                     # 对话UI面板
  NPCDialogueTriggerV2.cs               # NPC对话触发器
```

## JSON格式说明

### 对话文件整体结构

```json
{
  "version": 1,
  "dialogueId": "npc_blacksmith_01",
  "variables": [
    {
      "name": "hasDeliveredOre",
      "type": "Bool",
      "defaultValue": "false"
    },
    {
      "name": "trustLevel",
      "type": "Int",
      "defaultValue": "0"
    }
  ],
  "startNodeId": "start",
  "nodes": [
    // 节点列表...
  ]
}
```

### 节点类型

#### 1. Dialogue节点（对话行）

```json
{
  "nodeId": "start",
  "type": "Dialogue",
  "speaker": "铁匠",
  "portrait": "portrait_blacksmith",
  "text": "你好，旅行者。有什么我能帮忙的吗？",
  "typewriterSpeed": 0.03,
  "onShowActions": [
    {
      "type": "IncrementVariable",
      "key": "greetedTimes",
      "count": 1
    }
  ],
  "next": "next_node_id"
}
```

#### 2. Choice节点（选项分支）

```json
{
  "nodeId": "choice_01",
  "type": "Choice",
  "prompt": "选择你的回应：",
  "options": [
    {
      "text": "选项1文本",
      "next": "node_for_option1",
      "condition": null,
      "actions": null
    },
    {
      "text": "选项2文本（仅当hasDeliveredOre为true时显示）",
      "next": "node_for_option2",
      "condition": {
        "variable": "hasDeliveredOre",
        "op": "Equals",
        "value": "true"
      },
      "actions": null
    }
  ]
}
```

#### 3. Condition节点（条件分支）

```json
{
  "nodeId": "branch_01",
  "type": "Condition",
  "conditions": [
    {
      "variable": "hasDeliveredOre",
      "op": "Equals",
      "value": "true",
      "nextIfTrue": "dialogue_delivered",
      "nextIfFalse": "dialogue_not_delivered"
    }
  ],
  "defaultNext": "dialogue_not_delivered"
}
```

#### 4. Action节点（动作执行）

```json
{
  "nodeId": "give_ore",
  "type": "Action",
  "actions": [
    {
      "type": "SetVariable",
      "key": "hasDeliveredOre",
      "value": "true"
    },
    {
      "type": "AddItem",
      "key": "equip_dark_sword",
      "count": 1
    },
    {
      "type": "RemoveItem",
      "key": "story_ore",
      "count": 1
    }
  ],
  "next": "dialogue_reward"
}
```

#### 5. End节点（对话结束）

```json
{
  "nodeId": "end",
  "type": "End"
}
```

### 条件操作符

| 操作符 | 说明 | 适用类型 |
|--------|------|----------|
| `Equals` | 等于 | bool, int, string |
| `NotEquals` | 不等于 | bool, int, string |
| `GreaterThan` | 大于 | int |
| `LessThan` | 小于 | int |
| `GreaterThanOrEquals` | 大于等于 | int |
| `LessThanOrEquals` | 小于等于 | int |
| `HasItem` | 持有物品 | value=物品ID |
| `QuestFlag` | 剧情标记已设置 | variable=标记键 |

### 动作类型

| 动作类型 | 说明 | 参数 |
|----------|------|------|
| `SetVariable` | 设置变量值 | key=变量名, value=值 |
| `IncrementVariable` | 递增变量值 | key=变量名, count=递增量 |
| `AddItem` | 添加物品 | key=物品ID, count=数量 |
| `RemoveItem` | 移除物品 | key=物品ID, count=数量 |
| `SetQuestFlag` | 设置剧情标记 | key=标记键, value=true/false |
| `PlaySFX` | 播放音效 | key=音效资源键 |
| `TriggerEvent` | 触发自定义事件 | key=事件名 |

### 文本插值

在对话文本中使用 `{var:variableName}` 语法插入变量值：

```json
{
  "text": "你已经来过{var:greetedTimes}次了。"
}
```

支持格式说明符：

```json
{
  "text": "你的信任度是{var:trustLevel:D2}"  // 输出: 02
}
```

## 使用步骤

### 1. 创建对话JSON文件

在 `Assets/Resources/Dialogues/` 目录下创建JSON文件，如 `npc_villager.json`。

### 2. 配置场景

1. 创建NPC GameObject，添加Collider2D组件（勾选Is Trigger）
2. 添加 `NPCDialogueTriggerV2` 组件
3. 配置对话数据（拖入JSON文件或DialogueGraphConfig）
4. 配置依赖对象（DialogueRunner、DialoguePanel等）

### 3. 配置DialogueManagerV2

1. 创建空GameObject，命名为"DialogueManager"
2. 添加 `DialogueManagerV2` 组件
3. 配置所有依赖对象（InputBridge、ConditionProvider、ActionExecutor等）

### 4. 运行测试

1. 运行场景
2. 控制玩家靠近NPC
3. 看到"按E对话"提示
4. 按E键开始对话
5. 体验分支对话和条件判断

## 移植到其他项目

1. 复制 `Assets/Scripts/DialogueSystem/` 目录到新项目
2. 实现5个接口的项目特定版本：
   - `IDialogueInputProvider` - 桥接新项目的输入系统
   - `IDialogueUIProvider` - 桥接新项目的UI系统
   - `IGameConditionProvider` - 桥接新项目的物品/任务系统
   - `IGameActionExecutor` - 桥接新项目的物品增删/标记设置
   - `IDialogueSaveProvider` - 桥接新项目的存档系统
3. 创建新的 `NPCDialogueTriggerV2` 组件
4. 配置场景和对话数据

## 注意事项

1. JsonUtility不支持多态，所有节点类型使用统一的 `DialogueNode` 类，通过 `type` 字段区分
2. 变量名区分大小写
3. 条件操作符区分大小写
4. 节点ID必须唯一
5. 对话JSON文件必须放在 `Resources/Dialogues/` 目录下才能使用 `Resources.Load` 加载
