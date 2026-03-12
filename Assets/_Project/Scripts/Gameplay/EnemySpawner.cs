using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private enum RoundPoolMode
    {
        Additive = 0,
        Replace = 1
    }

    [System.Serializable]
    private class RoundEnemyPoolEntry
    {
        [LocalizedLabel("起始回合")]
        [Min(1)] public int round = 1;
        [LocalizedLabel("回合池模式")]
        public RoundPoolMode mode = RoundPoolMode.Additive;
        [LocalizedLabel("该回合敌人预制体列表")]
        public EnemyController[] prefabs;
    }

    [LocalizedLabel("普通敌人预制体列表")]
    [SerializeField] private EnemyController[] enemyPrefabs;
    [Header("Round Enemy Pools / 回合敌人池")]
    [LocalizedLabel("启用回合敌人池")]
    [SerializeField] private bool useRoundEnemyPools = false;
    [LocalizedLabel("基础敌人池作为种子")]
    [SerializeField] private bool seedWithBaseEnemyPrefabs = true;
    [LocalizedLabel("回合敌人池配置")]
    [SerializeField] private List<RoundEnemyPoolEntry> roundEnemyPools = new List<RoundEnemyPoolEntry>();

    [Header("Scene References / 场景引用")]
    [LocalizedLabel("敌人根节点")]
    [SerializeField] private Transform enemiesRoot;
    [LocalizedLabel("玩家")]
    [SerializeField] private Transform player;
    [LocalizedLabel("子弹根节点")]
    [SerializeField] private Transform projectilesRoot;

    [Header("Spawn / 刷怪")]
    [LocalizedLabel("刷怪间隔 (秒)")]
    [SerializeField] private float spawnInterval = 1.0f;
    [LocalizedLabel("刷怪半径")]
    [SerializeField] private float spawnRadius = 7f;
    [LocalizedLabel("最小生成离玩家距离")]
    [SerializeField, Min(0.5f)] private float minSpawnDistanceFromPlayer = 3.5f;
    [LocalizedLabel("最大存活数量")]
    [SerializeField] private int maxAlive = 30;
    [LocalizedLabel("每次刷怪数量")]
    [SerializeField, Min(1)] private int spawnPerTick = 2;
    [LocalizedLabel("同次刷怪扩散半径")]
    [SerializeField, Min(0f)] private float intraTickSpreadRadius = 1.4f;
    [LocalizedLabel("生成点最小敌距")]
    [SerializeField, Min(0f)] private float minSpawnSpacing = 1.2f;
    [LocalizedLabel("生成点重试次数")]
    [SerializeField, Min(1)] private int spawnPositionAttempts = 18;
    [LocalizedLabel("生成点检测层")]
    [SerializeField] private LayerMask spawnSpacingMask = ~0;
    [LocalizedLabel("生成点仅检测敌人层")]
    [SerializeField] private bool spawnSpacingUseEnemyLayerOnly = true;

    [Header("Spawn Warning / 生成预警")]
    [LocalizedLabel("预警持续时间")]
    [SerializeField, Min(0f)] private float spawnWarningDuration = 0.5f;
    [LocalizedLabel("预警圆环颜色")]
    [SerializeField] private Color spawnWarningColor = new Color(1f, 0.2f, 0.15f, 0.5f);
    [LocalizedLabel("预警圆环半径")]
    [SerializeField, Min(0.1f)] private float spawnWarningRadius = 0.5f;
    [LocalizedLabel("预警圆环线段数")]
    [SerializeField, Min(8)] private int warningCircleSegments = 24;

    [Header("Safe Gap / 安全缺口")]
    [LocalizedLabel("启用安全缺口")]
    [SerializeField] private bool enableSafeGap = true;
    [LocalizedLabel("安全缺口角度")]
    [SerializeField, Range(30f, 120f)] private float safeGapAngle = 70f;
    [LocalizedLabel("缺口方向更新间隔")]
    [SerializeField, Min(0.5f)] private float safeGapRotateInterval = 3f;

    [Header("Extra Difficulty / 额外难度")]
    [LocalizedLabel("全局敌人生命倍率")]
    [SerializeField, Min(0.1f)] private float globalEnemyHpMultiplier = 1.25f;
    [LocalizedLabel("全局敌人速度倍率")]
    [SerializeField, Min(0.1f)] private float globalEnemySpeedMultiplier = 1.15f;

    [Header("Round Curves / 回合曲线倍率")]
    [LocalizedLabel("启用回合曲线")]
    [SerializeField] private bool useRoundCurves = true;
    [LocalizedLabel("曲线最大回合")]
    [SerializeField, Min(2)] private int roundCurveMaxRound = 11;
    [LocalizedLabel("刷怪间隔曲线")]
    [SerializeField] private AnimationCurve spawnIntervalCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.55f);
    [LocalizedLabel("每次刷怪数量曲线")]
    [SerializeField] private AnimationCurve spawnPerTickCurve = AnimationCurve.Linear(0f, 1f, 1f, 2.0f);
    [LocalizedLabel("最大存活数量曲线")]
    [SerializeField] private AnimationCurve maxAliveCurve = AnimationCurve.Linear(0f, 1f, 1f, 2.2f);
    [LocalizedLabel("敌人生命倍率曲线")]
    [SerializeField] private AnimationCurve hpMultiplierCurve = AnimationCurve.Linear(0f, 1f, 1f, 1.8f);
    [LocalizedLabel("敌人速度倍率曲线")]
    [SerializeField] private AnimationCurve speedMultiplierCurve = AnimationCurve.Linear(0f, 1f, 1f, 1.35f);

    [Header("XP Drop / 经验掉落")]
    [LocalizedLabel("经验掉落预制体")]
    [SerializeField] private XPPickup xpPickupPrefab;
    [LocalizedLabel("现金掉落预制体")]
    [SerializeField] private CashPickup cashPickupPrefab;
    [LocalizedLabel("掉落物根节点")]
    [SerializeField] private Transform pickupsRoot;

    [Header("Boss Round / Boss回合")]
    [LocalizedLabel("Boss回合刷出Boss")]
    [SerializeField] private bool spawnBossOnBossRound = true;
    [LocalizedLabel("Boss预制体")]
    [SerializeField] private EnemyController bossPrefab;
    [LocalizedLabel("Boss回合继续刷普通敌人")]
    [SerializeField] private bool spawnRegularEnemiesDuringBossRound = true;

    // Runtime state
    private float timer;
    private float safeGapTimer;
    private float safeGapDirection;
    private float runtimeSpawnInterval;
    private int runtimeSpawnPerTick;
    private int runtimeMaxAlive;
    private float runtimeEnemyHpMultiplier;
    private float runtimeEnemySpeedMultiplier;
    private int trackedRound = -1;
    private bool bossSpawnedThisRound;
    private readonly Collider2D[] spawnSpacingHits = new Collider2D[32];
    private EnemyController[] runtimeEnemyPool;
    private float[] runtimeEnemyWeights;
    private float runtimeWeightTotal;
    private readonly List<EnemyController> runtimeEnemyPoolBuffer = new List<EnemyController>(16);
    private readonly List<float> runtimeWeightBuffer = new List<float>(16);
    private readonly List<RoundEnemyPoolEntry> sortedRoundEnemyPools = new List<RoundEnemyPoolEntry>(16);

    // Warning circle material (reused)
    private Material warningLineMaterial;

    #region Per-Round Spawn Config

    private struct RoundSpawnConfig
    {
        public float interval;
        public int perTick;
        public int maxAlive;
        // Weights per enemy type (by name keyword match): melee, dash, ranged, tank, treasure
        public float wMelee, wDash, wRanged, wTank, wTreasure;
    }

    private static RoundSpawnConfig GetBuiltInRoundConfig(int round)
    {
        switch (round)
        {
            case 1:
                return new RoundSpawnConfig
                {
                    interval = 1.2f, perTick = 1, maxAlive = 15,
                    wMelee = 1f, wDash = 0f, wRanged = 0f, wTank = 0f, wTreasure = 0f
                };
            case 2:
                return new RoundSpawnConfig
                {
                    interval = 1.0f, perTick = 2, maxAlive = 20,
                    wMelee = 0.8f, wDash = 0.2f, wRanged = 0f, wTank = 0f, wTreasure = 0f
                };
            case 3:
                return new RoundSpawnConfig
                {
                    interval = 1.0f, perTick = 2, maxAlive = 22,
                    wMelee = 0.65f, wDash = 0.15f, wRanged = 0.2f, wTank = 0f, wTreasure = 0f
                };
            case 4:
                return new RoundSpawnConfig
                {
                    interval = 0.9f, perTick = 2, maxAlive = 25,
                    wMelee = 0.45f, wDash = 0.18f, wRanged = 0.17f, wTank = 0.12f, wTreasure = 0.08f
                };
            default: // R5+
                return new RoundSpawnConfig
                {
                    interval = Mathf.Max(0.5f, 0.8f - (round - 5) * 0.03f),
                    perTick = Mathf.Min(4, 3 + (round - 5) / 3),
                    maxAlive = Mathf.Min(45, 30 + (round - 5) * 2),
                    wMelee = 0.35f, wDash = 0.2f, wRanged = 0.2f, wTank = 0.15f, wTreasure = 0.1f
                };
        }
    }

    #endregion

    private void OnEnable()
    {
        timer = 0f;
        safeGapTimer = 0f;
        safeGapDirection = Random.Range(0f, 360f);
        trackedRound = -1;
        bossSpawnedThisRound = false;
        RefreshRuntimeSpawnSettings();
        int currentRound = GameFlowController.Instance != null ? Mathf.Max(1, GameFlowController.Instance.GetCurrentRound()) : 1;
        RefreshRuntimeEnemyPool(currentRound);
        RunLogger.Event(
            $"EnemySpawner enabled: interval={spawnInterval:F2}s, radius={spawnRadius:F1}, maxAlive={maxAlive}, " +
            $"perTick={spawnPerTick}, hpX={globalEnemyHpMultiplier:F2}, speedX={globalEnemySpeedMultiplier:F2}, " +
            $"roundCurves={useRoundCurves}");
    }

    private void OnDisable()
    {
        RunLogger.Event("EnemySpawner disabled.");
    }

    private void OnDestroy()
    {
        if (warningLineMaterial != null)
            Destroy(warningLineMaterial);
    }

    private void Update()
    {
        if (enemiesRoot == null || player == null) return;

        GameFlowController flow = GameFlowController.Instance;
        int currentRound = flow != null ? Mathf.Max(1, flow.GetCurrentRound()) : 1;
        if (currentRound != trackedRound)
        {
            trackedRound = currentRound;
            bossSpawnedThisRound = false;
            RefreshRuntimeEnemyPool(currentRound);
        }

        bool isBossRound = flow != null && flow.IsBossRoundActive();
        if (isBossRound)
        {
            TrySpawnBossForRound();
            if (!spawnRegularEnemiesDuringBossRound)
                return;
        }

        if (runtimeEnemyPool == null || runtimeEnemyPool.Length == 0) return;

        if (enableSafeGap)
        {
            safeGapTimer -= Time.deltaTime;
            if (safeGapTimer <= 0f)
            {
                safeGapTimer = safeGapRotateInterval;
                safeGapDirection = Random.Range(0f, 360f);
            }
        }

        RefreshRuntimeSpawnSettings();

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = runtimeSpawnInterval;

        if (enemiesRoot.childCount >= runtimeMaxAlive) return;
        int canSpawn = Mathf.Max(0, runtimeMaxAlive - enemiesRoot.childCount);
        int spawnCount = Mathf.Min(runtimeSpawnPerTick, canSpawn);
        for (int i = 0; i < spawnCount; i++)
            SpawnOne();
    }

    private void SpawnOne()
    {
        EnemyController prefab = PickWeightedPrefab();
        if (prefab == null) return;

        Vector3 pos = ResolveSpawnPositionWithSpacing();

        if (spawnWarningDuration > 0f)
            StartCoroutine(SpawnWithWarning(prefab, pos));
        else
            SpawnEnemy(prefab, pos);
    }

    private EnemyController PickWeightedPrefab()
    {
        if (runtimeEnemyPool == null || runtimeEnemyPool.Length == 0) return null;
        if (runtimeEnemyWeights == null || runtimeWeightTotal <= 0f)
            return runtimeEnemyPool[Random.Range(0, runtimeEnemyPool.Length)];

        float roll = Random.Range(0f, runtimeWeightTotal);
        float cumulative = 0f;
        for (int i = 0; i < runtimeEnemyWeights.Length; i++)
        {
            cumulative += runtimeEnemyWeights[i];
            if (roll <= cumulative)
                return runtimeEnemyPool[i];
        }
        return runtimeEnemyPool[runtimeEnemyPool.Length - 1];
    }

    private IEnumerator SpawnWithWarning(EnemyController prefab, Vector3 pos)
    {
        // Create warning circle
        GameObject warning = CreateWarningCircle(pos);

        float elapsed = 0f;
        while (elapsed < spawnWarningDuration)
        {
            elapsed += Time.deltaTime;
            // Pulse alpha
            if (warning != null)
            {
                LineRenderer lr = warning.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    float pulse = 0.3f + 0.7f * Mathf.Abs(Mathf.Sin(elapsed * Mathf.PI * 3f));
                    Color c = spawnWarningColor;
                    c.a = spawnWarningColor.a * pulse;
                    lr.startColor = c;
                    lr.endColor = c;
                }
            }
            yield return null;
        }

        if (warning != null) Destroy(warning);

        // Check if spawner still active
        if (this == null || !isActiveAndEnabled) yield break;

        SpawnEnemy(prefab, pos);
    }

    private GameObject CreateWarningCircle(Vector3 center)
    {
        GameObject go = new GameObject("SpawnWarning");
        go.transform.position = center;

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.alignment = LineAlignment.View;
        lr.numCapVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.startWidth = 0.06f;
        lr.endWidth = 0.06f;
        lr.startColor = spawnWarningColor;
        lr.endColor = spawnWarningColor;
        lr.sortingOrder = 200;

        if (warningLineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) warningLineMaterial = new Material(shader);
        }
        if (warningLineMaterial != null) lr.material = warningLineMaterial;

        int seg = Mathf.Max(8, warningCircleSegments);
        lr.positionCount = seg;
        float step = 2f * Mathf.PI / seg;
        for (int i = 0; i < seg; i++)
        {
            float angle = step * i;
            lr.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * spawnWarningRadius, Mathf.Sin(angle) * spawnWarningRadius, 0f));
        }

        return go;
    }

    private void SpawnEnemy(EnemyController prefab, Vector3 pos)
    {
        if (prefab == null) return;

        var e = Instantiate(prefab, pos, Quaternion.identity, enemiesRoot);
        e.Init(player, xpPickupPrefab, pickupsRoot, cashPickupPrefab);

        if (GameFlowController.Instance != null)
        {
            GameFlowController.Instance.GetCurrentEnemyMultipliers(out float hpMul, out float speedMul);
            hpMul *= runtimeEnemyHpMultiplier;
            speedMul *= runtimeEnemySpeedMultiplier;
            e.ApplyRuntimeModifiers(hpMul, speedMul);
        }
        else
        {
            e.ApplyRuntimeModifiers(runtimeEnemyHpMultiplier, runtimeEnemySpeedMultiplier);
        }

        var shooter = e.GetComponent<EnemyShooter>();
        if (shooter != null)
            shooter.Init(player, projectilesRoot);
    }

    private void TrySpawnBossForRound()
    {
        if (!spawnBossOnBossRound || bossPrefab == null) return;
        if (bossSpawnedThisRound) return;

        if (HasAliveBossInScene())
        {
            bossSpawnedThisRound = true;
            RunLogger.Warning($"Boss spawn skipped for round {trackedRound}: alive boss already exists.");
            return;
        }

        bossSpawnedThisRound = true;
        RefreshRuntimeSpawnSettings();
        Vector3 bossPos = ResolveSpawnPositionWithSpacing();
        SpawnEnemy(bossPrefab, bossPos);
        RunLogger.Event($"Boss spawned for round {trackedRound}. one-time spawn enforced.");
    }

    private bool HasAliveBossInScene()
    {
        BossAttackController[] bosses = enemiesRoot != null
            ? enemiesRoot.GetComponentsInChildren<BossAttackController>(true)
            : FindObjectsOfType<BossAttackController>();

        for (int i = 0; i < bosses.Length; i++)
        {
            BossAttackController boss = bosses[i];
            if (boss == null || !boss.gameObject.activeInHierarchy) continue;
            EnemyController bossEnemy = boss.GetComponent<EnemyController>();
            if (bossEnemy == null || bossEnemy.CurrentHP > 0f) return true;
        }
        return false;
    }

    #region Spawn Position

    private Vector3 ResolveSpawnPosition()
    {
        if (player == null) return transform.position;

        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic)
            return player.position + (Vector3)RandomScreenOffset();

        // Spawn within camera view but outside min distance from player.
        float halfH = cam.orthographicSize * 0.85f; // Slight margin from screen edge
        float halfW = halfH * cam.aspect;

        for (int i = 0; i < 12; i++)
        {
            float x = Random.Range(-halfW, halfW);
            float y = Random.Range(-halfH, halfH);
            Vector3 candidate = cam.transform.position + new Vector3(x, y, 0f);
            candidate.z = 0f;

            float distSqr = ((Vector2)candidate - (Vector2)player.position).sqrMagnitude;
            if (distSqr < minSpawnDistanceFromPlayer * minSpawnDistanceFromPlayer)
                continue;

            // Clamp to boundary
            if (CircleBoundary.Instance != null)
            {
                Vector2 clamped = CircleBoundary.Instance.ClampPosition(candidate);
                float clampedDistSqr = (clamped - (Vector2)player.position).sqrMagnitude;
                if (clampedDistSqr < minSpawnDistanceFromPlayer * minSpawnDistanceFromPlayer)
                    continue;
                candidate = new Vector3(clamped.x, clamped.y, 0f);
            }

            return candidate;
        }

        // Fallback: random direction at spawnRadius
        return player.position + (Vector3)RandomScreenOffset();
    }

    private Vector2 RandomScreenOffset()
    {
        float radius = Mathf.Max(spawnRadius, minSpawnDistanceFromPlayer);
        Vector2 dir = Random.insideUnitCircle.normalized;
        if (dir.sqrMagnitude <= 0.0001f) dir = Vector2.right;
        float dist = Random.Range(minSpawnDistanceFromPlayer, radius);
        return dir * dist;
    }

    private Vector3 ResolveSpawnPositionWithSpacing()
    {
        int attempts = Mathf.Max(1, spawnPositionAttempts);
        float spacing = Mathf.Max(0f, minSpawnSpacing);
        float spread = Mathf.Max(0f, intraTickSpreadRadius);

        Vector3 fallback = ResolveSpawnPosition();

        for (int i = 0; i < attempts; i++)
        {
            Vector3 candidate = ResolveSpawnPosition();
            if (spread > 0f)
            {
                Vector2 jitter = Random.insideUnitCircle * spread;
                candidate += new Vector3(jitter.x, jitter.y, 0f);
            }

            // Boundary clamp
            if (CircleBoundary.Instance != null)
            {
                Vector2 clamped = CircleBoundary.Instance.ClampPosition(candidate);
                candidate = new Vector3(clamped.x, clamped.y, 0f);
            }

            // Min distance from player check
            if (player != null)
            {
                float distSqr = ((Vector2)candidate - (Vector2)player.position).sqrMagnitude;
                if (distSqr < minSpawnDistanceFromPlayer * minSpawnDistanceFromPlayer)
                    continue;
            }

            // Safe gap check
            if (enableSafeGap && player != null && IsInSafeGap(candidate))
                continue;

            if (spacing <= 0f || IsSpawnPointClear(candidate, spacing))
                return candidate;

            fallback = candidate;
        }

        return fallback;
    }

    private bool IsInSafeGap(Vector3 spawnPos)
    {
        Vector2 dir = (Vector2)(spawnPos - player.position);
        if (dir.sqrMagnitude < 0.01f) return false;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float diff = Mathf.DeltaAngle(angle, safeGapDirection);
        return Mathf.Abs(diff) < safeGapAngle * 0.5f;
    }

    private bool IsSpawnPointClear(Vector3 point, float spacingRadius)
    {
        int queryMask = ResolveSpawnSpacingMask();
        int hitCount = Physics2D.OverlapCircleNonAlloc(point, spacingRadius, spawnSpacingHits, queryMask);
        if (hitCount <= 0) return true;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = spawnSpacingHits[i];
            if (hit == null) continue;
            EnemyController enemy = hit.GetComponent<EnemyController>() ?? hit.GetComponentInParent<EnemyController>();
            if (enemy != null && enemy.isActiveAndEnabled) return false;
        }
        return true;
    }

    private int ResolveSpawnSpacingMask()
    {
        int enemyLayer = enemiesRoot != null ? enemiesRoot.gameObject.layer : -1;
        if (enemyLayer < 0) return spawnSpacingMask.value;
        int enemyLayerMask = 1 << enemyLayer;
        if (spawnSpacingUseEnemyLayerOnly) return enemyLayerMask;
        int mask = spawnSpacingMask.value;
        return mask == 0 ? enemyLayerMask : mask | enemyLayerMask;
    }

    #endregion

    #region Enemy Pool

    private void RefreshRuntimeEnemyPool(int currentRound)
    {
        runtimeEnemyPoolBuffer.Clear();
        runtimeWeightBuffer.Clear();

        // Always use built-in per-round config for weights
        RoundSpawnConfig config = GetBuiltInRoundConfig(currentRound);

        // Resolve prefabs by name
        EnemyController melee = FindPrefabByNameKeyword("\u8FD1\u6218", "melee");
        EnemyController dash = FindPrefabByNameKeyword("\u51B2\u523A", "dash", "charger");
        EnemyController ranged = FindPrefabByNameKeyword("\u8FDC\u7A0B", "ranged");
        EnemyController tank = FindPrefabByNameKeyword("\u8089\u76FE", "tank", "brute", "heavy");
        EnemyController treasure = FindPrefabByNameKeyword("\u5B9D\u7BB1", "chest", "treasure");

        if (melee == null) melee = FindFirstNonNullPrefab();

        AddWeighted(melee, config.wMelee);
        AddWeighted(dash, config.wDash);
        AddWeighted(ranged, config.wRanged);
        AddWeighted(tank, config.wTank);
        AddWeighted(treasure, config.wTreasure);

        // If user has custom round pool config, merge those too
        if (useRoundEnemyPools && HasConfiguredRoundPoolEntries())
        {
            runtimeEnemyPoolBuffer.Clear();
            runtimeWeightBuffer.Clear();

            if (seedWithBaseEnemyPrefabs)
                AddUniquePrefabs(enemyPrefabs);

            sortedRoundEnemyPools.Clear();
            for (int i = 0; i < roundEnemyPools.Count; i++)
            {
                RoundEnemyPoolEntry entry = roundEnemyPools[i];
                if (entry == null || entry.round <= 0) continue;
                sortedRoundEnemyPools.Add(entry);
            }
            sortedRoundEnemyPools.Sort((a, b) => a.round.CompareTo(b.round));

            for (int i = 0; i < sortedRoundEnemyPools.Count; i++)
            {
                RoundEnemyPoolEntry entry = sortedRoundEnemyPools[i];
                if (entry.round > currentRound) break;
                if (entry.mode == RoundPoolMode.Replace) runtimeEnemyPoolBuffer.Clear();
                AddUniquePrefabs(entry.prefabs);
            }

            if (runtimeEnemyPoolBuffer.Count <= 0) AddUniquePrefabs(enemyPrefabs);

            // Equal weights for custom config
            runtimeWeightBuffer.Clear();
            for (int i = 0; i < runtimeEnemyPoolBuffer.Count; i++)
                runtimeWeightBuffer.Add(1f);
        }

        runtimeEnemyPool = runtimeEnemyPoolBuffer.ToArray();
        runtimeEnemyWeights = runtimeWeightBuffer.ToArray();
        runtimeWeightTotal = 0f;
        for (int i = 0; i < runtimeEnemyWeights.Length; i++)
            runtimeWeightTotal += runtimeEnemyWeights[i];

        RunLogger.Event(
            $"Round {currentRound} enemy pool ready: types={runtimeEnemyPool.Length}, " +
            $"config=R{currentRound}(interval={config.interval:F2}, perTick={config.perTick}, maxAlive={config.maxAlive})");
    }

    private void AddWeighted(EnemyController prefab, float weight)
    {
        if (prefab == null || weight <= 0f) return;
        runtimeEnemyPoolBuffer.Add(prefab);
        runtimeWeightBuffer.Add(weight);
    }

    private bool HasConfiguredRoundPoolEntries()
    {
        if (roundEnemyPools == null || roundEnemyPools.Count <= 0) return false;
        for (int i = 0; i < roundEnemyPools.Count; i++)
        {
            RoundEnemyPoolEntry entry = roundEnemyPools[i];
            if (entry == null || entry.round <= 0 || entry.prefabs == null) continue;
            for (int p = 0; p < entry.prefabs.Length; p++)
                if (entry.prefabs[p] != null) return true;
        }
        return false;
    }

    private EnemyController FindPrefabByNameKeyword(params string[] keywords)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length <= 0 || keywords == null || keywords.Length <= 0)
            return null;

        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            EnemyController prefab = enemyPrefabs[i];
            if (prefab == null) continue;
            string nameLower = prefab.name != null ? prefab.name.ToLowerInvariant() : string.Empty;
            for (int k = 0; k < keywords.Length; k++)
            {
                string keyword = keywords[k];
                if (string.IsNullOrWhiteSpace(keyword)) continue;
                if (nameLower.Contains(keyword.ToLowerInvariant())) return prefab;
            }
        }
        return null;
    }

    private EnemyController FindFirstNonNullPrefab()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length <= 0) return null;
        for (int i = 0; i < enemyPrefabs.Length; i++)
            if (enemyPrefabs[i] != null) return enemyPrefabs[i];
        return null;
    }

    private void AddUniqueIfNotNull(EnemyController prefab)
    {
        if (prefab == null || runtimeEnemyPoolBuffer.Contains(prefab)) return;
        runtimeEnemyPoolBuffer.Add(prefab);
    }

    private void AddUniquePrefabs(EnemyController[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0) return;
        for (int i = 0; i < prefabs.Length; i++)
        {
            EnemyController prefab = prefabs[i];
            if (prefab == null || runtimeEnemyPoolBuffer.Contains(prefab)) continue;
            runtimeEnemyPoolBuffer.Add(prefab);
        }
    }

    #endregion

    #region Runtime Settings

    private void RefreshRuntimeSpawnSettings()
    {
        int currentRound = 1;
        if (GameFlowController.Instance != null)
            currentRound = Mathf.Max(1, GameFlowController.Instance.GetCurrentRound());

        // Use built-in per-round config as base
        RoundSpawnConfig config = GetBuiltInRoundConfig(currentRound);

        // Apply round curves on top of per-round config
        float t = GetRoundCurveT();
        float hpMul = useRoundCurves ? EvaluateRoundCurve(hpMultiplierCurve, t) : 1f;
        float speedMul = useRoundCurves ? EvaluateRoundCurve(speedMultiplierCurve, t) : 1f;

        runtimeSpawnInterval = Mathf.Max(0.05f, config.interval);
        runtimeSpawnPerTick = Mathf.Max(1, config.perTick);
        runtimeMaxAlive = Mathf.Max(1, config.maxAlive);
        runtimeEnemyHpMultiplier = Mathf.Max(0.1f, globalEnemyHpMultiplier * hpMul);
        runtimeEnemySpeedMultiplier = Mathf.Max(0.1f, globalEnemySpeedMultiplier * speedMul);
    }

    private float GetRoundCurveT()
    {
        int currentRound = 1;
        if (GameFlowController.Instance != null)
            currentRound = Mathf.Max(1, GameFlowController.Instance.GetCurrentRound());
        int maxRound = Mathf.Max(2, roundCurveMaxRound);
        return Mathf.Clamp01((currentRound - 1f) / (maxRound - 1f));
    }

    private float EvaluateRoundCurve(AnimationCurve curve, float t)
    {
        if (curve == null || curve.length == 0) return 1f;
        return Mathf.Max(0.01f, curve.Evaluate(Mathf.Clamp01(t)));
    }

    #endregion

    private void OnValidate()
    {
        if (roundEnemyPools == null) return;
        for (int i = 0; i < roundEnemyPools.Count; i++)
        {
            RoundEnemyPoolEntry entry = roundEnemyPools[i];
            if (entry == null) continue;
            entry.round = Mathf.Max(1, entry.round);
        }
    }
}
