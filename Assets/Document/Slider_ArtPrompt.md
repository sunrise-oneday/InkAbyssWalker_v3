# Slider 组件 - 生图提示词（水墨画风格）

## 项目信息
- 游戏名：InkAbyssWalker（墨渊行者）
- 美术风格：传统中国水墨画（宣纸底、墨色边框、山水竹石）
- 目标：生成各部件素材，由 Python 做背景扣除

---

## 结构拆解

| 部件 | 用途 | 建议尺寸 |
|------|------|----------|
| Background | 滑块轨道底图 | 400×50 横条 |
| Fill | 填充色块 | 400×50 横条 |
| Handle | 拖动手柄 | 60×60 方形 |

所有部件生成时**使用纯白色背景**，方便 Python 扣除。

---

## 参考风格关键词

核心视觉元素（从参考图提取）：
- 米黄色宣纸质感底色（#F5E6C8 ~ #EDE0CC）
- 墨色（浓墨/淡墨/焦墨）毛笔边框
- 山水画元素：远山、松树、竹叶
- 祥云纹样装饰
- 水墨晕染过渡（浓淡渐变）

---

## 1. Background（轨道底图）

### 中文提示词
```
中国传统水墨画风格，水平长条形UI滑块轨道底图。
米黄色宣纸质感背景，上下两侧有淡墨毛笔线条勾勒的细边框，
左侧有淡淡的远山水墨剪影，右侧有几笔竹叶点缀，
整体呈现宣纸的温暖米黄色调，带有自然的纸张纤维纹理，
边缘有轻微的墨色晕染，画面干净简洁，
2D游戏UI素材，纯白色背景便于抠图，工笔与写意结合
```

### English Prompt
```
Traditional Chinese ink wash painting style, horizontal bar UI slider track background.
Warm beige rice paper texture, thin ink brush stroke borders on top and bottom edges,
faint distant mountain silhouette on the left, a few bamboo leaf strokes on the right,
natural rice paper fiber texture throughout, subtle ink bleeding at edges,
clean and minimal composition, 2D game UI asset,
solid white background for easy removal, gongbi meets xieyi style
```

### Stable Diffusion 格式
```
(traditional Chinese ink wash painting:1.4), (rice paper texture:1.4),
UI slider track background, horizontal bar, (warm beige:1.3),
thin ink brush stroke borders, distant mountain silhouette left,
bamboo leaf strokes right, paper fiber texture, subtle ink bleeding edges,
clean minimal composition, solid white background, 2D UI asset
Negative: dark background, neon colors, 3D render, modern style, text, watermark
```

### Midjourney 格式
```
/imagine prompt: traditional Chinese ink wash painting UI slider track, horizontal bar,
warm beige rice paper texture, thin ink brush borders, faint mountain silhouette,
bamboo leaf accents, paper fiber texture, clean minimal, solid white background --ar 8:1 --style raw
```

---

## 2. Fill（填充色块）

### 中文提示词
```
中国传统水墨画风格，水平长条形UI滑块填充条。
从左到右由浓墨渐变到淡墨的水墨渐变效果，
带有毛笔运笔的自然笔触纹理，墨色有浓淡干湿的变化，
像是刚用毛笔蘸墨一笔横扫而过，起笔处墨色浓重，
收笔处墨色渐淡带有飞白效果，整体呈现水墨的流动感，
2D游戏UI素材，纯白色背景便于抠图
```

### English Prompt
```
Traditional Chinese ink wash painting style, horizontal bar UI slider fill bar.
Left to right gradient from heavy ink to light ink wash effect,
natural brush stroke texture with variations in ink density,
like a single horizontal brushstroke across rice paper,
heavy ink at the start, fading to light ink with feibai (flying white) effect at the end,
fluid ink wash motion feel, 2D game UI asset,
solid white background for easy removal
```

### Stable Diffusion 格式
```
(traditional Chinese ink wash painting:1.4), (brush stroke texture:1.3),
horizontal bar, ink gradient left to right, (heavy ink to light ink:1.3),
feibai flying white brush effect, natural ink density variation,
rice paper texture, fluid ink motion, solid white background, 2D UI asset
Negative: dark background, neon colors, 3D render, modern style, text, watermark, geometric shapes
```

### Midjourney 格式
```
/imagine prompt: traditional Chinese ink wash painting, horizontal slider fill bar,
heavy ink to light ink gradient, feibai brush effect, natural ink density variation,
rice paper texture, fluid single brushstroke feel, solid white background --ar 8:1 --style raw
```

---

## 3. Handle（拖动手柄）

### 中文提示词
```
中国传统水墨画风格，圆形UI滑块手柄。
中央有一朵小号的水墨祥云纹样，用淡墨勾勒，
边缘有水墨晕染的柔和过渡，像是墨滴在宣纸上自然晕开的效果，
整体呈米黄色宣纸底色，带有轻微的纸张纤维纹理，
手柄形状为正圆形，祥云图案居中，
2D游戏UI素材，纯白色背景便于抠图
```

### English Prompt
```
Traditional Chinese ink wash painting style, circular UI slider handle.
Center features a small ink cloud pattern (xiangyun), drawn with light ink,
soft ink bleeding transitions at edges, like ink drops naturally spreading on rice paper,
warm beige rice paper base color, subtle paper fiber texture,
perfectly circular shape, cloud motif centered,
2D game UI asset, solid white background for easy removal
```

### Stable Diffusion 格式
```
(traditional Chinese ink wash painting:1.4), (circular UI handle:1.3),
(rice paper texture:1.3), small xiangyun cloud pattern center,
light ink brushwork, soft ink bleeding edges, ink drop spreading effect,
warm beige color, paper fiber texture, solid white background, 2D UI asset
Negative: dark background, neon colors, 3D render, modern style, text, watermark
```

### Midjourney 格式
```
/imagine prompt: traditional Chinese ink wash painting, circular slider handle,
small xiangyun cloud pattern center, light ink brushwork,
soft ink bleeding edges, rice paper texture, warm beige,
solid white background --ar 1:1 --style raw
```

---

## 色值参考

| 元素 | 主色 | 说明 |
|------|------|------|
| 宣纸底色 | #F5E6C8 ~ #EDE0CC | 温暖米黄，不同深浅均可 |
| 浓墨 | #2C2C2C ~ #1A1A1A | 边框、重笔触 |
| 淡墨 | #8B8682 ~ #A09890 | 远山、淡装饰 |
| 飞白 | #C8B89A ~ #D4C8B0 | 笔触末端飞白效果 |
| 祥云 | #6B6560 ~ #7D7570 | 淡墨勾勒纹样 |

## Python 扣图建议

所有部件使用**纯白色背景**生成，然后用以下脚本扣除：

```python
from PIL import Image
import numpy as np

def remove_white_bg(img_path, output_path, threshold=240):
    """扣除纯白色背景，保留水墨元素"""
    img = Image.open(img_path).convert("RGBA")
    data = np.array(img)

    # 将接近纯白的像素设为透明
    r, g, b, a = data[:,:,0], data[:,:,1], data[:,:,2], data[:,:,3]
    white_mask = (r > threshold) & (g > threshold) & (b > threshold)
    data[white_mask, 3] = 0

    Image.fromarray(data).save(output_path)
    print(f"已保存: {output_path}")

# 使用示例
remove_white_bg("slider_bg_raw.png", "slider_bg.png")
```

## 生成顺序建议

1. 先生 Background（轨道底图）—— 确认宣纸质感和边框风格
2. 再生 Fill（填充条）—— 确认墨色浓淡渐变效果
3. 最后生 Handle（手柄）—— 确认祥云纹样大小和位置
