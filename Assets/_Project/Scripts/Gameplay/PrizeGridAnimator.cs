using System;
using System.Collections;
using UnityEngine;

public class PrizeGridAnimator : MonoBehaviour
{
    [Header("Spin Timing")]
    [SerializeField, Min(0.01f)] private float minDelay = 0.04f;
    [SerializeField, Min(0.01f)] private float maxDelay = 0.18f;
    [SerializeField, Min(0)] private int extraLoops = 3;
    [SerializeField, Range(0.1f, 0.95f)] private float slowdownStartRatio = 0.65f;
    [SerializeField, Min(1f)] private float highlightScale = 1.14f;

    [Header("SFX (Optional)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip tickSfx;
    [SerializeField] private AudioClip stopSfx;

    private PrizeGridCellView[] cells;
    private Coroutine spinCoroutine;
    private Action onComplete;
    private int currentIndex = -1;

    public bool IsSpinning => spinCoroutine != null;
    public int CurrentIndex => currentIndex;

    public void BindCells(PrizeGridCellView[] cellViews)
    {
        cells = cellViews;
        if (cells == null)
            return;

        CacheBaseScale();
        ResetHighlights();
    }

    public void PlayToTarget(int targetIndex, Action callback)
    {
        if (cells == null || cells.Length == 0)
        {
            callback?.Invoke();
            return;
        }

        targetIndex = Mathf.Clamp(targetIndex, 0, cells.Length - 1);
        CancelCurrentSpin();
        onComplete = callback;
        spinCoroutine = StartCoroutine(SpinRoutine(targetIndex));
    }

    public void CancelAndReset()
    {
        CancelCurrentSpin();
        onComplete = null;
        currentIndex = -1;
        ResetHighlights();
    }

    private IEnumerator SpinRoutine(int targetIndex)
    {
        int count = cells.Length;
        int startIndex = currentIndex < 0 ? 0 : currentIndex;
        int delta = PositiveMod(targetIndex - startIndex, count);
        if (delta == 0)
            delta = count;

        int totalSteps = Mathf.Max(0, extraLoops) * count + delta;
        totalSteps = Mathf.Max(1, totalSteps);

        for (int step = 1; step <= totalSteps; step++)
        {
            currentIndex = (startIndex + step) % count;
            ApplyHighlight(currentIndex);
            PlaySfx(tickSfx);

            float progress = step / (float)totalSteps;
            float slowT = progress <= slowdownStartRatio
                ? 0f
                : Mathf.InverseLerp(slowdownStartRatio, 1f, progress);
            float delay = Mathf.Lerp(minDelay, maxDelay, slowT);

            yield return new WaitForSecondsRealtime(delay);
        }

        currentIndex = targetIndex;
        ApplyHighlight(currentIndex);
        PlaySfx(stopSfx);

        spinCoroutine = null;
        Action callback = onComplete;
        onComplete = null;
        callback?.Invoke();
    }

    private void CacheBaseScale()
    {
        if (cells == null) return;

        for (int i = 0; i < cells.Length; i++)
        {
            PrizeGridCellView cell = cells[i];
            if (cell == null)
                continue;

            RectTransform t = GetCellTransform(cell);
            cell.baseScale = t != null ? t.localScale : Vector3.one;
        }
    }

    private void ResetHighlights()
    {
        if (cells == null) return;

        for (int i = 0; i < cells.Length; i++)
        {
            PrizeGridCellView cell = cells[i];
            if (cell == null) continue;

            RectTransform t = GetCellTransform(cell);
            if (t != null)
            {
                Vector3 baseScale = cell.baseScale == Vector3.zero ? Vector3.one : cell.baseScale;
                t.localScale = baseScale;
            }

            if (cell.highlightOverlay != null)
                cell.highlightOverlay.enabled = false;
        }
    }

    private void ApplyHighlight(int activeIndex)
    {
        if (cells == null) return;

        for (int i = 0; i < cells.Length; i++)
        {
            PrizeGridCellView cell = cells[i];
            if (cell == null) continue;

            bool isActive = i == activeIndex;
            RectTransform t = GetCellTransform(cell);
            if (t != null)
            {
                Vector3 baseScale = cell.baseScale == Vector3.zero ? Vector3.one : cell.baseScale;
                t.localScale = isActive ? baseScale * highlightScale : baseScale;
            }

            if (cell.highlightOverlay != null)
                cell.highlightOverlay.enabled = isActive;
        }
    }

    private RectTransform GetCellTransform(PrizeGridCellView cell)
    {
        if (cell == null) return null;
        if (cell.root != null) return cell.root;
        if (cell.icon != null) return cell.icon.rectTransform;
        if (cell.highlightOverlay != null) return cell.highlightOverlay.rectTransform;
        if (cell.label != null) return cell.label.rectTransform;
        return null;
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

    private int PositiveMod(int value, int mod)
    {
        if (mod <= 0) return 0;
        int m = value % mod;
        return m < 0 ? m + mod : m;
    }

    private void OnDisable()
    {
        CancelAndReset();
    }
}
