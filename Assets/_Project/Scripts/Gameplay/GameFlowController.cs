using System.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    public enum GameState { Title, Gameplay, Settlement, Shop, GameOver, Victory }
    public enum DeathType { KilledByMonster, FailedDebt }
    public bool IsInGameplayState => state == GameState.Gameplay;

    [Header("Panels / 面板")]
    [LocalizedLabel("Panel Title / 标题面板")]
    [SerializeField] private GameObject panelTitle;
    [LocalizedLabel("Panel HUD / HUD面板")]
    [SerializeField] private GameObject panelHUD;
    [LocalizedLabel("Panel Level Up / 升级面板")]
    [SerializeField] private GameObject panelLevelUp;
    [LocalizedLabel("Panel Settlement / 结算面板")]
    [SerializeField] private GameObject panelSettlement;
    [LocalizedLabel("Panel Shop / 商店面板")]
    [SerializeField] private GameObject panelShop;
    [LocalizedLabel("Panel Death Monster / 怪物击杀死亡面板")]
    [SerializeField] private DeathPanel monsterDeathPanel;
    [LocalizedLabel("Panel Death Debt / 债务失败死亡面板")]
    [SerializeField] private DeathPanel debtFailurePanel;
    [LocalizedLabel("Panel Victory / 胜利面板")]
    [SerializeField] private VictoryPanel victoryPanel;
    [LocalizedLabel("Panel Pause Menu / 暂停面板")]
    [SerializeField] private GameObject panelPauseMenu;
    [LocalizedLabel("Panel Settings Placeholder / 设置占位面板")]
    [SerializeField] private GameObject panelSettingsPlaceholder;
    [LocalizedLabel("Pause Settings Menu / 暂停设置菜单")]
    [SerializeField] private SettingsMenuController pauseSettingsMenu;

    [Header("World Roots / 场景根节点")]
    [LocalizedLabel("Enemies Root / 敌人根节点")]
    [SerializeField] private Transform enemiesRoot;
    [LocalizedLabel("Projectiles Root / 子弹根节点")]
    [SerializeField] private Transform projectilesRoot;
    [LocalizedLabel("Pickups Root / 掉落根节点")]
    [SerializeField] private Transform pickupsRoot;

    [Header("World References / 世界引用")]
    [LocalizedLabel("Player Motor / 玩家移动组件")]
    [SerializeField] private PlayerMotor2D playerMotor;
    [LocalizedLabel("Camera Follow / 相机跟随组件")]
    [SerializeField] private CameraFollow2D cameraFollow;

    [Header("HUD Text / HUD文案")]
    [LocalizedLabel("Text Round / 回合文本")]
    [SerializeField] private TMP_Text textRound;
    [LocalizedLabel("Text Cash / 金钱文本")]
    [SerializeField] private TMP_Text textCash;
    [LocalizedLabel("Text Debt / 债务文本")]
    [SerializeField] private TMP_Text textDebt;
    [LocalizedLabel("Text Countdown / 倒计时文本")]
    [SerializeField] private TMP_Text textCountdown;
    [LocalizedLabel("Round Intro Seconds / 开场停留时长")]
    [SerializeField] private float roundIntroSeconds = 2.5f;

    [Header("Round Intro Overlay / 回合开场遮罩")]
    [LocalizedLabel("Round Intro Overlay / 开场遮罩")]
    [SerializeField] private CanvasGroup roundIntroOverlay;
    [LocalizedLabel("Round Intro Round Text / 开场回合文本")]
    [SerializeField] private TMP_Text roundIntroRoundText;
    [LocalizedLabel("Round Intro Debt Text / 开场债务文本")]
    [SerializeField] private TMP_Text roundIntroDebtText;
    [LocalizedLabel("Round Intro Continue Hint Text / 开场继续提示文本")]
    [SerializeField] private TMP_Text roundIntroContinueHintText;
    [LocalizedLabel("Round Intro Require Any Key / 开场需任意键继续")]
    [SerializeField] private bool roundIntroRequireAnyKeyToContinue = true;
    [LocalizedLabel("Round Intro Hint Message / 开场提示文案")]
    [SerializeField] private string roundIntroContinueHintMessage = "Press Any Key to Continue";
    [LocalizedLabel("Round Intro Fade In Seconds / 开场淡入时长")]
    [SerializeField] private float roundIntroFadeInSeconds = 0.15f;
    [LocalizedLabel("Round Intro Fade Out Seconds / 开场淡出时长")]
    [SerializeField] private float roundIntroFadeOutSeconds = 0.30f;

    [Header("Round Clear Overlay / 回合通过遮罩")]
    [LocalizedLabel("Round Clear Overlay / 通关遮罩")]
    [SerializeField] private CanvasGroup roundClearOverlay;
    [LocalizedLabel("Round Clear Title Text / 通关标题文本")]
    [SerializeField] private TMP_Text roundClearTitleText;
    [LocalizedLabel("Round Clear Sub Text / 通关副标题文本")]
    [SerializeField] private TMP_Text roundClearSubText;
    [LocalizedLabel("Show Round Clear Transition / 显示通关过渡")]
    [SerializeField] private bool showRoundClearTransition = true;
    [LocalizedLabel("Round Clear Title Message / 通关标题文案")]
    [SerializeField] private string roundClearTitleMessage = "YOU PASS!";
    [LocalizedLabel("Round Clear Sub Message / 通关副文案")]
    [SerializeField] private string roundClearSubMessage = "Round Cleared";
    [LocalizedLabel("Round Clear Seconds / 通关停留时长")]
    [SerializeField, Min(0f)] private float roundClearSeconds = 1.2f;
    [LocalizedLabel("Round Clear Fade In Seconds / 通关淡入时长")]
    [SerializeField, Min(0f)] private float roundClearFadeInSeconds = 0.12f;
    [LocalizedLabel("Round Clear Fade Out Seconds / 通关淡出时长")]
    [SerializeField, Min(0f)] private float roundClearFadeOutSeconds = 0.18f;
    [LocalizedLabel("Auto Collect Drops On Round Clear / 通关自动吸取掉落")]
    [SerializeField] private bool autoCollectDropsOnRoundClear = true;
    [LocalizedLabel("Round Clear Auto Collect Radius / 自动吸取半径")]
    [SerializeField, Min(0f)] private float roundClearAutoCollectRadius = 4.5f;
    [LocalizedLabel("Round Clear Auto Collect Move Speed / 自动吸取移动速度")]
    [SerializeField, Min(0f)] private float roundClearAutoCollectMoveSpeed = 15f;
    [LocalizedLabel("Round Clear Auto Collect Distance / 自动吸取判定距离")]
    [SerializeField, Min(0f)] private float roundClearAutoCollectCollectDistance = 0.2f;
    [LocalizedLabel("Round Clear Overlay Alpha During Collect / 吸取时遮罩透明度")]
    [SerializeField, Range(0f, 1f)] private float roundClearOverlayAlphaDuringCollect = 0.45f;
    [LocalizedLabel("Round Clear Collect Delay Seconds / 吸取完成延迟")]
    [SerializeField, Min(0f)] private float roundClearCollectDelaySeconds = 0.12f;
    [LocalizedLabel("Round Clear Auto Collect Max Wait Seconds / 自动吸取最大等待")]
    [SerializeField, Min(0f)] private float roundClearAutoCollectMaxWaitSeconds = 15f;

    [Header("HUD Health / 血量UI")]
    [LocalizedLabel("Health UI / 血量界面")]
    [SerializeField] private HealthUI healthUI;

    [Header("HUD XP / 经验UI")]
    [LocalizedLabel("XP UI / 经验界面")]
    [SerializeField] private XPUI xpUI;

    [Header("Level Up Rewards / 升级奖励")]
    [LocalizedLabel("Level Up Panel / 升级面板脚本")]
    [SerializeField] private LevelUpPanel levelUpPanel;
    [LocalizedLabel("Post Level Up Safety Invuln Seconds / 升级后无敌时长")]
    [SerializeField, Min(0f)] private float postLevelUpSafetyInvulnSeconds = 0.75f;
    [LocalizedLabel("Clear Enemy Projectiles After Level Up / 升级后清敌方子弹")]
    [SerializeField] private bool clearEnemyProjectilesAfterLevelUp = true;
    [LocalizedLabel("Defer Level Up Rewards During Round Clear / 通关阶段延后升级奖励")]
    [SerializeField] private bool deferLevelUpRewardsDuringRoundClear = true;

    [Header("Settlement Text / 结算文案")]
    [LocalizedLabel("Text Due / 应付文本")]
    [SerializeField] private TMP_Text textDue;
    [LocalizedLabel("Text Paid / 已付文本")]
    [SerializeField] private TMP_Text textPaid;
    [LocalizedLabel("Text Remaining Debt / 剩余债务文本")]
    [SerializeField] private TMP_Text textRemainingDebt;

    [Header("Run Config / 跑局配置")]
    [LocalizedLabel("Total Rounds / 普通总回合数")]
    [SerializeField] private int totalRounds = 10;
    [LocalizedLabel("Base Due / 基础债务")]
    [SerializeField] private int baseDue = 500;
    [LocalizedLabel("Step Due / 每回合债务增量")]
    [SerializeField] private int stepDue = 200;
    [LocalizedLabel("Round Duration Seconds / 每回合时长")]
    [SerializeField] private float roundDurationSeconds = 30f;
    [LocalizedLabel("Base XP To Next / 基础升级经验")]
    [SerializeField, Min(1)] private int baseXpToNext = 10;

    [Header("Debt Curve / 债务曲线")]
    [LocalizedLabel("Use Debt Curve Multiplier / 启用债务曲线倍率")]
    [SerializeField] private bool useDebtCurveMultiplier = true;
    [LocalizedLabel("Debt Curve / 债务曲线")]
    [SerializeField] private AnimationCurve debtCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 2f);
    [LocalizedLabel("Debt Min Growth Per Round / 债务最小每回合增长")]
    [SerializeField, Min(0.001f)] private float debtMinGrowthPerRound = 0.08f;

    [Header("Enemy Difficulty Curve / 敌人难度曲线")]
    [LocalizedLabel("Enemy HP Curve / 敌人血量曲线")]
    [SerializeField] private AnimationCurve enemyHpCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 2.2f);
    [LocalizedLabel("Enemy Speed Curve / 敌人速度曲线")]
    [SerializeField] private AnimationCurve enemySpeedCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1.7f);
    [LocalizedLabel("Enemy HP Min Growth Per Round / 敌人血量最小每回合增长")]
    [SerializeField, Min(0.01f)] private float enemyHpMinGrowthPerRound = 0.12f;
    [LocalizedLabel("Enemy Speed Min Growth Per Round / 敌人速度最小每回合增长")]
    [SerializeField, Min(0.01f)] private float enemySpeedMinGrowthPerRound = 0.05f;

    [Header("XP / 经验配置")]
    [LocalizedLabel("Level / 当前等级(初始)")]
    [SerializeField] private int level = 1;
    [LocalizedLabel("XP / 当前经验(初始)")]
    [SerializeField] private int xp = 0;
    [LocalizedLabel("XP To Next / 下级经验(初始)")]
    [SerializeField] private int xpToNext = 10;
    [LocalizedLabel("XP Curve Max Level / 经验曲线最大等级")]
    [SerializeField, Min(2)] private int xpCurveMaxLevel = 25;
    [LocalizedLabel("XP To Next Curve / 升级经验曲线")]
    [SerializeField] private AnimationCurve xpToNextCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 5f);
    [LocalizedLabel("XP Min Growth Per Level / 升级经验最小每级增长")]
    [SerializeField, Min(0.001f)] private float xpMinGrowthPerLevel = 0.12f;

    [Header("Weapon Upgrade Pool / 武器升级池")]
    [LocalizedLabel("Weapon Upgrade Pool Asset / 武器升级池资源")]
    [SerializeField] private WeaponUpgradePoolAsset weaponUpgradePoolAsset;

    [Header("Gameplay Systems / 核心系统")]
    [LocalizedLabel("Enemy Spawner / 刷怪器")]
    [SerializeField] private EnemySpawner enemySpawner;
    [LocalizedLabel("Player Shooter / 玩家射击")]
    [SerializeField] private PlayerShooter playerShooter;
    [LocalizedLabel("Shop System / 商店系统")]
    [SerializeField] private ShopSystem shopSystem;

    [Header("Fail Animation / 失败动画")]
    [LocalizedLabel("Fail Animator / 失败动画器")]
    [SerializeField] private Animator failAnimator;
    [LocalizedLabel("Fail Trigger Name / 失败触发器名")]
    [SerializeField] private string failTriggerName = "Fail";

    [Header("Debug / 调试")]
    [LocalizedLabel("Enable Debug Hotkeys / 启用调试热键")]
    [SerializeField] private bool enableDebugHotkeys = true;
    [LocalizedLabel("Debug Jump Boss Round Key / 跳Boss热键")]
    [SerializeField] private KeyCode debugJumpBossRoundKey = KeyCode.F6;
    [LocalizedLabel("Debug Reset Stats Before Boss / 跳Boss前重置属性")]
    [SerializeField] private bool debugResetStatsBeforeBoss = false;

    private enum SettingsReturnTarget
    {
        None,
        PauseMenu,
        TitleMenu
    }

    private GameState state;
    private int roundIndex;          // 1-based
    private int cash;
    private Coroutine roundTimerCo;
    private Coroutine roundIntroCo;
    private Coroutine roundClearCo;
    private bool roundIntroOverlayAutoCreated;
    private bool roundClearOverlayAutoCreated;
    private bool roundIntroActive;
    private bool roundClearActive;
    private bool pauseMenuOpen;
    private SettingsReturnTarget settingsReturnTarget = SettingsReturnTarget.None;
    private float roundTimeRemaining;  // 当前回合剩余时间
    private readonly RunProgressionState runProgression = new RunProgressionState();
    private int pendingDeferredLevelUpChoices;
    private DeathType currentDeathType = DeathType.KilledByMonster;

    // 商店道具加成（每局重置）
    private int bonusXPPerKill;
    private float bonusXPMagnetRadius;
    private float cashBonusPercent;

    public int BonusXPPerKill => bonusXPPerKill;
    public float BonusXPMagnetRadius => bonusXPMagnetRadius;

    private void Awake()
    {
        // 单例
        if (Instance != null && Instance != this)
        {
            bool oldInDontDestroy = Instance.gameObject.scene.name == "DontDestroyOnLoad";
            bool newInDontDestroy = gameObject.scene.name == "DontDestroyOnLoad";

            if (oldInDontDestroy && !newInDontDestroy)
            {
                RunLogger.Warning("Replacing stale GameFlowController from DontDestroyOnLoad with scene instance.");
                Destroy(Instance.gameObject);
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            Instance = this;
        }

        if (panelSettingsPlaceholder != null)
            panelSettingsPlaceholder.SetActive(false);

        if (enemySpawner == null)
            enemySpawner = FindObjectOfType<EnemySpawner>();
        if (playerShooter == null)
            playerShooter = FindObjectOfType<PlayerShooter>();
        EnsureShopSystemBound();
        EnsurePauseSettingsBound();
        EnsureDeathPanelsBound();
        DisableLegacyGameOverPanelIfPresent();

        // 尝试自动绑定两套死亡面板（怪物击杀 / 债务失败）

        if (victoryPanel == null)
        {
            GameObject found = GameObject.Find("Panel_Victory");
            if (found != null)
                victoryPanel = found.GetComponent<VictoryPanel>();
            else
            {
                VictoryPanel[] all = Resources.FindObjectsOfTypeAll<VictoryPanel>();
                for (int i = 0; i < all.Length; i++)
                {
                    VictoryPanel vp = all[i];
                    if (vp == null) continue;
                    if (vp.gameObject.scene.IsValid() && vp.gameObject.scene.isLoaded)
                    {
                        victoryPanel = vp;
                        break;
                    }
                }
            }

            if (victoryPanel != null)
                RunLogger.Event("VictoryPanel auto-bound to GameFlowController.");
            else
                RunLogger.Warning("VictoryPanel not assigned and not found in scene. Assign Panel_Victory in GameFlowController Inspector.");
        }

        // 初始化升级池
        EnsureWeaponUpgradePool();

        // 初始数据
        baseXpToNext = Mathf.Max(1, baseXpToNext);
        level = Mathf.Max(1, level);
        roundIndex = 1;
        cash = 0;
        xp = 0;
        xpToNext = CalculateXpToNext(level);
        runProgression.Reset();
        runProgression.BeginRound();

        RunLogger.Event($"GameFlow ready: rounds={totalRounds}, round1Due={CalcDue(1)}, dueStep={stepDue}, roundDuration={roundDurationSeconds:F1}s");

        // 初始界面：只显示Title
        SwitchState(GameState.Title);
        ForceClosePauseMenu(false);
        RefreshHUD();
    }

    private void EnsureWeaponUpgradePool()
    {
        if (weaponUpgradePoolAsset != null && weaponUpgradePoolAsset.Entries != null && weaponUpgradePoolAsset.Entries.Count > 0)
            return;

        RunLogger.Warning("Weapon upgrade pool asset is missing or empty.");
    }

    private void EnsureDeathPanelsBound()
    {
        if (monsterDeathPanel == null)
            monsterDeathPanel = FindDeathPanelByName("Panel_Death_Monster");
        if (monsterDeathPanel == null)
            monsterDeathPanel = FindDeathPanelByName("Panel_Death");

        if (debtFailurePanel == null)
            debtFailurePanel = FindDeathPanelByName("Panel_Death_Debt");
        if (debtFailurePanel == null)
            debtFailurePanel = FindDeathPanelByName("Panel_Death");

        if (monsterDeathPanel != null)
            RunLogger.Event("Monster death panel auto-bound.");
        else
            RunLogger.Warning("Monster death panel missing. Assign Panel_Death_Monster in GameFlowController.");

        if (debtFailurePanel != null)
            RunLogger.Event("Debt failure panel auto-bound.");
        else
            RunLogger.Warning("Debt failure panel missing. Assign Panel_Death_Debt in GameFlowController.");
    }

    private DeathPanel FindDeathPanelByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        GameObject found = GameObject.Find(objectName);
        if (found != null)
            return found.GetComponent<DeathPanel>();

        DeathPanel[] all = Resources.FindObjectsOfTypeAll<DeathPanel>();
        for (int i = 0; i < all.Length; i++)
        {
            DeathPanel panel = all[i];
            if (panel == null)
                continue;
            if (!panel.gameObject.scene.IsValid() || !panel.gameObject.scene.isLoaded)
                continue;
            if (panel.gameObject.name == objectName)
                return panel;
        }

        return null;
    }

    private void DisableLegacyGameOverPanelIfPresent()
    {
        GameObject legacyPanel = GameObject.Find("Panel_GameOver");
        if (legacyPanel == null)
            return;

        if (legacyPanel.activeSelf)
            legacyPanel.SetActive(false);
    }

    private void Update()
    {
        // 快速测试热键
        if (Input.GetKeyDown(KeyCode.F1)) SwitchState(GameState.Title);
        if (Input.GetKeyDown(KeyCode.F2)) StartRun();
        if (Input.GetKeyDown(KeyCode.F3)) EndRound();
        if (Input.GetKeyDown(KeyCode.F4)) EnterShop();
        if (Input.GetKeyDown(KeyCode.F5)) SwitchState(GameState.GameOver);
        if (enableDebugHotkeys && Input.GetKeyDown(debugJumpBossRoundKey)) DebugJumpToBossRound(debugResetStatsBeforeBoss);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (panelSettingsPlaceholder != null && panelSettingsPlaceholder.activeSelf)
            {
                BackFromPauseSettings();
            }
            else if (pauseMenuOpen)
            {
                ResumeFromPauseMenu();
            }
            else if (CanOpenPauseMenu())
            {
                OpenPauseMenu();
            }
        }
        
        // 更新倒计时显示
        if (state == GameState.Gameplay && textCountdown != null)
        {
            if (IsCurrentRoundBoss())
            {
                textCountdown.text = "Boss: No Time Limit";
            }
            else
            {
                int seconds = Mathf.Max(0, Mathf.CeilToInt(roundTimeRemaining));
                textCountdown.text = $"Time: {seconds}s";
            }
        }
    }

    // ====== Public APIs (给其他系统调用) ======

    // 敌人死亡自动加钱会调用这个
    public void AddCash(int amount)
    {
        int v = Mathf.Max(0, amount);
        if (v == 0) return;

        // 追债回扣：按百分比加成
        if (cashBonusPercent > 0f)
            v = Mathf.Max(v, Mathf.RoundToInt(v * (1f + cashBonusPercent / 100f)));

        cash += v;
        RunLogger.Event($"Cash +{v}, total={cash}");
        RefreshHUD();
        if (shopSystem != null) shopSystem.RefreshShopUI();
    }

    // 捡到XP会调用这个
    public void AddXP(int amount)
    {
        int v = Mathf.Max(0, amount);
        if (v == 0) return;

        xp += v;
        RunLogger.Event($"XP +{v}, current={xp}/{xpToNext}, level={level}");
        
        // 检查是否升级
        while (xp >= xpToNext)
        {
            xp -= xpToNext;
            LevelUp();
            
            // 升级后立即更新UI显示
            if (xpUI != null)
                xpUI.UpdateXPDisplay();
        }

        // 更新XP UI
        if (xpUI != null)
            xpUI.UpdateXPDisplay();
    }

