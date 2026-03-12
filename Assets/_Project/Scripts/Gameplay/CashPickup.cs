using UnityEngine;

public class CashPickup : MonoBehaviour
{
    [SerializeField] private int amount = 20;
    [Header("SFX / 音效")]
    [SerializeField] private AudioClip sfxCollect;
    [Header("Magnet")]
    [SerializeField, Min(0f)] private float magnetRadius = 2.6f;
    [SerializeField, Min(0f)] private float magnetSpeed = 8f;
    [SerializeField, Min(0f)] private float autoCollectDistance = 0.18f;

    private static Transform cachedPlayer;
    private bool collected;
    private float magnetRadiusSqr;
    private float autoCollectDistanceSqr;

    public void SetAmount(int value) => amount = Mathf.Max(1, value);

    private void Awake()
    {
        RebuildCachedValues();
        EnsurePickupCollider();
    }

    private void OnValidate()
    {
        RebuildCachedValues();
    }

    private void Update()
    {
        if (collected)
            return;

        Transform player = ResolvePlayer();
        if (player == null)
            return;

        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        float sqrDist = toPlayer.sqrMagnitude;
        if (sqrDist > magnetRadiusSqr)
            return;

        transform.position = Vector2.MoveTowards(transform.position, player.position, magnetSpeed * Time.deltaTime);

        if (sqrDist <= autoCollectDistanceSqr)
            Collect();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Collect();
    }

    public bool ForceCollect()
    {
        if (collected)
            return false;

        Collect();
        return collected;
    }

    private void Collect()
    {
        if (collected)
            return;

        collected = true;

        if (GameFlowController.Instance != null)
        {
            GameFlowController.Instance.AddCash(amount);
            GameFlowController.Instance.NotifyGameplayTutorialPickupCollected(transform.position);
        }

        RunLogger.Event($"Cash pickup collected: +{amount}");
        if (sfxCollect != null && SFXManager.Instance != null)
            SFXManager.Instance.Play(sfxCollect, 0.4f);

        Destroy(gameObject);
    }

    private static Transform ResolvePlayer()
    {
        if (cachedPlayer != null)
            return cachedPlayer;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            cachedPlayer = playerObject.transform;

        return cachedPlayer;
    }

    private void RebuildCachedValues()
    {
        amount = Mathf.Max(1, amount);
        magnetRadius = Mathf.Max(0f, magnetRadius);
        magnetSpeed = Mathf.Max(0f, magnetSpeed);
        autoCollectDistance = Mathf.Max(0f, autoCollectDistance);

        magnetRadiusSqr = magnetRadius * magnetRadius;
        autoCollectDistanceSqr = autoCollectDistance * autoCollectDistance;
    }

    private void EnsurePickupCollider()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
            circle.radius = 0.32f;
            circle.isTrigger = true;
            col = circle;
        }
        else
        {
            col.isTrigger = true;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.simulated = true;
    }
}
