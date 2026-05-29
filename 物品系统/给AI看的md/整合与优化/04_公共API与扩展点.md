---
tags: [API, 扩展, Service]
status: 现行
delivery: 主工程打包
related: [[系统功能与工程规范]], [[01_AI速读_商店背包]], [[一些特别的使用方法]]
---

# 公共 API 与扩展点

> 主工程与战斗系统应只依赖本节 API + Data 类型，不依赖 `Test/` 与具体 UI。

---

## §1 边界速查

| 数据 | 谁读 | 谁执行 |
|---|---|---|
| `Equipment.statMods` | UI 显示、`GetAllStatMods()` | 主工程战斗结算 |
| `Equipment.skillMods` | `GetAllSkillMods()` | **主工程技能系统** |
| `Equipment.extraEffects` | `GetAllExtraEffects()` | **主工程效果系统** |
| `Consumable.useEffects` | `TryConsumeAt` 返回列表 | **主工程** |
| `GameplayEffectSO` 字段 | 展示 / 传递 | 主工程定义执行语义 |

---

## §2 Inventory

| API | 说明 |
|---|---|
| `Count` / `Items` | 当前格列表 |
| `TryAdd(item, count, out msg)` | 入包（堆叠规则内） |
| `TryTakeAt` / `TryTakePartialAt` | 取出（卖/装内部用） |
| `TryConsumeAt(bagIndex, count, context, out effects, out msg)` | 消耗品；**effects 需主工程执行** |
| `GetConsumables(contextFilter?)` | Step13 对外查询 |
| `TryGetDefinition(id, out def)` | 查表 |
| `LoadFromData(InventoryData)` | 读档 |

---

## §3 ShopService

| API | 说明 |
|---|---|
| `Open(ShopTableSO table = null)` | 开店；null 用 defaultTable |
| `Close()` | 关店并 **归档** 限购/runtime |
| `IsOpen` | 是否营业中 |
| `TryBuy(itemId, count)` → `BuyResult` | 购买 |
| `TrySell(bagSlotIndex, count, out msg)` → `SellResult` | 卖出 |
| `GetVisibleEntries()` | 当前货架 |
| `GetPrice(entry)` / `GetRemainingStock(itemId)` | UI / 逻辑 |
| `Refresh()` | 随机池刷新（若配表启用） |
| `ExportRuntimeArchive()` / `ImportRuntimeArchive()` | 存档用 |
| 事件 `OnShopOpened` / `OnShopClosed` / `OnShopChanged` | UI 刷新 |

**BuyResult**：`Success` · `ShopClosed` · `InvalidItem` · `OutOfStock` · `NoCurrency` · `NoSpace`

---

## §4 WalletService

| API | 说明 |
|---|---|
| `Get(CurrencyId)` / `Has` / `Add` / `TrySpend` | 当前仅 `CurrencyId.Ink` |
| `LoadFromData(CurrencyWallet)` | 读档 |

---

## §5 EquipmentService

| API | 说明 |
|---|---|
| `TryEquipFromBag(bagIndex, runeSlotIndex, out msg)` | 装符文（离包进槽） |
| `TryUnequip(runeSlotIndex, out msg)` | 卸符文回包 |
| `GetEquippedStack(runeSlotIndex)` | 槽位内容 |
| `GetAllStatMods()` | 聚合 flat/percent |
| `GetAllSkillMods()` | 聚合；**不执行** |
| `GetAllExtraEffects()` | 聚合；**不执行** |
| `RuneSlotCount` | 默认 3 |
| `OnEquipped` / `OnUnequipped` | UI / 主工程监听 |
| `ApplySaveData` / `CaptureSaveData` | 存档（经 StoreSaveService） |

---

## §6 StoreSaveService

| API | 说明 |
|---|---|
| `CaptureAll()` → `SaveBundle` | 内存快照 |
| `ApplyAll(bundle, out error)` | 还原 |
| `CaptureAllJson()` / `ApplyAllJson` | 主存档字符串块 |
| `RoundTripSelfTest(out error)` | 自检 |
| `LogExternalQuerySnapshot()` | Debug 查询 |

---

## §7 StoreInventoryPanelController（UI 入口）

| API | 说明 |
|---|---|
| `OpenShop` / `CloseShop` / `ToggleShop` | Close 会 `ShopService.Close()` |
| `OpenInventory` / `CloseInventory` / `ToggleInventory` | |
| `CloseAll` | |
| `IsShopOpen` / `IsInventoryOpen` | |

---

## §8 TestCharacter（主工程替换点）

| API | 现状 |
|---|---|
| `Get(StatType)` | 基础属性 |
| `GetAll()` | 属性列表 |
| `LoadFromData` | 存档 |

**替换策略**：主工程角色实现相同读取能力；`InventoryUI.RefreshStats` 与 `StoreSaveService` 改引用。**本阶段不抽接口**，见 [[02_接入主工程清单]] §3.3。

---

## §9 扩展点（Future 目录说明）

| 计划功能 | 建议落点 |
|---|---|
| GameplayEffect 执行器 | `Services/Effects/`（主工程或本子系统新增） |
| 合成 | `Services/Crafting/` |
| 磁盘存档 | 主工程 Save 模块调 `CaptureAllJson` |
| 正式角色 | 替换 `TestCharacter` |
| 背包拖拽 | `UI/Inventory/` only |

详见工程内 `Future/README.md`。

---

## §10 ItemDatabase

| API | 说明 |
|---|---|
| `TryGet(string id, out ItemBase)` | 全局 id 查定义 |
| Inspector 列表 | 启动时建索引 |
