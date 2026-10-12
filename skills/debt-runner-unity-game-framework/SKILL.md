---
name: debt-runner-unity-game-framework
description: Unity game UI flow and technical framework template based on Debt Runner. Use when designing or implementing a Unity 2D game with menu/gameplay/settlement/shop/game-over/victory panels, Inspector-driven components, ScriptableObject data, CanvasGroup UI lifecycle, editor panel templates, HUD layout, tutorial overlays, or reusable UI architecture for future games.
---

# Debt Runner Unity Game Framework

## 核心目标

使用这个 skill 时，把 Debt Runner 当作一个可复用的 Unity 2D 游戏制作模板：先建立清晰的游戏状态机，再围绕状态机设计 UI flow、Panel 生命周期、Inspector 可调参数、Component 分工、Editor 模板和数据资产。

默认判断标准：

- 游戏流程必须由一个中心控制器统一管理。
- UI Panel 只负责显示、输入转发和自身内容更新。
- 复杂 Panel 用 Editor 菜单一键生成，避免手动搭层级导致绑定错误。
- 策划/美术需要调的内容放在 Inspector 或 ScriptableObject。
- 显隐和交互用 `CanvasGroup` 控制，不靠反复销毁/重建对象。

## 开工前必须先明确

写代码前先回答这些问题：

1. 游戏有哪些完整状态？
2. 玩家看到的 UI flow 是什么？
3. 哪些 Panel 是互斥主界面，哪些是覆盖层，哪些是 Modal？
4. 哪些数值和文案必须在 Inspector 可调？
5. 哪些商品、升级、权重、效果应该做成 ScriptableObject？
6. 哪些转场会暂停时间、恢复时间、锁输入、切 BGM？

不要从按钮开始设计。先从状态和 flow 开始。

## 推荐 UI Flow

Debt Runner 的默认 flow：

```text
Studio Splash
  -> Title
      -> Story Intro，可选首次漫画
      -> Gameplay
          -> Round Intro Overlay
          -> 战斗 + HUD
          -> Level Up Modal，可出现多次
          -> Times Up / Round Clear 转场
          -> Settlement
          -> Shop
          -> Next Round -> Gameplay
          -> Boss Round -> Victory Presentation
      -> Game Over Transition
      -> Death Panel 或 Run Summary
      -> Back To Title / Restart
```

用 enum 表达状态，不要堆布尔值：

```csharp
public enum GameState { Title, Gameplay, Settlement, Shop, GameOver, Victory }
public enum DeathType { KilledByMonster, FailedDebt }
```

玩家按钮只调用明确的 flow 方法：

- `StartRun()`
- `ConfirmSettlementAndEnterShop()`
- `EnterShop()`
- `NextRound()`
- `TriggerGameOver(DeathType deathType)`
- `Restart()`
- `BackToMenu()`
- `OpenPauseMenu()`
- `ResumeFromPauseMenu()`

转场细节放到 private helper，例如 `ShowRoundIntro`、`ShowGameOverTransition`、`FinalizeGameOver`、`RefreshHUD`、`SetGameplaySystemsActive`。

## Unity 场景结构

除非有加载性能或关卡隔离要求，默认使用一个主 gameplay scene。

推荐层级：

```text
Bootstrap / GameRoot
  GameFlowController
  Managers
    BGMManager
    SFXManager
    EventSystem
  World
    Player
    EnemyRoot
    ProjectileRoot
    PickupRoot
    Camera
  Canvas
    Panel_Title
    Panel_HUD
    Panel_LevelUp
    Panel_Settlement
    Panel_Shop
    Panel_Death_Monster
    Panel_Death_Debt
    Panel_RunSummary
    Panel_Victory
    Panel_PauseMenu
    Panel_Settings
    Overlay_RoundPresentation
```

保留稳定命名。Editor 模板和自动绑定会依赖类似 `Panel_HUD`、`Panel_Shop`、`Text_Cash`、`Btn_NextRound` 的名字。不要随意改生成对象名。

## 核心框架

### GameFlowController

把 `GameFlowController` 作为唯一的游戏流程权威，负责：

- 当前 `GameState`
- 回合、现金、债务、XP、等级、Boss 回合状态
- 顶层 Panel 显隐
- `Time.timeScale`
- 全局输入门控
- Gameplay 系统开关
- BGM 状态通知
- Game Over / Victory 路由

