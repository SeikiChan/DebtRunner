# 死亡面板使用指南（双面板版）

本游戏有 **两个独立的死亡面板**，分别对应不同的死因：

| 面板名称 | 死因 | Inspector 字段 |
|----------|------|---------------|
| `Panel_Death_Monster` | 被怪物击杀 | GameFlowController → Panel Death Monster |
| `Panel_Death_Debt` | 债务失败 | GameFlowController → Panel Death Debt |

---

## 快速开始

### 步骤 1: 生成面板

1. 确保场景中存在 **Canvas**
2. 菜单栏 **GameObject → DebtRunner → Create Monster Death Panel** → 生成怪物击杀面板
3. 菜单栏 **GameObject → DebtRunner → Create Debt Failure Panel** → 生成债务失败面板

两个面板完全独立，各自有各自的文案、背景和颜色。

### 步骤 2: 连接到 GameFlowController

1. 在 Hierarchy 中选中 **GameFlowController** 所在的 GameObject
2. Inspector 中找到 Panels 区域：
   - `Panel Death Monster` → 拖入 **Panel_Death_Monster**
   - `Panel Death Debt` → 拖入 **Panel_Death_Debt**

### 步骤 3: 自定义

点击任一面板，在 `DeathPanel` 组件中可修改：

| 字段 | 说明 |
|------|------|
| Title | 大标题（如 "SLAIN!" 或 "DEBT DEFAULTED!"）|
| Reason | 原因标签（如 "KILLED BY MONSTER"）|
| Description | 详细描述 |
| Background | 可替换的背景 Sprite |

---

## UI 层级结构

每个面板的内部结构：

```
Panel_Death_Monster (或 Panel_Death_Debt)
├── Background          (半透明遮罩)
├── BackgroundImage      (可替换背景素材)
└── Content
    ├── DeathTitle       (大标题)
    ├── DeathReason      (死因标签)
    ├── DeathDescription (描述文字)
    ├── TipText          (提示文字)
    └── ButtonsContainer
        ├── Btn_Restart  (重新开始按钮)
        └── Btn_MainMenu (返回主菜单按钮)
```

---

## 触发流程

### 被怪物击杀
```
PlayerHealth.TakeDamage() → HP <= 0
  → GameFlowController.TriggerGameOver()
  → 显示 Panel_Death_Monster
```

### 债务失败
```
结算时 Cash < Due
  → GameFlowController.ShowGameOverWithDeathPanel(FailedDebt)
  → 显示 Panel_Death_Debt
```

---

## 常见问题

**Q: 之前已经有旧的 Panel_Death，怎么办？**
A: 删除旧的 Panel_Death，然后用菜单分别创建两个新面板。

**Q: 面板创建后文字没有显示？**
A: 模板会自动连接所有 Text 引用。如果手动创建面板，请确保 DeathPanel 组件中
   Title Text、Reason Text、Description Text 三个字段都已赋值。

**Q: 如何替换背景？**
A: 准备 Sprite 素材，拖到 DeathPanel 组件的 Background 字段即可。
