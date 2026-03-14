using System.Collections;
using UnityEngine;

/// <summary>
/// Player hit feedback: flash color on hit, blink during invulnerability,
/// and play a short death animation before the loss transition.
/// </summary>
[DisallowMultipleComponent]
public class PlayerHitFeedback : MonoBehaviour
{
    [Header("Target Renderer")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("Flash")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField, Min(0.01f)] private float flashDuration = 0.08f;

    [Header("Blink")]
    [SerializeField, Min(0.01f)] private float blinkInterval = 0.1f;

    [Header("Death")]
    [SerializeField, Min(0.1f)] private float deathDuration = 1.1f;
    [SerializeField, Range(0f, 1f)] private float deathEndScaleMultiplier = 0.08f;
    [SerializeField, Min(0f)] private float deathDropDistance = 0.22f;
    [SerializeField] private float deathSpinAngle = 95f;
    [SerializeField, Range(0f, 1f)] private float deathEndAlpha = 0.1f;

    private SpriteRenderer spriteRenderer;
    private Transform visualTarget;
    private PlayerVisualAnim visualAnim;
    private Color originalColor;
    private bool hasOriginalColor;
    private Vector3 defaultLocalPosition;
    private Vector3 defaultLocalScale;
    private Quaternion defaultLocalRotation;
    private bool hasDefaultPose;
    private Coroutine flashRoutine;
    private Coroutine blinkRoutine;
    private Coroutine deathRoutine;
    private bool isDying;
    private bool restoreVisualAnimEnabled;

    private void Awake()
    {
        ResolveTargetRenderer();
        ResolveVisualAnim();
    }

    private void Start()
    {
        ResolveTargetRenderer();
        ResolveVisualAnim();
    }

    public void PlayHitFlash()
    {
        ResolveTargetRenderer();
        if (spriteRenderer == null || isDying)
            return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
            spriteRenderer.color = originalColor;
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    public void StartBlink()
    {
        ResolveTargetRenderer();
        if (spriteRenderer == null || isDying)
            return;

        StopBlinkInternal();
        blinkRoutine = StartCoroutine(BlinkRoutine());
    }

    public void StopBlink()
    {
        StopBlinkInternal();
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
    }

    public float PlayDeath()
    {
        ResolveTargetRenderer();
        ResolveVisualAnim();
        if (spriteRenderer == null || visualTarget == null)
            return 0f;

        if (isDying)
            return deathDuration;

        isDying = true;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        StopBlinkInternal();

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        spriteRenderer.color = originalColor;
        spriteRenderer.enabled = true;

        if (visualAnim != null)
        {
            restoreVisualAnimEnabled = visualAnim.enabled;
            visualAnim.enabled = false;
            visualAnim.ResetVisualPose();
        }

        deathRoutine = StartCoroutine(DeathRoutine());
        return deathDuration;
    }

    public void ResetVisualState()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        StopBlinkInternal();

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        ResolveTargetRenderer();
        ResolveVisualAnim();

        if (visualAnim != null)
        {
            bool shouldEnableVisualAnim = isDying ? restoreVisualAnimEnabled : visualAnim.enabled;
            visualAnim.enabled = false;
            visualAnim.ResetVisualPose();
            visualAnim.enabled = shouldEnableVisualAnim;
        }
        else if (visualTarget != null && hasDefaultPose)
        {
            visualTarget.localPosition = defaultLocalPosition;
            visualTarget.localRotation = defaultLocalRotation;
            visualTarget.localScale = defaultLocalScale;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            spriteRenderer.enabled = true;
        }

        isDying = false;
        restoreVisualAnimEnabled = false;
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null)
        {
            flashRoutine = null;
            yield break;
        }

        spriteRenderer.color = flashColor;
        yield return new WaitForSecondsRealtime(flashDuration);
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        flashRoutine = null;
    }

    private IEnumerator BlinkRoutine()
    {
        while (spriteRenderer != null)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSecondsRealtime(blinkInterval);
        }
    }

    private IEnumerator DeathRoutine()
    {
        Vector3 startPosition = visualTarget.localPosition;
        Vector3 startScale = visualTarget.localScale;
        Quaternion startRotation = visualTarget.localRotation;
        Vector3 endPosition = startPosition + new Vector3(0f, -deathDropDistance, 0f);
        Vector3 endScale = new Vector3(
            startScale.x * Mathf.Max(0f, deathEndScaleMultiplier),
            startScale.y * Mathf.Max(0f, deathEndScaleMultiplier * 0.72f),
            startScale.z);
        Quaternion endRotation = startRotation * Quaternion.Euler(0f, 0f, Random.value > 0.5f ? deathSpinAngle : -deathSpinAngle);
        Color startColor = spriteRenderer.color;
        Color endColor = startColor;
        endColor.a = Mathf.Clamp01(deathEndAlpha);
        float duration = Mathf.Max(0.1f, deathDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            if (visualTarget != null)
            {
                visualTarget.localPosition = Vector3.Lerp(startPosition, endPosition, ease);
                visualTarget.localRotation = Quaternion.Lerp(startRotation, endRotation, ease);
                visualTarget.localScale = Vector3.Lerp(startScale, endScale, ease);
            }

            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(startColor, endColor, ease);

            yield return null;
        }

        if (visualTarget != null)
        {
            visualTarget.localPosition = endPosition;
            visualTarget.localRotation = endRotation;
            visualTarget.localScale = endScale;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = endColor;

        deathRoutine = null;
    }

    private void StopBlinkInternal()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
    }

    private void OnDisable()
    {
        ResetVisualState();
    }

    private void ResolveTargetRenderer()
    {
        if (targetRenderer != null)
        {
            if (spriteRenderer != targetRenderer)
            {
                spriteRenderer = targetRenderer;
                visualTarget = targetRenderer.transform;
                originalColor = spriteRenderer.color;
                hasOriginalColor = true;
                CacheDefaultPose();
            }
            else if (!hasOriginalColor)
            {
                originalColor = spriteRenderer.color;
                hasOriginalColor = true;
            }

            return;
        }

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0)
            return;

        SpriteRenderer best = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null)
                continue;

            int score = 0;
            string nameLower = sr.gameObject.name.ToLowerInvariant();
            bool isShadow = nameLower.Contains("shadow");

            if (isShadow)
                score -= 2000;
            else
                score += 600;

            if (sr.enabled && sr.gameObject.activeInHierarchy)
                score += 1000;

            if (sr.transform == transform)
                score -= 150;

            score += sr.sortingOrder * 10;

            if (score > bestScore)
            {
                bestScore = score;
                best = sr;
            }
        }

        if (best == null)
            return;

        if (spriteRenderer != best)
        {
            spriteRenderer = best;
            visualTarget = best.transform;
            originalColor = best.color;
            hasOriginalColor = true;
            CacheDefaultPose();
        }
        else if (!hasOriginalColor)
        {
            originalColor = best.color;
            hasOriginalColor = true;
        }
    }

    private void ResolveVisualAnim()
    {
        if (visualAnim == null)
            visualAnim = GetComponent<PlayerVisualAnim>();
    }

    private void CacheDefaultPose()
    {
        if (visualTarget == null)
            return;

        defaultLocalPosition = visualTarget.localPosition;
        defaultLocalScale = visualTarget.localScale;
        defaultLocalRotation = visualTarget.localRotation;
        hasDefaultPose = true;
    }
}
