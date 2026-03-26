using System.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    public enum GameState { Title, Gameplay, Settlement, Shop, GameOver, Victory }
    public enum DeathType { KilledByMonster, FailedDebt }

    [System.Serializable]
    private class StoryComicPage
    {
        [SerializeField] private Sprite pageSprite;
        [SerializeField, Range(2, 4)] private int clicksToAdvance = 2;

        public Sprite PageSprite => pageSprite;
        public int ClicksToAdvance => Mathf.Clamp(clicksToAdvance, 2, 4);
    }

    public bool IsInGameplayState => state == GameState.Gameplay;
    public bool IsPauseMenuOpen => pauseMenuOpen;
    public bool IsInShopState => state == GameState.Shop;

    [SerializeField] private GameObject panelTitle;
    [SerializeField] private GameObject panelHUD;
    [SerializeField] private GameObject panelLevelUp;
    [SerializeField] private GameObject panelSettlement;
    [SerializeField] private GameObject panelShop;
    [SerializeField] private GameObject panelCredits;
    [SerializeField] private DeathPanel monsterDeathPanel;
    [SerializeField] private DeathPanel debtFailurePanel;
    [SerializeField] private VictoryPanel victoryPanel;
    [SerializeField] private GameObject panelPauseMenu;
    [SerializeField] private GameObject panelSettingsPlaceholder;
    [SerializeField] private SettingsMenuController pauseSettingsMenu;
    [SerializeField] private QuitConfirmDialog quitConfirmDialog;
    [SerializeField] private RoundPresentationController roundPresentation;
    [SerializeField] private BossHealthBarController bossHealthBar;
    [SerializeField] private PlayerActiveItemController playerActiveItemController;

    [Header("Story Intro / 开场漫画")]
    [SerializeField] private bool enableStoryComicIntro = true;
    [SerializeField] private bool lockStorySkipOnFirstPlay = true;
    [SerializeField] private string storyPlayedFlagKey = "DebtRunner.StoryComicPlayed";
    [SerializeField] private List<StoryComicPage> storyComicPages = new List<StoryComicPage>();
    [SerializeField] private CanvasGroup storyIntroOverlay;
    [SerializeField] private Image storyIntroBackground;
    [SerializeField] private Image storyIntroPageImage;
    [SerializeField] private TMP_Text storyIntroHintText;
    [SerializeField] private Button storyIntroSkipButton;
    [SerializeField] private TMP_Text storyIntroSkipButtonText;
    [SerializeField] private string storyIntroHintTemplate = "Click to continue";
    [SerializeField] private string storyIntroSkipButtonLabel = "Skip Story";

    [SerializeField] private Transform enemiesRoot;
    [SerializeField] private Transform projectilesRoot;
    [SerializeField] private Transform pickupsRoot;

    [SerializeField] private PlayerMotor2D playerMotor;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private CameraFollow2D cameraFollow;

    [SerializeField] private TMP_Text textRound;
    [SerializeField] private TMP_Text textCash;
    [SerializeField] private TMP_Text textDebt;
    [SerializeField] private TMP_Text textCountdown;
    [SerializeField] private bool autoCreateGameplayRoundText = true;
    [SerializeField] private bool isolateCountdownMaterialAtRuntime = true;
    [HideInInspector, SerializeField] private float roundIntroSeconds = 2.5f;

    [HideInInspector, SerializeField] private CanvasGroup roundIntroOverlay;
    [HideInInspector, SerializeField] private TMP_Text roundIntroRoundText;
    [HideInInspector, SerializeField] private TMP_Text roundIntroDebtText;
    [HideInInspector, SerializeField] private TMP_Text roundIntroContinueHintText;
    [HideInInspector, SerializeField] private bool roundIntroRequireAnyKeyToContinue = true;
    [HideInInspector, SerializeField] private string roundIntroContinueHintMessage = "Press Any Key to Continue";
    [HideInInspector, SerializeField] private float roundIntroFadeInSeconds = 0.15f;
    [HideInInspector, SerializeField] private float roundIntroFadeOutSeconds = 0.30f;

    [HideInInspector, SerializeField] private CanvasGroup roundClearOverlay;
    [HideInInspector, SerializeField] private TMP_Text roundClearTitleText;
    [HideInInspector, SerializeField] private TMP_Text roundClearSubText;
    [HideInInspector, SerializeField] private bool showRoundClearTransition = true;
    [HideInInspector, SerializeField] private string roundClearTitleMessage = "YOU PASS!";
    [HideInInspector, SerializeField] private string roundClearSubMessage = "Round Cleared";
    [HideInInspector, SerializeField, Min(0f)] private float roundClearSeconds = 1.2f;
    [HideInInspector, SerializeField, Min(0f)] private float roundClearFadeInSeconds = 0.12f;
    [HideInInspector, SerializeField, Min(0f)] private float roundClearFadeOutSeconds = 0.18f;
    [SerializeField] private bool autoCollectDropsOnRoundClear = true;
    [SerializeField, Min(0f)] private float roundClearAutoCollectRadius = 4.5f;
    [SerializeField, Min(0f)] private float roundClearAutoCollectMoveSpeed = 15f;
    [SerializeField, Min(0f)] private float roundClearAutoCollectCollectDistance = 0.2f;
    [HideInInspector, SerializeField, Range(0f, 1f)] private float roundClearOverlayAlphaDuringCollect = 0.45f;
    [SerializeField, Min(0f)] private float roundClearCollectDelaySeconds = 0.12f;
    [SerializeField, Min(0f)] private float roundClearAutoCollectMaxWaitSeconds = 15f;

    [HideInInspector, SerializeField] private CanvasGroup timesUpOverlay;
    [HideInInspector, SerializeField] private TMP_Text timesUpTitleText;
    [HideInInspector, SerializeField] private bool showTimesUpTransition = true;
    [HideInInspector, SerializeField] private string timesUpTitleMessage = "TIME'S UP";
    [HideInInspector, SerializeField, Min(0f)] private float timesUpSeconds = 1f;
    [HideInInspector, SerializeField, Min(0f)] private float timesUpFadeInSeconds = 0.08f;
    [HideInInspector, SerializeField, Min(0f)] private float timesUpFadeOutSeconds = 0.12f;

    [HideInInspector, SerializeField] private CanvasGroup gameOverTransitionOverlay;
    [HideInInspector, SerializeField] private TMP_Text gameOverTransitionTitleText;
    [HideInInspector, SerializeField] private TMP_Text gameOverTransitionSubText;
    [HideInInspector, SerializeField] private bool showGameOverTransition = true;
    [HideInInspector, SerializeField] private string gameOverTransitionTitleMessage = "YOU LOSS";
    [HideInInspector, SerializeField] private string gameOverTransitionSubMessage = "Run Failed";
    [HideInInspector, SerializeField, Min(0f)] private float gameOverTransitionSeconds = 1.1f;
    [HideInInspector, SerializeField, Min(0f)] private float gameOverTransitionFadeInSeconds = 0.12f;
    [HideInInspector, SerializeField, Min(0f)] private float gameOverTransitionFadeOutSeconds = 0.18f;
    [SerializeField] private bool playPlayerDeathAnimationOnLoss = true;

    [Header("Pickup Flow")]
    [SerializeField, Min(0)] private int pickupFunnelMinActiveCount = 25;
    [SerializeField, Min(0f)] private float pickupFunnelForwardOffset = 0.5f;
    [SerializeField, Min(0.01f)] private float pickupFunnelHoldSeconds = 0.2f;
    [SerializeField, Min(0.1f)] private float pickupFunnelSpeedMultiplier = 1.18f;
    [SerializeField, Min(0f)] private float pickupFunnelArrivalDistance = 0.14f;

    [SerializeField] private HealthUI healthUI;

    [SerializeField] private XPUI xpUI;

    [SerializeField] private LevelUpPanel levelUpPanel;
    [SerializeField, Min(0f)] private float postLevelUpSafetyInvulnSeconds = 0.75f;
    [SerializeField] private bool clearEnemyProjectilesAfterLevelUp = true;
    [SerializeField] private bool deferLevelUpRewardsDuringRoundClear = true;

    [SerializeField] private AudioClip sfxLevelUp;
    [SerializeField] private AudioClip sfxUIButtonClick;
    [SerializeField] private AudioClip sfxUIButtonHover;
    [SerializeField, Min(0f)] private float uiButtonHoverSfxMinInterval = 0.03f;
    [SerializeField] private bool enableKeyboardUINavigation = true;
    [SerializeField] private bool useWASDForUINavigation = true;
    [SerializeField] private bool useSpaceForUIConfirm = true;
    [Header("Cursor")]
    [SerializeField] private bool hideCursorDuringActiveGameplay = true;
    [SerializeField] private bool keyboardTabCyclesSelection = true;
    [SerializeField, Range(0.1f, 1f)] private float gamepadUINavigationDeadzone = 0.55f;
    [SerializeField, Min(0.01f)] private float gamepadUINavigationInitialRepeatDelay = 0.24f;
    [SerializeField, Min(0.01f)] private float gamepadUINavigationRepeatInterval = 0.12f;

    [Header("Gameplay Tutorial")]
    [SerializeField] private bool enableGameplayTutorial = true;
    [SerializeField] private TMP_FontAsset gameplayTutorialFont;
    [SerializeField, TextArea(2, 3)] private string gameplayTutorialMoveText = "MOVE WITH WASD";
    [SerializeField, TextArea(1, 2)] private string gameplayTutorialDashText = "PRESS SPACE TO DASH";
    [SerializeField, TextArea(2, 4)] private string gameplayTutorialPickupText = "PICK UP DROPS FROM KILLS\nXP LEVELS YOU UP. CASH PAYS DEBT.";
    [SerializeField, TextArea(2, 4)] private string gameplayTutorialDebtText = "WORK HARD. PAY BACK.\nTHE COLLECTOR NEVER MISSES A PAYMENT.";
    [SerializeField] private Color gameplayTutorialTextColor = new Color(0.96f, 0.97f, 0.93f, 1f);
    [SerializeField] private Color gameplayTutorialCompleteColor = new Color(0.34f, 1f, 0.44f, 1f);
    [SerializeField] private Vector3 gameplayTutorialMoveOffset = new Vector3(0f, 2.55f, 0f);
    [SerializeField] private Vector3 gameplayTutorialPickupOffset = new Vector3(0f, 2.35f, 0f);
    [SerializeField, Min(0.5f)] private float gameplayTutorialMoveLifetime = 5.5f;
    [SerializeField, Min(0.5f)] private float gameplayTutorialDashLifetime = 5.5f;
    [SerializeField, Min(0.5f)] private float gameplayTutorialPickupLifetime = 7.5f;
    [SerializeField, Min(0.1f)] private float gameplayTutorialFontSize = 5.4f;
    [SerializeField, Min(0.01f)] private float gameplayTutorialTextScale = 0.17f;
    [SerializeField, Min(0.05f)] private float gameplayTutorialCompleteFadeDuration = 0.7f;

    [Header("Boss Victory Cinematic")]
    [SerializeField] private bool playBossVictoryCinematic = true;
    [SerializeField, Min(0f)] private float bossVictoryFreezeLeadSeconds = 0.18f;
    [SerializeField, Min(0.1f)] private float bossVictoryShakeSeconds = 0.85f;
    [SerializeField, Min(0f)] private float bossVictoryShakeAmplitude = 0.18f;
    [SerializeField, Min(0f)] private float bossVictoryShakeFrequency = 42f;
    [SerializeField, Min(0.1f)] private float bossVictoryDissolveSeconds = 0.7f;
    [SerializeField, Min(1)] private int bossVictoryShardCount = 6;
    [SerializeField, Min(0f)] private float bossVictoryShardSpeed = 2.8f;
    [SerializeField, Min(0f)] private float bossVictoryCameraShakeSeconds = 0.55f;
    [SerializeField, Min(0f)] private float bossVictoryCameraShakeAmplitude = 0.18f;
    [SerializeField, Min(0f)] private float bossVictoryCameraShakeFrequency = 34f;
    [SerializeField] private Color bossVictoryScreenFlashPrimaryColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color bossVictoryScreenFlashSecondaryColor = new Color(1f, 0.18f, 0.12f, 0.72f);
    [SerializeField, Min(0.01f)] private float bossVictoryScreenFlashPrimarySeconds = 0.08f;
    [SerializeField, Min(0.01f)] private float bossVictoryScreenFlashSecondarySeconds = 0.22f;
    [SerializeField] private Color bossVictoryFlashColor = new Color(1f, 0.93f, 0.72f, 1f);
    [SerializeField] private Color bossVictoryShardColor = new Color(1f, 0.68f, 0.36f, 0.95f);

    [Header("Victory Presentation")]
    [SerializeField] private string victoryThanksText = "THANKS FOR PLAYING";
    [SerializeField] private string victoryThanksSubText = "Debt paid. For now.";
    [SerializeField, Min(0.1f)] private float victoryThanksSeconds = 2.4f;
    [SerializeField, Min(0.1f)] private float victoryCreditsSeconds = 5f;
    [SerializeField, Min(0.05f)] private float victoryPresentationFadeSeconds = 0.35f;
    [SerializeField] private Color victoryPresentationBackdropColor = new Color(0f, 0f, 0f, 0.82f);
    [SerializeField] private Color victoryThanksTextColor = new Color(0.98f, 0.96f, 0.91f, 1f);

    [SerializeField] private TMP_Text textDue;
    [SerializeField] private TMP_Text textPaid;
    [SerializeField] private TMP_Text textRemainingDebt;
    [SerializeField] private TMP_Text textSettlementCashEarned;
    [SerializeField] private TMP_Text textSettlementDebtPayment;
    [SerializeField] private TMP_Text textSettlementNetCash;
    [SerializeField] private TMP_Text textSettlementNextRoundDebt;
    [SerializeField] private TMP_Text textSettlementRoundsLeft;

    [SerializeField] private int totalRounds = 10;
    [SerializeField] private int firstRoundDue = 50;
    [SerializeField] private int baseDue = 160;
    [SerializeField] private int stepDue = 85;
    [SerializeField, Min(0f)] private float cashDropGrowthPerRound = 0.38f;
    [SerializeField, Min(0f)] private float xpDropGrowthPerRound = 0.28f;
    [SerializeField] private float roundDurationSeconds = 22f;
    [SerializeField, Min(1)] private int countdownUrgentThresholdSeconds = 10;
    [SerializeField] private Color countdownUrgentColor = new Color(1f, 0.78f, 0.22f, 1f);
    [SerializeField] private Color countdownCriticalColor = new Color(1f, 0.28f, 0.28f, 1f);
    [SerializeField, Min(1f)] private float countdownUrgentScaleMultiplier = 1.28f;
    [SerializeField, Min(0f)] private float countdownTickPopSeconds = 0.18f;
    [SerializeField, Min(0f)] private float countdownTickPopAmplitude = 0.16f;
    [SerializeField, Min(1)] private int baseXpToNext = 100;

    [SerializeField] private bool useDebtCurveMultiplier = true;
    [SerializeField] private AnimationCurve debtCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 2f);
    [SerializeField, Min(0.001f)] private float debtMinGrowthPerRound = 0.05f;
    [SerializeField] private bool useIncomeScaledDebt = true;
    [SerializeField, Range(0f, 1f)] private float incomeScaledDebtRatio = 0.45f;
    [SerializeField, Min(1)] private int lateRoundDebtPressureStartRound = 6;
    [SerializeField, Range(0f, 0.25f)] private float lateRoundDebtRatioBonusPerRound = 0.06f;

    [SerializeField] private AnimationCurve enemyHpCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1.75f);
    [SerializeField] private AnimationCurve enemySpeedCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1.35f);
    [SerializeField, Min(0.01f)] private float enemyHpMinGrowthPerRound = 0.08f;
    [SerializeField, Min(0.01f)] private float enemySpeedMinGrowthPerRound = 0.03f;
    [SerializeField, Range(0.5f, 1f)] private float enemyDifficultyScaleAtRound1 = 0.9f;
    [SerializeField, Range(0.5f, 1.2f)] private float enemyDifficultyScaleAtBoss = 1f;
    [SerializeField] private AnimationCurve enemyDifficultyRamp = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Min(1f)] private float maxNextRoundEnemyHpBuffMultiplier = 1.75f;
    [SerializeField, Min(1f)] private float maxNextRoundEnemySpeedBuffMultiplier = 1.4f;
    [SerializeField, Min(1f)] private float maxNextRoundEnemyCountBuffMultiplier = 1.6f;

    [SerializeField] private int level = 1;
    [SerializeField] private int xp = 0;
    [SerializeField] private int xpToNext = 12;
    [SerializeField, Min(2)] private int xpCurveMaxLevel = 25;
    [SerializeField] private AnimationCurve xpToNextCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 9f);
    [SerializeField, Min(0.001f)] private float xpMinGrowthPerLevel = 0.2f;
    [SerializeField, Min(1f)] private float xpRequirementMultiplierAtLevel1 = 1f;
    [SerializeField, Min(1f)] private float xpRequirementMultiplierAtHighLevel = 2.3f;

    [Header("Weapon Upgrade Curve")]
    [SerializeField, Min(0.1f)] private float lockedModeOfferWeight = 1.35f;
    [SerializeField, Min(0.1f)] private float lockedBaseOfferWeight = 0.8f;
    [SerializeField, Min(0f)] private float lowerRankModeCatchUpWeightBonus = 0.3f;
    [SerializeField, Min(1)] private int orbitRingProjectileCap = 8;
    [SerializeField, Min(0)] private int orbitRingOverflowDamageAdd = 6;
    [SerializeField, Min(0f)] private float orbitRingOverflowFireRateAdd = 0.01f;

    [Header("Enemy Pressure")]
    [SerializeField, Min(1f)] private float enemyHpPressureAtRound1 = 1.05f;
    [SerializeField, Min(1f)] private float enemyHpPressureAtBoss = 1.25f;
    [SerializeField, Min(1f)] private float enemySpeedPressureAtRound1 = 1.0f;
    [SerializeField, Min(1f)] private float enemySpeedPressureAtBoss = 1.08f;

    [SerializeField] private WeaponUpgradePoolAsset weaponUpgradePoolAsset;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Header("Debug / 测试")]
    [SerializeField] private bool enableUpgradeTestHotkey = true;
    [SerializeField] private KeyCode upgradeTestHotkey = KeyCode.F7;
    [SerializeField] private bool enableBossRoundTestHotkey = true;
    [SerializeField] private KeyCode bossRoundTestHotkey = KeyCode.F6;
    [SerializeField] private KeyCode bossRoundTestAltHotkey = KeyCode.B;
    [SerializeField] private bool enableShopRoundSkipHotkey = true;
    [SerializeField] private KeyCode shopRoundSkipHotkey = KeyCode.F8;
