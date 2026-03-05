using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// One-click generator for a 3-choice level-up UI panel with hover lamp effects.
/// </summary>
public static class LevelUpPanelTemplate
{
    private struct CardRefs
    {
        public UpgradeCard upgradeCard;
        public UpgradeCardHoverLight hoverLight;
        public Image frameImage;
        public Image lampGlowImage;
        public Image iconImage;
        public TMP_Text titleText;
        public TMP_Text descText;
        public Button button;
    }

    [MenuItem("GameObject/DebtRunner/Create LevelUp Panel Template (3 Choices)")]
    public static void CreateLevelUpPanelTemplate()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found. Create a Canvas first.", "OK");
            return;
        }

        const string rootName = "Panel_LevelUp_3Choice";
        Transform existing = canvas.transform.Find(rootName);
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Hint", $"{rootName} already exists in this Canvas.", "OK");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        GameObject root = new GameObject(rootName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(LevelUpPanel));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(canvas.transform, false);
        Stretch(rootRect);

        Image rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(0f, 0f, 0f, 0f);
        rootImage.raycastTarget = false;

        GameObject dimObj = new GameObject("DimBackground", typeof(RectTransform), typeof(Image));
        RectTransform dimRect = dimObj.GetComponent<RectTransform>();
        dimRect.SetParent(rootRect, false);
        Stretch(dimRect);
        Image dimImage = dimObj.GetComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, 0.66f);
        dimImage.raycastTarget = true;

        GameObject panelObj = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.SetParent(rootRect, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1240f, 720f);
        panelRect.anchoredPosition = Vector2.zero;
        Image panelImage = panelObj.GetComponent<Image>();
        panelImage.color = new Color(0.05f, 0.05f, 0.07f, 0.95f);
        panelImage.raycastTarget = false;

        TMP_Text title = CreateText(
            "Title_LevelUp",
            panelRect,
            new Vector2(0f, 300f),
            new Vector2(900f, 90f),
            "LEVEL UP!",
            68f,
            FontStyles.Bold,
            Color.white);
        title.outlineWidth = 0.20f;
        title.outlineColor = Color.black;

        TMP_Text hint = CreateText(
            "Hint_SelectOne",
            panelRect,
            new Vector2(0f, 245f),
            new Vector2(900f, 40f),
            "Choose 1 card",
            26f,
            FontStyles.Normal,
            new Color(0.85f, 0.85f, 0.85f, 1f));

        RectTransform row = CreateRect("CardsRow", panelRect, new Vector2(0f, -35f), new Vector2(1120f, 500f));

        CardRefs[] cards = new CardRefs[3];
        float startX = -360f;
        float gap = 360f;
        for (int i = 0; i < cards.Length; i++)
        {
            float x = startX + gap * i;
            cards[i] = CreateCard(row, i, x);
        }

        LevelUpPanel levelUpPanel = root.GetComponent<LevelUpPanel>();
        SerializedObject panelSO = new SerializedObject(levelUpPanel);
        panelSO.FindProperty("panel").objectReferenceValue = panelObj;
        panelSO.FindProperty("dimBackground").objectReferenceValue = dimImage;

        SerializedProperty slotsProp = panelSO.FindProperty("cardSlots");
        slotsProp.arraySize = cards.Length;
        for (int i = 0; i < cards.Length; i++)
            slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i].upgradeCard;
        panelSO.ApplyModifiedPropertiesWithoutUndo();

        // Optional soft auto-bind to GameFlowController if fields are currently missing.
        GameFlowController flow = Object.FindObjectOfType<GameFlowController>();
        if (flow != null)
        {
            SerializedObject flowSO = new SerializedObject(flow);
            SerializedProperty panelLevelUpProp = flowSO.FindProperty("panelLevelUp");
            SerializedProperty levelUpPanelProp = flowSO.FindProperty("levelUpPanel");

            if (panelLevelUpProp != null && panelLevelUpProp.objectReferenceValue == null)
                panelLevelUpProp.objectReferenceValue = panelObj;

            if (levelUpPanelProp != null && levelUpPanelProp.objectReferenceValue == null)
                levelUpPanelProp.objectReferenceValue = levelUpPanel;

            flowSO.ApplyModifiedPropertiesWithoutUndo();
        }

        // Keep visible in edit mode for scene tweaking.
        // Runtime hide/show is still controlled by LevelUpPanel/GameFlowController.
        CanvasGroup cg = root.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        Undo.RegisterCreatedObjectUndo(root, "Create LevelUp Panel Template");
        EditorGUIUtility.PingObject(root);
        Selection.activeGameObject = root;

        EditorUtility.DisplayDialog(
            "Created",
            "3-choice level-up panel template created.\n\n" +
            "- Includes top lamp hover highlight on each card.\n" +
            "- No refresh button is included.\n" +
            "- If needed, assign Panel and LevelUpPanel refs in GameFlowController.",
            "OK");
    }

    private static CardRefs CreateCard(Transform parent, int index, float anchoredX)
    {
        GameObject cardObj = new GameObject(
            $"Card_{index + 1}",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(UpgradeCard),
            typeof(UpgradeCardHoverLight));

        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.SetParent(parent, false);
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(300f, 430f);
        cardRect.anchoredPosition = new Vector2(anchoredX, 0f);

        Image frameImage = cardObj.GetComponent<Image>();
        frameImage.color = new Color(0.72f, 0.72f, 0.75f, 1f);
        frameImage.raycastTarget = true;

        Button btn = cardObj.GetComponent<Button>();
        btn.targetGraphic = frameImage;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 1f);
        cb.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        cb.selectedColor = new Color(1f, 1f, 1f, 1f);
        cb.disabledColor = new Color(0.55f, 0.55f, 0.58f, 0.75f);
        btn.colors = cb;

        RectTransform cardInner = CreateRect("Inner", cardRect, new Vector2(0f, 0f), new Vector2(278f, 408f));
        Image innerImage = cardInner.gameObject.AddComponent<Image>();
        innerImage.color = new Color(0.94f, 0.94f, 0.94f, 1f);
        innerImage.raycastTarget = false;

        RectTransform lampHousing = CreateRect("LampHousing", cardRect, new Vector2(0f, 208f), new Vector2(110f, 14f));
        Image housingImage = lampHousing.gameObject.AddComponent<Image>();
        housingImage.color = new Color(0.20f, 0.20f, 0.22f, 1f);
        housingImage.raycastTarget = false;

        RectTransform lampGlowRect = CreateRect("LampGlow", cardRect, new Vector2(0f, 208f), new Vector2(128f, 28f));
        Image lampGlow = lampGlowRect.gameObject.AddComponent<Image>();
        lampGlow.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        lampGlow.type = Image.Type.Simple;
        lampGlow.color = new Color(1f, 0.9f, 0.35f, 0.10f);
        lampGlow.raycastTarget = false;

        TMP_Text title = CreateText(
            "CardTitle",
            cardRect,
            new Vector2(0f, 165f),
            new Vector2(250f, 46f),
            "CARD NAME",
            33f,
            FontStyles.Bold,
            new Color(0.14f, 0.14f, 0.14f, 1f));

        RectTransform artFrame = CreateRect("ArtFrame", cardRect, new Vector2(0f, 25f), new Vector2(220f, 245f));
        Image artFrameImage = artFrame.gameObject.AddComponent<Image>();
        artFrameImage.color = new Color(0.12f, 0.12f, 0.14f, 1f);
        artFrameImage.raycastTarget = false;

        RectTransform iconRect = CreateRect("CardIcon", artFrame, Vector2.zero, new Vector2(196f, 222f));
        Image icon = iconRect.gameObject.AddComponent<Image>();
        icon.color = new Color(0.72f, 0.20f, 0.20f, 1f);
        icon.raycastTarget = false;

        TMP_Text desc = CreateText(
            "CardDescription",
            cardRect,
            new Vector2(0f, -160f),
            new Vector2(250f, 110f),
            "Upgrade description goes here.",
            21f,
            FontStyles.Normal,
            new Color(0.16f, 0.16f, 0.16f, 0.95f));
        desc.enableWordWrapping = true;

        UpgradeCard upgradeCard = cardObj.GetComponent<UpgradeCard>();
        SerializedObject cardSO = new SerializedObject(upgradeCard);
        cardSO.FindProperty("cardImage").objectReferenceValue = icon;
        cardSO.FindProperty("titleText").objectReferenceValue = title;
        cardSO.FindProperty("descriptionText").objectReferenceValue = desc;
        cardSO.FindProperty("selectButton").objectReferenceValue = btn;
        cardSO.ApplyModifiedPropertiesWithoutUndo();

        UpgradeCardHoverLight hoverLight = cardObj.GetComponent<UpgradeCardHoverLight>();
        SerializedObject hoverSO = new SerializedObject(hoverLight);
        hoverSO.FindProperty("lampGlow").objectReferenceValue = lampGlow;
        hoverSO.FindProperty("cardFrame").objectReferenceValue = frameImage;
        hoverSO.FindProperty("scaleTarget").objectReferenceValue = cardRect;
        hoverSO.ApplyModifiedPropertiesWithoutUndo();

        return new CardRefs
        {
            upgradeCard = upgradeCard,
            hoverLight = hoverLight,
            frameImage = frameImage,
            lampGlowImage = lampGlow,
            iconImage = icon,
            titleText = title,
            descText = desc,
            button = btn
        };
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
        Color color)
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
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
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
