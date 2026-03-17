using System.Collections.Generic;
using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    private const float DefaultOrbitVisualSizeMultiplier = 3f;

    [SerializeField] private PlayerMotor2D motor;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform projectilesRoot;

    [Header("SFX / 音效")]
    [LocalizedLabel("射击音效")]
    [SerializeField] private AudioClip sfxShoot;

    [Header("Weapon (base)")]
    [SerializeField] private float fireInterval = 0.30f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private int damage = 10;

    [Header("Spread / Multi-shot")]
    [SerializeField] private int extraProjectiles = 0;
    [SerializeField] private float spreadAngleStep = 8f;

    [Header("Projectile Traits")]
    [SerializeField] private int pierceCount = 0;
    [SerializeField] private float knockbackMultiplier = 1f;
    [SerializeField] private bool enableEnemyKnockback = true;
    [SerializeField] private int onHitScatterCount = 0;
    [SerializeField] private float onHitScatterAngle = 18f;

    [Header("Auto Aim")]
    [SerializeField, Min(0f)] private float autoAimMaxDistance = 8.5f;
    [SerializeField] private bool autoAimRequireOnScreen = true;
    [SerializeField, Range(0f, 0.25f)] private float autoAimViewportPadding = 0.02f;

    [Header("Orbiting Projectiles")]
    [SerializeField] private int orbitProjectileCount = 0;
    [SerializeField] private float orbitRadius = 3.1f;
    [SerializeField] private float orbitAngularSpeed = 140f;
    [SerializeField] private float orbitHitCooldown = 0.2f;
    [SerializeField] private float orbitDamageScale = 0.65f;
    [SerializeField] private float orbitVisualScale = 1f;
    [SerializeField] private float orbitVisualSizeMultiplier = DefaultOrbitVisualSizeMultiplier;
    [SerializeField, Min(0f)] private float orbitRadiusPadding = 0.32f;
    [SerializeField] private Color orbitVisualTint = new Color(1f, 1f, 1f, 0.95f);

    [Header("Sweep Burst")]
    [SerializeField] private int novaProjectileCount = 0;
    [SerializeField] private float novaBurstInterval = 2.2f;
    [SerializeField] private float novaDamageScale = 0.75f;
    [SerializeField] private float novaProjectileSpeedScale = 0.85f;

    [Header("Return Flight")]
    [SerializeField] private bool returnProjectilesEnabled = false;
    [SerializeField, Min(1f)] private float returnSpeedMultiplier = 1.35f;

    [Header("Optional")]
    [SerializeField] private Transform muzzle;
    [Header("Shot Origin")]
    [SerializeField] private bool fireFromCharacterEdge = true;
    [SerializeField] private bool useColliderBasedSpawnDistance = true;
    [SerializeField, Min(0f)] private float projectileSpawnDistance = 0.48f;
    [SerializeField, Min(0f)] private float projectileSpawnPadding = 0.06f;

    private float timer;
    private float orbitSpinAngle;
    private float novaTimer;
    private readonly List<Projectile> orbitProjectiles = new List<Projectile>();
    private Collider2D[] selfColliders;
    private SpriteRenderer[] selfSpriteRenderers;

    private float baseFireInterval;
    private float baseProjectileSpeed;
    private int baseDamage;
    private float baseKnockbackMultiplier;
    private float baseOrbitRadius;
    private float baseOrbitAngularSpeed;
    private float baseOrbitVisualScale;
    private float baseOrbitVisualSizeMultiplier;
    private float baseNovaBurstInterval;
    private bool baseReturnProjectilesEnabled;
    private float baseReturnSpeedMultiplier;
    private Vector3 orbitVisualBaseScale = Vector3.one;
    private float orbitVisualExtent = 0.15f;

    private void Reset()
    {
        motor = GetComponent<PlayerMotor2D>();
    }

    private void Awake()
    {
        baseFireInterval = fireInterval;
        baseProjectileSpeed = projectileSpeed;
        baseDamage = damage;
        baseKnockbackMultiplier = Mathf.Max(0f, knockbackMultiplier);
        baseOrbitRadius = orbitRadius;
        baseOrbitAngularSpeed = orbitAngularSpeed;
        baseOrbitVisualScale = orbitVisualScale;
        baseOrbitVisualSizeMultiplier = GetResolvedOrbitVisualSizeMultiplier();
        baseNovaBurstInterval = novaBurstInterval;
        baseReturnProjectilesEnabled = returnProjectilesEnabled;
        baseReturnSpeedMultiplier = Mathf.Max(1f, returnSpeedMultiplier);
        selfColliders = GetComponentsInChildren<Collider2D>(true);
        selfSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        CacheProjectileVisualTemplate();
    }

    private void OnEnable()
    {
        timer = 0f;
        novaTimer = Mathf.Max(0.25f, novaBurstInterval);
        SyncOrbitProjectiles();
    }

    private void OnDisable()
    {
        ClearOrbitProjectiles();
    }

    private void OnDestroy()
    {
        ClearOrbitProjectiles();
    }

    private void Update()
    {
        if (motor == null || projectilePrefab == null) return;

        EnemyController targetEnemy = GetAutoAimTarget();
        HandleNovaBurst(targetEnemy);

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        if (targetEnemy == null) return;

        Vector2 dir = ((Vector2)targetEnemy.transform.position - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude <= 0.0001f)
            return;

        timer = fireInterval;
        FireSpread(dir);
    }

    private void FixedUpdate()
    {
        if (motor == null || projectilePrefab == null)
            return;

        HandleOrbitingProjectiles();
    }

    private EnemyController GetAutoAimTarget()
    {
        IReadOnlyList<EnemyController> enemies = EnemyController.ActiveEnemies;
        if (enemies == null || enemies.Count == 0) return null;

        EnemyController bestTarget = null;
        float bestHp = float.MaxValue;
        float bestSqrDistance = float.MaxValue;
        Vector2 selfPos = transform.position;
        float maxDistance = Mathf.Max(0f, autoAimMaxDistance);
        float maxDistanceSqr = maxDistance <= 0f ? float.PositiveInfinity : maxDistance * maxDistance;
        Camera mainCam = autoAimRequireOnScreen ? Camera.main : null;
        float hpEpsilon = 0.0001f;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyController candidate = enemies[i];
            if (!IsValidTarget(candidate))
                continue;

            Vector2 delta = (Vector2)candidate.transform.position - selfPos;
            float sqrDistance = delta.sqrMagnitude;
            if (sqrDistance <= 0.0001f)
                continue;
            if (sqrDistance > maxDistanceSqr)
                continue;
            if (autoAimRequireOnScreen && !IsTargetOnScreen(candidate.transform.position, mainCam))
                continue;

            float hp = Mathf.Max(0f, candidate.CurrentHP);
            bool betterByHp = hp < bestHp - hpEpsilon;
            bool sameHp = Mathf.Abs(hp - bestHp) <= hpEpsilon;
            bool betterByDistance = sqrDistance < bestSqrDistance;

            if (bestTarget == null || betterByHp || (sameHp && betterByDistance))
            {
                bestHp = hp;
                bestSqrDistance = sqrDistance;
                bestTarget = candidate;
            }
        }

        return bestTarget;
    }

    private bool IsValidTarget(EnemyController enemy)
    {
        if (enemy == null || !enemy.isActiveAndEnabled)
            return false;

        // Filter out container objects or misconfigured scene nodes accidentally carrying EnemyController.
        if (enemy.Rb == null)
            return false;
        if (enemy.TargetCollider == null || !enemy.TargetCollider.enabled)
            return false;

        return true;
    }

    private bool IsTargetOnScreen(Vector3 worldPos, Camera cam)
    {
        if (cam == null)
            return true;

        Vector3 viewport = cam.WorldToViewportPoint(worldPos);
        if (viewport.z <= 0f)
            return false;

        float padding = Mathf.Clamp01(autoAimViewportPadding);
        return viewport.x >= padding &&
               viewport.x <= 1f - padding &&
               viewport.y >= padding &&
               viewport.y <= 1f - padding;
    }

    private void FireSpread(Vector2 baseDirection)
    {
        int shotCount = Mathf.Max(1, 1 + extraProjectiles);
        if ((shotCount % 2) == 1)
        {
            // Odd counts keep a visible center lane, then expand outward in pairs.
            FireSingleShot(baseDirection, 0f);

            for (int ring = 1; ring <= shotCount / 2; ring++)
            {
                float angleOffset = spreadAngleStep * ring;
                FireSingleShot(baseDirection, -angleOffset);
                FireSingleShot(baseDirection, angleOffset);
            }
        }
        else
        {
            // Even counts split around the center so the first multi-shot upgrade reads clearly.
            for (int pair = 0; pair < shotCount / 2; pair++)
            {
                float angleOffset = spreadAngleStep * (pair + 0.5f);
                FireSingleShot(baseDirection, -angleOffset);
                FireSingleShot(baseDirection, angleOffset);
            }
        }

        if (sfxShoot != null && SFXManager.Instance != null)
            SFXManager.Instance.Play(sfxShoot, 0.5f);
    }

    private void FireSingleShot(Vector2 baseDirection, float angleOffset)
    {
        Vector2 shotDirection = Rotate(baseDirection, angleOffset);
        Vector3 spawnPos = ResolveProjectileSpawnPosition(shotDirection);

        Projectile proj = Projectile.Spawn(projectilePrefab, spawnPos, Quaternion.identity, projectilesRoot);
        if (proj == null) return;

        proj.Fire(
            shotDirection,
            projectileSpeed,
            damage,
            pierceCount,
            GetAppliedKnockbackMultiplier(),
            onHitScatterCount,
            onHitScatterAngle,
            transform,
            returnProjectilesEnabled,
            returnSpeedMultiplier);
    }

    private Vector2 Rotate(Vector2 value, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
    }

    private Vector3 ResolveProjectileSpawnPosition(Vector2 shotDirection)
    {
        if (muzzle != null)
            return muzzle.position;

        Vector2 dir = shotDirection.sqrMagnitude > 0.0001f ? shotDirection.normalized : Vector2.up;
        if (!fireFromCharacterEdge)
            return transform.position + (Vector3)(dir * Mathf.Max(0f, projectileSpawnDistance));

        float spawnDistance = Mathf.Max(0f, projectileSpawnDistance);
        if (useColliderBasedSpawnDistance)
        {
            float colliderEdge = EstimateColliderEdgeDistance(dir);
            spawnDistance = Mathf.Max(spawnDistance, colliderEdge + Mathf.Max(0f, projectileSpawnPadding));
        }

        return transform.position + (Vector3)(dir * spawnDistance);
    }

    private float EstimateColliderEdgeDistance(Vector2 direction)
    {
        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
        float ax = Mathf.Abs(dir.x);
        float ay = Mathf.Abs(dir.y);
        Vector2 origin = transform.position;
        float best = 0f;

        if (selfColliders != null)
        {
            for (int i = 0; i < selfColliders.Length; i++)
            {
                Collider2D col = selfColliders[i];
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
                    continue;

                float projected = EstimateProjectedSupportFromBounds(col.bounds, origin, dir, ax, ay);
                if (projected > best)
                    best = projected;
            }
        }

        if (selfSpriteRenderers != null)
        {
            for (int i = 0; i < selfSpriteRenderers.Length; i++)
            {
                SpriteRenderer sr = selfSpriteRenderers[i];
                if (sr == null || !sr.enabled || !sr.gameObject.activeInHierarchy)
                    continue;

                float projected = EstimateProjectedSupportFromBounds(sr.bounds, origin, dir, ax, ay);
                if (projected > best)
                    best = projected;
            }
        }

        return best;
    }

    private float EstimateProjectedSupportFromBounds(Bounds bounds, Vector2 origin, Vector2 dir, float absDirX, float absDirY)
    {
        Vector2 toCenter = (Vector2)bounds.center - origin;
        float centerProjection = Vector2.Dot(toCenter, dir);
        Vector3 ext = bounds.extents;
        float halfProjection = (ext.x * absDirX) + (ext.y * absDirY);
        return Mathf.Max(0f, centerProjection + halfProjection);
    }

    private void HandleOrbitingProjectiles()
    {
        if (orbitProjectileCount <= 0)
        {
            ClearOrbitProjectiles();
            return;
        }

        SyncOrbitProjectiles();
        if (orbitProjectiles.Count == 0)
            return;

        orbitSpinAngle = Mathf.Repeat(orbitSpinAngle + orbitAngularSpeed * Time.fixedDeltaTime, 360f);
        float resolvedOrbitRadius = ResolveOrbitRadius();
        int orbitDamage = Mathf.Max(1, Mathf.RoundToInt(damage * orbitDamageScale));
        float orbitKnockback = GetAppliedKnockbackMultiplier(0.65f);
        Vector3 orbitScale = GetOrbitVisualScale();
        int activeCount = orbitProjectiles.Count;
        for (int i = 0; i < activeCount; i++)
        {
            Projectile orbitProjectile = orbitProjectiles[i];
            if (orbitProjectile == null)
                continue;

            float angle = orbitSpinAngle + (360f / Mathf.Max(1, activeCount)) * i;
            Vector2 position = GetOrbitWorldPosition(angle, resolvedOrbitRadius);
            Vector2 currentPosition = orbitProjectile.transform.position;
            Vector2 velocity = Time.fixedDeltaTime > 0f
                ? (position - currentPosition) / Time.fixedDeltaTime
                : Vector2.zero;

            orbitProjectile.ConfigureOrbitProfile(
                orbitDamage,
                orbitKnockback,
                onHitScatterCount,
                onHitScatterAngle,
                orbitHitCooldown);
            orbitProjectile.UpdateOrbitPose(position, velocity, angle - 90f, orbitScale, orbitVisualTint);
        }
    }

    private void HandleNovaBurst(EnemyController targetEnemy)
    {
        if (novaProjectileCount <= 0)
            return;
        if (targetEnemy == null)
            return;

        novaTimer -= Time.deltaTime;
        if (novaTimer > 0f)
            return;

        FireNovaBurst();
        novaTimer = Mathf.Max(0.35f, novaBurstInterval);
    }

    private void FireNovaBurst()
    {
        int count = Mathf.Max(1, novaProjectileCount);
        int burstDamage = Mathf.Max(1, Mathf.RoundToInt(damage * novaDamageScale));
        float burstSpeed = Mathf.Max(1f, projectileSpeed * novaProjectileSpeedScale);

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector3 spawnPos = ResolveProjectileSpawnPosition(dir);

            Projectile proj = Projectile.Spawn(projectilePrefab, spawnPos, Quaternion.identity, projectilesRoot);
            if (proj == null) continue;
            proj.Fire(
                dir,
                burstSpeed,
                burstDamage,
                0,
                GetAppliedKnockbackMultiplier(0.85f),
                0,
                0f,
                transform,
                returnProjectilesEnabled,
                returnSpeedMultiplier);
        }
    }

    private void CacheProjectileVisualTemplate()
    {
        if (projectilePrefab == null)
            return;

        SpriteRenderer projectileRenderer = projectilePrefab.GetComponent<SpriteRenderer>();
        orbitVisualBaseScale = projectilePrefab.transform.localScale;

        if (projectileRenderer != null && projectileRenderer.sprite != null)
        {
            Vector3 extents = projectileRenderer.sprite.bounds.extents;
            orbitVisualExtent = Mathf.Max(
                extents.x * Mathf.Abs(orbitVisualBaseScale.x),
                extents.y * Mathf.Abs(orbitVisualBaseScale.y));
        }
    }

    private void SyncOrbitProjectiles()
    {
        if (orbitProjectileCount <= 0)
        {
            ClearOrbitProjectiles();
            return;
        }

        for (int i = orbitProjectiles.Count - 1; i >= 0; i--)
        {
            if (orbitProjectiles[i] == null)
                orbitProjectiles.RemoveAt(i);
        }

        while (orbitProjectiles.Count < orbitProjectileCount)
        {
            Projectile orbitProjectile = Projectile.Spawn(projectilePrefab, transform.position, Quaternion.identity, projectilesRoot);
            if (orbitProjectile == null)
            {
                RunLogger.Warning($"Orbit projectile spawn failed. requested={orbitProjectileCount}, existing={orbitProjectiles.Count}");
                break;
            }

            orbitProjectile.BeginOrbit(
                Mathf.Max(1, Mathf.RoundToInt(damage * orbitDamageScale)),
                GetAppliedKnockbackMultiplier(0.65f),
                onHitScatterCount,
                onHitScatterAngle,
                orbitHitCooldown);
            orbitProjectiles.Add(orbitProjectile);
        }

        while (orbitProjectiles.Count > orbitProjectileCount)
        {
            int lastIndex = orbitProjectiles.Count - 1;
            Projectile orbitProjectile = orbitProjectiles[lastIndex];
            orbitProjectiles.RemoveAt(lastIndex);
            if (orbitProjectile != null)
                orbitProjectile.Despawn();
        }
    }

    private Vector2 GetOrbitWorldPosition(float angleDegrees, float resolvedOrbitRadius)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * resolvedOrbitRadius;
        return (Vector2)transform.position + offset;
    }

    private void ClearOrbitProjectiles()
    {
        for (int i = 0; i < orbitProjectiles.Count; i++)
        {
            if (orbitProjectiles[i] != null)
                orbitProjectiles[i].Despawn();
        }

        orbitProjectiles.Clear();
    }

    public void ResetRuntimeStats()
    {
        damage = baseDamage;
        projectileSpeed = baseProjectileSpeed;
        fireInterval = baseFireInterval;

        extraProjectiles = 0;
        spreadAngleStep = 8f;
        pierceCount = 0;
        knockbackMultiplier = baseKnockbackMultiplier;
        onHitScatterCount = 0;
        onHitScatterAngle = 18f;

        orbitProjectileCount = 0;
        orbitRadius = Mathf.Max(0f, baseOrbitRadius);
        orbitAngularSpeed = Mathf.Max(0f, baseOrbitAngularSpeed);
        orbitVisualScale = Mathf.Max(0.01f, baseOrbitVisualScale);
        orbitVisualSizeMultiplier = Mathf.Max(0.01f, baseOrbitVisualSizeMultiplier);
        returnProjectilesEnabled = baseReturnProjectilesEnabled;
        returnSpeedMultiplier = Mathf.Max(1f, baseReturnSpeedMultiplier);
        novaProjectileCount = 0;
        novaBurstInterval = Mathf.Max(0.45f, baseNovaBurstInterval);
        novaTimer = Mathf.Max(0.25f, novaBurstInterval);
        ClearOrbitProjectiles();
    }

    public void ApplyUpgrade(WeaponUpgrade upgrade)
    {
        if (upgrade == null) return;

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
                case WeaponUpgradeEffectType.NovaProjectileCountAdd:
                    novaProjectileCount += effect.intValue;
                    break;
                case WeaponUpgradeEffectType.NovaIntervalAdd:
                    novaBurstInterval += effect.floatValue;
                    break;
                case WeaponUpgradeEffectType.ReturnEnable:
                    if (effect.intValue > 0)
                        returnProjectilesEnabled = true;
                    break;
                case WeaponUpgradeEffectType.ReturnSpeedMultiplierAdd:
                    returnSpeedMultiplier += effect.floatValue;
                    break;
            }
        }

        damage = Mathf.Max(1, damage);
        projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
        fireInterval = Mathf.Max(0.01f, fireInterval);
        extraProjectiles = Mathf.Max(0, extraProjectiles);
        spreadAngleStep = Mathf.Max(0f, spreadAngleStep);
        pierceCount = Mathf.Max(0, pierceCount);
        knockbackMultiplier = Mathf.Max(0f, knockbackMultiplier);
        onHitScatterCount = Mathf.Max(0, onHitScatterCount);
        onHitScatterAngle = Mathf.Max(0f, onHitScatterAngle);
        orbitProjectileCount = Mathf.Max(0, orbitProjectileCount);
        orbitRadius = Mathf.Max(0f, orbitRadius);
        orbitAngularSpeed = Mathf.Max(0f, orbitAngularSpeed);
        novaProjectileCount = Mathf.Max(0, novaProjectileCount);
        novaBurstInterval = Mathf.Max(0.45f, novaBurstInterval);
        returnSpeedMultiplier = Mathf.Max(1f, returnSpeedMultiplier);
        novaTimer = Mathf.Min(novaTimer, Mathf.Max(0.25f, novaBurstInterval));
        SyncOrbitProjectiles();

        RunLogger.Event(
            $"Weapon upgraded: dmg={damage}, rate={1f / fireInterval:F2}/s, speed={projectileSpeed:F1}, " +
            $"multi={1 + extraProjectiles}, pierce={pierceCount}, scatter={onHitScatterCount}, orbit={orbitProjectileCount}, nova={novaProjectileCount}, return={returnProjectilesEnabled}");
    }

    public int GetDamage() => damage;
    public float GetFireRate() => 1f / fireInterval;

    private float GetAppliedKnockbackMultiplier(float extraScale = 1f)
    {
        if (!enableEnemyKnockback)
            return 0f;

        return Mathf.Max(0f, knockbackMultiplier) * Mathf.Max(0f, extraScale);
    }

    private void OnDrawGizmosSelected()
    {
        if (orbitProjectileCount <= 0) return;

        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.65f);
        float resolvedOrbitRadius = ResolveOrbitRadius();
        for (int i = 0; i < orbitProjectileCount; i++)
        {
            float angle = (360f / Mathf.Max(1, orbitProjectileCount)) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 pos = transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * resolvedOrbitRadius;
            Gizmos.DrawWireSphere(pos, Mathf.Max(0.05f, orbitVisualExtent * GetResolvedOrbitVisualScaleMultiplier()));
        }
    }

    private float ResolveOrbitRadius()
    {
        float playerExtent = EstimateSelfExtentRadius();
        float cardExtent = orbitVisualExtent * GetResolvedOrbitVisualScaleMultiplier();
        float minimumRadius = playerExtent + cardExtent + Mathf.Max(0f, orbitRadiusPadding);
        return Mathf.Max(orbitRadius, minimumRadius);
    }

    private float EstimateSelfExtentRadius()
    {
        float best = 0f;

        if (selfColliders != null)
        {
            for (int i = 0; i < selfColliders.Length; i++)
            {
                Collider2D col = selfColliders[i];
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
                    continue;

                Vector3 extents = col.bounds.extents;
                best = Mathf.Max(best, Mathf.Max(extents.x, extents.y));
            }
        }

        if (selfSpriteRenderers != null)
        {
            for (int i = 0; i < selfSpriteRenderers.Length; i++)
            {
                SpriteRenderer sr = selfSpriteRenderers[i];
                if (sr == null || !sr.enabled || !sr.gameObject.activeInHierarchy)
                    continue;

                Vector3 extents = sr.bounds.extents;
                best = Mathf.Max(best, Mathf.Max(extents.x, extents.y));
            }
        }

        return best;
    }

    private Vector3 GetOrbitVisualScale()
    {
        float scaleMultiplier = GetResolvedOrbitVisualScaleMultiplier();
        Vector3 baseScale = orbitVisualBaseScale == Vector3.zero ? Vector3.one : orbitVisualBaseScale;
        return new Vector3(
            baseScale.x * scaleMultiplier,
            baseScale.y * scaleMultiplier,
            baseScale.z == 0f ? 1f : baseScale.z);
    }

    private float GetResolvedOrbitVisualScaleMultiplier()
    {
        return Mathf.Max(0.01f, orbitVisualScale) * GetResolvedOrbitVisualSizeMultiplier();
    }

    private float GetResolvedOrbitVisualSizeMultiplier()
    {
        return orbitVisualSizeMultiplier > 0f ? orbitVisualSizeMultiplier : DefaultOrbitVisualSizeMultiplier;
    }
}
