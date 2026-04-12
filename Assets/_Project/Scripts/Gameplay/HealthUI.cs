using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthUI : MonoBehaviour
{
    private static Sprite runtimeShieldFallbackSprite;
    private static Sprite runtimeLowHealthEdgeWarningSprite;

    private enum DisplayMode
    {
        Icons = 0,
        Numeric = 1
    }

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private DisplayMode displayMode = DisplayMode.Icons;
    [SerializeField] private Image healthIconTemplate;
    [SerializeField] private Image shieldIconTemplate;
    [SerializeField] private Sprite shieldIconSprite;
    [SerializeField] private Image numericHealthIcon;
    [SerializeField] private TMP_Text numericHealthText;
    [SerializeField] private Image numericShieldIcon;
    [SerializeField] private TMP_Text numericShieldText;
    [SerializeField] private string numericValuePrefix = "X";
    [SerializeField] private bool showMaxHealthInNumericDisplay = true;
    [SerializeField] private string numericHealthSeparator = " / ";
    [SerializeField, Range(0.01f, 1f)] private float lowHealthWarningThresholdNormalized = 0.45f;
    [SerializeField, Range(0.01f, 1f)] private float lowHealthCriticalThresholdNormalized = 0.22f;
    [SerializeField] private Color numericHealthWarningColor = new Color(1f, 0.76f, 0.24f, 1f);
    [SerializeField] private Color numericHealthCriticalColor = new Color(1f, 0.30f, 0.30f, 1f);
    [SerializeField] private bool pulseLowHealthNumericDisplay = true;
    [SerializeField, Min(0.01f)] private float lowHealthPulseSpeed = 6f;
    [SerializeField, Range(1f, 1.5f)] private float lowHealthWarningPulseScale = 1.06f;
    [SerializeField, Range(1f, 1.5f)] private float lowHealthCriticalPulseScale = 1.12f;
    [Header("Low Health Edge Warning")]
    [SerializeField, Min(1)] private int lowHealthEdgeWarningHpThreshold = 2;
    [SerializeField] private Color lowHealthEdgeWarningColor = new Color(1f, 0.08f, 0.08f, 0.72f);
    [SerializeField, Range(0f, 1f)] private float lowHealthEdgeWarningMinAlpha = 0.18f;
    [SerializeField, Range(0f, 1f)] private float lowHealthEdgeWarningMaxAlpha = 0.42f;
    [SerializeField, Min(0.01f)] private float lowHealthEdgeWarningPulseSpeed = 3.2f;
    [SerializeField] private float spacing = 10f;
    [SerializeField, Min(1)] private int maxIconsPerRow = 5;
    [SerializeField, Min(0f)] private float rowSpacing = 18f;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private Color shieldActiveColor = new Color(0.35f, 0.75f, 1f, 0.95f);
    [SerializeField] private Color shieldInactiveColor = new Color(0.35f, 0.75f, 1f, 0f);
    [SerializeField] private Vector2 shieldOffset = new Vector2(12f, 10f);
    [SerializeField, Range(0.1f, 2f)] private float shieldScale = 0.52f;
    [SerializeField] private bool hideInShopState = true;

    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float animationDuration = 0.2f;

    private Image[] healthIcons;
    private Image[] shieldIcons;
    private int lastHP = -1;
    private int lastMaxHP = -1;
    private int lastShieldCharges = -1;
    private Coroutine shopRevealCo;
    private Color numericHealthTextBaseColor = Color.white;
    private Color numericHealthIconBaseColor = Color.white;
    private Vector3 numericHealthTextBaseScale = Vector3.one;
    private Vector3 numericHealthIconBaseScale = Vector3.one;
    private bool numericVisualDefaultsCached;
    private TMP_Text cachedNumericHealthTextSource;
    private Image cachedNumericHealthIconSource;
    private Image lowHealthEdgeWarningImage;
    private CanvasGroup lowHealthEdgeWarningCanvasGroup;
    private bool lowHealthEdgeWarningOverlayAutoCreated;

    private bool UseNumericDisplay => displayMode == DisplayMode.Numeric;

    public void ResetHealthUI()
    {
        lastHP = -1;
        lastMaxHP = -1;
        lastShieldCharges = -1;
        ResetLowHealthVisualState();
        if (playerHealth != null && (healthIcons != null || UseNumericDisplay))
        {
            UpdateHealthUI();
            RefreshLowHealthVisibility(playerHealth.CurrentHP, playerHealth.MaxHP);
            RefreshLowHealthEdgeWarning(playerHealth.CurrentHP);
        }
    }

    public void ClearTransientWarningVisuals()
    {
        ResetLowHealthVisualState();
    }

    private void OnDisable()
    {
        ResetLowHealthVisualState();
    }

    public void SetHiddenForShop(bool hideNow)
    {
        if (!hideInShopState)
            return;

        bool shouldBeActive = !hideNow;
        if (gameObject.activeSelf == shouldBeActive)
            return;

        gameObject.SetActive(shouldBeActive);
        if (shouldBeActive)
            ResetHealthUI();
    }

    public void RevealForShopFeedback(float seconds)
    {
        if (!hideInShopState || seconds <= 0f)
            return;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            ResetHealthUI();
            shopRevealCo = null;
            return;
        }

        if (shopRevealCo != null)
            StopCoroutine(shopRevealCo);

        ResetHealthUI();
        shopRevealCo = StartCoroutine(RevealForShopFeedbackRoutine(seconds));
    }

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogError("HealthUI: PlayerHealth not found.");
            gameObject.SetActive(false);
            return;
        }

        if (!UseNumericDisplay && healthIconTemplate == null)
        {
            Debug.LogError("HealthUI: healthIconTemplate is not assigned.");
            gameObject.SetActive(false);
            return;
        }

        EnsureNumericRefsBound();

        if (UseNumericDisplay)
        {
            if (healthIconTemplate != null)
                healthIconTemplate.gameObject.SetActive(false);
            if (shieldIconTemplate != null)
                shieldIconTemplate.gameObject.SetActive(false);
        }
        else
        {
            CreateHealthIcons();
        }

        lastHP = playerHealth.CurrentHP;
        lastMaxHP = playerHealth.MaxHP;
        UpdateHealthUI();
        EnsureLowHealthEdgeWarningOverlay();
        RefreshLowHealthEdgeWarning(playerHealth.CurrentHP);
    }

    private void CreateHealthIcons()
    {
        if (UseNumericDisplay)
            return;

        if (playerHealth == null || healthIconTemplate == null)
        {
            healthIcons = null;
            shieldIcons = null;
            return;
        }

        if (healthIcons != null)
        {
            for (int i = 0; i < healthIcons.Length; i++)
            {
                if (healthIcons[i] != null)
                    Destroy(healthIcons[i].gameObject);
            }
        }

        int maxHP = playerHealth.MaxHP;
        healthIcons = new Image[maxHP];
        shieldIcons = new Image[maxHP];

        healthIconTemplate.gameObject.SetActive(false);
        if (shieldIconTemplate != null)
            shieldIconTemplate.gameObject.SetActive(false);

        for (int i = 0; i < maxHP; i++)
        {
            Image newIcon = Instantiate(healthIconTemplate, transform);
            newIcon.gameObject.SetActive(true);
            newIcon.name = $"HealthIcon_{i + 1}";

            RectTransform rectTransform = newIcon.GetComponent<RectTransform>();
            PositionIcon(rectTransform, i);

            healthIcons[i] = newIcon;
            shieldIcons[i] = CreateShieldOverlay(newIcon, i);
        }

        ResizeContainer(maxHP);
    }

    private void Update()
    {
        if (playerHealth == null) return;

        int currentHP = playerHealth.CurrentHP;
        int currentMaxHP = playerHealth.MaxHP;
        int currentShieldCharges = playerHealth.ShieldCharges;

        if (currentMaxHP != lastMaxHP)
        {
            lastMaxHP = currentMaxHP;

            if (UseNumericDisplay)
            {
                EnsureNumericRefsBound();
            }
            else
            {
                CreateHealthIcons();
            }

            lastHP = -1;
            lastShieldCharges = -1;
        }

        if (currentHP != lastHP || currentShieldCharges != lastShieldCharges)
        {
            lastHP = currentHP;
            lastShieldCharges = currentShieldCharges;
            UpdateHealthUI();
        }

        RefreshLowHealthVisibility(currentHP, currentMaxHP);
        RefreshLowHealthEdgeWarning(currentHP);
    }

    private void UpdateHealthUI()
    {
        if (playerHealth == null)
            return;

        if (UseNumericDisplay)
        {
            UpdateNumericUI(playerHealth.CurrentHP, playerHealth.ShieldCharges);
            return;
        }

        if (healthIcons == null)
            return;

        int currentHP = playerHealth.CurrentHP;
        bool canAnimate = useAnimation && isActiveAndEnabled && gameObject.activeInHierarchy && animationDuration > 0f;

        for (int i = 0; i < healthIcons.Length; i++)
        {
            Image icon = healthIcons[i];
            if (icon == null) continue;

            Color targetColor = i < currentHP ? activeColor : inactiveColor;
            if (canAnimate)
                StartCoroutine(AnimateHealthIcon(icon, targetColor));
            else
                icon.color = targetColor;
        }

        UpdateShieldUI(playerHealth.ShieldCharges, canAnimate);
    }

    private void EnsureNumericRefsBound()
    {
        if (!UseNumericDisplay)
            return;

        if (numericHealthIcon == null)
            numericHealthIcon = transform.Find("Image_HpValueIcon")?.GetComponent<Image>();

        if (numericHealthText == null)
            numericHealthText = transform.Find("Text_HpValue")?.GetComponent<TMP_Text>();

        if (numericShieldIcon == null)
            numericShieldIcon = transform.Find("Image_ShieldValueIcon")?.GetComponent<Image>();

        if (numericShieldText == null)
            numericShieldText = transform.Find("Text_ShieldValue")?.GetComponent<TMP_Text>();

        CacheNumericVisualDefaults();
    }

    private void UpdateNumericUI(int currentHP, int shieldCharges)
    {
        if (numericHealthIcon != null)
            numericHealthIcon.enabled = true;

        if (numericHealthText != null)
        {
            int safeCurrentHp = Mathf.Max(0, currentHP);
            int safeMaxHp = playerHealth != null ? Mathf.Max(1, playerHealth.MaxHP) : Mathf.Max(1, safeCurrentHp);
            numericHealthText.text = showMaxHealthInNumericDisplay
                ? $"{safeCurrentHp}{numericHealthSeparator}{safeMaxHp}"
                : $"{numericValuePrefix}{safeCurrentHp}";
        }

        bool showShield = numericShieldIcon != null || numericShieldText != null;
        if (!showShield)
            return;

        if (numericShieldIcon != null)
            numericShieldIcon.enabled = true;

        if (numericShieldText != null)
            numericShieldText.text = $"{numericValuePrefix}{Mathf.Max(0, shieldCharges)}";
    }

    private void CacheNumericVisualDefaults()
    {
        if (!UseNumericDisplay)
            return;

        bool textSourceChanged = numericHealthText != cachedNumericHealthTextSource;
        bool iconSourceChanged = numericHealthIcon != cachedNumericHealthIconSource;

        if (numericHealthText != null && (!numericVisualDefaultsCached || textSourceChanged))
        {
            numericHealthTextBaseColor = numericHealthText.color;
            numericHealthTextBaseScale = numericHealthText.rectTransform.localScale;
            cachedNumericHealthTextSource = numericHealthText;
        }

        if (numericHealthIcon != null && (!numericVisualDefaultsCached || iconSourceChanged))
        {
            numericHealthIconBaseColor = numericHealthIcon.color;
            numericHealthIconBaseScale = numericHealthIcon.rectTransform.localScale;
            cachedNumericHealthIconSource = numericHealthIcon;
        }

        numericVisualDefaultsCached = numericHealthText != null || numericHealthIcon != null;
    }

    private void RefreshLowHealthVisibility(int currentHP, int currentMaxHP)
    {
        if (!UseNumericDisplay)
            return;

        if (!numericVisualDefaultsCached)
            CacheNumericVisualDefaults();

        if (numericHealthText == null && numericHealthIcon == null)
            return;

        float safeMaxHp = Mathf.Max(1f, currentMaxHP);
        float healthRatio = Mathf.Clamp01(Mathf.Max(0, currentHP) / safeMaxHp);
        float warningThreshold = Mathf.Clamp01(lowHealthWarningThresholdNormalized);
        float criticalThreshold = Mathf.Clamp01(Mathf.Min(lowHealthCriticalThresholdNormalized, warningThreshold));

        Color targetTextColor = numericHealthTextBaseColor;
        Color targetIconColor = numericHealthIconBaseColor;
        float pulseScale = 1f;

        if (healthRatio <= criticalThreshold)
        {
            targetTextColor = numericHealthCriticalColor;
            targetIconColor = numericHealthCriticalColor;
            pulseScale = pulseLowHealthNumericDisplay
                ? Mathf.Lerp(1f, Mathf.Max(1f, lowHealthCriticalPulseScale), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.Max(0.01f, lowHealthPulseSpeed)))
                : 1f;
        }
        else if (healthRatio <= warningThreshold)
        {
            float t = warningThreshold <= criticalThreshold
                ? 1f
                : Mathf.InverseLerp(warningThreshold, criticalThreshold, healthRatio);
            targetTextColor = Color.Lerp(numericHealthWarningColor, numericHealthCriticalColor, t);
            targetIconColor = targetTextColor;
            pulseScale = pulseLowHealthNumericDisplay
                ? Mathf.Lerp(1f, Mathf.Max(1f, lowHealthWarningPulseScale), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.Max(0.01f, lowHealthPulseSpeed)))
                : 1f;
        }

        if (numericHealthText != null)
        {
            numericHealthText.color = targetTextColor;
            numericHealthText.rectTransform.localScale = numericHealthTextBaseScale * pulseScale;
        }

        if (numericHealthIcon != null)
        {
            numericHealthIcon.color = targetIconColor;
            numericHealthIcon.rectTransform.localScale = numericHealthIconBaseScale * pulseScale;
        }
    }

    private void ResetLowHealthVisualState()
    {
        if (!numericVisualDefaultsCached)
            CacheNumericVisualDefaults();

        if (numericHealthText != null)
        {
            numericHealthText.color = numericHealthTextBaseColor;
            numericHealthText.rectTransform.localScale = numericHealthTextBaseScale;
        }

        if (numericHealthIcon != null)
        {
            numericHealthIcon.color = numericHealthIconBaseColor;
            numericHealthIcon.rectTransform.localScale = numericHealthIconBaseScale;
        }

        if (lowHealthEdgeWarningCanvasGroup != null)
            lowHealthEdgeWarningCanvasGroup.alpha = 0f;
    }

    private void EnsureLowHealthEdgeWarningOverlay()
    {
        if (lowHealthEdgeWarningImage != null && lowHealthEdgeWarningCanvasGroup != null)
            return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
            parentCanvas = FindObjectOfType<Canvas>();
        if (parentCanvas == null)
            return;

        Transform existing = parentCanvas.transform.Find("LowHealthEdgeWarningOverlay");
        if (existing != null)
        {
            lowHealthEdgeWarningCanvasGroup = existing.GetComponent<CanvasGroup>();
            lowHealthEdgeWarningImage = existing.GetComponent<Image>();
            return;
        }

        GameObject overlayRoot = new GameObject(
            "LowHealthEdgeWarningOverlay",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image));
        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.SetParent(parentCanvas.transform, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.SetAsLastSibling();

        lowHealthEdgeWarningCanvasGroup = overlayRoot.GetComponent<CanvasGroup>();
        lowHealthEdgeWarningCanvasGroup.alpha = 0f;
        lowHealthEdgeWarningCanvasGroup.blocksRaycasts = false;
        lowHealthEdgeWarningCanvasGroup.interactable = false;

        lowHealthEdgeWarningImage = overlayRoot.GetComponent<Image>();
        lowHealthEdgeWarningImage.sprite = ResolveLowHealthEdgeWarningSprite();
        lowHealthEdgeWarningImage.color = lowHealthEdgeWarningColor;
        lowHealthEdgeWarningImage.type = Image.Type.Simple;
        lowHealthEdgeWarningImage.preserveAspect = false;
        lowHealthEdgeWarningImage.raycastTarget = false;

        lowHealthEdgeWarningOverlayAutoCreated = true;
    }

    private void RefreshLowHealthEdgeWarning(int currentHP)
    {
        EnsureLowHealthEdgeWarningOverlay();
        if (lowHealthEdgeWarningCanvasGroup == null || lowHealthEdgeWarningImage == null)
            return;

        bool shouldShow =
            playerHealth != null &&
            !playerHealth.IsDead &&
            currentHP <= Mathf.Max(1, lowHealthEdgeWarningHpThreshold) &&
            (GameFlowController.Instance == null || GameFlowController.Instance.IsInGameplayState);

        if (!shouldShow)
        {
            lowHealthEdgeWarningCanvasGroup.alpha = 0f;
            return;
        }

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.Max(0.01f, lowHealthEdgeWarningPulseSpeed));
        lowHealthEdgeWarningCanvasGroup.alpha = Mathf.Lerp(
            Mathf.Clamp01(lowHealthEdgeWarningMinAlpha),
            Mathf.Clamp01(lowHealthEdgeWarningMaxAlpha),
            pulse);
        lowHealthEdgeWarningImage.color = lowHealthEdgeWarningColor;
    }

    private static Sprite ResolveLowHealthEdgeWarningSprite()
    {
        if (runtimeLowHealthEdgeWarningSprite != null)
            return runtimeLowHealthEdgeWarningSprite;

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "RuntimeLowHealthEdgeWarning";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxDistance = center.magnitude;
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color solid = new Color(1f, 1f, 1f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance01 = Vector2.Distance(pos, center) / Mathf.Max(0.0001f, maxDistance);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 1f, distance01));
                texture.SetPixel(x, y, Color.Lerp(clear, solid, alpha));
            }
        }

        texture.Apply(false, true);
        runtimeLowHealthEdgeWarningSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f);
        runtimeLowHealthEdgeWarningSprite.name = "RuntimeLowHealthEdgeWarning";
        return runtimeLowHealthEdgeWarningSprite;
    }

    private System.Collections.IEnumerator AnimateHealthIcon(Image icon, Color targetColor)
    {
        if (icon == null) yield break;

        Color startColor = icon.color;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            if (icon == null) yield break;
            elapsed += Time.deltaTime;
            icon.color = Color.Lerp(startColor, targetColor, Mathf.Clamp01(elapsed / animationDuration));
            yield return null;
        }

        if (icon != null)
            icon.color = targetColor;
    }

    private void UpdateShieldUI(int shieldCharges, bool canAnimate)
    {
        if (shieldIcons == null) return;

        int shown = Mathf.Clamp(shieldCharges, 0, shieldIcons.Length);
        for (int i = 0; i < shieldIcons.Length; i++)
        {
            Image shield = shieldIcons[i];
            if (shield == null) continue;

            Color targetColor = i < shown ? shieldActiveColor : shieldInactiveColor;
            if (canAnimate)
                StartCoroutine(AnimateShieldIcon(shield, targetColor));
            else
                shield.color = targetColor;
        }
    }

    private Image CreateShieldOverlay(Image parentIcon, int index)
    {
        if (parentIcon == null)
            return null;

        Image overlay;
        if (shieldIconTemplate != null)
        {
            overlay = Instantiate(shieldIconTemplate, parentIcon.transform);
            overlay.gameObject.SetActive(true);
        }
        else
        {
            GameObject go = new GameObject($"ShieldIcon_{index + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parentIcon.transform, false);
            overlay = go.GetComponent<Image>();
            overlay.sprite = ResolveShieldSprite(parentIcon);
            overlay.type = Image.Type.Simple;
        }

        overlay.raycastTarget = false;
        overlay.preserveAspect = true;
        overlay.color = shieldInactiveColor;
        overlay.enabled = overlay.sprite != null;

        RectTransform heartRect = parentIcon.rectTransform;
        RectTransform shieldRect = overlay.rectTransform;
        shieldRect.anchorMin = new Vector2(0.5f, 0.5f);
        shieldRect.anchorMax = new Vector2(0.5f, 0.5f);
        shieldRect.pivot = new Vector2(0.5f, 0.5f);
        shieldRect.anchoredPosition = shieldOffset;

        Vector2 baseSize = heartRect.rect.size;
        float clampedScale = Mathf.Clamp(shieldScale, 0.1f, 2f);
        if (shieldRect.sizeDelta.sqrMagnitude <= 0.001f)
            shieldRect.sizeDelta = baseSize * clampedScale;
        else
            shieldRect.sizeDelta *= clampedScale;

        return overlay;
    }

    private void PositionIcon(RectTransform rectTransform, int index)
    {
        if (rectTransform == null)
            return;

        int iconsPerRow = Mathf.Max(1, maxIconsPerRow);
        int column = index % iconsPerRow;
        int row = index / iconsPerRow;
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;
        float stepX = width + spacing;
        float stepY = height + rowSpacing;
        rectTransform.anchoredPosition = new Vector2(column * stepX, row * -stepY);
    }

    private void ResizeContainer(int iconCount)
    {
        RectTransform container = transform as RectTransform;
        if (container == null || healthIconTemplate == null)
            return;

        int iconsPerRow = Mathf.Max(1, maxIconsPerRow);
        int rows = Mathf.Max(1, Mathf.CeilToInt(iconCount / (float)iconsPerRow));
        float iconWidth = healthIconTemplate.rectTransform.rect.width;
        float iconHeight = healthIconTemplate.rectTransform.rect.height;
        float width = (Mathf.Min(iconCount, iconsPerRow) * iconWidth) + (Mathf.Max(0, Mathf.Min(iconCount, iconsPerRow) - 1) * spacing);
        float height = (rows * iconHeight) + (Mathf.Max(0, rows - 1) * rowSpacing);
        container.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        container.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    private Sprite ResolveShieldSprite(Image parentIcon)
    {
        if (shieldIconSprite != null)
            return shieldIconSprite;

        if (shieldIconTemplate != null && shieldIconTemplate.sprite != null)
            return shieldIconTemplate.sprite;

        if (parentIcon != null && parentIcon.sprite != null)
            return parentIcon.sprite;

        if (runtimeShieldFallbackSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            runtimeShieldFallbackSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
            runtimeShieldFallbackSprite.name = "RuntimeShieldFallback";
        }

        return runtimeShieldFallbackSprite;
    }

    private System.Collections.IEnumerator AnimateShieldIcon(Image icon, Color targetColor)
    {
        if (icon == null) yield break;

        Color startColor = icon.color;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            if (icon == null) yield break;
            elapsed += Time.deltaTime;
            icon.color = Color.Lerp(startColor, targetColor, Mathf.Clamp01(elapsed / animationDuration));
            yield return null;
        }

        if (icon != null)
            icon.color = targetColor;
    }

    private System.Collections.IEnumerator RevealForShopFeedbackRoutine(float seconds)
    {
        bool shouldHideAgain = GameFlowController.Instance != null && GameFlowController.Instance.IsInShopState;
        yield return new WaitForSecondsRealtime(seconds);

        if (shouldHideAgain && GameFlowController.Instance != null && GameFlowController.Instance.IsInShopState)
            gameObject.SetActive(false);

        shopRevealCo = null;
    }
}
