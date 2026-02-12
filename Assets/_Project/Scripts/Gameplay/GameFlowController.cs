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

    [Header("HUD Health")]
    [SerializeField] private HealthUI healthUI;

    [Header("HUD XP")]
    [SerializeField] private XPUI xpUI;

    [Header("Level Up Rewards")]
    [SerializeField] private LevelUpPanel levelUpPanel;

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

    // 升级选项数据库
    private WeaponUpgrade[] allUpgrades;


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

        // 初始化升级选项库
        InitializeUpgrades();

        // 初始数据
        roundIndex = 1;
        cash = 0;
        xp = 0;
        debtRemaining = startingDebt;

        // 初始界面：只显示Title
        SwitchState(GameState.Title);
        RefreshHUD();
    }

    /// <summary>
    /// 初始化所有可用的升级选项
    /// </summary>
    private void InitializeUpgrades()
    {
        allUpgrades = new WeaponUpgrade[]
        {
            new WeaponUpgrade("伤害强化 I", "提升伤害 +1", null, power: 1, speed: 0),
            new WeaponUpgrade("速度强化 I", "提升弹速 +2", null, power: 0, speed: 2),
            new WeaponUpgrade("攻速强化 I", "提升攻速 +0.05/秒", null, power: 0, fireRate: 0.05f),
            new WeaponUpgrade("伤害强化 II", "提升伤害 +2", null, power: 2, speed: 0),
            new WeaponUpgrade("多功能强化", "伤害+1 弹速+1", null, power: 1, speed: 1),
        };
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

    // 捡到XP会调用这个
    public void AddXP(int amount)
    {
        int v = Mathf.Max(0, amount);
        if (v == 0) return;

        xp += v;
        
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

    Debug.Log($"升级到 {level} 级！下一级需要 {xpToNext} 点经验");

    if (levelUpPanel != null)
    {
        WeaponUpgrade[] selectedUpgrades = SelectRandomUpgrades(3);
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
        if (allUpgrades == null || allUpgrades.Length < count)
        {
            Debug.LogError("升级选项不足");
            return new WeaponUpgrade[0];
        }

        WeaponUpgrade[] selected = new WeaponUpgrade[count];
        System.Collections.Generic.List<int> indices = new System.Collections.Generic.List<int>();

        // 随机选择不重复的索引
        for (int i = 0; i < allUpgrades.Length; i++)
            indices.Add(i);

        for (int i = 0; i < count; i++)
        {
            int randomIdx = Random.Range(0, indices.Count);
            selected[i] = allUpgrades[indices[randomIdx]];
            indices.RemoveAt(randomIdx);
        }

        return selected;
    }

    /// <summary>
    /// 玩家选择了升级
    /// </summary>
    private void OnUpgradeSelected(WeaponUpgrade upgrade)
    {
        if (playerShooter != null)
            playerShooter.ApplyUpgrade(upgrade);

        // 恢复游戏时间
        Time.timeScale = 1f;

        Debug.Log($"应用升级: {upgrade.title}");
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
        Time.timeScale = 1f;

        // 新开局：重置
        roundIndex = 1;
        cash = 0;
        xp = 0;
        level = 1;
        xpToNext = 10;
        debtRemaining = startingDebt;

        // 重置玩家血量和血量UI
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.RestoreHealth();
        
        if (healthUI != null)
            healthUI.ResetHealthUI();

        // 重置XP UI
        if (xpUI != null)
            xpUI.UpdateXPDisplay();

        // 隐藏升级面板并重置武器
        if (levelUpPanel != null)
            levelUpPanel.ForceHideImmediate();

        SwitchState(GameState.Gameplay);
        StartRoundTimer();
        RefreshHUD();
    }

    // 回合结束入口（未来由倒计时/事件触发）
    public void EndRound()
    {
        if (state != GameState.Gameplay) return;
        Time.timeScale = 1f;        StopRoundTimer();

        // 注意：已移除“随机加钱”。现金应来自击杀敌人时 AddCash(固定值)。

        SwitchState(GameState.Settlement);
        ApplySettlement();
        RefreshHUD();
    }

    // UI Button: Settlement Continue
    public void ConfirmSettlementAndEnterShop()
    {
        if (state != GameState.Settlement) return;

        Time.timeScale = 1f;

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
        Time.timeScale = 1f;
        SwitchState(GameState.Shop);
        RefreshHUD();
    }

    // UI Button: Next Round
    public void NextRound()
    {
        if (state != GameState.Shop) return;

        Time.timeScale = 1f;

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

    // 玩家受到致命伤害 - 触发游戏结束
    public void TriggerGameOver()
    {
        if (state != GameState.Gameplay) return;
        Time.timeScale = 1f; // 确保游戏时间恢复
        StopRoundTimer();
        SwitchState(GameState.GameOver);
    }

    // UI Button: Main Menu
    public void BackToMenu()
    {
        // 确保游戏时间恢复
        Time.timeScale = 1f;
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