不要让按钮、商店卡片、死亡面板直接改变大流程。它们只调用 `GameFlowController` 暴露的方法。

`SwitchState(GameState next)` 是唯一处理大状态副作用的位置：

- 激活/隐藏 Title、HUD、Settlement、Shop、Death、Victory。
- 离开状态时清掉残留 Modal。
- 明确处理暂停和恢复时间。
- 通知音频管理器。
- 刷新鼠标、键盘、手柄和 UI 选择状态。

### Gameplay 组件

把玩法组件拆小：

- 玩家移动、射击、血量在 Player 组件上。
- 敌人生成、AI、攻击在 Enemy/Spawner 组件上。
- 掉落物自己处理吸附和拾取。
- 商店效果通过 `GameFlowController` 或对应玩家系统应用。
- Boss 攻击用独立 Boss controller，并用 enum 表达阶段和招式。

Flow controller 只调用窄接口：

```csharp
playerMotor.SetInputEnabled(active);
enemySpawner.SetSpawning(active);
playerShooter.ApplyUpgrade(upgrade);
playerHealth.ResetForRun();
```

## Panel 生命周期模板

大多数 Panel 使用这个模式：

```csharp
private CanvasGroup canvasGroup;

private void Awake()
{
    canvasGroup = GetComponent<CanvasGroup>();
    if (canvasGroup == null)
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
}

public void ShowPanel()
{
    UpdatePanelContent();
    transform.SetAsLastSibling();
    gameObject.SetActive(true);
    canvasGroup.alpha = 1f;
    canvasGroup.interactable = true;
    canvasGroup.blocksRaycasts = true;
}

public void HidePanel()
{
    canvasGroup.alpha = 0f;
    canvasGroup.interactable = false;
    canvasGroup.blocksRaycasts = false;
    gameObject.SetActive(false);
}
```

规则：

- 用 `CanvasGroup.alpha` 控制可见性。
- 用 `interactable` 控制 UI 是否能点。
- 用 `blocksRaycasts` 控制是否拦截点击。
- 有动画时，先关输入，再淡出，最后 `SetActive(false)`。
- 暂停期间还要播放 UI 动画时，用 `Time.unscaledDeltaTime` 和 `WaitForSecondsRealtime`。

## Panel 类型

### Persistent HUD

HUD 只在 gameplay 和少量结算转场可见。

包含：

- Health UI
- Cash / Debt
- Timer
- XP bar
- Active Item HUD
- Boss Health Bar

当文字长度会变化时，不要硬摆位置。使用 HUD layout controller 读取 TMP preferred width，动态调整底框宽度和图标间距。

### Modal Panel

Level Up、Settings、Quit Confirm、HUD Guide、Run Summary 都属于 Modal。

要求：

- `blocksRaycasts = true`
- 打开时锁 gameplay 输入或 `Time.timeScale = 0f`
- 关闭时明确恢复到原状态
- 设置默认选中按钮，支持键盘/手柄
- 用 `selectionLocked` 防止双击或重复提交

### Transition Overlay

Round Intro、Round Clear、Times Up、Game Over Transition 放到一个 `RoundPresentationController`。

它只负责表现：

- 持有 overlay 的 `CanvasGroup` 和 TMP 引用
- 必要时自动创建 fallback overlay
- 提供 `Play...` / `Stop...` API
- 用 callback 通知完成
- 不直接改长期游戏状态

### Outcome Panel

如果失败原因的美术和文案不同，使用两个独立 Panel。Debt Runner 里怪物击杀和债务失败分别是：

- `Panel_Death_Monster`
- `Panel_Death_Debt`

这比一个通用死亡面板里塞大量条件分支更可靠，也更适合美术单独调。

每个 Outcome Panel 持有：

- Title text
- Reason / Subtitle text
- Description text
- 可替换背景 Sprite
- Restart button
- Main Menu button

## Inspector 设计

Inspector 的目标用户是调游戏的人，不是写代码的人。

字段顺序：

1. References
2. UI Binding
3. Text / Content
4. Visual Style
5. Timing / Animation
6. Economy / Combat Tuning
7. Audio
8. Debug，只在 editor/development build 使用

示例：

```csharp
[Header("UI Binding / UI绑定")]
[SerializeField] private TMP_Text textCash;

[Header("Timing")]
[SerializeField, Min(0f)] private float fadeInSeconds = 0.15f;

[SerializeField, Range(0f, 1f)] private float backdropAlpha = 0.72f;

[SerializeField, TextArea(2, 4)] private string description;
```

