using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;

/// <summary>
/// One-click credits panel generator.
/// </summary>
public static class CreditsPanelTemplate
{
    private struct CreditEntry
    {
        public string Role;
        public string Name;

        public CreditEntry(string role, string name)
        {
            Role = role;
            Name = name;
        }
    }

    private static readonly CreditEntry[] Entries =
    {
        new CreditEntry("Art Director / Game Designer / Programmer / SFX Designer", "Yilun Huang"),
        new CreditEntry("UI Designer", "Yipu Yuan"),
        new CreditEntry("Character Designer / Prop Designer", "Ziqi Wang"),
        new CreditEntry("Environment Designer", "Lilit Baghdasaryan"),
        new CreditEntry("Mentor Support", "Todd Gorang"),
    };

    [MenuItem("GameObject/DebtRunner/Create Credits Panel Template")]
    public static void CreateCreditsPanelTemplate()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found. Create a Canvas first.", "OK");
            return;
        }

        const string panelName = "Panel_Credits";
        Transform existing = canvas.transform.Find(panelName);
        if (existing != null)
        {
            bool updated = TryUpdateExistingCreditsTexts(existing);
            EditorUtility.DisplayDialog(
                "Hint",
                updated
                    ? $"{panelName} already exists.\nTexts were updated to English."
                    : $"{panelName} already exists.",
                "OK");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        GameObject panelRoot = new GameObject(panelName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.SetParent(canvas.transform, false);
        Stretch(panelRect);

        Image rootImage = panelRoot.GetComponent<Image>();
        rootImage.color = new Color(0f, 0f, 0f, 0f);
        rootImage.raycastTarget = false;

        RectTransform bgRect = CreateRect("Background", panelRect, Vector2.zero, Vector2.zero);
        Stretch(bgRect);
        Image bgImage = bgRect.gameObject.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.74f);
        bgImage.raycastTarget = true;

        RectTransform frameRect = CreateRect("Frame", panelRect, new Vector2(0f, 0f), new Vector2(1260f, 700f));
        Image frameImage = frameRect.gameObject.AddComponent<Image>();
        frameImage.color = new Color(0.07f, 0.07f, 0.10f, 0.96f);
        frameImage.raycastTarget = false;
        AddGoldOutline(frameRect.gameObject, 5f);

        TMP_Text title = CreateText(
            "Title",
            frameRect,
            new Vector2(0f, 268f),
            new Vector2(980f, 88f),
            "CREDITS",
            76f,
            FontStyles.Bold,
            new Color(0.95f, 0.85f, 0.58f, 1f),
            TextAlignmentOptions.Center);
        title.outlineWidth = 0.16f;
        title.outlineColor = new Color(0f, 0f, 0f, 0.95f);

        CreateText(
            "Subtitle",
            frameRect,
            new Vector2(0f, 214f),
            new Vector2(980f, 44f),
            "Debt Runner Team",
            34f,
            FontStyles.Normal,
            new Color(0.86f, 0.86f, 0.90f, 1f),
            TextAlignmentOptions.Center);

        RectTransform listRect = CreateRect("CreditsList", frameRect, new Vector2(0f, 22f), new Vector2(1100f, 420f));
        Image listBg = listRect.gameObject.AddComponent<Image>();
        listBg.color = new Color(0.09f, 0.09f, 0.12f, 0.92f);
        listBg.raycastTarget = false;
        AddGoldOutline(listRect.gameObject, 3f);

        float y = 146f;
        for (int i = 0; i < Entries.Length; i++)
        {
            CreateCreditsRow(listRect, Entries[i], y);
            if (i < Entries.Length - 1)
                CreateSeparator(listRect, y - 34f, 1020f);
            y -= 72f;
        }

        Button btnBack = CreateButton(
            "Btn_CreditsBack",
            frameRect,
            new Vector2(0f, -280f),
            new Vector2(300f, 74f),
            "BACK",
            new Color(0.94f, 0.66f, 0.24f, 1f),
            new Color(0.14f, 0.09f, 0.03f, 1f));

        BindToGameFlowController(panelRoot, btnBack);

        // Keep visible in edit mode for easy layout/material tweaks.
        panelRoot.SetActive(true);

        Undo.RegisterCreatedObjectUndo(panelRoot, "Create Credits Panel");
        EditorGUIUtility.PingObject(panelRoot);
        Selection.activeGameObject = panelRoot;

        EditorUtility.DisplayDialog(
            "Created",
            "Credits panel created.\n\n" +
            "- Panel_Credits bound to GameFlowController.panelCredits\n" +
            "- BACK button bound to GameFlowController.CloseCredits()\n\n" +
            "If you have a title button for credits, bind it to OpenCredits().",
            "OK");
    }

    private static void BindToGameFlowController(GameObject panelRoot, Button btnBack)
    {
        GameFlowController flow = Object.FindObjectOfType<GameFlowController>();
        if (flow == null)
            return;

        SerializedObject so = new SerializedObject(flow);
        SerializedProperty panelCreditsProp = so.FindProperty("panelCredits");
        if (panelCreditsProp != null)
            panelCreditsProp.objectReferenceValue = panelRoot;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(flow);

        for (int i = btnBack.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(btnBack.onClick, i);
        UnityEventTools.AddPersistentListener(btnBack.onClick, flow.CloseCredits);
        EditorUtility.SetDirty(btnBack);
    }

    private static bool TryUpdateExistingCreditsTexts(Transform panelRoot)
    {
        if (panelRoot == null)
            return false;

        bool updatedAny = false;
        Transform list = panelRoot.Find("Frame/CreditsList");
        if (list == null)
            return false;

        for (int i = 0; i < Entries.Length; i++)
        {
            string key = Entries[i].Name.Replace(" ", string.Empty);
            Transform roleTf = list.Find($"Role_{key}");
            Transform nameTf = list.Find($"Name_{key}");

            if (roleTf != null)
            {
                TMP_Text roleText = roleTf.GetComponent<TMP_Text>();
                if (roleText != null)
                {
                    roleText.text = Entries[i].Role;
                    updatedAny = true;
                }
            }

            if (nameTf != null)
            {
                TMP_Text nameText = nameTf.GetComponent<TMP_Text>();
                if (nameText != null)
                {
                    nameText.text = Entries[i].Name;
                    updatedAny = true;
                }
            }
        }

        if (updatedAny)
            EditorUtility.SetDirty(panelRoot.gameObject);

        return updatedAny;
    }

    private static void CreateCreditsRow(Transform parent, CreditEntry entry, float y)
    {
        TMP_Text role = CreateText(
            $"Role_{entry.Name.Replace(" ", string.Empty)}",
            parent,
            new Vector2(-500f, y),
            new Vector2(690f, 58f),
            entry.Role,
            32f,
            FontStyles.Bold,
            new Color(0.95f, 0.84f, 0.56f, 1f),
            TextAlignmentOptions.Left);
        role.enableAutoSizing = true;
        role.fontSizeMin = 22f;
        role.fontSizeMax = 32f;

        TMP_Text name = CreateText(
            $"Name_{entry.Name.Replace(" ", string.Empty)}",
            parent,
            new Vector2(330f, y),
            new Vector2(350f, 58f),
            entry.Name,
            36f,
            FontStyles.Bold,
            new Color(0.22f, 0.95f, 0.85f, 1f),
            TextAlignmentOptions.Right);
        name.enableAutoSizing = true;
        name.fontSizeMin = 24f;
        name.fontSizeMax = 36f;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        return rect;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        Vector2 pos,
        Vector2 size,
        string text,
        float fontSize,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        Vector2 pos,
        Vector2 size,
        string label,
        Color bgColor,
        Color textColor)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image image = btnObj.GetComponent<Image>();
        image.color = bgColor;
        AddGoldOutline(btnObj, 3f);

        TMP_Text labelText = CreateText(
            "Text",
            rect,
            Vector2.zero,
            Vector2.zero,
            label,
            48f,
            FontStyles.Bold,
            textColor,
            TextAlignmentOptions.Center);
        Stretch(labelText.rectTransform);

        return btnObj.GetComponent<Button>();
    }

    private static void CreateSeparator(Transform parent, float y, float width)
    {
        RectTransform rect = CreateRect("Separator", parent, new Vector2(0f, y), new Vector2(width, 2f));
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.85f, 0.66f, 0.26f, 0.65f);
        image.raycastTarget = false;
    }

    private static void AddGoldOutline(GameObject target, float thickness)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = new Color(0.90f, 0.66f, 0.26f, 0.95f);
        int px = Mathf.RoundToInt(Mathf.Max(1f, thickness));
        outline.effectDistance = new Vector2(px, -px);
        outline.useGraphicAlpha = true;
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
