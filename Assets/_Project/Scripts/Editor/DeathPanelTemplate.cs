using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 死亡面板模板工具 — 分别生成怪物击杀和债务失败两套独立面板
/// 使用方式：
///   GameObject -> DebtRunner -> Create Monster Death Panel
///   GameObject -> DebtRunner -> Create Debt Failure Panel
/// </summary>
public class DeathPanelTemplate
{
    // ─────────────────────────────────────────────
    //  菜单项：怪物击杀死亡面板
    // ─────────────────────────────────────────────
    [MenuItem("GameObject/DebtRunner/Create Monster Death Panel")]
    public static void CreateMonsterDeathPanel()
    {
        CreatePanel(new PanelConfig
        {
            panelName          = "Panel_Death_Monster",
            title              = "SLAIN!",
            reason             = "KILLED BY MONSTER",
            description        = "You were defeated by enemies in battle.",
            titleColor         = new Color(1f, 0.2f, 0.2f, 1f),        // 红色
            reasonColor        = new Color(1f, 0.8f, 0.2f, 1f),        // 金色
            bgOverlayColor     = new Color(0.15f, 0f, 0f, 0.7f),       // 深红遮罩
            bgImageColor       = new Color(0.3f, 0.05f, 0.05f, 0.5f),  // 暗红
            successMessage     =
                "怪物击杀死亡面板已创建！\n\n" +
                "下一步：\n" +
                "1. 将 Panel_Death_Monster 拖到 GameFlowController 的\n" +
                "   \"Panel Death Monster\" 字段\n" +
                "2. 在 Inspector 中可自定义文案和背景素材"
        });
    }

    // ─────────────────────────────────────────────
    //  菜单项：债务失败死亡面板
    // ─────────────────────────────────────────────
    [MenuItem("GameObject/DebtRunner/Create Debt Failure Panel")]
    public static void CreateDebtFailurePanel()
    {
        CreatePanel(new PanelConfig
        {
            panelName          = "Panel_Death_Debt",
            title              = "DEBT DEFAULTED!",
            reason             = "DEBT FAILURE",
            description        = "You couldn't pay back your debt. The collectors are coming.",
            titleColor         = new Color(1f, 0.75f, 0.15f, 1f),      // 金色
            reasonColor        = new Color(0.9f, 0.4f, 0.4f, 1f),      // 浅红
            bgOverlayColor     = new Color(0.08f, 0.06f, 0f, 0.75f),   // 深黄遮罩
            bgImageColor       = new Color(0.25f, 0.18f, 0f, 0.5f),    // 暗金
            successMessage     =
                "债务失败死亡面板已创建！\n\n" +
                "下一步：\n" +
                "1. 将 Panel_Death_Debt 拖到 GameFlowController 的\n" +
                "   \"Panel Death Debt\" 字段\n" +
                "2. 在 Inspector 中可自定义文案和背景素材"
        });
    }

    // ─────────────────────────────────────────────
    //  内部配置结构
    // ─────────────────────────────────────────────
    private struct PanelConfig
    {
        public string panelName;
        public string title;
        public string reason;
        public string description;
        public Color  titleColor;
        public Color  reasonColor;
        public Color  bgOverlayColor;
        public Color  bgImageColor;
        public string successMessage;
    }

