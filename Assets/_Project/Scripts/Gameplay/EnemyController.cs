using UnityEngine;
using TMPro;

public class EnemyController : MonoBehaviour
{
    [Header("Move / 移动")]
    [LocalizedLabel("移动速度")]
    [SerializeField] private float moveSpeed = 2.5f;
    [Header("Enemy Separation / 敌人分离")]
    [LocalizedLabel("启用敌人分离")]
    [SerializeField] private bool enableEnemySeparation = true;
    [LocalizedLabel("分离检测半径")]
    [SerializeField, Min(0f)] private float separationRadius = 1.25f;
    [LocalizedLabel("分离强度")]
    [SerializeField, Min(0f)] private float separationStrength = 7f;
    [LocalizedLabel("分离最大速度")]
    [SerializeField, Min(0f)] private float separationMaxSpeed = 4.8f;
    [LocalizedLabel("近距离强分离半径")]
    [SerializeField, Min(0f)] private float closeRepelDistance = 0.42f;
    [LocalizedLabel("近距离分离倍率")]
    [SerializeField, Min(1f)] private float closeRepelBoost = 1.9f;
    [LocalizedLabel("分离检测层")]
    [SerializeField] private LayerMask separationMask = ~0;
    [LocalizedLabel("分离仅检测自身层")]
    [SerializeField] private bool separationUseOwnLayerOnly = true;
    [LocalizedLabel("分离缓存大小")]
    [SerializeField, Min(1)] private int separationBufferSize = 64;

    [Header("HP / 生命")]
    [LocalizedLabel("最大生命值")]
    [SerializeField] private int maxHP = 3;
    private float hp;

    [Header("Rewards / 击杀奖励")]
    [LocalizedLabel("现金奖励")]
    [SerializeField] private int cashValue = 20;
    [LocalizedLabel("经验掉落")]
    [SerializeField] private int xpDrop = 1;
    [Header("Popup: Cash / 金币飘字")]
    [LocalizedLabel("飘字颜色")]
    [SerializeField] private Color cashPopupColor = new Color(0.64f, 1f, 0.64f, 1f);
    [LocalizedLabel("飘字字体")]
    [SerializeField] private TMP_FontAsset cashPopupFont;
    [LocalizedLabel("飘字字号")]
    [SerializeField, Min(0.1f)] private float cashPopupTextSize = 6f;
    [LocalizedLabel("飘字缩放")]
    [SerializeField, Min(0.01f)] private float cashPopupTextScale = 0.12f;
    [LocalizedLabel("飘字上升速度")]
    [SerializeField, Min(0f)] private float cashPopupRiseSpeed = 1.5f;
    [LocalizedLabel("飘字持续时间")]
    [SerializeField, Min(0.05f)] private float cashPopupLifetime = 0.9f;
    [LocalizedLabel("飘字淡出时长")]
    [SerializeField, Min(0.01f)] private float cashPopupFadeOutDuration = 0.35f;
    [Header("Drop: HP / 血包掉落")]
    [LocalizedLabel("血包掉率")]
    [SerializeField, Range(0f, 1f)] private float hpDropChance = 0.12f;
    [LocalizedLabel("血包回复量")]
    [SerializeField] private int hpHealAmount = 1;
    [LocalizedLabel("血包预制体")]
    [SerializeField] private HealthPickup hpPickupPrefab;

    private Rigidbody2D rb;
    private Transform player;
    private int baseMaxHP;
    private float baseMoveSpeed;
    private float runtimeMaxHP;
    private EnemyHitKnockback hitKnockback;

    private XPPickup xpPrefab;
    private Transform pickupsRoot;
    private Collider2D[] separationHits;

