using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 商店面板模板工具 — 一键创建完整的可调节商店UI
/// 使用方式：GameObject -> DebtRunner -> Create Shop Panel Template
/// </summary>
public class ShopPanelTemplate
{
    // 转盘扇区默认配置
    private static readonly PrizeOutcomeType[] DefaultSegmentOutcomes =
    {
        PrizeOutcomeType.Cash,
        PrizeOutcomeType.ThankYou,
        PrizeOutcomeType.FreeItem,
        PrizeOutcomeType.DebtUp,
        PrizeOutcomeType.Cash,
        PrizeOutcomeType.ThankYou,
        PrizeOutcomeType.FreeItem,
        PrizeOutcomeType.EnemyStronger,
    };

    private static readonly Color[] DefaultSegmentColors =
    {
        new Color(0.18f, 0.72f, 0.32f, 1f),  // Cash - 绿色
        new Color(0.55f, 0.55f, 0.60f, 1f),  // ThankYou - 灰色
        new Color(0.25f, 0.58f, 0.95f, 1f),  // FreeItem - 蓝色
        new Color(0.88f, 0.25f, 0.25f, 1f),  // DebtUp - 红色
        new Color(0.18f, 0.72f, 0.32f, 1f),  // Cash - 绿色
        new Color(0.55f, 0.55f, 0.60f, 1f),  // ThankYou - 灰色
        new Color(0.25f, 0.58f, 0.95f, 1f),  // FreeItem - 蓝色
        new Color(0.72f, 0.28f, 0.72f, 1f),  // EnemyStronger - 紫色
    };

    private static readonly string[] DefaultSegmentLabels =
    {
        "CASH", "THANKS", "FREE", "DEBT+",
        "CASH", "THANKS", "FREE", "ENEMY+",
    };

