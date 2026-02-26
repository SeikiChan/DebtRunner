using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public class BossShockwaveRing : MonoBehaviour
{
    [SerializeField, Min(16)] private int segments = 72;

    private LineRenderer lineRenderer;
    private Transform player;
    private PlayerHealth playerHealth;
    private PlayerMotor2D playerMotor;
    private Rigidbody2D playerRb;

    private float startRadius;
    private float endRadius;
    private float duration;
    private float hitThickness;
    private float knockbackImpulse;
    private int damage;
    private float elapsed;
    private bool hitApplied;
    private bool initialized;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = true;
            lineRenderer.positionCount = Mathf.Max(16, segments);
        }
    }

    public void Init(
        Transform playerTransform,
        PlayerHealth playerHp,
        PlayerMotor2D playerMove,
        Rigidbody2D playerBody,
        float radiusFrom,
        float radiusTo,
        float expandSeconds,
        float ringThickness,
        int ringDamage,
        float ringKnockbackImpulse)
    {
        player = playerTransform;
        playerHealth = playerHp;
        playerMotor = playerMove;
        playerRb = playerBody;
        startRadius = Mathf.Max(0.05f, radiusFrom);
        endRadius = Mathf.Max(startRadius, radiusTo);
        duration = Mathf.Max(0.01f, expandSeconds);
        hitThickness = Mathf.Max(0.05f, ringThickness);
        damage = Mathf.Max(1, ringDamage);
        knockbackImpulse = Mathf.Max(0f, ringKnockbackImpulse);
        elapsed = 0f;
        hitApplied = false;
        initialized = true;

        if (lineRenderer != null)
            lineRenderer.loop = true;

        UpdateRingVisual(startRadius);
    }

    public void SetSegments(int value)
    {
        segments = Mathf.Max(16, value);
        if (lineRenderer != null)
            lineRenderer.positionCount = segments;
    }

    private void Update()
    {
        if (!initialized)
            return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float radius = Mathf.Lerp(startRadius, endRadius, t);

        UpdateRingVisual(radius);
        TryHitPlayer(radius);

        if (t >= 1f)
            Destroy(gameObject);
    }

    private void UpdateRingVisual(float radius)
    {
        if (lineRenderer == null)
            return;

        int pointCount = Mathf.Max(16, segments);
        if (lineRenderer.positionCount != pointCount)
            lineRenderer.positionCount = pointCount;

        float step = Mathf.PI * 2f / pointCount;
        Vector3 center = transform.position;
        for (int i = 0; i < pointCount; i++)
        {
            float a = i * step;
            Vector3 pos = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            lineRenderer.SetPosition(i, pos);
        }
    }

    private void TryHitPlayer(float ringRadius)
    {
        if (hitApplied || player == null || playerHealth == null)
            return;

        float dist = Vector2.Distance(player.position, transform.position);
        if (Mathf.Abs(dist - ringRadius) > hitThickness)
            return;

        playerHealth.TakeDamage(damage);

        Vector2 knockDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        if (knockDir.sqrMagnitude > 0.0001f)
        {
            if (playerMotor != null)
                playerMotor.ApplyExternalImpulse(knockDir, knockbackImpulse);
            else if (playerRb != null)
                playerRb.linearVelocity += knockDir * knockbackImpulse;
        }

        hitApplied = true;
    }
}
