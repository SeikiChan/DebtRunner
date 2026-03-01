using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 死亡结算面板 - 每个实例只负责显示一种死因
/// 怪物击杀和债务失败各用一个独立的面板实例，互不干扰
/// </summary>
public class DeathPanel : MonoBehaviour
{
    [Header("UI 引用 / UI References")]
    [LocalizedLabel("标题文本 / Title Text")]
    [SerializeField] private TMP_Text deathTitleText;

    [LocalizedLabel("原因文本 / Reason Text")]
    [SerializeField] private TMP_Text deathReasonText;

    [LocalizedLabel("描述文本 / Description Text")]
    [SerializeField] private TMP_Text deathDescriptionText;

    [LocalizedLabel("背景图像 / Background Image")]
    [SerializeField] private Image deathBackgroundImage;

    [Header("文案配置 / Text Config")]
    [LocalizedLabel("标题 / Title")]
    [SerializeField] private string title = "YOU DIED!";

    [LocalizedLabel("原因 / Reason")]
    [SerializeField] private string reason = "";

    [LocalizedLabel("描述 / Description")]
    [SerializeField] private string description = "";

    [Header("可替换素材 / Background Sprite")]
    [LocalizedLabel("背景 / Background")]
    [SerializeField] private Sprite backgroundSprite;

    [Header("Buttons / 按钮")]
    [LocalizedLabel("Restart Button / 重启按钮")]
    public Button restartButton;
    [LocalizedLabel("Main Menu Button / 返回主菜单按钮")]
    public Button mainMenuButton;
    [LocalizedLabel("Restart Button Text / 重启按钮文本")]
    [SerializeField] private string restartButtonText = "Restart";
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

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() =>
            {
                if (GameFlowController.Instance != null)
                    GameFlowController.Instance.Restart();
            });
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(() =>
            {
                if (GameFlowController.Instance != null)
                    GameFlowController.Instance.BackToMenu();
            });
        }
    }

    /// <summary>
    /// 显示面板 — 使用本实例自身配置的文案和素材
    /// </summary>
    public void ShowDeathPanel()
    {
        UpdatePanelContent();

        if (rectTransform != null)
            rectTransform.SetAsLastSibling();

        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void HideDeathPanel()
    {
        gameObject.SetActive(false);
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void UpdatePanelContent()
    {
        if (deathTitleText != null)
            deathTitleText.text = title;

        if (deathReasonText != null)
            deathReasonText.text = reason;

        if (deathDescriptionText != null)
            deathDescriptionText.text = description;

        if (deathBackgroundImage != null && backgroundSprite != null)
            deathBackgroundImage.sprite = backgroundSprite;

        if (restartButton != null)
        {
            TextMeshProUGUI t = restartButton.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = restartButtonText;
            restartButton.gameObject.SetActive(true);
        }

        if (mainMenuButton != null)
        {
            TextMeshProUGUI t = mainMenuButton.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = mainMenuButtonText;
            mainMenuButton.gameObject.SetActive(true);
        }

        RunLogger.Event($"Death panel shown: title={title}");
    }
}
