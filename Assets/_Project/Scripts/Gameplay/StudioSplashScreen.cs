using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Studio splash screen — white background, "Presented by" text fades in first,
/// then logo fades in. Smooth sequenced animation.
/// </summary>
public class StudioSplashScreen : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text presentedByText;
    [SerializeField] private Image studioLogoImage;

    [Header("Timing")]
    [SerializeField] private float textFadeInDuration = 0.5f;
    [SerializeField] private float textToLogoPause = 0.3f;
    [SerializeField] private float logoFadeInDuration = 0.6f;
    [SerializeField] private float holdDuration = 1.6f;
    [SerializeField] private float fadeOutDuration = 0.6f;
    [SerializeField] private bool skipOnClick = true;

    private bool finished;
    private bool skipping;

    public bool IsFinished => finished;

    public void Play(System.Action onComplete = null)
    {
        finished = false;
        skipping = false;
        gameObject.SetActive(true);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Background fully visible from frame 1.
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        // Hide text and logo initially.
        SetTextAlpha(0f);
        SetLogoAlpha(0f);

        StartCoroutine(SplashRoutine(onComplete));
    }

    public void Skip()
    {
        skipping = true;
    }

    private void Update()
    {
        if (!finished && skipOnClick)
        {
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
                skipping = true;
        }
    }

    private IEnumerator SplashRoutine(System.Action onComplete)
    {
        float elapsed;

        // 1) Fade in "Presented by" text.
        elapsed = 0f;
        while (elapsed < textFadeInDuration && !skipping)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, textFadeInDuration));
            SetTextAlpha(t);
            yield return null;
        }
        SetTextAlpha(1f);

        // 2) Brief pause between text and logo.
        elapsed = 0f;
        while (elapsed < textToLogoPause && !skipping)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 3) Fade in logo.
        elapsed = 0f;
        while (elapsed < logoFadeInDuration && !skipping)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, logoFadeInDuration));
            SetLogoAlpha(t);
            yield return null;
        }
        SetLogoAlpha(1f);

        // If skipping, snap everything visible.
        if (skipping)
        {
            SetTextAlpha(1f);
            SetLogoAlpha(1f);
        }

        // 4) Hold.
        elapsed = 0f;
        while (elapsed < holdDuration && !skipping)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Fire onComplete BEFORE fade-out so title appears underneath.
        finished = true;
        onComplete?.Invoke();

        // 5) Fade out entire panel.
        elapsed = 0f;
        float fadeDur = skipping ? 0.2f : fadeOutDuration;
        while (elapsed < fadeDur)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fadeDur));
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        gameObject.SetActive(false);
    }

    private void SetTextAlpha(float a)
    {
        if (presentedByText != null)
        {
            Color c = presentedByText.color;
            c.a = a;
            presentedByText.color = c;
        }
    }

    private void SetLogoAlpha(float a)
    {
        if (studioLogoImage != null)
        {
            Color c = studioLogoImage.color;
            c.a = a;
            studioLogoImage.color = c;
        }
    }
}
