using System.Collections;
using TMPro;
using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    public enum GameState { Title, Gameplay, Settlement, Shop, GameOver }

    [Header("Panels")]
    [SerializeField] private GameObject panelTitle;
    [SerializeField] private GameObject panelHUD;
    [SerializeField] private GameObject panelLevelUp;
    [SerializeField] private GameObject panelSettlement;
    [SerializeField] private GameObject panelShop;
    [SerializeField] private GameObject panelGameOver;

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

    [Header("Settlement Text")]
    [SerializeField] private TMP_Text textDue;
    [SerializeField] private TMP_Text textPaid;
    [SerializeField] private TMP_Text textRemainingDebt;

    [Header("Run Config (temporary numbers)")]
    [SerializeField] private int totalRounds = 10;
    [SerializeField] private int startingDebt = 5000;
    [SerializeField] private int baseDue = 500;
    [SerializeField] private int stepDue = 200;
    [SerializeField] private float roundDurationSeconds = 30f;

    [Header("XP (temp)")]
    [SerializeField] private int level = 1;
    [SerializeField] private int xp = 0;
    [SerializeField] private int xpToNext = 10;


    [Header("Gameplay Systems")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private PlayerShooter playerShooter;

    private GameState state;
    private int roundIndex;          // 1-based
    private int cash;
    private int debtRemaining;
    private Coroutine roundTimerCo;

    private void Awake()
    {
        // 单例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 初始数据
        roundIndex = 1;
        cash = 0;
        xp = 0;
        debtRemaining = startingDebt;

        // 初始界面：只显示Title
        SwitchState(GameState.Title);
        RefreshHUD();
    }

    private void Update()
    {
        // 快速测试热键
        if (Input.GetKeyDown(KeyCode.F1)) SwitchState(GameState.Title);
        if (Input.GetKeyDown(KeyCode.F2)) StartRun();
        if (Input.GetKeyDown(KeyCode.F3)) EndRound();
        if (Input.GetKeyDown(KeyCode.F4)) EnterShop();
        if (Input.GetKeyDown(KeyCode.F5)) SwitchState(GameState.GameOver);
    }

    // ====== Public APIs (给其他系统调用) ======

    // 敌人死亡自动加钱会调用这个
    public void AddCash(int amount)
    {
        int v = Mathf.Max(0, amount);
        if (v == 0) return;

        cash += v;
        RefreshHUD();
    }

    // 捡到XP会调用这个（现在先只累计，下一步再做升级弹窗）
    public void AddXP(int amount)
    {
        int v = Mathf.Max(0, amount);
        if (v == 0) return;

        xp += v;
        // TODO: 下一步做 XP 环 / 升级面板触发
    }

    // UI Button: Start
    public void StartRun()
    {
        // 新开局：重置
        roundIndex = 1;
        cash = 0;
        xp = 0;
        debtRemaining = startingDebt;

        SwitchState(GameState.Gameplay);
        StartRoundTimer();
        RefreshHUD();
    }

    // 回合结束入口（未来由倒计时/事件触发）
    public void EndRound()
    {
        if (state != GameState.Gameplay) return;

        StopRoundTimer();

        // 注意：已移除“随机加钱”。现金应来自击杀敌人时 AddCash(固定值)。

        SwitchState(GameState.Settlement);
        ApplySettlement();
        RefreshHUD();
    }

    // UI Button: Settlement Continue
    public void ConfirmSettlementAndEnterShop()
    {
        if (state != GameState.Settlement) return;

        int due = CalcDue(roundIndex, debtRemaining);
        if (cash < due)
        {
            SwitchState(GameState.GameOver);
            return;
        }

        EnterShop();
    }

    public void EnterShop()
    {
        SwitchState(GameState.Shop);
        RefreshHUD();
    }

    // UI Button: Next Round
    public void NextRound()
    {
        if (state != GameState.Shop) return;

        roundIndex += 1;
        if (roundIndex > totalRounds)
        {
            // 临时：先回Title当作占位胜利（后面换Boss链路）
            SwitchState(GameState.Title);
            return;
        }

        SwitchState(GameState.Gameplay);
        StartRoundTimer();
        RefreshHUD();
    }

    // UI Button: Restart（直接开始游戏）
    public void Restart()
    {
        StartRun();
    }

    // UI Button: Main Menu
    public void BackToMenu()
    {
        StopRoundTimer();
        SwitchState(GameState.Title);
    }

    // UI Button: Quit
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

    // ====== Internal ======

    private void SwitchState(GameState next)
{
    state = next;

    if (panelTitle) panelTitle.SetActive(state == GameState.Title);
    if (panelHUD) panelHUD.SetActive(state == GameState.Gameplay || state == GameState.Settlement || state == GameState.Shop);
    if (panelLevelUp) panelLevelUp.SetActive(false);
    if (panelSettlement) panelSettlement.SetActive(state == GameState.Settlement);
    if (panelShop) panelShop.SetActive(state == GameState.Shop);
    if (panelGameOver) panelGameOver.SetActive(state == GameState.GameOver);

    bool inGameplay = (state == GameState.Gameplay);

    // 只在Gameplay运行：刷怪 + 玩家射击
    if (enemySpawner != null) enemySpawner.enabled = inGameplay;
    if (playerShooter != null) playerShooter.enabled = inGameplay;

    // 移动/镜头：只在Gameplay运行
    if (playerMotor != null)
    {
        playerMotor.enabled = inGameplay;
        var rb = playerMotor.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    if (cameraFollow != null)
        cameraFollow.enabled = inGameplay;
    
    // 离开Gameplay就清场（结算/商店/失败/回菜单都干净）
    if (!inGameplay)
        ClearWorld();
}

private void ClearWorld()
{
    ClearChildren(enemiesRoot);
    ClearChildren(projectilesRoot);
    ClearChildren(pickupsRoot);
}

private void ClearChildren(Transform root)
{
    if (root == null) return;
    for (int i = root.childCount - 1; i >= 0; i--)
        Destroy(root.GetChild(i).gameObject);
}


    private void StartRoundTimer()
    {
        StopRoundTimer();
        roundTimerCo = StartCoroutine(RoundTimer());
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
            yield return null;
        }

        if (state == GameState.Gameplay)
            EndRound();
    }

    private int CalcDue(int round, int debtLeft)
    {
        int due = baseDue + (round - 1) * stepDue;
        if (due > debtLeft) due = debtLeft;
        return Mathf.Max(due, 0);
    }

    private void ApplySettlement()
    {
        int due = CalcDue(roundIndex, debtRemaining);
        int paid = Mathf.Min(cash, due);

        cash -= paid;
        debtRemaining -= paid;

        if (textDue) textDue.text = $"Due: {due}";
        if (textPaid) textPaid.text = $"Paid: {paid}";
        if (textRemainingDebt) textRemainingDebt.text = $"Remaining Debt: {debtRemaining}";

        if (debtRemaining <= 0)
        {
            SwitchState(GameState.Title);
        }
    }

    private void RefreshHUD()
    {
        if (textRound) textRound.text = $"Round: {roundIndex}/{totalRounds}";
        if (textCash) textCash.text = $"$ {cash}";
        if (textDebt) textDebt.text = $"Debt: {debtRemaining}";
    }
}
