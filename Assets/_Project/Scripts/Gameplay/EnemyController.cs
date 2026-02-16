using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 2.5f;

    [Header("HP")]
    [SerializeField] private int maxHP = 3;
    private int hp;

    [Header("Rewards (fixed per enemy type)")]
    [SerializeField] private int cashValue = 20;
    [SerializeField] private int xpDrop = 1;

    private Rigidbody2D rb;
    private Transform player;

    private XPPickup xpPrefab;
    private Transform pickupsRoot;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hp = maxHP;
    }

    public void Init(Transform playerTf, XPPickup xpPickupPrefab, Transform pickupsParent)
    {
        player = playerTf;
        xpPrefab = xpPickupPrefab;
        pickupsRoot = pickupsParent;

        var shooter = GetComponent<EnemyShooter>();
        var worldProjectiles = GameObject.Find("World/Projectiles");
        if (shooter != null && worldProjectiles != null)
            shooter.Init(playerTf, worldProjectiles.transform);
    }

    public void ApplyRuntimeModifiers(float hpMultiplier, float speedMultiplier)
    {
        hpMultiplier = Mathf.Max(0.2f, hpMultiplier);
        speedMultiplier = Mathf.Max(0.2f, speedMultiplier);

        maxHP = Mathf.Max(1, Mathf.RoundToInt(maxHP * hpMultiplier));
        hp = maxHP;
        moveSpeed *= speedMultiplier;
    }

    private void FixedUpdate()
    {
        if (player == null) return;
        Vector2 pos = rb.position;
        Vector2 dir = ((Vector2)player.position - pos).normalized;
        rb.MovePosition(pos + dir * moveSpeed * Time.fixedDeltaTime);
    }

    public void TakeDamage(int dmg)
    {
        hp -= dmg;
        if (hp <= 0) Die();
    }

    private void Die()
    {
        RunLogger.Event($"Enemy defeated at {transform.position.x:F2},{transform.position.y:F2}. rewards: cash={cashValue}, xp={xpDrop}");

        GameFlowController.Instance.AddCash(cashValue);

        if (xpPrefab != null)
        {
            var p = Instantiate(xpPrefab, transform.position, Quaternion.identity, pickupsRoot);
            p.SetAmount(xpDrop);
        }

        Destroy(gameObject);
    }
}
