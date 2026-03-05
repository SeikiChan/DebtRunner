using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;

/// <summary>
/// One-click settlement panel generator (gold frame + two-column layout).
/// </summary>
public static class SettlementPanelTemplate
{
    [MenuItem("GameObject/DebtRunner/Create Settlement Panel Template")]
    public static void CreateSettlementPanelTemplate()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found. Create a Canvas first.", "OK");
            return;
        }

        const string panelName = "Panel_Settlement";
        Transform existing = canvas.transform.Find(panelName);
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Hint", $"{panelName} already exists.", "OK");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        GameObject panelRoot = new GameObject(panelName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.SetParent(canvas.transform, false);
        Stretch(panelRect);
        Image panelImage = panelRoot.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0f);
        panelImage.raycastTarget = false;

        RectTransform bgRect = CreateRect("Background", panelRect, Vector2.zero, Vector2.zero);
        Stretch(bgRect);
        Image bgImage = bgRect.gameObject.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.70f);
        bgImage.raycastTarget = true;

        RectTransform frameRect = CreateRect("Frame", panelRect, new Vector2(0f, 0f), new Vector2(1280f, 710f));
        Image frameImage = frameRect.gameObject.AddComponent<Image>();
        frameImage.color = new Color(0.07f, 0.07f, 0.10f, 0.96f);
        frameImage.raycastTarget = false;
        AddGoldOutline(frameRect.gameObject, 5f);

        TMP_Text titleText = CreateText(
            "Title",
            frameRect,
            new Vector2(0f, 285f),
            new Vector2(1000f, 90f),
            "ROUND CLEARED",
            72f,
            FontStyles.Bold,
            new Color(0.95f, 0.85f, 0.60f, 1f),
            TextAlignmentOptions.Center);
        titleText.outlineWidth = 0.18f;
        titleText.outlineColor = new Color(0f, 0f, 0f, 0.95f);

        CreateText(
            "Subtitle",
            frameRect,
            new Vector2(0f, 225f),
            new Vector2(1000f, 50f),
            "Cash Out & Settle Debt",
            40f,
            FontStyles.Normal,
            new Color(0.86f, 0.86f, 0.90f, 1f),
            TextAlignmentOptions.Center);

        RectTransform leftPanel = CreateRect("SummaryPanel", frameRect, new Vector2(-210f, 10f), new Vector2(760f, 360f));
        Image leftBg = leftPanel.gameObject.AddComponent<Image>();
        leftBg.color = new Color(0.09f, 0.09f, 0.12f, 0.92f);
        leftBg.raycastTarget = false;
        AddGoldOutline(leftPanel.gameObject, 3f);

        TMP_Text valCashEarned = CreateSummaryRow(leftPanel, 95f, "CASH EARNED", new Color(0.18f, 0.95f, 0.82f, 1f), " +$0");
        TMP_Text valDebtPayment = CreateSummaryRow(leftPanel, 0f, "DEBT PAYMENT", new Color(1f, 0.33f, 0.40f, 1f), " -$0");
        TMP_Text valNetCash = CreateSummaryRow(leftPanel, -95f, "NET CASH", new Color(0.22f, 1f, 0.42f, 1f), " +$0");

        CreateSeparator(leftPanel, 48f);
        CreateSeparator(leftPanel, -48f);

        RectTransform rightPanel = CreateRect("DebtPanel", frameRect, new Vector2(355f, 10f), new Vector2(390f, 360f));
        Image rightBg = rightPanel.gameObject.AddComponent<Image>();
        rightBg.color = new Color(0.09f, 0.09f, 0.12f, 0.92f);
        rightBg.raycastTarget = false;
        AddGoldOutline(rightPanel.gameObject, 3f);

        CreateText(
            "Text_NextDebtLabel",
            rightPanel,
            new Vector2(0f, 130f),
            new Vector2(340f, 48f),
            "NEXT ROUND DEBT",
            42f,
            FontStyles.Bold,
            new Color(0.95f, 0.78f, 0.34f, 1f),
            TextAlignmentOptions.Center);

        TMP_Text valNextDebt = CreateText(
            "Text_SettlementNextRoundDebt",
            rightPanel,
            new Vector2(0f, 58f),
            new Vector2(340f, 90f),
            "$0",
            74f,
            FontStyles.Bold,
            new Color(0.96f, 0.86f, 0.48f, 1f),
            TextAlignmentOptions.Center);

        CreateSeparator(rightPanel, -8f, 300f);

        CreateText(
            "Text_RoundsLeftLabel",
            rightPanel,
            new Vector2(0f, -64f),
            new Vector2(320f, 44f),
            "ROUNDS LEFT",
            38f,
            FontStyles.Bold,
            new Color(0.95f, 0.78f, 0.34f, 1f),
            TextAlignmentOptions.Center);

        TMP_Text valRoundsLeft = CreateText(
            "Text_SettlementRoundsLeft",
            rightPanel,
            new Vector2(0f, -118f),
            new Vector2(320f, 80f),
            "0",
            70f,
            FontStyles.Bold,
            new Color(0.92f, 0.92f, 0.95f, 1f),
            TextAlignmentOptions.Center);

        Button btnNext = CreateButton(
            "Btn_SettlementNext",
            frameRect,
            new Vector2(0f, -285f),
            new Vector2(320f, 74f),
            "NEXT",
            new Color(0.95f, 0.66f, 0.22f, 1f),
            new Color(0.13f, 0.08f, 0.02f, 1f));

        BindToGameFlowController(
            panelRoot,
            btnNext,
            valCashEarned,
            valDebtPayment,
            valNetCash,
            valNextDebt,
            valRoundsLeft);

        // Keep visible in edit mode so artists/designers can tweak immediately.
        panelRoot.SetActive(true);

        Undo.RegisterCreatedObjectUndo(panelRoot, "Create Settlement Panel");
        EditorGUIUtility.PingObject(panelRoot);
        Selection.activeGameObject = panelRoot;

        EditorUtility.DisplayDialog(
            "Created",
            "Settlement panel template created.\n\n" +
            "It displays:\n" +
            "- Cash earned this round\n" +
            "- Debt payment this round\n" +
            "- Net cash after payment\n" +
            "- Next round debt\n" +
            "- Rounds left\n\n" +
            "NEXT button is auto-bound when GameFlowController exists in scene.",
            "OK");
    }

    private static TMP_Text CreateSummaryRow(Transform parent, float y, string label, Color valueColor, string defaultValue)
    {
        CreateText(
            $"Label_{label.Replace(" ", string.Empty)}",
            parent,
            new Vector2(-245f, y),
            new Vector2(300f, 52f),
            label,
            44f,
            FontStyles.Bold,
            new Color(0.95f, 0.84f, 0.56f, 1f),
            TextAlignmentOptions.Left);

        return CreateText(
            $"Text_Settlement{label.Replace(" ", string.Empty)}",
            parent,
            new Vector2(200f, y),
            new Vector2(260f, 60f),
            defaultValue,
            52f,
            FontStyles.Bold,
            valueColor,
            TextAlignmentOptions.Right);
    }

    private static void BindToGameFlowController(
        GameObject panelRoot,
        Button btnNext,
        TMP_Text cashEarned,
        TMP_Text debtPayment,
        TMP_Text netCash,
        TMP_Text nextDebt,
        TMP_Text roundsLeft)
    {
        GameFlowController flow = Object.FindObjectOfType<GameFlowController>();
        if (flow == null)
            return;

        SerializedObject so = new SerializedObject(flow);
        AssignRefIfPropertyExists(so, "panelSettlement", panelRoot);
        AssignRefIfPropertyExists(so, "textPaid", cashEarned);
        AssignRefIfPropertyExists(so, "textDue", debtPayment);
        AssignRefIfPropertyExists(so, "textRemainingDebt", nextDebt);
        AssignRefIfPropertyExists(so, "textSettlementCashEarned", cashEarned);
        AssignRefIfPropertyExists(so, "textSettlementDebtPayment", debtPayment);
        AssignRefIfPropertyExists(so, "textSettlementNetCash", netCash);
        AssignRefIfPropertyExists(so, "textSettlementNextRoundDebt", nextDebt);
        AssignRefIfPropertyExists(so, "textSettlementRoundsLeft", roundsLeft);
        so.ApplyModifiedPropertiesWithoutUndo();

        for (int i = btnNext.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(btnNext.onClick, i);
        UnityEventTools.AddPersistentListener(btnNext.onClick, flow.ConfirmSettlementAndEnterShop);

        EditorUtility.SetDirty(flow);
        EditorUtility.SetDirty(btnNext);
    }

    private static void AssignRefIfPropertyExists(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
            prop.objectReferenceValue = value;
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
            50f,
            FontStyles.Bold,
            textColor,
            TextAlignmentOptions.Center);
        RectTransform labelRect = labelText.rectTransform;
        Stretch(labelRect);

        return btnObj.GetComponent<Button>();
    }

    private static void CreateSeparator(Transform parent, float y, float width = 680f)
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
