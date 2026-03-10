using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Selectable))]
public class UIKeyboardFocusIndicator : MonoBehaviour, ISelectHandler, IDeselectHandler
{
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
    private float pulseTimer;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        rectTransform = transform as RectTransform;
        if (rectTransform != null)
            baseScale = rectTransform.localScale;

        ResolveFocusGraphicAndOutline();
        ApplyFocusState(false, true);
    }

    private void OnEnable()
    {
        bool shouldFocus = false;
        EventSystem evt = EventSystem.current;
        if (evt != null && evt.currentSelectedGameObject == gameObject)
            shouldFocus = true;

        ApplyFocusState(shouldFocus, true);
    }

    private void OnDisable()
    {
        ApplyFocusState(false, true);
    }

    private void Update()
    {
        if (!useScaleEffect || rectTransform == null)
            return;

        float targetMul = focused ? focusedScaleMultiplier : 1f;
        if (focused && pulseWhenFocused && pulseAmplitude > 0f)
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
        ApplyFocusState(true, false);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        ApplyFocusState(false, false);
    }

    private void ResolveFocusGraphicAndOutline()
    {
        if (focusGraphic == null && selectable != null && selectable.targetGraphic != null)
            focusGraphic = selectable.targetGraphic;

        if (focusGraphic == null)
            focusGraphic = GetComponentInChildren<Graphic>(true);

        if (focusGraphic == null)
            return;

        outline = focusGraphic.gameObject.AddComponent<Outline>();

        outline.useGraphicAlpha = false;
        outline.effectDistance = focusOutlineDistance;
        outline.effectColor = focusOutlineColor;
        outline.enabled = false;
    }

    private void ApplyFocusState(bool value, bool immediateScale)
    {
        bool canFocus = value &&
                        selectable != null &&
                        selectable.IsActive() &&
                        selectable.IsInteractable();
        focused = canFocus;

        if (outline != null)
        {
            outline.effectDistance = focusOutlineDistance;
            outline.effectColor = focusOutlineColor;
            outline.enabled = focused;
        }

        if (immediateScale && rectTransform != null)
        {
            float mul = focused ? focusedScaleMultiplier : 1f;
            rectTransform.localScale = baseScale * mul;
        }

        if (!focused)
            pulseTimer = 0f;
    }
}
