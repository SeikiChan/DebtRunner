using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Studio splash screen — white background + studio logo.
/// Background stays fully opaque from frame 1; only logo fades in.
/// onComplete fires BEFORE fade-out so title can appear underneath.
/// </summary>
public class StudioSplashScreen : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image studioLogoImage;

    [Header("Config")]
    [SerializeField] private float fadeInDuration = 0.6f;
    [SerializeField] private float holdDuration = 1.8f;
    [SerializeField] private float fadeOutDuration = 0.5f;
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

        // Background fully visible from frame 1 — no scene bleed-through.
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        // Hide logo initially for fade-in.
        if (studioLogoImage != null)
            studioLogoImage.color = new Color(studioLogoImage.color.r, studioLogoImage.color.g, studioLogoImage.color.b, 0f);

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
        // Fade in logo only (background stays white).
        float elapsed = 0f;
        while (elapsed < fadeInDuration && !skipping)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fadeInDuration));
            SetLogoAlpha(t);
            yield return null;
        }
        SetLogoAlpha(1f);

        // Hold.
        elapsed = 0f;
        while (elapsed < holdDuration && !skipping)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Fire onComplete BEFORE fade-out so title shows underneath.
        finished = true;
        onComplete?.Invoke();

        // Fade out entire panel (reveals title beneath).
        elapsed = 0f;
        float fadeDur = skipping ? 0.15f : fadeOutDuration;
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