    [MenuItem("GameObject/DebtRunner/Create Shop Panel Template")]
    public static void CreateShopPanelTemplate()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("错误", "场景中需要存在 Canvas。请先创建 Canvas！", "确定");
            return;
        }

        Transform existing = canvas.transform.Find("Panel_Shop");
        if (existing != null)
        {
            EditorUtility.DisplayDialog("提示", "Panel_Shop 已存在！", "确定");
            return;
        }

        // ── 根面板 ──
        GameObject panelRoot = new GameObject("Panel_Shop");
        RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
        panelRoot.AddComponent<CanvasGroup>();
        Image panelImage = panelRoot.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0);
        panelImage.raycastTarget = false;

        panelRect.SetParent(canvas.transform, false);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // ── 背景遮罩 ──
        GameObject bgObj = new GameObject("Background");
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.06f, 0.06f, 0.08f, 0.88f);
        bgImage.raycastTarget = true;
        SetStretch(bgRect, panelRect);

        // ── 内容容器 ──
        GameObject contentObj = new GameObject("Content");
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.SetParent(panelRect, false);
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(1200, 800);
        contentRect.anchoredPosition = Vector2.zero;

        // ── 顶部：轮次 & 债务信息 ──
        TMP_Text textRoundInfo = CreateText("Text_RoundInfo", contentRect,
            new Vector2(0f, 385f), new Vector2(1100f, 40f),
            "Round 1/10    Next Debt: $100", 28, FontStyles.Bold,
            new Color(1f, 0.85f, 0.35f, 1f));

        // ── 顶部：现金信息 ──
        TMP_Text textCash = CreateText("Text_Cash", contentRect,
            new Vector2(0f, 350f), new Vector2(1100f, 40f),
            "Cash: $0", 24, FontStyles.Bold, Color.white);

        // ── 左侧：商品卡片区域 ──
        RectTransform itemsContainer = CreateRect("ShopItems", contentRect,
            new Vector2(-260f, 20f), new Vector2(520f, 600f));

        ShopSystem.ShopItemUIRefs[] itemRefs = new ShopSystem.ShopItemUIRefs[3];
        for (int i = 0; i < 3; i++)
        {
            float y = 190f - i * 200f;
            itemRefs[i] = CreateItemCard(itemsContainer, i, y);
        }

        // ── 右侧：转盘区域 ──
        RectTransform wheelArea = CreateRect("SpinningWheelArea", contentRect,
            new Vector2(310f, 60f), new Vector2(460f, 460f));

        // 转盘背景圆
        Image wheelAreaBg = wheelArea.gameObject.AddComponent<Image>();
        wheelAreaBg.color = new Color(0.12f, 0.12f, 0.15f, 0.9f);
        wheelAreaBg.raycastTarget = false;

        // 转盘容器（会旋转的部分）
        GameObject wheelContainerObj = new GameObject("WheelContainer");
        RectTransform wheelContainerRect = wheelContainerObj.AddComponent<RectTransform>();
        wheelContainerRect.SetParent(wheelArea, false);
        wheelContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
        wheelContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
        wheelContainerRect.sizeDelta = new Vector2(360f, 360f);
        wheelContainerRect.anchoredPosition = Vector2.zero;

        // 添加动画器
        SpinningWheelAnimator wheelAnimator = wheelContainerObj.AddComponent<SpinningWheelAnimator>();

        // 创建8个扇区
        SpinningWheelController.SegmentConfig[] segConfigs = new SpinningWheelController.SegmentConfig[8];
        float degreesPerSegment = 360f / 8f;

        for (int i = 0; i < 8; i++)
        {
            segConfigs[i] = CreateWheelSegment(wheelContainerRect, i, degreesPerSegment);
        }

        // 中心圆（装饰）
        GameObject centerCircle = new GameObject("CenterCircle");
        RectTransform centerRect = centerCircle.AddComponent<RectTransform>();
        centerRect.SetParent(wheelContainerRect, false);
        centerRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerRect.sizeDelta = new Vector2(60f, 60f);
        centerRect.anchoredPosition = Vector2.zero;
        Image centerImg = centerCircle.AddComponent<Image>();
        centerImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        centerImg.raycastTarget = false;

        // 指针（固定不旋转，在转盘外部上方）
        GameObject pointerObj = new GameObject("Pointer");
        RectTransform pointerRect = pointerObj.AddComponent<RectTransform>();
        pointerRect.SetParent(wheelArea, false);
        pointerRect.anchorMin = new Vector2(0.5f, 0.5f);
        pointerRect.anchorMax = new Vector2(0.5f, 0.5f);
        pointerRect.sizeDelta = new Vector2(30f, 40f);
        pointerRect.anchoredPosition = new Vector2(0f, 200f);
        pointerRect.localEulerAngles = new Vector3(0f, 0f, 180f); // 箭头朝下
        Image pointerImg = pointerObj.AddComponent<Image>();
        pointerImg.color = new Color(1f, 0.85f, 0.15f, 1f);
        pointerImg.raycastTarget = false;

        // ── 抽奖按钮 ──
        Button btnDraw = CreateButtonWithLabel("Btn_Draw", contentRect,
            new Vector2(310f, -210f), new Vector2(200f, 55f),
            "DRAW $120", new Color(0.96f, 0.82f, 0.26f, 1f), new Color(0.16f, 0.16f, 0.16f, 1f),
            out TMP_Text textDrawLabel);

        // ── 刷新按钮 ──
        Button btnRefresh = CreateButtonWithLabel("Btn_Refresh", contentRect,
            new Vector2(-350f, -330f), new Vector2(220f, 50f),
            "Refresh $80", new Color(0.85f, 0.85f, 0.85f, 0.92f), new Color(0.16f, 0.16f, 0.16f, 1f),
            out TMP_Text textRefreshLabel);

        // ── 下一轮按钮 ──
        Button btnNextRound = CreateButtonWithLabel("Btn_NextRound", contentRect,
            new Vector2(350f, -330f), new Vector2(220f, 50f),
            "Next Round", new Color(0.3f, 0.75f, 0.3f, 1f), Color.white,
            out _);

        // ── 信息文本 ──
        TMP_Text textInfo = CreateText("Text_Info", contentRect,
            new Vector2(0f, -380f), new Vector2(1100f, 60f),
            "Spend cash to upgrade. Draw can help or hurt the next round.",
            22, FontStyles.Normal, new Color(0.95f, 0.9f, 0.65f, 1f));
        textInfo.enableWordWrapping = true;

        // ══════════════════════════════════════
        //  添加组件并用 SerializedObject 连接引用
        // ══════════════════════════════════════

        // ── ShopSystem ──
        ShopSystem shopSystem = panelRoot.AddComponent<ShopSystem>();
        SerializedObject shopSO = new SerializedObject(shopSystem);

        shopSO.FindProperty("textRoundInfo").objectReferenceValue = textRoundInfo;
        shopSO.FindProperty("textCash").objectReferenceValue = textCash;
        shopSO.FindProperty("textInfo").objectReferenceValue = textInfo;
        shopSO.FindProperty("buttonRefresh").objectReferenceValue = btnRefresh;
        shopSO.FindProperty("textRefreshLabel").objectReferenceValue = textRefreshLabel;

        // 连接商品卡片
        SerializedProperty itemUIsProperty = shopSO.FindProperty("itemUIs");
        itemUIsProperty.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            SerializedProperty element = itemUIsProperty.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("titleText").objectReferenceValue = itemRefs[i].titleText;
            element.FindPropertyRelative("descText").objectReferenceValue = itemRefs[i].descText;
            element.FindPropertyRelative("priceText").objectReferenceValue = itemRefs[i].priceText;
            element.FindPropertyRelative("buyButton").objectReferenceValue = itemRefs[i].buyButton;
            element.FindPropertyRelative("buyButtonLabel").objectReferenceValue = itemRefs[i].buyButtonLabel;
            element.FindPropertyRelative("iconImage").objectReferenceValue = itemRefs[i].iconImage;
        }

        // ── SpinningWheelController ──
        SpinningWheelController wheelCtrl = panelRoot.AddComponent<SpinningWheelController>();
        SerializedObject wheelSO = new SerializedObject(wheelCtrl);

        wheelSO.FindProperty("wheelTransform").objectReferenceValue = wheelContainerRect;
        wheelSO.FindProperty("btnDraw").objectReferenceValue = btnDraw;
        wheelSO.FindProperty("textDrawLabel").objectReferenceValue = textDrawLabel;
        wheelSO.FindProperty("animator").objectReferenceValue = wheelAnimator;

        // 连接扇区配置
        SerializedProperty segmentsProperty = wheelSO.FindProperty("segments");
        segmentsProperty.arraySize = 8;
        for (int i = 0; i < 8; i++)
        {
            SerializedProperty seg = segmentsProperty.GetArrayElementAtIndex(i);
            seg.FindPropertyRelative("outcome").enumValueIndex = (int)segConfigs[i].outcome;
            SerializedProperty colorProp = seg.FindPropertyRelative("color");
            colorProp.colorValue = segConfigs[i].color;
            seg.FindPropertyRelative("image").objectReferenceValue = segConfigs[i].image;
            seg.FindPropertyRelative("label").objectReferenceValue = segConfigs[i].label;
        }

        // 连接 spinningWheel 到 ShopSystem
        shopSO.FindProperty("spinningWheel").objectReferenceValue = wheelCtrl;

        shopSO.ApplyModifiedPropertiesWithoutUndo();
        wheelSO.ApplyModifiedPropertiesWithoutUndo();

        // ── NextRound 按钮绑定（通过 UnityEvent）──
        // 注意：这个按钮需要在场景中手动绑定到 GameFlowController.NextRound()
        // 因为 GameFlowController 是运行时绑定的

        // 默认隐藏
        panelRoot.SetActive(false);

        Undo.RegisterCreatedObjectUndo(panelRoot, "Create Shop Panel");
        EditorGUIUtility.PingObject(panelRoot);
        Selection.activeGameObject = panelRoot;

        EditorUtility.DisplayDialog("成功",
            "商店面板已创建！\n\n" +
            "下一步：\n" +
            "1. 将 Panel_Shop 拖到 GameFlowController 的 \"Panel Shop\" 字段\n" +
            "2. Btn_NextRound 按钮需绑定 GameFlowController.NextRound()\n" +
            "3. 在 Inspector 中可自定义颜色、布局、文案\n" +
            "4. 转盘扇区颜色和分布在 SpinningWheelController 中调整",
            "确定");

        Debug.Log("Shop Panel Template created successfully!");
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

    private static RectTransform CreateRect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        return rect;
    }

    private static TMP_Text CreateText(string name, Transform parent,
        Vector2 pos, Vector2 size, string text, float fontSize,
        FontStyles style, Color color)
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

    private static Button CreateButtonWithLabel(string name, Transform parent,
        Vector2 pos, Vector2 size, string labelText,
        Color bgColor, Color textColor, out TMP_Text label)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
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
        tmp.text = labelText;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 26;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = textColor;

        label = tmp;
        return btnObj.GetComponent<Button>();
    }

    private static ShopSystem.ShopItemUIRefs CreateItemCard(Transform parent, int index, float y)
    {
        Color cardColor = new Color(0.08f, 0.08f, 0.10f, 0.85f);

        GameObject cardObj = new GameObject($"ItemCard_{index}");
        RectTransform cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.SetParent(parent, false);
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(480f, 180f);
        cardRect.anchoredPosition = new Vector2(0f, y);
        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.color = cardColor;
        cardBg.raycastTarget = false;

        // 图标
        GameObject iconObj = new GameObject("Icon");
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.SetParent(cardRect, false);
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(60f, 60f);
        iconRect.anchoredPosition = new Vector2(-180f, 20f);
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.color = Color.white;
        iconImage.raycastTarget = false;

        // 标题
        TMP_Text titleText = CreateText($"Title", cardRect,
            new Vector2(30f, 50f), new Vector2(300f, 40f),
            $"Item {index + 1}", 28, FontStyles.Bold, new Color(0.98f, 0.83f, 0.22f, 1f));

        // 描述
        TMP_Text descText = CreateText($"Description", cardRect,
            new Vector2(30f, 10f), new Vector2(300f, 50f),
            "Item description", 20, FontStyles.Normal, Color.white);
        descText.enableWordWrapping = true;

        // 价格
        TMP_Text priceText = CreateText($"Price", cardRect,
            new Vector2(-80f, -55f), new Vector2(120f, 35f),
            "$100", 24, FontStyles.Bold, new Color(0.62f, 1f, 0.62f, 1f));

        // 购买按钮
        Button buyButton = CreateButtonWithLabel($"Btn_Buy", cardRect,
            new Vector2(80f, -55f), new Vector2(140f, 40f),
            "BUY", new Color(0.95f, 0.95f, 0.95f, 0.92f), new Color(0.16f, 0.16f, 0.16f, 1f),
            out TMP_Text buyButtonLabel);

        return new ShopSystem.ShopItemUIRefs
        {
            iconImage = iconImage,
            titleText = titleText,
            descText = descText,
            priceText = priceText,
            buyButton = buyButton,
            buyButtonLabel = buyButtonLabel,
        };
    }

    private static SpinningWheelController.SegmentConfig CreateWheelSegment(
        Transform parent, int index, float degreesPerSegment)
    {
        float rotation = index * degreesPerSegment;

        // 创建扇形 Image
        GameObject segObj = new GameObject($"Segment_{index}");
        RectTransform segRect = segObj.AddComponent<RectTransform>();
        segRect.SetParent(parent, false);
        segRect.anchorMin = new Vector2(0.5f, 0.5f);
        segRect.anchorMax = new Vector2(0.5f, 0.5f);
        segRect.sizeDelta = new Vector2(360f, 360f);
        segRect.anchoredPosition = Vector2.zero;
        segRect.localEulerAngles = new Vector3(0f, 0f, -rotation);

        Image segImage = segObj.AddComponent<Image>();
        // 必须分配一个 Sprite，否则 Filled 模式的径向裁切不生效（会渲染成完整矩形）
        segImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        segImage.type = Image.Type.Filled;
        segImage.fillMethod = Image.FillMethod.Radial360;
        segImage.fillAmount = 1f / SpinningWheelController.SegmentCount;
        segImage.fillClockwise = true;
        segImage.fillOrigin = (int)Image.Origin360.Top;
        segImage.color = DefaultSegmentColors[index];
        segImage.raycastTarget = false;

        // 在扇区中心放置标签
        float labelAngle = (degreesPerSegment * 0.5f) * Mathf.Deg2Rad;
        float labelRadius = 120f;
        // 标签位置：相对于扇区起始，偏移半个扇区角度
        Vector2 labelPos = new Vector2(
            Mathf.Sin(labelAngle) * labelRadius,
            Mathf.Cos(labelAngle) * labelRadius
        );

        GameObject labelObj = new GameObject("Label");
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.SetParent(segRect, false);
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta = new Vector2(80f, 30f);
        labelRect.anchoredPosition = labelPos;
        // 反转标签旋转使文字正向
        labelRect.localEulerAngles = new Vector3(0f, 0f, rotation);

        TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = DefaultSegmentLabels[index];
        labelTmp.fontSize = 16;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.color = Color.white;
        labelTmp.raycastTarget = false;
        labelTmp.outlineWidth = 0.2f;
        labelTmp.outlineColor = Color.black;

        return new SpinningWheelController.SegmentConfig
        {
            outcome = DefaultSegmentOutcomes[index],
            color = DefaultSegmentColors[index],
            image = segImage,
            label = labelTmp,
        };
    }
}

#endif
