---
tags: [索引, 交付, 主工程]
status: 现行
delivery: 主工程打包
date: 2026-05-26
---

# 商店背包 · 主工程交付文档包

> **本文件夹整包拷贝**到主项目（与 Unity 代码一起交付）。  
> 代码：`StoreAndInventory/`（无 `Test/`）+ `Assets/prefab/` + Item/ShopTable SO。

---

## §1 阅读顺序（约 15 分钟）

| 顺序 | 文档 | 说明 |
|---|---|---|
| 1 | [[01_AI速读_商店背包]] | 边界、目录、当前交互 |
| 2 | [[系统功能与工程规范]] | **验收宪法** |
| 3 | [[04_公共API与扩展点]] | 主工程调哪些 API |
| 4 | [[02_接入主工程清单]] | 拷贝、Prefab B 策略、自检 |
| 5 | [[系统使用手册]] | Setup、配表、Play 测试 |
| 6 | [[03_修改物品系统注意事项]] | 改 SO/id/存档 前必读 |
| 7 | [[脚本结构]] + [[StoreAndInventory_README]] | 代码地图 |

**按需**：[[一些特别的使用方法]] · [[UI结构]] · [[工程清理与优化记录]] · [[10_项目功能优化方案]]

**设计背景（有 drift）**：[[03_设计与远期/商店背包系统]] — 冲突时以规范为准

---

## §2 文件夹结构

```
交付包/                          ← 本文件夹 = 文档交付物
├── README.md                    ← 本文件（入口）
├── 系统功能与工程规范.md
├── 系统使用手册.md
├── 脚本结构.md
├── 工程清理与优化记录.md
├── 01_必读/
│   └── StoreAndInventory_README.md
├── 02_参考/
│   ├── 一些特别的使用方法.md
│   └── UI结构.md
├── 整合与优化/
│   ├── 01_AI速读_商店背包.md
│   ├── 02_接入主工程清单.md
│   ├── 03_修改物品系统注意事项.md
│   ├── 04_公共API与扩展点.md
│   ├── 00_整合优化总方案.md
│   └── 10_项目功能优化方案.md
└── 03_设计与远期/
    ├── 商店背包系统.md
    └── 背包系统跨场景持久化.md
```

---

## §3 与 Unity 工程一并交付

| 类型 | 路径（子工程） | 主工程 |
|---|---|---|
| 脚本 | `Assets/Script/StoreAndInventory/` | 拷贝，**排除 `Test/`** |
| Prefab | `Assets/prefab/` | Data + Service + 格子模板 |
| UI | — | UI 子树 **合并主 Canvas**（策略 B） |
| SO | 物品 + ShopTable 资产 | 路径自定 |
| 文档 | **本 `交付包/` 文件夹** | 放 `Docs/StoreInventory/` 或 Obsidian vault |

---

## §4 文档 frontmatter 约定

| 字段 | 含义 |
|---|---|
| `delivery: 主工程打包` | 随工程交付 |
| 无此字段且在 `_本地归档/` | **不交付** |

---

## §5 接入步骤摘要

1. 拷贝代码 + Prefab + **本文件夹**
2. 实例化 Data / Service Prefab；UI 合并主 Canvas
3. 入口：NPC/HUD 调 `ShopService.Open` + `PanelController.OpenShop`
4. 存档：`StoreSaveService.CaptureAllJson` / `ApplyAllJson`
5. 详见 [[02_接入主工程清单]]
