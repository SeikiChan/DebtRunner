using UnityEngine;

[DisallowMultipleComponent]
public class PlayerVisualAnim : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMotor2D motor;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform visualTarget;
    [SerializeField] private bool autoCreateVisualChildIfNeeded = true;

    [Header("Idle")]
    [SerializeField, Min(0f)] private float idleBreathScale = 0.02f;
    [SerializeField, Min(0f)] private float idleBreathSpeed = 2f;

    [Header("Move")]
    [SerializeField, Min(0f)] private float moveBobAmplitude = 0.04f;
    [SerializeField, Min(0f)] private float moveBobFrequency = 10f;
    [SerializeField, Min(0f)] private float moveLeanAngle = 3f;
    [SerializeField, Min(0.01f)] private float leanLerpSpeed = 10f;
    [SerializeField] private bool flipSpriteByMoveX = true;

    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private Quaternion baseLocalRotation;
    private float breathTimer;
    private float bobTimer;
    private float currentLean;

    private void Reset()
    {
        motor = GetComponent<PlayerMotor2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        visualTarget = spriteRenderer != null ? spriteRenderer.transform : null;
    }

    private void Awake()
    {
        ResolveRefs();
        EnsureVisualTarget();

        if (visualTarget == null)
            return;

        CacheBaseTransform();
    }

    private void OnValidate()
    {
        if (motor == null)
            motor = GetComponent<PlayerMotor2D>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (visualTarget == null && spriteRenderer != null)
            visualTarget = spriteRenderer.transform;
    }

    private void LateUpdate()
    {
        if (visualTarget == null || motor == null)
            return;

        Vector2 move = motor.CurrentMoveInput;
        bool moving = move.sqrMagnitude > 0.001f;

        breathTimer += Time.deltaTime * idleBreathSpeed;
        float breath = Mathf.Sin(breathTimer) * idleBreathScale;

        float bob = 0f;
        if (moving)
        {
            float moveScale = Mathf.Clamp(move.magnitude, 0.6f, 1.6f);
            bobTimer += Time.deltaTime * moveBobFrequency * moveScale;
            bob = Mathf.Sin(bobTimer) * moveBobAmplitude;
        }

        float targetLean = moving ? -move.x * moveLeanAngle : 0f;
        float lerpT = 1f - Mathf.Exp(-leanLerpSpeed * Time.deltaTime);
        currentLean = Mathf.Lerp(currentLean, targetLean, lerpT);

        float scaleX = 1f - (breath * 0.35f);
        float scaleY = 1f + breath;
        if (moving)
        {
            float squash = Mathf.Abs(Mathf.Sin(bobTimer)) * 0.015f;
            scaleX += squash;
            scaleY -= squash;
        }

        visualTarget.localPosition = baseLocalPosition + new Vector3(0f, bob, 0f);
        visualTarget.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, currentLean);
        visualTarget.localScale = new Vector3(
            baseLocalScale.x * scaleX,
            baseLocalScale.y * scaleY,
            baseLocalScale.z);

        if (flipSpriteByMoveX && spriteRenderer != null)
        {
            Vector2 look = moving ? move : motor.LastMoveDir;
            if (Mathf.Abs(look.x) > 0.02f)
                spriteRenderer.flipX = look.x < 0f;
        }
    }

    private void OnDisable()
    {
        RestoreBaseTransform();
    }

    private void ResolveRefs()
    {
        if (motor == null)
            motor = GetComponent<PlayerMotor2D>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void EnsureVisualTarget()
    {
        if (visualTarget != null)
            return;

        if (spriteRenderer == null)
            return;

        visualTarget = spriteRenderer.transform;
        if (visualTarget != transform || !autoCreateVisualChildIfNeeded)
            return;

        // Keep physics/collider on root, animate a child renderer only.
        GameObject child = new GameObject("PlayerVisual");
        Transform childTf = child.transform;
        childTf.SetParent(transform, false);

        SpriteRenderer newRenderer = child.AddComponent<SpriteRenderer>();
        newRenderer.sprite = spriteRenderer.sprite;
        newRenderer.color = spriteRenderer.color;
        newRenderer.flipX = spriteRenderer.flipX;
        newRenderer.flipY = spriteRenderer.flipY;
        newRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        newRenderer.sortingOrder = spriteRenderer.sortingOrder;
        newRenderer.sharedMaterial = spriteRenderer.sharedMaterial;
        newRenderer.maskInteraction = spriteRenderer.maskInteraction;

        spriteRenderer.enabled = false;
        spriteRenderer = newRenderer;
        visualTarget = childTf;
    }

    private void CacheBaseTransform()
    {
        baseLocalPosition = visualTarget.localPosition;
        baseLocalScale = visualTarget.localScale;
        baseLocalRotation = visualTarget.localRotation;
    }

    private void RestoreBaseTransform()
    {
        if (visualTarget == null)
            return;

        visualTarget.localPosition = baseLocalPosition;
        visualTarget.localScale = baseLocalScale;
        visualTarget.localRotation = baseLocalRotation;
    }
}
