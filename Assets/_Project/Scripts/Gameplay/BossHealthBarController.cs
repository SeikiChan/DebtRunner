using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BossHealthBarController : MonoBehaviour
{
    [Header("Boss Health Bar / Boss血条")]
    [SerializeField] private CanvasGroup bossHealthBarOverlay;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private Image bossHealthFillImage;
    [SerializeField] private TMP_Text bossHealthValueText;
    [SerializeField] private string defaultBossName = "BOSS";
    [SerializeField] private bool showBossHealthValueText = true;

    [Header("Fallback UI / 自动生成")]
    [SerializeField] private bool preferSceneOverlay = true;
    [SerializeField] private bool autoCreateOverlay = true;
    [SerializeField] private Vector2 fallbackPanelSize = new Vector2(920f, 88f);
    [SerializeField] private Vector2 fallbackAnchoredPosition = new Vector2(0f, -56f);
    [SerializeField] private Color fallbackBackdropColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color fallbackPanelColor = new Color(0.12f, 0.12f, 0.14f, 0.96f);
    [SerializeField] private Color fallbackFillColor = new Color(0.92f, 0.16f, 0.12f, 1f);
    [SerializeField] private Color fallbackBossNameColor = new Color(0.98f, 0.95f, 0.90f, 1f);
    [SerializeField] private Color fallbackHealthValueColor = new Color(0.98f, 0.95f, 0.90f, 1f);
    [SerializeField, Min(0.01f)] private float bossSearchIntervalSeconds = 0.2f;

    private GameObject panelHUD;
    private EnemyController trackedBoss;
    private float nextBossSearchTime = -10f;
    private bool overlayAutoCreated;

    public void BindCanvasSource(GameObject hudPanel)
    {
        panelHUD = hudPanel;
    }

    public void ClearTracking()
    {
        trackedBoss = null;
        HideOverlay();
    }

    private void LateUpdate()
    {
        if (GameFlowController.Instance != null && !GameFlowController.Instance.IsInGameplayState)
        {
            HideOverlay();
            return;
        }

        RefreshTrackedBoss();
        if (!IsBossAlive(trackedBoss))
        {
            HideOverlay();
            return;
        }

        if (!EnsureOverlay())
            return;

        UpdateOverlay(trackedBoss);
    }

    private void RefreshTrackedBoss()
    {
        if (IsBossAlive(trackedBoss))
            return;

        if (Time.unscaledTime < nextBossSearchTime)
            return;

        nextBossSearchTime = Time.unscaledTime + Mathf.Max(0.01f, bossSearchIntervalSeconds);
        trackedBoss = FindTrackedBoss();
    }

    private EnemyController FindTrackedBoss()
    {
        var activeEnemies = EnemyController.ActiveEnemies;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            EnemyController enemy = activeEnemies[i] as EnemyController;
            if (!IsBossAlive(enemy))
                continue;

            return enemy;
        }

        return null;
    }

    private static bool IsBossAlive(EnemyController enemy)
    {
        return enemy != null
            && enemy.isActiveAndEnabled
            && enemy.gameObject.activeInHierarchy
            && enemy.CurrentHP > 0f
            && enemy.GetComponent<BossAttackController>() != null;
    }

    private void UpdateOverlay(EnemyController boss)
    {
        if (bossHealthBarOverlay != null)
        {
            if (!bossHealthBarOverlay.gameObject.activeSelf)
                bossHealthBarOverlay.gameObject.SetActive(true);

            bossHealthBarOverlay.transform.SetAsLastSibling();
        }

        if (bossNameText != null)
            bossNameText.text = ResolveBossName(boss);

        if (bossHealthFillImage != null)
            bossHealthFillImage.fillAmount = boss.HealthRatio;

        if (bossHealthValueText != null)
        {
            bossHealthValueText.gameObject.SetActive(showBossHealthValueText);
            if (showBossHealthValueText)
                bossHealthValueText.text = $"{Mathf.CeilToInt(boss.CurrentHP)}/{Mathf.CeilToInt(boss.MaxHP)}";
        }
    }

    private string ResolveBossName(EnemyController boss)
    {
        if (!string.IsNullOrWhiteSpace(defaultBossName))
            return defaultBossName;

        string bossName = boss != null ? boss.gameObject.name : "BOSS";
        return bossName.Replace("(Clone)", string.Empty).Replace('_', ' ').Trim();
    }

    private void HideOverlay()
    {
        if (bossHealthBarOverlay == null)
            return;

        if (bossHealthBarOverlay.gameObject.activeSelf)
            bossHealthBarOverlay.gameObject.SetActive(false);
    }

    private bool EnsureOverlay()
    {
        TryBindSceneOverlayRefs();

        if (bossHealthBarOverlay != null && bossHealthFillImage != null)
            return true;

        if (!autoCreateOverlay || overlayAutoCreated)
            return false;

        Canvas canvas = panelHUD != null ? panelHUD.GetComponentInParent<Canvas>() : FindObjectOfType<Canvas>();
        if (canvas == null)
            return false;

        GameObject root = new GameObject("BossHealthBarOverlayAuto", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(canvas.transform, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image rootImage = root.GetComponent<Image>();
        rootImage.color = fallbackBackdropColor;
        rootImage.raycastTarget = false;

        bossHealthBarOverlay = root.GetComponent<CanvasGroup>();
        bossHealthBarOverlay.alpha = 1f;
        bossHealthBarOverlay.interactable = false;
        bossHealthBarOverlay.blocksRaycasts = false;

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(rootRect, false);
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.sizeDelta = fallbackPanelSize;
        panelRect.anchoredPosition = fallbackAnchoredPosition;

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = fallbackPanelColor;
        panelImage.raycastTarget = false;

        bossNameText = CreateText(
            "BossNameText",
            panelRect,
            new Vector2(0f, -18f),
            new Vector2(fallbackPanelSize.x - 80f, 28f),
            defaultBossName,
            28f,
            FontStyles.Bold,
            fallbackBossNameColor);

        GameObject barBg = new GameObject("BarBackground", typeof(RectTransform), typeof(Image));
        RectTransform barBgRect = barBg.GetComponent<RectTransform>();
        barBgRect.SetParent(panelRect, false);
        barBgRect.anchorMin = new Vector2(0.5f, 1f);
        barBgRect.anchorMax = new Vector2(0.5f, 1f);
        barBgRect.pivot = new Vector2(0.5f, 1f);
        barBgRect.sizeDelta = new Vector2(fallbackPanelSize.x - 80f, 26f);
        barBgRect.anchoredPosition = new Vector2(0f, -50f);

        Image barBgImage = barBg.GetComponent<Image>();
        barBgImage.color = new Color(0.19f, 0.19f, 0.21f, 1f);
        barBgImage.raycastTarget = false;

        GameObject fill = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.SetParent(barBgRect, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        bossHealthFillImage = fill.GetComponent<Image>();
        bossHealthFillImage.color = fallbackFillColor;
        bossHealthFillImage.type = Image.Type.Filled;
        bossHealthFillImage.fillMethod = Image.FillMethod.Horizontal;
        bossHealthFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        bossHealthFillImage.fillAmount = 1f;
        bossHealthFillImage.raycastTarget = false;

        bossHealthValueText = CreateText(
            "BossHealthValueText",
            barBgRect,
            Vector2.zero,
            barBgRect.sizeDelta,
            "0/0",
            22f,
            FontStyles.Bold,
            fallbackHealthValueColor);

        bossHealthBarOverlay.gameObject.SetActive(false);
        overlayAutoCreated = true;
        return true;
    }

    private void TryBindSceneOverlayRefs()
    {
        if (!preferSceneOverlay)
            return;

        if (bossHealthBarOverlay != null
            && bossNameText != null
            && bossHealthFillImage != null
            && bossHealthValueText != null)
            return;

        Transform overlayRoot = FindSceneTransform("Panel_BossHealthBar");
        if (overlayRoot == null)
            return;

        if (bossHealthBarOverlay == null)
            bossHealthBarOverlay = overlayRoot.GetComponent<CanvasGroup>();

        if (bossNameText == null)
        {
            Transform nameRoot = FindSceneChild(overlayRoot, "BossNameText");
            bossNameText = nameRoot != null ? nameRoot.GetComponent<TMP_Text>() : null;
        }

        if (bossHealthFillImage == null)
        {
            Transform fillRoot = FindSceneChild(overlayRoot, "BarFill");
            bossHealthFillImage = fillRoot != null ? fillRoot.GetComponent<Image>() : null;
        }

        if (bossHealthValueText == null)
        {
            Transform valueRoot = FindSceneChild(overlayRoot, "BossHealthValueText");
            bossHealthValueText = valueRoot != null ? valueRoot.GetComponent<TMP_Text>() : null;
        }
    }

    private static TMP_Text CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, string value, float fontSize, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        text.outlineWidth = 0.25f;
        return text;
    }

    private static Transform FindSceneTransform(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate.name != objectName)
                continue;

            Scene scene = candidate.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            return candidate;
        }

        return null;
    }

    private static Transform FindSceneChild(Transform root, string childName)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name == childName)
                return child;
        }

        return null;
    }
}
