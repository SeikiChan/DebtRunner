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
    [SerializeField] private string victoryTitle = "VICTORY";
    [SerializeField] private string victorySubtitle = "You cleared all debts and conquered the arena!";
    [SerializeField] private Color victoryTitleColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Header("Killed By Monster / 被怪物击杀")]
    [SerializeField] private string monsterDeathTitle = "DEFEATED";
    [SerializeField] private string monsterDeathSubtitle = "You were overwhelmed by the horde...";
    [SerializeField] private Color monsterDeathTitleColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Failed Debt / 还不起债务")]
    [SerializeField] private string debtFailTitle = "BANKRUPT";
    [SerializeField] private string debtFailSubtitle = "You couldn't pay off your debts...";
    [SerializeField] private Color debtFailTitleColor = new Color(0.6f, 0.25f, 0.7f, 1f);

    [Header("Button Highlight / 按钮高亮")]
    [SerializeField] private Color restartHighlightColor = new Color(0.35f, 1f, 0.5f, 1f);
    [SerializeField] private Color menuHighlightColor = new Color(0.7f, 0.7f, 0.75f, 1f);

    private CanvasGroup canvasGroup;
    private Color restartNormalColor;
    private Color menuNormalColor;
    private Image restartImage;
    private Image menuImage;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Cache button images and normal colors for manual highlight (timeScale=0 safe)
        if (restartButton != null)
        {
            restartImage = restartButton.GetComponent<Image>();
            if (restartImage != null)
                restartNormalColor = restartImage.color;

            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (mainMenuButton != null)
        {
            menuImage = mainMenuButton.GetComponent<Image>();
            if (menuImage != null)
                menuNormalColor = menuImage.color;

            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
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

    private void Update()
    {
        // Manual highlight since Button ColorTween freezes at timeScale=0
        UpdateButtonHighlight(restartButton, restartImage, restartNormalColor, restartHighlightColor);
        UpdateButtonHighlight(mainMenuButton, menuImage, menuNormalColor, menuHighlightColor);
    }

    private void UpdateButtonHighlight(Button btn, Image img, Color normalColor, Color highlightColor)
    {
        if (btn == null || img == null)
            return;

        EventSystem es = EventSystem.current;
        bool isSelected = es != null && es.currentSelectedGameObject == btn.gameObject;
        img.color = isSelected ? highlightColor : normalColor;
    }

    public void ShowPanel(EndingType ending, int kills, int cashEarned, int xpEarned, int roundReached, int finalLevel)
    {
        string title;
        string subtitle;
        Color titleColor;

        switch (ending)
        {
            case EndingType.Victory:
                title = victoryTitle;
                subtitle = victorySubtitle;
                titleColor = victoryTitleColor;
                break;
            case EndingType.FailedDebt:
                title = debtFailTitle;
                subtitle = debtFailSubtitle;
                titleColor = debtFailTitleColor;
                break;
            default: // KilledByMonster
                title = monsterDeathTitle;
                subtitle = monsterDeathSubtitle;
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
            statsText.text =
                $"Enemies Killed:    {kills}\n" +
                $"Cash Earned:       ${cashEarned}\n" +
                $"XP Earned:         {xpEarned}\n" +
                $"Rounds Survived:   {roundReached}\n" +
                $"Final Level:       {finalLevel}";
        }

        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        Transform rt = transform;
        if (rt != null)
            rt.SetAsLastSibling();

        // Select first button for keyboard/gamepad navigation
        if (restartButton != null)
        {
            EventSystem es = EventSystem.current;
            if (es != null)
                es.SetSelectedGameObject(restartButton.gameObject);
        }
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
