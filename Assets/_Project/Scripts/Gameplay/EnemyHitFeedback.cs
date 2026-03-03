using System.Collections;
using UnityEngine;

/// <summary>
/// 敌人受击视觉反馈 — 闪白 + 缩放弹跳 + 死亡缩小消失
/// 挂在敌人 GameObject 上，自动查找 SpriteRenderer（兼容 EnemyVisualWobble 子物体）
/// </summary>
[DisallowMultipleComponent]
public class EnemyHitFeedback : MonoBehaviour
{
    [Header("Flash / 闪白")]
    [LocalizedLabel("闪白颜色")]
    [SerializeField] private Color flashColor = Color.white;
    [LocalizedLabel("闪白持续时间")]
    [SerializeField, Min(0.01f)] private float flashDuration = 0.06f;

    [Header("Scale Punch / 缩放弹跳")]
    [LocalizedLabel("受击放大倍率")]
    [SerializeField, Min(1f)] private float punchScale = 1.25f;
    [LocalizedLabel("弹跳恢复速度")]
    [SerializeField, Min(1f)] private float punchRecoverSpeed = 12f;

    [Header("Death / 死亡动画")]
    [LocalizedLabel("死亡缩小时长")]
    [SerializeField, Min(0.01f)] private float deathShrinkDuration = 0.2f;
    [LocalizedLabel("死亡旋转角度")]
    [SerializeField] private float deathSpinAngle = 45f;

    private SpriteRenderer spriteRenderer;
    private Transform visualTarget;
    private Color originalColor;
    private Coroutine flashRoutine;
    private float currentPunch = 1f;
    private Vector3 baseScale;
    private bool isDying;

    private void Start()
    {
        // 兼容 EnemyVisualWobble：它在 Awake 创建子物体，所以 Start 中查找
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.enabled)
            {
                spriteRenderer = sr;
                visualTarget = sr.transform;
                break;
            }
        }
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            baseScale = visualTarget.localScale;
        }
    }

    private void LateUpdate()
    {
        if (isDying || visualTarget == null) return;

        // 缩放弹跳恢复
        if (currentPunch > 1.001f)
        {
            currentPunch = Mathf.Lerp(currentPunch, 1f, 1f - Mathf.Exp(-punchRecoverSpeed * Time.deltaTime));
            // 叠加在 EnemyVisualWobble 的 scale 之上
            // EnemyVisualWobble 在 LateUpdate 先执行（默认 order），这里用乘法叠加
            Vector3 s = visualTarget.localScale;
            visualTarget.localScale = s * currentPunch;
        }
    }

    /// <summary>受击时调用：闪白 + 缩放弹跳</summary>
    public void PlayHit()
    {
        if (spriteRenderer == null || isDying) return;

        // 闪白
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());

        // 缩放弹跳
        currentPunch = punchScale;
    }

    /// <summary>死亡时调用：缩小+旋转+消失，返回动画时长</summary>
    public float PlayDeath()
    {
        if (isDying) return 0f;
        isDying = true;

        // 停止闪白
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        // 禁用 EnemyVisualWobble，防止它覆盖死亡动画的缩放
        var wobble = GetComponent<EnemyVisualWobble>();
        if (wobble != null)
            wobble.enabled = false;

        StartCoroutine(DeathRoutine());
        return deathShrinkDuration;
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        flashRoutine = null;
    }

    private IEnumerator DeathRoutine()
    {
        if (visualTarget == null) yield break;

        Vector3 startScale = visualTarget.localScale;
        Quaternion startRot = visualTarget.localRotation;
        float targetAngle = Random.value > 0.5f ? deathSpinAngle : -deathSpinAngle;
        float elapsed = 0f;

        while (elapsed < deathShrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / deathShrinkDuration);
            // 先快后慢的缩小曲线
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
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        isDying = false;
    }
}
