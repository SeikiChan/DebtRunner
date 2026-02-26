# 死亡结算面板 (Death Panel) 使用指南

## 快速开始

### 步骤 1: 生成 UI 模板

1. 在 Unity 编辑器中打开您的场景
2. 确保场景中存在 **Canvas**（如果没有，先创建一个）
3. 在菜单栏选择：**GameObject → DebtRunner → Create Death Panel Template**
4. 系统会自动生成完整的死亡结算面板 UI

### 步骤 2: 配置 GameFlowController

1. 找到场景中的 **GameFlowController**
2. 在 Inspector 面板中找到 **Panel Death** 字段
3. 将自动生成的 **Panel_Death** GameObject 拖拽到此字段

### 步骤 3: 自定义文案和素材

#### 文案配置位置
在 **Panel_Death** 的 **DeathPanel** 脚本中：

| 字段 | 说明 | 默认值 |
|------|------|--------|
| **Death Title** | 通用死亡标题 | "YOU DIED!" |
| **Killed By Monster Title** | 被怪物击杀时显示 | "SLAIN!" |
| **Killed By Monster Description** | 被怪物击杀的描述 | "You were defeated by enemies in battle." |
| **Debt Failure Title** | 债务失败时显示 | "DEBT DEFAULTED!" |
| **Debt Failure Description** | 债务失败的描述 | "You couldn't pay back your debt. Time's up." |

#### 素材替换

在 **DeathPanel** 脚本的 **可替换素材** 部分：

| 字段 | 说明 |
|------|------|
| **Monster Kill Background** | 被怪物击杀时的背景图像 (Sprite) |
| **Debt Failure Background** | 债务失败时的背景图像 (Sprite) |

**替换步骤：**
1. 在 Project 面板中找到您的背景图像 Sprite
2. 拖拽到对应的字段中
3. 如果不设置，则使用默认的灰色背景

### 步骤 4: 动画配置

在 **DeathPanel** 脚本的 **显示动画配置** 部分调整：

| 字段 | 说明 | 推荐值 |
|------|------|--------|
| **Fade In Duration** | 淡入时长（秒） | 0.3 |
| **Display Duration** | 停留时长（秒） | 2.0 |
| **Fade Out Duration** | 淡出时长（秒） | 0.3 |
| **Auto Show And Hide** | 自动显示与隐藏 | 打开 ✓ |

## UI 层级结构

生成的 UI 模板包含以下结构：

```
Canvas
└── Panel_Death (完整宽高覆盖)
    ├── Background (深色半透明覆盖)
    ├── BackgroundImage (可替换的背景素材)
    └── Content (居中内容容器)
        ├── DeathTitle (大标题: "YOU DIED!")
        ├── DeathReason (死因标签: "KILLED BY MONSTER")
        ├── DeathDescription (描述文本)
        └── TipText (提示文本: "Press Any Key to Continue...")
```

## 死亡触发方式

### 场景 1: 被怪物击杀
```
PlayerHealth.TakeDamage() 
  ↓
HP <= 0 
  ↓
GameFlowController.TriggerGameOver()
  ↓
显示 DeathType.KilledByMonster 面板
```

**显示内容：**
- 标题：Killed By Monster Title
- 描述：Killed By Monster Description
- 背景：Monster Kill Background

### 场景 2: 债务无法支付
```
ConfirmSettlementAndEnterShop() 
  ↓
Cash < Due 
  ↓
GameFlowController.ShowGameOverWithDeathPanel(FailedDebt)
  ↓
显示 DeathType.FailedDebt 面板
```

**显示内容：**
- 标题：Debt Failure Title
- 描述：Debt Failure Description
- 背景：Debt Failure Background

## 自定义示例

### 示例 1: 修改被怪物击杀的描述

在 DeathPanel Inspector 中：
- **Killed By Monster Title**: "SLAIN BY ENEMIES!"
- **Killed By Monster Description**: "You fought bravely but were overwhelmed."

### 示例 2: 替换背景素材

1. 准备两张图像素材：
   - `DeathScreen_KilledByMonster.png` (怪物击杀背景)
   - `DeathScreen_FailedDebt.png` (债务失败背景)

2. 导入到 Unity 并设置为 Sprite:
   - 右键 → Sprite → 2D and UI

3. 在 DeathPanel Inspector 中：
   - **Monster Kill Background**: 拖拽 `DeathScreen_KilledByMonster.png`
   - **Debt Failure Background**: 拖拽 `DeathScreen_FailedDebt.png`

### 示例 3: 调整颜色和样式

在 UI Hierarchy 中直接选中各个 Text 元素修改：

- **DeathTitle** → 修改字体大小、颜色、描边
- **DeathReason** → 修改标签文本和颜色
- **DeathDescription** → 修改描述内容

## 常见问题

**Q: 如何为不同的死因显示不同的音效？**
A: 可以在 DeathPanel 中扩展 `ShowDeathPanel()` 方法，根据 deathType 播放对应的音效。

**Q: 可以跳过自动显示/隐藏吗？**
A: 可以。关闭 **Auto Show And Hide** 选项，然后手动调用：
```csharp
deathPanel.HideDeathPanel(); // 手动隐藏
```

**Q: 如何在死亡后显示更多信息（如总回合数、总金钱）？**
A: 可以修改 DeathPanel 脚本，添加额外的文本字段来显示这些信息。

**Q: 背景图像尺寸应该是多少？**
A: 推荐使用与屏幕分辨率相同或更大的尺寸(如 1920x1080 或 1920x1440)，这样可以在各种分辨率下正常显示。

## 下一步

- 准备您的素材文件（背景图像等）
- 按照上述步骤进行配置
- 测试不同的死亡场景确保显示正确
