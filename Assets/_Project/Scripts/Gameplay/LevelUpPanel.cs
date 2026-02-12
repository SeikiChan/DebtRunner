using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 升级奖励面板管理（三选一）
/// 关键点：不要把挂脚本的 GameObject SetActive(false)，否则协程无法启动。
/// 这里用 CanvasGroup 隐藏/显示，避免 "Coroutine couldn't be started... inactive"。
/// </summary>
public class LevelUpPanel : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private GameObject panel;                 // 建议指向一个子物体 Content（包含 DimBackground/CardContainer）
    [SerializeField] private Image dimBackground;
    [SerializeField] private UpgradeCard[] cardSlots = new UpgradeCard[3];

    [Header("Dim Settings")]
    [SerializeField] private float dimTargetAlpha = 0.5f;
    [SerializeField] private float dimFadeDuration = 0.3f;

    private Action<WeaponUpgrade> onUpgradeSelected;
    private CanvasGroup canvasGroup;
    private Coroutine fadeCo;

    private void Awake()
    {
        // 兜底：如果没填 panel，就当成自己（但不会再 SetActive(false)）
        if (panel == null)
            panel = gameObject;

        // CanvasGroup 挂在“脚本所在物体”上，保证即使 panel 是自己也能隐藏显示
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        ForceHideImmediate();
    }

    /// <summary>
    /// 立刻隐藏（不启动协程、不依赖 activeInHierarchy）。
    /// 用于 StartRun/切状态时清理。
    /// </summary>
    public void ForceHideImmediate()
    {
        if (fadeCo != null)
        {
            StopCoroutine(fadeCo);
            fadeCo = null;
        }

        // 直接把遮罩归零
        if (dimBackground != null)
        {
            var c = dimBackground.color;
            c.a = 0f;
            dimBackground.color = c;
        }

        // 隐藏内容（如果 panel 是子物体可以一起关掉；如果是自己，保持 active 但不可见）
        if (panel != null && panel != gameObject)
            panel.SetActive(false);

        SetVisible(false);
    }

    /// <summary>
    /// 显示升级面板（三选一）
    /// </summary>
    public void ShowUpgradePanel(WeaponUpgrade[] upgrades, Action<WeaponUpgrade> onSelected)
    {
        onUpgradeSelected = onSelected;

        if (upgrades == null || upgrades.Length < 3)
        {
            Debug.LogError("LevelUpPanel: 至少需要3个升级选项");
            return;
        }

        // 先保证可见/可交互
        if (panel != null && panel != gameObject)
            panel.SetActive(true);

        SetVisible(true);

        // 配置卡牌
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i] != null)
                cardSlots[i].SetupCard(upgrades[i], OnCardSelected);
        }

        // 暗化背景淡入（用 unscaledDeltaTime，暂停也能跑）
        if (dimBackground != null)
        {
            StartFade(0f, dimTargetAlpha, dimFadeDuration);
        }
    }

    /// <summary>
    /// 隐藏升级面板（淡出）
    /// </summary>
    public void HideUpgradePanel()
    {
        // 如果对象不在层级激活状态，协程一定启动不了，直接强制隐藏
        if (!gameObject.activeInHierarchy)
        {
            ForceHideImmediate();
            return;
        }

        if (dimBackground != null)
        {
            // 淡出结束后再隐藏
            if (fadeCo != null) StopCoroutine(fadeCo);
            fadeCo = StartCoroutine(HideRoutine());
        }
        else
        {
            ForceHideImmediate();
        }
    }

    private IEnumerator HideRoutine()
    {
        yield return FadeDim(dimTargetAlpha, 0f, dimFadeDuration);

        if (panel != null && panel != gameObject)
            panel.SetActive(false);

        SetVisible(false);
        fadeCo = null;
    }

    private void OnCardSelected(WeaponUpgrade upgrade)
    {
        HideUpgradePanel();
        onUpgradeSelected?.Invoke(upgrade);
    }

    private void StartFade(float from, float to, float duration)
    {
        if (!gameObject.activeInHierarchy)
        {
            var c = dimBackground.color;
            c.a = to;
            dimBackground.color = c;
            return;
        }

        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(FadeDim(from, to, duration));
    }

    /// <summary>
    /// 屏幕暗化效果（unscaled）
    /// </summary>
    private IEnumerator FadeDim(float startAlpha, float endAlpha, float duration)
    {
        if (dimBackground == null) yield break;

        float elapsed = 0f;

        Color startColor = dimBackground.color;
        startColor.a = startAlpha;

        Color endColor = dimBackground.color;
        endColor.a = endAlpha;

        dimBackground.color = startColor;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            dimBackground.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        dimBackground.color = endColor;
        fadeCo = null;
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
