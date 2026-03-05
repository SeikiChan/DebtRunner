using System.Collections;
using UnityEngine;

/// <summary>
/// Player hit feedback: flash color on hit and blink during invulnerability.
/// Robustly resolves the visible player renderer (avoids picking FootShadow).
/// </summary>
[DisallowMultipleComponent]
public class PlayerHitFeedback : MonoBehaviour
{
    [Header("Target Renderer / 目标渲染器")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("Flash / 闪白")]
    [LocalizedLabel("闪白颜色")]
    [SerializeField] private Color flashColor = Color.white;
    [LocalizedLabel("闪白持续时间")]
    [SerializeField, Min(0.01f)] private float flashDuration = 0.08f;

    [Header("Blink / 闪烁")]
    [LocalizedLabel("闪烁间隔")]
    [SerializeField, Min(0.01f)] private float blinkInterval = 0.1f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool hasOriginalColor;
    private Coroutine flashRoutine;
    private Coroutine blinkRoutine;

    private void Awake()
    {
        ResolveTargetRenderer();
    }

    private void Start()
    {
        ResolveTargetRenderer();
    }

    /// <summary>
    /// Call on hit: quick color flash.
    /// </summary>
    public void PlayHitFlash()
    {
        ResolveTargetRenderer();
        if (spriteRenderer == null)
            return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
            // Ensure interrupted flash does not leave renderer tinted.
            spriteRenderer.color = originalColor;
        }
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    /// <summary>
    /// Start blinking for invulnerability window.
    /// </summary>
    public void StartBlink()
    {
        ResolveTargetRenderer();
        if (spriteRenderer == null)
            return;

        StopBlinkInternal();
        blinkRoutine = StartCoroutine(BlinkRoutine());
    }

    /// <summary>
    /// Stop blinking and restore visibility.
    /// </summary>
    public void StopBlink()
    {
        StopBlinkInternal();
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
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
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        StopBlinkInternal();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            spriteRenderer.enabled = true;
        }
    }

    private void ResolveTargetRenderer()
    {
        if (targetRenderer != null)
        {
            if (spriteRenderer != targetRenderer)
            {
                spriteRenderer = targetRenderer;
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
            if (sr == null)
                continue;

            int score = 0;
            string n = sr.gameObject.name;
            bool isShadow = !string.IsNullOrEmpty(n) && n.ToLowerInvariant().Contains("shadow");

            if (isShadow)
                score -= 2000;
            else
                score += 600;

            if (sr.enabled && sr.gameObject.activeInHierarchy)
                score += 1000;

            // Prefer child visual renderer over disabled root renderer.
            if (sr.transform == transform)
                score -= 150;

            score += sr.sortingOrder * 10;

            if (score > bestScore)
            {
                bestScore = score;
                best = sr;
            }
        }

        if (best != null)
        {
            if (spriteRenderer != best)
            {
                spriteRenderer = best;
                originalColor = best.color;
                hasOriginalColor = true;
            }
            else if (!hasOriginalColor)
            {
                originalColor = best.color;
                hasOriginalColor = true;
            }
        }
    }
}
