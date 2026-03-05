using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 设置面板模板工具 — 一键生成带 3 个音量滑块 + 分辨率下拉的设置面板
/// 使用方式：GameObject -> DebtRunner -> Create Settings Panel
/// </summary>
public class SettingsPanelTemplate
{
    [MenuItem("GameObject/DebtRunner/Create Settings Panel")]
    public static void CreateSettingsPanel()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("错误", "场景中需要存在 Canvas。请先创建 Canvas！", "确定");
            return;
        }

        Transform existing = canvas.transform.Find("Panel_SettingsPlaceholder");
        if (existing != null)
        {
            EditorUtility.DisplayDialog("提示", "Panel_SettingsPlaceholder 已存在！", "确定");
            return;
        }

        // ── 根面板 ──
        GameObject panelRoot = new GameObject("Panel_SettingsPlaceholder");
        RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
        panelRoot.AddComponent<CanvasGroup>();
        Image panelImage = panelRoot.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0);
        panelImage.raycastTarget = true;

        panelRect.SetParent(canvas.transform, false);
        SetStretch(panelRect, canvas.transform);

        // ── 背景遮罩 ──
        GameObject bgOverlay = new GameObject("Background");
        Image bgImage = bgOverlay.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.75f);
        bgImage.raycastTarget = true;
        SetStretch(bgOverlay.GetComponent<RectTransform>(), panelRect);

        // ── 内容容器 ──
        GameObject content = new GameObject("Content");
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.SetParent(panelRect, false);
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(520, 520);
        contentRect.anchoredPosition = Vector2.zero;

        // 内容背景
        Image contentBg = content.AddComponent<Image>();
        contentBg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);

        // ── 标题 ──
        CreateLabel("Title", contentRect, new Vector2(0, 220), new Vector2(400, 50),
            "SETTINGS", 36, FontStyles.Bold, Color.white);

        // ── Master Volume 行 ──
        float rowY = 140f;
        CreateLabel("Label_MasterVolume", contentRect, new Vector2(-130, rowY), new Vector2(200, 30),
            "Master Volume", 22, FontStyles.Normal, new Color(0.9f, 0.9f, 0.9f));
        Slider masterSlider = CreateSlider("Slider_MasterVolume", contentRect,
            new Vector2(60, rowY), new Vector2(220, 25), 0.8f);
        TextMeshProUGUI masterValueText = CreateLabel("Value_MasterVolume", contentRect,
            new Vector2(210, rowY), new Vector2(60, 30),
            "80%", 20, FontStyles.Normal, Color.white);

        // ── BGM Volume 行 ──
        rowY = 80f;
        CreateLabel("Label_BGMVolume", contentRect, new Vector2(-130, rowY), new Vector2(200, 30),
            "BGM Volume", 22, FontStyles.Normal, new Color(0.9f, 0.9f, 0.9f));
        Slider bgmSlider = CreateSlider("Slider_BGMVolume", contentRect,
            new Vector2(60, rowY), new Vector2(220, 25), 0.8f);
        TextMeshProUGUI bgmValueText = CreateLabel("Value_BGMVolume", contentRect,
            new Vector2(210, rowY), new Vector2(60, 30),
            "80%", 20, FontStyles.Normal, Color.white);

        // ── SFX Volume 行 ──
        rowY = 20f;
        CreateLabel("Label_SFXVolume", contentRect, new Vector2(-130, rowY), new Vector2(200, 30),
            "SFX Volume", 22, FontStyles.Normal, new Color(0.9f, 0.9f, 0.9f));
        Slider sfxSlider = CreateSlider("Slider_SFXVolume", contentRect,
            new Vector2(60, rowY), new Vector2(220, 25), 1.0f);
        TextMeshProUGUI sfxValueText = CreateLabel("Value_SFXVolume", contentRect,
            new Vector2(210, rowY), new Vector2(60, 30),
            "100%", 20, FontStyles.Normal, Color.white);

        // ── Resolution 行 ──
        rowY = -50f;
        CreateLabel("Label_Resolution", contentRect, new Vector2(-130, rowY), new Vector2(200, 30),
            "Resolution", 22, FontStyles.Normal, new Color(0.9f, 0.9f, 0.9f));
        TMP_Dropdown resDropdown = CreateDropdown("Dropdown_Resolution", contentRect,
            new Vector2(80, rowY), new Vector2(260, 35));

        // ── Resolution Hint ──
        rowY = -90f;
        TextMeshProUGUI resHint = CreateLabel("ResolutionHint", contentRect,
            new Vector2(0, rowY), new Vector2(460, 25),
            "", 16, FontStyles.Italic, new Color(0.7f, 0.7f, 0.7f, 0.6f));

        // ── Back 按钮 ──
        rowY = -180f;
        Button backBtn = CreateButton("Btn_Back", contentRect,
            new Vector2(0, rowY), "BACK", new Color(0.25f, 0.55f, 0.9f, 1f));

        // ── 添加 SettingsMenuController 并绑定引用 ──
        SettingsMenuController settings = panelRoot.AddComponent<SettingsMenuController>();
        SerializedObject so = new SerializedObject(settings);

        so.FindProperty("volumeSlider").objectReferenceValue = masterSlider;
        so.FindProperty("volumeValueText").objectReferenceValue = masterValueText;
        so.FindProperty("bgmVolumeSlider").objectReferenceValue = bgmSlider;
        so.FindProperty("bgmVolumeValueText").objectReferenceValue = bgmValueText;
        so.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxSlider;
        so.FindProperty("sfxVolumeValueText").objectReferenceValue = sfxValueText;
        so.FindProperty("resolutionTMPDropdown").objectReferenceValue = resDropdown;
        so.FindProperty("resolutionHintText").objectReferenceValue = resHint;
        so.FindProperty("backButton").objectReferenceValue = backBtn;

        so.ApplyModifiedPropertiesWithoutUndo();

        // 默认隐藏
        panelRoot.SetActive(false);

        Undo.RegisterCreatedObjectUndo(panelRoot, "Create Settings Panel");
        EditorGUIUtility.PingObject(panelRoot);
        Selection.activeGameObject = panelRoot;

        EditorUtility.DisplayDialog("成功",
            "设置面板已创建！\n\n" +
            "下一步：\n" +
            "1. 将 Panel_SettingsPlaceholder 拖到 GameFlowController 的\n" +
            "   \"Panel Settings Placeholder\" 字段\n" +
            "2. 运行游戏，暂停菜单中点 Settings 即可使用",
            "确定");
    }

    // ─────────────────────────────────────────────
    //  辅助方法
    // ─────────────────────────────────────────────

    private static void SetStretch(RectTransform child, Transform parent)
    {
        child.SetParent(parent, false);
        child.anchorMin = Vector2.zero;
        child.anchorMax = Vector2.one;
        child.offsetMin = Vector2.zero;
        child.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI CreateLabel(
        string name, Transform parent,
        Vector2 pos, Vector2 size,
        string text, float fontSize, FontStyles style, Color color)
    {
        GameObject obj = new GameObject(name);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Slider CreateSlider(
        string name, Transform parent,
        Vector2 pos, Vector2 size, float defaultValue)
    {
        // Slider root
        GameObject sliderObj = new GameObject(name);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.SetParent(parent, false);
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.sizeDelta = size;
        sliderRect.anchoredPosition = pos;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = defaultValue;
        slider.wholeNumbers = false;

        // Background
        GameObject bg = new GameObject("Background");
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        bgRect.SetParent(sliderRect, false);
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.SetParent(sliderRect, false);
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);

        GameObject fill = new GameObject("Fill");
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.3f, 0.65f, 1f, 1f);
        fillRect.SetParent(fillAreaRect, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // Handle Slide Area
        GameObject handleArea = new GameObject("Handle Slide Area");
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.SetParent(sliderRect, false);
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);

        GameObject handle = new GameObject("Handle");
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        handleRect.SetParent(handleAreaRect, false);
        handleRect.sizeDelta = new Vector2(20, 0);
        handleRect.anchorMin = new Vector2(0, 0);
        handleRect.anchorMax = new Vector2(0, 1);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;

        return slider;
    }

    private static TMP_Dropdown CreateDropdown(
        string name, Transform parent,
        Vector2 pos, Vector2 size)
    {
        GameObject ddObj = new GameObject(name);
        RectTransform ddRect = ddObj.AddComponent<RectTransform>();
        ddRect.SetParent(parent, false);
        ddRect.anchorMin = new Vector2(0.5f, 0.5f);
        ddRect.anchorMax = new Vector2(0.5f, 0.5f);
        ddRect.sizeDelta = size;
        ddRect.anchoredPosition = pos;

        Image ddBg = ddObj.AddComponent<Image>();
        ddBg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        TMP_Dropdown dropdown = ddObj.AddComponent<TMP_Dropdown>();

        // Label
        GameObject labelObj = new GameObject("Label");
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.SetParent(ddRect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10, 0);
        labelRect.offsetMax = new Vector2(-25, 0);

        TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
        labelTMP.text = "1920 x 1080";
        labelTMP.fontSize = 20;
        labelTMP.alignment = TextAlignmentOptions.Left;
        labelTMP.color = Color.white;

        dropdown.captionText = labelTMP;

        // Arrow
        GameObject arrow = new GameObject("Arrow");
        RectTransform arrowRect = arrow.AddComponent<RectTransform>();
        arrowRect.SetParent(ddRect, false);
        arrowRect.anchorMin = new Vector2(1, 0.5f);
        arrowRect.anchorMax = new Vector2(1, 0.5f);
        arrowRect.sizeDelta = new Vector2(20, 20);
        arrowRect.anchoredPosition = new Vector2(-15, 0);
        TextMeshProUGUI arrowText = arrow.AddComponent<TextMeshProUGUI>();
        arrowText.text = "▼";
        arrowText.fontSize = 14;
        arrowText.alignment = TextAlignmentOptions.Center;
        arrowText.color = Color.white;
        arrowText.raycastTarget = false;

        // Template (dropdown list)
        GameObject template = new GameObject("Template");
        RectTransform templateRect = template.AddComponent<RectTransform>();
        templateRect.SetParent(ddRect, false);
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.sizeDelta = new Vector2(0, 150);
        templateRect.anchoredPosition = Vector2.zero;

        Image templateBg = template.AddComponent<Image>();
        templateBg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

        ScrollRect scrollRect = template.AddComponent<ScrollRect>();
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.SetParent(templateRect, false);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        Image viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = Color.white;

        // Content container
        GameObject contentGO = new GameObject("Content");
        RectTransform contentR = contentGO.AddComponent<RectTransform>();
        contentR.SetParent(viewportRect, false);
        contentR.anchorMin = new Vector2(0, 1);
        contentR.anchorMax = new Vector2(1, 1);
        contentR.pivot = new Vector2(0.5f, 1f);
        contentR.sizeDelta = new Vector2(0, 28);

        scrollRect.content = contentR;
        scrollRect.viewport = viewportRect;

        // Item template
        GameObject item = new GameObject("Item");
        RectTransform itemRect = item.AddComponent<RectTransform>();
        itemRect.SetParent(contentR, false);
        itemRect.anchorMin = new Vector2(0, 0.5f);
        itemRect.anchorMax = new Vector2(1, 0.5f);
        itemRect.sizeDelta = new Vector2(0, 28);

        Toggle itemToggle = item.AddComponent<Toggle>();

        // Item background
        GameObject itemBg = new GameObject("Item Background");
        RectTransform itemBgRect = itemBg.AddComponent<RectTransform>();
        itemBgRect.SetParent(itemRect, false);
        itemBgRect.anchorMin = Vector2.zero;
        itemBgRect.anchorMax = Vector2.one;
        itemBgRect.offsetMin = Vector2.zero;
        itemBgRect.offsetMax = Vector2.zero;
        Image itemBgImg = itemBg.AddComponent<Image>();
        itemBgImg.color = new Color(0.25f, 0.25f, 0.3f, 1f);

        // Item checkmark
        GameObject checkmark = new GameObject("Item Checkmark");
        RectTransform checkRect = checkmark.AddComponent<RectTransform>();
        checkRect.SetParent(itemBgRect, false);
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;
        checkRect.offsetMin = Vector2.zero;
        checkRect.offsetMax = Vector2.zero;
        Image checkImg = checkmark.AddComponent<Image>();
        checkImg.color = new Color(0.3f, 0.65f, 1f, 0.4f);

        itemToggle.targetGraphic = itemBgImg;
        itemToggle.graphic = checkImg;

        // Item label
        GameObject itemLabel = new GameObject("Item Label");
        RectTransform itemLabelRect = itemLabel.AddComponent<RectTransform>();
        itemLabelRect.SetParent(itemRect, false);
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(10, 0);
        itemLabelRect.offsetMax = new Vector2(0, 0);

        TextMeshProUGUI itemLabelTMP = itemLabel.AddComponent<TextMeshProUGUI>();
        itemLabelTMP.text = "Option";
        itemLabelTMP.fontSize = 18;
        itemLabelTMP.alignment = TextAlignmentOptions.Left;
        itemLabelTMP.color = Color.white;

        dropdown.itemText = itemLabelTMP;
        dropdown.template = templateRect;

        template.SetActive(false);

        return dropdown;
    }

    private static Button CreateButton(
        string name, Transform parent,
        Vector2 pos, string label, Color bgColor)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(200, 50);
        rect.anchoredPosition = pos;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        GameObject labelObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 24;
        tmp.color = Color.white;

        return btnObj.GetComponent<Button>();
    }
}

#endif