private void LevelUp()
{
    level += 1;
    xpToNext = CalculateXpToNext(level);

    RunLogger.Event($"Level up -> {level}, nextXP={xpToNext}");

    if (ShouldDeferLevelUpRewardPresentation())
    {
        pendingDeferredLevelUpChoices += 1;
        RunLogger.Event($"Level-up reward deferred until next round. pending={pendingDeferredLevelUpChoices}");
        return;
    }

    if (!TryShowLevelUpRewardPanelNow())
    {
        pendingDeferredLevelUpChoices += 1;
        RunLogger.Event($"Level-up reward queued. pending={pendingDeferredLevelUpChoices}");
    }
}

private bool ShouldDeferLevelUpRewardPresentation()
{
    return deferLevelUpRewardsDuringRoundClear && roundClearActive;
}

private bool TryShowLevelUpRewardPanelNow()
{
    if (levelUpPanel == null)
        return false;

    if (panelLevelUp != null && panelLevelUp.activeSelf)
        return false;

    WeaponUpgrade[] selectedUpgrades = SelectRandomUpgrades(3);
    if (selectedUpgrades.Length < 3)
    {
        RunLogger.Error("Level up skipped: weapon upgrade pool has no valid entries.");
        return false;
    }

    levelUpPanel.ShowUpgradePanel(selectedUpgrades, OnUpgradeSelected);

    // 只有面板存在且成功走到这里才暂停游戏
    Time.timeScale = 0f;
    return true;
}

