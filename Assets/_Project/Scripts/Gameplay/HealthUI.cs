using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    private static Sprite runtimeShieldFallbackSprite;

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image healthIconTemplate;
    [SerializeField] private Image shieldIconTemplate;
    [SerializeField] private Sprite shieldIconSprite;
    [SerializeField] private float spacing = 10f;
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

    public void ResetHealthUI()
    {
        lastHP = -1;
        lastMaxHP = -1;
        lastShieldCharges = -1;
        if (playerHealth != null && healthIcons != null)
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

        if (healthIconTemplate == null)
        {
            Debug.LogError("HealthUI: healthIconTemplate is not assigned.");
            gameObject.SetActive(false);
            return;
        }

        CreateHealthIcons();

        lastHP = playerHealth.CurrentHP;
        lastMaxHP = playerHealth.MaxHP;
        UpdateHealthUI();
    }

    private void CreateHealthIcons()
    {
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
            rectTransform.anchoredPosition = new Vector2(i * (rectTransform.rect.width + spacing), 0f);

            healthIcons[i] = newIcon;
            shieldIcons[i] = CreateShieldOverlay(newIcon, i);
        }
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
            CreateHealthIcons();
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
        if (playerHealth == null || healthIcons == null) return;

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
}
