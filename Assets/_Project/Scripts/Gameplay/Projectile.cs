using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifeSeconds = 1.5f;
    [SerializeField] private float scatterSeekRadius = 4.5f;
    [SerializeField] private float scatterDamageScale = 0.7f;

    private const int MaxActiveProjectiles = 200;
    private static int activeCount;
    private static readonly Dictionary<int, Stack<Projectile>> pooledProjectiles = new Dictionary<int, Stack<Projectile>>();
    private static readonly Collider2D[] scatterHitBuffer = new Collider2D[128];
    private static readonly List<EnemyController> scatterCandidates = new List<EnemyController>(32);
    private static readonly HashSet<int> scatterSeenIds = new HashSet<int>();

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private int damage;
    private int pierceRemaining;
    private float knockbackMultiplier = 1f;
    private int onHitScatterCount;
    private float onHitScatterAngle;
    private float launchSpeed;
    private float returnSpeed;
    private float returnSpeedMultiplier = 1f;
    private bool returnToOwnerEnabled;
    private bool isReturning;
    private Transform returnTarget;
    private bool scatterTriggered;
    private bool isReturningToPool;
    private bool isOrbiting;
    private float orbitHitCooldown;
    private int poolKey;
    private Projectile sourcePrefab;
    private HashSet<int> hitEnemyIds;
    private Dictionary<int, float> orbitLastHitAt;
    private readonly List<int> staleOrbitHitKeys = new List<int>(16);
    private Vector3 defaultLocalScale = Vector3.one;
    private Color defaultColor = Color.white;
    private string defaultSortingLayerName;
    private int defaultSortingOrder;
    private bool countsTowardSpawnCap = true;

    public static Projectile Spawn(Projectile prefab, Vector3 position, Quaternion rotation, Transform parent, bool countTowardCap = true)
    {
        if (prefab == null)
            return null;

        Projectile source = prefab.sourcePrefab != null ? prefab.sourcePrefab : prefab;
        int key = source.GetInstanceID();
        if (!pooledProjectiles.TryGetValue(key, out Stack<Projectile> pool))
        {
            pool = new Stack<Projectile>();
            pooledProjectiles[key] = pool;
        }

        // Refuse new spawns before reusing/instantiating anything, otherwise a freshly
        // instantiated projectile can remain visible in-scene when we early-out at cap.
        if (countTowardCap && activeCount >= MaxActiveProjectiles)
            return null;

        Projectile projectile = null;
        while (pool.Count > 0 && projectile == null)
            projectile = pool.Pop();

        if (projectile == null)
            projectile = Instantiate(source, position, rotation, parent);

        projectile.sourcePrefab = source;
        projectile.poolKey = key;
        projectile.isReturningToPool = false;
        projectile.countsTowardSpawnCap = countTowardCap;
        projectile.transform.SetParent(parent, false);
        projectile.transform.SetPositionAndRotation(position, rotation);
        projectile.gameObject.SetActive(true);
        if (countTowardCap)
            activeCount++;
        return projectile;
    }

    public static void ResetPool()
    {
        foreach (KeyValuePair<int, Stack<Projectile>> pair in pooledProjectiles)
        {
            Stack<Projectile> pool = pair.Value;
            if (pool == null)
                continue;

            while (pool.Count > 0)
            {
                Projectile projectile = pool.Pop();
                if (projectile != null)
                    Destroy(projectile.gameObject);
            }
        }

        pooledProjectiles.Clear();
        activeCount = 0;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        hitEnemyIds = new HashSet<int>();
        orbitLastHitAt = new Dictionary<int, float>();
        if (sourcePrefab == null)
            sourcePrefab = this;
        defaultLocalScale = transform.localScale;
        if (spriteRenderer != null)
        {
            defaultColor = spriteRenderer.color;
            defaultSortingLayerName = spriteRenderer.sortingLayerName;
            defaultSortingOrder = spriteRenderer.sortingOrder;
        }
    }

    private void OnDisable()
    {
        CancelInvoke();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        returnTarget = null;
        returnToOwnerEnabled = false;
        isReturning = false;
        isOrbiting = false;
        orbitHitCooldown = 0f;
        orbitLastHitAt?.Clear();
        countsTowardSpawnCap = true;
        RestoreDefaultPresentation();
    }

    private void FixedUpdate()
    {
        if (isOrbiting)
            return;

        // Destroy projectile when it exits the play area boundary.
        if (CircleBoundary.Instance != null)
        {
            Vector2 pos = rb != null ? rb.position : (Vector2)transform.position;
            Vector2 center = CircleBoundary.Instance.Center;
            float rx = CircleBoundary.Instance.RadiusX;
            float ry = CircleBoundary.Instance.RadiusY;
            Vector2 offset = pos - center;
            float nx = offset.x / rx;
            float ny = offset.y / ry;
            if (nx * nx + ny * ny > 1f)
            {
                if (TryBeginReturn())
                    return;

                Release();
                return;
            }
        }

        if (isReturning && returnTarget == null)
        {
            Release();
            return;
        }

        if (isReturning && returnTarget != null && rb != null)
        {
            Vector2 toOwner = (Vector2)returnTarget.position - rb.position;
            float catchDistance = Mathf.Max(
                0.12f,
                Mathf.Max(0.02f, returnSpeed) * Time.fixedDeltaTime * 1.2f);
            if (toOwner.sqrMagnitude <= catchDistance * catchDistance)
            {
                Release();
                return;
            }

            SetVelocity(toOwner.normalized, returnSpeed);
        }
    }

    public void Fire(Vector2 dir, float speed, int dmg)
    {
        Fire(dir, speed, dmg, 0, 1f, 0, 0f, null, false, 1f);
    }

    public void Fire(
        Vector2 dir,
        float speed,
        int dmg,
        int pierceCount,
        float kbMultiplier,
        int scatterCount,
        float scatterAngle,
        Transform owner,
        bool enableReturn,
        float returnSpeedMul)
    {
        damage = Mathf.Max(1, dmg);
        pierceRemaining = Mathf.Max(0, pierceCount);
        knockbackMultiplier = Mathf.Max(0f, kbMultiplier);
        onHitScatterCount = Mathf.Max(0, scatterCount);
        onHitScatterAngle = Mathf.Clamp(scatterAngle, 0f, 160f);
        launchSpeed = Mathf.Max(0.1f, speed);
        returnSpeedMultiplier = Mathf.Max(1f, returnSpeedMul);
        returnSpeed = launchSpeed * returnSpeedMultiplier;
        returnTarget = owner;
        returnToOwnerEnabled = enableReturn && owner != null;
        isReturning = false;
        scatterTriggered = false;
        isReturningToPool = false;
        isOrbiting = false;
        orbitHitCooldown = 0f;
        RestoreDefaultPresentation();
        orbitLastHitAt?.Clear();

        if (hitEnemyIds == null)
            hitEnemyIds = new HashSet<int>();
        else
            hitEnemyIds.Clear();

        SetVelocity(dir, launchSpeed);

        CancelInvoke();
        Invoke(nameof(Expire), lifeSeconds);
    }

    public void BeginOrbit(int dmg, float kbMultiplier, int scatterCount, float scatterAngle, float hitCooldown)
    {
        ConfigureOrbitProfile(dmg, kbMultiplier, scatterCount, scatterAngle, hitCooldown);
        launchSpeed = 0f;
        returnSpeed = 0f;
        returnSpeedMultiplier = 1f;
        returnTarget = null;
        returnToOwnerEnabled = false;
        isReturning = false;
        scatterTriggered = false;
        isReturningToPool = false;

        if (hitEnemyIds == null)
            hitEnemyIds = new HashSet<int>();
        else
            hitEnemyIds.Clear();

        if (orbitLastHitAt == null)
            orbitLastHitAt = new Dictionary<int, float>();
        else
            orbitLastHitAt.Clear();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        CancelInvoke();
    }

    public void ConfigureOrbitProfile(int dmg, float kbMultiplier, int scatterCount, float scatterAngle, float hitCooldown)
    {
        damage = Mathf.Max(1, dmg);
        pierceRemaining = 0;
        knockbackMultiplier = Mathf.Max(0f, kbMultiplier);
        onHitScatterCount = Mathf.Max(0, scatterCount);
        onHitScatterAngle = Mathf.Clamp(scatterAngle, 0f, 160f);
        isOrbiting = true;
        orbitHitCooldown = Mathf.Max(0.01f, hitCooldown);
    }

    public void UpdateOrbitPose(Vector2 worldPosition, Vector2 worldVelocity, float rotationDegrees, Vector3 localScale, Color tint)
    {
        if (!isOrbiting)
            return;

        if (rb != null)
        {
            rb.linearVelocity = worldVelocity;
            rb.MovePosition(worldPosition);
        }
        else
        {
            transform.position = worldPosition;
        }

        Vector2 facingVelocity = worldVelocity.sqrMagnitude > 0.0001f
            ? worldVelocity
            : Rotate(Vector2.up, rotationDegrees);
        if (facingVelocity.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(facingVelocity.y, facingVelocity.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        else
        {
            transform.rotation = Quaternion.AngleAxis(rotationDegrees, Vector3.forward);
        }

        transform.localScale = localScale;
        if (spriteRenderer != null)
            spriteRenderer.color = tint;
    }

    public void Despawn()
    {
        Release();
    }

    private void Expire()
    {
        if (TryBeginReturn())
            return;

        Release();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyController enemy = other.GetComponent<EnemyController>() ?? other.GetComponentInParent<EnemyController>();
        if (enemy == null)
            return;

        int id = enemy.GetInstanceID();
        if (isOrbiting)
        {
            if (orbitLastHitAt == null)
                orbitLastHitAt = new Dictionary<int, float>();

            float now = Time.time;
            if (orbitLastHitAt.TryGetValue(id, out float lastHitAt) && now - lastHitAt < orbitHitCooldown)
                return;

            orbitLastHitAt[id] = now;
            if (orbitLastHitAt.Count > 64)
                PruneOrbitHitHistory(now);
        }
        else
        {
            if (hitEnemyIds.Contains(id))
                return;

            hitEnemyIds.Add(id);
        }

        Vector2 hitDirection = rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f
            ? rb.linearVelocity.normalized
            : ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;

        ApplyHit(enemy, hitDirection);

        if (isOrbiting)
            return;

        if (pierceRemaining > 0)
        {
            pierceRemaining -= 1;
            return;
        }

        if (TryBeginReturn())
            return;

        Release();
    }

    private void ApplyHit(EnemyController enemy, Vector2 hitDirection)
    {
        enemy.TakeDamage(damage, hitDirection, knockbackMultiplier);

        if ((!scatterTriggered || isOrbiting) && onHitScatterCount > 0)
        {
            if (!isOrbiting)
                scatterTriggered = true;

            SpawnHitScatter(hitDirection, enemy);
        }
    }

    private void SpawnHitScatter(Vector2 baseDirection, EnemyController initialTarget)
    {
        scatterTriggered = true;
        int count = Mathf.Max(0, onHitScatterCount);
        if (count == 0)
            return;

        float speed = rb != null ? rb.linearVelocity.magnitude : 8f;
        int splitDamage = Mathf.Max(1, Mathf.RoundToInt(damage * scatterDamageScale));
        int spawned = 0;
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, Mathf.Max(0.5f, scatterSeekRadius), scatterHitBuffer);
        scatterCandidates.Clear();
        scatterSeenIds.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = scatterHitBuffer[i];
            EnemyController enemy = hit != null
                ? hit.GetComponent<EnemyController>() ?? hit.GetComponentInParent<EnemyController>()
                : null;
            if (enemy == null || enemy == initialTarget || !enemy.isActiveAndEnabled)
                continue;
            if (!scatterSeenIds.Add(enemy.GetInstanceID()))
                continue;

            scatterCandidates.Add(enemy);
        }

        scatterCandidates.Sort((a, b) =>
        {
            float distA = (a.transform.position - transform.position).sqrMagnitude;
            float distB = (b.transform.position - transform.position).sqrMagnitude;
            return distA.CompareTo(distB);
        });

        for (int i = 0; i < scatterCandidates.Count && spawned < count; i++)
        {
            Vector2 dirToEnemy = scatterCandidates[i].transform.position - transform.position;
            if (dirToEnemy.sqrMagnitude <= 0.0001f)
                continue;

            SpawnScatterProjectile(dirToEnemy.normalized, speed, splitDamage, initialTarget);
            spawned++;
        }

        float totalSpread = onHitScatterAngle * Mathf.Max(0, count - 1);
        float startAngle = -totalSpread * 0.5f;

        for (int i = spawned; i < count; i++)
        {
            float angle = count == 1 ? 0f : startAngle + onHitScatterAngle * i;
            Vector2 dir = Rotate(baseDirection, angle);
            SpawnScatterProjectile(dir, speed, splitDamage, initialTarget);
        }
    }

    private void SpawnScatterProjectile(Vector2 dir, float speed, int splitDamage, EnemyController ignoredEnemy)
    {
        Vector2 spawnPosition = ResolveScatterSpawnPosition(dir, ignoredEnemy);
        Projectile split = Spawn(
            sourcePrefab != null ? sourcePrefab : this,
            spawnPosition,
            Quaternion.identity,
            transform.parent);
        if (split == null)
            return;

        split.Fire(
            dir,
            speed,
            splitDamage,
            0,
            knockbackMultiplier * 0.8f,
            0,
            onHitScatterAngle,
            null,
            false,
            1f);
        split.IgnoreEnemy(ignoredEnemy);
    }

    private Vector2 ResolveScatterSpawnPosition(Vector2 dir, EnemyController ignoredEnemy)
    {
        Vector2 normalizedDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.up;
        Vector2 origin = transform.position;

        if (ignoredEnemy != null && ignoredEnemy.TargetCollider != null)
        {
            Vector2 colliderEdge = ignoredEnemy.TargetCollider.ClosestPoint(origin - (normalizedDir * 0.35f));
            if ((colliderEdge - origin).sqrMagnitude > 0.000001f)
                origin = colliderEdge;
        }

        return origin + (normalizedDir * 0.18f);
    }

    private void IgnoreEnemy(EnemyController enemy)
    {
        if (enemy == null)
            return;

        if (hitEnemyIds == null)
            hitEnemyIds = new HashSet<int>();

        hitEnemyIds.Add(enemy.GetInstanceID());
    }

    private bool TryBeginReturn()
    {
        if (!returnToOwnerEnabled || isReturning || returnTarget == null)
            return false;

        isReturning = true;
        returnSpeed = Mathf.Max(launchSpeed, launchSpeed * returnSpeedMultiplier);
        CancelInvoke();
        return true;
    }

    private void SetVelocity(Vector2 direction, float speed)
    {
        if (rb == null)
            return;

        Vector2 velocity = direction.sqrMagnitude > 0.0001f
            ? direction.normalized * Mathf.Max(0.1f, speed)
            : Vector2.zero;
        rb.linearVelocity = velocity;

        if (velocity.sqrMagnitude <= 0.0001f)
            return;

        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void PruneOrbitHitHistory(float now)
    {
        if (orbitLastHitAt == null || orbitLastHitAt.Count == 0)
            return;

        staleOrbitHitKeys.Clear();
        foreach (KeyValuePair<int, float> kvp in orbitLastHitAt)
        {
            if (now - kvp.Value > Mathf.Max(orbitHitCooldown, 1f))
                staleOrbitHitKeys.Add(kvp.Key);
        }

        for (int i = 0; i < staleOrbitHitKeys.Count; i++)
            orbitLastHitAt.Remove(staleOrbitHitKeys[i]);
    }

    private void RestoreDefaultPresentation()
    {
        transform.localScale = defaultLocalScale;
        if (spriteRenderer == null)
            return;

        spriteRenderer.color = defaultColor;
        spriteRenderer.sortingLayerName = defaultSortingLayerName;
        spriteRenderer.sortingOrder = defaultSortingOrder;
    }

    private void Release()
    {
        if (isReturningToPool)
            return;

        isReturningToPool = true;
        if (countsTowardSpawnCap)
            activeCount = Mathf.Max(0, activeCount - 1);
        CancelInvoke();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        if (hitEnemyIds != null)
            hitEnemyIds.Clear();

        transform.SetParent(null, false);
        gameObject.SetActive(false);

        if (poolKey == 0)
        {
            Destroy(gameObject);
            return;
        }

        if (!pooledProjectiles.TryGetValue(poolKey, out Stack<Projectile> pool))
        {
            pool = new Stack<Projectile>();
            pooledProjectiles[poolKey] = pool;
        }

        pool.Push(this);
    }

    private Vector2 Rotate(Vector2 value, float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
    }
}
