using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Creates an editable story intro panel and auto-binds references on GameFlowController.
/// </summary>
public static class StoryIntroPanelTemplate
{
    [MenuItem("GameObject/DebtRunner/Create Story Intro Panel Template")]
    public static void CreateStoryIntroPanelTemplate()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found. Create a Canvas first.", "OK");
            return;
        }

        const string panelName = "Panel_StoryIntro";
        Transform existing = canvas.transform.Find(panelName);
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Hint", $"{panelName} already exists.", "OK");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        GameObject panelRoot = new GameObject(panelName, typeof(RectTransform), typeof(CanvasGroup));
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.SetParent(canvas.transform, false);
        Stretch(panelRect);

        CanvasGroup group = panelRoot.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        Image bg = CreateImage("Background", panelRect, new Color(0f, 0f, 0f, 0.9f));
        Stretch(bg.rectTransform);

        Image page = CreateImage("PageImage", panelRect, Color.white);
        RectTransform pageRect = page.rectTransform;
        pageRect.anchorMin = Vector2.zero;
        pageRect.anchorMax = Vector2.one;
        pageRect.offsetMin = new Vector2(32f, 128f);
        pageRect.offsetMax = new Vector2(-32f, -96f);
        page.preserveAspect = true;

        TMP_Text hint = CreateText(
            "StoryHintText",
            panelRect,
            new Vector2(0f, -492f),
            new Vector2(1400f, 120f),
            "Click to continue",
            34f);
        hint.fontStyle = FontStyles.Normal;
        hint.enableWordWrapping = true;

        GameObject skipObj = new GameObject("Btn_StorySkip", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform skipRect = skipObj.GetComponent<RectTransform>();
        skipRect.SetParent(panelRect, false);
        skipRect.anchorMin = new Vector2(1f, 1f);
        skipRect.anchorMax = new Vector2(1f, 1f);
        skipRect.pivot = new Vector2(1f, 1f);
        skipRect.sizeDelta = new Vector2(220f, 66f);
        skipRect.anchoredPosition = new Vector2(-44f, -38f);

        Image skipImage = skipObj.GetComponent<Image>();
        skipImage.color = new Color(0f, 0f, 0f, 0.72f);
        Button skipBtn = skipObj.GetComponent<Button>();
        skipBtn.targetGraphic = skipImage;

        TMP_Text skipText = CreateText(
            "Text",
            skipRect,
            Vector2.zero,
            new Vector2(220f, 66f),
            "Skip Story",
            30f);
        skipText.fontStyle = FontStyles.Normal;

        BindToGameFlow(group, bg, page, hint, skipBtn, skipText);

        // Keep visible for artist/designer adjustments.
        panelRoot.SetActive(true);

        Undo.RegisterCreatedObjectUndo(panelRoot, "Create Story Intro Panel");
        EditorGUIUtility.PingObject(panelRoot);
        Selection.activeGameObject = panelRoot;

        EditorUtility.DisplayDialog(
            "Created",
            "Story intro panel template created and references auto-bound to GameFlowController.",
            "OK");
    }

    private static void BindToGameFlow(
        CanvasGroup overlay,
        Image background,
        Image pageImage,
        TMP_Text hintText,
        Button skipButton,
        TMP_Text skipButtonText)
    {
        GameFlowController flow = Object.FindObjectOfType<GameFlowController>();
        if (flow == null)
            return;

        SerializedObject so = new SerializedObject(flow);
        AssignRefIfPropertyExists(so, "storyIntroOverlay", overlay);
        AssignRefIfPropertyExists(so, "storyIntroBackground", background);
        AssignRefIfPropertyExists(so, "storyIntroPageImage", pageImage);
        AssignRefIfPropertyExists(so, "storyIntroHintText", hintText);
        AssignRefIfPropertyExists(so, "storyIntroSkipButton", skipButton);
        AssignRefIfPropertyExists(so, "storyIntroSkipButtonText", skipButtonText);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(flow);
    }

    private static void AssignRefIfPropertyExists(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
            prop.objectReferenceValue = value;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        Vector2 pos,
        Vector2 size,
        string text,
        float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        tmp.raycastTarget = false;
        return tmp;
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
