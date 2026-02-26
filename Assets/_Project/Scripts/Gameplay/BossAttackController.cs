using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BossAttackController : MonoBehaviour
{
    private enum AttackPattern
    {
        ChipFan = 0,
        ReceiptSnipe = 1,
        ForeclosureDash = 2,
        InterestRing = 3,
        CollectionsCall = 4
    }

    private enum BossPhase
    {
        Phase1 = 0,
        Phase2 = 1,
        Enrage = 2
    }

    [Header("References / 引用")]
    [LocalizedLabel("Boss 敌人组件")]
    [SerializeField] private EnemyController bossEnemy;
    [LocalizedLabel("Boss 刚体")]
    [SerializeField] private Rigidbody2D bossRb;
    [LocalizedLabel("玩家 Transform")]
    [SerializeField] private Transform player;
    [LocalizedLabel("玩家移动组件")]
    [SerializeField] private PlayerMotor2D playerMotor;
    [LocalizedLabel("玩家生命组件")]
    [SerializeField] private PlayerHealth playerHealth;
    [LocalizedLabel("玩家刚体")]
    [SerializeField] private Rigidbody2D playerRb;
    [LocalizedLabel("敌方子弹预制体")]
    [SerializeField] private EnemyProjectile projectilePrefab;
    [LocalizedLabel("追债小怪预制体")]
    [SerializeField] private EnemyController debtCollectorPrefab;
    [LocalizedLabel("小怪经验掉落预制体")]
    [SerializeField] private XPPickup addsXpPickupPrefab;
    [LocalizedLabel("子弹根节点")]
    [SerializeField] private Transform projectilesRoot;
    [LocalizedLabel("敌人根节点")]
    [SerializeField] private Transform enemiesRoot;
    [LocalizedLabel("掉落物根节点")]
    [SerializeField] private Transform pickupsRoot;
    [LocalizedLabel("预警线材质")]
    [SerializeField] private Material telegraphMaterial;

    [Header("General Pace / 节奏")]
    [LocalizedLabel("开场延迟")]
    [SerializeField, Min(0f)] private float openingDelay = 0.85f;
    [LocalizedLabel("一阶段恢复窗口范围")]
    [SerializeField] private Vector2 phase1RecoveryRange = new Vector2(0.80f, 1.05f);
    [LocalizedLabel("二阶段恢复窗口范围")]
    [SerializeField] private Vector2 phase2RecoveryRange = new Vector2(0.60f, 0.85f);
    [LocalizedLabel("狂暴阶段恢复窗口范围")]
    [SerializeField] private Vector2 enrageRecoveryRange = new Vector2(0.40f, 0.60f);
    [LocalizedLabel("冲刺时暂停Boss常规移动")]
    [SerializeField] private bool suppressEnemyMovementDuringDash = true;

    [Header("Telegraph Visual / 预警表现")]
    [LocalizedLabel("预警颜色")]
    [SerializeField] private Color telegraphColor = new Color(1f, 0.25f, 0.20f, 0.92f);
    [LocalizedLabel("线预警宽度")]
    [SerializeField, Min(0.01f)] private float telegraphLineWidth = 0.10f;
    [LocalizedLabel("圆预警宽度")]
    [SerializeField, Min(0.01f)] private float telegraphCircleWidth = 0.08f;
    [LocalizedLabel("圆预警分段数")]
    [SerializeField, Min(16)] private int telegraphCircleSegments = 72;
    [LocalizedLabel("预警长度")]
    [SerializeField, Min(0.1f)] private float telegraphLength = 12f;
    [LocalizedLabel("预警排序层级")]
    [SerializeField] private int telegraphSortingOrder = 250;

    [Header("A) Chip Fan / 扇形散射")]
    [LocalizedLabel("A 预警时长")]
    [SerializeField, Min(0f)] private float chipFanTelegraphSeconds = 0.6f;
    [LocalizedLabel("A 最小子弹数")]
    [SerializeField, Min(1)] private int chipFanMinShots = 7;
    [LocalizedLabel("A 最大子弹数")]
    [SerializeField, Min(1)] private int chipFanMaxShots = 11;
    [LocalizedLabel("A 一阶段扇形角度")]
    [SerializeField, Min(1f)] private float chipFanSpreadPhase1 = 100f;
    [LocalizedLabel("A 二阶段扇形角度")]
    [SerializeField, Min(1f)] private float chipFanSpreadPhase2 = 80f;
    [LocalizedLabel("A 狂暴扇形角度")]
    [SerializeField, Min(1f)] private float chipFanSpreadEnrage = 66f;
    [LocalizedLabel("A 一阶段弹速")]
    [SerializeField, Min(0f)] private float chipFanProjectileSpeedPhase1 = 7.5f;
    [LocalizedLabel("A 二阶段弹速")]
    [SerializeField, Min(0f)] private float chipFanProjectileSpeedPhase2 = 8.6f;
    [LocalizedLabel("A 狂暴弹速")]
    [SerializeField, Min(0f)] private float chipFanProjectileSpeedEnrage = 9.6f;
    [LocalizedLabel("A 伤害")]
    [SerializeField, Min(1)] private int chipFanDamage = 1;

    [Header("B) Receipt Snipe / 收据点射")]
    [LocalizedLabel("B 预警时长")]
    [SerializeField, Min(0f)] private float receiptSnipeTelegraphSeconds = 0.5f;
    [LocalizedLabel("B 连发数量")]
    [SerializeField, Min(1)] private int receiptSnipeBurstCount = 3;
    [LocalizedLabel("B 一阶段连发间隔")]
    [SerializeField, Min(0f)] private float receiptSnipeBurstGapPhase1 = 0.14f;
    [LocalizedLabel("B 二阶段连发间隔")]
    [SerializeField, Min(0f)] private float receiptSnipeBurstGapPhase2 = 0.11f;
    [LocalizedLabel("B 狂暴连发间隔")]
    [SerializeField, Min(0f)] private float receiptSnipeBurstGapEnrage = 0.09f;
    [LocalizedLabel("B 弹速")]
    [SerializeField, Min(0f)] private float receiptSnipeProjectileSpeed = 11f;
    [LocalizedLabel("B 伤害")]
    [SerializeField, Min(1)] private int receiptSnipeDamage = 1;

    [Header("C) Foreclosure Dash / 直线冲刺")]
    [LocalizedLabel("C 预警时长")]
    [SerializeField, Min(0f)] private float dashTelegraphSeconds = 0.8f;
    [LocalizedLabel("C 冲刺速度")]
    [SerializeField, Min(0.1f)] private float dashSpeed = 18f;
    [LocalizedLabel("C 最小冲刺距离")]
    [SerializeField, Min(0.1f)] private float dashMinDistance = 3.5f;
    [LocalizedLabel("C 最大冲刺距离")]
    [SerializeField, Min(0.1f)] private float dashMaxDistance = 8.5f;
    [LocalizedLabel("C 伤害")]
    [SerializeField, Min(1)] private int dashDamage = 1;
    [LocalizedLabel("C 命中半径")]
    [SerializeField, Min(0f)] private float dashHitRadius = 0.85f;
    [LocalizedLabel("C 击退力度")]
    [SerializeField, Min(0f)] private float dashKnockbackImpulse = 8f;
    [LocalizedLabel("C 恢复时长")]
    [SerializeField, Min(0f)] private float dashRecoverySeconds = 0.6f;

    [Header("D) Interest Ring / 利息冲击环")]
    [LocalizedLabel("D 预警时长")]
    [SerializeField, Min(0f)] private float interestRingTelegraphSeconds = 0.6f;
    [LocalizedLabel("D 最少环数")]
    [SerializeField, Min(1)] private int interestRingMinCount = 2;
    [LocalizedLabel("D 最多环数")]
    [SerializeField, Min(1)] private int interestRingMaxCount = 3;
    [LocalizedLabel("D 一阶段环间隔")]
    [SerializeField, Min(0f)] private float interestRingDelayPhase1 = 0.42f;
    [LocalizedLabel("D 二阶段环间隔")]
    [SerializeField, Min(0f)] private float interestRingDelayPhase2 = 0.30f;
    [LocalizedLabel("D 狂暴环间隔")]
    [SerializeField, Min(0f)] private float interestRingDelayEnrage = 0.23f;
    [LocalizedLabel("D 起始半径")]
    [SerializeField, Min(0.1f)] private float interestRingStartRadius = 0.9f;
    [LocalizedLabel("D 结束半径")]
    [SerializeField, Min(0.1f)] private float interestRingEndRadius = 7f;
    [LocalizedLabel("D 一阶段扩张时长")]
    [SerializeField, Min(0.01f)] private float interestRingExpandSecondsPhase1 = 1.0f;
    [LocalizedLabel("D 二阶段扩张时长")]
    [SerializeField, Min(0.01f)] private float interestRingExpandSecondsPhase2 = 0.9f;
    [LocalizedLabel("D 狂暴扩张时长")]
    [SerializeField, Min(0.01f)] private float interestRingExpandSecondsEnrage = 0.8f;
    [LocalizedLabel("D 命中厚度")]
    [SerializeField, Min(0.01f)] private float interestRingHitThickness = 0.35f;
    [LocalizedLabel("D 伤害")]
    [SerializeField, Min(1)] private int interestRingDamage = 1;
    [LocalizedLabel("D 击退力度")]
    [SerializeField, Min(0f)] private float interestRingKnockbackImpulse = 6f;

    [Header("E) Collections Call / 召唤追债者")]
    [LocalizedLabel("E 预警时长")]
    [SerializeField, Min(0f)] private float collectionsCallTelegraphSeconds = 1.0f;
    [LocalizedLabel("E 最少召唤数")]
    [SerializeField, Min(1)] private int collectionsCallMinAdds = 2;
    [LocalizedLabel("E 最多召唤数")]
    [SerializeField, Min(1)] private int collectionsCallMaxAdds = 4;
    [LocalizedLabel("E 最小生成半径")]
    [SerializeField, Min(0.5f)] private float collectionsSpawnMinRadius = 2.8f;
    [LocalizedLabel("E 最大生成半径")]
    [SerializeField, Min(0.5f)] private float collectionsSpawnMaxRadius = 4.2f;
    [LocalizedLabel("E 最小间距")]
    [SerializeField, Min(0.1f)] private float collectionsMinSpacing = 1.2f;
    [LocalizedLabel("E 生成标记半径")]
    [SerializeField, Min(0.1f)] private float collectionsSpawnMarkRadius = 0.55f;
    [LocalizedLabel("E 小怪生命倍率")]
    [SerializeField, Min(0.1f)] private float addHpMultiplier = 0.9f;
    [LocalizedLabel("E 小怪速度倍率")]
    [SerializeField, Min(0.1f)] private float addSpeedMultiplier = 1.7f;

    private static Material cachedTelegraphMaterial;
    private readonly List<GameObject> liveTelegraphs = new List<GameObject>();
    private Coroutine attackLoopCo;
    private AttackPattern? lastPattern;
    private BossPhase? loggedPhase;
    private bool movementSuppressed;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        RestoreSuppressedMovement();
        if (attackLoopCo != null)
            StopCoroutine(attackLoopCo);
        attackLoopCo = StartCoroutine(AttackLoop());
    }

    private void OnDisable()
    {
        if (attackLoopCo != null)
        {
            StopCoroutine(attackLoopCo);
            attackLoopCo = null;
        }

        RestoreSuppressedMovement();
        ClearLiveTelegraphs();
    }

    private void OnValidate()
    {
        chipFanMaxShots = Mathf.Max(chipFanMinShots, chipFanMaxShots);
        interestRingMaxCount = Mathf.Max(interestRingMinCount, interestRingMaxCount);
        collectionsCallMaxAdds = Mathf.Max(collectionsCallMinAdds, collectionsCallMaxAdds);
        collectionsSpawnMaxRadius = Mathf.Max(collectionsSpawnMinRadius, collectionsSpawnMaxRadius);
        dashMaxDistance = Mathf.Max(dashMinDistance, dashMaxDistance);
    }

    private IEnumerator AttackLoop()
    {
        if (openingDelay > 0f)
            yield return new WaitForSeconds(openingDelay);

        while (enabled && gameObject.activeInHierarchy)
        {
            ResolveReferences();

            if (!CanExecuteAttack())
            {
                yield return null;
                continue;
            }

            BossPhase phase = ResolvePhase();
            if (!loggedPhase.HasValue || loggedPhase.Value != phase)
            {
                loggedPhase = phase;
                RunLogger.Event($"Boss phase -> {phase}");
            }

            AttackPattern pattern = PickPattern(phase);
            yield return ExecutePattern(pattern, phase);

            float recovery = GetRecoverySeconds(phase, pattern);
            if (recovery > 0f)
                yield return new WaitForSeconds(recovery);
        }
    }

    private bool CanExecuteAttack()
    {
        if (GameFlowController.Instance != null && !GameFlowController.Instance.IsInGameplayState)
            return false;

        if (bossEnemy != null && bossEnemy.CurrentHP <= 0f)
            return false;

        return player != null;
    }

    private BossPhase ResolvePhase()
    {
        if (bossEnemy == null)
            return BossPhase.Phase1;

        float hpRatio = bossEnemy.HealthRatio;
        if (hpRatio > 0.60f)
            return BossPhase.Phase1;
        if (hpRatio > 0.25f)
            return BossPhase.Phase2;
        return BossPhase.Enrage;
    }

    private AttackPattern PickPattern(BossPhase phase)
    {
        float wA = 0f;
        float wB = 0f;
        float wC = 0f;
        float wD = 0f;
        float wE = 0f;

        switch (phase)
        {
            case BossPhase.Phase1:
                wA = 0.42f;
                wB = 0.33f;
                wD = 0.25f;
                break;
            case BossPhase.Phase2:
                wA = 0.29f;
                wB = 0.23f;
                wC = 0.20f;
                wD = 0.18f;
                wE = 0.10f;
                break;
            default:
                wA = 0.34f;
                wB = 0.22f;
                wC = 0.20f;
                wD = 0.16f;
                wE = 0.08f;
                break;
        }

        int activePatternCount = 0;
        if (wA > 0f) activePatternCount++;
        if (wB > 0f) activePatternCount++;
        if (wC > 0f) activePatternCount++;
        if (wD > 0f) activePatternCount++;
        if (wE > 0f) activePatternCount++;

        AttackPattern picked = AttackPattern.ChipFan;
        for (int attempt = 0; attempt < 4; attempt++)
        {
            picked = WeightedPick(wA, wB, wC, wD, wE);
            if (!lastPattern.HasValue || picked != lastPattern.Value || activePatternCount <= 1 || attempt == 3)
                break;
        }

        lastPattern = picked;
        return picked;
    }

    private AttackPattern WeightedPick(float wA, float wB, float wC, float wD, float wE)
    {
        float total = wA + wB + wC + wD + wE;
        if (total <= 0f)
            return AttackPattern.ChipFan;

        float roll = Random.value * total;
        if ((roll -= wA) <= 0f) return AttackPattern.ChipFan;
        if ((roll -= wB) <= 0f) return AttackPattern.ReceiptSnipe;
        if ((roll -= wC) <= 0f) return AttackPattern.ForeclosureDash;
        if ((roll -= wD) <= 0f) return AttackPattern.InterestRing;
        return AttackPattern.CollectionsCall;
    }

    private IEnumerator ExecutePattern(AttackPattern pattern, BossPhase phase)
    {
        switch (pattern)
        {
            case AttackPattern.ChipFan:
                yield return ExecuteChipFan(phase);
                break;
            case AttackPattern.ReceiptSnipe:
                yield return ExecuteReceiptSnipe(phase);
                break;
            case AttackPattern.ForeclosureDash:
                yield return ExecuteForeclosureDash();
                break;
            case AttackPattern.InterestRing:
                yield return ExecuteInterestRing(phase);
                break;
            case AttackPattern.CollectionsCall:
                yield return ExecuteCollectionsCall(phase);
                break;
            default:
                yield return null;
                break;
        }
    }

    private IEnumerator ExecuteChipFan(BossPhase phase)
    {
        if (projectilePrefab == null)
            yield break;

        int shotCount = Random.Range(chipFanMinShots, chipFanMaxShots + 1);
        float spread = GetChipFanSpread(phase);
        float speed = GetChipFanProjectileSpeed(phase);
        Vector2 centerDir = GetPlayerFacingDirection();

        float startAngle = -0.5f * spread;
        float step = shotCount <= 1 ? 0f : spread / (shotCount - 1);
        Vector2 telegraphOrigin = transform.position;
        for (int i = 0; i < shotCount; i++)
        {
            float angle = startAngle + step * i;
            Vector2 dir = Rotate(centerDir, angle);
            SpawnLineTelegraph(telegraphOrigin, telegraphOrigin + dir * telegraphLength, chipFanTelegraphSeconds, telegraphLineWidth);
        }

        if (chipFanTelegraphSeconds > 0f)
            yield return new WaitForSeconds(chipFanTelegraphSeconds);

        Vector2 fireOrigin = transform.position;
        for (int i = 0; i < shotCount; i++)
        {
            float angle = startAngle + step * i;
            Vector2 dir = Rotate(centerDir, angle);
            SpawnProjectile(fireOrigin, dir, speed, chipFanDamage);
        }
    }

    private IEnumerator ExecuteReceiptSnipe(BossPhase phase)
    {
        if (projectilePrefab == null || player == null)
            yield break;

        Vector2 markedTarget = player.position;
        Vector2 telegraphOrigin = transform.position;
        SpawnLineTelegraph(telegraphOrigin, markedTarget, receiptSnipeTelegraphSeconds, telegraphLineWidth * 1.35f);

        if (receiptSnipeTelegraphSeconds > 0f)
            yield return new WaitForSeconds(receiptSnipeTelegraphSeconds);

        float burstGap = GetReceiptBurstGap(phase);
        int burstCount = Mathf.Max(1, receiptSnipeBurstCount);
        for (int i = 0; i < burstCount; i++)
        {
            Vector2 fireOrigin = transform.position;
            Vector2 toTarget = markedTarget - fireOrigin;
            if (toTarget.sqrMagnitude <= 0.0001f)
                toTarget = Vector2.right;

            SpawnProjectile(fireOrigin, toTarget.normalized, receiptSnipeProjectileSpeed, receiptSnipeDamage);

            if (i < burstCount - 1 && burstGap > 0f)
                yield return new WaitForSeconds(burstGap);
        }
    }

    private IEnumerator ExecuteForeclosureDash()
    {
        if (player == null)
            yield break;

        Vector2 start = transform.position;
        Vector2 toPlayer = (Vector2)player.position - start;
        if (toPlayer.sqrMagnitude <= 0.0001f)
            toPlayer = GetPlayerFacingDirection();

        Vector2 dir = toPlayer.normalized;
        float distance = Mathf.Clamp(toPlayer.magnitude, dashMinDistance, dashMaxDistance);
        Vector2 end = start + dir * distance;

        SpawnLineTelegraph(start, end, dashTelegraphSeconds, telegraphLineWidth * 1.5f);

        if (dashTelegraphSeconds > 0f)
            yield return new WaitForSeconds(dashTelegraphSeconds);

        if (suppressEnemyMovementDuringDash && bossEnemy != null && bossEnemy.enabled)
        {
            bossEnemy.enabled = false;
            movementSuppressed = true;
        }

        float duration = distance / Mathf.Max(0.1f, dashSpeed);
        float elapsed = 0f;
        bool hitDone = false;
        Vector2 previousPos = bossRb != null ? bossRb.position : (Vector2)transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            Vector2 nextPos = Vector2.Lerp(start, end, t);

            if (bossRb != null)
                bossRb.MovePosition(nextPos);
            else
                transform.position = nextPos;

            if (!hitDone && TryHitPlayerAlongSegment(previousPos, nextPos, dir))
                hitDone = true;

            previousPos = nextPos;
            yield return null;
        }

        RestoreSuppressedMovement();
    }

    private IEnumerator ExecuteInterestRing(BossPhase phase)
    {
        SpawnCircleTelegraph(transform.position, interestRingStartRadius, interestRingTelegraphSeconds, telegraphCircleWidth * 1.2f);

        if (interestRingTelegraphSeconds > 0f)
            yield return new WaitForSeconds(interestRingTelegraphSeconds);

        int ringCount = GetInterestRingCount(phase);
        float ringDelay = GetInterestRingDelay(phase);
        float ringExpandSeconds = GetInterestRingExpandSeconds(phase);

        for (int i = 0; i < ringCount; i++)
        {
            SpawnShockwaveRing(ringExpandSeconds);
            if (i < ringCount - 1 && ringDelay > 0f)
                yield return new WaitForSeconds(ringDelay);
        }
    }

    private IEnumerator ExecuteCollectionsCall(BossPhase phase)
    {
        if (debtCollectorPrefab == null)
            yield break;

        int addCount = GetCollectionsAddCount(phase);
        Vector2 center = player != null ? (Vector2)player.position : (Vector2)transform.position;
        List<Vector2> spawnPoints = BuildSpawnPoints(center, addCount);

        for (int i = 0; i < spawnPoints.Count; i++)
            SpawnCircleTelegraph(spawnPoints[i], collectionsSpawnMarkRadius, collectionsCallTelegraphSeconds, telegraphCircleWidth);

        if (collectionsCallTelegraphSeconds > 0f)
            yield return new WaitForSeconds(collectionsCallTelegraphSeconds);

        Transform root = enemiesRoot != null ? enemiesRoot : transform.parent;
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            EnemyController add = Instantiate(debtCollectorPrefab, spawnPoints[i], Quaternion.identity, root);
            add.Init(player, addsXpPickupPrefab, pickupsRoot);

            float hpMul = addHpMultiplier;
            float speedMul = addSpeedMultiplier;
            if (GameFlowController.Instance != null)
            {
                GameFlowController.Instance.GetCurrentEnemyMultipliers(out float currentHpMul, out float currentSpeedMul);
                hpMul *= currentHpMul;
                speedMul *= currentSpeedMul;
            }

            add.ApplyRuntimeModifiers(hpMul, speedMul);

            EnemyShooter shooter = add.GetComponent<EnemyShooter>();
            if (shooter != null)
                shooter.enabled = false;
        }
    }

    private bool TryHitPlayerAlongSegment(Vector2 from, Vector2 to, Vector2 dashDir)
    {
        if (player == null || playerHealth == null)
            return false;

        Vector2 playerPos = player.position;
        float dist = DistancePointToSegment(playerPos, from, to);
        if (dist > dashHitRadius)
            return false;

        playerHealth.TakeDamage(dashDamage);
        ApplyPlayerKnockback(dashDir, dashKnockbackImpulse);
        return true;
    }

    private void ApplyPlayerKnockback(Vector2 direction, float impulse)
    {
        if (direction.sqrMagnitude <= 0.0001f || impulse <= 0f)
            return;

        Vector2 knockDir = direction.normalized;
        if (playerMotor != null)
        {
            playerMotor.ApplyExternalImpulse(knockDir, impulse);
            return;
        }

        if (playerRb != null)
            playerRb.linearVelocity += knockDir * impulse;
    }

    private float GetChipFanSpread(BossPhase phase)
    {
        switch (phase)
        {
            case BossPhase.Phase2: return chipFanSpreadPhase2;
            case BossPhase.Enrage: return chipFanSpreadEnrage;
            default: return chipFanSpreadPhase1;
        }
    }

    private float GetChipFanProjectileSpeed(BossPhase phase)
    {
        switch (phase)
        {
            case BossPhase.Phase2: return chipFanProjectileSpeedPhase2;
            case BossPhase.Enrage: return chipFanProjectileSpeedEnrage;
            default: return chipFanProjectileSpeedPhase1;
        }
    }

    private float GetReceiptBurstGap(BossPhase phase)
    {
        switch (phase)
        {
            case BossPhase.Phase2: return receiptSnipeBurstGapPhase2;
            case BossPhase.Enrage: return receiptSnipeBurstGapEnrage;
            default: return receiptSnipeBurstGapPhase1;
        }
    }

    private int GetInterestRingCount(BossPhase phase)
    {
        if (phase == BossPhase.Phase1)
            return Mathf.Max(1, interestRingMinCount);

        if (phase == BossPhase.Enrage)
            return Mathf.Max(1, interestRingMaxCount);

        return Random.Range(Mathf.Max(1, interestRingMinCount), Mathf.Max(interestRingMinCount, interestRingMaxCount) + 1);
    }

    private float GetInterestRingDelay(BossPhase phase)
    {
        switch (phase)
        {
            case BossPhase.Phase2: return interestRingDelayPhase2;
            case BossPhase.Enrage: return interestRingDelayEnrage;
            default: return interestRingDelayPhase1;
        }
    }

    private float GetInterestRingExpandSeconds(BossPhase phase)
    {
        switch (phase)
        {
            case BossPhase.Phase2: return interestRingExpandSecondsPhase2;
            case BossPhase.Enrage: return interestRingExpandSecondsEnrage;
            default: return interestRingExpandSecondsPhase1;
        }
    }

    private int GetCollectionsAddCount(BossPhase phase)
    {
        int minAdds = Mathf.Max(1, collectionsCallMinAdds);
        int maxAdds = Mathf.Max(minAdds, collectionsCallMaxAdds);

        if (phase == BossPhase.Enrage)
            minAdds = Mathf.Min(maxAdds, minAdds + 1);

        return Random.Range(minAdds, maxAdds + 1);
    }

    private float GetRecoverySeconds(BossPhase phase, AttackPattern pattern)
    {
        if (pattern == AttackPattern.ForeclosureDash)
            return Mathf.Max(0f, dashRecoverySeconds);

        Vector2 range = phase1RecoveryRange;
        if (phase == BossPhase.Phase2)
            range = phase2RecoveryRange;
        else if (phase == BossPhase.Enrage)
            range = enrageRecoveryRange;

        float min = Mathf.Min(range.x, range.y);
        float max = Mathf.Max(range.x, range.y);
        return Random.Range(min, max);
    }

    private void SpawnProjectile(Vector2 origin, Vector2 direction, float speed, int damage)
    {
        if (projectilePrefab == null)
            return;

        EnemyProjectile shot = projectilesRoot != null
            ? Instantiate(projectilePrefab, origin, Quaternion.identity, projectilesRoot)
            : Instantiate(projectilePrefab, origin, Quaternion.identity);
        shot.Fire(direction, speed, damage);
    }

    private void SpawnShockwaveRing(float expandSeconds)
    {
        GameObject go = new GameObject("BossShockwaveRing", typeof(LineRenderer), typeof(BossShockwaveRing));
        Transform root = ResolveEffectsRoot();
        if (root != null)
            go.transform.SetParent(root, true);

        go.transform.position = transform.position;

        LineRenderer lr = go.GetComponent<LineRenderer>();
        ConfigureLineRenderer(lr, telegraphCircleWidth);
        if (lr != null)
            lr.loop = true;

        BossShockwaveRing ring = go.GetComponent<BossShockwaveRing>();
        ring.SetSegments(telegraphCircleSegments);
        ring.Init(
            player,
            playerHealth,
            playerMotor,
            playerRb,
            interestRingStartRadius,
            interestRingEndRadius,
            expandSeconds,
            interestRingHitThickness,
            interestRingDamage,
            interestRingKnockbackImpulse);
    }

    private void SpawnLineTelegraph(Vector2 from, Vector2 to, float lifeSeconds, float width)
    {
        GameObject go = CreateTelegraphObject("BossTelegraphLine");
        LineRenderer lr = go.GetComponent<LineRenderer>();
        ConfigureLineRenderer(lr, width);
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        RegisterTimedTelegraph(go, lifeSeconds);
    }

    private void SpawnCircleTelegraph(Vector2 center, float radius, float lifeSeconds, float width)
    {
        GameObject go = CreateTelegraphObject("BossTelegraphCircle");
        LineRenderer lr = go.GetComponent<LineRenderer>();
        ConfigureLineRenderer(lr, width);
        lr.loop = true;
        int pointCount = Mathf.Max(16, telegraphCircleSegments);
        lr.positionCount = pointCount;

        float step = Mathf.PI * 2f / pointCount;
        for (int i = 0; i < pointCount; i++)
        {
            float a = i * step;
            Vector3 pos = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Mathf.Max(0.01f, radius);
            lr.SetPosition(i, pos);
        }

        RegisterTimedTelegraph(go, lifeSeconds);
    }

    private void ConfigureLineRenderer(LineRenderer lineRenderer, float width)
    {
        if (lineRenderer == null)
            return;

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.startColor = telegraphColor;
        lineRenderer.endColor = telegraphColor;
        lineRenderer.sortingOrder = telegraphSortingOrder;

        Material mat = ResolveTelegraphMaterial();
        if (mat != null)
            lineRenderer.material = mat;
    }

    private Material ResolveTelegraphMaterial()
    {
        if (telegraphMaterial != null)
            return telegraphMaterial;

        if (cachedTelegraphMaterial != null)
            return cachedTelegraphMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            return null;

        cachedTelegraphMaterial = new Material(shader)
        {
            name = "BossTelegraphRuntimeMaterial"
        };
        return cachedTelegraphMaterial;
    }

    private GameObject CreateTelegraphObject(string objectName)
    {
        GameObject go = new GameObject(objectName, typeof(LineRenderer));
        Transform root = ResolveEffectsRoot();
        if (root != null)
            go.transform.SetParent(root, true);

        liveTelegraphs.Add(go);
        return go;
    }

    private void RegisterTimedTelegraph(GameObject go, float lifeSeconds)
    {
        if (go == null)
            return;

        if (lifeSeconds <= 0f)
        {
            UnregisterAndDestroyTelegraph(go);
            return;
        }

        StartCoroutine(DestroyTelegraphAfter(go, lifeSeconds));
    }

    private IEnumerator DestroyTelegraphAfter(GameObject go, float lifeSeconds)
    {
        yield return new WaitForSeconds(lifeSeconds);
        UnregisterAndDestroyTelegraph(go);
    }

    private void ClearLiveTelegraphs()
    {
        for (int i = liveTelegraphs.Count - 1; i >= 0; i--)
        {
            if (liveTelegraphs[i] != null)
                Destroy(liveTelegraphs[i]);
        }
        liveTelegraphs.Clear();
    }

    private void UnregisterAndDestroyTelegraph(GameObject go)
    {
        if (go == null)
            return;

        liveTelegraphs.Remove(go);
        Destroy(go);
    }

    private Transform ResolveEffectsRoot()
    {
        if (projectilesRoot != null)
            return projectilesRoot;
        if (transform.parent != null)
            return transform.parent;
        return null;
    }

    private void ResolveReferences()
    {
        if (bossEnemy == null)
            bossEnemy = GetComponent<EnemyController>();

        if (bossRb == null)
            bossRb = GetComponent<Rigidbody2D>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (player != null)
        {
            if (playerMotor == null)
                playerMotor = player.GetComponent<PlayerMotor2D>();
            if (playerHealth == null)
                playerHealth = player.GetComponent<PlayerHealth>();
            if (playerRb == null)
                playerRb = player.GetComponent<Rigidbody2D>();
        }

        if (projectilesRoot == null)
        {
            GameObject found = GameObject.Find("World/Projectiles");
            if (found != null)
                projectilesRoot = found.transform;
        }

        if (enemiesRoot == null)
        {
            GameObject found = GameObject.Find("World/Enemies");
            if (found != null)
                enemiesRoot = found.transform;
        }

        if (pickupsRoot == null)
        {
            GameObject found = GameObject.Find("World/Pickups");
            if (found != null)
                pickupsRoot = found.transform;
        }
    }

    private void RestoreSuppressedMovement()
    {
        if (!movementSuppressed)
            return;

        if (bossEnemy != null)
            bossEnemy.enabled = true;
        movementSuppressed = false;
    }

    private Vector2 GetPlayerFacingDirection()
    {
        if (playerMotor != null && playerMotor.LastMoveDir.sqrMagnitude > 0.0001f)
            return playerMotor.LastMoveDir.normalized;

        if (player != null)
        {
            Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
            if (toPlayer.sqrMagnitude > 0.0001f)
                return toPlayer.normalized;
        }

        return Vector2.right;
    }

    private List<Vector2> BuildSpawnPoints(Vector2 center, int count)
    {
        List<Vector2> points = new List<Vector2>(Mathf.Max(0, count));
        if (count <= 0)
            return points;

        float minSpacingSq = collectionsMinSpacing * collectionsMinSpacing;
        int attempts = Mathf.Max(12, count * 24);

        while (points.Count < count && attempts > 0)
        {
            attempts--;
            float radius = Random.Range(collectionsSpawnMinRadius, collectionsSpawnMaxRadius);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 candidate = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            bool valid = true;
            for (int i = 0; i < points.Count; i++)
            {
                if ((points[i] - candidate).sqrMagnitude < minSpacingSq)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
                points.Add(candidate);
        }

        if (points.Count >= count)
            return points;

        float fallbackRadius = Mathf.Lerp(collectionsSpawnMinRadius, collectionsSpawnMaxRadius, 0.55f);
        for (int i = points.Count; i < count; i++)
        {
            float angle = (Mathf.PI * 2f / Mathf.Max(1, count)) * i;
            points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * fallbackRadius);
        }

        return points;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y).normalized;
    }

    private static float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float denom = Vector2.Dot(ab, ab);
        if (denom <= 0.0001f)
            return Vector2.Distance(point, a);

        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / denom);
        Vector2 closest = a + ab * t;
        return Vector2.Distance(point, closest);
    }
}
