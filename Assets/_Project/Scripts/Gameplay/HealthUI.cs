using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthUI : MonoBehaviour
{
    private static Sprite runtimeShieldFallbackSprite;

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

    private bool UseNumericDisplay => displayMode == DisplayMode.Numeric;

    public void ResetHealthUI()
    {
        lastHP = -1;
        lastMaxHP = -1;
        lastShieldCharges = -1;
        if (playerHealth != null && (healthIcons != null || UseNumericDisplay))
            UpdateHealthUI();
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
    }

    private void UpdateNumericUI(int currentHP, int shieldCharges)
    {
        if (numericHealthIcon != null)
            numericHealthIcon.enabled = true;

        if (numericHealthText != null)
            numericHealthText.text = $"{numericValuePrefix}{Mathf.Max(0, currentHP)}";

        bool showShield = numericShieldIcon != null || numericShieldText != null;
        if (!showShield)
            return;

        if (numericShieldIcon != null)
            numericShieldIcon.enabled = true;

        if (numericShieldText != null)
            numericShieldText.text = $"{numericValuePrefix}{Mathf.Max(0, shieldCharges)}";
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