    // ─────────────────────────────────────────────
    //  核心创建逻辑（两个菜单项共用）
    // ─────────────────────────────────────────────
    private static void CreatePanel(PanelConfig cfg)
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("错误", "场景中需要存在 Canvas。请先创建 Canvas！", "确定");
            return;
        }

        Transform existing = canvas.transform.Find(cfg.panelName);
        if (existing != null)
        {
            EditorUtility.DisplayDialog("提示", $"{cfg.panelName} 已存在！", "确定");
            return;
        }

        // ── 根面板 ──
        GameObject panelRoot = new GameObject(cfg.panelName);
        RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
        panelRoot.AddComponent<CanvasGroup>();
        Image panelImage = panelRoot.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0);

        panelRect.SetParent(canvas.transform, false);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // ── 背景遮罩 ──
        GameObject bgOverlay = new GameObject("Background");
        RectTransform bgRect = bgOverlay.AddComponent<RectTransform>();
        Image bgImage = bgOverlay.AddComponent<Image>();
        bgImage.color = cfg.bgOverlayColor;
        bgImage.raycastTarget = true;
        SetStretch(bgRect, panelRect);

        // ── 背景精灵占位 ──
        GameObject bgImageObj = new GameObject("BackgroundImage");
        RectTransform bgImgRect = bgImageObj.AddComponent<RectTransform>();
        Image bgImg = bgImageObj.AddComponent<Image>();
        bgImg.color = cfg.bgImageColor;
        bgImg.raycastTarget = false;
        SetStretch(bgImgRect, panelRect);

        // ── 内容容器 ──
        GameObject contentContainer = new GameObject("Content");
        RectTransform contentRect = contentContainer.AddComponent<RectTransform>();
        contentContainer.AddComponent<LayoutElement>();
        contentRect.SetParent(panelRect, false);
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(1000, 600);
        contentRect.anchoredPosition = Vector2.zero;

        // ── 标题 ──
        TextMeshProUGUI titleTMP = CreateText("DeathTitle", contentRect,
            new Vector2(0, 150), new Vector2(900, 150),
            cfg.title, 100, FontStyles.Bold, cfg.titleColor, 0.3f);

        // ── 原因 ──
        TextMeshProUGUI reasonTMP = CreateText("DeathReason", contentRect,
            new Vector2(0, 60), new Vector2(800, 60),
            cfg.reason, 40, FontStyles.Bold, cfg.reasonColor, 0.2f);

        // ── 描述 ──
        TextMeshProUGUI descTMP = CreateText("DeathDescription", contentRect,
            new Vector2(0, -30), new Vector2(800, 100),
            cfg.description, 28, FontStyles.Normal, new Color(1, 1, 1, 0.8f), 0.2f);

        // ── 提示文字 ──
        CreateText("TipText", contentRect,
            new Vector2(0, -140), new Vector2(800, 60),
            "Press Any Key to Continue...", 24, FontStyles.Italic, new Color(0.8f, 0.8f, 0.8f, 0.6f), 0f);

        // ── 按钮容器 ──
        GameObject buttonsContainer = new GameObject("ButtonsContainer");
        RectTransform buttonsRect = buttonsContainer.AddComponent<RectTransform>();
        buttonsRect.SetParent(contentRect, false);
        buttonsRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonsRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonsRect.sizeDelta = new Vector2(600, 80);
        buttonsRect.anchoredPosition = new Vector2(0, -240);

        Button restartBtn = CreateButton("Btn_Restart", buttonsRect,
            new Vector2(-120, 0), "Restart", new Color(0.2f, 0.6f, 1f, 1f));

        Button menuBtn = CreateButton("Btn_MainMenu", buttonsRect,
            new Vector2(120, 0), "Main Menu", new Color(0.2f, 0.6f, 1f, 1f));

        // ── 添加 DeathPanel 组件并通过 SerializedObject 连接所有引用 ──
        DeathPanel deathPanel = panelRoot.AddComponent<DeathPanel>();
        SerializedObject so = new SerializedObject(deathPanel);

        // 连接 UI 引用
        so.FindProperty("deathTitleText").objectReferenceValue       = titleTMP;
        so.FindProperty("deathReasonText").objectReferenceValue      = reasonTMP;
        so.FindProperty("deathDescriptionText").objectReferenceValue = descTMP;
        so.FindProperty("deathBackgroundImage").objectReferenceValue = bgImg;

        // 设置文案
        so.FindProperty("title").stringValue       = cfg.title;
        so.FindProperty("reason").stringValue      = cfg.reason;
        so.FindProperty("description").stringValue = cfg.description;

        // 连接按钮
        so.FindProperty("restartButton").objectReferenceValue  = restartBtn;
        so.FindProperty("mainMenuButton").objectReferenceValue = menuBtn;

        so.ApplyModifiedPropertiesWithoutUndo();

        // 默认隐藏
        panelRoot.SetActive(false);

        Undo.RegisterCreatedObjectUndo(panelRoot, $"Create {cfg.panelName}");
        EditorGUIUtility.PingObject(panelRoot);
        Selection.activeGameObject = panelRoot;

        EditorUtility.DisplayDialog("成功", cfg.successMessage, "确定");
        Debug.Log($"{cfg.panelName} created successfully!");
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

    private static TextMeshProUGUI CreateText(
        string name, Transform parent,
        Vector2 pos, Vector2 size,
        string text, float fontSize, FontStyles style, Color color,
        float outlineWidth)
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
        if (outlineWidth > 0f)
        {
            tmp.outlineWidth = outlineWidth;
            tmp.outlineColor = Color.black;
        }
        return tmp;
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
        rect.sizeDelta = new Vector2(220, 60);
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
        tmp.fontSize = 28;
        tmp.color = Color.white;

        return btnObj.GetComponent<Button>();
    }
}

#endif
