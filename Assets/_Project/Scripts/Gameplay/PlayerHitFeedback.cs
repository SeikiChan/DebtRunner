using System.Collections;
using UnityEngine;

/// <summary>
/// 玩家受击视觉反馈 — 受击闪白 + 无敌时间内闪烁
/// 挂在玩家 GameObject 上，自动查找 SpriteRenderer
/// </summary>
[DisallowMultipleComponent]
public class PlayerHitFeedback : MonoBehaviour
{
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
    private Coroutine flashRoutine;
    private Coroutine blinkRoutine;

    private void Start()
    {
        // PlayerVisualAnim.Awake 会创建子物体并禁用根 SpriteRenderer，
        // 所以在 Start 中查找当前启用的 SpriteRenderer（子物体上的）
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.enabled)
            {
                spriteRenderer = sr;
                break;
            }
        }
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    /// <summary>
    /// 受击时调用：闪白一下
    /// </summary>
    public void PlayHitFlash()
    {
        if (spriteRenderer == null) return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    /// <summary>
    /// 无敌时间开始：开始闪烁
    /// </summary>
    public void StartBlink()
    {
        if (spriteRenderer == null) return;

        StopBlinkInternal();
        blinkRoutine = StartCoroutine(BlinkRoutine());
    }

    /// <summary>
    /// 无敌时间结束：停止闪烁，恢复可见
    /// </summary>
    public void StopBlink()
    {
        StopBlinkInternal();
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSecondsRealtime(flashDuration);
        spriteRenderer.color = originalColor;
        flashRoutine = null;
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
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
}
