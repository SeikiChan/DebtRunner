using UnityEngine;
using TMPro;

public class EnemyController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 2.5f;

    [Header("HP")]
    [SerializeField] private int maxHP = 3;
    private float hp;

    [Header("Rewards (fixed per enemy type)")]
    [SerializeField] private int cashValue = 20;
    [SerializeField] private int xpDrop = 1;
    [Header("Popup: Cash")]
    [SerializeField] private Color cashPopupColor = new Color(0.64f, 1f, 0.64f, 1f);
    [SerializeField] private TMP_FontAsset cashPopupFont;
    [SerializeField, Min(0.1f)] private float cashPopupTextSize = 6f;
    [SerializeField, Min(0.01f)] private float cashPopupTextScale = 0.12f;
    [SerializeField, Min(0f)] private float cashPopupRiseSpeed = 1.5f;
    [SerializeField, Min(0.05f)] private float cashPopupLifetime = 0.9f;
    [SerializeField, Min(0.01f)] private float cashPopupFadeOutDuration = 0.35f;
    [Header("Drop: HP")]
    [SerializeField, Range(0f, 1f)] private float hpDropChance = 0.12f;
    [SerializeField] private int hpHealAmount = 1;
    [SerializeField] private HealthPickup hpPickupPrefab;

    private Rigidbody2D rb;
    private Transform player;
    private int baseMaxHP;
    private float baseMoveSpeed;
    private EnemyHitKnockback hitKnockback;

    private XPPickup xpPrefab;
    private Transform pickupsRoot;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitKnockback = GetComponent<EnemyHitKnockback>();
        baseMaxHP = Mathf.Max(1, maxHP);
        baseMoveSpeed = Mathf.Max(0.2f, moveSpeed);
        hp = baseMaxHP;
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

        hp = Mathf.Max(1f, baseMaxHP * hpMultiplier);
        moveSpeed = baseMoveSpeed * speedMultiplier;
    }

    private void FixedUpdate()
    {
        if (player == null) return;
        Vector2 pos = rb.position;
        Vector2 dir = ((Vector2)player.position - pos).normalized;
        Vector2 knockbackVelocity = hitKnockback != null
            ? hitKnockback.GetCurrentVelocity(Time.fixedDeltaTime)
            : Vector2.zero;

        Vector2 velocity = (dir * moveSpeed) + knockbackVelocity;
        rb.MovePosition(pos + velocity * Time.fixedDeltaTime);
    }

    public void TakeDamage(int dmg)
    {
        TakeDamage(dmg, Vector2.zero, 1f);
    }

    public void TakeDamage(int dmg, Vector2 hitDirection)
    {
        TakeDamage(dmg, hitDirection, 1f);
    }

    public void TakeDamage(int dmg, Vector2 hitDirection, float knockbackForceMultiplier)
    {
        hp -= Mathf.Max(0, dmg);
        if (hitKnockback != null)
            hitKnockback.ApplyHit(hitDirection, knockbackForceMultiplier);

        if (hp <= 0f) Die();
    }

    private void Die()
    {
        RunLogger.Event($"Enemy defeated at {transform.position.x:F2},{transform.position.y:F2}. rewards: cash={cashValue}, xp={xpDrop}");

        if (GameFlowController.Instance != null)
            GameFlowController.Instance.AddCash(cashValue);

        SpawnCashPopup();

        if (xpPrefab != null)
        {
            var p = Instantiate(xpPrefab, transform.position, Quaternion.identity, pickupsRoot);
            p.SetAmount(xpDrop);
        }

        TryDropHealthPickup();

        Destroy(gameObject);
    }

    private void SpawnCashPopup()
    {
        if (cashValue <= 0)
            return;

        Vector3 popupPos = transform.position + Vector3.up * 0.85f + Vector3.right * Random.Range(-0.16f, 0.16f);
        WorldPopupText.Spawn(
            $"+${cashValue}",
            popupPos,
            cashPopupColor,
            cashPopupFont,
            cashPopupTextSize,
            cashPopupTextScale,
            cashPopupRiseSpeed,
            cashPopupLifetime,
            cashPopupFadeOutDuration);
    }

    private void TryDropHealthPickup()
    {
        if (hpDropChance <= 0f || Random.value > hpDropChance)
            return;

        HealthPickup pickup = null;

        if (hpPickupPrefab != null)
        {
            pickup = Instantiate(hpPickupPrefab, transform.position, Quaternion.identity, pickupsRoot);
        }
        else if (xpPrefab != null)
        {
            XPPickup fallback = Instantiate(xpPrefab, transform.position, Quaternion.identity, pickupsRoot);
            fallback.enabled = false;
            fallback.name = "HPPickup_Fallback";

            pickup = fallback.GetComponent<HealthPickup>();
            if (pickup == null)
                pickup = fallback.gameObject.AddComponent<HealthPickup>();
        }

        if (pickup == null)
            return;

        pickup.SetHealAmount(hpHealAmount);
        RunLogger.Event($"HP pickup dropped. heal={Mathf.Max(1, hpHealAmount)}, chance={hpDropChance:F2}");
    }
}