#endif

    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private PlayerShooter playerShooter;
    [SerializeField] private ShopSystem shopSystem;

    [SerializeField] private RunSummaryPanel runSummaryPanel;

    [Header("Studio Splash")]
    [SerializeField] private StudioSplashScreen studioSplash;

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
    private Coroutine roundClearCo;
    private Coroutine timerEndResolutionCo;
    private Coroutine playerLossPresentationCo;
    private bool roundClearActive;
    private bool playerLossPresentationActive;
    private bool creditsOpen;
    private bool creditsPanelMissingWarned;
    private bool pauseMenuOpen;
    private bool pauseQuitConfirmed;
    private bool titleQuitConfirmed;
    private SettingsReturnTarget settingsReturnTarget = SettingsReturnTarget.None;
    private float roundTimeRemaining;  // Remaining time in the current round
    private float lastUIButtonHoverSfxTime = -10f;
    private GameObject keyboardNavigationRoot;
    private MoveDirection heldGamepadUIMoveDirection = MoveDirection.None;
    private float nextGamepadUINavigationTime;
    private float nextBossDefeatCheckTime = -10f;
    private bool bossSeenInCurrentBossRound;
    private int trackedBossRoundIndex = -1;
    private bool bossVictorySequenceActive;
    private bool storyIntroActive;
    private bool storySkipRequested;
    private bool storyAdvanceRequested;
    private Coroutine storyIntroCo;
    private bool startRunQueuedAfterStory;
    private readonly RunProgressionState runProgression = new RunProgressionState();
    private int pendingDeferredLevelUpChoices;
    private readonly Dictionary<WeaponModeId, int> weaponModeRanks = new Dictionary<WeaponModeId, int>();
    private readonly Dictionary<WeaponBaseUpgradeId, int> weaponBaseUpgradeRanks = new Dictionary<WeaponBaseUpgradeId, int>();
    private DeathType currentDeathType = DeathType.KilledByMonster;
    private bool countdownStyleInitialized;
    private TMP_Text countdownStyleTarget;
    private Color countdownBaseColor = Color.white;
    private Vector3 countdownBaseScale = Vector3.one;
    private int lastDisplayedCountdownSecond = int.MinValue;
    private float countdownTickPopEndTime = -1f;
    private Material countdownRuntimeMaterial;

    // Shop item bonuses, reset every run.
    private int bonusXPPerKill;
    private float xpGainPercent;
    private float bonusXPMagnetRadius;
    private float cashBonusPercent;
    private int weaponUpgradeSelectionCount;
    private WeaponModeId modeA = WeaponModeId.None;
    private WeaponModeId modeB = WeaponModeId.None;

    public int BonusXPPerKill => bonusXPPerKill;
    public float BonusXPMagnetRadius => bonusXPMagnetRadius;
    private bool IsRoundIntroActive => roundPresentation != null && roundPresentation.IsRoundIntroActive;
    private bool IsTimesUpActive => roundPresentation != null && roundPresentation.IsTimesUpActive;

    public bool HasEquippedActiveItem()
    {
        EnsurePlayerActiveItemControllerBound();
        return playerActiveItemController != null && playerActiveItemController.HasEquippedItem;
    }

    public bool HasPurchasedActiveItem()
    {
        EnsurePlayerActiveItemControllerBound();
        return playerActiveItemController != null && playerActiveItemController.HasPurchasedEquippedItem;
    }

    public ActiveItemId GetEquippedActiveItemId()
    {
        EnsurePlayerActiveItemControllerBound();
        return playerActiveItemController != null ? playerActiveItemController.EquippedItem : ActiveItemId.None;
    }

    // ====== 局内统计（每局重置） ======
    private int runTotalKills;
    private int runTotalCashEarned;
    private int runTotalXPEarned;
    private int runHighestRound;
    private int currentRoundCashEarned;
    private int previousRoundCashEarned;
    private Coroutine gameplayTutorialCo;
    private Coroutine gameplayTutorialDebtFollowupCo;
    private Coroutine bossVictorySequenceCo;
    private Coroutine victoryPresentationCo;
    private WorldInstructionText activeGameplayTutorialHint;
    private CanvasGroup bossVictoryFlashOverlay;
    private Image bossVictoryFlashImage;
    private bool bossVictoryFlashOverlayAutoCreated;
    private CanvasGroup victoryCreditsCanvasGroup;
    private CanvasGroup victoryThanksOverlay;
    private TMP_Text victoryThanksTitleText;
    private TMP_Text victoryThanksSubtitleText;
    private bool victoryThanksOverlayAutoCreated;
    private bool victoryCreditsVisible;
    private Vector3 gameplayTutorialSpawnPosition;
    private bool gameplayTutorialFirstKillShown;
    private bool gameplayTutorialFirstPickupShown;
    private int gameplayTutorialObservedActiveItemUseCount;
    private GameplayTutorialHintType activeGameplayTutorialHintType;
    private GameplayTutorialStage gameplayTutorialStage;

    private enum GameplayTutorialHintType
    {
        None,
        Move,
        ActiveItem,
        Pickup,
        Debt
    }

    private enum GameplayTutorialStage
    {
        Inactive,
        WaitingForMove,
        WaitingForActiveItem,
        WaitingForFirstKill,
        WaitingForFirstPickup,
        Complete
    }

    private sealed class GeneratedWeaponUpgradeOffer
    {
        public WeaponUpgrade Upgrade;
        public float Weight;
    }

    private static readonly WeaponModeId[] AllWeaponModes =
    {
        WeaponModeId.OrbitRing,
        WeaponModeId.NovaWave,
        WeaponModeId.OnHitScatter,
        WeaponModeId.FanSpread,
        WeaponModeId.PierceLine,
        WeaponModeId.ReturnFlight,
    };

    private static readonly WeaponBaseUpgradeId[] AllWeaponBaseUpgrades =
    {
        WeaponBaseUpgradeId.FastHands,
        WeaponBaseUpgradeId.StampedLedger,
        WeaponBaseUpgradeId.AirMail,
    };

    public int RunTotalKills => runTotalKills;
    public int RunTotalCashEarned => runTotalCashEarned;
    public int RunTotalXPEarned => runTotalXPEarned;
    public int RunHighestRound => runHighestRound;

    public bool TryGetPickupFunnelTarget(
        out Vector3 target,
        out float holdSeconds,
        out float speedMultiplier,
        out float arrivalDistance)
    {
        target = Vector3.zero;
        holdSeconds = 0f;
        speedMultiplier = 1f;
        arrivalDistance = 0f;

        if (pickupsRoot == null || playerMotor == null)
            return false;

        if (pickupsRoot.childCount <= Mathf.Max(0, pickupFunnelMinActiveCount))
            return false;

        Vector2 forward = playerMotor.LastMoveDir.sqrMagnitude > 0.001f
            ? playerMotor.LastMoveDir.normalized
            : Vector2.right;
        target = playerMotor.transform.position + (Vector3)(forward * Mathf.Max(0f, pickupFunnelForwardOffset));
        holdSeconds = Mathf.Max(0.01f, pickupFunnelHoldSeconds);
        speedMultiplier = Mathf.Max(0.1f, pickupFunnelSpeedMultiplier);
        arrivalDistance = Mathf.Max(0f, pickupFunnelArrivalDistance);
        return true;
    }

    /// <summary>敌人被击杀时调用（EnemyController.Die）</summary>
    public void NotifyEnemyKilled(Vector3 worldPosition)
    {
        runTotalKills++;

        if (!enableGameplayTutorial || gameplayTutorialStage != GameplayTutorialStage.WaitingForFirstKill)
            return;

        gameplayTutorialFirstKillShown = true;
        gameplayTutorialStage = GameplayTutorialStage.WaitingForFirstPickup;
        StopGameplayTutorialSequence();
        ShowGameplayTutorialHint(
            gameplayTutorialPickupText,
            worldPosition + gameplayTutorialPickupOffset,
            gameplayTutorialPickupLifetime,
            GameplayTutorialHintType.Pickup);
    }

    public void NotifyGameplayTutorialPickupCollected(Vector3 worldPosition)
    {
        if (!enableGameplayTutorial || gameplayTutorialStage != GameplayTutorialStage.WaitingForFirstPickup)
            return;

        gameplayTutorialFirstPickupShown = true;
        gameplayTutorialStage = GameplayTutorialStage.Complete;
        bool completedPickupHint = TryCompleteGameplayTutorialHint(GameplayTutorialHintType.Pickup);

        if (gameplayTutorialDebtFollowupCo != null)
            StopCoroutine(gameplayTutorialDebtFollowupCo);

        if (completedPickupHint)
        {
            gameplayTutorialDebtFollowupCo = StartCoroutine(ShowDebtTutorialAfterDelay(
                worldPosition + gameplayTutorialPickupOffset,
                gameplayTutorialCompleteFadeDuration));
            StartFormalGameplayAfterTutorial();
            return;
        }

        ShowGameplayTutorialHint(
            gameplayTutorialDebtText,
            worldPosition + gameplayTutorialPickupOffset,
            gameplayTutorialPickupLifetime,
            GameplayTutorialHintType.Debt);

        StartFormalGameplayAfterTutorial();
    }

    private void Awake()
    {
        // Singleton
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

        SettingsMenuController.ApplyStartupDisplaySettings();
        PrepareInitialMenuSafetyState();
        EnsureCountdownMaterialIsolated();
        EnsureGameplayRoundTextBound();
        EnsureUIEventSystem();

        if (enemySpawner == null)
            enemySpawner = FindObjectOfType<EnemySpawner>();
        ResolvePlayerHealth();
        if (playerShooter == null)
            playerShooter = FindObjectOfType<PlayerShooter>();

        try { EnsureShopSystemBound(); }
        catch (System.Exception ex) { RunLogger.Error($"EnsureShopSystemBound failed: {ex.Message}"); }

        try { EnsurePauseSettingsBound(); }
        catch (System.Exception ex) { RunLogger.Error($"EnsurePauseSettingsBound failed: {ex.Message}"); }
        EnsureQuitConfirmDialogBound();
        EnsureRoundPresentationBound();
        EnsureBossHealthBarBound();
        EnsurePlayerActiveItemControllerBound();
        EnsurePauseMenuButtonsBound();

        try { EnsureDeathPanelsBound(); }
        catch (System.Exception ex) { RunLogger.Error($"EnsureDeathPanelsBound failed: {ex.Message}"); }

        DisableLegacyGameOverPanelIfPresent();

        // 灏濊瘯鑷姩缁戝畾涓ゅ姝讳骸闈㈡澘锛堟€墿鍑绘潃 / 鍊哄姟澶辫触锛?

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

        // 鍒濆鍖栧崌绾ф睜
        EnsureWeaponUpgradePool();

        // 鍒濆鏁版嵁
        baseXpToNext = Mathf.Max(1, baseXpToNext);
        level = Mathf.Max(1, level);
        roundIndex = 1;
        cash = 0;
        xp = 0;
        xpToNext = CalculateXpToNext(level);
        runProgression.Reset();
        runProgression.BeginRound();

        RunLogger.Event($"GameFlow ready: rounds={totalRounds}, round1Due={CalcDue(1)}, dueStep={stepDue}, roundDuration={roundDurationSeconds:F1}s");

        // 鍒濆鐣岄潰锛氬彧鏄剧ずTitle
        // If splash screen exists, hide everything until splash finishes.
        if (studioSplash != null)
        {
            // Hide all panels and disable gameplay systems during splash.
            if (panelTitle != null) panelTitle.SetActive(false);
            if (panelHUD != null) panelHUD.SetActive(false);
            SetGameplaySystemsActive(false);
        }
        else
        {
            SwitchState(GameState.Title);
        }
        ForceClosePauseMenu(false);
        BindHoverSfxToSceneButtons();
        BindKeyboardFocusIndicatorsToSceneSelectables();
        RefreshHUD();
        RefreshCursorVisibility();
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        if (roundPresentation == null)
            roundPresentation = GetComponent<RoundPresentationController>();

        if (bossHealthBar == null)
            bossHealthBar = GetComponent<BossHealthBarController>();

        if (roundPresentation != null)
            SyncRoundPresentationConfig();

        if (bossHealthBar != null)
            bossHealthBar.BindCanvasSource(panelHUD);
    }

    [ContextMenu("Sync Round Presentation Config")]
    private void EditorSyncRoundPresentationConfig()
    {
        if (roundPresentation == null)
            roundPresentation = GetComponent<RoundPresentationController>();

        if (roundPresentation == null)
            return;

        SyncRoundPresentationConfig();
    }

    private IEnumerator Start()
    {
        // Warm up global SFX singleton before first UI hover event.
        _ = SFXManager.Instance;

        BGMManager bgm = BGMManager.Instance != null ? BGMManager.Instance : FindObjectOfType<BGMManager>();
        if (bgm != null)
            bgm.OnGameStateChanged(state);

        // Play studio splash before title screen if assigned.
        if (studioSplash != null)
        {
            bool splashDone = false;
            studioSplash.Play(() => splashDone = true);

            while (!splashDone)
                yield return null;

            // Splash done — now show title.
            SwitchState(GameState.Title);
        }

        // Some UI buttons are instantiated/enabled in Start of other scripts.
        yield return null;
        BindHoverSfxToSceneButtons();
        BindKeyboardFocusIndicatorsToSceneSelectables();

        // One extra frame makes title-menu first-open binding stable.
        yield return null;
        BindHoverSfxToSceneButtons();
        BindKeyboardFocusIndicatorsToSceneSelectables();
    }

    private void OnDestroy()
    {
        StopStoryIntroPresentation(true);

        if (countdownRuntimeMaterial != null)
        {
            Destroy(countdownRuntimeMaterial);
            countdownRuntimeMaterial = null;
        }
    }

    private void PrepareInitialMenuSafetyState()
    {
        EnsureCreditsPanelBound();
        EnsureLevelUpPanelBound();
        if (panelTitle != null) panelTitle.SetActive(true);
        if (panelHUD != null) panelHUD.SetActive(false);
        if (levelUpPanel != null) levelUpPanel.ForceHideImmediate();
        else if (panelLevelUp != null) panelLevelUp.SetActive(false);
        if (panelSettlement != null) panelSettlement.SetActive(false);
        if (panelShop != null) panelShop.SetActive(false);
        if (panelCredits != null) panelCredits.SetActive(false);
        if (panelPauseMenu != null) panelPauseMenu.SetActive(false);
        if (panelSettingsPlaceholder != null) panelSettingsPlaceholder.SetActive(false);
        if (storyIntroOverlay != null) storyIntroOverlay.gameObject.SetActive(false);
        creditsOpen = false;
        ClearGameplayTutorialHint();
        EnsureCreditsButtonsBound();
    }

    private void EnsureWeaponUpgradePool()
    {
        if (weaponUpgradePoolAsset != null && weaponUpgradePoolAsset.Entries != null && weaponUpgradePoolAsset.Entries.Count > 0)
            return;

        RunLogger.Warning("Weapon upgrade pool asset is missing or empty.");
    }

    private bool EnsureLevelUpPanelBound()
    {
        if (levelUpPanel != null)
            return true;

        levelUpPanel = FindObjectOfType<LevelUpPanel>();
        if (levelUpPanel == null)
        {
            LevelUpPanel[] allPanels = Resources.FindObjectsOfTypeAll<LevelUpPanel>();
            for (int i = 0; i < allPanels.Length; i++)
            {
                LevelUpPanel panel = allPanels[i];
                if (panel == null)
                    continue;
                if (!panel.gameObject.scene.IsValid() || !panel.gameObject.scene.isLoaded)
                    continue;

                levelUpPanel = panel;
                break;
            }
        }

        if (levelUpPanel == null)
            return false;

        if (panelLevelUp == null)
            panelLevelUp = levelUpPanel.gameObject;

        RunLogger.Event("LevelUpPanel auto-bound to GameFlowController.");
        return true;
    }

    private bool IsLevelUpPanelOpen()
    {
        return levelUpPanel != null && levelUpPanel.IsShowing;
    }

    private void RefreshCursorVisibility()
    {
        bool shouldHideCursor =
            hideCursorDuringActiveGameplay &&
            state == GameState.Gameplay &&
            !pauseMenuOpen &&
            !storyIntroActive &&
            !creditsOpen &&
            !IsLevelUpPanelOpen();

        Cursor.visible = !shouldHideCursor;
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
        if (storyIntroActive)
        {
            HandleStoryIntroInput();
            return;
        }

        HandleKeyboardUINavigation();

        bool pausePressed = GameInput.IsPausePressed();
        bool backPressed = GameInput.IsBackPressed();
        if (pausePressed || backPressed)
        {
            if (creditsOpen)
            {
                CloseCredits();
            }
            else if (quitConfirmDialog != null && quitConfirmDialog.IsOpen && (pausePressed || backPressed))
            {
                ClosePauseQuitConfirm();
            }
            else
            if (panelSettingsPlaceholder != null && panelSettingsPlaceholder.activeSelf)
            {
                BackFromPauseSettings();
            }
            else if (pauseMenuOpen)
            {
                ResumeFromPauseMenu();
            }
            else if (pausePressed && CanOpenPauseMenu())
            {
                OpenPauseMenu();
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        TryHandleUpgradeTestHotkey();
        TryHandleBossRoundTestHotkey();
        TryHandleShopRoundSkipHotkey();
#endif

        UpdateCountdownDisplay();
        UpdateGameplayTutorialProgress();

        AutoCompleteBossRoundIfBossDefeated();
        TryShowDeferredLevelUpRewardIfReady();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void TryHandleUpgradeTestHotkey()
    {
        if (!enableUpgradeTestHotkey || !Input.GetKeyDown(upgradeTestHotkey))
            return;

        if (state != GameState.Gameplay || pauseMenuOpen || creditsOpen || IsRoundIntroActive || roundClearActive)
            return;

        if (IsLevelUpPanelOpen())
            return;

        if (!TryShowLevelUpRewardPanelNow())
            RunLogger.Warning("Upgrade test hotkey pressed, but no level-up choices could be shown.");
    }

    private void TryHandleBossRoundTestHotkey()
    {
        if (!enableBossRoundTestHotkey)
            return;

        bool pressedPrimary = Input.GetKeyDown(bossRoundTestHotkey);
        bool pressedAlt = bossRoundTestAltHotkey != KeyCode.None && Input.GetKeyDown(bossRoundTestAltHotkey);
        if (!pressedPrimary && !pressedAlt)
            return;

        if (storyIntroActive || pauseMenuOpen || creditsOpen || state == GameState.Title)
            return;

        DebugJumpToBossRound(false);
    }

    private void TryHandleShopRoundSkipHotkey()
    {
        if (!enableShopRoundSkipHotkey || !Input.GetKeyDown(shopRoundSkipHotkey))
            return;

        if (storyIntroActive || pauseMenuOpen || creditsOpen)
            return;

        DebugJumpToNextShopRound();
    }
#endif

    // ====== Public APIs (缁欏叾浠栫郴缁熻皟鐢? ======

    // 鏁屼汉姝讳骸鑷姩鍔犻挶浼氳皟鐢ㄨ繖涓?
    public void AddCash(int amount)
    {
        int v = Mathf.Max(0, amount);
        if (v == 0) return;

        // 杩藉€哄洖鎵ｏ細鎸夌櫨鍒嗘瘮鍔犳垚
        if (cashBonusPercent > 0f)
            v = Mathf.Max(v, Mathf.RoundToInt(v * (1f + cashBonusPercent / 100f)));

        cash += v;
        currentRoundCashEarned += v;
        runTotalCashEarned += v;
        RunLogger.Event($"Cash +{v}, total={cash}");
        RefreshHUD();
        if (shopSystem != null) shopSystem.RefreshShopUI();
    }

    // 鎹″埌XP浼氳皟鐢ㄨ繖涓?
    public void AddXP(int amount)
    {
        int v = Mathf.Max(0, amount);
        if (v == 0) return;

        if (xpGainPercent > 0f)
            v = Mathf.Max(v, Mathf.RoundToInt(v * (1f + xpGainPercent / 100f)));

        xp += v;
        runTotalXPEarned += v;
        RunLogger.Event($"XP +{v}, current={xp}/{xpToNext}, level={level}");

        if (ShouldSuppressLevelUpRewardsForBossVictory())
        {
            if (xpUI != null)
                xpUI.UpdateXPDisplay();

            RunLogger.Event("Boss victory is taking priority. XP was recorded, but level-up rewards were suppressed.");
            return;
        }
        
        // 妫€鏌ユ槸鍚﹀崌绾?
        while (xp >= xpToNext)
        {
            xp -= xpToNext;
            LevelUp();
            
            // 鍗囩骇鍚庣珛鍗虫洿鏂癠I鏄剧ず
            if (xpUI != null)
                xpUI.UpdateXPDisplay();
        }

        // 鏇存柊XP UI
        if (xpUI != null)
            xpUI.UpdateXPDisplay();
    }

private void LevelUp()
{
    level += 1;
    xpToNext = CalculateXpToNext(level);

    RunLogger.Event($"Level up -> {level}, nextXP={xpToNext}");

    if (sfxLevelUp != null && SFXManager.Instance != null)
        SFXManager.Instance.Play(sfxLevelUp);

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
    if (ShouldSuppressLevelUpRewardsForBossVictory())
        return false;

    if (!EnsureLevelUpPanelBound())
    {
        RunLogger.Warning("Level-up reward queued: LevelUpPanel reference missing.");
        return false;
    }

    if (IsLevelUpPanelOpen())
        return false;

    WeaponUpgrade[] selectedUpgrades = SelectRandomUpgrades(3);
    if (selectedUpgrades.Length < 3)
    {
        RunLogger.Error("Level up skipped: upgrade offer generator could not produce three valid entries.");
        return false;
    }

    if (panelHUD != null)
        panelHUD.SetActive(false);

    levelUpPanel.ShowUpgradePanel(selectedUpgrades, OnUpgradeSelected);
    PlayLevelUpPanelBGM();

    // 鍙湁闈㈡澘瀛樺湪涓旀垚鍔熻蛋鍒拌繖閲屾墠鏆傚仠娓告垙
    Time.timeScale = 0f;
    RefreshCursorVisibility();
    return true;
}

private void TryShowDeferredLevelUpRewardIfReady()
{
    if (pendingDeferredLevelUpChoices <= 0)
        return;

    if (ShouldSuppressLevelUpRewardsForBossVictory())
    {
        ClearPendingLevelUpRewardsForBossVictory();
        return;
    }

    if (state != GameState.Gameplay || IsRoundIntroActive || roundClearActive)
        return;

    if (IsLevelUpPanelOpen())
        return;

    if (!TryShowLevelUpRewardPanelNow())
        return;

    pendingDeferredLevelUpChoices = Mathf.Max(0, pendingDeferredLevelUpChoices - 1);
    RunLogger.Event($"Deferred level-up reward presented. remaining={pendingDeferredLevelUpChoices}");
}


    /// <summary>
    /// 闅忔満閫夋嫨鍗囩骇閫夐」
    /// </summary>
    private WeaponUpgrade[] SelectRandomUpgrades(int count)
    {
        if (count <= 0)
            return new WeaponUpgrade[0];

        if (weaponUpgradeSelectionCount <= 0 || modeA == WeaponModeId.None)
            return SelectInitialModeOffers(count, null);

        if (weaponUpgradeSelectionCount == 1 || modeB == WeaponModeId.None)
            return SelectInitialModeOffers(count, modeA);

        return SelectLockedModeAndBaseOffers(count);
    }

    private WeaponUpgrade[] SelectInitialModeOffers(int count, WeaponModeId? excludedMode)
    {
        List<WeaponModeId> eligibleModes = new List<WeaponModeId>(AllWeaponModes.Length);
        for (int i = 0; i < AllWeaponModes.Length; i++)
        {
            WeaponModeId mode = AllWeaponModes[i];
            if (excludedMode.HasValue && mode == excludedMode.Value)
                continue;

            eligibleModes.Add(mode);
        }

        if (eligibleModes.Count < count)
            return new WeaponUpgrade[0];

        List<WeaponModeId> pickedModes = PickRandomUniqueValues(eligibleModes, count);
        WeaponUpgrade[] selected = new WeaponUpgrade[pickedModes.Count];
        for (int i = 0; i < pickedModes.Count; i++)
            selected[i] = CreateModeUpgrade(pickedModes[i]);

        return selected;
    }

    private WeaponUpgrade[] SelectLockedModeAndBaseOffers(int count)
    {
        List<GeneratedWeaponUpgradeOffer> pool = new List<GeneratedWeaponUpgradeOffer>(AllWeaponBaseUpgrades.Length + 2);
        pool.Add(CreateWeightedModeOffer(modeA));
        pool.Add(CreateWeightedModeOffer(modeB));

        for (int i = 0; i < AllWeaponBaseUpgrades.Length; i++)
            pool.Add(CreateWeightedBaseOffer(AllWeaponBaseUpgrades[i]));

        List<GeneratedWeaponUpgradeOffer> picked = WeightedPickerUtility.PickUnique(pool, count, offer => offer.Weight);
        if (picked.Count < count)
            return new WeaponUpgrade[0];

        WeaponUpgrade[] selected = new WeaponUpgrade[count];
        for (int i = 0; i < count; i++)
            selected[i] = picked[i].Upgrade;

        return selected;
    }

    private GeneratedWeaponUpgradeOffer CreateWeightedModeOffer(WeaponModeId mode)
    {
        int modeRank = GetModeRank(mode);
        int siblingRank = mode == modeA ? GetModeRank(modeB) : GetModeRank(modeA);
        float weight = Mathf.Max(0.1f, lockedModeOfferWeight);
        if (modeRank < siblingRank)
            weight += Mathf.Max(0f, lowerRankModeCatchUpWeightBonus);

        return new GeneratedWeaponUpgradeOffer
        {
            Upgrade = CreateModeUpgrade(mode),
            Weight = weight,
        };
    }

    private GeneratedWeaponUpgradeOffer CreateWeightedBaseOffer(WeaponBaseUpgradeId baseUpgradeId)
    {
        int rank = GetBaseUpgradeRank(baseUpgradeId);
        float weight = Mathf.Max(0.1f, lockedBaseOfferWeight - (rank * 0.06f));

        return new GeneratedWeaponUpgradeOffer
        {
            Upgrade = CreateBaseUpgrade(baseUpgradeId),
            Weight = weight,
        };
    }

    private WeaponUpgrade CreateModeUpgrade(WeaponModeId mode)
    {
        int nextRank = GetModeRank(mode) + 1;
        string title = GetModeUpgradeTitle(mode);
        WeaponUpgrade upgrade = new WeaponUpgrade(title, GetModeUpgradeDescription(mode, nextRank), ResolveGeneratedUpgradeIcon(title))
        {
            rarity = GetModeUpgradeRarity(nextRank),
            trackType = WeaponUpgradeTrackType.Mode,
            modeId = mode,
            rank = nextRank,
            effects = BuildModeUpgradeEffects(mode, nextRank),
        };

        return upgrade;
    }

    private WeaponUpgrade CreateBaseUpgrade(WeaponBaseUpgradeId baseUpgradeId)
    {
        int nextRank = GetBaseUpgradeRank(baseUpgradeId) + 1;
        string title = GetBaseUpgradeTitle(baseUpgradeId);
        WeaponUpgrade upgrade = new WeaponUpgrade(title, GetBaseUpgradeDescription(baseUpgradeId, nextRank), ResolveGeneratedUpgradeIcon(title))
        {
            rarity = nextRank >= 4 ? UpgradeRarity.Uncommon : UpgradeRarity.Common,
            trackType = WeaponUpgradeTrackType.Base,
            baseUpgradeId = baseUpgradeId,
            rank = nextRank,
            effects = BuildBaseUpgradeEffects(baseUpgradeId, nextRank),
        };

        return upgrade;
    }

    private UpgradeRarity GetModeUpgradeRarity(int nextRank)
    {
        if (nextRank >= 5)
            return UpgradeRarity.Epic;
        if (nextRank >= 3)
            return UpgradeRarity.Rare;
        return UpgradeRarity.Uncommon;
    }

    private string GetModeUpgradeTitle(WeaponModeId mode)
    {
        switch (mode)
        {
            case WeaponModeId.OrbitRing:
                return "Collection Ring";
            case WeaponModeId.NovaWave:
                return "Final Notice";
            case WeaponModeId.OnHitScatter:
                return "Fine Print";
            case WeaponModeId.FanSpread:
                return "Double Entry";
            case WeaponModeId.PierceLine:
                return "Hard Copy";
            case WeaponModeId.ReturnFlight:
                return "Return To Sender";
            default:
                return "Mode Shift";
        }
    }

    private string GetModeUpgradeDescription(WeaponModeId mode, int nextRank)
    {
        switch (mode)
        {
            case WeaponModeId.OrbitRing:
                if (nextRank <= 1)
                    return "Collector cards orbit you.";

                return IsOrbitRingAtProjectileCap()
                    ? "Orbit ↑ Cap reached. Hits harder. Spins faster."
                    : "Orbit ↑ More cards. Wider ring. Faster spin.";
            case WeaponModeId.NovaWave:
                return nextRank <= 1
                    ? "A warning pulse bursts out."
                    : "Pulse ↑ Sooner. Wider. More paper.";
            case WeaponModeId.OnHitScatter:
                return nextRank <= 1
                    ? "Hits burst into chasing slips."
                    : "Slips ↑ More slips. Wider scatter. Better chase.";
            case WeaponModeId.FanSpread:
                return nextRank <= 1
                    ? "One throw becomes a fan."
                    : "Fan ↑ More cards. Wider fan.";
            case WeaponModeId.PierceLine:
                return nextRank <= 1
                    ? "Cards push through the line."
                    : "Pierce ↑ Deeper carry.";
            case WeaponModeId.ReturnFlight:
                return nextRank <= 1
                    ? "Cards loop back for a second pass."
                    : "Return ↑ Faster return. Harder on the way back.";
            default:
                return string.Empty;
        }
    }

    private string GetBaseUpgradeTitle(WeaponBaseUpgradeId baseUpgradeId)
    {
        switch (baseUpgradeId)
        {
            case WeaponBaseUpgradeId.QuickStamp:
                return "Quick Stamp";
            case WeaponBaseUpgradeId.StampedLedger:
                return "Stamped Ledger";
            case WeaponBaseUpgradeId.AirMail:
                return "Air Mail";
            case WeaponBaseUpgradeId.FastHands:
                return "Fast Hands";
            default:
                return "Base Upgrade";
        }
    }

    private string GetBaseUpgradeDescription(WeaponBaseUpgradeId baseUpgradeId, int nextRank)
    {
        switch (baseUpgradeId)
        {
            case WeaponBaseUpgradeId.QuickStamp:
                return nextRank <= 1
                    ? "Cards leave your hand with more snap."
                    : "Projectile Speed ↑ Faster travel.";
            case WeaponBaseUpgradeId.StampedLedger:
                return nextRank <= 1
                    ? "Heavier hits on every card."
                    : "Damage ↑ Boosts both locked modes.";
            case WeaponBaseUpgradeId.AirMail:
                return nextRank <= 1
                    ? "Cards fly faster and farther."
                    : "Flight ↑ Faster travel. Longer reach.";
            case WeaponBaseUpgradeId.FastHands:
                return nextRank <= 1
                    ? "Attack Speed ↑ Throw cards more often."
                    : "Attack Speed ↑ Higher firing frequency.";
            default:
                return string.Empty;
        }
    }

    private Sprite ResolveGeneratedUpgradeIcon(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return null;

        EnsureWeaponUpgradePool();
        if (weaponUpgradePoolAsset == null || weaponUpgradePoolAsset.Entries == null)
            return null;

        for (int i = 0; i < weaponUpgradePoolAsset.Entries.Count; i++)
        {
            WeaponUpgradeDefinition entry = weaponUpgradePoolAsset.Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.UpgradeTitle))
                continue;

            if (string.Equals(entry.UpgradeTitle.Trim(), title.Trim(), System.StringComparison.OrdinalIgnoreCase))
                return entry.Icon;
        }

        return null;
    }

    private List<WeaponUpgradeEffect> BuildModeUpgradeEffects(WeaponModeId mode, int nextRank)
    {
        List<WeaponUpgradeEffect> effects = new List<WeaponUpgradeEffect>(4);

        switch (mode)
        {
            case WeaponModeId.OrbitRing:
                int currentOrbitCount = playerShooter != null ? playerShooter.GetOrbitProjectileCount() : 0;
                int remainingOrbitSlots = Mathf.Max(0, Mathf.Max(1, orbitRingProjectileCap) - currentOrbitCount);
                if (nextRank == 1)
                {
                    int addedProjectiles = Mathf.Min(3, remainingOrbitSlots);
                    if (addedProjectiles > 0)
                        effects.Add(CreateEffect(WeaponUpgradeEffectType.OrbitProjectileCountAdd, addedProjectiles));
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.OrbitRadiusAdd, floatValue: 0.25f));
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.OrbitAngularSpeedAdd, floatValue: 14f));
                    AddOrbitOverflowBonuses(effects, 3 - addedProjectiles, false);
                }
                else if (nextRank % 2 == 1)
                {
                    int addedProjectiles = Mathf.Min(1, remainingOrbitSlots);
                    if (addedProjectiles > 0)
                        effects.Add(CreateEffect(WeaponUpgradeEffectType.OrbitProjectileCountAdd, addedProjectiles));
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.OrbitAngularSpeedAdd, floatValue: 12f));
                    AddOrbitOverflowBonuses(effects, 1 - addedProjectiles, true);
                }
                else
                {
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.OrbitRadiusAdd, floatValue: 0.1f));
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.OrbitAngularSpeedAdd, floatValue: 16f));
                    if (remainingOrbitSlots <= 0)
                        AddOrbitOverflowBonuses(effects, 1, true);
                }
                break;

            case WeaponModeId.NovaWave:
                if (nextRank == 1)
                {
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.NovaProjectileCountAdd, 6));
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.NovaIntervalAdd, floatValue: -0.2f));
                }
                else
                {
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.NovaProjectileCountAdd, 2));
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.NovaIntervalAdd, floatValue: -0.1f));
                }
                break;

            case WeaponModeId.OnHitScatter:
                effects.Add(CreateEffect(WeaponUpgradeEffectType.OnHitScatterCountAdd, 1));
                effects.Add(CreateEffect(WeaponUpgradeEffectType.OnHitScatterAngleAdd, floatValue: nextRank <= 2 ? 10f : 8f));
                break;

            case WeaponModeId.FanSpread:
                effects.Add(CreateEffect(WeaponUpgradeEffectType.ExtraProjectilesAdd, 1));
                effects.Add(CreateEffect(WeaponUpgradeEffectType.SpreadAngleAdd, floatValue: nextRank <= 2 ? 3f : 2f));
                if (nextRank >= 3)
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.FireRateAdd, floatValue: 0.008f));
                break;

            case WeaponModeId.PierceLine:
                effects.Add(CreateEffect(WeaponUpgradeEffectType.PierceAdd, 1));
                if (nextRank % 2 == 0)
                {
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.ProjectileSpeedAdd, floatValue: 1.5f));
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.KnockbackMultiplierAdd, floatValue: 0.15f));
                }
                else if (nextRank >= 3)
                {
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.DamageAdd, 6));
                }
                break;

            case WeaponModeId.ReturnFlight:
                if (nextRank == 1)
                {
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.ReturnEnable, 1));
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.ReturnSpeedMultiplierAdd, floatValue: 0.25f));
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.ProjectileSpeedAdd, floatValue: 1f));
                }
                else
                {
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.ReturnSpeedMultiplierAdd, floatValue: 0.18f));
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.ProjectileSpeedAdd, floatValue: 1f));
                    if (nextRank >= 3)
                        effects.Add(CreateEffect(WeaponUpgradeEffectType.DamageAdd, 4));
                }
                break;
        }

        return effects;
    }

    private List<WeaponUpgradeEffect> BuildBaseUpgradeEffects(WeaponBaseUpgradeId baseUpgradeId, int nextRank)
    {
        List<WeaponUpgradeEffect> effects = new List<WeaponUpgradeEffect>(2);

        switch (baseUpgradeId)
        {
            case WeaponBaseUpgradeId.QuickStamp:
                effects.Add(CreateEffect(WeaponUpgradeEffectType.ProjectileSpeedAdd, floatValue: nextRank <= 2 ? 1.4f : 1.1f));
                break;
            case WeaponBaseUpgradeId.StampedLedger:
                effects.Add(CreateEffect(WeaponUpgradeEffectType.DamageAdd, nextRank <= 2 ? 8 : 10));
                break;
            case WeaponBaseUpgradeId.AirMail:
                effects.Add(CreateEffect(WeaponUpgradeEffectType.ProjectileSpeedAdd, floatValue: nextRank <= 2 ? 1.8f : 1.4f));
                if (nextRank >= 3)
                    effects.Add(CreateEffect(WeaponUpgradeEffectType.KnockbackMultiplierAdd, floatValue: 0.08f));
                break;
            case WeaponBaseUpgradeId.FastHands:
                effects.Add(CreateEffect(WeaponUpgradeEffectType.FireRateAdd, floatValue: nextRank <= 2 ? 0.025f : 0.02f));
                break;
        }

        return effects;
    }

    private WeaponUpgradeEffect CreateEffect(WeaponUpgradeEffectType effectType, int intValue = 0, float floatValue = 0f)
    {
        return new WeaponUpgradeEffect
        {
            effectType = effectType,
            intValue = intValue,
            floatValue = floatValue,
        };
    }

    private bool IsOrbitRingAtProjectileCap()
    {
        if (playerShooter == null)
            return false;

        return playerShooter.GetOrbitProjectileCount() >= Mathf.Max(1, orbitRingProjectileCap);
    }

    private void AddOrbitOverflowBonuses(List<WeaponUpgradeEffect> effects, int overflowProjectileCount, bool includeFireRate)
    {
        if (effects == null || overflowProjectileCount <= 0)
            return;

        int damageAdd = Mathf.Max(0, orbitRingOverflowDamageAdd) * overflowProjectileCount;
        if (damageAdd > 0)
            effects.Add(CreateEffect(WeaponUpgradeEffectType.DamageAdd, damageAdd));

        if (!includeFireRate)
            return;

        float fireRateAdd = Mathf.Max(0f, orbitRingOverflowFireRateAdd) * overflowProjectileCount;
        if (fireRateAdd > 0f)
            effects.Add(CreateEffect(WeaponUpgradeEffectType.FireRateAdd, floatValue: fireRateAdd));
    }

    private int GetModeRank(WeaponModeId mode)
    {
        if (mode == WeaponModeId.None)
            return 0;

        return weaponModeRanks.TryGetValue(mode, out int rank) ? rank : 0;
    }

    private int GetBaseUpgradeRank(WeaponBaseUpgradeId baseUpgradeId)
    {
        if (baseUpgradeId == WeaponBaseUpgradeId.None)
            return 0;

        return weaponBaseUpgradeRanks.TryGetValue(baseUpgradeId, out int rank) ? rank : 0;
    }

    private void RecordWeaponUpgradeSelection(WeaponUpgrade upgrade)
    {
        if (upgrade == null)
            return;

        if (upgrade.trackType == WeaponUpgradeTrackType.Mode)
        {
            weaponUpgradeSelectionCount += 1;

            if (modeA == WeaponModeId.None)
                modeA = upgrade.modeId;
            else if (modeB == WeaponModeId.None && upgrade.modeId != modeA)
                modeB = upgrade.modeId;

            weaponModeRanks[upgrade.modeId] = GetModeRank(upgrade.modeId) + 1;
            return;
        }

        if (upgrade.trackType == WeaponUpgradeTrackType.Base)
        {
            weaponUpgradeSelectionCount += 1;
            weaponBaseUpgradeRanks[upgrade.baseUpgradeId] = GetBaseUpgradeRank(upgrade.baseUpgradeId) + 1;
        }
    }

    private void ResetWeaponUpgradeModeState()
    {
        weaponUpgradeSelectionCount = 0;
        modeA = WeaponModeId.None;
        modeB = WeaponModeId.None;
        weaponModeRanks.Clear();
        weaponBaseUpgradeRanks.Clear();
    }

    private List<T> PickRandomUniqueValues<T>(IReadOnlyList<T> source, int count)
    {
        List<T> remaining = new List<T>(source);
        List<T> picked = new List<T>(Mathf.Min(count, remaining.Count));

        while (picked.Count < count && remaining.Count > 0)
        {
            int index = Random.Range(0, remaining.Count);
            picked.Add(remaining[index]);
            remaining.RemoveAt(index);
        }

        return picked;
    }

    /// <summary>
    /// 鐜╁閫夋嫨浜嗗崌绾?
    /// </summary>
    private void OnUpgradeSelected(WeaponUpgrade upgrade)
    {
        if (upgrade == null)
        {
            RunLogger.Warning("Upgrade selected callback received null upgrade.");
        }
        else
        {
            if (playerShooter == null)
            {
                RunLogger.Warning($"Upgrade selected but playerShooter is missing. upgrade={upgrade.title}");
            }
            else
            {
                RecordWeaponUpgradeSelection(upgrade);
                playerShooter.ApplyUpgrade(upgrade);
            }
        }

    if (clearEnemyProjectilesAfterLevelUp)
        ClearEnemyProjectiles();

    // 鎭㈠娓告垙鏃堕棿
    Time.timeScale = 1f;

    RefreshGameplayHudVisibility();
    RefreshCursorVisibility();

    if (postLevelUpSafetyInvulnSeconds > 0f)
    {
        PlayerHealth resolvedPlayerHealth = ResolvePlayerHealth();
        if (resolvedPlayerHealth != null)
            resolvedPlayerHealth.GrantTemporaryInvulnerability(postLevelUpSafetyInvulnSeconds);
    }

    string upgradeTitle = upgrade != null ? upgrade.title : "<null>";
    RunLogger.Event($"Upgrade selected: {upgradeTitle}");

    TryShowDeferredLevelUpRewardIfReady();
    RefreshBGMForCurrentGameplayContext();
}

    /// <summary>鑾峰彇褰撳墠绛夌骇</summary>
    public int GetLevel() => level;

    /// <summary>鑾峰彇褰撳墠XP</summary>
    public int GetCurrentXP() => xp;

    /// <summary>鑾峰彇鍗囩骇鎵€闇€XP</summary>
    public int GetXPToNext() => xpToNext;
    public int GetCurrentRound() => roundIndex;
    public int GetTotalRounds() => totalRounds;
    public string GetNextRoundDebtDisplay() => GetDebtDisplay(roundIndex + 1);
    public int GetProjectedDebtForRound(int round, bool includeWheelDebtAdjustments = true) => CalcDue(round, includeWheelDebtAdjustments);
    public bool IsBossRoundActive() => IsCurrentRoundBoss();
    public int GetBossRoundNumber() => GetBossRoundIndex();
    public float GetCashDropMultiplierForRound(int round) => GetDropMultiplierForRound(round, cashDropGrowthPerRound);
    public float GetXPDropMultiplierForRound(int round) => GetDropMultiplierForRound(round, xpDropGrowthPerRound);

    private static float GetDropMultiplierForRound(int round, float growthPerRound)
    {
        int roundOffset = Mathf.Max(0, round - 1);
        return Mathf.Max(0f, 1f + roundOffset * Mathf.Max(0f, growthPerRound));
    }

    // UI Button: Start
    public void StartRun()
    {
        PlayUIButtonClickSfx();
        if (TryPlayStoryIntroBeforeRun())
            return;

        StartRunGameplayFlow();
    }

    private void StartRunGameplayFlow()
    {
        // 鎭㈠娓告垙鏃堕棿锛堜互闃茶繕鍦ㄦ殏鍋滅姸鎬侊級
        StopPlayerLossPresentation();
        StopRoundClearTransition(false);
        StopTimesUpTransition(false);
        StopGameOverTransition(false);
        Time.timeScale = 1f;

        // 鏂板紑灞€锛氶噸缃?
        roundIndex = 1;
        cash = 0;
        xp = 0;
        level = 1;
        baseXpToNext = Mathf.Max(1, baseXpToNext);
        xpToNext = CalculateXpToNext(level);
        pendingDeferredLevelUpChoices = 0;
        bossSeenInCurrentBossRound = false;
        trackedBossRoundIndex = -1;
        nextBossDefeatCheckTime = -10f;
        currentRoundCashEarned = 0;
        previousRoundCashEarned = 0;
        gameplayTutorialFirstKillShown = false;
        gameplayTutorialFirstPickupShown = false;
        gameplayTutorialObservedActiveItemUseCount = 0;
        gameplayTutorialStage = enableGameplayTutorial ? GameplayTutorialStage.WaitingForMove : GameplayTutorialStage.Inactive;
        runProgression.Reset();
        runProgression.BeginRound();
        ResetWeaponUpgradeModeState();
        LogCurrentEnemyDifficulty();

        RunLogger.Event($"Run started: rounds={totalRounds}, due={CalcDue(roundIndex)}, level={level}");
        Projectile.ResetPool();

        // 閲嶇疆鐜╁琛€閲忓拰琛€閲廢I
        PlayerHealth resolvedPlayerHealth = ResolvePlayerHealth();
        if (resolvedPlayerHealth != null)
        {
            resolvedPlayerHealth.ResetRuntimeStats();
            resolvedPlayerHealth.RestoreHealth();
        }

        if (playerMotor != null)
            playerMotor.ResetForNewRun();

        if (playerShooter != null)
            playerShooter.ResetRuntimeStats();

        EnsurePlayerActiveItemControllerBound();
        if (playerActiveItemController != null)
        {
            playerActiveItemController.ResetRuntimeState();
            SuppressActiveItemUseUntilRelease();
        }

        // 閲嶇疆鍟嗗簵閬撳叿鍔犳垚
        bonusXPPerKill = 0;
        xpGainPercent = 0f;
        bonusXPMagnetRadius = 0f;
        cashBonusPercent = 0f;

        // 重置局内统计
        runTotalKills = 0;
        runTotalCashEarned = 0;
        runTotalXPEarned = 0;
        runHighestRound = 1;
        gameplayTutorialSpawnPosition = ResolveGameplayTutorialAnchorPosition();
        ClearGameplayTutorialHint();
        StopGameplayTutorialSequence();

        // 闅愯棌鍗囩骇闈㈡澘骞堕噸缃鍣?
        if (levelUpPanel != null)
            levelUpPanel.ForceHideImmediate();
        HideAllDeathPanels();
        HideRunSummary();
        ClearWorld();

        SwitchState(GameState.Gameplay);
        if (cameraFollow != null)
            cameraFollow.SnapToTarget();

        if (healthUI != null)
            healthUI.ResetHealthUI();

        if (xpUI != null)
            xpUI.UpdateXPDisplay();

        RefreshHUD();

        if (enableGameplayTutorial)
        {
            BeginGameplayTutorialSequence();
        }
        else
        {
            ShowRoundIntro();
            StartRoundTimer();
        }
    }

    [ContextMenu("Debug/Jump To Boss Round")]
    private void DebugJumpToBossRoundContextMenu()
    {
        DebugJumpToBossRound(false);
    }

    public void DebugJumpToBossRound(bool resetStats)
    {
        StopPlayerLossPresentation();
        StopRoundIntro();
        StopRoundClearTransition(false);
        StopTimesUpTransition(false);
        StopGameOverTransition(false);
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

            PlayerHealth resolvedPlayerHealth = ResolvePlayerHealth();
            if (resolvedPlayerHealth != null)
            {
                resolvedPlayerHealth.ResetRuntimeStats();
                resolvedPlayerHealth.RestoreHealth();
            }

            if (playerMotor != null)
                playerMotor.ResetRuntimeStats();

            if (playerShooter != null)
                playerShooter.ResetRuntimeStats();

            EnsurePlayerActiveItemControllerBound();
            if (playerActiveItemController != null)
            {
                playerActiveItemController.ResetRuntimeState();
                SuppressActiveItemUseUntilRelease();
            }

            bonusXPPerKill = 0;
            xpGainPercent = 0f;
            bonusXPMagnetRadius = 0f;
            cashBonusPercent = 0f;
            ResetWeaponUpgradeModeState();
        }

        currentRoundCashEarned = 0;
        previousRoundCashEarned = 0;
        gameplayTutorialFirstKillShown = false;
        gameplayTutorialFirstPickupShown = false;
        gameplayTutorialObservedActiveItemUseCount = 0;
        gameplayTutorialStage = GameplayTutorialStage.Inactive;
        ClearGameplayTutorialHint();
        StopGameplayTutorialSequence();
        roundIndex = GetBossRoundIndex();
        MarkBossRoundEntered();
        runProgression.BeginRound();
        LogCurrentEnemyDifficulty();

        if (state != GameState.Gameplay)
            SwitchState(GameState.Gameplay);

        ClearWorld();
        TryPlayBossRoundBGM();
        ShowRoundIntro();
        StartRoundTimer();
        RefreshHUD();

        RunLogger.Event($"Debug jump to boss round: round={roundIndex}/{totalRounds}, reset={resetStats}");
    }

    [ContextMenu("Debug/Jump To Next Shop Round")]
    private void DebugJumpToNextShopRoundContextMenu()
    {
        DebugJumpToNextShopRound();
    }

    public void DebugJumpToNextShopRound()
    {
        int maxShopRound = Mathf.Max(1, GetBossRoundIndex() - 1);
        int targetRound = Mathf.Clamp(roundIndex + 1, 1, maxShopRound);

        if (targetRound == roundIndex && state == GameState.Shop)
        {
            RunLogger.Warning($"Already at the last shop round. round={roundIndex}, bossRound={GetBossRoundIndex()}");
            return;
        }

        StopPlayerLossPresentation();
        StopRoundIntro();
        StopRoundClearTransition(false);
        StopTimesUpTransition(false);
        StopGameOverTransition(false);
        StopRoundTimer();
        Time.timeScale = 1f;
        pendingDeferredLevelUpChoices = 0;

        if (levelUpPanel != null)
            levelUpPanel.ForceHideImmediate();

        HideAllDeathPanels();
        HideRunSummary();
        ClearGameplayTutorialHint();
        StopGameplayTutorialSequence();
        gameplayTutorialStage = GameplayTutorialStage.Inactive;
        gameplayTutorialFirstKillShown = false;
        gameplayTutorialFirstPickupShown = false;

        if (shopSystem != null)
            shopSystem.OnShopClosed();

        ClearWorld();

        currentRoundCashEarned = 0;
        previousRoundCashEarned = 0;
        roundIndex = targetRound;
        runHighestRound = Mathf.Max(runHighestRound, roundIndex);

        EnterShop();
        RunLogger.Event($"Debug jump to next shop round: round={roundIndex}/{totalRounds}, nextDue={CalcDue(roundIndex + 1)}");
    }

    // 鍥炲悎缁撴潫鍏ュ彛锛堟湭鏉ョ敱鍊掕鏃?浜嬩欢瑙﹀彂锛?
    public void EndRound()
    {
        EndRound(false);
    }

    public void NotifyBossDefeated(EnemyController defeatedBoss = null)
    {
        if (state != GameState.Gameplay)
            return;

        if (!IsCurrentRoundBoss())
            return;

        if (bossVictorySequenceActive)
            return;

        if (playBossVictoryCinematic && defeatedBoss != null && TryStartBossVictorySequence(defeatedBoss))
            return;

        RunLogger.Event("Boss defeat notified. Ending boss round.");
        EndRound(false);
    }

    public bool TryStartBossVictorySequence(EnemyController defeatedBoss)
    {
        if (!playBossVictoryCinematic || defeatedBoss == null || bossVictorySequenceActive)
            return bossVictorySequenceActive;

        if (state != GameState.Gameplay || !IsCurrentRoundBoss())
            return false;

        ClearPendingLevelUpRewardsForBossVictory();
        bossVictorySequenceActive = true;
        if (bossVictorySequenceCo != null)
            StopCoroutine(bossVictorySequenceCo);

        bossVictorySequenceCo = StartCoroutine(BossVictorySequenceRoutine(defeatedBoss));
        RunLogger.Event("Boss victory cinematic started.");
        return true;
    }

    private void AutoCompleteBossRoundIfBossDefeated()
    {
        if (state != GameState.Gameplay)
            return;

        if (IsPlayerDeathPendingOrActive())
            return;

        if (bossVictorySequenceActive)
            return;

        if (!IsCurrentRoundBoss())
            return;

        if (IsRoundIntroActive || roundClearActive)
            return;

        if (trackedBossRoundIndex != roundIndex)
            MarkBossRoundEntered();

        if (Time.unscaledTime < nextBossDefeatCheckTime)
            return;

        nextBossDefeatCheckTime = Time.unscaledTime + 0.2f;

        BossAttackController[] bosses = enemiesRoot != null
            ? enemiesRoot.GetComponentsInChildren<BossAttackController>(true)
            : FindObjectsOfType<BossAttackController>();

        bool hasAliveBoss = false;
        for (int i = 0; i < bosses.Length; i++)
        {
            BossAttackController boss = bosses[i];
            if (boss == null || !boss.gameObject.activeInHierarchy)
                continue;

            EnemyController bossEnemy = boss.GetComponent<EnemyController>();
            if (bossEnemy == null || bossEnemy.CurrentHP > 0f)
            {
                hasAliveBoss = true;
                bossSeenInCurrentBossRound = true;
                break;
            }
        }

        if (!hasAliveBoss && bossSeenInCurrentBossRound)
            NotifyBossDefeated();
    }

    private void EndRound(bool triggeredByTimer)
    {
        if (state != GameState.Gameplay || roundClearActive || IsTimesUpActive) return;
        Time.timeScale = 1f;
        StopRoundTimer();

        if (triggeredByTimer)
            ClearEnemiesForTimerEnd();

        ClearRoundEndTransientObjects();

        // 娉ㄦ剰锛氬凡绉婚櫎鈥滈殢鏈哄姞閽扁€濄€傜幇閲戝簲鏉ヨ嚜鍑绘潃鏁屼汉鏃?AddCash(鍥哄畾鍊?銆?
        RunLogger.Event($"Round {roundIndex} ended. cash={cash}, level={level}, xp={xp}/{xpToNext}, fromTimer={triggeredByTimer}");

        // 妫€鏌ユ槸鍚︽垬鑳滀簡Boss - 濡傛灉鏄垯鐩存帴鏄剧ず鑳滃埄鐣岄潰
        if (IsCurrentRoundBoss())
        {
            RunLogger.Event($"Boss defeated! Showing victory screen.");
            ClearPendingLevelUpRewardsForBossVictory();

            ShowRunSummary(RunSummaryPanel.EndingType.Victory);

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

        EnsureRoundPresentationBound();
        if (roundPresentation == null || roundPresentation.ShowRoundClearTransition)
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

        PlayUIButtonClickSfx();
        Time.timeScale = 1f;

        int due = CalcDue(roundIndex);
        if (cash < due)
            RunLogger.Error($"Settlement entered without enough cash. This should be impossible now. cash={cash}, due={due}");

        int paid = Mathf.Min(cash, due);
        // Settlement is now success-only. If this invariant is somehow broken, clamp to avoid negative cash.
        cash = Mathf.Max(0, cash - due);
        RunLogger.Event($"Settlement passed: due={due}, paid={paid}, cashLeft={cash}");

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

        PlayUIButtonClickSfx();

        if (shopSystem != null && shopSystem.IsPrizeDrawInProgress())
        {
            shopSystem.ShowPrizeInfo("Draw is still spinning.");
            return;
        }

        if (shopSystem != null)
            shopSystem.OnShopClosed();

        Time.timeScale = 1f;

        CommitRoundCashEarnedToHistory();
        roundIndex += 1;
        runHighestRound = Mathf.Max(runHighestRound, roundIndex);
        int bossRound = GetBossRoundIndex();
        if (roundIndex > bossRound)
        {
            RunLogger.Event("Already defeated boss. Returning to title.");
            SwitchState(GameState.Title);
            return;
        }

        if (roundIndex == bossRound)
        {
            MarkBossRoundEntered();
            RunLogger.Event($"Enter boss round: {roundIndex}/{totalRounds}");
        }
        else
            RunLogger.Event($"Next round -> {roundIndex}");

        runProgression.BeginRound();
        LogCurrentEnemyDifficulty();
        SuppressActiveItemUseUntilRelease();
        SwitchState(GameState.Gameplay);
        TryPlayBossRoundBGM();

        ShowRoundIntro();
        StartRoundTimer();
        RefreshHUD();
    }

    // UI Button: Restart锛堢洿鎺ュ紑濮嬫父鎴忥級
    public void Restart()
    {
        StartRun();
    }

    private void SuppressActiveItemUseUntilRelease(float minimumUnscaledLockSeconds = 0.08f)
    {
        EnsurePlayerActiveItemControllerBound();
        if (playerActiveItemController == null)
            return;

        playerActiveItemController.SuppressUseUntilRelease(minimumUnscaledLockSeconds);
    }

    // 鐜╁鍙楀埌鑷村懡浼ゅ - 瑙﹀彂娓告垙缁撴潫锛堣鎬墿鍑绘潃锛?
    public void TriggerGameOver()
    {
        TriggerGameOver(DeathType.KilledByMonster);
    }

    // 鐜╁娓告垙缁撴潫 - 鎸囧畾姝诲洜
    public void TriggerGameOver(DeathType deathType)
    {
        if (state == GameState.GameOver || playerLossPresentationActive) return; // 宸茬粡鏄疓ameOver鍒欏拷鐣ラ噸澶嶈Е鍙?

        if (deathType == DeathType.KilledByMonster && state != GameState.Gameplay)
        {
            RunLogger.Warning($"Ignored monster death trigger outside gameplay. state={state}, requested={deathType}");
            return;
        }

        if (state != GameState.Gameplay)
            RunLogger.Warning($"TriggerGameOver called while state={state}; proceeding to show death panel.");

        currentDeathType = deathType;
        RunLogger.Warning($"Game over triggered. round={roundIndex}, cash={cash}, due={CalcDue(roundIndex)}, level={level}, deathType={deathType}");

        CancelBossVictorySequenceForFailure();
        StopRoundClearTransition(false);
        StopRoundTimer();
        StopGameOverTransition(false);

        if (TryStartPlayerLossPresentation(deathType))
            return;

        ContinueGameOverFlow(deathType);
    }

    // 娓告垙缁撴潫 - 鏄剧ず姝讳骸闈㈡澘锛堢敤浜庨潪Gameplay鐘舵€侊級
    private void ShowGameOverWithDeathPanel(DeathType deathType)
    {
        if (state == GameState.GameOver || playerLossPresentationActive)
            return;

        currentDeathType = deathType;
        RunLogger.Warning($"Game over with death panel. round={roundIndex}, cash={cash}, due={CalcDue(roundIndex)}, level={level}, deathType={deathType}");

        CancelBossVictorySequenceForFailure();
        StopGameOverTransition(false);
        StopRoundClearTransition(false);
        StopRoundTimer();

        if (TryStartPlayerLossPresentation(deathType))
            return;

        ContinueGameOverFlow(deathType);
    }

    private void ContinueGameOverFlow(DeathType deathType)
    {
        PlayFailAnimation();

        EnsureRoundPresentationBound();
        if (roundPresentation != null)
        {
            ShowGameOverTransition(deathType);
            return;
        }

        FinalizeGameOver(deathType);
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

    private void ShowRunSummary(RunSummaryPanel.EndingType ending)
    {
        if (runSummaryPanel != null)
            runSummaryPanel.ShowPanel(ending, runTotalKills, runTotalCashEarned, runTotalXPEarned, runHighestRound, level);
    }

    private void FinalizeGameOver(DeathType deathType)
    {
        RunSummaryPanel.EndingType ending = deathType == DeathType.FailedDebt
            ? RunSummaryPanel.EndingType.FailedDebt
            : RunSummaryPanel.EndingType.KilledByMonster;
        ShowRunSummary(ending);

        Time.timeScale = 0f;
        SwitchState(GameState.GameOver);
    }

    private void ShowGameOverTransition(DeathType deathType)
    {
        EnsureRoundPresentationBound();
        if (roundPresentation == null)
        {
            FinalizeGameOver(deathType);
            return;
        }

        roundPresentation.StopGameOverTransition();

        HideRunSummary();
        roundPresentation.PlayGameOverTransition(
            deathType == DeathType.FailedDebt,
            () =>
            {
                Time.timeScale = 0f;
                SetGameplaySystemsActive(false);
                ClearRoundEndTransientObjects();
                ClearWorld();
            },
            () => FinalizeGameOver(deathType));

        RunLogger.Event($"Game over transition shown. duration={gameOverTransitionSeconds:F2}s, deathType={deathType}");
    }

    private bool TryStartPlayerLossPresentation(DeathType deathType)
    {
        if (!playPlayerDeathAnimationOnLoss)
            return false;

        PlayerHitFeedback playerHitFeedback = ResolvePlayerHitFeedback();
        if (playerHitFeedback == null)
            return false;

        float deathDuration = playerHitFeedback.PlayDeath();
        if (deathDuration <= 0f)
            return false;

        playerLossPresentationActive = true;
        Time.timeScale = 0f;
        HidePanelsForLossPresentation();
        SetGameplaySystemsActive(false);

        if (playerLossPresentationCo != null)
            StopCoroutine(playerLossPresentationCo);

        playerLossPresentationCo = StartCoroutine(PlayerLossPresentationRoutine(deathType, deathDuration));
        RunLogger.Event($"Player loss presentation started. duration={deathDuration:F2}s, deathType={deathType}");
        return true;
    }

    private IEnumerator PlayerLossPresentationRoutine(DeathType deathType, float deathDuration)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, deathDuration));
        playerLossPresentationCo = null;
        playerLossPresentationActive = false;
        ContinueGameOverFlow(deathType);
    }

    private void StopPlayerLossPresentation()
    {
        if (playerLossPresentationCo != null)
        {
            StopCoroutine(playerLossPresentationCo);
            playerLossPresentationCo = null;
        }

        playerLossPresentationActive = false;

        PlayerHitFeedback playerHitFeedback = ResolvePlayerHitFeedback();
        if (playerHitFeedback != null)
            playerHitFeedback.ResetVisualState();
    }

    private PlayerHitFeedback ResolvePlayerHitFeedback()
    {
        if (playerMotor != null)
        {
            PlayerHitFeedback motorFeedback = playerMotor.GetComponent<PlayerHitFeedback>();
            if (motorFeedback != null)
                return motorFeedback;
        }

        return FindObjectOfType<PlayerHitFeedback>();
    }

    private void HidePanelsForLossPresentation()
    {
        HideRunSummary();

        if (panelHUD != null)
            panelHUD.SetActive(false);

        if (levelUpPanel != null)
            levelUpPanel.ForceHideImmediate();
        else if (panelLevelUp != null)
            panelLevelUp.SetActive(false);

        if (panelSettlement != null)
            panelSettlement.SetActive(false);

        if (panelShop != null)
            panelShop.SetActive(false);

        if (panelPauseMenu != null)
            panelPauseMenu.SetActive(false);

        if (panelSettingsPlaceholder != null)
            panelSettingsPlaceholder.SetActive(false);

        pauseMenuOpen = false;
        settingsReturnTarget = SettingsReturnTarget.None;
        ClearGameplayTutorialHint();
    }

    private void StopGameOverTransition(bool finalizeIfNeeded)
    {
        if (roundPresentation != null)
            roundPresentation.StopGameOverTransition();

        if (finalizeIfNeeded)
            FinalizeGameOver(currentDeathType);
    }

    private IEnumerator BossVictorySequenceRoutine(EnemyController defeatedBoss)
    {
        StopRoundTimer();
        StopRoundClearTransition(false);
        pendingDeferredLevelUpChoices = 0;
        currentDeathType = DeathType.KilledByMonster;

        SetGameplaySystemsActive(false);
        ForceCollectAllPickupsImmediate();
        ClearBossVictoryStageExcept(defeatedBoss);
        Time.timeScale = 0f;

        if (defeatedBoss == null)
        {
            FinishBossVictorySequence();
            yield break;
        }

        BossAttackController bossAttack = defeatedBoss.GetComponent<BossAttackController>();
        if (bossAttack != null)
            bossAttack.enabled = false;

        EnemyHitFeedback hitFeedback = defeatedBoss.GetComponent<EnemyHitFeedback>();
        if (hitFeedback != null)
            hitFeedback.enabled = false;

        EnemyVisualWobble wobble = defeatedBoss.GetComponent<EnemyVisualWobble>();
        if (wobble != null)
            wobble.enabled = false;

        Rigidbody2D bossRb = defeatedBoss.GetComponent<Rigidbody2D>();
        if (bossRb != null)
            bossRb.linearVelocity = Vector2.zero;

        SpriteRenderer[] renderers = defeatedBoss.GetComponentsInChildren<SpriteRenderer>(true);
        Transform shakeTarget = defeatedBoss.transform;
        Vector3 baseLocalPosition = shakeTarget.localPosition;
        Vector3 baseLocalScale = shakeTarget.localScale;
        Color[] originalColors = CacheSpriteColors(renderers);
        Camera mainCamera = Camera.main;
        Transform cameraTransform = mainCamera != null ? mainCamera.transform : null;
        Vector3 cameraBasePosition = cameraTransform != null ? cameraTransform.position : Vector3.zero;

        StartCoroutine(BossVictoryScreenFlashRoutine());

        if (bossVictoryFreezeLeadSeconds > 0f)
            yield return new WaitForSecondsRealtime(bossVictoryFreezeLeadSeconds);

        float shakeElapsed = 0f;
        while (shakeElapsed < bossVictoryShakeSeconds)
        {
            shakeElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(shakeElapsed / Mathf.Max(0.01f, bossVictoryShakeSeconds));
            float amplitude = bossVictoryShakeAmplitude * Mathf.Lerp(0.35f, 1f, t);
            Vector2 jitter = Random.insideUnitCircle * amplitude;
            shakeTarget.localPosition = baseLocalPosition + new Vector3(jitter.x, jitter.y, 0f);

            if (cameraTransform != null)
                ApplyBossVictoryCameraShake(cameraTransform, cameraBasePosition, shakeElapsed);

            float flashT = 0.5f + (0.5f * Mathf.Sin(shakeElapsed * Mathf.Max(1f, bossVictoryShakeFrequency)));
            ApplySpriteFlash(renderers, originalColors, bossVictoryFlashColor, flashT * 0.7f);
            yield return null;
        }

        shakeTarget.localPosition = baseLocalPosition;
        if (cameraTransform != null)
            cameraTransform.position = cameraBasePosition;
        SpawnBossVictoryShards(renderers);

        float dissolveElapsed = 0f;
        while (dissolveElapsed < bossVictoryDissolveSeconds)
        {
            dissolveElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(dissolveElapsed / Mathf.Max(0.01f, bossVictoryDissolveSeconds));
            ApplySpriteFlash(renderers, originalColors, bossVictoryFlashColor, 1f - t);
            ApplySpriteAlpha(renderers, 1f - t);
            shakeTarget.localScale = Vector3.Lerp(baseLocalScale, baseLocalScale * 0.72f, t);
            yield return null;
        }

        if (defeatedBoss != null)
            Destroy(defeatedBoss.gameObject);

        FinishBossVictorySequence();
    }

    private void FinishBossVictorySequence()
    {
        bossVictorySequenceActive = false;
        bossVictorySequenceCo = null;

        if (BGMManager.Instance != null)
            BGMManager.Instance.PlayVictoryBGM();

        Time.timeScale = 0f;
        SwitchState(GameState.Victory);

        HideRunSummary();
        HideVictoryThanksOverlayImmediate();

        if (victoryPresentationCo != null)
            StopCoroutine(victoryPresentationCo);
        victoryPresentationCo = StartCoroutine(VictoryPresentationRoutine());
    }

    private bool IsPlayerDeathPendingOrActive()
    {
        if (playerLossPresentationActive)
            return true;

        PlayerHealth resolvedPlayerHealth = ResolvePlayerHealth();
        if (resolvedPlayerHealth == null)
            return false;

        return resolvedPlayerHealth.IsDead || resolvedPlayerHealth.CurrentHP <= 0;
    }

    private void CancelBossVictorySequenceForFailure()
    {
        if (!bossVictorySequenceActive && bossVictorySequenceCo == null)
            return;

        if (bossVictorySequenceCo != null)
        {
            StopCoroutine(bossVictorySequenceCo);
            bossVictorySequenceCo = null;
        }

        bossVictorySequenceActive = false;

        if (bossVictoryFlashOverlay != null)
        {
            bossVictoryFlashOverlay.alpha = 0f;
            bossVictoryFlashOverlay.gameObject.SetActive(false);
        }

        RunLogger.Warning("Boss victory sequence cancelled because a failure state took priority.");
    }

    private PlayerHealth ResolvePlayerHealth()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();

        return playerHealth;
    }

    private IEnumerator VictoryPresentationRoutine()
    {
        yield return ShowVictoryThanksRoutine();
        yield return ShowVictoryCreditsRoutine();

        HideVictoryThanksOverlayImmediate();
        SetVictoryCreditsVisible(false);
        ShowRunSummary(RunSummaryPanel.EndingType.Victory);
        victoryPresentationCo = null;
    }

    private IEnumerator ShowVictoryThanksRoutine()
    {
        if (!EnsureVictoryThanksOverlay())
            yield break;

        victoryThanksOverlay.gameObject.SetActive(true);
        victoryThanksOverlay.alpha = 0f;
        victoryThanksOverlay.transform.SetAsLastSibling();

        if (victoryThanksTitleText != null)
            victoryThanksTitleText.text = string.IsNullOrWhiteSpace(victoryThanksText) ? "THANKS FOR PLAYING" : victoryThanksText;
        if (victoryThanksSubtitleText != null)
            victoryThanksSubtitleText.text = string.IsNullOrWhiteSpace(victoryThanksSubText) ? string.Empty : victoryThanksSubText;

        float fadeDuration = Mathf.Max(0.01f, victoryPresentationFadeSeconds);
        yield return FadeCanvasGroupRealtime(victoryThanksOverlay, 0f, 1f, fadeDuration);
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, victoryThanksSeconds));
        yield return FadeCanvasGroupRealtime(victoryThanksOverlay, 1f, 0f, fadeDuration);
        victoryThanksOverlay.gameObject.SetActive(false);
    }

    private IEnumerator ShowVictoryCreditsRoutine()
    {
        if (!EnsureCreditsPanelBound())
            yield break;

        SetVictoryCreditsVisible(true);
        if (panelCredits != null)
            panelCredits.transform.SetAsLastSibling();

        CanvasGroup creditsGroup = EnsureVictoryCreditsCanvasGroup();
        float fadeDuration = Mathf.Max(0.01f, victoryPresentationFadeSeconds);
        if (creditsGroup != null)
        {
            creditsGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, victoryCreditsSeconds));
            yield return FadeCanvasGroupRealtime(creditsGroup, 1f, 0f, fadeDuration);
            creditsGroup.alpha = 1f;
        }
        else
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, victoryCreditsSeconds + fadeDuration));
        }
    }

    private IEnumerator FadeCanvasGroupRealtime(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

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

    private void SetVictoryCreditsVisible(bool visible)
    {
        victoryCreditsVisible = visible && EnsureCreditsPanelBound();
        RefreshTitleAndCreditsPanels();

        if (panelCredits == null)
            return;

        SetCreditsBackButtonVisible(!victoryCreditsVisible);

        Button[] buttons = panelCredits.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (IsCreditsBackButton(buttons[i]))
                continue;
            buttons[i].interactable = !victoryCreditsVisible;
        }
    }

    private CanvasGroup EnsureVictoryCreditsCanvasGroup()
    {
        if (panelCredits == null && !EnsureCreditsPanelBound())
            return null;

        if (panelCredits == null)
            return null;

        if (victoryCreditsCanvasGroup == null)
            victoryCreditsCanvasGroup = panelCredits.GetComponent<CanvasGroup>();
        if (victoryCreditsCanvasGroup == null)
            victoryCreditsCanvasGroup = panelCredits.AddComponent<CanvasGroup>();

        return victoryCreditsCanvasGroup;
    }

    private void SetCreditsBackButtonVisible(bool visible)
    {
        if (panelCredits == null)
            return;

        Button[] buttons = panelCredits.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (!IsCreditsBackButton(button))
                continue;

            button.gameObject.SetActive(visible);
        }
    }

    private bool IsCreditsBackButton(Button button)
    {
        if (button == null)
            return false;

        string name = button.name;
        return name == "Btn_Back" || name == "Btn_CreditsBack" || name == "Btn_CloseCredits";
    }

    private void HideVictoryThanksOverlayImmediate()
    {
        if (victoryThanksOverlay == null)
            return;

        victoryThanksOverlay.alpha = 0f;
        victoryThanksOverlay.gameObject.SetActive(false);
    }

    private void ForceCollectAllPickupsImmediate()
    {
        XPPickup[] xpPool = pickupsRoot != null
            ? pickupsRoot.GetComponentsInChildren<XPPickup>(true)
            : FindObjectsOfType<XPPickup>();
        HealthPickup[] hpPool = pickupsRoot != null
            ? pickupsRoot.GetComponentsInChildren<HealthPickup>(true)
            : FindObjectsOfType<HealthPickup>();
        CashPickup[] cashPool = pickupsRoot != null
            ? pickupsRoot.GetComponentsInChildren<CashPickup>(true)
            : FindObjectsOfType<CashPickup>();

        for (int i = 0; i < xpPool.Length; i++)
        {
            if (xpPool[i] != null)
                xpPool[i].ForceCollect();
        }

        for (int i = 0; i < hpPool.Length; i++)
        {
            if (hpPool[i] != null)
                hpPool[i].ForceCollect();
        }

        for (int i = 0; i < cashPool.Length; i++)
        {
            if (cashPool[i] != null)
                cashPool[i].ForceCollect();
        }
    }

    private void ClearBossVictoryStageExcept(EnemyController preservedBoss)
    {
        if (projectilesRoot != null)
            ClearChildren(projectilesRoot);
        if (pickupsRoot != null)
            ClearChildren(pickupsRoot);

        EnemyController[] enemies = enemiesRoot != null
            ? enemiesRoot.GetComponentsInChildren<EnemyController>(true)
            : FindObjectsOfType<EnemyController>();
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyController enemy = enemies[i];
            if (enemy == null || enemy == preservedBoss)
                continue;

            Destroy(enemy.gameObject);
        }

        WorldPopupText[] popupTexts = FindObjectsOfType<WorldPopupText>();
        for (int i = 0; i < popupTexts.Length; i++)
        {
            if (popupTexts[i] != null)
                Destroy(popupTexts[i].gameObject);
        }
    }

    private Color[] CacheSpriteColors(SpriteRenderer[] renderers)
    {
        if (renderers == null)
            return new Color[0];

        Color[] colors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            colors[i] = renderers[i] != null ? renderers[i].color : Color.white;

        return colors;
    }

    private void ApplySpriteFlash(SpriteRenderer[] renderers, Color[] originalColors, Color flashColor, float flashWeight)
    {
        if (renderers == null || originalColors == null)
            return;

        float weight = Mathf.Clamp01(flashWeight);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null)
                continue;

            sr.color = Color.Lerp(originalColors[Mathf.Min(i, originalColors.Length - 1)], flashColor, weight);
        }
    }

    private void ApplySpriteAlpha(SpriteRenderer[] renderers, float alphaMultiplier)
    {
        if (renderers == null)
            return;

        float alpha = Mathf.Clamp01(alphaMultiplier);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null)
                continue;

            Color color = sr.color;
            color.a *= alpha;
            sr.color = color;
        }
    }

    private void SpawnBossVictoryShards(SpriteRenderer[] renderers)
    {
        SpriteRenderer source = ResolveBossShardSource(renderers);
        if (source == null || source.sprite == null)
            return;

        int shardCount = Mathf.Max(1, bossVictoryShardCount);
        for (int i = 0; i < shardCount; i++)
        {
            GameObject shard = new GameObject("BossVictoryShard", typeof(SpriteRenderer));
            SpriteRenderer sr = shard.GetComponent<SpriteRenderer>();
            sr.sprite = source.sprite;
            sr.sortingLayerID = source.sortingLayerID;
            sr.sortingOrder = source.sortingOrder + 1;
            sr.color = bossVictoryShardColor;

            shard.transform.position = source.bounds.center;
            shard.transform.rotation = source.transform.rotation;
            shard.transform.localScale = source.transform.lossyScale * Random.Range(0.26f, 0.46f);

            Vector2 direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector2.up;

            StartCoroutine(BossVictoryShardRoutine(
                shard.transform,
                sr,
                direction * Random.Range(bossVictoryShardSpeed * 0.5f, bossVictoryShardSpeed),
                Random.Range(-180f, 180f)));
        }
    }

    private IEnumerator BossVictoryShardRoutine(Transform shardTransform, SpriteRenderer shardRenderer, Vector2 velocity, float angularVelocity)
    {
        if (shardTransform == null || shardRenderer == null)
            yield break;

        Vector3 startScale = shardTransform.localScale;
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, bossVictoryDissolveSeconds * 0.9f);

        while (elapsed < duration && shardTransform != null && shardRenderer != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            shardTransform.position += (Vector3)(velocity * Time.unscaledDeltaTime);
            shardTransform.Rotate(0f, 0f, angularVelocity * Time.unscaledDeltaTime);
            shardTransform.localScale = Vector3.Lerp(startScale, startScale * 0.2f, t);

            Color color = shardRenderer.color;
            color.a = Mathf.Lerp(bossVictoryShardColor.a, 0f, t);
            shardRenderer.color = color;
            yield return null;
        }

        if (shardTransform != null)
            Destroy(shardTransform.gameObject);
    }

    private SpriteRenderer ResolveBossShardSource(SpriteRenderer[] renderers)
    {
        if (renderers == null || renderers.Length == 0)
            return null;

        SpriteRenderer best = null;
        float bestArea = float.MinValue;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null || sr.sprite == null)
                continue;

            float area = sr.bounds.size.x * sr.bounds.size.y;
            if (area > bestArea)
            {
                bestArea = area;
                best = sr;
            }
        }

        return best;
    }

    private void ApplyBossVictoryCameraShake(Transform cameraTransform, Vector3 basePosition, float elapsed)
    {
        if (cameraTransform == null)
            return;

        float duration = Mathf.Max(0.01f, bossVictoryCameraShakeSeconds);
        float t = Mathf.Clamp01(elapsed / duration);
        float fade = 1f - t;
        float pulse = Mathf.Sin(elapsed * Mathf.Max(1f, bossVictoryCameraShakeFrequency));
        Vector2 noise = Random.insideUnitCircle * bossVictoryCameraShakeAmplitude * fade * (0.65f + (0.35f * Mathf.Abs(pulse)));
        cameraTransform.position = basePosition + new Vector3(noise.x, noise.y, 0f);
    }

    private IEnumerator BossVictoryScreenFlashRoutine()
    {
        if (!EnsureBossVictoryFlashOverlay())
            yield break;

        bossVictoryFlashOverlay.gameObject.SetActive(true);
        bossVictoryFlashOverlay.alpha = 0f;

        yield return FlashBossVictoryOverlay(
            bossVictoryScreenFlashPrimaryColor,
            Mathf.Max(0.01f, bossVictoryScreenFlashPrimarySeconds),
            1f);

        yield return FlashBossVictoryOverlay(
            bossVictoryScreenFlashSecondaryColor,
            Mathf.Max(0.01f, bossVictoryScreenFlashSecondarySeconds),
            bossVictoryScreenFlashSecondaryColor.a);

        bossVictoryFlashOverlay.alpha = 0f;
        bossVictoryFlashOverlay.gameObject.SetActive(false);
    }

    private IEnumerator FlashBossVictoryOverlay(Color color, float duration, float peakAlpha)
    {
        if (bossVictoryFlashOverlay == null || bossVictoryFlashImage == null)
            yield break;

        bossVictoryFlashImage.color = new Color(color.r, color.g, color.b, 1f);
        float halfDuration = Mathf.Max(0.01f, duration * 0.5f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = elapsed <= halfDuration
                ? Mathf.Lerp(0f, peakAlpha, elapsed / halfDuration)
                : Mathf.Lerp(peakAlpha, 0f, (elapsed - halfDuration) / halfDuration);
            bossVictoryFlashOverlay.alpha = Mathf.Clamp01(alpha);
            yield return null;
        }

        bossVictoryFlashOverlay.alpha = 0f;
    }

    private void HideRunSummary()
    {
        if (runSummaryPanel != null)
            runSummaryPanel.HidePanel();
    }

    // UI Button: Main Menu
    public void BackToMenu()
    {
        if (pauseMenuOpen && !pauseQuitConfirmed)
        {
            OpenPauseQuitConfirm();
            return;
        }

        bool skipClickSfx = pauseQuitConfirmed;
        pauseQuitConfirmed = false;
        if (!skipClickSfx)
            PlayUIButtonClickSfx();
        StopStoryIntroPresentation(true);
        HideRunSummary();

        // 纭繚娓告垙鏃堕棿鎭㈠
        StopRoundClearTransition(false);
        Time.timeScale = 1f;
        StopRoundTimer();
        RunLogger.Event("Back to title menu.");
        SwitchState(GameState.Title);
    }

    // UI Button: Credits
    public void OpenCredits()
    {
        if (state != GameState.Title)
            return;

        EnsureCreditsButtonsBound();

        if (!EnsureCreditsPanelBound())
        {
            if (!creditsPanelMissingWarned)
            {
                RunLogger.Warning("OpenCredits failed: Panel_Credits is not assigned and was not found in loaded scenes.");
                creditsPanelMissingWarned = true;
            }
            return;
        }

        creditsPanelMissingWarned = false;
        PlayUIButtonClickSfx();
        creditsOpen = true;
        RefreshTitleAndCreditsPanels();
        RunLogger.Event("Credits opened.");
    }

    // UI Button: Close Credits
    public void CloseCredits()
    {
        PlayUIButtonClickSfx();
        if (state != GameState.Title)
        {
            BackToMenu();
            return;
        }

        bool wasOpen = creditsOpen || (panelCredits != null && panelCredits.activeSelf);
        creditsOpen = false;
        RefreshTitleAndCreditsPanels();
        if (wasOpen)
            RunLogger.Event("Credits closed.");
    }

    // UI Button: Quit
    public void QuitGame()
    {
        if (pauseMenuOpen && !pauseQuitConfirmed)
        {
            OpenPauseQuitConfirm();
            return;
        }

        if (state == GameState.Title && !titleQuitConfirmed)
        {
            OpenTitleQuitConfirm();
            return;
        }

        bool skipClickSfx = pauseQuitConfirmed || titleQuitConfirmed;
        pauseQuitConfirmed = false;
        titleQuitConfirmed = false;
        if (!skipClickSfx)
            PlayUIButtonClickSfx();
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

        EnsurePauseMenuButtonsBound();
        PlayUIButtonClickSfx();
        pauseMenuOpen = true;
        Time.timeScale = 0f;
        SetGameplaySystemsActive(false);
        SetPauseMenuVisible(true);
        RefreshCursorVisibility();
        RunLogger.Event("Pause menu opened.");
    }

    public void ResumeFromPauseMenu()
    {
        if (!pauseMenuOpen) return;

        PlayUIButtonClickSfx();
        ForceClosePauseMenu(true);
        RunLogger.Event("Pause menu closed.");
    }

    public void OpenPauseSettings()
    {
        if (!EnsurePauseSettingsReady())
            return;

        ClosePauseQuitConfirm(false);
        PlayUIButtonClickSfx();
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
        PlayUIButtonClickSfx();

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
        RefreshCursorVisibility();
    }

    public void QuitFromPauseMenu()
    {
        OpenPauseQuitConfirm();
    }

    public void ConfirmQuitFromPauseMenu()
    {
        pauseQuitConfirmed = true;
        ClosePauseQuitConfirm(true);
        BackToMenu();
    }

    public void ConfirmExitGameFromTitle()
    {
        titleQuitConfirmed = true;
        ClosePauseQuitConfirm(true);
        QuitGame();
    }

    public void CancelQuitFromPauseMenu()
    {
        PlayUIButtonClickSfx();
        ClosePauseQuitConfirm();
    }

    private void OpenPauseQuitConfirm()
    {
        if (!pauseMenuOpen)
            return;
        EnsureQuitConfirmDialogBound();
        if (quitConfirmDialog == null)
            return;

        titleQuitConfirmed = false;
        PlayUIButtonClickSfx();
        if (quitConfirmDialog.OpenPauseQuitConfirm(ConfirmQuitFromPauseMenu, CancelQuitFromPauseMenu))
            RunLogger.Event("Pause quit confirmation opened.");
    }

    private void OpenTitleQuitConfirm()
    {
        if (state != GameState.Title)
            return;
        EnsureQuitConfirmDialogBound();
        if (quitConfirmDialog == null)
            return;

        pauseQuitConfirmed = false;
        PlayUIButtonClickSfx();
        if (quitConfirmDialog.OpenTitleExitConfirm(ConfirmExitGameFromTitle, CancelQuitFromPauseMenu))
            RunLogger.Event("Title exit confirmation opened.");
    }

    private void ClosePauseQuitConfirm(bool playClickSfx = false)
    {
        if (quitConfirmDialog == null || !quitConfirmDialog.IsOpen)
            return;

        if (playClickSfx)
            PlayUIButtonClickSfx();

        quitConfirmDialog.Close();
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

        if (playerShooter == null)
            playerShooter = FindObjectOfType<PlayerShooter>();

        PlayerHealth resolvedPlayerHealth = ResolvePlayerHealth();
        EnsurePlayerActiveItemControllerBound();
        bool purchasedShieldEffect = false;
        float nextRoundEnemyCountPercent = 0f;
        float nextRoundEnemySpeedPercent = 0f;
        float nextRoundRewardPercent = 0f;

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
                        if (resolvedPlayerHealth != null) resolvedPlayerHealth.AddMaxHealth(effect.intValue, true);
                        break;
                    case ShopItemEffectType.AddShieldCharges:
                        if (resolvedPlayerHealth != null) resolvedPlayerHealth.AddShieldCharges(effect.intValue);
                        purchasedShieldEffect = true;
                        break;
                    case ShopItemEffectType.PeriodicShieldUpgrade:
                        if (resolvedPlayerHealth != null) resolvedPlayerHealth.UpgradePeriodicShield(effect.floatValue);
                        purchasedShieldEffect = true;
                        break;
                    case ShopItemEffectType.XPPerKillAdd:
                        bonusXPPerKill += Mathf.Max(0, effect.intValue);
                        RunLogger.Event($"Bonus XP per kill +{effect.intValue}, total={bonusXPPerKill}");
                        break;
                    case ShopItemEffectType.XPGainPercentAdd:
                        xpGainPercent += Mathf.Max(0f, effect.floatValue);
                        RunLogger.Event($"XP gain bonus +{effect.floatValue:F1}%, total={xpGainPercent:F1}%");
                        break;
                    case ShopItemEffectType.XPMagnetRadiusAdd:
                        bonusXPMagnetRadius += Mathf.Max(0f, effect.floatValue);
                        RunLogger.Event($"Bonus XP magnet radius +{effect.floatValue:F1}, total={bonusXPMagnetRadius:F1}");
                        break;
                    case ShopItemEffectType.CashOnKillPercentAdd:
                        cashBonusPercent += Mathf.Max(0f, effect.floatValue);
                        RunLogger.Event($"Cash on kill bonus +{effect.floatValue:F1}%, total={cashBonusPercent:F1}%");
                        break;
                    case ShopItemEffectType.NextRoundEnemyCountPercentAdd:
                        nextRoundEnemyCountPercent += Mathf.Max(0f, effect.floatValue);
                        break;
                    case ShopItemEffectType.NextRoundEnemySpeedPercentAdd:
                        nextRoundEnemySpeedPercent += Mathf.Max(0f, effect.floatValue);
                        break;
                    case ShopItemEffectType.NextRoundRewardPercentAdd:
                        nextRoundRewardPercent += Mathf.Max(0f, effect.floatValue);
                        break;
                    case ShopItemEffectType.DamagePercentAdd:
                        if (playerShooter != null) playerShooter.AddDamagePercent(effect.floatValue);
                        break;
                    case ShopItemEffectType.FireRatePercentAdd:
                        if (playerShooter != null) playerShooter.AddFireRatePercent(effect.floatValue);
                        break;
                    case ShopItemEffectType.ProjectileSpeedPercentAdd:
                        if (playerShooter != null) playerShooter.AddProjectileSpeedPercent(effect.floatValue);
                        break;
                    case ShopItemEffectType.IFrameSecondsAdd:
                        if (resolvedPlayerHealth != null) resolvedPlayerHealth.AddInvulnerabilitySeconds(effect.floatValue);
                        break;
                    case ShopItemEffectType.ReviveChargesAdd:
                        if (resolvedPlayerHealth != null) resolvedPlayerHealth.AddReviveCharges(effect.intValue);
                        break;
                    case ShopItemEffectType.EquipActiveItem:
                        if (playerActiveItemController != null)
                            playerActiveItemController.Equip((ActiveItemId)effect.intValue);
                        break;
                }
            }
        }

        float nextRoundEnemyCountMultiplier = 1f + nextRoundEnemyCountPercent / 100f;
        float nextRoundEnemySpeedMultiplier = 1f + nextRoundEnemySpeedPercent / 100f;
        float nextRoundRewardMultiplier = 1f + nextRoundRewardPercent / 100f;

        if (nextRoundEnemyCountMultiplier > 1f || nextRoundEnemySpeedMultiplier > 1f)
            AddEnemyBuffToNextRound(1f, nextRoundEnemySpeedMultiplier, nextRoundEnemyCountMultiplier);

        if (nextRoundRewardMultiplier > 1f)
            AddRewardBuffToNextRound(nextRoundRewardMultiplier);

        if (healthUI != null)
        {
            healthUI.ResetHealthUI();
            if (purchasedShieldEffect)
                healthUI.RevealForShopFeedback(1.6f);
        }

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

    public void AddEnemyBuffToNextRound(float hpMultiplier, float speedMultiplier, float countMultiplier = 1f)
    {
        runProgression.AddEnemyBuffToNextRound(hpMultiplier, speedMultiplier, countMultiplier);
        runProgression.ClampNextRoundEnemyBuff(
            maxNextRoundEnemyHpBuffMultiplier,
            maxNextRoundEnemySpeedBuffMultiplier,
            maxNextRoundEnemyCountBuffMultiplier);
        RunLogger.Warning(
            $"Next round enemy buff queued. hpX={runProgression.NextRoundEnemyHpMultiplier:F2}, " +
            $"speedX={runProgression.NextRoundEnemySpeedMultiplier:F2}, countX={runProgression.NextRoundEnemyCountMultiplier:F2}");
        if (shopSystem != null) shopSystem.RefreshShopUI();
    }

    public void AddRewardBuffToNextRound(float rewardMultiplier)
    {
        runProgression.AddRewardBuffToNextRound(rewardMultiplier);
        RunLogger.Event($"Next round reward buff queued. rewardX={runProgression.NextRoundRewardMultiplier:F2}");
        if (shopSystem != null) shopSystem.RefreshShopUI();
    }

    public float CurrentRewardMultiplier => runProgression != null ? runProgression.CurrentRoundRewardMultiplier : 1f;

    public void GetCurrentEnemyMultipliers(out float hpMultiplier, out float speedMultiplier)
    {
        GetBaseEnemyMultipliersForRound(roundIndex, out float baseHpMultiplier, out float baseSpeedMultiplier);
        hpMultiplier = Mathf.Max(0.2f, baseHpMultiplier * runProgression.CurrentRoundEnemyHpMultiplier);
        speedMultiplier = Mathf.Max(0.2f, baseSpeedMultiplier * runProgression.CurrentRoundEnemySpeedMultiplier);
    }

    public float GetCurrentEnemyCountMultiplier()
    {
        return runProgression != null ? Mathf.Max(1f, runProgression.CurrentRoundEnemyCountMultiplier) : 1f;
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

        shopSystem.Bind(this);
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
        panelSettingsPlaceholder.SetActive(false);
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

    private void EnsureQuitConfirmDialogBound()
    {
        if (quitConfirmDialog == null)
            quitConfirmDialog = GetComponent<QuitConfirmDialog>();

        if (quitConfirmDialog == null)
            quitConfirmDialog = gameObject.AddComponent<QuitConfirmDialog>();

        if (quitConfirmDialog != null)
            quitConfirmDialog.Bind(panelPauseMenu);
    }

    private void EnsureRoundPresentationBound()
    {
        if (roundPresentation == null)
            roundPresentation = GetComponent<RoundPresentationController>();

        if (roundPresentation == null)
            roundPresentation = gameObject.AddComponent<RoundPresentationController>();

        if (roundPresentation == null)
            return;

        SyncRoundPresentationConfig();
    }

    private void EnsureBossHealthBarBound()
    {
        if (bossHealthBar == null)
            bossHealthBar = GetComponent<BossHealthBarController>();

        if (bossHealthBar == null)
            bossHealthBar = gameObject.AddComponent<BossHealthBarController>();

        if (bossHealthBar != null)
            bossHealthBar.BindCanvasSource(panelHUD);
    }

    private void EnsurePlayerActiveItemControllerBound()
    {
        if (playerActiveItemController == null)
            playerActiveItemController = GetComponent<PlayerActiveItemController>();

        if (playerActiveItemController == null)
            playerActiveItemController = gameObject.AddComponent<PlayerActiveItemController>();

        if (playerActiveItemController == null)
            return;

        playerActiveItemController.Bind(
            this,
            panelHUD,
            playerMotor,
            playerShooter,
            ResolvePlayerHealth());
    }

    private void SyncRoundPresentationConfig()
    {
        if (roundPresentation == null)
            return;

        roundPresentation.BindCanvasSource(panelHUD);
        roundPresentation.TryImportLegacyConfig(
            roundIntroSeconds,
            roundIntroOverlay,
            roundIntroRoundText,
            roundIntroDebtText,
            roundIntroContinueHintText,
            roundIntroRequireAnyKeyToContinue,
            roundIntroContinueHintMessage,
            roundIntroFadeInSeconds,
            roundIntroFadeOutSeconds,
            roundClearOverlay,
            roundClearTitleText,
            roundClearSubText,
            showRoundClearTransition,
            roundClearTitleMessage,
            roundClearSubMessage,
            roundClearSeconds,
            roundClearFadeInSeconds,
            roundClearFadeOutSeconds,
            roundClearOverlayAlphaDuringCollect,
            timesUpOverlay,
            timesUpTitleText,
            showTimesUpTransition,
            timesUpTitleMessage,
            timesUpSeconds,
            timesUpFadeInSeconds,
            timesUpFadeOutSeconds,
            gameOverTransitionOverlay,
            gameOverTransitionTitleText,
            gameOverTransitionSubText,
            showGameOverTransition,
            gameOverTransitionTitleMessage,
            gameOverTransitionSubMessage,
            gameOverTransitionSeconds,
            gameOverTransitionFadeInSeconds,
            gameOverTransitionFadeOutSeconds);
    }

    private bool TryPlayStoryIntroBeforeRun()
    {
        if (!enableStoryComicIntro || state != GameState.Title)
            return false;

        if (storyIntroActive)
            return true;

        List<StoryComicPage> validPages = GetValidStoryComicPages();
        if (validPages.Count <= 0)
            return false;

        if (!EnsureStoryIntroOverlayBound())
        {
            RunLogger.Warning("Story intro overlay missing. Start run directly.");
            return false;
        }

        bool allowSkip = IsStoryIntroAlreadyPlayed() || !lockStorySkipOnFirstPlay;
        StartStoryIntroPresentation(validPages, allowSkip);
        return true;
    }

    private void StartStoryIntroPresentation(List<StoryComicPage> pages, bool allowSkip)
    {
        StopStoryIntroPresentation(true);

        startRunQueuedAfterStory = true;
        storyIntroActive = true;
        storySkipRequested = false;
        storyAdvanceRequested = false;

        if (storyIntroOverlay != null)
        {
            storyIntroOverlay.alpha = 1f;
            storyIntroOverlay.gameObject.SetActive(true);
            storyIntroOverlay.transform.SetAsLastSibling();
            storyIntroOverlay.interactable = true;
            storyIntroOverlay.blocksRaycasts = true;
        }

        if (storyIntroSkipButtonText != null)
            storyIntroSkipButtonText.text = string.IsNullOrWhiteSpace(storyIntroSkipButtonLabel) ? "Skip Story" : storyIntroSkipButtonLabel;

        if (storyIntroSkipButton != null)
        {
            storyIntroSkipButton.onClick.RemoveListener(OnStorySkipButtonClicked);
            storyIntroSkipButton.gameObject.SetActive(allowSkip);
            if (allowSkip)
                storyIntroSkipButton.onClick.AddListener(OnStorySkipButtonClicked);
        }

        if (BGMManager.Instance != null)
            BGMManager.Instance.PlayStoryIntroBGM();

        RefreshTitleAndCreditsPanels();
        storyIntroCo = StartCoroutine(PlayStoryIntroRoutine(pages, allowSkip));
        RunLogger.Event($"Story intro started. pages={pages.Count}, allowSkip={allowSkip}");
    }

    private void StopStoryIntroPresentation(bool clearQueuedRun)
    {
        bool wasActive = storyIntroActive;

        if (storyIntroCo != null)
        {
            StopCoroutine(storyIntroCo);
            storyIntroCo = null;
        }

        storyIntroActive = false;
        storySkipRequested = false;
        storyAdvanceRequested = false;
        if (clearQueuedRun)
            startRunQueuedAfterStory = false;

        if (storyIntroSkipButton != null)
            storyIntroSkipButton.onClick.RemoveListener(OnStorySkipButtonClicked);

        if (storyIntroOverlay != null)
        {
            storyIntroOverlay.interactable = false;
            storyIntroOverlay.blocksRaycasts = false;
            storyIntroOverlay.gameObject.SetActive(false);
        }

        if (wasActive && BGMManager.Instance != null)
            BGMManager.Instance.OnGameStateChanged(state);

        RefreshTitleAndCreditsPanels();
    }

    private IEnumerator PlayStoryIntroRoutine(List<StoryComicPage> pages, bool allowSkip)
    {
        int totalSteps = pages.Count; // 1 click per page.
        int currentStep = 0;
        yield return null; // Avoid consuming the same click used to start run.

        for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
        {
            StoryComicPage page = pages[pageIndex];
            int pageSteps = 1; // Temporarily override — single click to advance.

            for (int step = 1; step <= pageSteps; step++)
            {
                currentStep += 1;
                ApplyStoryComicVisual(page, pageIndex + 1, pages.Count, step, pageSteps, currentStep, totalSteps, allowSkip);

                storyAdvanceRequested = false;
                while (!storyAdvanceRequested && !storySkipRequested)
                    yield return null;

                if (storySkipRequested && allowSkip)
                {
                    MarkStoryIntroPlayed();
                    FinishStoryIntroAndStartRun();
                    yield break;
                }

                storyAdvanceRequested = false;
            }
        }

        MarkStoryIntroPlayed();
        FinishStoryIntroAndStartRun();
    }

    private void FinishStoryIntroAndStartRun()
    {
        bool shouldStart = startRunQueuedAfterStory;
        StopStoryIntroPresentation(true);
        if (shouldStart)
            StartRunGameplayFlow();
    }

    private void ApplyStoryComicVisual(
        StoryComicPage page,
        int pageIndex,
        int pageCount,
        int step,
        int stepCount,
        int currentStep,
        int totalSteps,
        bool allowSkip)
    {
        if (storyIntroPageImage != null)
        {
            storyIntroPageImage.sprite = page.PageSprite;
            storyIntroPageImage.preserveAspect = true;

            float reveal = stepCount <= 1 ? 1f : Mathf.Lerp(0.35f, 1f, step / (float)stepCount);
            Color c = storyIntroPageImage.color;
            c.a = reveal;
            storyIntroPageImage.color = c;
        }

        if (storyIntroHintText != null)
        {
            string template = string.IsNullOrWhiteSpace(storyIntroHintTemplate)
                ? "Click to continue"
                : storyIntroHintTemplate;

            // Keep hint clean by default: no panel counter and no skip-key prompt.
            if (template.Contains("{0}") || template.Contains("{1}"))
                storyIntroHintText.text = string.Format(template, pageIndex, pageCount);
            else
                storyIntroHintText.text = template;
        }
    }

    private void HandleStoryIntroInput()
    {
        if (!storyIntroActive)
            return;

        bool allowSkip = storyIntroSkipButton != null && storyIntroSkipButton.gameObject.activeInHierarchy;
        if (allowSkip && (GameInput.IsBackPressed() || GameInput.IsPausePressed()))
        {
            storySkipRequested = true;
            return;
        }

        if (IsStoryAdvanceInputDown())
            storyAdvanceRequested = true;
    }

    private bool IsStoryAdvanceInputDown()
    {
        if (Input.GetMouseButtonDown(0))
            return true;
        if (GameInput.IsContinuePressed())
            return true;
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
                return true;
        }

        return false;
    }

    private void OnStorySkipButtonClicked()
    {
        if (!storyIntroActive)
            return;

        storySkipRequested = true;
    }

    private List<StoryComicPage> GetValidStoryComicPages()
    {
        List<StoryComicPage> validPages = new List<StoryComicPage>();
        if (storyComicPages == null || storyComicPages.Count <= 0)
            return validPages;

        for (int i = 0; i < storyComicPages.Count; i++)
        {
            StoryComicPage page = storyComicPages[i];
            if (page == null || page.PageSprite == null)
                continue;

            validPages.Add(page);
        }

        return validPages;
    }

    private bool IsStoryIntroAlreadyPlayed()
    {
        if (string.IsNullOrWhiteSpace(storyPlayedFlagKey))
            return false;
        return PlayerPrefs.GetInt(storyPlayedFlagKey, 0) == 1;
    }

    private void MarkStoryIntroPlayed()
    {
        if (string.IsNullOrWhiteSpace(storyPlayedFlagKey))
            return;

        PlayerPrefs.SetInt(storyPlayedFlagKey, 1);
        PlayerPrefs.Save();
    }

    private void TryBindStoryIntroOverlayChildren()
    {
        if (storyIntroOverlay == null)
            return;

        Transform root = storyIntroOverlay.transform;

        if (storyIntroBackground == null)
        {
            Transform bg = root.Find("Background");
            if (bg != null)
                storyIntroBackground = bg.GetComponent<Image>();
        }

        if (storyIntroPageImage == null)
        {
            Transform page = root.Find("PageImage");
            if (page != null)
                storyIntroPageImage = page.GetComponent<Image>();
        }

        if (storyIntroHintText == null)
        {
            Transform hint = root.Find("StoryHintText");
            if (hint != null)
                storyIntroHintText = hint.GetComponent<TMP_Text>();
        }

        if (storyIntroSkipButton == null)
        {
            Transform skip = root.Find("Btn_StorySkip");
            if (skip != null)
                storyIntroSkipButton = skip.GetComponent<Button>();
        }

        if (storyIntroSkipButtonText == null && storyIntroSkipButton != null)
        {
            Transform textTf = storyIntroSkipButton.transform.Find("Text");
            if (textTf != null)
                storyIntroSkipButtonText = textTf.GetComponent<TMP_Text>();
        }
    }

    private bool EnsureStoryIntroOverlayBound()
    {
        TryBindStoryIntroOverlayChildren();
        if (storyIntroOverlay != null && storyIntroPageImage != null && storyIntroHintText != null)
            return true;

        if (storyIntroOverlay != null)
        {
            RunLogger.Warning("Story intro overlay is assigned but missing required child refs. Please bind PageImage and StoryHintText.");
            return false;
        }

        Canvas canvas = panelTitle != null ? panelTitle.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
            return false;

        GameObject overlayRoot = new GameObject("StoryIntroOverlayAuto", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.SetParent(canvas.transform, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        storyIntroOverlay = overlayRoot.GetComponent<CanvasGroup>();
        storyIntroOverlay.alpha = 0f;
        storyIntroOverlay.interactable = false;
        storyIntroOverlay.blocksRaycasts = false;

        GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
        RectTransform bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.SetParent(overlayRect, false);
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        storyIntroBackground = bgGo.GetComponent<Image>();
        storyIntroBackground.color = new Color(0f, 0f, 0f, 0.9f);

        GameObject pageGo = new GameObject("PageImage", typeof(RectTransform), typeof(Image));
        RectTransform pageRect = pageGo.GetComponent<RectTransform>();
        pageRect.SetParent(overlayRect, false);
        pageRect.anchorMin = Vector2.zero;
        pageRect.anchorMax = Vector2.one;
        pageRect.offsetMin = new Vector2(32f, 128f);
        pageRect.offsetMax = new Vector2(-32f, -96f);
        storyIntroPageImage = pageGo.GetComponent<Image>();
        storyIntroPageImage.preserveAspect = true;
        storyIntroPageImage.color = Color.white;

        storyIntroHintText = CreateIntroText(
            overlayRect,
            "StoryHintText",
            new Vector2(0f, -492f),
            new Vector2(1400f, 120f),
            34f,
            Color.white);
        storyIntroHintText.fontStyle = FontStyles.Normal;
        storyIntroHintText.enableWordWrapping = true;

        GameObject skipBtnGo = new GameObject("Btn_StorySkip", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform skipBtnRect = skipBtnGo.GetComponent<RectTransform>();
        skipBtnRect.SetParent(overlayRect, false);
        skipBtnRect.anchorMin = new Vector2(1f, 1f);
        skipBtnRect.anchorMax = new Vector2(1f, 1f);
        skipBtnRect.pivot = new Vector2(1f, 1f);
        skipBtnRect.sizeDelta = new Vector2(220f, 66f);
        skipBtnRect.anchoredPosition = new Vector2(-44f, -38f);

        Image skipBtnImage = skipBtnGo.GetComponent<Image>();
        skipBtnImage.color = new Color(0f, 0f, 0f, 0.72f);
        storyIntroSkipButton = skipBtnGo.GetComponent<Button>();
        storyIntroSkipButton.targetGraphic = skipBtnImage;

        storyIntroSkipButtonText = CreateIntroText(
            skipBtnRect,
            "Text",
            Vector2.zero,
            new Vector2(220f, 66f),
            30f,
            Color.white);
        storyIntroSkipButtonText.fontStyle = FontStyles.Normal;
        storyIntroSkipButtonText.text = string.IsNullOrWhiteSpace(storyIntroSkipButtonLabel) ? "Skip Story" : storyIntroSkipButtonLabel;

        storyIntroOverlay.gameObject.SetActive(false);
        return true;
    }

    // ====== Internal ======

private void SwitchState(GameState next)
{
    if (next != GameState.Title && storyIntroActive)
        StopStoryIntroPresentation(false);

    ForceClosePauseMenu(true);

    GameState previous = state;
    state = next;
    if (previous != next)
        RunLogger.Event($"State {previous} -> {next}");

    if (previous == GameState.Shop && next != GameState.Shop && shopSystem != null)
        shopSystem.OnShopClosed();

    if (state != GameState.Title)
        creditsOpen = false;
    if (state != GameState.Victory)
        victoryCreditsVisible = false;

    RefreshTitleAndCreditsPanels();
    RefreshGameplayHudVisibility();
    if (state != GameState.Gameplay)
    {
        StopGameplayTutorialSequence();
        ClearGameplayTutorialHint();
    }
    if (levelUpPanel != null)
        levelUpPanel.ForceHideImmediate();
    else if (panelLevelUp != null)
        panelLevelUp.SetActive(false);
    if (panelSettlement) panelSettlement.SetActive(state == GameState.Settlement);
    if (panelShop) panelShop.SetActive(state == GameState.Shop);
    if (healthUI != null) healthUI.SetHiddenForShop(state == GameState.Shop);
    
    // 绂诲紑GameOver鐘舵€佹椂闅愯棌姝讳骸闈㈡澘
    if (previous == GameState.GameOver)
        HideAllDeathPanels();

    // 绂诲紑Victory鐘舵€佹椂闅愯棌鑳滃埄闈㈡澘
    if (previous == GameState.Victory && victoryPanel != null)
    {
        victoryPanel.HideVictoryPanel();
        Time.timeScale = 1f; // 鎭㈠鏃堕棿娴?
    }
    if (previous == GameState.Victory)
    {
        if (victoryPresentationCo != null)
        {
            StopCoroutine(victoryPresentationCo);
            victoryPresentationCo = null;
        }

        HideVictoryThanksOverlayImmediate();
    }

    bool inGameplay = (state == GameState.Gameplay);

    if (!inGameplay)
    {
        StopRoundIntro();
        StopRoundClearTransition(false);
        bossSeenInCurrentBossRound = false;
        trackedBossRoundIndex = -1;
        nextBossDefeatCheckTime = -10f;
    }

    bool gameplaySystemsActive = inGameplay && !IsRoundIntroActive && !roundClearActive;
    SetGameplaySystemsActive(gameplaySystemsActive);

    // Leave gameplay -> clean world objects (settlement/shop/gameover/title should be clean).
    if (!inGameplay)
        ClearWorld();

    if (BGMManager.Instance != null)
        BGMManager.Instance.OnGameStateChanged(state);

    BindHoverSfxToSceneButtons();
    BindKeyboardFocusIndicatorsToSceneSelectables();
    RefreshCursorVisibility();
}

    private void BeginGameplayTutorialSequence()
    {
        if (!enableGameplayTutorial || !isActiveAndEnabled || gameplayTutorialStage != GameplayTutorialStage.WaitingForMove)
            return;

        StopGameplayTutorialSequence();
        gameplayTutorialCo = StartCoroutine(GameplayTutorialSequence());
    }

    private void StopGameplayTutorialSequence()
    {
        if (gameplayTutorialCo == null)
        {
            if (gameplayTutorialDebtFollowupCo == null)
                return;
        }

        if (gameplayTutorialCo != null)
        {
            StopCoroutine(gameplayTutorialCo);
            gameplayTutorialCo = null;
        }

        if (gameplayTutorialDebtFollowupCo != null)
        {
            StopCoroutine(gameplayTutorialDebtFollowupCo);
            gameplayTutorialDebtFollowupCo = null;
        }
    }

    private IEnumerator GameplayTutorialSequence()
    {
        while (state == GameState.Gameplay && (IsRoundIntroActive || roundClearActive || storyIntroActive))
            yield return null;

        gameplayTutorialCo = null;

        if (state != GameState.Gameplay || gameplayTutorialStage != GameplayTutorialStage.WaitingForMove || !enableGameplayTutorial)
            yield break;

        ShowGameplayTutorialHint(
            gameplayTutorialMoveText,
            gameplayTutorialSpawnPosition + gameplayTutorialMoveOffset,
            gameplayTutorialMoveLifetime,
            GameplayTutorialHintType.Move);
    }

    private IEnumerator ShowDebtTutorialAfterDelay(Vector3 worldPosition, float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, delay));
        gameplayTutorialDebtFollowupCo = null;

        if (state != GameState.Gameplay || !enableGameplayTutorial)
            yield break;

        ShowGameplayTutorialHint(
            gameplayTutorialDebtText,
            worldPosition,
            gameplayTutorialPickupLifetime,
            GameplayTutorialHintType.Debt);
    }

    private void ShowGameplayTutorialHint(string content, Vector3 worldPosition, float lifetime, GameplayTutorialHintType hintType)
    {
        ClearGameplayTutorialHint();

        activeGameplayTutorialHint = WorldInstructionText.Spawn(
            content,
            worldPosition,
            gameplayTutorialTextColor,
            gameplayTutorialFont,
            gameplayTutorialFontSize,
            gameplayTutorialTextScale,
            lifetime);
        activeGameplayTutorialHintType = activeGameplayTutorialHint != null ? hintType : GameplayTutorialHintType.None;
    }

    private void ClearGameplayTutorialHint()
    {
        if (activeGameplayTutorialHint == null)
        {
            activeGameplayTutorialHintType = GameplayTutorialHintType.None;
            return;
        }

        Destroy(activeGameplayTutorialHint.gameObject);
        activeGameplayTutorialHint = null;
        activeGameplayTutorialHintType = GameplayTutorialHintType.None;
    }

    private void UpdateGameplayTutorialProgress()
    {
        if (!enableGameplayTutorial || state != GameState.Gameplay)
            return;

        if (gameplayTutorialStage == GameplayTutorialStage.WaitingForMove)
        {
            if (playerMotor == null || playerMotor.CurrentMoveInput.sqrMagnitude <= 0.001f)
                return;

            // The move hint is only feedback. Its auto-expiry must not block tutorial progress.
            if (activeGameplayTutorialHint != null &&
                activeGameplayTutorialHintType == GameplayTutorialHintType.Move &&
                !activeGameplayTutorialHint.IsCompleting)
            {
                TryCompleteGameplayTutorialHint(GameplayTutorialHintType.Move);
            }

            AdvanceGameplayTutorialAfterMovement();
            return;
        }

        if (gameplayTutorialStage != GameplayTutorialStage.WaitingForActiveItem)
            return;

        EnsurePlayerActiveItemControllerBound();
        if (playerActiveItemController == null || !playerActiveItemController.IsStarterDashEquipped)
        {
            AdvanceGameplayTutorialAfterActiveItemUse();
            return;
        }

        if (playerActiveItemController.TotalUses <= gameplayTutorialObservedActiveItemUseCount)
            return;

        if (activeGameplayTutorialHint != null &&
            activeGameplayTutorialHintType == GameplayTutorialHintType.ActiveItem &&
            !activeGameplayTutorialHint.IsCompleting)
        {
            TryCompleteGameplayTutorialHint(GameplayTutorialHintType.ActiveItem);
        }

        AdvanceGameplayTutorialAfterActiveItemUse();
    }

    private bool TryCompleteGameplayTutorialHint(GameplayTutorialHintType expectedType)
    {
        if (activeGameplayTutorialHint == null || activeGameplayTutorialHintType != expectedType)
            return false;

        if (activeGameplayTutorialHint.IsCompleting)
            return false;

        activeGameplayTutorialHint.Complete(
            gameplayTutorialCompleteColor,
            gameplayTutorialCompleteFadeDuration);
        return true;
    }

    private Vector3 ResolveGameplayTutorialAnchorPosition()
    {
        if (playerMotor != null)
            return playerMotor.transform.position;
        if (playerShooter != null)
            return playerShooter.transform.position;

        PlayerHealth resolvedPlayerHealth = ResolvePlayerHealth();
        if (resolvedPlayerHealth != null)
            return resolvedPlayerHealth.transform.position;

        return Vector3.zero;
    }

    private void AdvanceGameplayTutorialAfterMovement()
    {
        if (gameplayTutorialStage != GameplayTutorialStage.WaitingForMove)
            return;

        EnsurePlayerActiveItemControllerBound();
        if (playerActiveItemController != null &&
            playerActiveItemController.IsStarterDashEquipped &&
            !string.IsNullOrWhiteSpace(gameplayTutorialDashText))
        {
            gameplayTutorialStage = GameplayTutorialStage.WaitingForActiveItem;
            gameplayTutorialObservedActiveItemUseCount = playerActiveItemController.TotalUses;
            gameplayTutorialSpawnPosition = ResolveGameplayTutorialAnchorPosition();
            ShowGameplayTutorialHint(
                gameplayTutorialDashText,
                gameplayTutorialSpawnPosition + gameplayTutorialMoveOffset,
                gameplayTutorialDashLifetime,
                GameplayTutorialHintType.ActiveItem);
            return;
        }

        AdvanceGameplayTutorialAfterActiveItemUse();
    }

    private void AdvanceGameplayTutorialAfterActiveItemUse()
    {
        if (gameplayTutorialStage != GameplayTutorialStage.WaitingForMove &&
            gameplayTutorialStage != GameplayTutorialStage.WaitingForActiveItem)
        {
            return;
        }

        gameplayTutorialStage = GameplayTutorialStage.WaitingForFirstKill;
        gameplayTutorialSpawnPosition = ResolveGameplayTutorialAnchorPosition();

        if (enemySpawner == null || enemySpawner.SpawnTutorialEnemy() == null)
        {
            RunLogger.Warning("Gameplay tutorial enemy spawn failed. Starting formal gameplay to avoid soft-lock.");
            ForceStartFormalGameplayWithoutTutorialBlock();
            return;
        }

        RefreshGameplaySystemsForCurrentState();
    }

    private void StartFormalGameplayAfterTutorial()
    {
        if (state != GameState.Gameplay || gameplayTutorialStage != GameplayTutorialStage.Complete)
            return;

        RefreshGameplaySystemsForCurrentState();

        if (!IsCurrentRoundBoss() && roundTimerCo == null)
            StartRoundTimer();

        RunLogger.Event("Opening gameplay tutorial complete. Formal gameplay started.");
    }

    private void ForceStartFormalGameplayWithoutTutorialBlock()
    {
        gameplayTutorialStage = GameplayTutorialStage.Complete;
        gameplayTutorialFirstKillShown = true;
        gameplayTutorialFirstPickupShown = true;
        ClearGameplayTutorialHint();
        StopGameplayTutorialSequence();
        StartFormalGameplayAfterTutorial();
    }

    private bool IsGameplayTutorialBlockingRoundStart()
    {
        if (!enableGameplayTutorial || state != GameState.Gameplay)
            return false;

        return gameplayTutorialStage == GameplayTutorialStage.WaitingForMove ||
               gameplayTutorialStage == GameplayTutorialStage.WaitingForActiveItem ||
               gameplayTutorialStage == GameplayTutorialStage.WaitingForFirstKill ||
               gameplayTutorialStage == GameplayTutorialStage.WaitingForFirstPickup;
    }

    private void RefreshGameplaySystemsForCurrentState()
    {
        bool gameplaySystemsActive = state == GameState.Gameplay && !IsRoundIntroActive && !roundClearActive;
        SetGameplaySystemsActive(gameplaySystemsActive);
    }

private void RefreshTitleAndCreditsPanels()
{
    EnsureCreditsButtonsBound();

    bool hasCreditsPanel = EnsureCreditsPanelBound();
    bool inTitle = state == GameState.Title;
    if (inTitle && creditsOpen && !hasCreditsPanel)
    {
        if (!creditsPanelMissingWarned)
        {
            RunLogger.Warning("Credits requested but Panel_Credits is missing. Keeping title panel visible.");
            creditsPanelMissingWarned = true;
        }
        creditsOpen = false;
    }
    else if (hasCreditsPanel)
    {
        creditsPanelMissingWarned = false;
    }

    bool showCredits = ((inTitle && creditsOpen) || (state == GameState.Victory && victoryCreditsVisible)) && hasCreditsPanel;
    bool showTitle = inTitle && !showCredits && !storyIntroActive;

    if (panelTitle != null)
        panelTitle.SetActive(showTitle);
    if (hasCreditsPanel && panelCredits != null)
        panelCredits.SetActive(showCredits);
}

private bool EnsureVictoryThanksOverlay()
{
    if (victoryThanksOverlay != null && victoryThanksTitleText != null && victoryThanksSubtitleText != null)
        return true;

    if (victoryThanksOverlayAutoCreated)
        return false;

    Canvas parentCanvas = GetComponentInParent<Canvas>();
    if (parentCanvas == null)
        parentCanvas = FindObjectOfType<Canvas>();
    if (parentCanvas == null)
        return false;

    GameObject root = new GameObject("VictoryThanksOverlayAuto", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
    root.transform.SetParent(parentCanvas.transform, false);

    RectTransform rootRect = root.GetComponent<RectTransform>();
    rootRect.anchorMin = Vector2.zero;
    rootRect.anchorMax = Vector2.one;
    rootRect.offsetMin = Vector2.zero;
    rootRect.offsetMax = Vector2.zero;

    Image backdrop = root.GetComponent<Image>();
    backdrop.color = victoryPresentationBackdropColor;
    backdrop.raycastTarget = false;

    victoryThanksOverlay = root.GetComponent<CanvasGroup>();
    victoryThanksOverlay.alpha = 0f;
    victoryThanksOverlay.interactable = false;
    victoryThanksOverlay.blocksRaycasts = false;

    victoryThanksTitleText = CreateVictoryThanksText(root.transform, "ThanksTitle", 88f, FontStyles.UpperCase, 0f, 44f);
    victoryThanksSubtitleText = CreateVictoryThanksText(root.transform, "ThanksSubtitle", 34f, FontStyles.Normal, 0f, -34f);

    root.SetActive(false);
    victoryThanksOverlayAutoCreated = true;
    return true;
}

private TMP_Text CreateVictoryThanksText(Transform parent, string name, float fontSize, FontStyles fontStyle, float x, float y)
{
    GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
    textObj.transform.SetParent(parent, false);

    RectTransform rect = textObj.GetComponent<RectTransform>();
    rect.anchorMin = new Vector2(0.5f, 0.5f);
    rect.anchorMax = new Vector2(0.5f, 0.5f);
    rect.pivot = new Vector2(0.5f, 0.5f);
    rect.anchoredPosition = new Vector2(x, y);
    rect.sizeDelta = new Vector2(1400f, 160f);

    TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
    text.alignment = TextAlignmentOptions.Center;
    text.fontSize = fontSize;
    text.fontStyle = fontStyle;
    text.color = victoryThanksTextColor;
    text.raycastTarget = false;
    text.enableWordWrapping = false;
    return text;
}

private bool EnsureCreditsPanelBound()
{
    if (panelCredits != null)
        return true;

    GameObject found = GameObject.Find("Panel_Credits");
    if (found == null)
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null)
                continue;
            if (!go.scene.IsValid() || !go.scene.isLoaded)
                continue;
            if (go.name == "Panel_Credits")
            {
                found = go;
                break;
            }
        }
    }

    if (found == null)
        return false;

    panelCredits = found;
    if (!creditsOpen)
        panelCredits.SetActive(false);

    RunLogger.Event("Panel_Credits auto-bound to GameFlowController.");
    return true;
}

