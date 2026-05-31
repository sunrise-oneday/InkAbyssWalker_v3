# UI 与音频系统

## UI 系统 (`Assets/Scripts/UI/`)

### BasePanel
UI 面板基类：`OnOpen()` / `OnClose()` / `OnRefresh()` / `OnActivate()` / `OnDeactivate()` / `SetActive()`
所有面板（战斗面板、设置面板、商店面板）均继承此类。

### 战斗 UI (`Assets/Scripts/UI/Battle/`)

- **BattleUIController**：Facade 模式，统一管理 info/action/result/tooltip/HUD 面板
- **BattleActionPanel**：操作面板，动态生成技能按钮/形态按钮/结束回合，每个按钮自动附带 TooltipTrigger
- **BattleInfoPanel**：信息面板，显示回合数、AP/MP/大招能量条，大招按钮交互控制（能量不足时置灰）
- **BattleResultPanel**：结果面板（胜利/战败）
- **EntityHudSpawner**：血条生成器，战斗开始时为所有队伍成员动态创建 EntityHUD 实例
- **BuffTooltipPanel**：Buff 浮动提示面板，鼠标悬停图标时显示
- **FixedTooltipPanel**：固定位置技能详情提示面板，渐入渐出/弹性缩放/文字交叉淡入淡出动画

### 设置面板 (`Assets/Scripts/UI/Settings/`)

- **SettingsController**：全局设置控制器。ESC 打开/关闭暂停菜单，Q/E 切换设置标签页
- **AudioSettingsPanel**：音频设置（主音量/BGM/SFX 滑动条 + 静音开关）
- **ControlsSettingsPanel**：控制设置
- **GameSettingsPanel**：游戏设置

## 音频系统 (`Assets/Scripts/Audio/AudioManager.cs`)

单例（MonoBehaviour），负责 BGM/SFX 播放与三级音量控制。

公共 API：
- `PlayBGM(AudioClip)` / `StopBGM()` / `PlaySFX(AudioClip)` / `PlaySFXAtPoint(clip, pos, blend)`
- `SetMasterVolume(float)` / `SetBGMVolume(float)` / `SetSFXVolume(float)`

关键设计：
- 通过 PlayerPrefs 持久化三级音量（Master/BGM/SFX）
- 音量计算公式：`master * subVolume`
- DontDestroyOnLoad
