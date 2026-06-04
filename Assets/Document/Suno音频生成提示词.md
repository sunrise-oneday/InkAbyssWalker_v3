# Suno 音频生成提示词文档

> 项目：《墨渊行者》(InkAbyssWalker)
> 类型：2D 水墨风格类银河城回合制动作 RPG
> 生成工具：Suno AI (https://suno.com)
> 最后更新：2026-06-09

---

## 目录

1. [使用说明](#一使用说明)
2. [BGM 背景音乐](#二bgm-背景音乐)
3. [SFX 音效](#三sfx-音效)
4. [Suno 参数建议](#四suno-参数建议)
5. [命名规范](#五命名规范)

---

## 一、使用说明

### 1.1 Suno 基础用法

1. 访问 https://suno.com 并登录
2. 选择 "Create" → "Custom" 模式
3. 将下方提示词粘贴到 "Lyrics" 或 "Description" 字段
4. 设置 Style of Music（风格标签）
5. 设置 Title（标题）
6. 点击 "Create" 生成

### 1.2 提示词结构

每个提示词包含以下部分：
- **描述 (Description)**：音乐的整体氛围、情绪、场景
- **风格标签 (Style)**：音乐流派和乐器
- **时长建议**：推荐的音频时长
- **导出格式**：建议导出为 WAV 或 MP3

### 1.3 导入 Unity

生成的音频文件放入 `Assets/Resources/Audio/` 目录：
- BGM → `Assets/Resources/Audio/BK/`
- SFX → `Assets/Resources/Audio/Sound/`

---

## 二、BGM 背景音乐

### 2.1 主菜单 BGM

**文件名**：`BGM_MainMenu`

**描述 (Description)**：
```
Ancient Chinese ink wash painting atmosphere, ethereal and mysterious. 
A lonely guqin plays a slow, melancholic melody with subtle echo effects. 
Distant flute joins in, creating a sense of vast emptiness. 
Water droplet sounds occasionally punctuate the silence. 
The overall mood is contemplative, like standing at the edge of an abyss.
```

**风格标签 (Style)**：
```
Chinese traditional, ambient, ethereal, guqin, flute, atmospheric, dark ambient
```

**时长建议**：2-3 分钟（循环）

**备注**：作为游戏开场和暂停界面的背景音乐，需要营造水墨深渊的神秘氛围

---

### 2.2 探索区域 BGM - 墨渊表层

**文件名**：`BGM_Explore_Surface`

**描述 (Description)**：
```
Gentle exploration music for a dark ink wash world. 
Soft guzheng plucking with flowing water sounds in the background. 
Subtle bamboo flute melody weaves through ambient darkness. 
Occasional ink droplet sounds and distant echoes. 
The pace is slow and wandering, encouraging exploration. 
Mysterious yet not threatening, like walking through an ancient painting.
```

**风格标签 (Style)**：
```
Chinese traditional, ambient, guzheng, bamboo flute, atmospheric, exploration, calm
```

**时长建议**：3-4 分钟（循环）

**备注**：探索安全区域时使用，节奏缓慢，鼓励玩家探索环境

---

### 2.3 探索区域 BGM - 危险区域

**文件名**：`BGM_Explore_Danger`

**描述 (Description)**：
```
Tense exploration music with underlying threat. 
Low drone sounds mixed with occasional sharp guzheng notes. 
Heartbeat-like percussion slowly builds tension. 
Distant war drums hint at approaching danger. 
Ink splash sound effects integrated into rhythm. 
The atmosphere is uneasy, keeping player alert while exploring.
```

**风格标签 (Style)**：
```
Chinese traditional, dark ambient, tension, low drone, percussion, atmospheric, suspense
```

**时长建议**：3-4 分钟（循环）

**备注**：进入敌人区域或接近 Boss 房间时使用

---

### 2.4 普通战斗 BGM

**文件名**：`BGM_Battle_Normal`

**描述 (Description)**：
```
Fast-paced battle music with Chinese traditional instruments. 
Driving taiko drums provide the main rhythm. 
Pipa (Chinese lute) plays aggressive rapid-fire melodies. 
Erhu adds emotional intensity during chorus sections. 
Gong crashes mark important moments. 
The tempo is energetic but controlled, matching turn-based combat pacing. 
Includes brief pauses for player decision-making moments.
```

**风格标签 (Style)**：
```
Chinese traditional, battle, taiko drums, pipa, erhu, energetic, action, RPG battle
```

**时长建议**：2-3 分钟（循环）

**备注**：普通敌人战斗使用，节奏紧凑但有间歇，适合回合制战斗

---

### 2.5 Boss 战 BGM - 阶段1

**文件名**：`BGM_Boss_Phase1`

**描述 (Description)**：
```
Intimidating boss battle music with heavy Chinese orchestration. 
Massive war drums boom like thunder. 
Suona (Chinese oboe) screams with piercing intensity. 
Low brass and strings create wall of sound. 
The rhythm is relentless and oppressive. 
Occasional quiet moments build anticipation before explosive sections. 
Sense of facing an overwhelming enemy.
```

**风格标签 (Style)**：
```
Chinese traditional, epic, boss battle, war drums, suona, orchestral, intense, dark
```

**时长建议**：3-4 分钟（循环）

**备注**：Boss 战第一阶段（HP 100%-50%），压迫感强

---

### 2.6 Boss 战 BGM - 阶段2

**文件名**：`BGM_Boss_Phase2`

**描述 (Description)**：
```
Desperate final boss phase music. 
Tempo increases significantly from phase 1. 
All instruments play at maximum intensity. 
Suona reaches its highest, most frantic register. 
Double-time taiko drums create chaos. 
Brief moments of silence before explosive restarts. 
The feeling is do-or-die, last stand against ultimate evil. 
Ink splash sound effects sync with major beats.
```

**风格标签 (Style)**：
```
Chinese traditional, epic, boss battle, intense, frantic, suona, taiko, climactic, dark epic
```

**时长建议**：3-4 分钟（循环）

**备注**：Boss 战第二阶段（HP <50%），更加激烈绝望

---

### 2.7 商店 BGM

**文件名**：`BGM_Shop`

**描述 (Description)**：
```
Quirky merchant music with mysterious undertones. 
Light pipa plucking with playful rhythm. 
Subtle wind chimes and coin sound effects. 
Xiao (Chinese vertical flute) adds mystical merchant atmosphere. 
The tempo is moderate, unhurried. 
Feels like browsing wares in a shadowy ink painting shop. 
Friendly but with hint of shrewdness.
```

**风格标签 (Style)**：
```
Chinese traditional, merchant, playful, pipa, xiao, light, quirky, mysterious shop
```

**时长建议**：2-3 分钟（循环）

**备注**：商店界面使用，轻松有趣但带有神秘感

---

### 2.8 胜利 BGM

**文件名**：`BGM_Victory`

**描述 (Description)**：
```
Triumphant victory fanfare with Chinese traditional instruments. 
Gong crash followed by ascending guzheng arpeggios. 
Flute plays victorious melody. 
Light percussion celebration rhythm. 
The mood is victorious but tempered with ink painting aesthetics. 
Not too bombastic, maintains the artistic atmosphere. 
Brief 15-20 second piece that loops or fades.
```

**风格标签 (Style)**：
```
Chinese traditional, victory, triumphant, guzheng, flute, celebratory, short fanfare
```

**时长建议**：15-30 秒

**备注**：战斗胜利后播放，简短有力

---

### 2.9 失败 BGM

**文件名**：`BGM_Defeat`

**描述 (Description)**：
```
Somber defeat music with ink wash melancholy. 
Single erhu plays mournful descending melody. 
Slow, heavy taiko beats like a fading heartbeat. 
Ambient ink droplet sounds fade to silence. 
The mood is tragic but not hopeless. 
Suggests the journey continues despite this setback. 
Brief piece that transitions back to menu or retry.
```

**风格标签 (Style)**：
```
Chinese traditional, defeat, somber, erhu, melancholic, slow, tragic, atmospheric
```

**时长建议**：20-30 秒

**备注**：战斗失败后播放，悲壮但不绝望

---

## 三、SFX 音效

### 3.1 玩家动作音效

#### 3.1.1 脚步声

**文件名**：`SFX_Footstep`

**描述**：
```
Soft cloth shoe stepping on stone floor. 
Light, quick footstep sound. 
Subtle echo suggesting enclosed stone environment. 
Multiple variations needed (3-5 different takes).
```

**风格标签**：`footstep, cloth, stone, soft, indoor`
**时长**：0.1-0.2 秒
**数量**：生成 3-5 个变体

---

#### 3.1.2 跳跃

**文件名**：`SFX_Jump`

**描述**：
```
Clothing rustle and whoosh sound. 
Light cloth flapping in air. 
Subtle ink splash at launch point. 
Ascending pitch movement.
```

**风格标签**：`jump, whoosh, cloth, light, ascending`
**时长**：0.2-0.3 秒

---

#### 3.1.3 冲刺/闪避

**文件名**：`SFX_Dash`

**描述**：
```
Quick swoosh sound with ink trail effect. 
Fast movement through air. 
Subtle ink splash at start and end. 
Speed lines sound effect.
```

**风格标签**：`dash, swoosh, fast, ink, movement`
**时长**：0.2-0.4 秒

---

#### 3.1.4 普通攻击

**文件名**：`SFX_Attack_Normal`

**描述**：
```
Brush stroke slashing sound. 
Ink brush swiping through air. 
"Wah" sound like painting with force. 
Subtle ink splash on impact.
```

**风格标签**：`attack, brush, slash, ink, swipe, painting`
**时长**：0.2-0.3 秒
**数量**：生成 3 个变体（连击用）

---

#### 3.1.5 普通格挡

**文件名**：`SFX_Block_Normal`

**描述**：
```
Ink droplet collision sound. 
"Wap" sound of ink hitting shield. 
Subtle splash and bounce. 
Dull impact sound.
```

**风格标签**：`block, ink, impact, splash, dull`
**时长**：0.1-0.2 秒

---

#### 3.1.6 完美格挡（弹反）

**文件名**：`SFX_Block_Perfect`

**描述**：
```
Crystal clear chime sound. 
Qing stone bell resonance. 
Perfect timing reward sound. 
Bright, satisfying "ding" with ink splash overlay.
```

**风格标签**：`perfect block, chime, crystal, bright, satisfying, bell`
**时长**：0.3-0.5 秒

---

#### 3.1.7 完美闪避

**文件名**：`SFX_Dodge_Perfect`

**描述**：
```
Time slowing whoosh sound. 
Echo effect with ink trail. 
Phantom afterimage sound. 
Ethereal, ghostly movement.
```

**风格标签**：`perfect dodge, whoosh, echo, phantom, ethereal, slow motion`
**时长**：0.3-0.5 秒

---

### 3.2 技能音效

#### 3.2.1 墨刺（初临技能）

**文件名**：`SFX_Skill_InkThorn`

**描述**：
```
Sharp ink needle shooting sound. 
Quick, precise piercing motion. 
Ink splatter on hit. 
Fast, focused attack sound.
```

**风格标签**：`skill, ink, needle, pierce, sharp, focused`
**时长**：0.2-0.3 秒

---

#### 3.2.2 墨盾（初临技能）

**文件名**：`SFX_Skill_InkShield`

**描述**：
```
Ink barrier forming sound. 
Liquid ink solidifying into shield. 
Protective bubble activation. 
Defensive ink splash.
```

**风格标签**：`skill, shield, ink, barrier, defensive, liquid`
**时长**：0.3-0.5 秒

---

#### 3.2.3 破军（破墨技能）

**文件名**：`SFX_Skill_BreakArmy`

**描述**：
```
Massive ink explosion sound. 
Destructive force release. 
Heavy impact with ink splatter. 
Powerful, aggressive attack.
```

**风格标签**：`skill, explosion, ink, destructive, heavy, powerful`
**时长**：0.3-0.5 秒

---

#### 3.2.4 墨愈（归墨技能）

**文件名**：`SFX_Skill_InkHeal`

**描述**：
```
Healing ink absorption sound. 
Gentle liquid flow into body. 
Recovery chime overlay. 
Soothing, restorative effect.
```

**风格标签**：`skill, heal, ink, recovery, soothing, gentle`
**时长**：0.5-0.8 秒

---

#### 3.2.5 大招释放

**文件名**：`SFX_Ultimate`

**描述**：
```
Ultimate skill activation sound. 
Massive ink explosion with energy buildup. 
Cinematic, dramatic effect. 
Ground-shaking impact with ink tsunami.
```

**风格标签**：`ultimate, explosion, cinematic, dramatic, massive, energy`
**时长**：1.0-1.5 秒

---

### 3.3 敌人音效

#### 3.3.1 敌人攻击

**文件名**：`SFX_Enemy_Attack`

**描述**：
```
Ink creature attacking sound. 
Sludge-like movement with splash. 
Hostile ink splatter. 
Multiple variations for different enemies.
```

**风格标签**：`enemy, attack, ink, sludge, hostile, splash`
**时长**：0.2-0.3 秒
**数量**：生成 3 个变体

---

#### 3.3.2 敌人受伤

**文件名**：`SFX_Enemy_Hit`

**描述**：
```
Ink creature taking damage. 
Splash and splatter sound. 
Impact with ink dispersal. 
Pained reaction sound.
```

**风格标签**：`enemy, hit, ink, splash, impact, damage`
**时长**：0.1-0.2 秒

---

#### 3.3.3 敌人死亡

**文件名**：`SFX_Enemy_Death`

**描述**：
```
Ink creature dissolving. 
Ink block shattering and splashing. 
Dissolution into puddle. 
Satisfying destruction sound.
```

**风格标签**：`enemy, death, ink, shatter, dissolve, splash`
**时长**：0.3-0.5 秒

---

#### 3.3.4 Boss 特殊攻击

**文件名**：`SFX_Boss_SpecialAttack`

**描述**：
```
Boss charging massive attack. 
Energy buildup with ominous rumble. 
Ink gathering and compressing. 
Explosive release with devastating force.
```

**风格标签**：`boss, special, charge, ominous, explosive, devastating`
**时长**：0.8-1.2 秒

---

### 3.4 元素反应音效

#### 3.4.1 融化反应（火+冰）

**文件名**：`SFX_Element_Melt`

**描述**：
```
Fire and ice collision. 
Steam explosion sound. 
Sizzling and cracking. 
Elemental fusion reaction.
```

**风格标签**：`element, melt, fire, ice, steam, explosion`
**时长**：0.3-0.5 秒

---

#### 3.4.2 蒸发反应（水+火）

**文件名**：`SFX_Element_Vaporize`

**描述**：
```
Water hitting hot surface. 
Violent steam burst. 
Evaporation explosion. 
Dramatic elemental reaction.
```

**风格标签**：`element, vaporize, water, fire, steam, burst`
**时长**：0.3-0.5 秒

---

### 3.5 UI 音效

#### 3.5.1 按钮点击

**文件名**：`SFX_UI_Click`

**描述**：
```
Paper page turning sound. 
Soft, gentle click. 
Subtle brush stroke. 
Clean, minimal feedback.
```

**风格标签**：`UI, click, paper, gentle, brush, minimal`
**时长**：0.05-0.1 秒

---

#### 3.5.2 菜单打开

**文件名**：`SFX_UI_MenuOpen`

**描述**：
```
Scroll unrolling sound. 
Paper unfolding. 
Subtle ink brush accent. 
Opening reveal effect.
```

**风格标签**：`UI, menu, scroll, paper, unfold, reveal`
**时长**：0.2-0.3 秒

---

#### 3.5.3 菜单关闭

**文件名**：`SFX_UI_MenuClose`

**描述**：
```
Scroll rolling up sound. 
Paper closing. 
Subtle completion accent. 
Closing effect.
```

**风格标签**：`UI, menu, scroll, roll, close, complete`
**时长**：0.15-0.25 秒

---

#### 3.5.4 物品拾取

**文件名**：`SFX_UI_Pickup`

**描述**：
```
Item collection chime. 
Bright, rewarding sound. 
Subtle ink splash accent. 
Satisfying pickup feedback.
```

**风格标签**：`UI, pickup, chime, bright, rewarding, satisfying`
**时长**：0.15-0.25 秒

---

#### 3.5.5 物品使用

**文件名**：`SFX_UI_UseItem`

**描述**：
```
Item activation sound. 
Consumable use effect. 
Subtle magical ink accent. 
Use confirmation feedback.
```

**风格标签**：`UI, use, item, activation, magical, confirmation`
**时长**：0.2-0.3 秒

---

#### 3.5.6 错误/无法操作

**文件名**：`SFX_UI_Error`

**描述**：
```
Gentle error sound. 
Non-intrusive negative feedback. 
Subtle "cannot do" indicator. 
Polite denial sound.
```

**风格标签**：`UI, error, gentle, negative, denial, polite`
**时长**：0.1-0.15 秒

---

### 3.6 环境音效

#### 3.6.1 水滴声

**文件名**：`SFX_Env_WaterDrop`

**描述**：
```
Single water droplet falling into ink puddle. 
Echo in enclosed space. 
Subtle ripple effect. 
Atmospheric ambient sound.
```

**风格标签**：`environment, water, drop, echo, ripple, ambient`
**时长**：0.3-0.5 秒
**数量**：生成 3-5 个变体

---

#### 3.6.2 墨雾流动

**文件名**：`SFX_Env_InkFog`

**描述**：
```
Ink mist flowing sound. 
Low, atmospheric drone. 
Subtle movement through space. 
Mysterious ambient texture.
```

**风格标签**：`environment, ink, fog, drone, atmospheric, mysterious`
**时长**：2-3 秒（循环）
**数量**：生成 2-3 个变体

---

#### 3.6.3 宝箱打开

**文件名**：`SFX_Env_ChestOpen`

**描述**：
```
Ancient chest opening. 
Wood and metal creaking. 
Subtle magical ink release. 
Treasure reveal anticipation.
```

**风格标签**：`environment, chest, open, creak, magical, treasure`
**时长**：0.5-0.8 秒

---

#### 3.6.4 检查点激活

**文件名**：`SFX_Env_Checkpoint`

**描述**：
```
Checkpoint activation sound. 
Save point confirmation. 
Ink energy gathering and stabilizing. 
Safe haven resonance.
```

**风格标签**：`environment, checkpoint, save, activate, stable, safe`
**时长**：0.5-0.8 秒

---

### 3.7 破防/眩晕音效

#### 3.7.1 破防音效

**文件名**：`SFX_Break_Shatter`

**描述**：
```
Defense breaking sound. 
Shield shattering into ink fragments. 
Dramatic impact with slow-motion effect. 
Vulnerability moment emphasis.
```

**风格标签**：`break, shatter, shield, dramatic, impact, vulnerability`
**时长**：0.3-0.5 秒

---

#### 3.7.2 眩晕音效

**文件名**：`SFX_Stun_Dizzy`

**描述**：
```
Stun effect sound. 
Dizzy, disorienting feedback. 
Spinning ink particles. 
Confusion audio representation.
```

**风格标签**：`stun, dizzy, disorient, spinning, confusion`
**时长**：0.3-0.5 秒

---

## 四、Suno 参数建议

### 4.1 BGM 生成建议

| 参数 | 建议值 | 说明 |
|------|--------|------|
| 模型 | V3.5 或 V4 | 使用最新模型获得更好质量 |
| 时长 | 2-4 分钟 | 足够循环播放 |
| 风格多样性 | 中等 | 保持一致的水墨风格 |
| 歌词 | 纯音乐 | 大部分 BGM 无人声 |

### 4.2 SFX 生成建议

| 参数 | 建议值 | 说明 |
|------|--------|------|
| 模型 | V3.5 | 音效更适合短时长 |
| 时长 | 0.1-1.5 秒 | 音效要短小精悍 |
| 风格多样性 | 高 | 不同音效需要差异明显 |
| 歌词 | 纯音乐 | 音效无人声 |

### 4.3 质量优化

1. **多次生成**：每个提示词生成 3-5 次，选择最佳版本
2. **变体生成**：重要音效生成多个变体，避免重复感
3. **后期处理**：在 Unity 中调整音量、混响等参数
4. **循环测试**：BGM 需要测试无缝循环效果

---

## 五、命名规范

### 5.1 文件命名规则

```
[类型]_[场景/用途]_[具体名称]
```

**示例**：
- `BGM_MainMenu` - 主菜单背景音乐
- `BGM_Battle_Normal` - 普通战斗背景音乐
- `SFX_Attack_Normal` - 普通攻击音效
- `SFX_Skill_InkThorn` - 墨刺技能音效
- `SFX_Env_WaterDrop` - 环境水滴音效

### 5.2 Unity 中的组织结构

```
Assets/Resources/Audio/
├── BK/
│   ├── BGM_MainMenu.mp3
│   ├── BGM_Explore_Surface.mp3
│   ├── BGM_Explore_Danger.mp3
│   ├── BGM_Battle_Normal.mp3
│   ├── BGM_Boss_Phase1.mp3
│   ├── BGM_Boss_Phase2.mp3
│   ├── BGM_Shop.mp3
│   ├── BGM_Victory.mp3
│   └── BGM_Defeat.mp3
└── Sound/
    ├── SFX_Footstep_01.mp3
    ├── SFX_Footstep_02.mp3
    ├── SFX_Footstep_03.mp3
    ├── SFX_Jump.mp3
    ├── SFX_Dash.mp3
    ├── SFX_Attack_Normal_01.mp3
    ├── SFX_Block_Normal.mp3
    ├── SFX_Block_Perfect.mp3
    └── ...
```

---

## 附录：快速复制提示词

### 主菜单 BGM 快速复制

```
Ancient Chinese ink wash painting atmosphere, ethereal and mysterious. A lonely guqin plays a slow, melancholic melody with subtle echo effects. Distant flute joins in, creating a sense of vast emptiness. Water droplet sounds occasionally punctuate the silence. The overall mood is contemplative, like standing at the edge of an abyss.
```

风格：`Chinese traditional, ambient, ethereal, guqin, flute, atmospheric, dark ambient`

---

### 普通战斗 BGM 快速复制

```
Fast-paced battle music with Chinese traditional instruments. Driving taiko drums provide the main rhythm. Pipa (Chinese lute) plays aggressive rapid-fire melodies. Erhu adds emotional intensity during chorus sections. Gong crashes mark important moments. The tempo is energetic but controlled, matching turn-based combat pacing. Includes brief pauses for player decision-making moments.
```

风格：`Chinese traditional, battle, taiko drums, pipa, erhu, energetic, action, RPG battle`

---

### Boss 战 BGM 快速复制

```
Intimidating boss battle music with heavy Chinese orchestration. Massive war drums boom like thunder. Suona (Chinese oboe) screams with piercing intensity. Low brass and strings create wall of sound. The rhythm is relentless and oppressive. Occasional quiet moments build anticipation before explosive sections. Sense of facing an overwhelming enemy.
```

风格：`Chinese traditional, epic, boss battle, war drums, suona, orchestral, intense, dark`

---

### 普通攻击音效快速复制

```
Brush stroke slashing sound. Ink brush swiping through air. "Wah" sound like painting with force. Subtle ink splash on impact.
```

风格：`attack, brush, slash, ink, swipe, painting`

---

## 版本历史

| 版本 | 日期 | 更新内容 |
|------|------|----------|
| 1.0 | 2026-06-09 | 初始版本，包含完整 BGM 和 SFX 提示词 |

---

> **注意**：本提示词文档基于《墨渊行者》游戏设计文档生成，所有音频应保持水墨风格的一致性。建议定期根据游戏开发进度更新和补充提示词。
