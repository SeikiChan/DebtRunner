using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Serialization;

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
    [SerializeField, Range(0f, 2f)] private float sfxHitVolume = 0.8f;
    [SerializeField, Range(0f, 2f)] private float sfxDeathVolume = 0.9f;

    [Header("Hitbox")]
    [SerializeField] private bool fitHitboxToSprite = true;
    [SerializeField] private bool preferPolygonHitbox = true;
    [SerializeField, Range(0.8f, 1.25f)] private float spriteHitboxScale = 1.02f;

    [SerializeField] private int maxHP = 30;
    private float hp;

    [SerializeField] private int cashValue = 20;
    [SerializeField] private int xpDrop = 10;
    [SerializeField] private CashPickup cashPickupPrefab;
    [SerializeField, Min(0f)] private float pickupDropScatterRadius = 0.26f;
    [SerializeField, Range(0f, 0.95f)] private float pickupDropInnerRadiusRatio = 0.35f;
    [SerializeField, Min(0f)] private float pickupDropVerticalOffset = 0.05f;
    [SerializeField, Min(0f)] private float pickupDropMinSeparation = 0.28f;
    [SerializeField, Min(1)] private int pickupDropPositionAttempts = 12;
    [FormerlySerializedAs("cashPopupColor")]
    [SerializeField] private Color damagePopupColor = new Color(1f, 0.92f, 0.62f, 1f);
    [FormerlySerializedAs("cashPopupFont")]
    [SerializeField] private TMP_FontAsset damagePopupFont;
    [FormerlySerializedAs("cashPopupTextSize")]
    [SerializeField, Min(0.1f)] private float damagePopupTextSize = 6f;
    [FormerlySerializedAs("cashPopupTextScale")]
    [SerializeField, Min(0.01f)] private float damagePopupTextScale = 0.12f;
    [FormerlySerializedAs("cashPopupRiseSpeed")]
    [SerializeField, Min(0f)] private float damagePopupRiseSpeed = 1.5f;
    [FormerlySerializedAs("cashPopupLifetime")]
    [SerializeField, Min(0.05f)] private float damagePopupLifetime = 0.9f;
    [FormerlySerializedAs("cashPopupFadeOutDuration")]
    [SerializeField, Min(0.01f)] private float damagePopupFadeOutDuration = 0.35f;
    [SerializeField, Range(0f, 1f)] private float hpDropChance = 0.12f;
    [SerializeField] private int hpHealAmount = 10;
    [SerializeField] private HealthPickup hpPickupPrefab;

    private Rigidbody2D rb;
    private Transform player;
    private int baseMaxHP;
    private float baseMoveSpeed;
    private float runtimeMaxHP;
    private EnemyHitKnockback hitKnockback;
    private EnemyHitFeedback hitFeedback;
    private SpriteRenderer primarySpriteRenderer;
    private Collider2D targetCollider;

    private XPPickup xpPrefab;
    private XPPickup[] xpPickupPrefabs;
    private CashPickup cashPrefab;
    private Transform pickupsRoot;
    private Collider2D[] separationHits;
    private readonly List<Vector2> spritePhysicsShapeBuffer = new List<Vector2>(32);
    private static readonly List<EnemyController> activeEnemies = new List<EnemyController>(256);
    private static bool missingCashPickupWarned;

    public float CurrentHP => Mathf.Max(0f, hp);
    public float MaxHP => Mathf.Max(1f, runtimeMaxHP);
    public float HealthRatio => Mathf.Clamp01(CurrentHP / MaxHP);
    public Collider2D TargetCollider => targetCollider;
    public static IReadOnlyList<EnemyController> ActiveEnemies => activeEnemies;

    /// <summary>
    /// When true, FixedUpdate skips default chase movement.
    /// Used by movement/attack components such as EnemyDashAttack.
    /// </summary>
    public bool SuppressChaseMovement { get; set; }
    public Transform Player => player;
    public Rigidbody2D Rb => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        primarySpriteRenderer = ResolvePrimarySpriteRenderer();
        ConfigureSpriteHitbox();
        RefreshTargetCollider();
        hitKnockback = GetComponent<EnemyHitKnockback>();
        hitFeedback = GetComponent<EnemyHitFeedback>();
        if (hitFeedback == null)
            hitFeedback = gameObject.AddComponent<EnemyHitFeedback>();
        baseMaxHP = Mathf.Max(1, maxHP);
        baseMoveSpeed = Mathf.Max(0.2f, moveSpeed);
        runtimeMaxHP = baseMaxHP;
        hp = runtimeMaxHP;
        separationHits = new Collider2D[Mathf.Max(1, separationBufferSize)];
    }

    private void OnEnable()
    {
        RefreshTargetCollider();
        if (!activeEnemies.Contains(this))
            activeEnemies.Add(this);
    }

    private void OnDisable()
    {
        activeEnemies.Remove(this);
    }

    private SpriteRenderer ResolvePrimarySpriteRenderer()
    {
        if (primarySpriteRenderer != null)
            return primarySpriteRenderer;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        float bestArea = float.MinValue;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null || sr.sprite == null)
                continue;

            Bounds bounds = sr.bounds;
            float area = bounds.size.x * bounds.size.y;
            if (area > bestArea)
            {
                bestArea = area;
                primarySpriteRenderer = sr;
            }
        }

        return primarySpriteRenderer;
    }

    private void ConfigureSpriteHitbox()
    {
        if (!fitHitboxToSprite)
            return;

        SpriteRenderer sr = ResolvePrimarySpriteRenderer();
        if (sr == null || sr.sprite == null)
            return;

        if (preferPolygonHitbox && TryConfigurePolygonHitbox(sr))
            return;

        ConfigureFallbackBoxHitbox(sr);
    }

    private bool TryConfigurePolygonHitbox(SpriteRenderer sr)
    {
        Sprite sprite = sr.sprite;
        if (sprite == null)
            return false;

        int shapeCount = sprite.GetPhysicsShapeCount();
        if (shapeCount <= 0)
            return false;

        Collider2D previousCollider = GetComponent<Collider2D>();
        PolygonCollider2D polygon = GetComponent<PolygonCollider2D>();
        if (polygon == null)
        {
            bool wasTrigger = previousCollider == null || previousCollider.isTrigger;
            PhysicsMaterial2D sharedMaterial = previousCollider != null ? previousCollider.sharedMaterial : null;
            if (previousCollider != null)
            {
                previousCollider.enabled = false;
                Destroy(previousCollider);
            }

            polygon = gameObject.AddComponent<PolygonCollider2D>();
            polygon.isTrigger = wasTrigger;
            polygon.sharedMaterial = sharedMaterial;
        }

        polygon.enabled = true;
        polygon.pathCount = shapeCount;

        for (int i = 0; i < shapeCount; i++)
        {
            spritePhysicsShapeBuffer.Clear();
            sprite.GetPhysicsShape(i, spritePhysicsShapeBuffer);
            Vector2[] localPoints = new Vector2[spritePhysicsShapeBuffer.Count];
            Vector2 spriteCenter = sprite.bounds.center;

            for (int p = 0; p < spritePhysicsShapeBuffer.Count; p++)
            {
                Vector2 point = ApplySpriteFlip(spritePhysicsShapeBuffer[p], sr, sprite);
                point = spriteCenter + ((point - spriteCenter) * Mathf.Max(0.01f, spriteHitboxScale));
                Vector3 worldPoint = sr.transform.TransformPoint(point);
                localPoints[p] = transform.InverseTransformPoint(worldPoint);
            }

            polygon.SetPath(i, localPoints);
        }

        targetCollider = polygon;

        return true;
    }

    private void ConfigureFallbackBoxHitbox(SpriteRenderer sr)
    {
        Sprite sprite = sr.sprite;
        if (sprite == null)
            return;

        Collider2D previousCollider = GetComponent<Collider2D>();
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
        {
            bool wasTrigger = previousCollider == null || previousCollider.isTrigger;
            PhysicsMaterial2D sharedMaterial = previousCollider != null ? previousCollider.sharedMaterial : null;
            if (previousCollider != null)
            {
                previousCollider.enabled = false;
                Destroy(previousCollider);
            }

            box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = wasTrigger;
            box.sharedMaterial = sharedMaterial;
        }

        Bounds spriteBounds = sprite.bounds;
        Vector2 center = spriteBounds.center;
        Vector2 extents = spriteBounds.extents * Mathf.Max(0.01f, spriteHitboxScale);
        Vector2[] corners =
        {
            center + new Vector2(-extents.x, -extents.y),
            center + new Vector2(-extents.x, extents.y),
            center + new Vector2(extents.x, extents.y),
            center + new Vector2(extents.x, -extents.y)
        };

        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 point = ApplySpriteFlip(corners[i], sr, sprite);
            Vector3 worldPoint = sr.transform.TransformPoint(point);
            Vector2 localPoint = transform.InverseTransformPoint(worldPoint);
            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }

        box.enabled = true;
        box.offset = (min + max) * 0.5f;
        box.size = max - min;
        targetCollider = box;
    }

    private Vector2 ApplySpriteFlip(Vector2 point, SpriteRenderer sr, Sprite sprite)
    {
        Vector2 result = point;
        Vector2 center = sprite.bounds.center;

        if (sr.flipX)
            result.x = (center.x * 2f) - result.x;
        if (sr.flipY)
            result.y = (center.y * 2f) - result.y;

        return result;
    }

    private void RefreshTargetCollider()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        targetCollider = null;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null || !col.enabled)
                continue;

            targetCollider = col;
            return;
        }

        if (colliders.Length > 0)
            targetCollider = colliders[0];
    }

    public void Init(Transform playerTf, XPPickup xpPickupPrefab, Transform pickupsParent, CashPickup cashPickupTemplate = null)
    {
        Init(playerTf, xpPickupPrefab != null ? new[] { xpPickupPrefab } : null, pickupsParent, cashPickupTemplate);
    }

    public void Init(Transform playerTf, XPPickup[] xpPickupPrefabOptions, Transform pickupsParent, CashPickup cashPickupTemplate = null)
    {
        player = playerTf;
        xpPickupPrefabs = xpPickupPrefabOptions;
        xpPrefab = GetPrimaryXPPickupPrefab();
        cashPrefab = cashPickupTemplate != null ? cashPickupTemplate : cashPickupPrefab;
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

        int damageValue = Mathf.Max(0, dmg);
        hp -= damageValue;
        SpawnDamagePopup(damageValue);
        if (hitKnockback != null)
            hitKnockback.ApplyHit(hitDirection, knockbackForceMultiplier);

        if (hp <= 0f)
        {
            if (SFXManager.Instance != null)
            {
                AudioClip deathClip = sfxDeath != null ? sfxDeath : sfxHit;
                if (deathClip != null)
                    SFXManager.Instance.PlayAtPoint(deathClip, transform.position, sfxDeathVolume);
            }
            Die();
        }
        else
        {
            if (hitFeedback != null) hitFeedback.PlayHit();
            if (sfxHit != null && SFXManager.Instance != null)
                SFXManager.Instance.PlayExclusive(sfxHit, sfxHitVolume);
        }
    }

    private void Die()
    {
        RunLogger.Event($"Enemy defeated at {transform.position.x:F2},{transform.position.y:F2}. rewards: cash={cashValue}, xp={xpDrop}");

        // Apply reward multiplier from wheel "Risk & Reward" outcome
        float rewardMul = GameFlowController.Instance != null ? GameFlowController.Instance.CurrentRewardMultiplier : 1f;
        float roundCashMul = GameFlowController.Instance != null
            ? GameFlowController.Instance.GetCashDropMultiplierForRound(GameFlowController.Instance.GetCurrentRound())
            : 1f;
        int effectiveCash = Mathf.RoundToInt(cashValue * rewardMul * roundCashMul);
        int effectiveXP = Mathf.RoundToInt(xpDrop * rewardMul);

        bool isBossEnemy = GetComponent<BossAttackController>() != null;
        List<Vector2> reservedDropOffsets = new List<Vector2>(3);

        SpawnCashPickup(ResolvePickupDropPosition(reservedDropOffsets), effectiveCash);

        int totalXP = effectiveXP;
        if (GameFlowController.Instance != null)
            totalXP += GameFlowController.Instance.BonusXPPerKill;
        SpawnXPPickups(totalXP, reservedDropOffsets);

        TryDropHealthPickup(reservedDropOffsets);
        LateBossDefeatNotify(isBossEnemy);

        if (GameFlowController.Instance != null)
            GameFlowController.Instance.NotifyEnemyKilled(transform.position);

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        SuppressChaseMovement = true;

        if (isBossEnemy && GameFlowController.Instance != null && GameFlowController.Instance.TryStartBossVictorySequence(this))
            return;

        float deathDuration = 0f;
        if (hitFeedback != null)
            deathDuration = hitFeedback.PlayDeath();

        if (deathDuration > 0f)
            Destroy(gameObject, deathDuration + 0.02f);
        else
            Destroy(gameObject);
    }

    private void SpawnDamagePopup(int damageValue)
    {
        if (damageValue <= 0)
            return;

        Vector3 popupPos = transform.position + Vector3.up * 0.82f + Vector3.right * Random.Range(-0.14f, 0.14f);
        WorldPopupText.Spawn(
            damageValue.ToString(),
            popupPos,
            damagePopupColor,
            damagePopupFont,
            damagePopupTextSize,
            damagePopupTextScale,
            damagePopupRiseSpeed,
            damagePopupLifetime,
            damagePopupFadeOutDuration);
    }

    private void SpawnCashPickup(Vector3 dropPos, int amount)
    {
        if (amount <= 0)
            return;

        CashPickup pickup = null;

        if (cashPrefab != null)
        {
            pickup = Instantiate(cashPrefab, dropPos, Quaternion.identity, pickupsRoot);
        }
        else if (cashPickupPrefab != null)
        {
            pickup = Instantiate(cashPickupPrefab, dropPos, Quaternion.identity, pickupsRoot);
        }

        if (pickup == null)
        {
            if (!missingCashPickupWarned)
            {
                missingCashPickupWarned = true;
                RunLogger.Warning("Cash pickup prefab missing. Falling back to direct AddCash until a CashPickup prefab is assigned.");
            }

            if (GameFlowController.Instance != null)
                GameFlowController.Instance.AddCash(amount);
            return;
        }

        pickup.SetAmount(amount);
    }

    private void SpawnXPPickups(int totalXP, List<Vector2> reservedDropOffsets)
    {
        if (totalXP <= 0)
            return;

        XPPickup[] prefabs = GetAvailableXPPickupPrefabs();
        if (prefabs == null || prefabs.Length == 0)
            return;

        List<XPPickup> dropPlan = BuildExactXPPickupPlan(totalXP, prefabs);
        if (dropPlan == null || dropPlan.Count == 0)
        {
            XPPickup fallbackPrefab = GetPrimaryXPPickupPrefab();
            if (fallbackPrefab == null)
                return;

            XPPickup fallback = Instantiate(fallbackPrefab, ResolvePickupDropPosition(reservedDropOffsets), Quaternion.identity, pickupsRoot);
            fallback.SetAmount(totalXP);
            return;
        }

        for (int i = 0; i < dropPlan.Count; i++)
        {
            XPPickup pickupPrefab = dropPlan[i];
            if (pickupPrefab == null)
                continue;

            Instantiate(pickupPrefab, ResolvePickupDropPosition(reservedDropOffsets), Quaternion.identity, pickupsRoot);
        }
    }

    private XPPickup[] GetAvailableXPPickupPrefabs()
    {
        if (xpPickupPrefabs == null || xpPickupPrefabs.Length == 0)
            return xpPrefab != null ? new[] { xpPrefab } : null;

        List<XPPickup> valid = new List<XPPickup>(xpPickupPrefabs.Length);
        for (int i = 0; i < xpPickupPrefabs.Length; i++)
        {
            if (xpPickupPrefabs[i] != null)
                valid.Add(xpPickupPrefabs[i]);
        }

        if (valid.Count == 0)
            return xpPrefab != null ? new[] { xpPrefab } : null;

        return valid.ToArray();
    }

    private XPPickup GetPrimaryXPPickupPrefab()
    {
        if (xpPickupPrefabs != null)
        {
            for (int i = 0; i < xpPickupPrefabs.Length; i++)
            {
                if (xpPickupPrefabs[i] != null)
                    return xpPickupPrefabs[i];
            }
        }

        return xpPrefab;
    }

    private List<XPPickup> BuildExactXPPickupPlan(int totalXP, XPPickup[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0 || totalXP <= 0)
            return null;

        int[] minPickupCounts = new int[totalXP + 1];
        int[] pickedPrefabIndex = new int[totalXP + 1];
        for (int sum = 1; sum <= totalXP; sum++)
        {
            minPickupCounts[sum] = int.MaxValue;
            pickedPrefabIndex[sum] = -1;
        }

        minPickupCounts[0] = 0;

        for (int sum = 1; sum <= totalXP; sum++)
        {
            for (int i = 0; i < prefabs.Length; i++)
            {
                XPPickup prefab = prefabs[i];
                if (prefab == null)
                    continue;

                int value = prefab.Amount;
                if (value <= 0 || value > sum || minPickupCounts[sum - value] == int.MaxValue)
                    continue;

                int candidateCount = minPickupCounts[sum - value] + 1;
                int currentIndex = pickedPrefabIndex[sum];
                int currentValue = currentIndex >= 0 && currentIndex < prefabs.Length && prefabs[currentIndex] != null
                    ? prefabs[currentIndex].Amount
                    : -1;

                if (candidateCount < minPickupCounts[sum] ||
                    (candidateCount == minPickupCounts[sum] && value > currentValue))
                {
                    minPickupCounts[sum] = candidateCount;
                    pickedPrefabIndex[sum] = i;
                }
            }
        }

        if (pickedPrefabIndex[totalXP] < 0)
            return null;

        List<XPPickup> plan = new List<XPPickup>(minPickupCounts[totalXP]);
        int remaining = totalXP;
        while (remaining > 0)
        {
            int prefabIndex = pickedPrefabIndex[remaining];
            if (prefabIndex < 0 || prefabIndex >= prefabs.Length || prefabs[prefabIndex] == null)
                return null;

            XPPickup prefab = prefabs[prefabIndex];
            int value = prefab.Amount;
            if (value <= 0 || value > remaining)
                return null;

            plan.Add(prefab);
            remaining -= value;
        }

        return plan;
    }

    private Vector3 ResolvePickupDropPosition(List<Vector2> reservedOffsets)
    {
        float radius = Mathf.Max(0f, pickupDropScatterRadius);
        float yOffset = Mathf.Max(0f, pickupDropVerticalOffset);
        if (radius <= 0f)
            return transform.position + new Vector3(0f, yOffset, 0f);

        Vector2 chosenOffset = RandomDropOffset(radius);
        bool hasReserved = reservedOffsets != null && reservedOffsets.Count > 0;
        float minSeparation = Mathf.Max(0f, pickupDropMinSeparation);
        float maxPossibleSeparation = radius * 2f;
        if (hasReserved && minSeparation > maxPossibleSeparation)
            minSeparation = maxPossibleSeparation;
        float minSepSqr = minSeparation * minSeparation;
        int attempts = Mathf.Max(1, pickupDropPositionAttempts);

        if (hasReserved && minSeparation > 0f)
        {
            bool found = false;
            Vector2 bestCandidate = chosenOffset;
            float bestCandidateMinDistSqr = -1f;
            for (int i = 0; i < attempts; i++)
            {
                Vector2 candidate = RandomDropOffset(radius);
                bool tooClose = false;
                float candidateMinDistSqr = float.MaxValue;
                for (int j = 0; j < reservedOffsets.Count; j++)
                {
                    Vector2 reserved = reservedOffsets[j];
                    float distSqr = (candidate - reserved).sqrMagnitude;
                    if (distSqr < candidateMinDistSqr)
                        candidateMinDistSqr = distSqr;

                    if (distSqr < minSepSqr)
                    {
                        tooClose = true;
                    }
                }

                if (candidateMinDistSqr > bestCandidateMinDistSqr)
                {
                    bestCandidate = candidate;
                    bestCandidateMinDistSqr = candidateMinDistSqr;
                }

                if (!tooClose)
                {
                    chosenOffset = candidate;
                    found = true;
                    break;
                }
            }

            if (!found)
                chosenOffset = bestCandidate;
        }

        if (reservedOffsets != null)
            reservedOffsets.Add(chosenOffset);

        return transform.position + new Vector3(chosenOffset.x, chosenOffset.y + yOffset, 0f);
    }

    private Vector2 RandomDropOffset(float radius)
    {
        float clampedRadius = Mathf.Max(0f, radius);
        if (clampedRadius <= 0f)
            return Vector2.zero;

        float innerRatio = Mathf.Clamp01(pickupDropInnerRadiusRatio);
        float innerRadius = clampedRadius * innerRatio;

        Vector2 dir = Random.insideUnitCircle;
        if (dir.sqrMagnitude <= 0.0001f)
            dir = Vector2.right;
        dir.Normalize();

        float dist = innerRadius < clampedRadius
            ? Random.Range(innerRadius, clampedRadius)
            : clampedRadius;
        return dir * dist;
    }

    private void TryDropHealthPickup(List<Vector2> reservedDropOffsets)
    {
        if (hpDropChance <= 0f || Random.value > hpDropChance)
            return;

        HealthPickup pickup = null;

        if (hpPickupPrefab != null)
        {
            pickup = Instantiate(hpPickupPrefab, ResolvePickupDropPosition(reservedDropOffsets), Quaternion.identity, pickupsRoot);
        }
        else if (xpPrefab != null)
        {
            XPPickup fallback = Instantiate(xpPrefab, ResolvePickupDropPosition(reservedDropOffsets), Quaternion.identity, pickupsRoot);
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
            GameFlowController.Instance.NotifyBossDefeated(this);
    }
}

