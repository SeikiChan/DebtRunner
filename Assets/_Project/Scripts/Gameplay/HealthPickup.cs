using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [SerializeField] private int healAmount = 1;

    [Header("Magnet")]
    [SerializeField, Min(0f)] private float magnetRadius = 2.2f;
    [SerializeField, Min(0f)] private float magnetSpeed = 7f;
    [SerializeField, Min(0f)] private float autoCollectDistance = 0.18f;

    [Header("Visual (Optional)")]
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private bool tintVisualOnAwake = true;
    [SerializeField] private Color tintColor = new Color(1f, 0.38f, 0.38f, 1f);

    private static Transform cachedPlayer;
    private bool collected;
    private float magnetRadiusSqr;
    private float autoCollectDistanceSqr;

    public void SetHealAmount(int value) => healAmount = Mathf.Max(1, value);

    private void Awake()
    {
        RebuildCachedValues();

        if (tintVisualOnAwake)
        {
            if (visualRenderer == null)
                visualRenderer = GetComponent<SpriteRenderer>();

            if (visualRenderer != null)
                visualRenderer.color = tintColor;
        }
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

    private void Collect()
    {
        if (collected)
            return;

        PlayerHealth playerHealth = ResolvePlayerHealth();
        if (playerHealth == null)
            return;

        collected = true;
        int before = playerHealth.CurrentHP;
        playerHealth.Heal(healAmount);
        int healed = Mathf.Max(0, playerHealth.CurrentHP - before);
        RunLogger.Event($"HP pickup collected: +{healed}");
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

    private PlayerHealth ResolvePlayerHealth()
    {
        Transform player = ResolvePlayer();
        if (player != null)
        {
            PlayerHealth hpOnPlayer = player.GetComponent<PlayerHealth>();
            if (hpOnPlayer != null)
                return hpOnPlayer;
        }

        return FindObjectOfType<PlayerHealth>();
    }

    private void RebuildCachedValues()
    {
        healAmount = Mathf.Max(1, healAmount);
        magnetRadius = Mathf.Max(0f, magnetRadius);
        magnetSpeed = Mathf.Max(0f, magnetSpeed);
        autoCollectDistance = Mathf.Max(0f, autoCollectDistance);

        magnetRadiusSqr = magnetRadius * magnetRadius;
        autoCollectDistanceSqr = autoCollectDistance * autoCollectDistance;
    }
}
