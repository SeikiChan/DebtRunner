using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class FirstRunHudGuideController : MonoBehaviour
{
    private enum HudGuideTargetType
    {
        Health = 0,
        Timer = 1,
        Cash = 2,
        Debt = 3,
    }

    [Serializable]
    private class HudGuideStep
    {
        public HudGuideTargetType targetType = HudGuideTargetType.Health;
        public string title = "HEALTH";
        [TextArea(2, 4)] public string description = "Lose all hearts and the run is over.";
        public Vector2 calloutOffset = new Vector2(0f, -150f);
        public Vector2 calloutSize = new Vector2(460f, 150f);
        public Vector2 highlightPadding = new Vector2(24f, 18f);
        public Vector2 highlightSize = new Vector2(220f, 72f);
        public RectTransform sceneCalloutAnchor;
        public RectTransform sceneHighlightAnchor;
    }

    [Header("First-Run HUD Guide")]
    [SerializeField] private bool enableFirstRunHudGuide = true;
    [SerializeField] private string continueHint = "CLICK TO CONTINUE";
    [SerializeField] private List<HudGuideStep> steps = new List<HudGuideStep>
    {
        new HudGuideStep
        {
            targetType = HudGuideTargetType.Health,
            title = "HEALTH",
            description = "This is your survivability. If it drops to zero, the run ends.",
            calloutOffset = new Vector2(0f, -150f),
        },
        new HudGuideStep
        {
            targetType = HudGuideTargetType.Timer,
            title = "TIME",
            description = "When the timer ends, the round settles immediately.",
            calloutOffset = new Vector2(0f, -150f),
        },
        new HudGuideStep
        {
            targetType = HudGuideTargetType.Cash,
            title = "CASH",
            description = "Kills drop cash. Cash pays debt and buys upgrades.",
            calloutOffset = new Vector2(0f, -140f),
        },
        new HudGuideStep
        {
            targetType = HudGuideTargetType.Debt,
            title = "DEBT",
            description = "You must cover this payment each round or the run is lost.",
            calloutOffset = new Vector2(0f, -150f),
        },
    };

    [Header("Visuals")]
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.72f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.83f, 0.34f, 1f);
    [SerializeField] private Color calloutBackgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.92f);
    [SerializeField] private Color titleColor = new Color(1f, 0.94f, 0.78f, 1f);
    [SerializeField] private Color descriptionColor = Color.white;
    [SerializeField] private Color continueHintColor = new Color(1f, 1f, 1f, 0.88f);
    [SerializeField, Min(1f)] private float titleFontSize = 34f;
    [SerializeField, Min(1f)] private float descriptionFontSize = 24f;
    [SerializeField, Min(1f)] private float continueHintFontSize = 20f;
    [SerializeField, Min(1f)] private float highlightThickness = 6f;

    private HealthUI healthUI;
    private TMP_Text textCash;
    private TMP_Text textDebt;
    private TMP_Text textCountdown;
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private Image overlayBackdrop;
    [SerializeField] private RectTransform highlightRoot;
    [SerializeField] private Image highlightTop;
    [SerializeField] private Image highlightBottom;
    [SerializeField] private Image highlightLeft;
    [SerializeField] private Image highlightRight;
    [SerializeField] private RectTransform calloutRoot;
    [SerializeField] private Image calloutBackground;
    [SerializeField] private TMP_Text calloutTitleText;
    [SerializeField] private TMP_Text calloutDescriptionText;
    [SerializeField] private TMP_Text continueHintText;
    [SerializeField] private RectTransform sceneCalloutAnchorsRoot;
    [SerializeField] private RectTransform sceneHighlightAnchorsRoot;
    private bool overlayAutoCreated;

    public bool IsShowing { get; private set; }

    private void Awake()
    {
        if (Application.isPlaying)
            HideGuideImmediate();
    }

    public void Bind(HealthUI healthUiRef, TMP_Text cashText, TMP_Text debtText, TMP_Text countdownText)
    {
        healthUI = healthUiRef;
        textCash = cashText;
        textDebt = debtText;
        textCountdown = countdownText;
    }

    public bool ShouldShowGuide()
    {
        return enableFirstRunHudGuide;
    }

    public IEnumerator PlayGuideSequence()
    {
        if (!ShouldShowGuide())
            yield break;

        if (!EnsureOverlay())
        {
            RunLogger.Warning("FirstRunHudGuide: overlay could not be created.");
            yield break;
        }

        List<HudGuideStep> validSteps = GetValidSteps();
        if (validSteps.Count <= 0)
        {
            RunLogger.Warning("FirstRunHudGuide: no valid HUD targets found.");
            yield break;
        }

        IsShowing = true;
        overlayCanvasGroup.gameObject.SetActive(true);
        overlayCanvasGroup.alpha = 1f;
        overlayCanvasGroup.interactable = true;
        overlayCanvasGroup.blocksRaycasts = true;
        overlayCanvasGroup.transform.SetAsLastSibling();

        yield return null;

        for (int i = 0; i < validSteps.Count; i++)
        {
            ApplyStep(validSteps[i]);
            yield return WaitForAdvanceInput();
        }

        HideGuideImmediate();
    }

    public void StopGuideImmediate()
    {
        HideGuideImmediate();
    }

    [ContextMenu("Authoring/Create Editable Overlay In Scene")]
    private void CreateEditableOverlayInScene()
    {
        if (!EnsureOverlay(authoringMode: true))
        {
            RunLogger.Warning("FirstRunHudGuide: failed to create editable scene overlay.");
            return;
        }

        EnsureSceneCalloutAnchors();
        EnsureSceneHighlightAnchors();
        ApplyVisualSettings();
        ShowSceneOverlayPreview();
        MarkAuthoringObjectsDirty();
    }

    [ContextMenu("Authoring/Refresh Scene Overlay Visuals")]
    private void RefreshSceneOverlayVisuals()
    {
        if (!EnsureOverlay())
            return;

        ApplyVisualSettings();
        MarkAuthoringObjectsDirty();
    }

    [ContextMenu("Authoring/Show Scene Overlay Preview")]
    private void ShowSceneOverlayPreview()
    {
        if (!EnsureOverlay(authoringMode: true))
            return;

        EnsureSceneCalloutAnchors();
        EnsureSceneHighlightAnchors();
        overlayCanvasGroup.gameObject.SetActive(true);
        overlayCanvasGroup.alpha = 1f;
        overlayCanvasGroup.interactable = false;
        overlayCanvasGroup.blocksRaycasts = false;

        HudGuideStep previewStep = steps.Count > 0 ? steps[0] : null;
        if (previewStep != null)
        {
            Rect previewHighlightRect = ResolveHighlightRectForStep(previewStep);
            LayoutHighlight(previewHighlightRect);
            LayoutCallout(Vector2.zero, previewStep);
            calloutTitleText.text = string.IsNullOrWhiteSpace(previewStep.title) ? "TITLE" : previewStep.title;
            calloutDescriptionText.text = string.IsNullOrWhiteSpace(previewStep.description) ? "Description preview" : previewStep.description;
        }
        else
        {
            calloutRoot.anchoredPosition = Vector2.zero;
            calloutRoot.sizeDelta = new Vector2(460f, 150f);
            highlightRoot.anchoredPosition = Vector2.zero;
            highlightRoot.sizeDelta = new Vector2(220f, 72f);
            calloutTitleText.text = "TITLE";
            calloutDescriptionText.text = "Description preview";
        }

        continueHintText.text = continueHint;
        MarkAuthoringObjectsDirty();
    }

    [ContextMenu("Authoring/Hide Scene Overlay Preview")]
    private void HideSceneOverlayPreview()
    {
        HideGuideImmediate();
        MarkAuthoringObjectsDirty();
    }

    private void OnDisable()
    {
        HideGuideImmediate();
    }

    private void HideGuideImmediate()
    {
        IsShowing = false;
        if (overlayCanvasGroup == null)
            return;

        overlayCanvasGroup.alpha = 0f;
        overlayCanvasGroup.interactable = false;
        overlayCanvasGroup.blocksRaycasts = false;
        overlayCanvasGroup.gameObject.SetActive(false);
    }

    private bool EnsureOverlay(bool authoringMode = false)
    {
        if (HasOverlayReferences())
        {
            EnsureAuthoringRoots(authoringMode);
            ApplyVisualSettings();
            return true;
        }

        if (overlayAutoCreated)
            return false;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            return false;

        Transform parent = transform.parent != null ? transform.parent : canvas.transform;
        string rootName = authoringMode ? "FirstRunHudGuideOverlay" : "FirstRunHudGuideOverlayAuto";
        GameObject root = new GameObject(rootName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        overlayCanvasGroup = root.GetComponent<CanvasGroup>();
        overlayCanvasGroup.alpha = 0f;
        overlayCanvasGroup.interactable = false;
        overlayCanvasGroup.blocksRaycasts = false;

        overlayBackdrop = root.GetComponent<Image>();
        overlayBackdrop.color = backdropColor;
        overlayBackdrop.raycastTarget = true;

        highlightRoot = CreateRect(root.transform, "HighlightRoot");
        highlightTop = CreateSolidImage(highlightRoot, "Top");
        highlightBottom = CreateSolidImage(highlightRoot, "Bottom");
        highlightLeft = CreateSolidImage(highlightRoot, "Left");
        highlightRight = CreateSolidImage(highlightRoot, "Right");

        calloutRoot = CreateRect(root.transform, "CalloutRoot");
        calloutBackground = calloutRoot.gameObject.AddComponent<Image>();

        calloutTitleText = CreateLabel(calloutRoot, "Title", new Vector2(0f, -14f), new Vector2(420f, 40f), titleFontSize, titleColor, FontStyles.Bold);
        calloutDescriptionText = CreateLabel(calloutRoot, "Description", new Vector2(0f, -62f), new Vector2(420f, 78f), descriptionFontSize, descriptionColor, FontStyles.Normal);
        calloutDescriptionText.enableWordWrapping = true;
        calloutDescriptionText.alignment = TextAlignmentOptions.TopLeft;
        continueHintText = CreateLabel(calloutRoot, "ContinueHint", new Vector2(0f, -112f), new Vector2(420f, 24f), continueHintFontSize, continueHintColor, FontStyles.Bold);
        continueHintText.alignment = TextAlignmentOptions.BottomRight;

        sceneCalloutAnchorsRoot = CreateFullScreenRect(root.transform, "StepCalloutAnchors");
        sceneCalloutAnchorsRoot.gameObject.SetActive(authoringMode);
        sceneHighlightAnchorsRoot = CreateFullScreenRect(root.transform, "StepHighlightAnchors");
        sceneHighlightAnchorsRoot.gameObject.SetActive(authoringMode);

        ApplyVisualSettings();
        root.SetActive(authoringMode);
        overlayAutoCreated = !authoringMode;
        return true;
    }

    private void EnsureAuthoringRoots(bool authoringMode)
    {
        Transform overlayRoot = overlayCanvasGroup != null ? overlayCanvasGroup.transform : null;
        if (overlayRoot == null)
            return;

        if (sceneCalloutAnchorsRoot == null)
            sceneCalloutAnchorsRoot = FindChildRect(overlayRoot as RectTransform, "StepCalloutAnchors");
        if (sceneCalloutAnchorsRoot == null)
            sceneCalloutAnchorsRoot = CreateFullScreenRect(overlayRoot, "StepCalloutAnchors");

        if (sceneHighlightAnchorsRoot == null)
            sceneHighlightAnchorsRoot = FindChildRect(overlayRoot as RectTransform, "StepHighlightAnchors");
        if (sceneHighlightAnchorsRoot == null)
            sceneHighlightAnchorsRoot = CreateFullScreenRect(overlayRoot, "StepHighlightAnchors");

        sceneCalloutAnchorsRoot.gameObject.SetActive(authoringMode);
        sceneHighlightAnchorsRoot.gameObject.SetActive(authoringMode);
    }

    private bool HasOverlayReferences()
    {
        return overlayCanvasGroup != null
            && overlayBackdrop != null
            && highlightRoot != null
            && highlightTop != null
            && highlightBottom != null
            && highlightLeft != null
            && highlightRight != null
            && calloutRoot != null
            && calloutBackground != null
            && calloutTitleText != null
            && calloutDescriptionText != null
            && continueHintText != null;
    }

    private void ApplyVisualSettings()
    {
        if (overlayBackdrop != null)
        {
            overlayBackdrop.color = backdropColor;
            overlayBackdrop.raycastTarget = true;
        }

        if (calloutBackground != null)
        {
            calloutBackground.color = calloutBackgroundColor;
            calloutBackground.raycastTarget = false;
        }

        ApplyLabelStyle(calloutTitleText, titleFontSize, titleColor, FontStyles.Bold, TextAlignmentOptions.TopLeft, false);
        ApplyLabelStyle(calloutDescriptionText, descriptionFontSize, descriptionColor, FontStyles.Normal, TextAlignmentOptions.TopLeft, true);
        ApplyLabelStyle(continueHintText, continueHintFontSize, continueHintColor, FontStyles.Bold, TextAlignmentOptions.BottomRight, false);
    }

    private void ApplyLabelStyle(TMP_Text text, float fontSize, Color color, FontStyles fontStyle, TextAlignmentOptions alignment, bool wordWrap)
    {
        if (text == null)
            return;

        TMP_FontAsset font = ResolveGuideFont();
        if (font != null)
            text.font = font;

        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.enableWordWrapping = wordWrap;
        text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        text.outlineWidth = 0.2f;
        text.raycastTarget = false;
    }

    private void EnsureSceneCalloutAnchors()
    {
        if (sceneCalloutAnchorsRoot == null)
            return;

        for (int i = 0; i < steps.Count; i++)
        {
            HudGuideStep step = steps[i];
            if (step == null)
                continue;

            if (step.sceneCalloutAnchor == null)
            {
                string anchorName = $"{step.targetType}CalloutAnchor";
                RectTransform anchor = FindChildRect(sceneCalloutAnchorsRoot, anchorName);
                if (anchor == null)
                {
                    anchor = CreateRect(sceneCalloutAnchorsRoot, anchorName);
                    anchor.sizeDelta = new Vector2(
                        Mathf.Max(240f, step.calloutSize.x),
                        Mathf.Max(110f, step.calloutSize.y));
                    anchor.anchoredPosition = ResolveDefaultCalloutPosition(step);
                }

                step.sceneCalloutAnchor = anchor;
            }
        }
    }

    private void EnsureSceneHighlightAnchors()
    {
        if (sceneHighlightAnchorsRoot == null)
            return;

        for (int i = 0; i < steps.Count; i++)
        {
            HudGuideStep step = steps[i];
            if (step == null)
                continue;

            if (step.sceneHighlightAnchor == null)
            {
                string anchorName = $"{step.targetType}HighlightAnchor";
                RectTransform anchor = FindChildRect(sceneHighlightAnchorsRoot, anchorName);
                if (anchor == null)
                {
                    anchor = CreateRect(sceneHighlightAnchorsRoot, anchorName);
                    anchor.sizeDelta = ResolveDefaultHighlightSize(step);
                    anchor.anchoredPosition = ResolveDefaultHighlightPosition(step);
                }

                step.sceneHighlightAnchor = anchor;
            }
        }
    }

    private List<HudGuideStep> GetValidSteps()
    {
        List<HudGuideStep> valid = new List<HudGuideStep>(steps.Count);
        for (int i = 0; i < steps.Count; i++)
        {
            HudGuideStep step = steps[i];
            if (step == null || ResolveTarget(step.targetType) == null)
                continue;

            valid.Add(step);
        }

        return valid;
    }

    private IEnumerator WaitForAdvanceInput()
    {
        yield return null;
        while (Input.GetMouseButton(0))
            yield return null;

        while (true)
        {
            if (Input.GetMouseButtonDown(0)
                || Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                yield break;
            }

            yield return null;
        }
    }

    private void ApplyStep(HudGuideStep step)
    {
        RectTransform target = ResolveTarget(step.targetType);
        if (target == null)
            return;

        Canvas.ForceUpdateCanvases();
        Rect targetRect = GetTargetRectInOverlaySpace(target);
        LayoutHighlight(ResolveHighlightRectForStep(step, targetRect));
        LayoutCallout(targetRect.center, step);

        calloutTitleText.text = string.IsNullOrWhiteSpace(step.title) ? step.targetType.ToString().ToUpperInvariant() : step.title;
        calloutDescriptionText.text = step.description ?? string.Empty;
        continueHintText.text = continueHint;
    }

    private RectTransform ResolveTarget(HudGuideTargetType targetType)
    {
        switch (targetType)
        {
            case HudGuideTargetType.Health:
                return healthUI != null ? healthUI.transform as RectTransform : null;
            case HudGuideTargetType.Timer:
                return textCountdown != null ? textCountdown.rectTransform : null;
            case HudGuideTargetType.Cash:
                return textCash != null ? textCash.rectTransform : null;
            case HudGuideTargetType.Debt:
                return textDebt != null ? textDebt.rectTransform : null;
            default:
                return null;
        }
    }

    private Rect GetTargetRectInOverlaySpace(RectTransform target)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        RectTransform overlayRect = overlayCanvasGroup.transform as RectTransform;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < corners.Length; i++)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRect,
                RectTransformUtility.WorldToScreenPoint(null, corners[i]),
                null,
                out Vector2 localPoint);
            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void LayoutHighlight(Rect targetRect)
    {
        float thickness = Mathf.Max(1f, highlightThickness);
        float width = targetRect.width;
        float height = targetRect.height;
        highlightRoot.sizeDelta = new Vector2(width, height);
        highlightRoot.anchoredPosition = targetRect.center;

        SetHighlightImage(highlightTop, new Vector2(0f, (height - thickness) * 0.5f), new Vector2(width, thickness));
        SetHighlightImage(highlightBottom, new Vector2(0f, (thickness - height) * 0.5f), new Vector2(width, thickness));
        SetHighlightImage(highlightLeft, new Vector2((thickness - width) * 0.5f, 0f), new Vector2(thickness, height));
        SetHighlightImage(highlightRight, new Vector2((width - thickness) * 0.5f, 0f), new Vector2(thickness, height));
    }

    private void LayoutCallout(Vector2 targetCenter, HudGuideStep step)
    {
        RectTransform overlayRect = overlayCanvasGroup.transform as RectTransform;
        Vector2 size;
        Vector2 position;

        if (step.sceneCalloutAnchor != null)
        {
            Rect anchorRect = GetTargetRectInOverlaySpace(step.sceneCalloutAnchor);
            size = new Vector2(
                Mathf.Max(240f, anchorRect.width),
                Mathf.Max(110f, anchorRect.height));
            position = anchorRect.center;
        }
        else
        {
            size = new Vector2(
                Mathf.Max(240f, step.calloutSize.x),
                Mathf.Max(110f, step.calloutSize.y));
            position = targetCenter + step.calloutOffset;
        }

        Vector2 canvasHalf = overlayRect != null ? overlayRect.rect.size * 0.5f : new Vector2(960f, 540f);
        float margin = 28f;
        position.x = Mathf.Clamp(position.x, -canvasHalf.x + (size.x * 0.5f) + margin, canvasHalf.x - (size.x * 0.5f) - margin);
        position.y = Mathf.Clamp(position.y, -canvasHalf.y + (size.y * 0.5f) + margin, canvasHalf.y - (size.y * 0.5f) - margin);

        calloutRoot.sizeDelta = size;
        calloutRoot.anchoredPosition = position;
    }

    private void SetHighlightImage(Image image, Vector2 anchoredPosition, Vector2 size)
    {
        if (image == null)
            return;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        image.color = highlightColor;
        image.raycastTarget = false;
    }

    private RectTransform CreateRect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private RectTransform CreateFullScreenRect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private Image CreateSolidImage(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        return go.GetComponent<Image>();
    }

    private TMP_Text CreateLabel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, float fontSize, Color color, FontStyles fontStyle)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.font = ResolveGuideFont();
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = fontStyle;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = false;
        text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        text.outlineWidth = 0.2f;
        text.raycastTarget = false;
        return text;
    }

    private TMP_FontAsset ResolveGuideFont()
    {
        if (textCash != null && textCash.font != null)
            return textCash.font;
        if (textDebt != null && textDebt.font != null)
            return textDebt.font;
        if (textCountdown != null && textCountdown.font != null)
            return textCountdown.font;

        return TMP_Settings.defaultFontAsset;
    }

    private Vector2 ResolveDefaultCalloutPosition(HudGuideStep step)
    {
        RectTransform target = ResolveTarget(step.targetType);
        if (target == null || overlayCanvasGroup == null)
            return step.calloutOffset;

        Canvas.ForceUpdateCanvases();
        Rect targetRect = GetTargetRectInOverlaySpace(target);
        return targetRect.center + step.calloutOffset;
    }

    private Vector2 ResolveDefaultHighlightPosition(HudGuideStep step)
    {
        RectTransform target = ResolveTarget(step.targetType);
        if (target == null || overlayCanvasGroup == null)
            return Vector2.zero;

        Canvas.ForceUpdateCanvases();
        Rect targetRect = GetTargetRectInOverlaySpace(target);
        return targetRect.center;
    }

    private Vector2 ResolveDefaultHighlightSize(HudGuideStep step)
    {
        RectTransform target = ResolveTarget(step.targetType);
        if (target == null || overlayCanvasGroup == null)
            return new Vector2(
                Mathf.Max(40f, step.highlightSize.x),
                Mathf.Max(40f, step.highlightSize.y));

        Canvas.ForceUpdateCanvases();
        Rect targetRect = GetTargetRectInOverlaySpace(target);
        return new Vector2(
            Mathf.Max(40f, targetRect.width + Mathf.Max(0f, step.highlightPadding.x) * 2f),
            Mathf.Max(40f, targetRect.height + Mathf.Max(0f, step.highlightPadding.y) * 2f));
    }

    private Rect ResolveHighlightRectForStep(HudGuideStep step)
    {
        RectTransform target = ResolveTarget(step.targetType);
        Rect targetRect = target != null ? GetTargetRectInOverlaySpace(target) : Rect.zero;
        return ResolveHighlightRectForStep(step, targetRect);
    }

    private Rect ResolveHighlightRectForStep(HudGuideStep step, Rect targetRect)
    {
        if (step != null && step.sceneHighlightAnchor != null)
            return GetTargetRectInOverlaySpace(step.sceneHighlightAnchor);

        Vector2 padding = step != null ? step.highlightPadding : Vector2.zero;
        float width = targetRect.width + Mathf.Max(0f, padding.x) * 2f;
        float height = targetRect.height + Mathf.Max(0f, padding.y) * 2f;
        return new Rect(
            targetRect.center.x - width * 0.5f,
            targetRect.center.y - height * 0.5f,
            width,
            height);
    }

    private RectTransform FindChildRect(RectTransform parent, string childName)
    {
        if (parent == null)
            return null;

        Transform child = parent.Find(childName);
        return child as RectTransform;
    }

    private void MarkAuthoringObjectsDirty()
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        if (overlayCanvasGroup != null)
            EditorUtility.SetDirty(overlayCanvasGroup.gameObject);
        if (gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }
}
