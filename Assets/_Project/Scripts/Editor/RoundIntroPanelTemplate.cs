using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Creates a round intro overlay panel with editable text fields.
/// Menu: GameObject > DebtRunner > Create Round Intro Panel
/// Also creates a round clear overlay in the same step.
/// </summary>
public static class RoundIntroPanelTemplate
{
    private static readonly Color gold = new Color(0.95f, 0.80f, 0.12f, 1f);
    private static readonly Color clearAccent = new Color(0.20f, 1f, 0.72f, 1f);

    [MenuItem("GameObject/DebtRunner/Create Round Intro Panel")]
    public static void CreateRoundIntroPanel()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found.", "OK");
            return;
        }

        bool createdIntro = false;
        bool createdClear = false;

        // --- Round Intro ---
        const string introName = "Panel_RoundIntro";
        Transform existingIntro = canvas.transform.Find(introName);
        if (existingIntro != null)
        {
            Selection.activeGameObject = existingIntro.gameObject;
        }
        else
        {
            CreateRoundIntroOverlay(canvas.transform, introName);
            createdIntro = true;
        }

        // --- Round Clear ---
        const string clearName = "Panel_RoundClear";
        Transform existingClear = canvas.transform.Find(clearName);
        if (existingClear == null)
        {
            CreateRoundClearOverlay(canvas.transform, clearName);
            createdClear = true;
        }

        // Auto-bind to GameFlowController
        GameFlowController gfc = Object.FindObjectOfType<GameFlowController>();
        if (gfc != null)
        {
            SerializedObject gfcSo = new SerializedObject(gfc);

            if (createdIntro)
            {
                Transform intro = canvas.transform.Find(introName);
                if (intro != null)
                {
                    CanvasGroup cg = intro.GetComponent<CanvasGroup>();
                    gfcSo.FindProperty("roundIntroOverlay").objectReferenceValue = cg;

                    Transform roundText = intro.Find("RoundText");
                    if (roundText != null)
                        gfcSo.FindProperty("roundIntroRoundText").objectReferenceValue = roundText.GetComponent<TMP_Text>();

                    Transform debtText = intro.Find("DebtText");
                    if (debtText != null)
                        gfcSo.FindProperty("roundIntroDebtText").objectReferenceValue = debtText.GetComponent<TMP_Text>();

                    Transform hintText = intro.Find("ContinueHintText");
                    if (hintText != null)
                        gfcSo.FindProperty("roundIntroContinueHintText").objectReferenceValue = hintText.GetComponent<TMP_Text>();
                }
            }

            if (createdClear)
            {
                Transform clear = canvas.transform.Find(clearName);
                if (clear != null)
                {
                    CanvasGroup cg = clear.GetComponent<CanvasGroup>();
                    gfcSo.FindProperty("roundClearOverlay").objectReferenceValue = cg;

                    Transform titleText = clear.Find("RoundClearTitleText");
                    if (titleText != null)
                        gfcSo.FindProperty("roundClearTitleText").objectReferenceValue = titleText.GetComponent<TMP_Text>();

                    Transform subText = clear.Find("RoundClearSubText");
                    if (subText != null)
                        gfcSo.FindProperty("roundClearSubText").objectReferenceValue = subText.GetComponent<TMP_Text>();
                }
            }

            gfcSo.ApplyModifiedPropertiesWithoutUndo();
        }

        string msg = "";
        if (createdIntro) msg += $"{introName} created.\n";
        else msg += $"{introName} already exists.\n";
        if (createdClear) msg += $"{clearName} created.\n";
        else msg += $"{clearName} already exists.\n";
        msg += "\nAll references auto-bound to GameFlowController.";

        EditorUtility.DisplayDialog("Done", msg, "OK");

        if (createdIntro)
            Selection.activeGameObject = canvas.transform.Find(introName)?.gameObject;
    }

    private static void CreateRoundIntroOverlay(Transform parent, string name)
    {
        // Root — fullscreen dark overlay
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        Stretch(rootRect);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.82f);
        bg.raycastTarget = false;

        CanvasGroup cg = root.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // Upper decorative line
        CreateLine(root.transform, "LineTop", 70f, gold);

        // Round text — "ROUND 1/5"
        CreateText(root.transform, "RoundText",
            new Vector2(0f, 165f), new Vector2(980f, 120f),
            88f, gold, FontStyles.Bold, "ROUND 1/5");

        // Lower decorative line
        CreateLine(root.transform, "LineBottom", -190f, gold);

        // Debt text — "DEBT OWED\n$220"
        CreateText(root.transform, "DebtText",
            new Vector2(0f, -55f), new Vector2(980f, 300f),
            90f, gold, FontStyles.Bold, "DEBT   OWED\n$220");

        // Continue hint text
        TMP_Text hintTmp = CreateText(root.transform, "ContinueHintText",
            new Vector2(0f, -250f), new Vector2(980f, 80f),
            36f, Color.white, FontStyles.Normal, "Press Any Key to Continue");
        hintTmp.gameObject.SetActive(false);

        root.SetActive(false);
        Undo.RegisterCreatedObjectUndo(root, "Create Round Intro Panel");
    }

    private static void CreateRoundClearOverlay(Transform parent, string name)
    {
        // Root — fullscreen dark overlay
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        Stretch(rootRect);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);
        bg.raycastTarget = false;

        CanvasGroup cg = root.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // Title text — "ROUND CLEAR"
        CreateText(root.transform, "RoundClearTitleText",
            new Vector2(0f, 46f), new Vector2(1000f, 180f),
            96f, clearAccent, FontStyles.Bold, "ROUND CLEAR");

        // Sub text — "Round 1/5"
        CreateText(root.transform, "RoundClearSubText",
            new Vector2(0f, -110f), new Vector2(1000f, 180f),
            46f, Color.white, FontStyles.Normal, "Round 1/5");

        root.SetActive(false);
        Undo.RegisterCreatedObjectUndo(root, "Create Round Clear Panel");
    }

    private static void CreateLine(Transform parent, string name, float y, Color color)
    {
        GameObject line = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(820f, 4f);
        rect.anchoredPosition = new Vector2(0f, y);

        Image img = line.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    private static TMP_Text CreateText(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, Color color, FontStyles style, string defaultText)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        tmp.outlineWidth = 0.25f;
        tmp.raycastTarget = false;

        return tmp;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
