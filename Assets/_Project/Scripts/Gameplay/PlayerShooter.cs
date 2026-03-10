using System.Collections.Generic;
using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private PlayerMotor2D motor;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform projectilesRoot;

    [Header("SFX / 音效")]
    [LocalizedLabel("射击音效")]
    [SerializeField] private AudioClip sfxShoot;

    [Header("Weapon (base)")]
    [SerializeField] private float fireInterval = 0.30f;
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

    [Header("Auto Aim")]
    [SerializeField, Min(0f)] private float autoAimMaxDistance = 8.5f;
    [SerializeField] private bool autoAimRequireOnScreen = true;
    [SerializeField, Range(0f, 0.25f)] private float autoAimViewportPadding = 0.02f;

    [Header("Orbiting Projectiles")]
    [SerializeField] private int orbitProjectileCount = 0;
    [SerializeField] private float orbitRadius = 1.6f;
    [SerializeField] private float orbitAngularSpeed = 140f;
    [SerializeField] private float orbitHitRadius = 0.35f;
    [SerializeField] private float orbitHitCooldown = 0.2f;
    [SerializeField] private float orbitDamageScale = 0.65f;
    [SerializeField] private float orbitVisualScale = 0.85f;
    [SerializeField] private Color orbitVisualTint = new Color(1f, 1f, 1f, 0.95f);

    [Header("Sweep Burst")]
    [SerializeField] private int novaProjectileCount = 0;
    [SerializeField] private float novaBurstInterval = 2.2f;
    [SerializeField] private float novaDamageScale = 0.75f;
    [SerializeField] private float novaProjectileSpeedScale = 0.85f;

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
    private readonly Dictionary<int, float> orbitLastHitAt = new Dictionary<int, float>();
    private readonly List<SpriteRenderer> orbitVisuals = new List<SpriteRenderer>();
    private Collider2D[] selfColliders;
    private SpriteRenderer[] selfSpriteRenderers;

    private float baseFireInterval;
    private float baseProjectileSpeed;
    private int baseDamage;
    private float baseKnockbackMultiplier;
    private Sprite orbitVisualSprite;
    private Material orbitVisualMaterial;
    private string orbitSortingLayerName;
    private int orbitSortingOrder;

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
        selfColliders = GetComponentsInChildren<Collider2D>(true);
        selfSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        CacheProjectileVisualTemplate();
    }

    private void OnEnable()
    {
        timer = 0f;
        novaTimer = Mathf.Max(0.25f, novaBurstInterval);
        SyncOrbitVisuals();
    }

    private void OnDisable()
    {
        ClearOrbitVisuals();
    }

    private void OnDestroy()
    {
        ClearOrbitVisuals();
    }

    private void Update()
    {
        if (motor == null || projectilePrefab == null) return;

        HandleOrbitingProjectiles();
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

    private EnemyController GetAutoAimTarget()
    {
        EnemyController[] enemies = FindObjectsOfType<EnemyController>();
        if (enemies == null || enemies.Length == 0) return null;

        EnemyController bestTarget = null;
        float bestHp = float.MaxValue;
        float bestSqrDistance = float.MaxValue;
        Vector2 selfPos = transform.position;
        float maxDistance = Mathf.Max(0f, autoAimMaxDistance);
        float maxDistanceSqr = maxDistance <= 0f ? float.PositiveInfinity : maxDistance * maxDistance;
        Camera mainCam = autoAimRequireOnScreen ? Camera.main : null;
        float hpEpsilon = 0.0001f;

        for (int i = 0; i < enemies.Length; i++)
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
        if (enemy.GetComponent<Rigidbody2D>() == null)
            return false;
        if (enemy.GetComponent<Collider2D>() == null)
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
        float totalSpread = spreadAngleStep * (shotCount - 1);
        float startAngle = -totalSpread * 0.5f;

        for (int i = 0; i < shotCount; i++)
        {
            float angleOffset = shotCount == 1 ? 0f : startAngle + (spreadAngleStep * i);
            Vector2 shotDirection = Rotate(baseDirection, angleOffset);
            Vector3 spawnPos = ResolveProjectileSpawnPosition(shotDirection);

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

        if (sfxShoot != null && SFXManager.Instance != null)
            SFXManager.Instance.Play(sfxShoot, 0.5f);
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
        if (orbitProjectileCount <= 0 || orbitRadius <= 0f)
        {
            ClearOrbitVisuals();
            return;
        }

        orbitSpinAngle += orbitAngularSpeed * Time.deltaTime;
        SyncOrbitVisuals();
        UpdateOrbitVisualPositions();

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
        int count = Mathf.Clamp(novaProjectileCount, 1, 24);
        int burstDamage = Mathf.Max(1, Mathf.RoundToInt(damage * novaDamageScale));
        float burstSpeed = Mathf.Max(1f, projectileSpeed * novaProjectileSpeedScale);

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector3 spawnPos = ResolveProjectileSpawnPosition(dir);

            Projectile proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity, projectilesRoot);
            proj.Fire(
                dir,
                burstSpeed,
                burstDamage,
                0,
                GetAppliedKnockbackMultiplier(0.85f),
                0,
                0f);
        }
    }

    private void CacheProjectileVisualTemplate()
    {
        if (projectilePrefab == null)
            return;

        SpriteRenderer projectileRenderer = projectilePrefab.GetComponent<SpriteRenderer>();
        if (projectileRenderer == null)
            return;

        orbitVisualSprite = projectileRenderer.sprite;
        orbitVisualMaterial = projectileRenderer.sharedMaterial;
        orbitSortingLayerName = projectileRenderer.sortingLayerName;
        orbitSortingOrder = projectileRenderer.sortingOrder - 1;
    }

    private void SyncOrbitVisuals()
    {
        if (orbitProjectileCount <= 0 || orbitRadius <= 0f)
        {
            ClearOrbitVisuals();
            return;
        }

        if (orbitVisualSprite == null)
            CacheProjectileVisualTemplate();
        if (orbitVisualSprite == null)
            return;

        while (orbitVisuals.Count < orbitProjectileCount)
        {
            GameObject go = new GameObject($"OrbitCard_{orbitVisuals.Count + 1}", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = orbitVisualSprite;
            sr.sharedMaterial = orbitVisualMaterial;
            sr.sortingLayerName = orbitSortingLayerName;
            sr.sortingOrder = orbitSortingOrder;
            sr.color = orbitVisualTint;
            go.transform.localScale = Vector3.one * Mathf.Max(0.01f, orbitVisualScale);
            orbitVisuals.Add(sr);
        }

        while (orbitVisuals.Count > orbitProjectileCount)
        {
            int lastIndex = orbitVisuals.Count - 1;
            SpriteRenderer sr = orbitVisuals[lastIndex];
            orbitVisuals.RemoveAt(lastIndex);
            if (sr != null)
                Destroy(sr.gameObject);
        }
    }

    private void UpdateOrbitVisualPositions()
    {
        int count = Mathf.Min(orbitProjectileCount, orbitVisuals.Count);
        for (int i = 0; i < count; i++)
        {
            SpriteRenderer sr = orbitVisuals[i];
            if (sr == null)
                continue;

            float angle = orbitSpinAngle + (360f / orbitProjectileCount) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 localOffset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;
            sr.transform.localPosition = localOffset;
            sr.transform.localRotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
    }

    private void ClearOrbitVisuals()
    {
        for (int i = 0; i < orbitVisuals.Count; i++)
        {
            if (orbitVisuals[i] != null)
                Destroy(orbitVisuals[i].gameObject);
        }

        orbitVisuals.Clear();
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
        orbitRadius = 1.6f;
        orbitAngularSpeed = 140f;
        novaProjectileCount = 0;
        novaTimer = Mathf.Max(0.25f, novaBurstInterval);
        orbitLastHitAt.Clear();
        ClearOrbitVisuals();
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
                case WeaponUpgradeEffectType.NovaProjectileCountAdd:
                    novaProjectileCount += effect.intValue;
                    break;
            }
        }

        // Keep upgrades fun while preventing extreme runaway scaling.
        damage = Mathf.Clamp(damage, 1, 10);
        projectileSpeed = Mathf.Clamp(projectileSpeed, 1f, 26f);
        fireInterval = Mathf.Clamp(fireInterval, 0.08f, 2f);
        extraProjectiles = Mathf.Clamp(extraProjectiles, 0, 4);
        spreadAngleStep = Mathf.Clamp(spreadAngleStep, 0f, 30f);
        pierceCount = Mathf.Clamp(pierceCount, 0, 3);
        knockbackMultiplier = Mathf.Max(0f, knockbackMultiplier);
        onHitScatterCount = Mathf.Clamp(onHitScatterCount, 0, 4);
        onHitScatterAngle = Mathf.Clamp(onHitScatterAngle, 0f, 70f);
        orbitProjectileCount = Mathf.Clamp(orbitProjectileCount, 0, 4);
        orbitRadius = Mathf.Clamp(orbitRadius, 0.8f, 3f);
        orbitAngularSpeed = Mathf.Clamp(orbitAngularSpeed, 60f, 300f);
        novaProjectileCount = Mathf.Clamp(novaProjectileCount, 0, 24);
        SyncOrbitVisuals();

        RunLogger.Event(
            $"Weapon upgraded: dmg={damage}, rate={1f / fireInterval:F2}/s, speed={projectileSpeed:F1}, " +
            $"multi={1 + extraProjectiles}, pierce={pierceCount}, scatter={onHitScatterCount}, orbit={orbitProjectileCount}, nova={novaProjectileCount}");
    }

    public int GetDamage() => damage;
    public float GetFireRate() => 1f / fireInterval;

    private float GetAppliedKnockbackMultiplier(float extraScale = 1f)
    {
        if (!enableEnemyKnockback)
            return 0f;

        // maxKnockbackMultiplier is treated as extra headroom above base knockback.
        float cap = Mathf.Max(0f, baseKnockbackMultiplier + Mathf.Max(0f, maxKnockbackMultiplier));
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
