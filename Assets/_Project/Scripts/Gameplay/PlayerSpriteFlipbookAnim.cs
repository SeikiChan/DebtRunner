using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSpriteFlipbookAnim : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMotor2D motor;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Frames")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite[] moveFrames;
    [SerializeField] private bool pingPongPlayback = false;
    [SerializeField, Min(0)] private int moveLoopStartIndex = 0;
    [SerializeField, Min(0)] private int moveLoopFrameCount = 0;

    [Header("Playback")]
    [SerializeField, Min(0.001f)] private float moveInputThreshold = 0.05f;
    [SerializeField, Min(1f)] private float framesPerSecond = 12f;
    [SerializeField] private bool scalePlaybackByMoveAmount = true;
    [SerializeField, Min(0.1f)] private float minPlaybackScale = 0.85f;
    [SerializeField, Min(0.1f)] private float maxPlaybackScale = 1.2f;

    private Sprite defaultSprite;
    private float frameTimer;
    private int frameCursor;
    private bool wasMovingLastFrame;

    private void Reset()
    {
        motor = GetComponent<PlayerMotor2D>();
        spriteRenderer = ResolveBestSpriteRenderer();
    }

    private void Awake()
    {
        ResolveRefs();
        CacheDefaultSprite();
        ApplyIdleSprite();
    }

    private void OnValidate()
    {
        if (motor == null)
            motor = GetComponent<PlayerMotor2D>();

        if (spriteRenderer == null || !spriteRenderer.enabled || IsExcludedRenderer(spriteRenderer))
            spriteRenderer = ResolveBestSpriteRenderer();

        CacheDefaultSprite();
    }

    private void Update()
    {
        ResolveRefs();
        if (spriteRenderer == null)
            return;

        Vector2 move = motor != null ? motor.CurrentMoveInput : Vector2.zero;
        bool moving = move.sqrMagnitude > (moveInputThreshold * moveInputThreshold);

        if (!moving || moveFrames == null || moveFrames.Length == 0)
        {
            frameTimer = 0f;
            frameCursor = 0;
            wasMovingLastFrame = false;
            ApplyIdleSprite();
            return;
        }

        ResolveLoopRange(moveFrames.Length, out int loopStartIndex, out int loopFrameCount);
        if (!wasMovingLastFrame)
        {
            frameTimer = 0f;
            frameCursor = 0;
        }

        float playbackScale = 1f;
        if (scalePlaybackByMoveAmount)
        {
            float moveAmount = Mathf.Clamp01(move.magnitude);
            playbackScale = Mathf.Lerp(minPlaybackScale, maxPlaybackScale, moveAmount);
        }

        frameTimer += Time.deltaTime * framesPerSecond * playbackScale;
        while (frameTimer >= 1f)
        {
            frameTimer -= 1f;
            frameCursor++;
        }

        int spriteIndex = loopStartIndex + ResolveFrameIndex(frameCursor, loopFrameCount, pingPongPlayback);
        Sprite nextSprite = moveFrames[spriteIndex];
        if (nextSprite != null)
            spriteRenderer.sprite = nextSprite;

        wasMovingLastFrame = true;
    }

    public void SetMoveFrames(Sprite[] sprites)
    {
        moveFrames = sprites;
        frameTimer = 0f;
        frameCursor = 0;
        wasMovingLastFrame = false;
        ApplyIdleSprite();
    }

    private void ResolveRefs()
    {
        if (motor == null)
            motor = GetComponent<PlayerMotor2D>();

        if (spriteRenderer == null || !spriteRenderer.enabled || IsExcludedRenderer(spriteRenderer))
            spriteRenderer = ResolveBestSpriteRenderer();
    }

    private void CacheDefaultSprite()
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null)
            defaultSprite = spriteRenderer.sprite;
    }

    private void ApplyIdleSprite()
    {
        if (spriteRenderer == null)
            return;

        Sprite target = idleSprite;
        if (target == null && moveFrames != null && moveFrames.Length > 0)
            target = moveFrames[0];
        if (target == null)
            target = defaultSprite;

        if (target != null)
            spriteRenderer.sprite = target;
    }

    private void ResolveLoopRange(int frameCount, out int loopStartIndex, out int loopFrameCount)
    {
        if (frameCount <= 0)
        {
            loopStartIndex = 0;
            loopFrameCount = 0;
            return;
        }

        loopStartIndex = Mathf.Clamp(moveLoopStartIndex, 0, frameCount - 1);
        int maxAvailableCount = frameCount - loopStartIndex;
        loopFrameCount = moveLoopFrameCount > 0
            ? Mathf.Clamp(moveLoopFrameCount, 1, maxAvailableCount)
            : maxAvailableCount;
    }

    private SpriteRenderer ResolveBestSpriteRenderer()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer fallback = null;
        int bestScore = int.MinValue;
        int bestSortingLayer = int.MinValue;
        int bestSortingOrder = int.MinValue;
        float bestArea = float.NegativeInfinity;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer candidate = renderers[i];
            if (candidate == null)
                continue;
            if (IsExcludedRenderer(candidate))
                continue;

            if (fallback == null)
                fallback = candidate;

            if (!candidate.enabled || !candidate.gameObject.activeInHierarchy)
                continue;

            int score = 0;
            string nameLower = candidate.gameObject.name.ToLowerInvariant();
            if (nameLower.Contains("playervisual"))
                score += 1000;
            if (candidate.transform == transform)
                score += 100;
            if (candidate.sprite != null)
                score += 10;

            int sortingLayer = candidate.sortingLayerID;
            int sortingOrder = candidate.sortingOrder;
            Bounds bounds = candidate.bounds;
            float area = bounds.size.x * bounds.size.y;

            bool better = false;
            if (score > bestScore)
                better = true;
            else if (score == bestScore && sortingLayer > bestSortingLayer)
                better = true;
            else if (score == bestScore && sortingLayer == bestSortingLayer && sortingOrder > bestSortingOrder)
                better = true;
            else if (score == bestScore && sortingLayer == bestSortingLayer && sortingOrder == bestSortingOrder && area > bestArea)
                better = true;

            if (!better)
                continue;

            fallback = candidate;
            bestScore = score;
            bestSortingLayer = sortingLayer;
            bestSortingOrder = sortingOrder;
            bestArea = area;
        }

        return fallback;
    }

    private static bool IsExcludedRenderer(SpriteRenderer candidate)
    {
        if (candidate == null)
            return false;

        string nameLower = candidate.gameObject.name.ToLowerInvariant();
        if (nameLower.Contains("shadow"))
            return true;
        if (nameLower.Contains("shieldaura") || nameLower.Contains("shieldoutline"))
            return true;

        Transform parent = candidate.transform.parent;
        while (parent != null)
        {
            string parentLower = parent.name.ToLowerInvariant();
            if (parentLower.Contains("shadow"))
                return true;
            if (parentLower.Contains("shieldaura") || parentLower.Contains("shieldoutline"))
                return true;
            parent = parent.parent;
        }

        return false;
    }

    private static int ResolveFrameIndex(int cursor, int frameCount, bool pingPong)
    {
        if (frameCount <= 1)
            return 0;

        if (!pingPong)
            return Mathf.Abs(cursor) % frameCount;

        int cycleLength = (frameCount * 2) - 2;
        if (cycleLength <= 0)
            return 0;

        int cycleIndex = Mathf.Abs(cursor) % cycleLength;
        return cycleIndex < frameCount
            ? cycleIndex
            : cycleLength - cycleIndex;
    }
}