private void EnsureCreditsButtonsBound()
{
    BindButtonOnPanel(panelTitle, OpenCredits, "Btn_Credit", "Btn_Credits", "Btn_CreditsOpen");

    if (!EnsureCreditsPanelBound())
        return;

    BindButtonOnPanel(panelCredits, CloseCredits, "Btn_Back", "Btn_CreditsBack", "Btn_CloseCredits");
}

private void EnsurePauseMenuButtonsBound()
{
    if (panelPauseMenu == null)
        return;

    RebindButtonOnPanel(panelPauseMenu, ResumeFromPauseMenu, new[] { "Btn_BackToGame", "Btn_Resume" }, ResumeFromPauseMenu);
    RebindButtonOnPanel(panelPauseMenu, OpenPauseSettings, new[] { "Btn_Settings", "Btn_Option", "Btn_Options" }, OpenPauseSettings);
    RebindButtonOnPanel(panelPauseMenu, QuitFromPauseMenu, new[] { "Btn_Quit", "Btn_BackToMenu", "Btn_ReturnToTitle" }, BackToMenu, QuitGame, QuitFromPauseMenu);
}

private void BindButtonOnPanel(GameObject panel, UnityAction action, params string[] buttonNames)
{
    if (panel == null || action == null || buttonNames == null || buttonNames.Length == 0)
        return;

    Button[] buttons = panel.GetComponentsInChildren<Button>(true);
    for (int i = 0; i < buttons.Length; i++)
    {
        Button button = buttons[i];
        if (button == null)
            continue;

        bool nameMatch = false;
        for (int j = 0; j < buttonNames.Length; j++)
        {
            string candidate = buttonNames[j];
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            if (button.name == candidate)
            {
                nameMatch = true;
                break;
            }
        }

        if (!nameMatch)
            continue;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }
}

