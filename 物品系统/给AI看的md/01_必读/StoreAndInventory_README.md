# StoreAndInventory 脚本结构

> **Obsidian 副本**（同步自 Unity `Assets/Script/StoreAndInventory/README.md`）。  
> 文档包入口：[[README]] · AI 速读：[[01_AI速读_商店背包]]  
> delivery: 主工程打包

> **依赖方向**：`UI → Services → Catalog → Data`  
> **禁止**：Data 引用 UGUI；UI 写业务规则；Services 反向依赖 UI。  
> 详细说明见 Obsidian：`交付包/脚本结构.md`

## 主工程接入（3 步）

1. **拷贝**：`StoreAndInventory/`（不含 `Test/`）+ `Assets/prefab/` + Item/ShopTable SO
2. **场景**：Data + Service Prefab；UI 合并主 Canvas（[[02_接入主工程清单]] 策略 B）
3. **入口 + 存档**：`ShopService.Open` + `PanelController`；`CaptureAllJson` / `ApplyAllJson`

## UI

| 文件夹 | 脚本 |
|---|---|
| `Common/` | `ItemTooltipUI`, `StoreInventoryPanelController` |
| `Shop/` | `ShopUI`, `ShopItemUI`, `SellItemUI` |
| `Inventory/` | `InventoryUI`, `InventoryItemUI`, `EquipmentSlotUI`, `AttributeStatLineUI` |

## Test

- `ItemTestController.cs` — **勿拷入主工程**

## Editor 菜单

- `MoYuan → Setup Full Store Loop (A+B+C)`