private void TryShowDeferredLevelUpRewardIfReady()
{
    if (pendingDeferredLevelUpChoices <= 0)
        return;

    if (state != GameState.Gameplay || roundIntroActive || roundClearActive)
        return;

    if (panelLevelUp != null && panelLevelUp.activeSelf)
        return;

    if (!TryShowLevelUpRewardPanelNow())
        return;

    pendingDeferredLevelUpChoices = Mathf.Max(0, pendingDeferredLevelUpChoices - 1);
    RunLogger.Event($"Deferred level-up reward presented. remaining={pendingDeferredLevelUpChoices}");
}


    /// <summary>
    /// 随机选择升级选项
    /// </summary>
    private WeaponUpgrade[] SelectRandomUpgrades(int count)
    {
        if (count <= 0) return new WeaponUpgrade[0];

        EnsureWeaponUpgradePool();
        if (weaponUpgradePoolAsset == null || weaponUpgradePoolAsset.Entries == null || weaponUpgradePoolAsset.Entries.Count == 0)
            return SelectFallbackRandomUpgrades(count);

        System.Collections.Generic.List<WeaponUpgradeDefinition> pickedDefinitions =
            WeightedPickerUtility.PickUnique(weaponUpgradePoolAsset.Entries, count, weaponUpgradePoolAsset.GetEffectiveWeight);

        while (pickedDefinitions.Count < count)
        {
            System.Collections.Generic.List<WeaponUpgradeDefinition> oneMore =
                WeightedPickerUtility.PickUnique(weaponUpgradePoolAsset.Entries, 1, weaponUpgradePoolAsset.GetEffectiveWeight);
            if (oneMore.Count == 0 || oneMore[0] == null)
                break;
            pickedDefinitions.Add(oneMore[0]);
        }

        if (pickedDefinitions.Count < count)
            return new WeaponUpgrade[0];

        WeaponUpgrade[] selected = new WeaponUpgrade[count];
        for (int i = 0; i < count; i++)
            selected[i] = pickedDefinitions[i] != null ? pickedDefinitions[i].CreateRuntimeUpgrade() : null;

        return selected;
    }

    private WeaponUpgrade[] SelectFallbackRandomUpgrades(int count)
    {
        System.Collections.Generic.List<WeaponUpgrade> fallbackPool = CreateDefaultFallbackWeaponUpgrades();
        if (fallbackPool.Count == 0)
            return new WeaponUpgrade[0];

        WeaponUpgrade[] selected = new WeaponUpgrade[count];
        System.Collections.Generic.List<int> indices = new System.Collections.Generic.List<int>();
        for (int i = 0; i < fallbackPool.Count; i++)
            indices.Add(i);

        for (int i = 0; i < count; i++)
        {
            WeaponUpgrade picked;
            if (indices.Count > 0)
            {
                int randomIdx = Random.Range(0, indices.Count);
                picked = fallbackPool[indices[randomIdx]];
                indices.RemoveAt(randomIdx);
            }
            else
            {
                picked = fallbackPool[Random.Range(0, fallbackPool.Count)];
            }

            selected[i] = picked;
        }

        return selected;
    }

    /// <summary>
    /// 玩家选择了升级
    /// </summary>
    private void OnUpgradeSelected(WeaponUpgrade upgrade)
    {
        if (upgrade != null && playerShooter != null)
            playerShooter.ApplyUpgrade(upgrade);
        else if (upgrade == null)
            RunLogger.Warning("Upgrade selected callback received null upgrade.");

        if (clearEnemyProjectilesAfterLevelUp)
            ClearEnemyProjectiles();

        // 恢复游戏时间
        Time.timeScale = 1f;

        if (postLevelUpSafetyInvulnSeconds > 0f)
        {
            PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.GrantTemporaryInvulnerability(postLevelUpSafetyInvulnSeconds);
        }

        string upgradeTitle = upgrade != null ? upgrade.title : "<null>";
        RunLogger.Event($"Upgrade selected: {upgradeTitle}");

        TryShowDeferredLevelUpRewardIfReady();
    }

    /// <summary>获取当前等级</summary>
    public int GetLevel() => level;

    /// <summary>获取当前XP</summary>
    public int GetCurrentXP() => xp;

    /// <summary>获取升级所需XP</summary>
    public int GetXPToNext() => xpToNext;
    public int GetCurrentRound() => roundIndex;
    public int GetTotalRounds() => totalRounds;
    public string GetNextRoundDebtDisplay() => GetDebtDisplay(roundIndex + 1);
    public bool IsBossRoundActive() => IsCurrentRoundBoss();
    public int GetBossRoundNumber() => GetBossRoundIndex();

    // UI Button: Start
    public void StartRun()
    {
        // 恢复游戏时间（以防还在暂停状态）
        StopRoundClearTransition(false);
        Time.timeScale = 1f;

        // 新开局：重置
        roundIndex = 1;
        cash = 0;
        xp = 0;
        level = 1;
        baseXpToNext = Mathf.Max(1, baseXpToNext);
        xpToNext = CalculateXpToNext(level);
        pendingDeferredLevelUpChoices = 0;
        runProgression.Reset();
        runProgression.BeginRound();
        LogCurrentEnemyDifficulty();

        RunLogger.Event($"Run started: rounds={totalRounds}, due={CalcDue(roundIndex)}, level={level}");

        // 重置玩家血量和血量UI
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.ResetRuntimeStats();
            playerHealth.RestoreHealth();
        }

        if (playerMotor != null)
            playerMotor.ResetRuntimeStats();

        if (playerShooter != null)
            playerShooter.ResetRuntimeStats();

        // 重置商店道具加成
        bonusXPPerKill = 0;
        bonusXPMagnetRadius = 0f;
        cashBonusPercent = 0f;

        // 隐藏升级面板并重置武器
        if (levelUpPanel != null)
            levelUpPanel.ForceHideImmediate();
        HideAllDeathPanels();

        SwitchState(GameState.Gameplay);
        ShowRoundIntro();

        if (healthUI != null)
            healthUI.ResetHealthUI();

        if (xpUI != null)
            xpUI.UpdateXPDisplay();

        StartRoundTimer();
        RefreshHUD();
    }

    [ContextMenu("Debug/Jump To Boss Round")]
    private void DebugJumpToBossRoundContextMenu()
    {
        DebugJumpToBossRound(false);
    }

    public void DebugJumpToBossRound(bool resetStats)
    {
        StopRoundIntro();
        StopRoundClearTransition(false);
        StopRoundTimer();
        Time.timeScale = 1f;
        pendingDeferredLevelUpChoices = 0;

        if (levelUpPanel != null)
            levelUpPanel.ForceHideImmediate();
        HideAllDeathPanels();

        if (resetStats)
        {
            cash = 0;
            xp = 0;
            level = 1;
            baseXpToNext = Mathf.Max(1, baseXpToNext);
            xpToNext = CalculateXpToNext(level);
            runProgression.Reset();

            PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.ResetRuntimeStats();
                playerHealth.RestoreHealth();
            }

            if (playerMotor != null)
                playerMotor.ResetRuntimeStats();

            if (playerShooter != null)
                playerShooter.ResetRuntimeStats();

            bonusXPPerKill = 0;
            bonusXPMagnetRadius = 0f;
            cashBonusPercent = 0f;
        }

        roundIndex = GetBossRoundIndex();
        runProgression.BeginRound();
        LogCurrentEnemyDifficulty();

        if (state != GameState.Gameplay)
            SwitchState(GameState.Gameplay);

        ClearWorld();
        ShowRoundIntro();
        StartRoundTimer();
        RefreshHUD();

        RunLogger.Event($"Debug jump to boss round: round={roundIndex}/{totalRounds}, reset={resetStats}");
    }

    // 回合结束入口（未来由倒计时/事件触发）
    public void EndRound()
    {
        EndRound(false);
    }

    private void EndRound(bool triggeredByTimer)
    {
        if (state != GameState.Gameplay || roundClearActive) return;
        Time.timeScale = 1f;
        StopRoundTimer();

        if (triggeredByTimer)
            ClearEnemiesForTimerEnd();

        ClearRoundEndTransientObjects();

        // 注意：已移除“随机加钱”。现金应来自击杀敌人时 AddCash(固定值)。
        RunLogger.Event($"Round {roundIndex} ended. cash={cash}, level={level}, xp={xp}/{xpToNext}, fromTimer={triggeredByTimer}");

        // 检查是否战胜了Boss - 如果是则直接显示胜利界面
        if (IsCurrentRoundBoss())
        {
            RunLogger.Event($"Boss defeated! Showing victory screen.");
            
            if (victoryPanel != null)
            {
                victoryPanel.ShowVictoryPanel(roundIndex, cash, level);
            }
            
            // 暂停游戏
            Time.timeScale = 0f;
            
            SwitchState(GameState.Victory);
            return;
        }

        int due = CalcDue(roundIndex);
        if (cash < due)
        {
            RunLogger.Warning($"Round failed on debt check at round end: cash={cash}, due={due}, fromTimer={triggeredByTimer}");
            ShowGameOverWithDeathPanel(DeathType.FailedDebt);
            return;
        }

        if (showRoundClearTransition)
        {
            ShowRoundClearTransition();
            return;
        }

        EnterSettlementAfterRoundEnd();
    }

    // UI Button: Settlement Continue
    public void ConfirmSettlementAndEnterShop()
    {
        if (state != GameState.Settlement) return;

        Time.timeScale = 1f;

        int due = CalcDue(roundIndex);
        if (cash < due)
        {
            RunLogger.Warning($"Settlement failed: cash={cash}, due={due}");
            ShowGameOverWithDeathPanel(DeathType.FailedDebt);
            return;
        }

        // 支付本轮债务
        cash -= due;
        RunLogger.Event($"Settlement passed: due={due}, paid={due}, cashLeft={cash}");

        EnterShop();
    }

    public void EnterShop()
    {
        Time.timeScale = 1f;
        RunLogger.Event($"Enter shop at round {roundIndex}. cash={cash}, currentDue={CalcDue(roundIndex)}, nextDue={CalcDue(roundIndex + 1)}");
        SwitchState(GameState.Shop);
        EnsureShopSystemBound();
        if (shopSystem != null)
            shopSystem.OpenShop();
        RefreshHUD();
    }

    // UI Button: Next Round
    public void NextRound()
    {
        if (state != GameState.Shop) return;

        if (shopSystem != null && shopSystem.IsPrizeDrawInProgress())
        {
            shopSystem.ShowPrizeInfo("Draw is still spinning.");
            return;
        }

        if (shopSystem != null)
            shopSystem.OnShopClosed();

        Time.timeScale = 1f;

        roundIndex += 1;
        int bossRound = GetBossRoundIndex();
        if (roundIndex > bossRound)
        {
            RunLogger.Event("Already defeated boss. Returning to title.");
            SwitchState(GameState.Title);
            return;
        }

        if (roundIndex == bossRound)
            RunLogger.Event($"Enter boss round: {roundIndex}/{totalRounds}");
        else
            RunLogger.Event($"Next round -> {roundIndex}");

        runProgression.BeginRound();
        LogCurrentEnemyDifficulty();
        SwitchState(GameState.Gameplay);
        ShowRoundIntro();
        StartRoundTimer();
        RefreshHUD();
    }

    // UI Button: Restart（直接开始游戏）
    public void Restart()
    {
        StartRun();
    }

    // 玩家受到致命伤害 - 触发游戏结束（被怪物击杀）
    public void TriggerGameOver()
    {
        TriggerGameOver(DeathType.KilledByMonster);
    }

    // 玩家游戏结束 - 指定死因
    public void TriggerGameOver(DeathType deathType)
    {
        if (state == GameState.GameOver) return; // 已经是GameOver则忽略重复触发

        if (deathType == DeathType.KilledByMonster && state != GameState.Gameplay)
        {
            RunLogger.Warning($"Ignored monster death trigger outside gameplay. state={state}, requested={deathType}");
            return;
        }

        if (state != GameState.Gameplay)
            RunLogger.Warning($"TriggerGameOver called while state={state}; proceeding to show death panel.");

        currentDeathType = deathType;
        RunLogger.Warning($"Game over triggered. round={roundIndex}, cash={cash}, due={CalcDue(roundIndex)}, level={level}, deathType={deathType}");

        PlayFailAnimation();
        StopRoundClearTransition(false);
        StopRoundTimer();

        // 显示对应死因的死亡面板（并暂停世界）
        DeathPanel targetPanel = GetDeathPanelForType(deathType);
        if (targetPanel != null)
            targetPanel.ShowDeathPanel();
        else
            RunLogger.Warning($"No death panel assigned for deathType={deathType}.");

        // 暂停游戏世界，冻结一切动作
        Time.timeScale = 0f;

        SwitchState(GameState.GameOver);
    }

    // 游戏结束 - 显示死亡面板（用于非Gameplay状态）
    private void ShowGameOverWithDeathPanel(DeathType deathType)
    {
        currentDeathType = deathType;
        RunLogger.Warning($"Game over with death panel. round={roundIndex}, cash={cash}, due={CalcDue(roundIndex)}, level={level}, deathType={deathType}");
        
        // 显示对应死因的死亡面板
        DeathPanel targetPanel = GetDeathPanelForType(deathType);
        if (targetPanel != null)
            targetPanel.ShowDeathPanel();
        else
            RunLogger.Warning($"No death panel assigned for deathType={deathType}.");

        Time.timeScale = 0f;
        
        SwitchState(GameState.GameOver);
    }

    private DeathPanel GetDeathPanelForType(DeathType deathType)
    {
        DeathPanel panel = deathType == DeathType.FailedDebt ? debtFailurePanel : monsterDeathPanel;
        if (panel == null)
            RunLogger.Warning($"Death panel is null for deathType={deathType}. Check Inspector assignment.");
        return panel;
    }

    private void HideAllDeathPanels()
    {
        if (monsterDeathPanel != null)
            monsterDeathPanel.HideDeathPanel();
        if (debtFailurePanel != null)
            debtFailurePanel.HideDeathPanel();
    }

    // UI Button: Main Menu
    public void BackToMenu()
    {
        // 确保游戏时间恢复
        StopRoundClearTransition(false);
        Time.timeScale = 1f;
        StopRoundTimer();
        RunLogger.Event("Back to title menu.");
        SwitchState(GameState.Title);
    }

    // UI Button: Quit
    public void QuitGame()
    {
        RunLogger.Event("Quit game requested.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

    public void OpenPauseMenu()
    {
        if (pauseMenuOpen || !CanOpenPauseMenu()) return;

        pauseMenuOpen = true;
        Time.timeScale = 0f;
        SetGameplaySystemsActive(false);
        SetPauseMenuVisible(true);
        RunLogger.Event("Pause menu opened.");
    }

    public void ResumeFromPauseMenu()
    {
        if (!pauseMenuOpen) return;

        ForceClosePauseMenu(true);
        RunLogger.Event("Pause menu closed.");
    }

    public void OpenPauseSettings()
    {
        if (!EnsurePauseSettingsReady())
            return;

        bool openFromPause = pauseMenuOpen || state == GameState.Gameplay || state == GameState.Shop;
        RunLogger.Event($"OpenPauseSettings requested. state={state}, fromPause={openFromPause}, pauseOpen={pauseMenuOpen}");

        if (openFromPause)
        {
            if (!pauseMenuOpen)
                OpenPauseMenu();

            if (!pauseMenuOpen)
                return;

            settingsReturnTarget = SettingsReturnTarget.PauseMenu;
        }
        else
        {
            settingsReturnTarget = SettingsReturnTarget.TitleMenu;
            pauseMenuOpen = false;
            if (panelPauseMenu != null)
                panelPauseMenu.SetActive(false);
        }

        panelSettingsPlaceholder.SetActive(true);

        if (pauseSettingsMenu != null)
            pauseSettingsMenu.ShowMenu();
    }

    public void BackFromPauseSettings()
    {
        if (pauseSettingsMenu != null)
            pauseSettingsMenu.HideMenu();

        if (panelSettingsPlaceholder != null)
            panelSettingsPlaceholder.SetActive(false);

        if (settingsReturnTarget == SettingsReturnTarget.PauseMenu)
        {
            pauseMenuOpen = true;
            if (panelPauseMenu != null)
                panelPauseMenu.SetActive(true);
            Time.timeScale = 0f;
            SetGameplaySystemsActive(false);
        }
        else if (settingsReturnTarget == SettingsReturnTarget.TitleMenu)
        {
            pauseMenuOpen = false;
            if (panelPauseMenu != null)
                panelPauseMenu.SetActive(false);
        }

        settingsReturnTarget = SettingsReturnTarget.None;
    }

    public void QuitFromPauseMenu()
    {
        ForceClosePauseMenu(true);
        QuitGame();
    }

    public int GetCashAmount() => cash;

    public bool TrySpendCash(int amount)
    {
        int v = Mathf.Max(0, amount);
        if (v == 0) return true;
        if (cash < v) return false;

        cash -= v;
        RunLogger.Event($"Cash spent: -{v}, total={cash}");
        RefreshHUD();
        if (shopSystem != null) shopSystem.RefreshShopUI();
        return true;
    }

    public void ApplyShopUpgrade(WeaponUpgrade upgrade)
    {
        if (upgrade == null) return;

        if (playerShooter == null)
            playerShooter = FindObjectOfType<PlayerShooter>();

        if (playerShooter == null)
        {
            RunLogger.Warning($"Shop upgrade failed: shooter missing. {upgrade.title}");
            return;
        }

        playerShooter.ApplyUpgrade(upgrade);
        RunLogger.Event($"Shop upgrade applied: {upgrade.title}");
    }

    public void ApplyShopItem(ShopItemDefinition item)
    {
        if (item == null) return;

        if (playerMotor == null)
            playerMotor = FindObjectOfType<PlayerMotor2D>();

        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();

        if (item.Effects != null)
        {
            for (int i = 0; i < item.Effects.Count; i++)
            {
                ShopItemEffect effect = item.Effects[i];
                if (effect == null) continue;

                switch (effect.effectType)
                {
                    case ShopItemEffectType.MoveSpeedPercentAdd:
                        if (playerMotor != null) playerMotor.AddMoveSpeedPercent(effect.floatValue);
                        break;
                    case ShopItemEffectType.MaxHealthAdd:
                        if (playerHealth != null) playerHealth.AddMaxHealth(effect.intValue, true);
                        break;
                    case ShopItemEffectType.AddShieldCharges:
                        if (playerHealth != null) playerHealth.AddShieldCharges(effect.intValue);
                        break;
                    case ShopItemEffectType.XPPerKillAdd:
                        bonusXPPerKill += Mathf.Max(0, effect.intValue);
                        RunLogger.Event($"Bonus XP per kill +{effect.intValue}, total={bonusXPPerKill}");
                        break;
                    case ShopItemEffectType.XPMagnetRadiusAdd:
                        bonusXPMagnetRadius += Mathf.Max(0f, effect.floatValue);
                        RunLogger.Event($"Bonus XP magnet radius +{effect.floatValue:F1}, total={bonusXPMagnetRadius:F1}");
                        break;
                    case ShopItemEffectType.CashOnKillPercentAdd:
                        cashBonusPercent += Mathf.Max(0f, effect.floatValue);
                        RunLogger.Event($"Cash on kill bonus +{effect.floatValue:F1}%, total={cashBonusPercent:F1}%");
                        break;
                }
            }
        }

        if (healthUI != null)
            healthUI.ResetHealthUI();

        RunLogger.Event($"Shop item applied: {item.ItemTitle}");
    }

    public void AddDebtPenaltyToNextRound(int amount)
    {
        int v = Mathf.Max(0, amount);
        if (v == 0) return;

        runProgression.AddDebtIncreaseToNextRound(v);
        RunLogger.Warning($"Next round debt increased by {v}. pending={runProgression.NextRoundDebtIncrease}");
        if (shopSystem != null) shopSystem.RefreshShopUI();
    }

    public void AddEnemyBuffToNextRound(float hpMultiplier, float speedMultiplier)
    {
        runProgression.AddEnemyBuffToNextRound(hpMultiplier, speedMultiplier);
        RunLogger.Warning($"Next round enemy buff queued. hpX={runProgression.NextRoundEnemyHpMultiplier:F2}, speedX={runProgression.NextRoundEnemySpeedMultiplier:F2}");
        if (shopSystem != null) shopSystem.RefreshShopUI();
    }

    public void GetCurrentEnemyMultipliers(out float hpMultiplier, out float speedMultiplier)
    {
        GetBaseEnemyMultipliersForRound(roundIndex, out float baseHpMultiplier, out float baseSpeedMultiplier);
        hpMultiplier = Mathf.Max(0.2f, baseHpMultiplier * runProgression.CurrentRoundEnemyHpMultiplier);
        speedMultiplier = Mathf.Max(0.2f, baseSpeedMultiplier * runProgression.CurrentRoundEnemySpeedMultiplier);
    }

    private void EnsureShopSystemBound()
    {
        if (panelShop == null) return;

        if (shopSystem == null)
            shopSystem = panelShop.GetComponent<ShopSystem>();

        if (shopSystem == null)
        {
            RunLogger.Warning("ShopSystem component not found on panelShop. Use menu to create shop panel template.");
            return;
        }

        shopSystem.Bind(this, runProgression);
    }

    private void EnsurePauseSettingsBound()
    {
        if (panelSettingsPlaceholder == null)
        {
            GameObject foundSettingsPanel = GameObject.Find("Panel_SettingsPlaceholder");
            if (foundSettingsPanel != null)
                panelSettingsPlaceholder = foundSettingsPanel;
        }

        if (panelSettingsPlaceholder == null)
            return;

        if (pauseSettingsMenu == null)
            pauseSettingsMenu = panelSettingsPlaceholder.GetComponent<SettingsMenuController>();

        if (pauseSettingsMenu == null)
            pauseSettingsMenu = panelSettingsPlaceholder.AddComponent<SettingsMenuController>();

        pauseSettingsMenu.Bind(this);
    }

    private bool EnsurePauseSettingsReady()
    {
        EnsurePauseSettingsBound();

        if (panelSettingsPlaceholder == null)
        {
            RunLogger.Error("OpenPauseSettings failed: panelSettingsPlaceholder is not assigned and could not be found.");
            return false;
        }

        return true;
    }

    // ====== Internal ======

private void SwitchState(GameState next)
{
    ForceClosePauseMenu(true);

    GameState previous = state;
    state = next;
    if (previous != next)
        RunLogger.Event($"State {previous} -> {next}");

    if (previous == GameState.Shop && next != GameState.Shop && shopSystem != null)
        shopSystem.OnShopClosed();

    if (panelTitle) panelTitle.SetActive(state == GameState.Title);
    if (panelHUD) panelHUD.SetActive(state == GameState.Gameplay || state == GameState.Settlement || state == GameState.Shop);
    if (panelLevelUp) panelLevelUp.SetActive(false);
    if (panelSettlement) panelSettlement.SetActive(state == GameState.Settlement);
    if (panelShop) panelShop.SetActive(state == GameState.Shop);
    
    // 离开GameOver状态时隐藏死亡面板
    if (previous == GameState.GameOver)
        HideAllDeathPanels();

    // 离开Victory状态时隐藏胜利面板
    if (previous == GameState.Victory && victoryPanel != null)
    {
        victoryPanel.HideVictoryPanel();
        Time.timeScale = 1f; // 恢复时间流
    }

    bool inGameplay = (state == GameState.Gameplay);

    if (!inGameplay)
    {
        StopRoundIntro();
        StopRoundClearTransition(false);
    }

    bool gameplaySystemsActive = inGameplay && !roundIntroActive && !roundClearActive;
    SetGameplaySystemsActive(gameplaySystemsActive);
    
    // 离开Gameplay就清场（结算/商店/失败/回菜单都干净）
    if (!inGameplay)
        ClearWorld();
}

private void ClearWorld()
{
    int enemyCount = CountChildren(enemiesRoot);
    int projectileCount = CountChildren(projectilesRoot);
    int pickupCount = CountChildren(pickupsRoot);

    ClearChildren(enemiesRoot);
    ClearChildren(projectilesRoot);
    ClearChildren(pickupsRoot);

    if (enemyCount > 0 || projectileCount > 0 || pickupCount > 0)
        RunLogger.Event($"World cleared: enemies={enemyCount}, projectiles={projectileCount}, pickups={pickupCount}");
}

private void ClearEnemiesForTimerEnd()
{
    EnemyController[] enemies = enemiesRoot != null
        ? enemiesRoot.GetComponentsInChildren<EnemyController>(true)
        : FindObjectsOfType<EnemyController>();

    int enemyCleared = 0;
    for (int i = 0; i < enemies.Length; i++)
    {
        EnemyController enemy = enemies[i];
        if (enemy == null) continue;

        enemy.gameObject.SetActive(false);
        Destroy(enemy.gameObject);
        enemyCleared++;
    }

    if (enemyCleared > 0)
    {
        RunLogger.Event(
            $"Round timer cleanup done: enemies={enemyCleared}, pickupsKept={CountChildren(pickupsRoot)}");
    }
}

private void ClearRoundEndTransientObjects()
{
    int projectileCount = CountChildren(projectilesRoot);
    if (projectileCount > 0)
        ClearChildren(projectilesRoot);

    WorldPopupText[] popupTexts = FindObjectsOfType<WorldPopupText>();
    int popupCount = 0;
    for (int i = 0; i < popupTexts.Length; i++)
    {
        if (popupTexts[i] == null) continue;
        Destroy(popupTexts[i].gameObject);
        popupCount++;
    }

    if (projectileCount > 0 || popupCount > 0)
    {
        RunLogger.Event(
            $"Round end transient cleanup: projectiles={projectileCount}, popups={popupCount}");
    }
}

private int ClearEnemyProjectiles(string reason = "Post-level-up safety")
{
    if (projectilesRoot == null)
        return 0;

    EnemyProjectile[] enemyProjectiles = projectilesRoot.GetComponentsInChildren<EnemyProjectile>(true);
    if (enemyProjectiles == null || enemyProjectiles.Length == 0)
        return 0;

    int cleared = 0;
    for (int i = 0; i < enemyProjectiles.Length; i++)
    {
        if (enemyProjectiles[i] == null) continue;
        enemyProjectiles[i].gameObject.SetActive(false);
        Destroy(enemyProjectiles[i].gameObject);
        cleared++;
    }

    if (cleared > 0)
        RunLogger.Event($"{reason}: cleared {cleared} enemy projectile(s).");

    return cleared;
}

private void ClearChildren(Transform root)
{
    if (root == null) return;
    for (int i = root.childCount - 1; i >= 0; i--)
        Destroy(root.GetChild(i).gameObject);
}

private int CountChildren(Transform root)
{
    return root == null ? 0 : root.childCount;
}

private void SetGameplaySystemsActive(bool active)
{
    if (enemySpawner != null) enemySpawner.enabled = active;
    if (playerShooter != null) playerShooter.enabled = active;

    if (playerMotor != null)
    {
        playerMotor.enabled = active;
        if (!active)
        {
            var rb = playerMotor.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    if (cameraFollow != null)
        cameraFollow.enabled = active;
}

private bool CanOpenPauseMenu()
{
    bool canPauseState = state == GameState.Gameplay || state == GameState.Shop;
    if (!canPauseState)
        return false;

    if (state == GameState.Gameplay && (roundIntroActive || roundClearActive))
        return false;

    return Time.timeScale > 0.01f;
}

private void ForceClosePauseMenu(bool resumeGameplayIfNeeded)
{
    pauseMenuOpen = false;
    settingsReturnTarget = SettingsReturnTarget.None;
    SetPauseMenuVisible(false);

    if (!resumeGameplayIfNeeded)
        return;

    if (state == GameState.Gameplay && !roundIntroActive && !roundClearActive)
    {
        Time.timeScale = 1f;
        SetGameplaySystemsActive(true);
        return;
    }

    if (state == GameState.Shop)
        Time.timeScale = 1f;
}

private void SetPauseMenuVisible(bool visible)
{
    if (panelPauseMenu != null)
        panelPauseMenu.SetActive(visible);

    if (!visible)
    {
        if (pauseSettingsMenu != null)
            pauseSettingsMenu.HideMenu();

        if (panelSettingsPlaceholder != null)
            panelSettingsPlaceholder.SetActive(false);
    }
}


    private void StartRoundTimer()
    {
        StopRoundTimer();
        if (IsCurrentRoundBoss())
        {
            roundTimeRemaining = -1f;
            RunLogger.Event($"Round {roundIndex} is boss round. Timer disabled.");
            return;
        }

        roundTimeRemaining = roundDurationSeconds;
        roundTimerCo = StartCoroutine(RoundTimer());
        RunLogger.Event($"Round {roundIndex} timer started: {roundDurationSeconds:F1}s");
    }

    private void StopRoundTimer()
    {
        if (roundTimerCo != null)
        {
            StopCoroutine(roundTimerCo);
            roundTimerCo = null;
        }
    }

    private IEnumerator RoundTimer()
    {
        float t = roundDurationSeconds;
        while (t > 0f && state == GameState.Gameplay)
        {
            t -= Time.deltaTime;
            roundTimeRemaining = t;  // 实时更新剩余时间
            yield return null;
        }

        if (state == GameState.Gameplay)
        {
            roundTimeRemaining = 0f;
            RunLogger.Event($"Round {roundIndex} timer reached 0.");
            EndRound(true);
        }
    }

    private int CalcDue(int round)
    {
        if (round <= 0) return 0;
        int due = baseDue + (round - 1) * stepDue;
        if (useDebtCurveMultiplier)
        {
            float debtMultiplier = EvaluateMonotonicCurve(debtCurve, Mathf.Max(1, round), debtMinGrowthPerRound);
            due = Mathf.RoundToInt(due * debtMultiplier);
        }

        if (round == roundIndex)
            due += runProgression.CurrentRoundDebtIncrease;
        else if (round == roundIndex + 1)
            due += runProgression.NextRoundDebtIncrease;

        return Mathf.Max(due, 0);
    }

    private void ApplySettlement()
    {
        int due = CalcDue(roundIndex);
        int nextRound = roundIndex + 1;
        int nextDue = CalcDue(nextRound);
        RunLogger.Event($"Settlement preview: round={roundIndex}, due={due}, cash={cash}, nextDue={nextDue}");

        // 显示结算信息（这里不进行实际扣款操作）
        if (textDue) textDue.text = $"Due: {due}";
        if (textPaid) textPaid.text = $"Cash: {cash}";
        if (textRemainingDebt)
            textRemainingDebt.text = nextRound <= GetBossRoundIndex()
                ? $"Next Round Debt: {GetDebtDisplay(nextRound)}"
                : "Next Round Debt: -";
    }

    private void RefreshHUD()
    {
        if (textRound) textRound.text = $"Round: {roundIndex}/{totalRounds}";
        if (textCash) textCash.text = $"$ {cash}";
        if (textDebt) textDebt.text = $"Debt Owed: {GetDebtDisplay(roundIndex)}";
        // 倒计时在Update中更新，这里只初始化
        if (state != GameState.Gameplay && textCountdown != null)
        {
            textCountdown.text = "";
        }
        UpdateRoundIntroText();
    }

    private void PlayFailAnimation()
    {
        if (failAnimator == null || string.IsNullOrWhiteSpace(failTriggerName)) return;
        failAnimator.SetTrigger(failTriggerName);
        RunLogger.Event($"Fail animation triggered: {failTriggerName}");
    }

    private void EnterSettlementAfterRoundEnd()
    {
        if (state != GameState.Gameplay)
            return;

        SwitchState(GameState.Settlement);
        ApplySettlement();
        RefreshHUD();
    }

    private void ShowRoundClearTransition()
    {
        if (roundClearCo != null || roundClearActive)
            StopRoundClearTransition(false);

        UpdateRoundClearText();
        bool useOverlay = EnsureRoundClearOverlay();

        roundClearActive = true;
        Time.timeScale = 0f;
        SetGameplaySystemsActive(false);

        if (useOverlay && roundClearOverlay != null)
        {
            roundClearOverlay.gameObject.SetActive(true);
            roundClearOverlay.transform.SetAsLastSibling();
            roundClearOverlay.alpha = 0f;
        }

        roundClearCo = StartCoroutine(RoundClearRoutine(useOverlay));
        RunLogger.Event($"Round clear transition shown. overlay={useOverlay}, duration={roundClearSeconds:F2}s");
    }

    private void StopRoundClearTransition(bool resumeGameplayIfNeeded)
    {
        if (roundClearCo != null)
        {
            StopCoroutine(roundClearCo);
            roundClearCo = null;
        }

        if (roundClearOverlay != null)
        {
            roundClearOverlay.alpha = 0f;
            roundClearOverlay.gameObject.SetActive(false);
        }

        if (!roundClearActive)
            return;

        roundClearActive = false;
        if (resumeGameplayIfNeeded && state == GameState.Gameplay && !roundIntroActive)
        {
            Time.timeScale = 1f;
            SetGameplaySystemsActive(true);
        }
    }

    private IEnumerator RoundClearRoutine(bool useOverlay)
    {
        if (useOverlay && roundClearOverlay != null)
        {
            yield return FadeCanvasGroup(roundClearOverlay, 0f, 1f, roundClearFadeInSeconds);
        }

        float collectStartedAt = Time.unscaledTime;
        yield return PlayRoundClearAutoCollectAnimationRealtime(useOverlay);

        float remainHold = Mathf.Max(0f, roundClearSeconds - (Time.unscaledTime - collectStartedAt));
        if (remainHold > 0f)
            yield return new WaitForSecondsRealtime(remainHold);

        yield return WaitForLevelUpPanelToCloseRealtime();

        if (useOverlay && roundClearOverlay != null)
        {
            yield return FadeCanvasGroup(roundClearOverlay, roundClearOverlay.alpha, 0f, roundClearFadeOutSeconds);
            roundClearOverlay.gameObject.SetActive(false);
        }

        roundClearCo = null;
        roundClearActive = false;
        Time.timeScale = 1f;

        EnterSettlementAfterRoundEnd();
    }

    private IEnumerator PlayRoundClearAutoCollectAnimationRealtime(bool useOverlay)
    {
        if (!autoCollectDropsOnRoundClear)
            yield break;

        Transform playerTransform = ResolvePlayerTransform();
        if (playerTransform == null)
            yield break;

        Camera mainCamera = Camera.main;
        float radiusSqr = roundClearAutoCollectRadius * roundClearAutoCollectRadius;
        float collectDistanceSqr = roundClearAutoCollectCollectDistance * roundClearAutoCollectCollectDistance;
        float moveSpeed = Mathf.Max(0.01f, roundClearAutoCollectMoveSpeed);

        XPPickup[] xpPool = pickupsRoot != null
            ? pickupsRoot.GetComponentsInChildren<XPPickup>(true)
            : FindObjectsOfType<XPPickup>();
        HealthPickup[] hpPool = pickupsRoot != null
            ? pickupsRoot.GetComponentsInChildren<HealthPickup>(true)
            : FindObjectsOfType<HealthPickup>();

        List<XPPickup> xpTargets = new List<XPPickup>();
        List<HealthPickup> hpTargets = new List<HealthPickup>();
        Vector2 playerPos = playerTransform.position;

        for (int i = 0; i < xpPool.Length; i++)
        {
            XPPickup pickup = xpPool[i];
            if (pickup == null || !pickup.isActiveAndEnabled)
                continue;

            if (IsPickupEligibleForRoundClearCollect(pickup.transform.position, playerPos, mainCamera, radiusSqr))
                xpTargets.Add(pickup);
        }

        for (int i = 0; i < hpPool.Length; i++)
        {
            HealthPickup pickup = hpPool[i];
            if (pickup == null || !pickup.isActiveAndEnabled)
                continue;

            if (IsPickupEligibleForRoundClearCollect(pickup.transform.position, playerPos, mainCamera, radiusSqr))
                hpTargets.Add(pickup);
        }

        if (xpTargets.Count == 0 && hpTargets.Count == 0)
            yield break;

        if (useOverlay && roundClearOverlay != null)
            roundClearOverlay.alpha = Mathf.Clamp01(Mathf.Min(roundClearOverlay.alpha, roundClearOverlayAlphaDuringCollect));

        float timeoutAt = Time.unscaledTime + roundClearAutoCollectMaxWaitSeconds;
        int xpCollected = 0;
        int hpCollected = 0;

        while (xpTargets.Count > 0 || hpTargets.Count > 0)
        {
            if (playerTransform == null)
                break;

            playerPos = playerTransform.position;
            float step = moveSpeed * Time.unscaledDeltaTime;

            for (int i = xpTargets.Count - 1; i >= 0; i--)
            {
                XPPickup pickup = xpTargets[i];
                if (pickup == null)
                {
                    xpTargets.RemoveAt(i);
                    continue;
                }

                pickup.transform.position = Vector2.MoveTowards(pickup.transform.position, playerPos, step);

                Vector2 delta = (Vector2)pickup.transform.position - playerPos;
                if (delta.sqrMagnitude <= collectDistanceSqr)
                {
                    if (pickup.ForceCollect())
                        xpCollected++;
                    xpTargets.RemoveAt(i);
                }
            }

            for (int i = hpTargets.Count - 1; i >= 0; i--)
            {
                HealthPickup pickup = hpTargets[i];
                if (pickup == null)
                {
                    hpTargets.RemoveAt(i);
                    continue;
                }

                pickup.transform.position = Vector2.MoveTowards(pickup.transform.position, playerPos, step);

                Vector2 delta = (Vector2)pickup.transform.position - playerPos;
                if (delta.sqrMagnitude <= collectDistanceSqr)
                {
                    if (pickup.ForceCollect())
                        hpCollected++;
                    hpTargets.RemoveAt(i);
                }
            }

            if (roundClearAutoCollectMaxWaitSeconds > 0f && Time.unscaledTime >= timeoutAt)
            {
                RunLogger.Warning("Round clear auto-collect animation timed out. Forcing remaining pickups.");
                break;
            }

            yield return null;
        }

        for (int i = 0; i < xpTargets.Count; i++)
        {
            XPPickup pickup = xpTargets[i];
            if (pickup == null) continue;
            if (pickup.ForceCollect())
                xpCollected++;
        }

        for (int i = 0; i < hpTargets.Count; i++)
        {
            HealthPickup pickup = hpTargets[i];
            if (pickup == null) continue;
            if (pickup.ForceCollect())
                hpCollected++;
        }

        int total = xpCollected + hpCollected;
        if (total > 0)
        {
            string collectScope = mainCamera != null ? "screen" : "radius-fallback";
            RunLogger.Event(
                $"Round clear auto-collect animated: total={total}, xp={xpCollected}, hp={hpCollected}, scope={collectScope}, speed={roundClearAutoCollectMoveSpeed:F1}");
        }

        if (total > 0 && roundClearCollectDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(roundClearCollectDelaySeconds);
    }

    private Transform ResolvePlayerTransform()
    {
        if (playerMotor != null)
            return playerMotor.transform;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            return playerObject.transform;

        return null;
    }

    private bool IsPickupEligibleForRoundClearCollect(Vector3 pickupWorldPos, Vector2 playerPos, Camera mainCamera, float radiusSqrFallback)
    {
        if (mainCamera != null)
        {
            Vector3 viewport = mainCamera.WorldToViewportPoint(pickupWorldPos);
            const float edgePadding = 0.02f;
            return viewport.z > 0f
                && viewport.x >= -edgePadding
                && viewport.x <= 1f + edgePadding
                && viewport.y >= -edgePadding
                && viewport.y <= 1f + edgePadding;
        }

        if (radiusSqrFallback > 0f)
        {
            Vector2 delta = (Vector2)pickupWorldPos - playerPos;
            return delta.sqrMagnitude <= radiusSqrFallback;
        }

        return true;
    }

    private IEnumerator WaitForLevelUpPanelToCloseRealtime()
    {
        if (panelLevelUp == null || !panelLevelUp.activeSelf)
            yield break;

        float timeoutAt = Time.unscaledTime + roundClearAutoCollectMaxWaitSeconds;
        while (panelLevelUp.activeSelf)
        {
            if (roundClearAutoCollectMaxWaitSeconds > 0f && Time.unscaledTime >= timeoutAt)
            {
                RunLogger.Warning("Round clear auto-collect wait timed out while level-up panel is still open.");
                yield break;
            }

            yield return null;
        }
    }

    private void ShowRoundIntro()
    {
        RefreshHUD();
        bool canUseOverlay = EnsureRoundIntroOverlay();
        if (canUseOverlay)
            SetRoundDebtVisible(false);
        else
            SetRoundDebtVisible(true);

        if (roundIntroCo != null || roundIntroActive)
            StopRoundIntro();

        roundIntroActive = true;
        Time.timeScale = 0f;
        SetGameplaySystemsActive(false);

        roundIntroCo = StartCoroutine(RoundIntroRoutine(canUseOverlay));
        RunLogger.Event($"Round intro shown. overlay={canUseOverlay}, requireAnyKey={roundIntroRequireAnyKeyToContinue}, duration={roundIntroSeconds:F1}s");
    }

    private void StopRoundIntro()
    {
        if (roundIntroCo != null)
        {
            StopCoroutine(roundIntroCo);
            roundIntroCo = null;
        }

        if (roundIntroOverlay != null)
        {
            roundIntroOverlay.alpha = 0f;
            roundIntroOverlay.gameObject.SetActive(false);
        }

        SetRoundIntroHintVisible(false);

        if (roundIntroActive)
        {
            roundIntroActive = false;
            Time.timeScale = 1f;
        }

        if (state == GameState.Gameplay)
            SetGameplaySystemsActive(true);

        SetRoundDebtVisible(false);
    }

    private IEnumerator RoundIntroRoutine(bool useOverlay)
    {
        if (useOverlay && roundIntroOverlay != null)
        {
            roundIntroOverlay.gameObject.SetActive(true);
            roundIntroOverlay.transform.SetAsLastSibling();
            roundIntroOverlay.alpha = 0f;

            yield return FadeCanvasGroup(roundIntroOverlay, 0f, 1f, roundIntroFadeInSeconds);

            if (roundIntroRequireAnyKeyToContinue)
            {
                SetRoundIntroHintVisible(true);
                yield return WaitForRoundIntroContinueInput();
                SetRoundIntroHintVisible(false);
            }
            else
            {
                float hold = Mathf.Max(0f, roundIntroSeconds - roundIntroFadeInSeconds - roundIntroFadeOutSeconds);
                if (hold > 0f)
                    yield return new WaitForSecondsRealtime(hold);
            }

            yield return FadeCanvasGroup(roundIntroOverlay, 1f, 0f, roundIntroFadeOutSeconds);
            roundIntroOverlay.gameObject.SetActive(false);
        }
        else
        {
            if (roundIntroRequireAnyKeyToContinue)
            {
                yield return WaitForRoundIntroContinueInput();
            }
            else if (roundIntroSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(roundIntroSeconds);
            }
        }

        roundIntroCo = null;

        if (roundIntroActive)
        {
            roundIntroActive = false;
            Time.timeScale = 1f;
            if (state == GameState.Gameplay)
                SetGameplaySystemsActive(true);
        }

        if (state == GameState.Gameplay)
            SetRoundDebtVisible(false);

        TryShowDeferredLevelUpRewardIfReady();

        RunLogger.Event("Round intro finished. Gameplay resumed.");
    }

    private IEnumerator WaitForRoundIntroContinueInput()
    {
        // Skip one frame to avoid consuming the click/key that started the round.
        yield return null;

        while (roundIntroActive && state == GameState.Gameplay)
        {
            if (Input.anyKeyDown)
                yield break;

            yield return null;
        }
    }

    private void SetRoundIntroHintVisible(bool visible)
    {
        if (roundIntroContinueHintText == null)
            return;

        roundIntroContinueHintText.gameObject.SetActive(visible);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;
        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }

    private void UpdateRoundIntroText()
    {
        if (roundIntroRoundText != null)
            roundIntroRoundText.text = $"ROUND {roundIndex}/{totalRounds}";

        if (roundIntroDebtText != null)
            roundIntroDebtText.text = $"DEBT   OWED\n{GetDebtDisplay(roundIndex)}";

        if (roundIntroContinueHintText != null)
            roundIntroContinueHintText.text = roundIntroContinueHintMessage;
    }

    private void UpdateRoundClearText()
    {
        if (roundClearTitleText != null)
            roundClearTitleText.text = roundClearTitleMessage;

        if (roundClearSubText == null)
            return;

        string baseText = string.IsNullOrWhiteSpace(roundClearSubMessage) ? "Round Cleared" : roundClearSubMessage;
        roundClearSubText.text = $"{baseText}\nRound {roundIndex}/{totalRounds}";
    }

    private int GetBossRoundIndex()
    {
        return totalRounds + 1;
    }

    private bool IsCurrentRoundBoss()
    {
        return roundIndex == GetBossRoundIndex();
    }

    private System.Collections.Generic.List<WeaponUpgrade> CreateDefaultFallbackWeaponUpgrades()
    {
        return new System.Collections.Generic.List<WeaponUpgrade>
        {
            new WeaponUpgrade("伤害强化 I", "提升伤害 +1", null, power: 1),
            new WeaponUpgrade("速度强化 I", "提升弹速 +2", null, speed: 2f),
            new WeaponUpgrade("攻速强化 I", "提升攻速 +0.05/秒", null, fireRate: 0.05f),
            new WeaponUpgrade("散射弹", "额外子弹 +1, 散射角 +4", null)
            {
                effects = new System.Collections.Generic.List<WeaponUpgradeEffect>
                {
                    new WeaponUpgradeEffect { effectType = WeaponUpgradeEffectType.ExtraProjectilesAdd, intValue = 1 },
                    new WeaponUpgradeEffect { effectType = WeaponUpgradeEffectType.SpreadAngleAdd, floatValue = 4f },
                }
            },
            new WeaponUpgrade("贯穿弹", "子弹穿透 +1, 击退增强 +0.2", null)
            {
                effects = new System.Collections.Generic.List<WeaponUpgradeEffect>
                {
                    new WeaponUpgradeEffect { effectType = WeaponUpgradeEffectType.PierceAdd, intValue = 1 },
                    new WeaponUpgradeEffect { effectType = WeaponUpgradeEffectType.KnockbackMultiplierAdd, floatValue = 0.2f },
                }
            },
        };
    }

    private void LogCurrentEnemyDifficulty()
    {
        GetBaseEnemyMultipliersForRound(roundIndex, out float baseHpMultiplier, out float baseSpeedMultiplier);
        float finalHpMultiplier = baseHpMultiplier * runProgression.CurrentRoundEnemyHpMultiplier;
        float finalSpeedMultiplier = baseSpeedMultiplier * runProgression.CurrentRoundEnemySpeedMultiplier;

        RunLogger.Event(
            $"Enemy scaling round {roundIndex}: baseHPx={baseHpMultiplier:F2}, baseSpeedx={baseSpeedMultiplier:F2}, " +
            $"buffHPx={runProgression.CurrentRoundEnemyHpMultiplier:F2}, buffSpeedx={runProgression.CurrentRoundEnemySpeedMultiplier:F2}, " +
            $"finalHPx={finalHpMultiplier:F2}, finalSpeedx={finalSpeedMultiplier:F2}");
    }

    private void GetBaseEnemyMultipliersForRound(int round, out float hpMultiplier, out float speedMultiplier)
    {
        int safeRound = Mathf.Max(1, round);
        hpMultiplier = EvaluateMonotonicCurve(enemyHpCurve, safeRound, enemyHpMinGrowthPerRound);
        speedMultiplier = EvaluateMonotonicCurve(enemySpeedCurve, safeRound, enemySpeedMinGrowthPerRound);
    }

    private float EvaluateMonotonicCurve(AnimationCurve curve, int targetRound, float minGrowthPerRound)
    {
        float value = Mathf.Max(1f, EvaluateCurveAtRound(curve, 1));
        if (targetRound <= 1)
            return value;

        float minGrowth = Mathf.Max(0.001f, minGrowthPerRound);
        for (int round = 2; round <= targetRound; round++)
        {
            float targetValue = Mathf.Max(1f, EvaluateCurveAtRound(curve, round));
            float minValue = value + minGrowth;
            value = Mathf.Max(targetValue, minValue);
        }

        return value;
    }

    private int CalculateXpToNext(int targetLevel)
    {
        int safeLevel = Mathf.Max(1, targetLevel);
        float growth = EvaluateMonotonicCurveByIndex(
            xpToNextCurve,
            safeLevel,
            Mathf.Max(2, xpCurveMaxLevel),
            xpMinGrowthPerLevel);
        int result = Mathf.RoundToInt(baseXpToNext * growth);
        return Mathf.Max(1, result);
    }

    private float EvaluateMonotonicCurveByIndex(AnimationCurve curve, int targetIndex, int maxIndex, float minGrowthPerStep)
    {
        float value = Mathf.Max(1f, EvaluateCurveAtIndex(curve, 1, maxIndex));
        if (targetIndex <= 1)
            return value;

        float minGrowth = Mathf.Max(0.001f, minGrowthPerStep);
        for (int index = 2; index <= targetIndex; index++)
        {
            float targetValue = Mathf.Max(1f, EvaluateCurveAtIndex(curve, index, maxIndex));
            float minValue = value + minGrowth;
            value = Mathf.Max(targetValue, minValue);
        }

        return value;
    }

    private float EvaluateCurveAtRound(AnimationCurve curve, int round)
    {
        if (curve == null || curve.length == 0)
            return 1f;

        return curve.Evaluate(GetRoundCurveT(round));
    }

    private float GetRoundCurveT(int round)
    {
        int safeRound = Mathf.Max(1, round);
        int maxRound = Mathf.Max(2, GetBossRoundIndex());
        return Mathf.Clamp01((safeRound - 1f) / (maxRound - 1f));
    }

    private float EvaluateCurveAtIndex(AnimationCurve curve, int index, int maxIndex)
    {
        if (curve == null || curve.length == 0)
            return 1f;

        int safeIndex = Mathf.Max(1, index);
        int safeMax = Mathf.Max(2, maxIndex);
        float t = Mathf.Clamp01((safeIndex - 1f) / (safeMax - 1f));
        return curve.Evaluate(t);
    }

    private string GetDebtDisplay(int round)
    {
        if (round == GetBossRoundIndex())
            return "∞";
        return $"${CalcDue(round)}";
    }

    private bool EnsureRoundIntroOverlay()
    {
        if (roundIntroOverlay != null && roundIntroRoundText != null && roundIntroDebtText != null)
            return true;

        if (roundIntroOverlayAutoCreated)
            return false;

        Canvas canvas = panelHUD != null ? panelHUD.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
            return false;

        Transform parent = panelHUD != null && panelHUD.transform.parent != null
            ? panelHUD.transform.parent
            : canvas.transform;

        GameObject overlayRoot = new GameObject("RoundIntroOverlayAuto", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.SetParent(parent, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image bg = overlayRoot.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.82f);
        bg.raycastTarget = false;

        roundIntroOverlay = overlayRoot.GetComponent<CanvasGroup>();
        roundIntroOverlay.alpha = 0f;
        roundIntroOverlay.interactable = false;
        roundIntroOverlay.blocksRaycasts = false;

        Color gold = new Color(0.95f, 0.80f, 0.12f, 1f);

        CreateIntroLine(overlayRoot.transform, 70f, gold);
        CreateIntroLine(overlayRoot.transform, -190f, gold);

        roundIntroRoundText = CreateIntroText(
            overlayRoot.transform,
            "RoundText",
            new Vector2(0f, 165f),
            new Vector2(980f, 120f),
            88f,
            gold);

        roundIntroDebtText = CreateIntroText(
            overlayRoot.transform,
            "DebtText",
            new Vector2(0f, -55f),
            new Vector2(980f, 300f),
            90f,
            gold);

        roundIntroContinueHintText = CreateIntroText(
            overlayRoot.transform,
            "ContinueHintText",
            new Vector2(0f, -250f),
            new Vector2(980f, 80f),
            36f,
            Color.white);
        roundIntroContinueHintText.fontStyle = FontStyles.Normal;
        roundIntroContinueHintText.text = roundIntroContinueHintMessage;
        roundIntroContinueHintText.gameObject.SetActive(false);

        roundIntroOverlay.gameObject.SetActive(false);
        roundIntroOverlayAutoCreated = true;
        UpdateRoundIntroText();
        return true;
    }

    private bool EnsureRoundClearOverlay()
    {
        if (roundClearOverlay != null && roundClearTitleText != null && roundClearSubText != null)
            return true;

        if (roundClearOverlayAutoCreated)
            return false;

        Canvas canvas = panelHUD != null ? panelHUD.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
            return false;

        Transform parent = panelHUD != null && panelHUD.transform.parent != null
            ? panelHUD.transform.parent
            : canvas.transform;

        GameObject overlayRoot = new GameObject("RoundClearOverlayAuto", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.SetParent(parent, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image bg = overlayRoot.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);
        bg.raycastTarget = false;

        roundClearOverlay = overlayRoot.GetComponent<CanvasGroup>();
        roundClearOverlay.alpha = 0f;
        roundClearOverlay.interactable = false;
        roundClearOverlay.blocksRaycasts = false;

        Color accent = new Color(0.20f, 1f, 0.72f, 1f);

        roundClearTitleText = CreateIntroText(
            overlayRoot.transform,
            "RoundClearTitleText",
            new Vector2(0f, 46f),
            new Vector2(1000f, 180f),
            96f,
            accent);

        roundClearSubText = CreateIntroText(
            overlayRoot.transform,
            "RoundClearSubText",
            new Vector2(0f, -110f),
            new Vector2(1000f, 180f),
            46f,
            Color.white);
        roundClearSubText.fontStyle = FontStyles.Normal;

        roundClearOverlay.gameObject.SetActive(false);
        roundClearOverlayAutoCreated = true;
        UpdateRoundClearText();
        return true;
    }

    private void CreateIntroLine(Transform parent, float y, Color color)
    {
        GameObject line = new GameObject("Line", typeof(RectTransform), typeof(Image));
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(820f, 4f);
        rect.anchoredPosition = new Vector2(0f, y);

        Image image = line.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private TMP_Text CreateIntroText(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        text.outlineWidth = 0.25f;
        text.raycastTarget = false;
        return text;
    }

    private void SetRoundDebtVisible(bool visible)
    {
        if (textRound != null) textRound.gameObject.SetActive(visible);
        if (textDebt != null) textDebt.gameObject.SetActive(visible);
    }
}
