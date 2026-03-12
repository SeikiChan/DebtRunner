using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHitFeedback : MonoBehaviour
{
    [Header("Target Renderer")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("Flash")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField, Min(0.01f)] private float flashDuration = 0.08f;

    [Header("Scale Punch")]
    [SerializeField, Min(1f)] private float punchScale = 1.32f;
    [SerializeField, Min(1f)] private float punchRecoverSpeed = 12f;

    [Header("Hit Burst")]
    [SerializeField] private Color burstColor = new Color(1f, 0.62f, 0.24f, 0.85f);
    [SerializeField, Min(0.01f)] private float burstDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float burstStartScale = 0.35f;
    [SerializeField, Min(0.01f)] private float burstEndScale = 0.95f;

    [Header("Death")]
    [SerializeField, Min(0.01f)] private float deathShrinkDuration = 0.2f;
    [SerializeField] private float deathSpinAngle = 45f;

    private SpriteRenderer spriteRenderer;
    private Transform visualTarget;
    private Color originalColor;
    private bool hasOriginalColor;
    private Coroutine flashRoutine;
    private Coroutine burstRoutine;
    private float currentPunch = 1f;
    private float lastAppliedPunch = 1f;
    private bool isDying;
    private SpriteRenderer burstRenderer;

    private void Awake()
    {
        SanitizeValues();
        ResolveTargetRenderer();
        EnsureBurstRenderer();
    }

    private void OnEnable()
    {
        SanitizeValues();
        ResolveTargetRenderer();
        EnsureBurstRenderer();
    }

    private void Start()
    {
        SanitizeValues();
        ResolveTargetRenderer();
        EnsureBurstRenderer();
    }

    private void OnValidate()
    {
        SanitizeValues();
    }

    private void LateUpdate()
    {
        if (isDying || visualTarget == null)
            return;

        if (currentPunch > 1.001f)
        {
            if (lastAppliedPunch > 1.001f)
                visualTarget.localScale /= lastAppliedPunch;

            currentPunch = Mathf.Lerp(currentPunch, 1f, 1f - Mathf.Exp(-punchRecoverSpeed * Time.deltaTime));
            visualTarget.localScale *= currentPunch;
            lastAppliedPunch = currentPunch;
        }
        else if (lastAppliedPunch > 1.001f)
        {
            visualTarget.localScale /= lastAppliedPunch;
            lastAppliedPunch = 1f;
        }
    }

    public void PlayHit()
    {
        if (isDying)
            return;

        ResolveTargetRenderer();
        EnsureBurstRenderer();
        if (spriteRenderer == null)
            return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
            spriteRenderer.color = originalColor;
        }

        flashRoutine = StartCoroutine(FlashRoutine());
        currentPunch = punchScale;
        PlayBurst();
    }

    public float PlayDeath()
    {
        if (isDying)
            return 0f;

        isDying = true;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        if (burstRoutine != null)
        {
            StopCoroutine(burstRoutine);
            burstRoutine = null;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        if (burstRenderer != null)
            burstRenderer.enabled = false;

        EnemyVisualWobble wobble = GetComponent<EnemyVisualWobble>();
        if (wobble != null)
            wobble.enabled = false;

        StartCoroutine(DeathRoutine());
        return deathShrinkDuration;
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

    private void PlayBurst()
    {
        if (burstRenderer == null)
            return;

        if (burstRoutine != null)
            StopCoroutine(burstRoutine);

        burstRoutine = StartCoroutine(BurstRoutine());
    }

    private IEnumerator BurstRoutine()
    {
        burstRenderer.enabled = true;
        burstRenderer.color = burstColor;
        burstRenderer.transform.localScale = Vector3.one * Mathf.Max(0.01f, burstStartScale);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, burstDuration);
        Vector3 start = Vector3.one * Mathf.Max(0.01f, burstStartScale);
        Vector3 end = Vector3.one * Mathf.Max(burstStartScale, burstEndScale);
        Color color = burstColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            burstRenderer.transform.localScale = Vector3.Lerp(start, end, t);
            color.a = Mathf.Lerp(burstColor.a, 0f, t);
            burstRenderer.color = color;
            yield return null;
        }

        burstRenderer.enabled = false;
        burstRoutine = null;
    }

    private IEnumerator DeathRoutine()
    {
        if (visualTarget == null)
            yield break;

        Vector3 startScale = visualTarget.localScale;
        Quaternion startRot = visualTarget.localRotation;
        float targetAngle = Random.value > 0.5f ? deathSpinAngle : -deathSpinAngle;
        float elapsed = 0f;

        while (elapsed < deathShrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / deathShrinkDuration);
            float ease = 1f - (1f - t) * (1f - t);

            if (visualTarget != null)
            {
                visualTarget.localScale = Vector3.Lerp(startScale, Vector3.zero, ease);
                visualTarget.localRotation = startRot * Quaternion.Euler(0f, 0f, targetAngle * ease);
            }

            yield return null;
        }

        if (visualTarget != null)
            visualTarget.localScale = Vector3.zero;
    }

    private void OnDisable()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        if (burstRoutine != null)
        {
            StopCoroutine(burstRoutine);
            burstRoutine = null;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        if (burstRenderer != null)
            burstRenderer.enabled = false;
        if (visualTarget != null && lastAppliedPunch > 1.001f)
            visualTarget.localScale /= lastAppliedPunch;

        isDying = false;
        currentPunch = 1f;
        lastAppliedPunch = 1f;
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
            if (sr == null || sr == burstRenderer)
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
        }
        else if (!hasOriginalColor)
        {
            originalColor = best.color;
            hasOriginalColor = true;
        }
    }

    private void EnsureBurstRenderer()
    {
        if (burstRenderer != null || spriteRenderer == null || visualTarget == null)
            return;

        GameObject burstObject = new GameObject("HitBurst", typeof(SpriteRenderer));
        burstObject.transform.SetParent(visualTarget, false);
        burstRenderer = burstObject.GetComponent<SpriteRenderer>();
        burstRenderer.sprite = RuntimeSpriteFactory.GetHitPulseSprite();
        burstRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        burstRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        burstRenderer.color = burstColor;
        burstRenderer.enabled = false;
    }

    private void SanitizeValues()
    {
        flashDuration = Mathf.Max(0.01f, flashDuration);
        flashColor.a = Mathf.Clamp(flashColor.a, 0f, 1f);
        punchScale = Mathf.Max(1f, punchScale);
        punchRecoverSpeed = Mathf.Max(1f, punchRecoverSpeed);
        burstDuration = Mathf.Max(0.01f, burstDuration);
        burstStartScale = Mathf.Max(0.01f, burstStartScale);
        burstEndScale = Mathf.Max(0.01f, burstEndScale);
        deathShrinkDuration = Mathf.Max(0.01f, deathShrinkDuration);
    }
}
