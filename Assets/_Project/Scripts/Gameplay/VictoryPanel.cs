using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 胜利结算面板 - 显示玩家战胜Boss后的胜利界面
/// </summary>
public class VictoryPanel : MonoBehaviour
{
    [Header("胜利面板 UI 引用")]
    [LocalizedLabel("胜利标题文本 / Victory Title Text")]
    [SerializeField] private TMP_Text victoryTitleText;
    
    [LocalizedLabel("胜利副标题文本 / Victory Subtitle Text")]
    [SerializeField] private TMP_Text victorySubtitleText;

    [LocalizedLabel("胜利背景图像 / Victory Background Image")]
    [SerializeField] private Image victoryBackgroundImage;

    [Header("文案配置")]
    [LocalizedLabel("胜利标题 / Victory Title")]
    [SerializeField] private string victoryTitle = "DEBT CLEAR";
    
    [LocalizedLabel("胜利副标题 / Victory Subtitle")]
    [SerializeField] private string victorySubtitle = "YOU'RE FREE";

    [LocalizedLabel("胜利统计文本 / Victory Stats Text")]
    [SerializeField] private TMP_Text victoryStatsText;
    
    [LocalizedLabel("统计文本模板 / Stats Template")]
    [SerializeField] private string statsTemplate = "Total Rounds Survived: {0}\nTotal Cash Earned: {1}\nFinal Level: {2}";

    [Header("可替换素材")]
    [LocalizedLabel("胜利背景 / Victory Background")]
    [SerializeField] private Sprite victoryBackground;

    [Header("Buttons / 按钮")]
    [LocalizedLabel("End Game Button / 游戏结束按钮")]
    public Button endGameButton;
    
    [LocalizedLabel("Main Menu Button / 返回主菜单按钮")]
    public Button mainMenuButton;
    
    [LocalizedLabel("End Game Button Text / 游戏结束按钮文本")]
    [SerializeField] private string endGameButtonText = "End Game";
    
    [LocalizedLabel("Main Menu Button Text / 返回主菜单按钮文本")]
    [SerializeField] private string mainMenuButtonText = "Main Menu";

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rectTransform = GetComponent<RectTransform>();
        
        // 初始化为透明
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        // 给按钮绑定默认处理
        if (endGameButton != null)
        {
            endGameButton.onClick.RemoveAllListeners();
            endGameButton.onClick.AddListener(() =>
            {
                RunLogger.Event("End Game button clicked.");
                if (GameFlowController.Instance != null)
                    GameFlowController.Instance.BackToMenu();
            });
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(() =>
            {
                RunLogger.Event("Main Menu button clicked.");
                if (GameFlowController.Instance != null)
                    GameFlowController.Instance.BackToMenu();
            });
        }
    }

    /// <summary>
    /// 显示胜利界面
    /// </summary>
    public void ShowVictoryPanel(int finalRound, int totalCash, int finalLevel)
    {
        UpdatePanelContent(finalRound, totalCash, finalLevel);

        // 确保面板在最上层并激活
        if (rectTransform != null)
            rectTransform.SetAsLastSibling();

        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        RunLogger.Event($"Victory panel shown. round={finalRound}, cash={totalCash}, level={finalLevel}");
    }

    /// <summary>
    /// 隐藏胜利界面
    /// </summary>
    public void HideVictoryPanel()
    {
        gameObject.SetActive(false);
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 更新面板内容
    /// </summary>
    private void UpdatePanelContent(int finalRound, int totalCash, int finalLevel)
    {
        if (victoryTitleText != null)
            victoryTitleText.text = victoryTitle;

        if (victorySubtitleText != null)
            victorySubtitleText.text = victorySubtitle;

        // 显示统计信息
        if (victoryStatsText != null)
        {
            victoryStatsText.text = string.Format(statsTemplate, finalRound, totalCash, finalLevel);
        }

        // 应用背景素材
        if (victoryBackgroundImage != null && victoryBackground != null)
        {
            victoryBackgroundImage.sprite = victoryBackground;
        }

        // 设置按钮文本
        if (endGameButton != null)
        {
            TextMeshProUGUI t = endGameButton.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = endGameButtonText;
            endGameButton.gameObject.SetActive(true);
        }

        if (mainMenuButton != null)
        {
            TextMeshProUGUI t = mainMenuButton.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = mainMenuButtonText;
            mainMenuButton.gameObject.SetActive(true);
        }

        RunLogger.Event($"Victory panel content updated. round={finalRound}, cash={totalCash}, level={finalLevel}");
    }
}
