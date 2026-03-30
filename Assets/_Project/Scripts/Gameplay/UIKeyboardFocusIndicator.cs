using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Selectable))]
public class UIKeyboardFocusIndicator : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    private const string TransparentOutlineButtonName = "Btn_Mute";
    [SerializeField] private Graphic focusGraphic;
    [SerializeField] private bool useScaleEffect = true;
    [SerializeField, Min(1f)] private float focusedScaleMultiplier = 1.05f;
    [SerializeField, Min(1f)] private float scaleLerpSpeed = 16f;
    [SerializeField] private bool pulseWhenFocused = true;
    [SerializeField, Min(0f)] private float pulseAmplitude = 0.025f;
    [SerializeField, Min(0f)] private float pulseFrequency = 8f;
    [SerializeField] private Color focusOutlineColor = new Color(1f, 0.95f, 0.45f, 0.95f);
    [SerializeField] private Vector2 focusOutlineDistance = new Vector2(3f, -3f);

    private Selectable selectable;
    private RectTransform rectTransform;
    private Outline outline;
    private Vector3 baseScale = Vector3.one;
    private bool focused;
    private bool hovered;
    private float pulseTimer;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        rectTransform = transform as RectTransform;
        if (rectTransform != null)
            baseScale = rectTransform.localScale;

        if (string.Equals(gameObject.name, TransparentOutlineButtonName, System.StringComparison.Ordinal))
            focusOutlineColor = new Color(focusOutlineColor.r, focusOutlineColor.g, focusOutlineColor.b, 0f);

        ResolveFocusGraphicAndOutline();
        focused = false;
        hovered = false;
        ApplyVisualState(immediateScale: true);
    }

    private void OnEnable()
    {
        bool shouldFocus = false;
        EventSystem evt = EventSystem.current;
        if (evt != null && evt.currentSelectedGameObject == gameObject)
            shouldFocus = true;

        focused = shouldFocus;
        ApplyVisualState(immediateScale: true);
    }

    private void OnDisable()
    {
        focused = false;
        hovered = false;
        ApplyVisualState(immediateScale: true);
    }

    private void Update()
    {
        if (!useScaleEffect || rectTransform == null)
            return;

        bool highlighted = IsHighlighted();
        float targetMul = highlighted ? focusedScaleMultiplier : 1f;
        if (highlighted && pulseWhenFocused && pulseAmplitude > 0f)
        {
            pulseTimer += Time.unscaledDeltaTime * pulseFrequency;
            targetMul += Mathf.Sin(pulseTimer) * pulseAmplitude;
        }
        else
        {
            pulseTimer = 0f;
        }

        Vector3 targetScale = baseScale * targetMul;
        float t = 1f - Mathf.Exp(-scaleLerpSpeed * Time.unscaledDeltaTime);
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, t);
    }

    public void OnSelect(BaseEventData eventData)
    {
        focused = true;
        ApplyVisualState(immediateScale: false);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        focused = false;
        ApplyVisualState(immediateScale: false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (GameFlowController.Instance != null && !GameFlowController.Instance.IsMousePointerInteractionAllowed)
            return;

        hovered = true;
        ApplyVisualState(immediateScale: false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        ApplyVisualState(immediateScale: false);
    }

    private void ResolveFocusGraphicAndOutline()
    {
        if (focusGraphic == null && selectable != null && selectable.targetGraphic != null)
            focusGraphic = selectable.targetGraphic;

        if (focusGraphic == null)
            focusGraphic = GetComponentInChildren<Graphic>(true);

        if (focusGraphic == null)
            return;

        outline = focusGraphic.GetComponent<Outline>();
        if (outline == null)
            outline = focusGraphic.gameObject.AddComponent<Outline>();

        outline.useGraphicAlpha = false;
        outline.effectDistance = focusOutlineDistance;
        outline.effectColor = focusOutlineColor;
        outline.enabled = false;
    }

    private bool IsHighlighted()
    {
        return selectable != null &&
               selectable.IsActive() &&
               selectable.IsInteractable() &&
               (focused || hovered);
    }

    private void ApplyVisualState(bool immediateScale)
    {
        bool highlighted = IsHighlighted();

        if (outline != null)
        {
            outline.effectDistance = focusOutlineDistance;
            outline.effectColor = focusOutlineColor;
            outline.enabled = highlighted;
        }

        if (immediateScale && rectTransform != null)
        {
            float mul = highlighted ? focusedScaleMultiplier : 1f;
            rectTransform.localScale = baseScale * mul;
        }

        if (!highlighted)
            pulseTimer = 0f;
    }
}