    public float CurrentHP => Mathf.Max(0f, hp);
    public float MaxHP => Mathf.Max(1f, runtimeMaxHP);
    public float HealthRatio => Mathf.Clamp01(CurrentHP / MaxHP);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitKnockback = GetComponent<EnemyHitKnockback>();
        baseMaxHP = Mathf.Max(1, maxHP);
        baseMoveSpeed = Mathf.Max(0.2f, moveSpeed);
        runtimeMaxHP = baseMaxHP;
        hp = runtimeMaxHP;
        separationHits = new Collider2D[Mathf.Max(1, separationBufferSize)];
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

        runtimeMaxHP = Mathf.Max(1f, baseMaxHP * hpMultiplier);
        hp = runtimeMaxHP;
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
        Vector2 separationVelocity = ComputeSeparationVelocity(pos, out int nearbyEnemies);

        // In dense crowds, reduce direct "rush-to-player" force and let separation dominate.
        float seekScale = Mathf.Lerp(1f, 0.42f, Mathf.Clamp01((nearbyEnemies - 1) / 7f));
        Vector2 velocity = (dir * moveSpeed * seekScale) + knockbackVelocity + separationVelocity;
        rb.MovePosition(pos + velocity * Time.fixedDeltaTime);
    }

    private Vector2 ComputeSeparationVelocity(Vector2 selfPos, out int nearbyEnemyCount)
    {
        nearbyEnemyCount = 0;
        if (!enableEnemySeparation || separationRadius <= 0f || separationStrength <= 0f)
            return Vector2.zero;

        int bufferSize = Mathf.Max(1, separationBufferSize);
        if (separationHits == null || separationHits.Length != bufferSize)
            separationHits = new Collider2D[bufferSize];

        int queryMask = ResolveSeparationMask();
        int count = Physics2D.OverlapCircleNonAlloc(selfPos, separationRadius, separationHits, queryMask);
        if (count <= 0)
            return Vector2.zero;

        Vector2 repel = Vector2.zero;
        int neighbors = 0;
        int closeNeighbors = 0;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = separationHits[i];
            if (hit == null)
                continue;

            EnemyController other = hit.GetComponent<EnemyController>();
            if (other == null)
                other = hit.GetComponentInParent<EnemyController>();

            if (other == null || other == this || !other.isActiveAndEnabled)
                continue;

            Vector2 delta = selfPos - (Vector2)other.transform.position;
            float dist = delta.magnitude;

            if (dist <= 0.0001f)
            {
                delta = Random.insideUnitCircle;
                dist = Mathf.Max(0.0001f, delta.magnitude);
            }

            if (dist >= separationRadius)
                continue;

            float weight = 1f - (dist / separationRadius);
            weight *= weight;
            if (dist <= closeRepelDistance)
            {
                weight *= closeRepelBoost;
                closeNeighbors++;
            }
            repel += delta.normalized * weight;
            neighbors++;
        }

        nearbyEnemyCount = neighbors;
        if (neighbors <= 0)
            return Vector2.zero;

        if (repel.sqrMagnitude <= 0.0001f)
        {
            if (closeNeighbors <= 0)
                return Vector2.zero;

            // Break perfect overlap stacks with a stable pseudo-random nudge per enemy.
            float phase = (GetInstanceID() * 0.173f) + (Time.time * 2.1f);
            repel = new Vector2(Mathf.Sin(phase), Mathf.Cos(phase));
        }

        float crowdScale = Mathf.Lerp(1f, 2.2f, Mathf.Clamp01((neighbors - 1) / 8f));
        Vector2 separationVelocity = repel * separationStrength * crowdScale;
        float maxSpeed = Mathf.Max(separationMaxSpeed, moveSpeed * 2.2f);
        if (maxSpeed > 0f)
            separationVelocity = Vector2.ClampMagnitude(separationVelocity, maxSpeed);

        return separationVelocity;
    }

    private int ResolveSeparationMask()
    {
        int ownLayerMask = 1 << gameObject.layer;
        if (separationUseOwnLayerOnly)
            return ownLayerMask;

        int mask = separationMask.value;
        if (mask == 0)
            return ownLayerMask;

        // Guarantee own layer is included even if inspector mask is misconfigured.
        return mask | ownLayerMask;
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
