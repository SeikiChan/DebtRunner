using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Creates a run summary panel and auto-binds references on RunSummaryPanel + GameFlowController.
/// Menu: GameObject > DebtRunner > Create Run Summary Panel
/// </summary>
public static class RunSummaryPanelTemplate
{
    [MenuItem("GameObject/DebtRunner/Create Run Summary Panel")]
    public static void CreateRunSummaryPanel()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found. Create a Canvas first.", "OK");
            return;
        }

        const string panelName = "Panel_RunSummary";
        Transform existing = canvas.transform.Find(panelName);
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Hint", $"{panelName} already exists.", "OK");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // Root panel
        GameObject panelRoot = new GameObject(panelName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.SetParent(canvas.transform, false);
        Stretch(panelRect);

        Image rootBg = panelRoot.GetComponent<Image>();
        rootBg.color = new Color(0f, 0f, 0f, 0.75f);
        rootBg.raycastTarget = true;

        CanvasGroup cg = panelRoot.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        RunSummaryPanel summaryComp = panelRoot.AddComponent<RunSummaryPanel>();
        panelRoot.SetActive(false);

        // Content container (centered box)
        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(Image));
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.SetParent(panelRect, false);
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(500f, 420f);
        Image contentBg = content.GetComponent<Image>();
        contentBg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

        float yPos = 170f;

        // Title text (VICTORY / DEFEATED)
        TMP_Text titleText = CreateTMPText("TitleText", contentRect, "RUN SUMMARY", 36, FontStyles.Bold);
        RectTransform titleRect = ((RectTransform)titleText.transform);
        titleRect.anchoredPosition = new Vector2(0f, yPos);
        titleRect.sizeDelta = new Vector2(460f, 50f);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.85f, 0.2f, 1f);
        yPos -= 55f;

        // Result text (subtitle)
        TMP_Text resultText = CreateTMPText("ResultText", contentRect, "You cleared all debts!", 20, FontStyles.Normal);
        RectTransform resultRect = ((RectTransform)resultText.transform);
        resultRect.anchoredPosition = new Vector2(0f, yPos);
        resultRect.sizeDelta = new Vector2(460f, 36f);
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        yPos -= 50f;

        // Separator
        GameObject sep = new GameObject("Separator", typeof(RectTransform), typeof(Image));
        RectTransform sepRect = sep.GetComponent<RectTransform>();
        sepRect.SetParent(contentRect, false);
        sepRect.anchoredPosition = new Vector2(0f, yPos);
        sepRect.sizeDelta = new Vector2(400f, 2f);
        sep.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
        yPos -= 20f;

        // Stats text (monospaced-style)
        TMP_Text statsText = CreateTMPText("StatsText", contentRect,
            "Enemies Killed:    0\nCash Earned:       $0\nXP Earned:         0\nRounds Survived:   0\nFinal Level:       1",
            18, FontStyles.Normal);
        RectTransform statsRect = ((RectTransform)statsText.transform);
        statsRect.anchoredPosition = new Vector2(0f, yPos - 50f);
        statsRect.sizeDelta = new Vector2(420f, 150f);
        statsText.alignment = TextAlignmentOptions.Left;
        statsText.color = Color.white;
        statsText.lineSpacing = 8f;

        // Buttons row
        float btnY = -165f;
        float btnWidth = 180f;
        float btnHeight = 45f;
        float btnSpacing = 20f;

        Button restartBtn = CreateButton("Btn_Restart", contentRect, "RESTART",
            new Vector2(-btnWidth / 2f - btnSpacing / 2f, btnY), new Vector2(btnWidth, btnHeight),
            new Color(0.2f, 0.7f, 0.3f, 1f));

        Button menuBtn = CreateButton("Btn_MainMenu", contentRect, "MAIN MENU",
            new Vector2(btnWidth / 2f + btnSpacing / 2f, btnY), new Vector2(btnWidth, btnHeight),
            new Color(0.4f, 0.4f, 0.45f, 1f));

        // Auto-bind references via SerializedObject
        SerializedObject so = new SerializedObject(summaryComp);
        so.FindProperty("titleText").objectReferenceValue = titleText;
        so.FindProperty("resultText").objectReferenceValue = resultText;
        so.FindProperty("statsText").objectReferenceValue = statsText;
        so.FindProperty("restartButton").objectReferenceValue = restartBtn;
        so.FindProperty("mainMenuButton").objectReferenceValue = menuBtn;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Auto-bind to GameFlowController if present
        GameFlowController gfc = Object.FindObjectOfType<GameFlowController>();
        if (gfc != null)
        {
            SerializedObject gfcSo = new SerializedObject(gfc);
            SerializedProperty prop = gfcSo.FindProperty("runSummaryPanel");
            if (prop != null)
            {
                prop.objectReferenceValue = summaryComp;
                gfcSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        Undo.RegisterCreatedObjectUndo(panelRoot, "Create Run Summary Panel");
        Selection.activeGameObject = panelRoot;
        EditorUtility.DisplayDialog("Done", $"{panelName} created.\nDrag into GameFlowController.runSummaryPanel if not auto-bound.", "OK");
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static TMP_Text CreateTMPText(string name, RectTransform parent, string text, int fontSize, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.enableAutoSizing = false;
        tmp.raycastTarget = false;

        return tmp;
    }

    private static Button CreateButton(string name, RectTransform parent, string label, Vector2 pos, Vector2 size, Color bgColor)
    {
        GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform btnRect = btnGo.GetComponent<RectTransform>();
        btnRect.SetParent(parent, false);
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = pos;
        btnRect.sizeDelta = size;

        Image btnImg = btnGo.GetComponent<Image>();
        btnImg.color = bgColor;

        Button btn = btnGo.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = bgColor;
        cb.highlightedColor = new Color(
            Mathf.Min(1f, bgColor.r * 1.4f),
            Mathf.Min(1f, bgColor.g * 1.4f),
            Mathf.Min(1f, bgColor.b * 1.4f), 1f);
        cb.selectedColor = cb.highlightedColor;
        cb.pressedColor = bgColor * 0.75f;
        cb.fadeDuration = 0f;
        btn.colors = cb;

        TMP_Text btnLabel = CreateTMPText("Label", btnRect, label, 20, FontStyles.Bold);
        RectTransform labelRect = ((RectTransform)btnLabel.transform);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        btnLabel.alignment = TextAlignmentOptions.Center;
        btnLabel.color = Color.white;

        return btn;
    }
}
#endif
