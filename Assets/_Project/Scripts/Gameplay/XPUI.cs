using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class XPUI : MonoBehaviour
{
    [SerializeField] private Image xpBarFill;
    [SerializeField] private Image xpBarBackground;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text xpText;
    [SerializeField] private Color fillColor = Color.yellow;
    [SerializeField] private Color levelUpFlashColor = Color.white;
    
    [Header("动画设置")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float fillAnimationDuration = 0.2f;
    [SerializeField] private float levelUpFlashDuration = 0.3f;

    private float lastFillAmount = 0f;
    private int lastLevel = 1;
    private Coroutine animationCo;
    private Coroutine levelUpFlashCo;

    private void Start()
    {
        if (xpBarFill == null)
        {
            Debug.LogError("XPUI: 没有设置 xpBarFill Image");
            gameObject.SetActive(false);
            return;
        }

        // 初始化颜色
        if (xpBarFill != null)
            xpBarFill.color = fillColor;

        lastLevel = GameFlowController.Instance.GetLevel();
        UpdateXPDisplay();
    }

    /// <summary>
    /// 更新经验条显示
    /// </summary>
    public void UpdateXPDisplay()
    {
        if (GameFlowController.Instance == null) return;

        int currentXP = GameFlowController.Instance.GetCurrentXP();
        int xpToNext = GameFlowController.Instance.GetXPToNext();
        int level = GameFlowController.Instance.GetLevel();

        // 计算进度百分比
        float fillAmount = xpToNext > 0 ? (float)currentXP / xpToNext : 1f;
        fillAmount = Mathf.Clamp01(fillAmount);

        // 检测升级
        bool leveledUp = level > lastLevel;
        if (leveledUp)
        {
            lastLevel = level;
            OnLevelUp();
        }

        // 更新等级文字
        if (levelText != null)
            levelText.text = $"Lv.{level}";

        // 更新XP文字
        if (xpText != null)
            xpText.text = $"{currentXP}/{xpToNext}";

        // 更新进度条
        if (xpBarFill != null)
        {
            if (useAnimation && fillAmount != lastFillAmount)
            {
                // 停止之前的动画
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
    }

    /// <summary>
    /// 升级时的特效
    /// </summary>
    private void OnLevelUp()
    {
        if (levelUpFlashCo != null)
            StopCoroutine(levelUpFlashCo);

        levelUpFlashCo = StartCoroutine(LevelUpFlash());
    }

    /// <summary>
    /// 升级闪烁效果
    /// </summary>
    private System.Collections.IEnumerator LevelUpFlash()
    {
        if (xpBarFill == null) yield break;

        Color originalColor = fillColor;
        float elapsed = 0f;

        // 闪烁3次
        for (int i = 0; i < 3; i++)
        {
            // 闪白色
            while (elapsed < levelUpFlashDuration / 2)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (levelUpFlashDuration / 2);
                xpBarFill.color = Color.Lerp(originalColor, levelUpFlashColor, t);
                yield return null;
            }

            elapsed = 0f;

            // 闪回原色
            while (elapsed < levelUpFlashDuration / 2)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (levelUpFlashDuration / 2);
                xpBarFill.color = Color.Lerp(levelUpFlashColor, originalColor, t);
                yield return null;
            }

            elapsed = 0f;
        }

        xpBarFill.color = originalColor;

        // 升级后进度条重置为空，立即更新显示
        if (xpBarFill != null)
            xpBarFill.fillAmount = GameFlowController.Instance.GetCurrentXP() / (float)GameFlowController.Instance.GetXPToNext();
    }

    /// <summary>
    /// 经验条填充动画
    /// </summary>
    private System.Collections.IEnumerator AnimateFill(float startFill, float targetFill)
    {
        float elapsed = 0f;

        while (elapsed < fillAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float currentFill = Mathf.Lerp(startFill, targetFill, elapsed / fillAnimationDuration);
            if (xpBarFill != null)
                xpBarFill.fillAmount = currentFill;
            yield return null;
        }

        if (xpBarFill != null)
            xpBarFill.fillAmount = targetFill;
    }
}

