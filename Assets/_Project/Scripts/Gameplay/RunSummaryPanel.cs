using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 局内结算面板 — 游戏结束（失败/胜利）时展示本局统计数据
/// 三种结局：被怪物击杀、还不起债务、击败Boss胜利
/// </summary>
public class RunSummaryPanel : MonoBehaviour
{
    public enum EndingType { Victory, KilledByMonster, FailedDebt }

    [Header("UI References / UI 引用")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Victory / 胜利")]
    [SerializeField] private string victoryTitle = "胜利";
    [SerializeField] private string victorySubtitle = "你已还清所有债务，征服了竞技场！";
    [SerializeField] private Color victoryTitleColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Header("Killed By Monster / 被怪物击杀")]
    [SerializeField] private string monsterDeathTitle = "战败";
    [SerializeField] private string monsterDeathSubtitle = "你最终被怪物大军淹没了……";
    [SerializeField] private Color monsterDeathTitleColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Failed Debt / 还不起债务")]
    [SerializeField] private string debtFailTitle = "破产";
    [SerializeField] private string debtFailSubtitle = "你没能偿还债务……";
    [SerializeField] private Color debtFailTitleColor = new Color(0.6f, 0.25f, 0.7f, 1f);

    [Header("Button Highlight / 按钮高亮")]
    [SerializeField] private Color restartHighlightColor = new Color(0.35f, 1f, 0.5f, 1f);
    [SerializeField] private Color menuHighlightColor = new Color(0.7f, 0.7f, 0.75f, 1f);

    private CanvasGroup canvasGroup;

    private bool hasSummary;
    private EndingType lastEnding;
    private int lastKills, lastCashEarned, lastXpEarned, lastRoundReached, lastFinalLevel;

    private void OnEnable()
    {
        LocalizationTable.LocaleChanged += RefreshLocalizedSummary;
    }

    private void OnDisable()
    {
        LocalizationTable.LocaleChanged -= RefreshLocalizedSummary;
    }

    private void RefreshLocalizedSummary()
    {
        if (hasSummary)
            SetSummaryText(lastEnding, lastKills, lastCashEarned, lastXpEarned, lastRoundReached, lastFinalLevel);
    }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
            ApplyButtonHighlightColors(restartButton, restartHighlightColor);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            ApplyButtonHighlightColors(mainMenuButton, menuHighlightColor);
        }

        SetupNavigation();
    }

    private void OnRestartClicked()
    {
        HidePanel();
        Time.timeScale = 1f;
        if (GameFlowController.Instance != null)
            GameFlowController.Instance.Restart();
    }

    private void OnMainMenuClicked()
    {
        HidePanel();
        Time.timeScale = 1f;
        if (GameFlowController.Instance != null)
            GameFlowController.Instance.BackToMenu();
    }

    private void SetupNavigation()
    {
        if (restartButton == null || mainMenuButton == null)
            return;

        // Explicit left-right navigation between buttons
        Navigation restartNav = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnRight = mainMenuButton,
            selectOnLeft = mainMenuButton
        };
        restartButton.navigation = restartNav;

        Navigation menuNav = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnRight = restartButton,
            selectOnLeft = restartButton
        };
        mainMenuButton.navigation = menuNav;
    }

    private void ApplyButtonHighlightColors(Button button, Color highlightColor)
    {
        if (button == null)
            return;

        ColorBlock colors = button.colors;
        colors.highlightedColor = highlightColor;
        colors.selectedColor = highlightColor;
        button.colors = colors;
    }

    public void ShowPanel(EndingType ending, int kills, int cashEarned, int xpEarned, int roundReached, int finalLevel)
    {
        lastEnding = ending;
        lastKills = kills;
        lastCashEarned = cashEarned;
        lastXpEarned = xpEarned;
        lastRoundReached = roundReached;
        lastFinalLevel = finalLevel;
        hasSummary = true;
        SetSummaryText(ending, kills, cashEarned, xpEarned, roundReached, finalLevel);

        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        transform.SetAsLastSibling();
        if (restartButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
        }
    }

    private void SetSummaryText(EndingType ending, int kills, int cashEarned, int xpEarned, int roundReached, int finalLevel)
    {
        string title;
        string subtitle;
        Color titleColor;

        switch (ending)
        {
            case EndingType.Victory:
                title = LocalizationTable.Get("summary.victory.title", victoryTitle);
                subtitle = LocalizationTable.Get("summary.victory.subtitle", victorySubtitle);
                titleColor = victoryTitleColor;
                break;
            case EndingType.FailedDebt:
                title = LocalizationTable.Get("summary.debt.title", debtFailTitle);
                subtitle = LocalizationTable.Get("summary.debt.subtitle", debtFailSubtitle);
                titleColor = debtFailTitleColor;
                break;
            default: // KilledByMonster
                title = LocalizationTable.Get("summary.monster.title", monsterDeathTitle);
                subtitle = LocalizationTable.Get("summary.monster.subtitle", monsterDeathSubtitle);
                titleColor = monsterDeathTitleColor;
                break;
        }

        if (titleText != null)
        {
            titleText.text = title;
            titleText.color = titleColor;
        }

        if (resultText != null)
            resultText.text = subtitle;

        if (statsText != null)
        {
            statsText.text = string.Format(
                LocalizationTable.Get("summary.stats", "击败敌人：{0}\n获得现金：${1}\n获得经验：{2}\n存活回合：{3}\n最终等级：{4}"),
                kills, cashEarned, xpEarned, roundReached, finalLevel);
        }

        if (restartButton != null)
            LocalizationTable.SetText(restartButton.GetComponentInChildren<TMP_Text>(true), "summary.restart", "重新开始");
        if (mainMenuButton != null)
            LocalizationTable.SetText(mainMenuButton.GetComponentInChildren<TMP_Text>(true), "summary.menu", "返回主菜单");
    }

    public void HidePanel()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }
}
