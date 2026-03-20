using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

public static class RoundPresentationPanelTemplate
{
    [MenuItem("GameObject/DebtRunner/Create Times Up Panel")]
    public static void CreateTimesUpPanel()
    {
        CreateOverlay(
            "Panel_TimesUp",
            "TIME'S UP",
            subMessage: null,
            bindMode: BindMode.TimesUp);
    }

    [MenuItem("GameObject/DebtRunner/Create Game Over Transition Panel")]
    public static void CreateGameOverTransitionPanel()
    {
        CreateOverlay(
            "Panel_GameOverTransition",
            "YOU LOSS",
            "Run Failed",
            bindMode: BindMode.GameOver);
    }

    private enum BindMode
    {
        TimesUp,
        GameOver,
    }

    private static void CreateOverlay(string rootName, string titleMessage, string subMessage, BindMode bindMode)
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found. Create a Canvas first.", "OK");
            return;
        }

        Transform existing = canvas.transform.Find(rootName);
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Hint", $"{rootName} already exists in this Canvas.", "OK");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        GameObject root = new GameObject(rootName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(canvas.transform, false);
        Stretch(rootRect);

        Image backdrop = root.GetComponent<Image>();
        backdrop.color = bindMode == BindMode.TimesUp
            ? new Color(0f, 0f, 0f, 0.65f)
            : new Color(0f, 0f, 0f, 0.78f);
        backdrop.raycastTarget = false;

        CanvasGroup overlay = root.GetComponent<CanvasGroup>();
        overlay.alpha = 1f;
        overlay.interactable = false;
        overlay.blocksRaycasts = false;

        Color titleColor = bindMode == BindMode.TimesUp
            ? new Color(0.98f, 0.95f, 0.92f, 1f)
            : new Color(1f, 0.26f, 0.22f, 1f);

        TMP_Text titleText = CreateText(
            "Title",
            rootRect,
            new Vector2(0f, bindMode == BindMode.TimesUp ? 0f : 42f),
            new Vector2(1080f, bindMode == BindMode.TimesUp ? 220f : 180f),
            titleMessage,
            bindMode == BindMode.TimesUp ? 98f : 96f,
            FontStyles.Bold,
            titleColor);

        TMP_Text subText = null;
        if (!string.IsNullOrWhiteSpace(subMessage))
        {
            subText = CreateText(
                "SubText",
                rootRect,
                new Vector2(0f, -112f),
                new Vector2(1000f, 180f),
                subMessage,
                46f,
                FontStyles.Normal,
                Color.white);
        }

        root.SetActive(false);

        BindToRoundPresentation(root, overlay, titleText, subText, bindMode);

        Undo.RegisterCreatedObjectUndo(root, $"Create {rootName}");
        EditorGUIUtility.PingObject(root);
        Selection.activeGameObject = root;

        EditorUtility.DisplayDialog(
            "Created",
            $"{rootName} created and auto-bound to RoundPresentationController when possible.",
            "OK");
    }

    private static void BindToRoundPresentation(GameObject root, CanvasGroup overlay, TMP_Text titleText, TMP_Text subText, BindMode bindMode)
    {
        RoundPresentationController presentation = Object.FindObjectOfType<RoundPresentationController>();
        GameFlowController flow = Object.FindObjectOfType<GameFlowController>();

        if (presentation == null && flow != null)
            presentation = flow.GetComponent<RoundPresentationController>();
        if (presentation == null && flow != null)
            presentation = flow.gameObject.AddComponent<RoundPresentationController>();

        if (presentation == null)
            return;

        SerializedObject presentationSO = new SerializedObject(presentation);
        if (bindMode == BindMode.TimesUp)
        {
            presentationSO.FindProperty("timesUpOverlay").objectReferenceValue = overlay;
            presentationSO.FindProperty("timesUpTitleText").objectReferenceValue = titleText;
        }
        else
        {
            presentationSO.FindProperty("gameOverTransitionOverlay").objectReferenceValue = overlay;
            presentationSO.FindProperty("gameOverTransitionTitleText").objectReferenceValue = titleText;
            presentationSO.FindProperty("gameOverTransitionSubText").objectReferenceValue = subText;
        }

        presentationSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(presentation);

        if (flow != null)
        {
            SerializedObject flowSO = new SerializedObject(flow);
            SerializedProperty roundPresentationProp = flowSO.FindProperty("roundPresentation");
            if (roundPresentationProp != null)
                roundPresentationProp.objectReferenceValue = presentation;
            flowSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flow);
        }
    }

    private static TMP_Text CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, string textValue, float fontSize, FontStyles fontStyle, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
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