数据字段默认使用 private serialized field，加只读属性：

```csharp
[SerializeField, Min(0)] private int price = 120;
public int Price => Mathf.Max(0, price);
```

不要为了 Editor Template 绑定把字段改 public。用 `SerializedObject` 绑定 private serialized field。

## Component 分工

按职责拆脚本：

- `GameFlowController`：游戏状态和大流程。
- `RoundPresentationController`：全屏转场 overlay。
- `ShopSystem`：商店商品、刷新、购买、UI 刷新。
- `SpinningWheelController`：转盘结果和概率。
- `SpinningWheelAnimator`：旋转动画和 tick/stop 音效。
- `LevelUpPanel`：升级三选一的显示和选择生命周期。
- `UpgradeCard`：单张升级卡渲染。
- `RunSummaryPanel`：结束统计。
- `SettingsMenuController`：音量、分辨率、全屏、PlayerPrefs。
- `GameplayHudLayoutController`：HUD 自适应排版。
- `FirstRunHudGuideController`：首次 HUD 教程和 callout。

Panel 脚本不要修改无关玩法状态。它可以调用 flow/controller。

## 数据层

商品、升级、权重和效果使用 ScriptableObject：

```csharp
[CreateAssetMenu(menuName = "DebtRunner/Shop/Shop Item Definition", fileName = "ShopItem_")]
public class ShopItemDefinition : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private string itemTitle;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;

    [Header("Pricing & Drop")]
    [SerializeField, Min(0)] private int price;
    [SerializeField] private UpgradeRarity rarity;
    [SerializeField, Min(0f)] private float weightPercent;

    [Header("Effects")]
    [SerializeField] private List<ShopItemEffect> effects;
}
```

用 pool asset 管理 weighted list。概率计算放 utility/controller，不放 UI card。

常用 enum：

- `UpgradeRarity`
- `WeaponUpgradeEffectType`
- `ShopItemEffectType`
- `WeaponModeId`
- `ActiveItemId`
- `PrizeOutcomeType`

资产不要默认可信。用 clamped getter、null check 和日志保护运行时。

## Editor Template 模式

复杂 Panel 必须提供 Editor 菜单生成器，避免重复手搭。

模板结构：

```csharp
[MenuItem("GameObject/DebtRunner/Create Shop Panel Template")]
public static void CreateShopPanelTemplate()
{
    Canvas canvas = Object.FindObjectOfType<Canvas>();
    if (canvas == null)
    {
        EditorUtility.DisplayDialog("Error", "No Canvas found. Create a Canvas first.", "OK");
        return;
    }

    Transform existing = canvas.transform.Find("Panel_Shop");
    if (existing != null)
    {
        Selection.activeGameObject = existing.gameObject;
        return;
    }

    // Create root, children, runtime components, then bind serialized refs.
}
```

模板职责：

- 查找场景 Canvas。
- 检查同名 Panel，防止重复创建。
- 创建 full-screen `RectTransform` root。
- root 上添加 `CanvasGroup`。
- 创建稳定命名的 children。
- 添加 runtime component。
- 用 `SerializedObject` 绑定 private serialized refs。
- `Undo.RegisterCreatedObjectUndo` 支持撤销。
- `Selection.activeGameObject` 和 `EditorGUIUtility.PingObject` 方便继续编辑。
- 弹窗只提示下一步必要操作。

## UI Authoring 规则

默认遵守：

- 全屏 Panel root 放 Canvas 下，并 stretch 到父级。
- 内容放 `Content` 子节点。
- 装饰图 `raycastTarget = false`。
- Modal 背景或点击 blocker 才 `raycastTarget = true`。
- 所有文本用 TextMeshPro。
- 按钮命名使用动作：`Btn_Start`、`Btn_NextRound`、`Btn_Restart`。
- 默认文案放 Inspector，不只写在 hierarchy 里。
- Modal / Outcome 显示时调用 `transform.SetAsLastSibling()`。
- 键盘/手柄使用 explicit navigation。
- Hover / Click SFX 统一绑定或由专门 emitter 组件负责。

## 输入、暂停和鼠标

Gameplay 输入和 UI 输入必须分离：

