using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image healthIconTemplate;
    [SerializeField] private float spacing = 10f;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float animationDuration = 0.2f;

    private Image[] healthIcons;
    private int lastHP = -1;
    private int lastMaxHP = -1;

    public void ResetHealthUI()
    {
        lastHP = -1;
        lastMaxHP = -1;
        if (playerHealth != null && healthIcons != null)
            UpdateHealthUI();
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

        healthIconTemplate.gameObject.SetActive(false);

        for (int i = 0; i < maxHP; i++)
        {
            Image newIcon = Instantiate(healthIconTemplate, transform);
            newIcon.gameObject.SetActive(true);
            newIcon.name = $"HealthIcon_{i + 1}";

            RectTransform rectTransform = newIcon.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(i * (rectTransform.rect.width + spacing), 0f);

            healthIcons[i] = newIcon;
        }
    }

    private void Update()
    {
        if (playerHealth == null) return;

        int currentHP = playerHealth.CurrentHP;
        int currentMaxHP = playerHealth.MaxHP;

        if (currentMaxHP != lastMaxHP)
        {
            lastMaxHP = currentMaxHP;
            CreateHealthIcons();
            lastHP = -1;
        }

        if (currentHP != lastHP)
        {
            lastHP = currentHP;
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
    }

    private System.Collections.IEnumerator AnimateHealthIcon(Image icon, Color targetColor)
    {
        if (icon == null) yield break;

        Color startColor = icon.color;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            icon.color = Color.Lerp(startColor, targetColor, Mathf.Clamp01(elapsed / animationDuration));
            yield return null;
        }

        icon.color = targetColor;
    }
}
