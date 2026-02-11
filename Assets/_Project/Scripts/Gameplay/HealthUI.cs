using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image healthIconTemplate;
    [SerializeField] private float spacing = 10f; // 血量格子之间的间隔
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    
    // 血量变化时的简单动画选项
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float animationDuration = 0.2f;

    private Image[] healthIcons;
    private int lastHP = -1;

    /// <summary>重置血量UI显示</summary>
    public void ResetHealthUI()
    {
        lastHP = -1;
        if (playerHealth != null && healthIcons != null)
        {
            UpdateHealthUI();
        }
    }

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();
        
        if (playerHealth == null)
        {
            Debug.LogError("HealthUI: 找不到 PlayerHealth 组件");
            gameObject.SetActive(false);
            return;
        }

        if (healthIconTemplate == null)
        {
            Debug.LogError("HealthUI: 没有设置 healthIconTemplate");
            gameObject.SetActive(false);
            return;
        }

        // 根据最大血量复制血量格子
        CreateHealthIcons();
        
        // 初始化UI显示
        lastHP = playerHealth.CurrentHP;
        UpdateHealthUI();
    }

    private void CreateHealthIcons()
    {
        int maxHP = playerHealth.MaxHP;
        healthIcons = new Image[maxHP];

        // 隐藏模板
        healthIconTemplate.gameObject.SetActive(false);

        for (int i = 0; i < maxHP; i++)
        {
            // 复制模板
            Image newIcon = Instantiate(healthIconTemplate, transform);
            newIcon.gameObject.SetActive(true);
            newIcon.name = $"HealthIcon_{i + 1}";

            // 设置位置（横向排列）
            RectTransform rectTransform = newIcon.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(i * (rectTransform.rect.width + spacing), 0);

            healthIcons[i] = newIcon;
        }
    }

    private void Update()
    {
        if (playerHealth == null) return;

        int currentHP = playerHealth.CurrentHP;
        
        // 只在血量变化时更新
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
        
        // 更新血量UI
        for (int i = 0; i < healthIcons.Length; i++)
        {
            if (healthIcons[i] == null) continue;

            // 第i格血还有则显示为活跃色，否则显示为灰色
            if (i < currentHP)
            {
                // 血量存在 - 显示为活跃色
                if (useAnimation)
                    StartCoroutine(AnimateHealthIcon(healthIcons[i], activeColor));
                else
                    healthIcons[i].color = activeColor;
            }
            else
            {
                // 血量不存在 - 显示为灰色
                if (useAnimation)
                    StartCoroutine(AnimateHealthIcon(healthIcons[i], inactiveColor));
                else
                    healthIcons[i].color = inactiveColor;
            }
        }
    }

    private System.Collections.IEnumerator AnimateHealthIcon(Image icon, Color targetColor)
    {
        Color startColor = icon.color;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            icon.color = Color.Lerp(startColor, targetColor, elapsed / animationDuration);
            yield return null;
        }

        icon.color = targetColor;
    }
}


