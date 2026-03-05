using UnityEngine;
using TMPro;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private bool enableEnemySeparation = true;
    [SerializeField, Min(0f)] private float separationRadius = 1.25f;
    [SerializeField, Min(0f)] private float separationStrength = 7f;
    [SerializeField, Min(0f)] private float separationMaxSpeed = 4.8f;
    [SerializeField, Min(0f)] private float closeRepelDistance = 0.42f;
    [SerializeField, Min(1f)] private float closeRepelBoost = 1.9f;
    [SerializeField] private LayerMask separationMask = ~0;
    [SerializeField] private bool separationUseOwnLayerOnly = true;
    [SerializeField, Min(1)] private int separationBufferSize = 64;

    [SerializeField] private AudioClip sfxHit;
    [SerializeField] private AudioClip sfxDeath;

    [SerializeField] private int maxHP = 3;
    private float hp;

    [SerializeField] private int cashValue = 20;
    [SerializeField] private int xpDrop = 1;
    [SerializeField] private Color cashPopupColor = new Color(0.64f, 1f, 0.64f, 1f);
    [SerializeField] private TMP_FontAsset cashPopupFont;
    [SerializeField, Min(0.1f)] private float cashPopupTextSize = 6f;
    [SerializeField, Min(0.01f)] private float cashPopupTextScale = 0.12f;
    [SerializeField, Min(0f)] private float cashPopupRiseSpeed = 1.5f;
    [SerializeField, Min(0.05f)] private float cashPopupLifetime = 0.9f;
    [SerializeField, Min(0.01f)] private float cashPopupFadeOutDuration = 0.35f;
    [SerializeField, Range(0f, 1f)] private float hpDropChance = 0.12f;
    [SerializeField] private int hpHealAmount = 1;
    [SerializeField] private HealthPickup hpPickupPrefab;

    private Rigidbody2D rb;
    private Transform player;
    private int baseMaxHP;
    private float baseMoveSpeed;
    private float runtimeMaxHP;
    private EnemyHitKnockback hitKnockback;
    private EnemyHitFeedback hitFeedback;

    private XPPickup xpPrefab;
    private Transform pickupsRoot;
    private Collider2D[] separationHits;

    public float CurrentHP => Mathf.Max(0f, hp);
    public float MaxHP => Mathf.Max(1f, runtimeMaxHP);
    public float HealthRatio => Mathf.Clamp01(CurrentHP / MaxHP);

    /// <summary>
    /// 褰撲负 true 鏃讹紝FixedUpdate 璺宠繃杩借釜绉诲姩銆?
    /// EnemyDashAttack / EnemyOrbitMovement 绛夌粍浠朵娇鐢ㄦ灞炴€ф帴绠＄Щ鍔ㄣ€?
    /// </summary>
    public bool SuppressChaseMovement { get; set; }
    public Transform Player => player;
    public Rigidbody2D Rb => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitKnockback = GetComponent<EnemyHitKnockback>();
        hitFeedback = GetComponent<EnemyHitFeedback>();
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
        if (SuppressChaseMovement) return;
        Vector2 pos = rb.position;
        Vector2 dir = ((Vector2)player.position - pos).normalized;
        Vector2 knockbackVelocity = hitKnockback != null
            ? hitKnockback.GetCurrentVelocity(Time.fixedDeltaTime)
            : Vector2.zero;
        Vector2 separationVelocity = ComputeSeparationVelocity(pos, out int nearbyEnemies);

        // In dense crowds, reduce direct "rush-to-player" force and let separation dominate.
        float seekScale = Mathf.Lerp(1f, 0.42f, Mathf.Clamp01((nearbyEnemies - 1) / 7f));
        Vector2 velocity = (dir * moveSpeed * seekScale) + knockbackVelocity + separationVelocity;
        Vector2 newPos = pos + velocity * Time.fixedDeltaTime;
        if (CircleBoundary.Instance != null)
            newPos = CircleBoundary.Instance.ClampPosition(newPos);
        rb.MovePosition(newPos);
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
        if (hp <= 0f) return; // 宸叉浜″拷鐣?

        hp -= Mathf.Max(0, dmg);
        if (hitKnockback != null)
            hitKnockback.ApplyHit(hitDirection, knockbackForceMultiplier);

        if (hp <= 0f)
        {
            if (SFXManager.Instance != null)
            {
                AudioClip deathClip = sfxDeath != null ? sfxDeath : sfxHit;
                if (deathClip != null)
                    SFXManager.Instance.PlayAtPoint(deathClip, transform.position, 0.6f);
            }
            Die();
        }
        else
        {
            if (hitFeedback != null) hitFeedback.PlayHit();
            if (sfxHit != null && SFXManager.Instance != null)
                SFXManager.Instance.PlayAtPoint(sfxHit, transform.position, 0.4f);
        }
    }

    private void Die()
    {
        RunLogger.Event($"Enemy defeated at {transform.position.x:F2},{transform.position.y:F2}. rewards: cash={cashValue}, xp={xpDrop}");

        bool isBossEnemy = GetComponent<BossAttackController>() != null;

        if (GameFlowController.Instance != null)
            GameFlowController.Instance.AddCash(cashValue);

        SpawnCashPopup();

        if (xpPrefab != null)
        {
            int totalXP = xpDrop;
            if (GameFlowController.Instance != null)
                totalXP += GameFlowController.Instance.BonusXPPerKill;
            var p = Instantiate(xpPrefab, transform.position, Quaternion.identity, pickupsRoot);
            p.SetAmount(totalXP);
        }

        TryDropHealthPickup();
        LateBossDefeatNotify(isBossEnemy);

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        SuppressChaseMovement = true;

        float deathDuration = 0f;
        if (hitFeedback != null)
            deathDuration = hitFeedback.PlayDeath();

        if (deathDuration > 0f)
            Destroy(gameObject, deathDuration + 0.02f);
        else
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

    private void LateBossDefeatNotify(bool isBossEnemy)
    {
        if (!isBossEnemy)
            return;

        if (GameFlowController.Instance != null)
            GameFlowController.Instance.NotifyBossDefeated();
    }
}

