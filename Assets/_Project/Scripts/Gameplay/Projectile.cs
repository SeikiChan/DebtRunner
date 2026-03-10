using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifeSeconds = 2f;
    [SerializeField] private float scatterSeekRadius = 4.5f;
    [SerializeField] private float scatterDamageScale = 0.7f;

    private Rigidbody2D rb;
    private int damage;
    private int pierceRemaining;
    private float knockbackMultiplier = 1f;
    private int onHitScatterCount;
    private float onHitScatterAngle;
    private bool scatterTriggered;
    private HashSet<int> hitEnemyIds;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitEnemyIds = new HashSet<int>();
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
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy == null) return;

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

        Destroy(gameObject);
    }

    private void SpawnHitScatter(Vector2 baseDirection, EnemyController initialTarget)
    {
        scatterTriggered = true;
        int count = Mathf.Max(0, onHitScatterCount);
        if (count == 0) return;

        float speed = rb != null ? rb.linearVelocity.magnitude : 8f;
        int splitDamage = Mathf.Max(1, Mathf.RoundToInt(damage * scatterDamageScale));
        int spawned = 0;
        Collider2D[] nearbyHits = Physics2D.OverlapCircleAll(transform.position, Mathf.Max(0.5f, scatterSeekRadius));
        List<EnemyController> candidates = new List<EnemyController>();
        HashSet<int> seenIds = new HashSet<int>();

        for (int i = 0; i < nearbyHits.Length; i++)
        {
            EnemyController enemy = nearbyHits[i].GetComponent<EnemyController>();
            if (enemy == null || enemy == initialTarget || !enemy.isActiveAndEnabled)
                continue;
            if (!seenIds.Add(enemy.GetInstanceID()))
                continue;

            candidates.Add(enemy);
        }

        candidates.Sort((a, b) =>
        {
            float distA = (a.transform.position - transform.position).sqrMagnitude;
            float distB = (b.transform.position - transform.position).sqrMagnitude;
            return distA.CompareTo(distB);
        });

        for (int i = 0; i < candidates.Count && spawned < count; i++)
        {
            Vector2 dirToEnemy = (candidates[i].transform.position - transform.position);
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
        Projectile split = Instantiate(this, transform.position, Quaternion.identity, transform.parent);
        split.Fire(
            dir,
            speed,
            splitDamage,
            0,
            knockbackMultiplier * 0.8f,
            0,
            onHitScatterAngle);
    }

    private Vector2 Rotate(Vector2 value, float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
    }
}
