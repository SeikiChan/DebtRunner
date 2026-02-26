using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 死亡结算面板 - 显示玩家死因和相关UI
/// 支持两种死亡原因: 被怪物击杀 或 回合结束时债务不足
/// </summary>
public class DeathPanel : MonoBehaviour
{
    public enum DeathType
    {
        KilledByMonster,    // 被怪物打死
        FailedDebt          // 债务不足
    }

    [Header("死亡面板 UI 引用")]
    [LocalizedLabel("死亡标题文本 / Death Title Text")]
    [SerializeField] private TMP_Text deathTitleText;
    
    [LocalizedLabel("死亡原因文本 / Death Reason Text")]
    [SerializeField] private TMP_Text deathReasonText;
    
    [LocalizedLabel("死因描述文本 / Death Description Text")]
    [SerializeField] private TMP_Text deathDescriptionText;

    [LocalizedLabel("死亡背景图像 / Death Background Image")]
    [SerializeField] private Image deathBackgroundImage;

    [Header("死亡文案配置")]
    [LocalizedLabel("通用死亡标题 / Generic Death Title")]
    [SerializeField] private string deathTitle = "YOU DIED!";

    [LocalizedLabel("被怪物击杀标题 / Killed By Monster Title")]
    [SerializeField] private string killedByMonsterTitle = "SLAIN!";
    
    [LocalizedLabel("被怪物击杀描述 / Killed By Monster Description")]
    [SerializeField] private string killedByMonsterDescription = "You were defeated by enemies in battle.";

    [LocalizedLabel("债务失败标题 / Debt Failure Title")]
    [SerializeField] private string debtFailureTitle = "DEBT DEFAULTED!";
    
    [LocalizedLabel("债务失败描述 / Debt Failure Description")]
    [SerializeField] private string debtFailureDescription = "You couldn't pay back your debt. Time's up.";

    [Header("可替换素材")]
    [LocalizedLabel("被怪物击杀背景 / Monster Kill Background")]
    [SerializeField] private Sprite monsterKillBackground;

    [LocalizedLabel("债务失败背景 / Debt Failure Background")]
    [SerializeField] private Sprite debtFailureBackground;

    [Header("显示动画配置")]
    [LocalizedLabel("淡入时长(秒) / Fade In Duration")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.3f;

    [LocalizedLabel("停留时长(秒) / Display Duration")]
    [SerializeField, Min(0f)] private float displayDuration = 2f;

    [LocalizedLabel("淡出时长(秒) / Fade Out Duration")]
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.3f;

    [LocalizedLabel("自动显示与隐藏 / Auto Show And Hide")]
    [SerializeField] private bool autoShowAndHide = true;

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
    private Coroutine displayRoutine;
    private DeathType currentDeathType = DeathType.KilledByMonster;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rectTransform = GetComponent<RectTransform>();
        // 初始化为透明（避免在编辑器或重新绑定时瞬间可见）
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        // 给按钮绑定默认处理（运行时安全绑定）
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
    /// 显示死亡结算面板
    /// </summary>
    public void ShowDeathPanel(DeathType deathType)
    {
        currentDeathType = deathType;
        UpdatePanelContent(deathType);

        if (displayRoutine != null)
            StopCoroutine(displayRoutine);

        // 确保面板在最上层并激活
        if (rectTransform != null)
            rectTransform.SetAsLastSibling();

        // 激活并确保完全可见，不再自动淡出（冻结在界面上）
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // 不启动自动隐藏的 coroutine - 保持面板显示直到手动 HideDeathPanel 被调用
    }

    /// <summary>
    /// 隐藏死亡结算面板
    /// </summary>
    public void HideDeathPanel()
    {
        if (displayRoutine != null)
        {
            StopCoroutine(displayRoutine);
            displayRoutine = null;
        }

        gameObject.SetActive(false);
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 根据死亡类型更新面板内容
    /// </summary>
    private void UpdatePanelContent(DeathType deathType)
    {
        string titleText;
        string descriptionText;
        Sprite backgroundSprite = null;

        switch (deathType)
        {
            case DeathType.KilledByMonster:
                titleText = killedByMonsterTitle;
                descriptionText = killedByMonsterDescription;
                backgroundSprite = monsterKillBackground;
                break;

            case DeathType.FailedDebt:
                titleText = debtFailureTitle;
                descriptionText = debtFailureDescription;
                backgroundSprite = debtFailureBackground;
                break;

            default:
                titleText = deathTitle;
                descriptionText = "";
                break;
        }

        if (deathTitleText != null)
            deathTitleText.text = titleText;

        if (deathDescriptionText != null)
            deathDescriptionText.text = descriptionText;

        // 显示死亡类型标签
        if (deathReasonText != null)
        {
            deathReasonText.text = deathType == DeathType.KilledByMonster ? "KILLED BY MONSTER" : "DEBT DEFAULTED";
        }

        // 应用背景素材(如果已设置)
        if (deathBackgroundImage != null && backgroundSprite != null)
        {
            deathBackgroundImage.sprite = backgroundSprite;
        }

        // 设置按钮文本并确保按钮可见
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

        RunLogger.Event($"Death panel content updated: type={deathType}");
    }

    /// <summary>
    /// 显示/隐藏动画routine
    /// </summary>
    private IEnumerator DisplayRoutine()
    {
        yield return FadeIn();
        yield return Hold();
        yield return FadeOut();

        if (gameObject.activeInHierarchy)
            gameObject.SetActive(false);
    }

    private IEnumerator FadeIn()
    {
        if (fadeInDuration <= 0f)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            if (canvasGroup != null)
                canvasGroup.alpha = t;
            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private IEnumerator Hold()
    {
        if (displayDuration > 0f)
            yield return new WaitForSeconds(displayDuration);
    }

    private IEnumerator FadeOut()
    {
        // Fade out will no longer be used when panel is frozen, but keep logic here if used.
        if (fadeOutDuration <= 0f)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(1f - (elapsed / fadeOutDuration));
            if (canvasGroup != null)
                canvasGroup.alpha = t;
            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public DeathType GetCurrentDeathType()
    {
        return currentDeathType;
    }
}