- 只有 `GameState.Gameplay` 且不处于 round intro、round clear、modal、pause 时，玩法输入才有效。
- Modal/Menu 打开时，键盘/手柄导航仍然有效。
- 鼠标显示与 pointer raycaster 可以根据状态启用/禁用。

暂停：

```csharp
Time.timeScale = 0f;
panelPauseMenu.SetActive(true);
```

恢复：

```csharp
panelPauseMenu.SetActive(false);
Time.timeScale = 1f;
```

只在当前状态允许互动时恢复时间。不要在 GameOver/Victory/StoryIntro 中无条件 `Time.timeScale = 1f`。

## 音频 Flow

BGM 由状态驱动：

- Title：主菜单音乐
- Gameplay：战斗音乐
- Shop：商店音乐
- Boss Round：Boss 音乐
- Level Up：升级音乐或 sting
- GameOver：失败音乐
- Victory：胜利音乐

SFX 可以在交互本地触发，但必须经过统一 manager，确保音量设置生效。

用 `PlayerPrefs` 保存：

- Master volume
- BGM volume
- SFX volume
- Resolution
- Fullscreen
- Mute state

## 必须防守的失败模式

实现后检查：

- 场景没有 Canvas 时，Editor Template 给明确弹窗，不抛 null exception。
- 缺少可选 Panel 时只 log 一次，不每帧刷屏。
- Game Over 不能被伤害和债务失败重复触发。
- Level Up 不能双击选两张卡。
- 离开 pause、level-up、game-over、victory、settings 后时间恢复正确。
- Shop 不能卖空商品，不能扣负数价格。
- 回合结束等待 Level Up 只在设计要求时发生。
- Boss Victory 会清掉 pending level-up。
- 怪物死亡和债务失败进入不同结果 UI。
- HUD 文本变长不会压住图标或计时器。
- 生成的 Panel 默认 inactive，除非是 authoring preview。

## 制作流程

按这个顺序做新游戏：

1. 定义 `GameState` 和完整 UI flow。
2. 建场景 root、World、Canvas 和 Managers。
3. 写 `GameFlowController`，先接空 Panel refs。
4. 做 Title、HUD、Pause、GameOver 的最小可用版本。
5. 给每个 Panel 加 `CanvasGroup` 生命周期。
6. 接入回合：战斗计时、结算、商店、下一回合。
7. 把商品、升级、权重、效果移到 ScriptableObject。
8. 为复杂 Panel 写 Editor Template。
9. 加键盘/手柄导航和默认选中。
10. 接 BGM/SFX 和 Settings 持久化。
11. 加 tutorial overlay 和 authoring context menu。
12. 手动跑完每条状态转移。

## 验证清单

Unity 中至少手测：

- 首次进入 Title，玩法系统未激活。
- Start Run 正确进入 story intro 或 gameplay。
- Round Intro 暂停玩法，结束后恢复。
- HUD 正确刷新现金、债务、XP、血量、计时器。
- Level Up 打开时锁玩法，只能选择一次，关闭后恢复。
- Timer 结束进入 Settlement 或 Debt Failure。
- Settlement 付款后进入 Shop。
- Shop refresh/buy/draw 后现金和 UI 无旧值。
- Next Round 清理 transient objects 并开始下一回合。
- Pause 在 gameplay/shop 都能打开，Settings 返回正确父菜单。
- 怪物死亡显示 `Panel_Death_Monster`。
- 债务失败显示 `Panel_Death_Debt`。
- Restart 从失败状态开始干净新 run。
- Back To Menu 清理世界对象、Panel、Coroutine、TimeScale。
- Boss Victory 不被失败流程和 pending level-up 干扰。
- Victory presentation 能走到最终总结。
- 每个 Modal 都能用键盘/手柄操作。

## 什么时候重写

如果已有项目满足以下条件，可以 patch：

- 已经有一个大致正确的 flow controller。
- Panel ownership 清楚。
- 大部分状态转移只有小 bug。

如果出现这些情况，直接重写 UI 框架：

- 多个脚本各自判断当前游戏状态。
- Panel 直接改无关玩法系统。
- `Time.timeScale` 散落在很多 UI 脚本里。
- 每帧用 `Find` 查 Panel refs。
- Outcome screen 靠大量条件分支拼出来。
- 策划数据硬编码在玩法逻辑里。

重写方向固定为：一个 flow controller、小 Panel component、ScriptableObject 数据、Editor Template 生成层级、显式状态转移测试。
