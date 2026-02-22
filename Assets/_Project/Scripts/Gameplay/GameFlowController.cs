using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    public enum GameState { Title, Gameplay, Settlement, Shop, GameOver }
    public bool IsInGameplayState => state == GameState.Gameplay;

    [Header("Panels")]
    [SerializeField] private GameObject panelTitle;
    [SerializeField] private GameObject panelHUD;
    [SerializeField] private GameObject panelLevelUp;
    [SerializeField] private GameObject panelSettlement;
    [SerializeField] private GameObject panelShop;
    [SerializeField] private GameObject panelGameOver;
    [SerializeField] private GameObject panelPauseMenu;
    [SerializeField] private GameObject panelSettingsPlaceholder;
    [SerializeField] private SettingsMenuController pauseSettingsMenu;

    [Header("World Roots (for cleanup)")]
    [SerializeField] private Transform enemiesRoot;
    [SerializeField] private Transform projectilesRoot;
    [SerializeField] private Transform pickupsRoot;

    [Header("World References")]
    [SerializeField] private PlayerMotor2D playerMotor;
    [SerializeField] private CameraFollow2D cameraFollow;

    [Header("HUD Text")]
    [SerializeField] private TMP_Text textRound;
    [SerializeField] private TMP_Text textCash;
    [SerializeField] private TMP_Text textDebt;
    [SerializeField] private TMP_Text textCountdown;  // 倒计时显示
    [SerializeField] private float roundIntroSeconds = 2.5f;

    [Header("Round Intro Overlay (Optional)")]
    [SerializeField] private CanvasGroup roundIntroOverlay;
    [SerializeField] private TMP_Text roundIntroRoundText;
    [SerializeField] private TMP_Text roundIntroDebtText;
    [SerializeField] private TMP_Text roundIntroContinueHintText;
    [SerializeField] private bool roundIntroRequireAnyKeyToContinue = true;
    [SerializeField] private string roundIntroContinueHintMessage = "Press Any Key to Continue";
    [SerializeField] private float roundIntroFadeInSeconds = 0.15f;
    [SerializeField] private float roundIntroFadeOutSeconds = 0.30f;

    [Header("Round Clear Overlay (Optional)")]
    [SerializeField] private CanvasGroup roundClearOverlay;
    [SerializeField] private TMP_Text roundClearTitleText;
    [SerializeField] private TMP_Text roundClearSubText;
    [SerializeField] private bool showRoundClearTransition = true;
    [SerializeField] private string roundClearTitleMessage = "YOU PASS!";
    [SerializeField] private string roundClearSubMessage = "Round Cleared";
    [SerializeField, Min(0f)] private float roundClearSeconds = 1.2f;
    [SerializeField, Min(0f)] private float roundClearFadeInSeconds = 0.12f;
    [SerializeField, Min(0f)] private float roundClearFadeOutSeconds = 0.18f;

    [Header("HUD Health")]
    [SerializeField] private HealthUI healthUI;

    [Header("HUD XP")]
    [SerializeField] private XPUI xpUI;

    [Header("Level Up Rewards")]
    [SerializeField] private LevelUpPanel levelUpPanel;
    [SerializeField, Min(0f)] private float postLevelUpSafetyInvulnSeconds = 0.75f;
    [SerializeField] private bool clearEnemyProjectilesAfterLevelUp = true;

    [Header("Settlement Text")]
    [SerializeField] private TMP_Text textDue;
    [SerializeField] private TMP_Text textPaid;
    [SerializeField] private TMP_Text textRemainingDebt;

    [Header("Run Config (temporary numbers)")]
    [SerializeField] private int totalRounds = 10;
    [SerializeField] private int baseDue = 500;
    [SerializeField] private int stepDue = 200;
    [SerializeField] private float roundDurationSeconds = 30f;

    [Header("Enemy Difficulty Curve")]
    [SerializeField] private AnimationCurve enemyHpCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 2.2f);
    [SerializeField] private AnimationCurve enemySpeedCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1.7f);
    [SerializeField, Min(0.01f)] private float enemyHpMinGrowthPerRound = 0.12f;
    [SerializeField, Min(0.01f)] private float enemySpeedMinGrowthPerRound = 0.05f;

    [Header("XP (temp)")]
    [SerializeField] private int level = 1;
    [SerializeField] private int xp = 0;
    [SerializeField] private int xpToNext = 10;

    [Header("Weapon Upgrade Pool Asset")]
    [SerializeField] private WeaponUpgradePoolAsset weaponUpgradePoolAsset;


    [Header("Gameplay Systems")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private PlayerShooter playerShooter;
    [SerializeField] private ShopSystem shopSystem;

    [Header("Fail Animation (Optional)")]
    [SerializeField] private Animator failAnimator;
    [SerializeField] private string failTriggerName = "Fail";

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

        // 初始化升级池
        EnsureWeaponUpgradePool();

        // 初始数据
        roundIndex = 1;
        cash = 0;
        xp = 0;
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

    private void Update()
    {
        // 快速测试热键
        if (Input.GetKeyDown(KeyCode.F1)) SwitchState(GameState.Title);
        if (Input.GetKeyDown(KeyCode.F2)) StartRun();
        if (Input.GetKeyDown(KeyCode.F3)) EndRound();
        if (Input.GetKeyDown(KeyCode.F4)) EnterShop();
        if (Input.GetKeyDown(KeyCode.F5)) SwitchState(GameState.GameOver);

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
            int seconds = Mathf.Max(0, Mathf.CeilToInt(roundTimeRemaining));
            textCountdown.text = $"Time: {seconds}s";
        }
    }

    // ====== Public APIs (给其他系统调用) ======

    // 敌人死亡自动加钱会调用这个
    public void AddCash(int amount)
    {
        int v = Mathf.Max(0, amount);
        if (v == 0) return;

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
    xpToNext = Mathf.RoundToInt(xpToNext * 1.1f);

    RunLogger.Event($"Level up -> {level}, nextXP={xpToNext}");

    if (levelUpPanel != null)
    {
        WeaponUpgrade[] selectedUpgrades = SelectRandomUpgrades(3);
        if (selectedUpgrades.Length < 3)
        {
            RunLogger.Error("Level up skipped: weapon upgrade pool has no valid entries.");
            return;
        }

        levelUpPanel.ShowUpgradePanel(selectedUpgrades, OnUpgradeSelected);

        // 只有面板存在且成功走到这里才暂停游戏
        Time.timeScale = 0f;
    }
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
    }

    /// <summary>获取当前等级</summary>
    public int GetLevel() => level;

    /// <summary>获取当前XP</summary>
    public int GetCurrentXP() => xp;

    /// <summary>获取升级所需XP</summary>
    public int GetXPToNext() => xpToNext;

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
        xpToNext = 10;
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

        // 隐藏升级面板并重置武器
        if (levelUpPanel != null)
            levelUpPanel.ForceHideImmediate();

        SwitchState(GameState.Gameplay);
        ShowRoundIntro();

        if (healthUI != null)
            healthUI.ResetHealthUI();

        if (xpUI != null)
            xpUI.UpdateXPDisplay();

        StartRoundTimer();
        RefreshHUD();
    }

    // 回合结束入口（未来由倒计时/事件触发）
    public void EndRound()
    {
        if (state != GameState.Gameplay || roundClearActive) return;
        Time.timeScale = 1f;
        StopRoundTimer();

        // 注意：已移除“随机加钱”。现金应来自击杀敌人时 AddCash(固定值)。
        RunLogger.Event($"Round {roundIndex} ended. cash={cash}, level={level}, xp={xp}/{xpToNext}");

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
            PlayFailAnimation();
            SwitchState(GameState.GameOver);
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
            RunLogger.Event("Reached end of boss round. Return to title.");
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

    // 玩家受到致命伤害 - 触发游戏结束
    public void TriggerGameOver()
    {
        if (state != GameState.Gameplay) return;
        RunLogger.Warning($"Game over triggered. round={roundIndex}, cash={cash}, due={CalcDue(roundIndex)}, level={level}");
        PlayFailAnimation();
        StopRoundClearTransition(false);
        Time.timeScale = 1f; // 确保游戏时间恢复
        StopRoundTimer();
        SwitchState(GameState.GameOver);
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
        if (playerHealth == null && item.Effects != null)
        {
            for (int i = 0; i < item.Effects.Count; i++)
            {
                if (item.Effects[i] == null) continue;
                if (item.Effects[i].effectType == ShopItemEffectType.MaxHealthAdd ||
                    item.Effects[i].effectType == ShopItemEffectType.Heal ||
                    item.Effects[i].effectType == ShopItemEffectType.AddShieldCharges ||
                    item.Effects[i].effectType == ShopItemEffectType.EnablePeriodicShield)
                {
                    playerHealth = FindObjectOfType<PlayerHealth>();
                    break;
                }
            }
        }

        if (item.Effects != null)
        {
            for (int i = 0; i < item.Effects.Count; i++)
            {
                ShopItemEffect effect = item.Effects[i];
                if (effect == null) continue;

                switch (effect.effectType)
                {
                    case ShopItemEffectType.MoveSpeedFlatAdd:
                        if (playerMotor != null) playerMotor.AddMoveSpeedFlat(effect.floatValue);
                        break;
                    case ShopItemEffectType.MoveSpeedPercentAdd:
                        if (playerMotor != null) playerMotor.AddMoveSpeedPercent(effect.floatValue);
                        break;
                    case ShopItemEffectType.MaxHealthAdd:
                        if (playerHealth != null) playerHealth.AddMaxHealth(effect.intValue, true);
                        break;
                    case ShopItemEffectType.Heal:
                        if (playerHealth != null) playerHealth.Heal(effect.intValue);
                        break;
                    case ShopItemEffectType.AddShieldCharges:
                        if (playerHealth != null) playerHealth.AddShieldCharges(effect.intValue);
                        break;
                    case ShopItemEffectType.EnablePeriodicShield:
                        if (playerHealth != null) playerHealth.EnablePeriodicShield(Mathf.Max(0.1f, effect.floatValue), Mathf.Max(1, effect.intValue));
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
            shopSystem = panelShop.AddComponent<ShopSystem>();

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
    if (panelGameOver) panelGameOver.SetActive(state == GameState.GameOver);

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

private void ClearEnemyProjectiles()
{
    if (projectilesRoot == null)
        return;

    EnemyProjectile[] enemyProjectiles = projectilesRoot.GetComponentsInChildren<EnemyProjectile>(true);
    if (enemyProjectiles == null || enemyProjectiles.Length == 0)
        return;

    int cleared = 0;
    for (int i = 0; i < enemyProjectiles.Length; i++)
    {
        if (enemyProjectiles[i] == null) continue;
        enemyProjectiles[i].gameObject.SetActive(false);
        Destroy(enemyProjectiles[i].gameObject);
        cleared++;
    }

    if (cleared > 0)
        RunLogger.Event($"Post-level-up safety: cleared {cleared} enemy projectile(s).");
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
            EndRound();
        }
    }

    private int CalcDue(int round)
    {
        if (round <= 0) return 0;
        int due = baseDue + (round - 1) * stepDue;

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

            float hold = Mathf.Max(0f, roundClearSeconds - roundClearFadeInSeconds - roundClearFadeOutSeconds);
            if (hold > 0f)
                yield return new WaitForSecondsRealtime(hold);

            yield return FadeCanvasGroup(roundClearOverlay, 1f, 0f, roundClearFadeOutSeconds);
            roundClearOverlay.gameObject.SetActive(false);
        }
        else if (roundClearSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(roundClearSeconds);
        }

        roundClearCo = null;
        roundClearActive = false;
        Time.timeScale = 1f;

        EnterSettlementAfterRoundEnd();
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
