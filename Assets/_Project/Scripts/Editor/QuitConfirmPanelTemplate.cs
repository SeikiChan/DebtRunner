using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

public static class QuitConfirmPanelTemplate
{
    [MenuItem("GameObject/DebtRunner/Create Pause Quit Confirm Panel")]
    public static void CreatePauseQuitConfirmPanel()
    {
        CreatePanel(
            "Panel_QuitConfirm_Pause",
            "QUIT RUN?",
            "Return to the title screen?\nYour current run progress will be lost.",
            bindPausePanel: true);
    }

    [MenuItem("GameObject/DebtRunner/Create Title Exit Confirm Panel")]
    public static void CreateTitleExitConfirmPanel()
    {
        CreatePanel(
            "Panel_QuitConfirm_TitleExit",
            "EXIT GAME?",
            "Close the game now?",
            bindPausePanel: false);
    }

    private static void CreatePanel(string rootName, string title, string body, bool bindPausePanel)
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
        backdrop.color = new Color(0f, 0f, 0f, 0.72f);
        backdrop.raycastTarget = true;

        CanvasGroup overlay = root.GetComponent<CanvasGroup>();
        overlay.alpha = 1f;
        overlay.interactable = true;
        overlay.blocksRaycasts = true;

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(rootRect, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(720f, 320f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.11f, 0.12f, 0.14f, 0.97f);
        panelImage.raycastTarget = true;

        TMP_Text titleText = CreateText(
            "Title",
            panelRect,
            new Vector2(0f, 78f),
            new Vector2(620f, 72f),
            title,
            40f,
            FontStyles.Bold,
            new Color(0.98f, 0.95f, 0.88f, 1f));

        TMP_Text bodyText = CreateText(
            "Body",
            panelRect,
            new Vector2(0f, 8f),
            new Vector2(620f, 96f),
            body,
            26f,
            FontStyles.Normal,
            new Color(0.9f, 0.9f, 0.9f, 1f));
        bodyText.enableWordWrapping = true;

        Button confirmButton = CreateButton(
            "Btn_QuitConfirm",
            panelRect,
            new Vector2(-120f, -96f),
            new Vector2(220f, 68f),
            "QUIT",
            new Color(0.73f, 0.22f, 0.18f, 1f));

        Button cancelButton = CreateButton(
            "Btn_QuitCancel",
            panelRect,
            new Vector2(120f, -96f),
            new Vector2(220f, 68f),
            "CANCEL",
            new Color(0.23f, 0.27f, 0.32f, 1f));

        QuitConfirmDialog dialog = Object.FindObjectOfType<QuitConfirmDialog>();
        GameFlowController flow = Object.FindObjectOfType<GameFlowController>();
        if (dialog == null && flow != null)
            dialog = flow.GetComponent<QuitConfirmDialog>();
        if (dialog == null && flow != null)
            dialog = flow.gameObject.AddComponent<QuitConfirmDialog>();

        if (dialog != null)
        {
            SerializedObject dialogSO = new SerializedObject(dialog);
            dialogSO.FindProperty(bindPausePanel ? "pauseQuitPanelRoot" : "titleExitPanelRoot").objectReferenceValue = root;

            if (flow != null)
            {
                SerializedObject flowSO = new SerializedObject(flow);
                SerializedProperty quitDialogProp = flowSO.FindProperty("quitConfirmDialog");
                if (quitDialogProp != null)
                    quitDialogProp.objectReferenceValue = dialog;
                flowSO.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(flow);
            }

            dialogSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dialog);
        }

        Undo.RegisterCreatedObjectUndo(root, bindPausePanel ? "Create Pause Quit Confirm Panel" : "Create Title Exit Confirm Panel");
        EditorGUIUtility.PingObject(root);
        Selection.activeGameObject = root;

        EditorUtility.DisplayDialog(
            "Created",
            $"{rootName} created in the scene and auto-bound to QuitConfirmDialog when possible.",
            "OK");
    }

    private static Button CreateButton(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, string label, Color fillColor)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = fillColor;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 0.9f);
        colors.selectedColor = new Color(1f, 1f, 1f, 0.96f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
        button.colors = colors;

        CreateText(
            "Text",
            rect,
            Vector2.zero,
            size,
            label,
            28f,
            FontStyles.Bold,
            new Color(0.98f, 0.97f, 0.93f, 1f));

        return button;
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
