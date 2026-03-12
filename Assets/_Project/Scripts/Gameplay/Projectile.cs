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
    private int damage;
    private int pierceRemaining;
    private float knockbackMultiplier = 1f;
    private int onHitScatterCount;
    private float onHitScatterAngle;
    private bool scatterTriggered;
    private bool isReturningToPool;
    private int poolKey;
    private Projectile sourcePrefab;
    private HashSet<int> hitEnemyIds;

    public static Projectile Spawn(Projectile prefab, Vector3 position, Quaternion rotation, Transform parent)
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

        Projectile projectile = null;
        while (pool.Count > 0 && projectile == null)
            projectile = pool.Pop();

        if (projectile == null)
            projectile = Instantiate(source, position, rotation, parent);

        // Cap active projectiles to prevent lag spikes.
        if (activeCount >= MaxActiveProjectiles)
        {
            if (projectile != null && projectile != source)
            {
                pool.Push(projectile);
            }
            return null;
        }

        projectile.sourcePrefab = source;
        projectile.poolKey = key;
        projectile.isReturningToPool = false;
        projectile.transform.SetParent(parent, false);
        projectile.transform.SetPositionAndRotation(position, rotation);
        projectile.gameObject.SetActive(true);
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
        hitEnemyIds = new HashSet<int>();
        if (sourcePrefab == null)
            sourcePrefab = this;
    }

    private void OnDisable()
    {
        CancelInvoke();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
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
                Release();
                return;
            }
        }
    }

    public void Fire(Vector2 dir, float speed, int dmg)
    {
        Fire(dir, speed, dmg, 0, 1f, 0, 0f);
    }

    public void Fire(Vector2 dir, float speed, int dmg, int pierceCount, float kbMultiplier, int scatterCount, float scatterAngle)
    {
        damage = Mathf.Max(1, dmg);
        pierceRemaining = Mathf.Max(0, pierceCount);
        knockbackMultiplier = Mathf.Max(0f, kbMultiplier);
        onHitScatterCount = Mathf.Max(0, scatterCount);
        onHitScatterAngle = Mathf.Clamp(scatterAngle, 0f, 160f);
        scatterTriggered = false;
        isReturningToPool = false;

        if (hitEnemyIds == null)
            hitEnemyIds = new HashSet<int>();
        else
            hitEnemyIds.Clear();

        Vector2 velocity = dir.sqrMagnitude > 0.0001f ? dir.normalized * Mathf.Max(0.1f, speed) : Vector2.zero;
        rb.linearVelocity = velocity;

        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        CancelInvoke();
        Invoke(nameof(Expire), lifeSeconds);
    }

    private void Expire()
    {
        Release();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyController enemy = other.GetComponent<EnemyController>() ?? other.GetComponentInParent<EnemyController>();
        if (enemy == null)
            return;

        int id = enemy.GetInstanceID();
        if (hitEnemyIds.Contains(id))
            return;

        hitEnemyIds.Add(id);

        Vector2 hitDirection = rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f
            ? rb.linearVelocity.normalized
            : ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;

        enemy.TakeDamage(damage, hitDirection, knockbackMultiplier);

        if (!scatterTriggered && onHitScatterCount > 0)
            SpawnHitScatter(hitDirection, enemy);

        if (pierceRemaining > 0)
        {
            pierceRemaining -= 1;
            return;
        }

        Release();
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

            SpawnScatterProjectile(dirToEnemy.normalized, speed, splitDamage);
            spawned++;
        }

        float totalSpread = onHitScatterAngle * Mathf.Max(0, count - 1);
        float startAngle = -totalSpread * 0.5f;

        for (int i = spawned; i < count; i++)
        {
            float angle = count == 1 ? 0f : startAngle + onHitScatterAngle * i;
            Vector2 dir = Rotate(baseDirection, angle);
            SpawnScatterProjectile(dir, speed, splitDamage);
        }
    }

    private void SpawnScatterProjectile(Vector2 dir, float speed, int splitDamage)
    {
        Projectile split = Spawn(sourcePrefab != null ? sourcePrefab : this, transform.position, Quaternion.identity, transform.parent);
        if (split == null) return;
        split.Fire(
            dir,
            speed,
            splitDamage,
            0,
            knockbackMultiplier * 0.8f,
            0,
            onHitScatterAngle);
    }

    private void Release()
    {
        if (isReturningToPool)
            return;

        isReturningToPool = true;
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