private void RebindButtonOnPanel(GameObject panel, UnityAction action, string[] buttonNames, params UnityAction[] actionsToRemove)
{
    if (panel == null || action == null || buttonNames == null || buttonNames.Length == 0)
        return;

    Button[] buttons = panel.GetComponentsInChildren<Button>(true);
    for (int i = 0; i < buttons.Length; i++)
    {
        Button button = buttons[i];
        if (button == null)
            continue;

        bool nameMatch = false;
        for (int j = 0; j < buttonNames.Length; j++)
        {
            string candidate = buttonNames[j];
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            if (button.name == candidate)
            {
                nameMatch = true;
                break;
            }
        }

        if (!nameMatch)
            continue;

        if (actionsToRemove != null)
        {
            for (int j = 0; j < actionsToRemove.Length; j++)
            {
                UnityAction removeAction = actionsToRemove[j];
                if (removeAction != null)
                    button.onClick.RemoveListener(removeAction);
            }
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }
}

private void TryPlayBossRoundBGM()
{
    if (!IsCurrentRoundBoss())
        return;

    if (BGMManager.Instance != null)
        BGMManager.Instance.PlayBossBGM();
}

private void PlayLevelUpPanelBGM()
{
    if (BGMManager.Instance == null)
        return;

    BGMManager.Instance.PlayLevelUpBGM();
}

private void RefreshBGMForCurrentGameplayContext()
{
    if (BGMManager.Instance == null)
        return;

    if (state != GameState.Gameplay)
    {
        BGMManager.Instance.OnGameStateChanged(state);
        return;
    }

    if (IsLevelUpPanelOpen())
    {
        BGMManager.Instance.PlayLevelUpBGM();
        return;
    }

    if (IsCurrentRoundBoss())
        BGMManager.Instance.PlayBossBGM();
    else
        BGMManager.Instance.OnGameStateChanged(GameState.Gameplay);
}

private void ClearWorld()
{
    int enemyCount = CountChildren(enemiesRoot);
    int projectileCount = CountChildren(projectilesRoot);
    int pickupCount = CountChildren(pickupsRoot);

    ClearChildren(enemiesRoot);
    ClearChildren(projectilesRoot);
    ClearChildren(pickupsRoot);
    int rogueProjectileCount = Projectile.DespawnAllActiveInScene();

    if (enemyCount > 0 || projectileCount > 0 || pickupCount > 0 || rogueProjectileCount > 0)
    {
        RunLogger.Event(
            $"World cleared: enemies={enemyCount}, projectiles={projectileCount}, rogueProjectiles={rogueProjectileCount}, pickups={pickupCount}");
    }
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
    int rogueProjectileCount = Projectile.DespawnAllActiveInScene();

    WorldPopupText[] popupTexts = FindObjectsOfType<WorldPopupText>();
    int popupCount = 0;
    for (int i = 0; i < popupTexts.Length; i++)
    {
        if (popupTexts[i] == null) continue;
        popupTexts[i].gameObject.SetActive(false);
        Destroy(popupTexts[i].gameObject);
        popupCount++;
    }

    if (projectileCount > 0 || rogueProjectileCount > 0 || popupCount > 0)
    {
        RunLogger.Event(
            $"Round end transient cleanup: projectiles={projectileCount}, rogueProjectiles={rogueProjectileCount}, popups={popupCount}");
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
    {
        GameObject child = root.GetChild(i).gameObject;
        child.SetActive(false);
        Destroy(child);
    }
}

private int CountChildren(Transform root)
{
    return root == null ? 0 : root.childCount;
}

private void SetGameplaySystemsActive(bool active)
{
    bool allowRegularEnemySpawns = active && !IsGameplayTutorialBlockingRoundStart();
    if (enemySpawner != null) enemySpawner.enabled = allowRegularEnemySpawns;
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

    if (state == GameState.Gameplay && (IsRoundIntroActive || roundClearActive))
        return false;

    return Time.timeScale > 0.01f;
}

private void ForceClosePauseMenu(bool resumeGameplayIfNeeded)
{
    pauseMenuOpen = false;
    pauseQuitConfirmed = false;
    titleQuitConfirmed = false;
    settingsReturnTarget = SettingsReturnTarget.None;
    SetPauseMenuVisible(false);

    if (resumeGameplayIfNeeded)
    {
        if (state == GameState.Gameplay && !IsRoundIntroActive && !roundClearActive)
        {
            Time.timeScale = 1f;
            SetGameplaySystemsActive(true);
        }
        else if (state == GameState.Shop)
        {
            Time.timeScale = 1f;
        }
    }

    RefreshCursorVisibility();
}

private void SetPauseMenuVisible(bool visible)
{
    if (panelPauseMenu != null)
        panelPauseMenu.SetActive(visible);

    if (!visible)
    {
        ClosePauseQuitConfirm(false);

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
            roundTimeRemaining = t;  // 瀹炴椂鏇存柊鍓╀綑鏃堕棿
            yield return null;
        }

        if (state == GameState.Gameplay)
        {
            roundTimeRemaining = 0f;
            RunLogger.Event($"Round {roundIndex} timer reached 0.");
            roundTimerCo = null;
            bool timesUpStarted = false;
            EnsureRoundPresentationBound();
            if (roundPresentation == null || roundPresentation.ShowTimesUpTransition)
            {
                if (roundPresentation != null)
                {
                    timesUpStarted = roundPresentation.PlayTimesUp(
                        () =>
                        {
                            Time.timeScale = 0f;
                            SetGameplaySystemsActive(false);
                        },
                        StartTimerEndResolution);
                }
            }

            if (!timesUpStarted)
                StartTimerEndResolution();
        }
    }

    private void StopTimesUpTransition(bool restoreTimeScale)
    {
        if (roundPresentation != null)
            roundPresentation.StopTimesUp();
        StopTimerEndResolution();
        if (restoreTimeScale)
            Time.timeScale = 1f;
    }

    private void StartTimerEndResolution()
    {
        StopTimerEndResolution();
        timerEndResolutionCo = StartCoroutine(TimerEndResolutionRoutine());
    }

    private void StopTimerEndResolution()
    {
        if (timerEndResolutionCo == null)
            return;

        StopCoroutine(timerEndResolutionCo);
        timerEndResolutionCo = null;
    }

    private IEnumerator TimerEndResolutionRoutine()
    {
        Time.timeScale = 0f;
        SetGameplaySystemsActive(false);

        yield return PlayRoundEndAutoCollectAnimationRealtime(false);
        yield return WaitForLevelUpPanelToCloseRealtime();

        timerEndResolutionCo = null;
        EndRound(true);
    }

    private int CalcDue(int round)
    {
        return CalcDue(round, true);
    }

    private int CalcDue(int round, bool includeWheelDebtAdjustments)
    {
        if (round <= 0) return 0;
        if (round == 1)
            return Mathf.Max(0, firstRoundDue);

        int due = baseDue + (round - 1) * stepDue;
        if (useDebtCurveMultiplier)
        {
            float debtMultiplier = EvaluateMonotonicCurve(debtCurve, Mathf.Max(1, round), debtMinGrowthPerRound);
            due = Mathf.RoundToInt(due * debtMultiplier);
        }

        if (useIncomeScaledDebt)
            due = Mathf.Max(due, Mathf.RoundToInt(GetGrossCashForDebtRound(round) * GetIncomeScaledDebtRatioForRound(round)));

        if (includeWheelDebtAdjustments)
        {
            if (round == roundIndex)
                due += runProgression.CurrentRoundDebtIncrease;
            else if (round == roundIndex + 1)
                due += runProgression.NextRoundDebtIncrease;
        }

        return Mathf.Max(due, 0);
    }

    private void ApplySettlement()
    {
        int due = CalcDue(roundIndex);
        int nextRound = roundIndex + 1;
        int nextDue = CalcDue(nextRound);
        int cashEarned = currentRoundCashEarned;
        int netCash = cash - due;
        int roundsLeft = Mathf.Max(0, GetBossRoundIndex() - roundIndex);
        bool hasNextRound = nextRound <= GetBossRoundIndex();
        string nextDebtText = hasNextRound ? GetDebtDisplay(nextRound) : "-";

        RunLogger.Event(
            $"Settlement preview: round={roundIndex}, cashEarned={cashEarned}, cashOnHand={cash}, due={due}, net={netCash}, nextDue={nextDue}, roundsLeft={roundsLeft}");

        // Legacy fields (backward-compatible with older settlement panels).
        if (textDue) textDue.text = $"Debt Payment: -{FormatCurrency(due)}";
        if (textPaid) textPaid.text = $"Cash Earned: +{FormatCurrency(cashEarned)}";
        if (textRemainingDebt)
            textRemainingDebt.text = hasNextRound
                ? $"Next Round Debt: {nextDebtText}"
                : "Next Round Debt: -";

        // New settlement template fields.
        if (textSettlementCashEarned) textSettlementCashEarned.text = $"+{FormatCurrency(cashEarned)}";
        if (textSettlementDebtPayment) textSettlementDebtPayment.text = $"-{FormatCurrency(due)}";
        if (textSettlementNetCash) textSettlementNetCash.text = $"{FormatSignedCurrency(netCash)}";
        if (textSettlementNextRoundDebt) textSettlementNextRoundDebt.text = nextDebtText;
        if (textSettlementRoundsLeft) textSettlementRoundsLeft.text = roundsLeft.ToString();
    }

    private static string FormatCurrency(int amount)
    {
        return $"${Mathf.Max(0, amount):N0}";
    }

    private static string FormatSignedCurrency(int amount)
    {
        string sign = amount >= 0 ? "+" : "-";
        return $"{sign}${Mathf.Abs(amount):N0}";
    }

    private void RefreshHUD()
    {
        RefreshGameplayHudVisibility();

        if (textRound) textRound.text = GetGameplayRoundHudText();
        if (textCash) textCash.text = $"{Mathf.Max(0, cash):N0}";
        if (textDebt) textDebt.text = $"DEBT: {GetDebtDisplay(roundIndex)}";
        // 鍊掕鏃跺湪Update涓洿鏂帮紝杩欓噷鍙垵濮嬪寲
        if (state != GameState.Gameplay && textCountdown != null)
        {
            textCountdown.text = "";
            ResetCountdownStyle();
        }
    }

    private string GetGameplayRoundHudText()
    {
        return IsCurrentRoundBoss()
            ? "BOSS ROUND"
            : $"ROUND {roundIndex:00}/{totalRounds:00}";
    }

    private void UpdateCountdownDisplay()
    {
        if (textCountdown == null)
            return;

        EnsureCountdownStyleInitialized();

        if (state != GameState.Gameplay || pauseMenuOpen)
        {
            textCountdown.text = string.Empty;
            ResetCountdownStyle();
            return;
        }

        if (IsGameplayTutorialBlockingRoundStart())
        {
            textCountdown.text = string.Empty;
            ResetCountdownStyle();
            return;
        }

        if (IsCurrentRoundBoss())
        {
            textCountdown.text = "BOSS";
            ResetCountdownStyle();
            return;
        }

        int seconds = Mathf.Max(0, Mathf.CeilToInt(roundTimeRemaining));
        int minutes = seconds / 60;
        int remainderSeconds = seconds % 60;
        textCountdown.text = $"{minutes:00}:{remainderSeconds:00}";

        int urgentThreshold = Mathf.Max(1, countdownUrgentThresholdSeconds);
        bool urgent = seconds > 0 && seconds <= urgentThreshold;
        if (!urgent)
        {
            ResetCountdownStyle();
            lastDisplayedCountdownSecond = seconds;
            return;
        }

        if (seconds != lastDisplayedCountdownSecond)
        {
            lastDisplayedCountdownSecond = seconds;
            countdownTickPopEndTime = Time.unscaledTime + countdownTickPopSeconds;
        }

        float normalizedUrgency = urgentThreshold > 1
            ? 1f - Mathf.Clamp01((seconds - 1f) / (urgentThreshold - 1f))
            : 1f;
        float scale = Mathf.Lerp(1f, countdownUrgentScaleMultiplier, normalizedUrgency);

        if (countdownTickPopSeconds > 0f && countdownTickPopEndTime > Time.unscaledTime)
        {
            float remain01 = Mathf.Clamp01((countdownTickPopEndTime - Time.unscaledTime) / countdownTickPopSeconds);
            scale += countdownTickPopAmplitude * remain01 * remain01;
        }

        textCountdown.color = Color.Lerp(countdownUrgentColor, countdownCriticalColor, normalizedUrgency);
        textCountdown.rectTransform.localScale = countdownBaseScale * scale;
    }

    private void EnsureCountdownStyleInitialized()
    {
        if (textCountdown == null)
            return;

        if (countdownStyleInitialized && countdownStyleTarget == textCountdown)
            return;

        countdownStyleTarget = textCountdown;
        countdownBaseColor = textCountdown.color;
        countdownBaseScale = textCountdown.rectTransform != null
            ? textCountdown.rectTransform.localScale
            : Vector3.one;
        countdownStyleInitialized = true;
    }

    private void EnsureCountdownMaterialIsolated()
    {
        if (!isolateCountdownMaterialAtRuntime || textCountdown == null || countdownRuntimeMaterial != null)
            return;

        Material shared = textCountdown.fontSharedMaterial;
        if (shared == null)
            return;

        countdownRuntimeMaterial = new Material(shared);
        countdownRuntimeMaterial.name = $"{shared.name}_CountdownRuntime";
        textCountdown.fontSharedMaterial = countdownRuntimeMaterial;
        countdownStyleInitialized = false;
    }

    private void ResetCountdownStyle()
    {
        if (textCountdown == null)
            return;

        EnsureCountdownStyleInitialized();
        textCountdown.color = countdownBaseColor;
        if (textCountdown.rectTransform != null)
            textCountdown.rectTransform.localScale = countdownBaseScale;

        countdownTickPopEndTime = -1f;
        lastDisplayedCountdownSecond = int.MinValue;
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

    private void PlayUIButtonClickSfx(float volumeScale = 1f)
    {
        if (sfxUIButtonClick != null && SFXManager.Instance != null)
            SFXManager.Instance.Play(sfxUIButtonClick, volumeScale);
    }

    public void PlayUIButtonHoverSfx(float volumeScale = 1f)
    {
        if (sfxUIButtonHover == null || SFXManager.Instance == null)
            return;

        if (uiButtonHoverSfxMinInterval > 0f)
        {
            float now = Time.unscaledTime;
            if (now - lastUIButtonHoverSfxTime < uiButtonHoverSfxMinInterval)
                return;

            lastUIButtonHoverSfxTime = now;
        }

        SFXManager.Instance.Play(sfxUIButtonHover, volumeScale);
    }

    private void BindHoverSfxToSceneButtons()
    {
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        int attachedCount = 0;

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            if (!button.gameObject.scene.IsValid() || !button.gameObject.scene.isLoaded)
                continue;

            if (button.GetComponent<UIButtonHoverSfxEmitter>() != null)
                continue;

            button.gameObject.AddComponent<UIButtonHoverSfxEmitter>();
            attachedCount++;
        }

        if (attachedCount > 0)
            RunLogger.Event($"UI hover SFX emitter attached to {attachedCount} button(s).");
    }

    private void BindKeyboardFocusIndicatorsToSceneSelectables()
    {
        if (!enableKeyboardUINavigation)
            return;

        Selectable[] selectables = Resources.FindObjectsOfTypeAll<Selectable>();
        int attachedCount = 0;

        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];
            if (selectable == null)
                continue;
            if (!selectable.gameObject.scene.IsValid() || !selectable.gameObject.scene.isLoaded)
                continue;
            if (selectable.GetComponent<UIKeyboardFocusIndicator>() != null)
                continue;

            selectable.gameObject.AddComponent<UIKeyboardFocusIndicator>();
            attachedCount++;
        }

        if (attachedCount > 0)
            RunLogger.Event($"Keyboard focus indicator attached to {attachedCount} selectable(s).");
    }

    private void EnsureUIEventSystem()
    {
        if (!enableKeyboardUINavigation)
            return;

        if (EventSystem.current != null)
            return;

        EventSystem existing = FindObjectOfType<EventSystem>();
        if (existing != null)
            return;

        GameObject eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        RunLogger.Event($"EventSystem auto-created for keyboard UI navigation: {eventSystemGo.name}");
    }

    private void HandleKeyboardUINavigation()
    {
        if (!enableKeyboardUINavigation)
            return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            EnsureUIEventSystem();
            eventSystem = EventSystem.current;
            if (eventSystem == null)
                return;
        }

        // We drive move/submit explicitly for stable "WASD + Space" behavior.
        if (eventSystem.sendNavigationEvents)
            eventSystem.sendNavigationEvents = false;

        GameObject root = ResolveKeyboardNavigationRoot();
        if (root == null || !root.activeInHierarchy)
        {
            keyboardNavigationRoot = null;
            ResetHeldGamepadUINavigation();
            return;
        }

        bool rootChanged = keyboardNavigationRoot != root;
        keyboardNavigationRoot = root;
        if (rootChanged)
            BindKeyboardFocusIndicatorsToSceneSelectables();

        if (keyboardTabCyclesSelection && Input.GetKeyDown(KeyCode.Tab))
        {
            bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            CycleKeyboardSelection(eventSystem, root, reverse);
            return;
        }

        GameObject selected = eventSystem.currentSelectedGameObject;
        bool hasValidSelected = selected != null &&
                                selected.activeInHierarchy &&
                                selected.transform.IsChildOf(root.transform);
        if (!hasValidSelected || rootChanged)
            SelectFirstKeyboardTarget(eventSystem, root);

        TryMoveSelectionWithWASD(eventSystem, root);

        if (useSpaceForUIConfirm)
            TrySubmitSelectionWithSpace(eventSystem, root);
    }

    private GameObject ResolveKeyboardNavigationRoot()
    {
        if (quitConfirmDialog != null && quitConfirmDialog.NavigationRoot != null)
            return quitConfirmDialog.NavigationRoot;
        if (pauseSettingsMenu != null && pauseSettingsMenu.gameObject.activeInHierarchy)
            return pauseSettingsMenu.gameObject;
        if (panelSettingsPlaceholder != null && panelSettingsPlaceholder.activeInHierarchy)
            return panelSettingsPlaceholder;
        if (pauseMenuOpen && panelPauseMenu != null && panelPauseMenu.activeInHierarchy)
            return panelPauseMenu;
        if (storyIntroActive && storyIntroOverlay != null && storyIntroOverlay.gameObject.activeInHierarchy)
            return storyIntroOverlay.gameObject;
        if (IsLevelUpPanelOpen())
        {
            if (panelLevelUp != null && panelLevelUp.activeInHierarchy)
                return panelLevelUp;
            if (levelUpPanel != null && levelUpPanel.gameObject.activeInHierarchy)
                return levelUpPanel.gameObject;
        }

        switch (state)
        {
            case GameState.Title:
                if (creditsOpen && panelCredits != null && panelCredits.activeInHierarchy)
                    return panelCredits;
                return panelTitle != null && panelTitle.activeInHierarchy ? panelTitle : null;

            case GameState.Settlement:
                return panelSettlement != null && panelSettlement.activeInHierarchy ? panelSettlement : null;

            case GameState.Shop:
                return panelShop != null && panelShop.activeInHierarchy ? panelShop : null;

            case GameState.GameOver:
                if (runSummaryPanel != null && runSummaryPanel.gameObject.activeInHierarchy)
                    return runSummaryPanel.gameObject;
                if (monsterDeathPanel != null && monsterDeathPanel.gameObject.activeInHierarchy)
                    return monsterDeathPanel.gameObject;
                if (debtFailurePanel != null && debtFailurePanel.gameObject.activeInHierarchy)
                    return debtFailurePanel.gameObject;
                return null;

            case GameState.Victory:
                if (runSummaryPanel != null && runSummaryPanel.gameObject.activeInHierarchy)
                    return runSummaryPanel.gameObject;
                if (victoryPanel != null && victoryPanel.gameObject.activeInHierarchy)
                    return victoryPanel.gameObject;
                return null;
        }

        return null;
    }

    private void SelectFirstKeyboardTarget(EventSystem eventSystem, GameObject root)
    {
        if (eventSystem == null || root == null)
            return;

        List<Selectable> candidates = GatherKeyboardSelectables(root);
        if (candidates.Count <= 0)
            return;

        eventSystem.SetSelectedGameObject(candidates[0].gameObject);
    }

    private void CycleKeyboardSelection(EventSystem eventSystem, GameObject root, bool reverse)
    {
        if (eventSystem == null || root == null)
            return;

        List<Selectable> candidates = GatherKeyboardSelectables(root);
        if (candidates.Count <= 0)
            return;

        GameObject current = eventSystem.currentSelectedGameObject;
        int currentIndex = -1;
        if (current != null)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                Selectable selectable = candidates[i];
                if (selectable == null)
                    continue;
                if (current == selectable.gameObject || current.transform.IsChildOf(selectable.transform))
                {
                    currentIndex = i;
                    break;
                }
            }
        }

        int nextIndex;
        if (currentIndex < 0)
            nextIndex = reverse ? candidates.Count - 1 : 0;
        else if (reverse)
            nextIndex = currentIndex <= 0 ? candidates.Count - 1 : currentIndex - 1;
        else
            nextIndex = currentIndex >= candidates.Count - 1 ? 0 : currentIndex + 1;

        eventSystem.SetSelectedGameObject(candidates[nextIndex].gameObject);
        PlayUIButtonHoverSfx(0.7f);
    }

    private List<Selectable> GatherKeyboardSelectables(GameObject root)
    {
        List<Selectable> result = new List<Selectable>();
        if (root == null)
            return result;

        Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];
            if (selectable == null)
                continue;
            if (!selectable.IsActive() || !selectable.IsInteractable())
                continue;
            if (!selectable.gameObject.activeInHierarchy)
                continue;

            result.Add(selectable);
        }

        return result;
    }

    private void TryMoveSelectionWithWASD(EventSystem eventSystem, GameObject root)
    {
        if (eventSystem == null || root == null)
            return;

        MoveDirection moveDir = GetUISelectionMoveDirection();
        Vector2 moveVector = GetMoveVector(moveDir);

        if (moveDir == MoveDirection.None)
            return;

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (selected == null || !selected.activeInHierarchy || !selected.transform.IsChildOf(root.transform))
        {
            SelectFirstKeyboardTarget(eventSystem, root);
            return;
        }

        if (pauseSettingsMenu != null &&
            root == pauseSettingsMenu.gameObject &&
            pauseSettingsMenu.TryHandleKeyboardMove(moveDir, selected))
        {
            return;
        }

        AxisEventData axisEvent = new AxisEventData(eventSystem)
        {
            moveDir = moveDir,
            moveVector = moveVector
        };

        GameObject before = eventSystem.currentSelectedGameObject;
        ExecuteEvents.Execute(before, axisEvent, ExecuteEvents.moveHandler);
        GameObject after = eventSystem.currentSelectedGameObject;
        if (after != null && after != before)
            PlayUIButtonHoverSfx(0.7f);
    }

    private void TrySubmitSelectionWithSpace(EventSystem eventSystem, GameObject root)
    {
        if (eventSystem == null || root == null)
            return;
        if (!IsUIConfirmPressed())
            return;

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (selected == null || !selected.activeInHierarchy || !selected.transform.IsChildOf(root.transform))
        {
            SelectFirstKeyboardTarget(eventSystem, root);
            selected = eventSystem.currentSelectedGameObject;
        }

        if (selected == null)
            return;

        EnsurePlayerActiveItemControllerBound();
        if (playerActiveItemController != null)
            playerActiveItemController.SuppressUseUntilRelease();

        if (pauseSettingsMenu != null &&
            root == pauseSettingsMenu.gameObject &&
            pauseSettingsMenu.TryHandleKeyboardSubmit(selected))
        {
            return;
        }

        BaseEventData submitEvent = new BaseEventData(eventSystem);
        ExecuteEvents.Execute(selected, submitEvent, ExecuteEvents.submitHandler);
    }

    private bool IsUIConfirmPressed()
    {
        if (useSpaceForUIConfirm && GameInput.IsKeyboardConfirmPressed())
            return true;
        if (GameInput.IsGamepadConfirmPressed())
            return true;

        return false;
    }

    private MoveDirection GetUISelectionMoveDirection()
    {
        if (useWASDForUINavigation)
        {
            if (GameInput.IsUIUpPressed())
                return MoveDirection.Up;
            if (GameInput.IsUIDownPressed())
                return MoveDirection.Down;
            if (GameInput.IsUILeftPressed())
                return MoveDirection.Left;
            if (GameInput.IsUIRightPressed())
                return MoveDirection.Right;
            if (GameInput.IsAnyUIKeyboardDirectionHeld())
            {
                ResetHeldGamepadUINavigation();
                return MoveDirection.None;
            }
        }

        float horizontal = GameInput.GetHorizontalAxisRaw();
        float vertical = GameInput.GetVerticalAxisRaw();
        float deadzone = Mathf.Clamp01(gamepadUINavigationDeadzone);

        MoveDirection axisDirection = MoveDirection.None;
        float absHorizontal = Mathf.Abs(horizontal);
        float absVertical = Mathf.Abs(vertical);

        if (absHorizontal >= deadzone || absVertical >= deadzone)
        {
            if (absVertical >= absHorizontal)
                axisDirection = vertical >= 0f ? MoveDirection.Up : MoveDirection.Down;
            else
                axisDirection = horizontal >= 0f ? MoveDirection.Right : MoveDirection.Left;
        }

        if (axisDirection == MoveDirection.None)
        {
            ResetHeldGamepadUINavigation();
            return MoveDirection.None;
        }

        float now = Time.unscaledTime;
        if (axisDirection != heldGamepadUIMoveDirection)
        {
            heldGamepadUIMoveDirection = axisDirection;
            nextGamepadUINavigationTime = now + Mathf.Max(0.01f, gamepadUINavigationInitialRepeatDelay);
            return axisDirection;
        }

        if (now >= nextGamepadUINavigationTime)
        {
            nextGamepadUINavigationTime = now + Mathf.Max(0.01f, gamepadUINavigationRepeatInterval);
            return axisDirection;
        }

        return MoveDirection.None;
    }

    private void ResetHeldGamepadUINavigation()
    {
        heldGamepadUIMoveDirection = MoveDirection.None;
        nextGamepadUINavigationTime = 0f;
    }

    private static Vector2 GetMoveVector(MoveDirection direction)
    {
        switch (direction)
        {
            case MoveDirection.Up:
                return Vector2.up;
            case MoveDirection.Down:
                return Vector2.down;
            case MoveDirection.Left:
                return Vector2.left;
            case MoveDirection.Right:
                return Vector2.right;
            default:
                return Vector2.zero;
        }
    }

    private void CommitRoundCashEarnedToHistory()
    {
        previousRoundCashEarned = Mathf.Max(0, currentRoundCashEarned);
        currentRoundCashEarned = 0;
    }

    private int GetGrossCashForDebtRound(int round)
    {
        if (round <= 1)
            return 0;
        if (round == roundIndex)
            return Mathf.Max(0, previousRoundCashEarned);
        if (round == roundIndex + 1)
            return Mathf.Max(0, currentRoundCashEarned);
        if (round < roundIndex)
            return Mathf.Max(0, previousRoundCashEarned);

        return Mathf.Max(0, currentRoundCashEarned);
    }

    private float GetIncomeScaledDebtRatioForRound(int round)
    {
        float ratio = Mathf.Max(0f, incomeScaledDebtRatio);
        int startRound = Mathf.Max(1, lateRoundDebtPressureStartRound);
        int lateSteps = Mathf.Max(0, round - startRound + 1);
        if (lateSteps > 0)
            ratio += lateSteps * Mathf.Max(0f, lateRoundDebtRatioBonusPerRound);

        return Mathf.Clamp01(ratio);
    }

    private void ShowRoundClearTransition()
    {
        if (roundClearCo != null || roundClearActive)
            StopRoundClearTransition(false);

        if (BGMManager.Instance != null)
            BGMManager.Instance.PlayVictoryBGM();

        EnsureRoundPresentationBound();
        bool useOverlay = roundPresentation != null && roundPresentation.PrepareRoundClearOverlay(roundIndex, totalRounds);

        roundClearActive = true;
        Time.timeScale = 0f;
        SetGameplaySystemsActive(false);

        roundClearCo = StartCoroutine(RoundClearRoutine(useOverlay));
        float duration = roundPresentation != null ? roundPresentation.RoundClearSeconds : roundClearSeconds;
        RunLogger.Event($"Round clear transition shown. overlay={useOverlay}, duration={duration:F2}s");
    }

    private void StopRoundClearTransition(bool resumeGameplayIfNeeded)
    {
        if (roundClearCo != null)
        {
            StopCoroutine(roundClearCo);
            roundClearCo = null;
        }

        if (roundPresentation != null)
            roundPresentation.StopRoundClearOverlay();

        if (!roundClearActive)
            return;

        roundClearActive = false;
        if (resumeGameplayIfNeeded && state == GameState.Gameplay && !IsRoundIntroActive)
        {
            Time.timeScale = 1f;
            SetGameplaySystemsActive(true);
        }
    }

    private IEnumerator RoundClearRoutine(bool useOverlay)
    {
        if (useOverlay && roundPresentation != null)
            yield return roundPresentation.FadeRoundClearOverlayIn();

        float collectStartedAt = Time.unscaledTime;
        yield return PlayRoundEndAutoCollectAnimationRealtime(useOverlay);

        float holdSeconds = roundPresentation != null ? roundPresentation.RoundClearSeconds : roundClearSeconds;
        float remainHold = Mathf.Max(0f, holdSeconds - (Time.unscaledTime - collectStartedAt));
        if (remainHold > 0f)
            yield return new WaitForSecondsRealtime(remainHold);

        yield return WaitForLevelUpPanelToCloseRealtime();

        if (useOverlay && roundPresentation != null)
            yield return roundPresentation.FadeRoundClearOverlayOut();

        roundClearCo = null;
        roundClearActive = false;
        Time.timeScale = 1f;

        EnterSettlementAfterRoundEnd();
    }

    private IEnumerator PlayRoundEndAutoCollectAnimationRealtime(bool useOverlay)
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
        CashPickup[] cashPool = pickupsRoot != null
            ? pickupsRoot.GetComponentsInChildren<CashPickup>(true)
            : FindObjectsOfType<CashPickup>();

        List<XPPickup> xpTargets = new List<XPPickup>();
        List<HealthPickup> hpTargets = new List<HealthPickup>();
        List<CashPickup> cashTargets = new List<CashPickup>();
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

        for (int i = 0; i < cashPool.Length; i++)
        {
            CashPickup pickup = cashPool[i];
            if (pickup == null || !pickup.isActiveAndEnabled)
                continue;

            if (IsPickupEligibleForRoundClearCollect(pickup.transform.position, playerPos, mainCamera, radiusSqr))
                cashTargets.Add(pickup);
        }

        if (xpTargets.Count == 0 && hpTargets.Count == 0 && cashTargets.Count == 0)
            yield break;

        if (useOverlay && roundPresentation != null)
            roundPresentation.ClampRoundClearOverlayAlphaDuringCollect();

        float timeoutAt = Time.unscaledTime + roundClearAutoCollectMaxWaitSeconds;
        int xpCollected = 0;
        int hpCollected = 0;
        int cashCollected = 0;

        while (xpTargets.Count > 0 || hpTargets.Count > 0 || cashTargets.Count > 0)
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

            for (int i = cashTargets.Count - 1; i >= 0; i--)
            {
                CashPickup pickup = cashTargets[i];
                if (pickup == null)
                {
                    cashTargets.RemoveAt(i);
                    continue;
                }

                pickup.transform.position = Vector2.MoveTowards(pickup.transform.position, playerPos, step);

                Vector2 delta = (Vector2)pickup.transform.position - playerPos;
                if (delta.sqrMagnitude <= collectDistanceSqr)
                {
                    if (pickup.ForceCollect())
                        cashCollected++;
                    cashTargets.RemoveAt(i);
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

        for (int i = 0; i < cashTargets.Count; i++)
        {
            CashPickup pickup = cashTargets[i];
            if (pickup == null) continue;
            if (pickup.ForceCollect())
                cashCollected++;
        }

        int total = xpCollected + hpCollected + cashCollected;
        if (total > 0)
        {
            string collectScope = mainCamera != null ? "screen" : "radius-fallback";
            RunLogger.Event(
                $"Round clear auto-collect animated: total={total}, xp={xpCollected}, hp={hpCollected}, cash={cashCollected}, scope={collectScope}, speed={roundClearAutoCollectMoveSpeed:F1}");
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
        if (!IsLevelUpPanelOpen())
            yield break;

        float timeoutAt = Time.unscaledTime + roundClearAutoCollectMaxWaitSeconds;
        while (IsLevelUpPanelOpen())
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
        EnsureRoundPresentationBound();
        if (roundPresentation == null)
        {
            RefreshGameplayHudVisibility();
            SetRoundDebtVisible(true);
            Time.timeScale = 1f;
            SetGameplaySystemsActive(true);
            TryShowDeferredLevelUpRewardIfReady();
            return;
        }

        roundPresentation.PlayRoundIntro(
            roundIndex,
            totalRounds,
            GetDebtDisplay(roundIndex),
            canUseOverlay =>
            {
                RefreshGameplayHudVisibility();
                SetRoundDebtVisible(!canUseOverlay);
                Time.timeScale = 0f;
                SetGameplaySystemsActive(false);
            },
            () =>
            {
                SuppressActiveItemUseUntilRelease();
                Time.timeScale = 1f;
                if (state == GameState.Gameplay)
                    SetGameplaySystemsActive(true);

                RefreshGameplayHudVisibility();
                if (state == GameState.Gameplay)
                    SetRoundDebtVisible(true);

                TryShowDeferredLevelUpRewardIfReady();
                RunLogger.Event("Round intro finished. Gameplay resumed.");
            });

        RunLogger.Event($"Round intro shown. requireAnyKey={roundIntroRequireAnyKeyToContinue}, duration={roundIntroSeconds:F1}s");
    }

    private void StopRoundIntro()
    {
        if (roundPresentation != null)
            roundPresentation.StopRoundIntro();

        Time.timeScale = 1f;

        if (state == GameState.Gameplay)
            SetGameplaySystemsActive(true);

        RefreshGameplayHudVisibility();
        SetRoundDebtVisible(true);
    }

    private bool ShouldSuppressLevelUpRewardsForBossVictory()
    {
        return bossVictorySequenceActive || state == GameState.Victory;
    }

    private void ClearPendingLevelUpRewardsForBossVictory()
    {
        pendingDeferredLevelUpChoices = 0;

        if (levelUpPanel != null)
            levelUpPanel.ForceHideImmediate();
        else if (panelLevelUp != null)
            panelLevelUp.SetActive(false);
    }

    private void RefreshGameplayHudVisibility()
    {
        if (panelHUD == null)
            return;

        bool shouldShowHud = (state == GameState.Gameplay || state == GameState.Settlement)
            && !IsLevelUpPanelOpen()
            && !playerLossPresentationActive;

        if (panelHUD.activeSelf != shouldShowHud)
            panelHUD.SetActive(shouldShowHud);
    }

    private int GetBossRoundIndex()
    {
        return totalRounds + 1;
    }

    private bool IsCurrentRoundBoss()
    {
        return roundIndex == GetBossRoundIndex();
    }

    private void MarkBossRoundEntered()
    {
        trackedBossRoundIndex = roundIndex;
        bossSeenInCurrentBossRound = false;
        // Give spawner a short grace window before first boss-defeat fallback check.
        nextBossDefeatCheckTime = Time.unscaledTime + 0.8f;
    }

    private void LogCurrentEnemyDifficulty()
    {
        GetBaseEnemyMultipliersForRound(roundIndex, out float baseHpMultiplier, out float baseSpeedMultiplier);
        float finalHpMultiplier = baseHpMultiplier * runProgression.CurrentRoundEnemyHpMultiplier;
        float finalSpeedMultiplier = baseSpeedMultiplier * runProgression.CurrentRoundEnemySpeedMultiplier;
        float finalCountMultiplier = runProgression.CurrentRoundEnemyCountMultiplier;

        RunLogger.Event(
            $"Enemy scaling round {roundIndex}: baseHPx={baseHpMultiplier:F2}, baseSpeedx={baseSpeedMultiplier:F2}, " +
            $"buffHPx={runProgression.CurrentRoundEnemyHpMultiplier:F2}, buffSpeedx={runProgression.CurrentRoundEnemySpeedMultiplier:F2}, " +
            $"buffCountx={runProgression.CurrentRoundEnemyCountMultiplier:F2}, finalHPx={finalHpMultiplier:F2}, " +
            $"finalSpeedx={finalSpeedMultiplier:F2}, finalCountx={finalCountMultiplier:F2}");
    }

    private void GetBaseEnemyMultipliersForRound(int round, out float hpMultiplier, out float speedMultiplier)
    {
        int safeRound = Mathf.Max(1, round);
        float hpCurveValue = EvaluateMonotonicCurve(enemyHpCurve, safeRound, enemyHpMinGrowthPerRound);
        float speedCurveValue = EvaluateMonotonicCurve(enemySpeedCurve, safeRound, enemySpeedMinGrowthPerRound);
        float difficultyScale = EvaluateEnemyDifficultyScale(safeRound);
        float roundT = GetRoundCurveT(safeRound);
        float hpPressure = Mathf.Lerp(enemyHpPressureAtRound1, enemyHpPressureAtBoss, roundT);
        float speedPressure = Mathf.Lerp(enemySpeedPressureAtRound1, enemySpeedPressureAtBoss, roundT);

        hpMultiplier = Mathf.Max(1f, hpCurveValue * difficultyScale * hpPressure);
        speedMultiplier = Mathf.Max(1f, speedCurveValue * difficultyScale * speedPressure);
    }

    private float EvaluateEnemyDifficultyScale(int round)
    {
        float t = GetRoundCurveT(round);
        float rampT = enemyDifficultyRamp != null && enemyDifficultyRamp.length > 0
            ? Mathf.Clamp01(enemyDifficultyRamp.Evaluate(t))
            : t;
        float start = Mathf.Max(0.1f, enemyDifficultyScaleAtRound1);
        float end = Mathf.Max(0.1f, enemyDifficultyScaleAtBoss);
        return Mathf.Lerp(start, end, rampT);
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
        float levelT = Mathf.Clamp01((safeLevel - 1f) / Mathf.Max(1f, xpCurveMaxLevel - 1f));
        float requirementMultiplier = Mathf.Lerp(
            Mathf.Max(1f, xpRequirementMultiplierAtLevel1),
            Mathf.Max(1f, xpRequirementMultiplierAtHighLevel),
            levelT);
        int result = Mathf.RoundToInt(baseXpToNext * growth * requirementMultiplier);
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
            return "INF";
        return $"${CalcDue(round)}";
    }

    private bool EnsureBossVictoryFlashOverlay()
    {
        if (bossVictoryFlashOverlay != null && bossVictoryFlashImage != null)
            return true;

        if (bossVictoryFlashOverlayAutoCreated)
            return false;

        Canvas canvas = panelHUD != null ? panelHUD.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
            return false;

        Transform parent = panelHUD != null && panelHUD.transform.parent != null
            ? panelHUD.transform.parent
            : canvas.transform;

        GameObject overlayRoot = new GameObject("BossVictoryFlashOverlayAuto", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.SetParent(parent, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        bossVictoryFlashImage = overlayRoot.GetComponent<Image>();
        bossVictoryFlashImage.color = Color.white;
        bossVictoryFlashImage.raycastTarget = false;

        bossVictoryFlashOverlay = overlayRoot.GetComponent<CanvasGroup>();
        bossVictoryFlashOverlay.alpha = 0f;
        bossVictoryFlashOverlay.interactable = false;
        bossVictoryFlashOverlay.blocksRaycasts = false;
        bossVictoryFlashOverlay.gameObject.SetActive(false);
        bossVictoryFlashOverlay.transform.SetAsLastSibling();

        bossVictoryFlashOverlayAutoCreated = true;
        return true;
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

    private void EnsureGameplayRoundTextBound()
    {
        if (textRound != null || !autoCreateGameplayRoundText || panelHUD == null)
            return;

        if (panelHUD.GetComponent<RectTransform>() == null)
            return;

        textRound = CreateGameplayHudCornerText(
            panelHUD.transform,
            "GameplayRoundTextAuto",
            new Vector2(24f, -24f),
            new Vector2(260f, 38f),
            28f,
            new Color(0.98f, 0.96f, 0.90f, 1f));

        RunLogger.Event("Gameplay round HUD text auto-created.");
    }

    private TMP_Text CreateGameplayHudCornerText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, float fontSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = false;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        text.outlineWidth = 0.2f;
        text.raycastTarget = false;
        return text;
    }

    private void SetRoundDebtVisible(bool visible)
    {
        bool shouldShowDebt = visible && !IsCurrentRoundBoss();

        if (textRound != null) textRound.gameObject.SetActive(visible);
        if (textDebt != null) textDebt.gameObject.SetActive(shouldShowDebt);
    }
}
