using System.Collections.Generic;
using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private PlayerMotor2D motor;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform projectilesRoot;

    [Header("Weapon (base)")]
    [SerializeField] private float fireInterval = 0.25f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private int damage = 1;

    [Header("Spread / Multi-shot")]
    [SerializeField] private int extraProjectiles = 0;
    [SerializeField] private float spreadAngleStep = 8f;

    [Header("Projectile Traits")]
    [SerializeField] private int pierceCount = 0;
    [SerializeField] private float knockbackMultiplier = 1f;
    [SerializeField] private bool enableEnemyKnockback = true;
    [SerializeField, Min(0f)] private float maxKnockbackMultiplier = 0.6f;
    [SerializeField] private int onHitScatterCount = 0;
    [SerializeField] private float onHitScatterAngle = 18f;

    [Header("Orbiting Projectiles")]
    [SerializeField] private int orbitProjectileCount = 0;
    [SerializeField] private float orbitRadius = 1.6f;
    [SerializeField] private float orbitAngularSpeed = 140f;
    [SerializeField] private float orbitHitRadius = 0.35f;
    [SerializeField] private float orbitHitCooldown = 0.2f;
    [SerializeField] private float orbitDamageScale = 0.65f;

    [Header("Optional")]
    [SerializeField] private Transform muzzle;

    private float timer;
    private float orbitSpinAngle;
    private readonly Dictionary<int, float> orbitLastHitAt = new Dictionary<int, float>();

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

        HandleOrbitingProjectiles();

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        EnemyController nearestEnemy = GetNearestEnemy();
        if (nearestEnemy == null) return;

        Vector2 dir = ((Vector2)nearestEnemy.transform.position - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude <= 0.0001f)
            return;

        timer = fireInterval;
        FireSpread(dir);
    }

    private EnemyController GetNearestEnemy()
    {
        EnemyController[] enemies = FindObjectsOfType<EnemyController>();
        if (enemies == null || enemies.Length == 0) return null;

        EnemyController nearest = null;
        float nearestSqrDistance = float.MaxValue;
        Vector2 selfPos = transform.position;

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyController candidate = enemies[i];
            if (!IsValidTarget(candidate))
                continue;

            Vector2 delta = (Vector2)candidate.transform.position - selfPos;
            float sqrDistance = delta.sqrMagnitude;
            if (sqrDistance <= 0.0001f)
                continue;

            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private bool IsValidTarget(EnemyController enemy)
    {
        if (enemy == null || !enemy.isActiveAndEnabled)
            return false;

        // Filter out container objects or misconfigured scene nodes accidentally carrying EnemyController.
        if (enemy.GetComponent<Rigidbody2D>() == null)
            return false;
        if (enemy.GetComponent<Collider2D>() == null)
            return false;

        return true;
    }

    private void FireSpread(Vector2 baseDirection)
    {
        int shotCount = Mathf.Max(1, 1 + extraProjectiles);
        float totalSpread = spreadAngleStep * (shotCount - 1);
        float startAngle = -totalSpread * 0.5f;

        Vector3 spawnPos = muzzle ? muzzle.position : transform.position;
        for (int i = 0; i < shotCount; i++)
        {
            float angleOffset = shotCount == 1 ? 0f : startAngle + (spreadAngleStep * i);
            Vector2 shotDirection = Rotate(baseDirection, angleOffset);

            Projectile proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity, projectilesRoot);
            proj.Fire(
                shotDirection,
                projectileSpeed,
                damage,
                pierceCount,
                GetAppliedKnockbackMultiplier(),
                onHitScatterCount,
                onHitScatterAngle);
        }
    }

    private Vector2 Rotate(Vector2 value, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
    }

    private void HandleOrbitingProjectiles()
    {
        if (orbitProjectileCount <= 0 || orbitRadius <= 0f)
            return;

        orbitSpinAngle += orbitAngularSpeed * Time.deltaTime;
        float now = Time.time;
        int orbitDamage = Mathf.Max(1, Mathf.RoundToInt(damage * orbitDamageScale));

        for (int i = 0; i < orbitProjectileCount; i++)
        {
            float angle = orbitSpinAngle + (360f / orbitProjectileCount) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;
            Vector2 orbitPos = (Vector2)transform.position + offset;

            Collider2D[] hits = Physics2D.OverlapCircleAll(orbitPos, orbitHitRadius);
            for (int h = 0; h < hits.Length; h++)
            {
                EnemyController enemy = hits[h].GetComponent<EnemyController>();
                if (enemy == null) continue;

                int id = enemy.GetInstanceID();
                if (orbitLastHitAt.TryGetValue(id, out float lastTime) && now - lastTime < orbitHitCooldown)
                    continue;

                orbitLastHitAt[id] = now;
                enemy.TakeDamage(orbitDamage, offset.normalized, GetAppliedKnockbackMultiplier(0.65f));
            }
        }

        if (orbitLastHitAt.Count > 64)
        {
            List<int> keys = new List<int>(orbitLastHitAt.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                if (now - orbitLastHitAt[keys[i]] > 5f)
                    orbitLastHitAt.Remove(keys[i]);
            }
        }
    }

    public void ResetRuntimeStats()
    {
        damage = baseDamage;
        projectileSpeed = baseProjectileSpeed;
        fireInterval = baseFireInterval;

        extraProjectiles = 0;
        spreadAngleStep = 8f;
        pierceCount = 0;
        knockbackMultiplier = 1f;
        onHitScatterCount = 0;
        onHitScatterAngle = 18f;

        orbitProjectileCount = 0;
        orbitRadius = 1.6f;
        orbitAngularSpeed = 140f;
        orbitLastHitAt.Clear();
    }

    public void ApplyUpgrade(WeaponUpgrade upgrade)
    {
        if (upgrade == null) return;

        upgrade.ConvertLegacyStatsToEffects();
        if (upgrade.effects == null || upgrade.effects.Count == 0)
            return;

        for (int i = 0; i < upgrade.effects.Count; i++)
        {
            WeaponUpgradeEffect effect = upgrade.effects[i];
            if (effect == null) continue;

            switch (effect.effectType)
            {
                case WeaponUpgradeEffectType.DamageAdd:
                    damage += effect.intValue;
                    break;
                case WeaponUpgradeEffectType.FireRateAdd:
                    fireInterval -= effect.floatValue;
                    break;
                case WeaponUpgradeEffectType.ProjectileSpeedAdd:
                    projectileSpeed += effect.floatValue;
                    break;
                case WeaponUpgradeEffectType.ExtraProjectilesAdd:
                    extraProjectiles += effect.intValue;
                    break;
                case WeaponUpgradeEffectType.SpreadAngleAdd:
                    spreadAngleStep += effect.floatValue;
                    break;
                case WeaponUpgradeEffectType.PierceAdd:
                    pierceCount += effect.intValue;
                    break;
                case WeaponUpgradeEffectType.KnockbackMultiplierAdd:
                    knockbackMultiplier += effect.floatValue;
                    break;
                case WeaponUpgradeEffectType.OnHitScatterCountAdd:
                    onHitScatterCount += effect.intValue;
                    break;
                case WeaponUpgradeEffectType.OnHitScatterAngleAdd:
                    onHitScatterAngle += effect.floatValue;
                    break;
                case WeaponUpgradeEffectType.OrbitProjectileCountAdd:
                    orbitProjectileCount += effect.intValue;
                    break;
                case WeaponUpgradeEffectType.OrbitRadiusAdd:
                    orbitRadius += effect.floatValue;
                    break;
                case WeaponUpgradeEffectType.OrbitAngularSpeedAdd:
                    orbitAngularSpeed += effect.floatValue;
                    break;
            }
        }

        damage = Mathf.Max(1, damage);
        projectileSpeed = Mathf.Max(1f, projectileSpeed);
        fireInterval = Mathf.Max(0.05f, fireInterval);
        extraProjectiles = Mathf.Max(0, extraProjectiles);
        spreadAngleStep = Mathf.Clamp(spreadAngleStep, 0f, 35f);
        pierceCount = Mathf.Max(0, pierceCount);
        knockbackMultiplier = Mathf.Max(0f, knockbackMultiplier);
        onHitScatterCount = Mathf.Max(0, onHitScatterCount);
        onHitScatterAngle = Mathf.Clamp(onHitScatterAngle, 0f, 120f);
        orbitProjectileCount = Mathf.Clamp(orbitProjectileCount, 0, 12);
        orbitRadius = Mathf.Clamp(orbitRadius, 0.4f, 4f);

        RunLogger.Event(
            $"Weapon upgraded: dmg={damage}, rate={1f / fireInterval:F2}/s, speed={projectileSpeed:F1}, " +
            $"multi={1 + extraProjectiles}, pierce={pierceCount}, scatter={onHitScatterCount}, orbit={orbitProjectileCount}");
    }

    public int GetDamage() => damage;
    public float GetFireRate() => 1f / fireInterval;

    private float GetAppliedKnockbackMultiplier(float extraScale = 1f)
    {
        if (!enableEnemyKnockback)
            return 0f;

        float cap = Mathf.Max(0f, maxKnockbackMultiplier);
        float clampedMultiplier = Mathf.Min(Mathf.Max(0f, knockbackMultiplier), cap);
        return clampedMultiplier * Mathf.Max(0f, extraScale);
    }

    private void OnDrawGizmosSelected()
    {
        if (orbitProjectileCount <= 0 || orbitRadius <= 0f) return;

        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.65f);
        for (int i = 0; i < orbitProjectileCount; i++)
        {
            float angle = (360f / Mathf.Max(1, orbitProjectileCount)) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 pos = transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;
            Gizmos.DrawWireSphere(pos, orbitHitRadius);
        }
    }
}
