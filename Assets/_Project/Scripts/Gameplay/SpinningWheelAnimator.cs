using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 转盘旋转动画器 — 控制转盘的旋转效果
/// 快速开始 → 逐渐减速 → 停在目标角度
/// </summary>
public class SpinningWheelAnimator : MonoBehaviour
{
    [Header("Spin Config / 旋转配置")]
    [LocalizedLabel("Extra Spins / 额外圈数")]
    [SerializeField, Min(1)] private int extraSpins = 3;
    [LocalizedLabel("Spin Duration / 旋转总时长")]
    [SerializeField, Min(0.5f)] private float spinDuration = 2.5f;
    [LocalizedLabel("Ease Power / 缓动强度")]
    [SerializeField, Min(1f)] private float easePower = 3f;

    [Header("SFX (Optional) / 音效")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip tickSfx;
    [SerializeField] private AudioClip stopSfx;
    [LocalizedLabel("Tick Interval Degrees / 滴答间隔角度")]
    [SerializeField, Min(1f)] private float tickIntervalDegrees = 45f;

    private RectTransform wheelTransform;
    private Coroutine spinCoroutine;
    private Action onComplete;

    public bool IsSpinning => spinCoroutine != null;

    public void BindWheel(RectTransform wheel)
    {
        wheelTransform = wheel;
    }

    /// <summary>
    /// 旋转到目标角度（顺时针，0°=正上方）
    /// </summary>
    public void SpinToAngle(float targetAngle, Action callback)
    {
        if (wheelTransform == null)
        {
            callback?.Invoke();
            return;
        }

        CancelCurrentSpin();
        onComplete = callback;
        spinCoroutine = StartCoroutine(SpinRoutine(targetAngle));
    }

    public void CancelAndReset()
    {
        CancelCurrentSpin();
        onComplete = null;
    }

    private IEnumerator SpinRoutine(float targetAngle)
    {
        // 当前 Z 旋转（Unity UI 的 Z 轴旋转是顺时针为负值）
        float startAngle = wheelTransform.localEulerAngles.z;

        // 从当前角度旋转到目标角度的旋转量（顺时针）
        // 指针在顶部(0°)，扇区在屏幕上的位置 = localAngle - wheelZ
        // 要让指针对准扇区中心 α，需要 wheelZ = α
        // finalZ = startAngle - totalRotation = targetAngle
        // 所以 totalRotation = startAngle - targetAngle
        float rotationToTarget = ((startAngle - targetAngle) % 360f + 360f) % 360f;
        if (rotationToTarget < 1f) rotationToTarget += 360f;
        float totalRotation = extraSpins * 360f + rotationToTarget;

        float elapsed = 0f;
        float lastTickAngle = 0f;

        while (elapsed < spinDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / spinDuration);

            // 缓出曲线：开始快，结束慢
            float eased = 1f - Mathf.Pow(1f - t, easePower);

            float currentRotation = eased * totalRotation;
            wheelTransform.localEulerAngles = new Vector3(0f, 0f, startAngle - currentRotation);

            // 播放滴答音效
            if (currentRotation - lastTickAngle >= tickIntervalDegrees)
            {
                PlaySfx(tickSfx);
                lastTickAngle += tickIntervalDegrees;
            }

            yield return null;
        }

        // 确保精确停在目标角度
        wheelTransform.localEulerAngles = new Vector3(0f, 0f, startAngle - totalRotation);
        PlaySfx(stopSfx);

        spinCoroutine = null;
        Action callback = onComplete;
        onComplete = null;
        callback?.Invoke();
    }

    private void CancelCurrentSpin()
    {
        if (spinCoroutine == null) return;
        StopCoroutine(spinCoroutine);
        spinCoroutine = null;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    private void OnDisable()
    {
        CancelAndReset();
    }
}
