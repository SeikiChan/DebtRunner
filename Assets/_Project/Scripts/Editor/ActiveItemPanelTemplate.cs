using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

public static class ActiveItemPanelTemplate
{
    [MenuItem("GameObject/DebtRunner/Create Active Item HUD Panel")]
    public static void CreateActiveItemHudPanel()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found. Create a Canvas first.", "OK");
            return;
        }

        Transform existing = canvas.transform.Find("Panel_ActiveItemHUD");
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Hint", "Panel_ActiveItemHUD already exists in this Canvas.", "OK");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        GameObject root = new GameObject("Panel_ActiveItemHUD", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(canvas.transform, false);
        rootRect.anchorMin = new Vector2(1f, 0f);
        rootRect.anchorMax = new Vector2(1f, 0f);
        rootRect.pivot = new Vector2(1f, 0f);
        rootRect.sizeDelta = new Vector2(334f, 96f);
        rootRect.anchoredPosition = new Vector2(-28f, 94f);

        CanvasGroup overlay = root.GetComponent<CanvasGroup>();
        overlay.alpha = 1f;
        overlay.interactable = false;
        overlay.blocksRaycasts = false;

        Image frameImage = CreateFrame(rootRect);
        TMP_Text itemNameText = CreateText(
            "Text_ItemName",
            rootRect,
            new Vector2(16f, -14f),
            new Vector2(190f, 30f),
            "ACTIVE ITEM",
            24f,
            FontStyles.Bold,
            new Color(0.99f, 0.95f, 0.86f, 1f),
            TextAlignmentOptions.Left);

        TMP_Text statusText = CreateText(
            "Text_Status",
            rootRect,
            new Vector2(16f, -50f),
            new Vector2(190f, 24f),
            "[SPACE] READY",
            18f,
            FontStyles.Bold,
            new Color(0.88f, 0.90f, 0.93f, 1f),
            TextAlignmentOptions.Left);

        RectTransform cooldownRoot = CreateCooldownRoot(rootRect);
        CreateDiscImage("Image_CooldownBackplate", cooldownRoot, Color.black, 0.18f);
        Image readyGlow = CreateDiscImage("Image_ReadyGlow", cooldownRoot, new Color(1f, 0.90f, 0.34f, 0.72f), 1f);
        Image cooldownWheel = CreateDiscImage("Image_CooldownWheel", cooldownRoot, new Color(0f, 0f, 0f, 0.82f), 1f);
        cooldownWheel.type = Image.Type.Filled;
        cooldownWheel.fillMethod = Image.FillMethod.Radial360;
        cooldownWheel.fillOrigin = (int)Image.Origin360.Top;
        cooldownWheel.fillClockwise = true;
        cooldownWheel.fillAmount = 0.66f;

        TMP_Text cooldownValueText = CreateText(
            "Text_CooldownValue",
            cooldownRoot,
            new Vector2(0f, -3f),
            new Vector2(58f, 24f),
            "4.0",
            20f,
            FontStyles.Bold,
            new Color(0.99f, 0.95f, 0.86f, 1f),
            TextAlignmentOptions.Center);
        CenterStretchText(cooldownValueText.rectTransform);

        root.SetActive(false);

        GameFlowController flow = Object.FindObjectOfType<GameFlowController>();
        PlayerActiveItemController controller = null;
        if (flow != null)
            controller = flow.GetComponent<PlayerActiveItemController>();
        if (controller == null && flow != null)
            controller = flow.gameObject.AddComponent<PlayerActiveItemController>();

        if (controller != null)
        {
            SerializedObject controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("activeItemPanelRoot").objectReferenceValue = root;
            controllerSO.FindProperty("activeItemOverlay").objectReferenceValue = overlay;
            controllerSO.FindProperty("activeItemFrameImage").objectReferenceValue = frameImage;
            controllerSO.FindProperty("activeItemNameText").objectReferenceValue = itemNameText;
            controllerSO.FindProperty("activeItemStatusText").objectReferenceValue = statusText;
            controllerSO.FindProperty("activeItemCooldownValueText").objectReferenceValue = cooldownValueText;
            controllerSO.FindProperty("activeItemCooldownWheelImage").objectReferenceValue = cooldownWheel;
            controllerSO.FindProperty("activeItemReadyGlowImage").objectReferenceValue = readyGlow;
            controllerSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        if (flow != null)
        {
            SerializedObject flowSO = new SerializedObject(flow);
            SerializedProperty controllerProp = flowSO.FindProperty("playerActiveItemController");
            if (controllerProp != null && controller != null)
                controllerProp.objectReferenceValue = controller;
            flowSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flow);
        }

        Undo.RegisterCreatedObjectUndo(root, "Create Active Item HUD Panel");
        EditorGUIUtility.PingObject(root);
        Selection.activeGameObject = root;

        EditorUtility.DisplayDialog(
            "Created",
            "Panel_ActiveItemHUD created and auto-bound to PlayerActiveItemController when possible.",
            "OK");
    }

    private static Image CreateFrame(Transform parent)
    {
        GameObject go = new GameObject("Image_Frame", typeof(RectTransform), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.09f, 0.09f, 0.11f, 0.96f);
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform CreateCooldownRoot(Transform parent)
    {
        GameObject go = new GameObject("Panel_Cooldown", typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(68f, 68f);
        rect.anchoredPosition = new Vector2(-12f, 0f);
        return rect;
    }

    private static Image CreateDiscImage(string name, Transform parent, Color color, float scale)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.one * (64f * Mathf.Max(0.1f, scale));

        Image image = go.GetComponent<Image>();
        image.sprite = RuntimeSpriteFactory.GetHitPulseSprite();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        string value,
        float fontSize,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        text.outlineWidth = 0.2f;
        return text;
    }

    private static void CenterStretchText(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -3f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
#endif
