using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private PlayerMotor2D motor;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform projectilesRoot;

    [Header("Weapon (temp)")]
    [SerializeField] private float fireInterval = 0.25f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private int damage = 1;

    [Header("Optional")]
    [SerializeField] private Transform muzzle;

    private float timer;

    private float baseFireInterval;
    private float baseProjectileSpeed;
    private int baseDamage;

    private void Reset()
    {
        motor = GetComponent<PlayerMotor2D>();
    }

    private void Awake()
    {
        baseFireInterval = fireInterval;
        baseProjectileSpeed = projectileSpeed;
        baseDamage = damage;
    }

    private void OnEnable()
    {
        timer = 0f;
    }

    private void Update()
    {
        if (motor == null || projectilePrefab == null) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        EnemyController nearestEnemy = GetNearestEnemy();
        if (nearestEnemy == null) return;

        Vector2 dir = ((Vector2)nearestEnemy.transform.position - (Vector2)transform.position).normalized;

        timer = fireInterval;

        Vector3 spawnPos = muzzle ? muzzle.position : transform.position;
        var proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity, projectilesRoot);
        proj.Fire(dir, projectileSpeed, damage);
    }

    private EnemyController GetNearestEnemy()
    {
        EnemyController[] enemies = FindObjectsOfType<EnemyController>();
        if (enemies.Length == 0) return null;

        EnemyController nearest = enemies[0];
        float nearestDistance = Vector2.Distance(transform.position, nearest.transform.position);

        for (int i = 1; i < enemies.Length; i++)
        {
            float distance = Vector2.Distance(transform.position, enemies[i].transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = enemies[i];
            }
        }

        return nearest;
    }

    public void ApplyUpgrade(WeaponUpgrade upgrade)
    {
        if (upgrade == null) return;

        damage += upgrade.upgradePower;

        if (upgrade.upgradeFireRate > 0)
            fireInterval -= upgrade.upgradeFireRate;

        if (upgrade.upgradeSpeed > 0)
            projectileSpeed += upgrade.upgradeSpeed;

        fireInterval = Mathf.Max(0.05f, fireInterval);

        RunLogger.Event($"Weapon upgraded: damage={damage}, fireRate={1f / fireInterval:F2}/s, projectileSpeed={projectileSpeed:F2}");
    }

    public int GetDamage() => damage;

    public float GetFireRate() => 1f / fireInterval;
}
