using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XPUI : MonoBehaviour
{
    [SerializeField] private Image xpBarFill;
    [SerializeField] private Image xpBarBackground;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text xpText;
    [SerializeField] private Color fillColor = Color.yellow;
    [SerializeField] private Color levelUpFlashColor = Color.white;

    [Header("Animation")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float fillAnimationDuration = 0.2f;
    [SerializeField] private float levelUpFlashDuration = 0.3f;

    private float lastFillAmount;
    private int lastLevel = 1;
    private Coroutine animationCo;
    private Coroutine levelUpFlashCo;
    private FontStyles originalLevelStyle;
    private float originalLevelSpacing;
    private bool levelStyleCaptured;

    private void OnEnable()
    {
        LocalizationTable.LocaleChanged += UpdateXPDisplay;
    }

    private void OnDisable()
    {
        LocalizationTable.LocaleChanged -= UpdateXPDisplay;
    }

    private void Start()
    {
        if (xpBarFill == null)
        {
            Debug.LogError("XPUI: xpBarFill is not assigned.");
            gameObject.SetActive(false);
            return;
        }

        xpBarFill.color = fillColor;

        if (GameFlowController.Instance != null)
            lastLevel = GameFlowController.Instance.GetLevel();

        UpdateXPDisplay();
    }

    public void UpdateXPDisplay()
    {
        if (GameFlowController.Instance == null) return;

        int currentXP = GameFlowController.Instance.GetCurrentXP();
        int xpToNext = GameFlowController.Instance.GetXPToNext();
        int level = GameFlowController.Instance.GetLevel();

        float fillAmount = xpToNext > 0 ? (float)currentXP / xpToNext : 1f;
        fillAmount = Mathf.Clamp01(fillAmount);

        bool leveledUp = level > lastLevel;
        if (leveledUp)
        {
            lastLevel = level;
            OnLevelUp();
        }

        if (levelText != null)
        {
            if (!levelStyleCaptured)
            {
                originalLevelStyle = levelText.fontStyle;
                originalLevelSpacing = levelText.characterSpacing;
                levelStyleCaptured = true;
            }
            bool chinese = LocalizationTable.CurrentLocale == "zh-CN";
            levelText.fontStyle = chinese ? FontStyles.Normal : originalLevelStyle;
            levelText.characterSpacing = chinese ? 1.5f : originalLevelSpacing;
            levelText.text = string.Format(LocalizationTable.Get("hud.level", "等级 {0}"), level);
            KeepLevelLabelInsideItsSlot();
        }

        if (xpText != null)
            xpText.text = $"{currentXP}/{xpToNext}";

        if (xpBarFill == null)
            return;

        bool canAnimateFill = useAnimation && isActiveAndEnabled && gameObject.activeInHierarchy && fillAnimationDuration > 0f;
        if (canAnimateFill && !Mathf.Approximately(fillAmount, lastFillAmount))
        {
            if (animationCo != null)
                StopCoroutine(animationCo);

            animationCo = StartCoroutine(AnimateFill(lastFillAmount, fillAmount));
        }
        else
        {
            xpBarFill.fillAmount = fillAmount;
        }

        lastFillAmount = fillAmount;
    }

    private void KeepLevelLabelInsideItsSlot()
    {
        levelText.textWrappingMode = TextWrappingModes.NoWrap;
        levelText.overflowMode = TextOverflowModes.Ellipsis;
        levelText.fontSize = 23f;
        levelText.fontSizeMin = 16f;
        levelText.fontSizeMax = 23f;
        levelText.enableAutoSizing = true;

        if (xpBarFill == null)
            return;

        RectTransform fillRect = xpBarFill.rectTransform;
        RectTransform levelRect = levelText.rectTransform;
        RectTransform container = fillRect.parent as RectTransform;
        if (container == null || levelRect.parent != container)
            return;

        float containerLeft = -container.rect.width * container.pivot.x;
        float labelRight = levelRect.anchoredPosition.x
            + levelRect.rect.width * (1f - levelRect.pivot.x) - levelText.margin.z;
        float minimumFillInset = labelRight - containerLeft + 12f;
        Vector2 offsetMin = fillRect.offsetMin;
        if (offsetMin.x < minimumFillInset)
        {
            offsetMin.x = minimumFillInset;
            fillRect.offsetMin = offsetMin;
        }
    }

    private void OnLevelUp()
    {
        if (levelUpFlashCo != null)
            StopCoroutine(levelUpFlashCo);

        bool canAnimateFlash = useAnimation && isActiveAndEnabled && gameObject.activeInHierarchy && levelUpFlashDuration > 0f;
        if (canAnimateFlash)
        {
            levelUpFlashCo = StartCoroutine(LevelUpFlash());
        }
        else if (xpBarFill != null)
        {
            xpBarFill.color = fillColor;
        }
    }

    private System.Collections.IEnumerator LevelUpFlash()
    {
        if (xpBarFill == null) yield break;

        Color originalColor = fillColor;

        for (int i = 0; i < 3; i++)
        {
            float elapsed = 0f;
            float halfDuration = levelUpFlashDuration * 0.5f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = halfDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / halfDuration);
                xpBarFill.color = Color.Lerp(originalColor, levelUpFlashColor, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = halfDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / halfDuration);
                xpBarFill.color = Color.Lerp(levelUpFlashColor, originalColor, t);
                yield return null;
            }
        }

        xpBarFill.color = originalColor;

        if (GameFlowController.Instance != null)
        {
            int xpToNext = GameFlowController.Instance.GetXPToNext();
            int currentXP = GameFlowController.Instance.GetCurrentXP();
            xpBarFill.fillAmount = xpToNext > 0 ? (float)currentXP / xpToNext : 1f;
        }
    }

    private System.Collections.IEnumerator AnimateFill(float startFill, float targetFill)
    {
        float elapsed = 0f;

        while (elapsed < fillAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = fillAnimationDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fillAnimationDuration);
            if (xpBarFill != null)
                xpBarFill.fillAmount = Mathf.Lerp(startFill, targetFill, t);
            yield return null;
        }

        if (xpBarFill != null)
            xpBarFill.fillAmount = targetFill;
    }
}
