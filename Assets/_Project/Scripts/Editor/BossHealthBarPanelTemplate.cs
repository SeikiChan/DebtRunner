using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

public static class BossHealthBarPanelTemplate
{
    [MenuItem("GameObject/DebtRunner/Create Boss Health Bar Panel")]
    public static void CreateBossHealthBarPanel()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found. Create a Canvas first.", "OK");
            return;
        }

        Transform existing = canvas.transform.Find("Panel_BossHealthBar");
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Hint", "Panel_BossHealthBar already exists in this Canvas.", "OK");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        GameObject root = new GameObject("Panel_BossHealthBar", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(canvas.transform, false);
        Stretch(rootRect);

        Image backdrop = root.GetComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0f);
        backdrop.raycastTarget = false;

        CanvasGroup overlay = root.GetComponent<CanvasGroup>();
        overlay.alpha = 1f;
        overlay.interactable = false;
        overlay.blocksRaycasts = false;

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(rootRect, false);
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.sizeDelta = new Vector2(920f, 88f);
        panelRect.anchoredPosition = new Vector2(0f, -56f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.12f, 0.12f, 0.14f, 0.96f);
        panelImage.raycastTarget = false;

        TMP_Text bossNameText = CreateText(
            "BossNameText",
            panelRect,
            new Vector2(0f, -18f),
            new Vector2(840f, 28f),
            "BOSS",
            28f,
            FontStyles.Bold,
            new Color(0.98f, 0.95f, 0.90f, 1f));

        GameObject barBg = new GameObject("BarBackground", typeof(RectTransform), typeof(Image));
        RectTransform barBgRect = barBg.GetComponent<RectTransform>();
        barBgRect.SetParent(panelRect, false);
        barBgRect.anchorMin = new Vector2(0.5f, 1f);
        barBgRect.anchorMax = new Vector2(0.5f, 1f);
        barBgRect.pivot = new Vector2(0.5f, 1f);
        barBgRect.sizeDelta = new Vector2(840f, 26f);
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

        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color(0.92f, 0.16f, 0.12f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
        fillImage.raycastTarget = false;

        TMP_Text hpValueText = CreateText(
            "BossHealthValueText",
            barBgRect,
            Vector2.zero,
            new Vector2(840f, 26f),
            "0/0",
            22f,
            FontStyles.Bold,
            new Color(0.98f, 0.95f, 0.90f, 1f));

        root.SetActive(false);

        BossHealthBarController healthBar = Object.FindObjectOfType<BossHealthBarController>();
        GameFlowController flow = Object.FindObjectOfType<GameFlowController>();
        if (healthBar == null && flow != null)
            healthBar = flow.GetComponent<BossHealthBarController>();
        if (healthBar == null && flow != null)
            healthBar = flow.gameObject.AddComponent<BossHealthBarController>();

        if (healthBar != null)
        {
            SerializedObject healthBarSO = new SerializedObject(healthBar);
            healthBarSO.FindProperty("bossHealthBarOverlay").objectReferenceValue = overlay;
            healthBarSO.FindProperty("bossNameText").objectReferenceValue = bossNameText;
            healthBarSO.FindProperty("bossHealthFillImage").objectReferenceValue = fillImage;
            healthBarSO.FindProperty("bossHealthValueText").objectReferenceValue = hpValueText;
            healthBarSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(healthBar);
        }

        if (flow != null)
        {
            SerializedObject flowSO = new SerializedObject(flow);
            SerializedProperty healthBarProp = flowSO.FindProperty("bossHealthBar");
            if (healthBarProp != null && healthBar != null)
                healthBarProp.objectReferenceValue = healthBar;
            flowSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flow);
        }

        Undo.RegisterCreatedObjectUndo(root, "Create Boss Health Bar Panel");
        EditorGUIUtility.PingObject(root);
        Selection.activeGameObject = root;

        EditorUtility.DisplayDialog(
            "Created",
            "Panel_BossHealthBar created and auto-bound to BossHealthBarController when possible.",
            "OK");
    }

    private static TMP_Text CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, string textValue, float fontSize, FontStyles fontStyle, Color color)
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
        text.text = textValue;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        text.outlineWidth = 0.25f;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
#endif
