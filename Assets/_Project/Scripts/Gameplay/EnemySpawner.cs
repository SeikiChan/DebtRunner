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

    private enum EnemySpawnArchetype
    {
        Unknown = 0,
        Melee = 1,
        Dash = 2,
        Ranged = 3,
        Tank = 4,
        Treasure = 5
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
    [SerializeField] private float spawnInterval = 0.52f;
    [LocalizedLabel("刷怪半径")]
    [SerializeField] private float spawnRadius = 7f;
    [LocalizedLabel("最小生成离玩家距离")]
    [SerializeField, Min(0.5f)] private float minSpawnDistanceFromPlayer = 5.1f;
    [LocalizedLabel("最大存活数量")]
    [SerializeField] private int maxAlive = 48;
    [LocalizedLabel("每次刷怪数量")]
    [SerializeField, Min(1)] private int spawnPerTick = 3;
    [LocalizedLabel("同次刷怪扩散半径")]
    [SerializeField, Min(0f)] private float intraTickSpreadRadius = 1.4f;
    [LocalizedLabel("生成点最小敌距")]
    [SerializeField, Min(0f)] private float minSpawnSpacing = 1.1f;
    [LocalizedLabel("生成点重试次数")]
    [SerializeField, Min(1)] private int spawnPositionAttempts = 30;
    [LocalizedLabel("生成点检测层")]
    [SerializeField] private LayerMask spawnSpacingMask = ~0;
    [LocalizedLabel("生成点仅检测敌人层")]
    [SerializeField] private bool spawnSpacingUseEnemyLayerOnly = true;

    [Header("Spawn Warning / 生成预警")]
    [LocalizedLabel("预警持续时间")]
    [SerializeField, Min(0f)] private float spawnWarningDuration = 1.15f;
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
    [SerializeField, Range(30f, 120f)] private float safeGapAngle = 34f;
    [LocalizedLabel("缺口方向更新间隔")]
    [SerializeField, Min(0.5f)] private float safeGapRotateInterval = 3f;

    [Header("Extra Difficulty / 额外难度")]
    [LocalizedLabel("全局敌人生命倍率")]
    [SerializeField, Min(0.1f)] private float globalEnemyHpMultiplier = 1.25f;
    [LocalizedLabel("全局敌人速度倍率")]
    [SerializeField, Min(0.1f)] private float globalEnemySpeedMultiplier = 1.15f;
    [Header("Late-Round HP Surge / 鍚庢湡琛€閲忓姞鍘?")]
    [SerializeField, Min(1)] private int lateRoundHpSurgeStartRound = 5;
    [SerializeField, Min(1)] private int lateRoundHpSurgePeakRound = 8;
    [SerializeField, Min(0f)] private float lateRoundMeleeFlatHpAtStart = 18f;
    [SerializeField, Min(0f)] private float lateRoundMeleeFlatHpAtPeak = 34f;
    [SerializeField, Min(0f)] private float lateRoundDashFlatHpAtStart = 16f;
    [SerializeField, Min(0f)] private float lateRoundDashFlatHpAtPeak = 30f;
    [SerializeField, Min(0f)] private float lateRoundRangedFlatHpAtStart = 16f;
    [SerializeField, Min(0f)] private float lateRoundRangedFlatHpAtPeak = 30f;
    [SerializeField, Min(0f)] private float lateRoundTankFlatHpAtStart = 10f;
    [SerializeField, Min(0f)] private float lateRoundTankFlatHpAtPeak = 20f;

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
    [SerializeField, Min(1)] private int maxSpawnTicksPerFrame = 4;

    [Header("XP Drop / 经验掉落")]
    [LocalizedLabel("经验掉落预制体")]
    [SerializeField] private XPPickup xpPickupPrefab;
    [SerializeField] private XPPickup[] xpPickupPrefabs;
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
    [Header("Treasure Spawn Limit / 宝箱怪上限")]
    [SerializeField, Min(0)] private int treasureSpawnLimitMidRounds = 1;
    [SerializeField, Min(0)] private int treasureSpawnLimitLateRounds = 2;

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
    private int treasureSpawnsThisRound;
    private int pendingTreasureSpawnsThisRound;
    private int pendingSpawnReservations;
    private EnemyController runtimeTreasurePrefab;
    private readonly Collider2D[] spawnSpacingHits = new Collider2D[32];
    private EnemyController[] runtimeEnemyPool;
    private float[] runtimeEnemyWeights;
    private EnemySpawnArchetype[] runtimeEnemyArchetypes;
    private readonly List<EnemyController> runtimeEnemyPoolBuffer = new List<EnemyController>(16);
    private readonly List<float> runtimeWeightBuffer = new List<float>(16);
    private readonly List<EnemySpawnArchetype> runtimeEnemyArchetypeBuffer = new List<EnemySpawnArchetype>(16);
    private readonly List<RoundEnemyPoolEntry> sortedRoundEnemyPools = new List<RoundEnemyPoolEntry>(16);
    private int selectedSpawnsThisRound;
    private int selectedMeleeSpawnsThisRound;
    private int selectedDashSpawnsThisRound;
    private int selectedRangedSpawnsThisRound;
    private int selectedTankSpawnsThisRound;
    private int selectedTreasureSpawnsThisRound;

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
                    interval = 1.28f, perTick = 1, maxAlive = 14,
                    wMelee = 1f, wDash = 0f, wRanged = 0f, wTank = 0f, wTreasure = 0f
                };
            case 2:
                return new RoundSpawnConfig
                {
                    interval = 1.14f, perTick = 1, maxAlive = 18,
                    wMelee = 0.9f, wDash = 0.1f, wRanged = 0f, wTank = 0f, wTreasure = 0f
                };
            case 3:
                return new RoundSpawnConfig
                {
                    interval = 1.02f, perTick = 2, maxAlive = 24,
                    wMelee = 0.7f, wDash = 0.2f, wRanged = 0.1f, wTank = 0f, wTreasure = 0f
                };
            case 4:
                return new RoundSpawnConfig
                {
                    interval = 0.92f, perTick = 2, maxAlive = 30,
                    wMelee = 0.52f, wDash = 0.2f, wRanged = 0.2f, wTank = 0.08f, wTreasure = 0f
                };
            case 5:
                return new RoundSpawnConfig
                {
                    interval = 0.80f, perTick = 2, maxAlive = 38,
                    wMelee = 0.48f, wDash = 0.2f, wRanged = 0.2f, wTank = 0.08f, wTreasure = 0.04f
                };
            case 6:
                return new RoundSpawnConfig
                {
                    interval = 0.72f, perTick = 2, maxAlive = 46,
                    wMelee = 0.42f, wDash = 0.2f, wRanged = 0.22f, wTank = 0.12f, wTreasure = 0.04f
                };
            case 7:
                return new RoundSpawnConfig
                {
                    interval = 0.60f, perTick = 3, maxAlive = 58,
                    wMelee = 0.36f, wDash = 0.2f, wRanged = 0.22f, wTank = 0.12f, wTreasure = 0.1f
                };
            case 8:
                return new RoundSpawnConfig
                {
                    interval = 0.56f, perTick = 3, maxAlive = 64,
                    wMelee = 0.32f, wDash = 0.2f, wRanged = 0.26f, wTank = 0.12f, wTreasure = 0.1f
                };
            case 9:
                return new RoundSpawnConfig
                {
                    interval = 0.52f, perTick = 3, maxAlive = 72,
                    wMelee = 0.28f, wDash = 0.2f, wRanged = 0.26f, wTank = 0.16f, wTreasure = 0.1f
                };
            case 10:
                return new RoundSpawnConfig
                {
                    interval = 0.48f, perTick = 3, maxAlive = 78,
                    wMelee = 0.24f, wDash = 0.2f, wRanged = 0.26f, wTank = 0.2f, wTreasure = 0.1f
                };
            default:
                int roundsPastTen = Mathf.Max(0, round - 10);
                return new RoundSpawnConfig
                {
                    interval = Mathf.Max(0.4f, 0.48f - roundsPastTen * 0.012f),
                    perTick = 3,
                    maxAlive = Mathf.Min(110, 78 + roundsPastTen * 6),
                    wMelee = 0.24f, wDash = 0.2f, wRanged = 0.26f, wTank = 0.2f, wTreasure = 0.1f
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
        ResetRoundSpawnTracking();
        runtimeTreasurePrefab = null;
        pendingSpawnReservations = 0;
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
            ResetRoundSpawnTracking();
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

        ProcessSpawnTicks();
    }

    private void SpawnOne()
    {
        EnemyController prefab = PickWeightedPrefab(out EnemySpawnArchetype archetype);
        if (prefab == null) return;
        RegisterSpawnSelection(archetype);
        pendingSpawnReservations++;

        bool isTreasure = IsTreasurePrefab(prefab);
        if (isTreasure)
            pendingTreasureSpawnsThisRound++;

        Vector3 pos = ResolveSpawnPositionWithSpacing();

        if (spawnWarningDuration > 0f)
            StartCoroutine(SpawnWithWarning(prefab, pos, archetype));
        else
            SpawnEnemy(prefab, pos, archetype);
    }

    public EnemyController SpawnTutorialEnemy()
    {
        if (enemiesRoot == null || player == null)
        {
            RunLogger.Warning("Tutorial enemy spawn skipped: EnemySpawner is missing player or enemiesRoot.");
            return null;
        }

        int currentRound = GameFlowController.Instance != null ? Mathf.Max(1, GameFlowController.Instance.GetCurrentRound()) : 1;
        if (currentRound != trackedRound)
        {
            trackedRound = currentRound;
            ResetRoundSpawnTracking();
            RefreshRuntimeEnemyPool(currentRound);
        }

        RefreshRuntimeSpawnSettings();

        EnemyController prefab = FindPrefabByNameKeyword("\u8FD1\u6218", "melee");
        if (prefab == null || LooksLikeTreasurePrefab(prefab))
            prefab = FindFirstNonTreasurePrefab();

        if (prefab == null)
        {
            RunLogger.Warning("Tutorial enemy spawn skipped: no suitable non-treasure enemy prefab found.");
            return null;
        }

        Vector3 pos = ResolveSpawnPositionWithSpacing();
        EnemyController spawned = SpawnEnemy(prefab, pos, EnemySpawnArchetype.Melee);
        if (spawned != null)
            RunLogger.Event($"Tutorial enemy spawned: {spawned.name} at {pos.x:F2},{pos.y:F2}.");
        return spawned;
    }

    private EnemyController PickWeightedPrefab(out EnemySpawnArchetype archetype)
    {
        archetype = EnemySpawnArchetype.Unknown;
        if (runtimeEnemyPool == null || runtimeEnemyPool.Length == 0) return null;
        bool canSpawnTreasure = CanSpawnTreasureThisRound();
        if (runtimeEnemyWeights == null || runtimeEnemyWeights.Length == 0)
        {
            List<EnemyController> valid = new List<EnemyController>(runtimeEnemyPool.Length);
            List<EnemySpawnArchetype> validArchetypes = new List<EnemySpawnArchetype>(runtimeEnemyPool.Length);
            for (int i = 0; i < runtimeEnemyPool.Length; i++)
            {
                EnemyController prefab = runtimeEnemyPool[i];
                if (prefab == null) continue;
                if (!canSpawnTreasure && IsTreasurePrefab(prefab)) continue;
                valid.Add(prefab);
                validArchetypes.Add(runtimeEnemyArchetypes != null && i < runtimeEnemyArchetypes.Length
                    ? runtimeEnemyArchetypes[i]
                    : DetectArchetype(prefab));
            }

            if (valid.Count <= 0) return null;
            int randomIndex = Random.Range(0, valid.Count);
            archetype = validArchetypes[randomIndex];
            return valid[randomIndex];
        }

        float validWeightTotal = 0f;
        for (int i = 0; i < runtimeEnemyWeights.Length && i < runtimeEnemyPool.Length; i++)
        {
            EnemyController prefab = runtimeEnemyPool[i];
            if (prefab == null) continue;
            if (!canSpawnTreasure && IsTreasurePrefab(prefab)) continue;
            validWeightTotal += Mathf.Max(0f, runtimeEnemyWeights[i]);
        }

        if (validWeightTotal <= 0f)
            return null;

        int bestIndex = -1;
        float bestScore = float.NegativeInfinity;
        int bestActualCount = int.MaxValue;
        for (int i = 0; i < runtimeEnemyWeights.Length && i < runtimeEnemyPool.Length; i++)
        {
            EnemyController prefab = runtimeEnemyPool[i];
            if (prefab == null) continue;
            if (!canSpawnTreasure && IsTreasurePrefab(prefab)) continue;

            float weight = Mathf.Max(0f, runtimeEnemyWeights[i]);
            if (weight <= 0f)
                continue;

            EnemySpawnArchetype candidateArchetype = runtimeEnemyArchetypes != null && i < runtimeEnemyArchetypes.Length
                ? runtimeEnemyArchetypes[i]
                : DetectArchetype(prefab);
            int actualCount = GetSelectedSpawnCount(candidateArchetype);
            float normalizedWeight = weight / validWeightTotal;
            float targetCountAfterNextSpawn = normalizedWeight * (selectedSpawnsThisRound + 1f);
            float deficit = targetCountAfterNextSpawn - actualCount;

            if (deficit > bestScore + 0.0001f
                || (Mathf.Abs(deficit - bestScore) <= 0.0001f && actualCount < bestActualCount))
            {
                bestIndex = i;
                bestScore = deficit;
                bestActualCount = actualCount;
            }
        }

        if (bestIndex >= 0)
        {
            archetype = runtimeEnemyArchetypes != null && bestIndex < runtimeEnemyArchetypes.Length
                ? runtimeEnemyArchetypes[bestIndex]
                : DetectArchetype(runtimeEnemyPool[bestIndex]);
            return runtimeEnemyPool[bestIndex];
        }

        return null;
    }

    private IEnumerator SpawnWithWarning(EnemyController prefab, Vector3 pos, EnemySpawnArchetype archetype)
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
        if (this == null || !isActiveAndEnabled)
        {
            pendingSpawnReservations = Mathf.Max(0, pendingSpawnReservations - 1);
            if (IsTreasurePrefab(prefab))
                pendingTreasureSpawnsThisRound = Mathf.Max(0, pendingTreasureSpawnsThisRound - 1);
            yield break;
        }

        SpawnEnemy(prefab, pos, archetype);
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

    private EnemyController SpawnEnemy(EnemyController prefab, Vector3 pos, EnemySpawnArchetype archetype = EnemySpawnArchetype.Unknown)
    {
        if (prefab == null) return null;

        pendingSpawnReservations = Mathf.Max(0, pendingSpawnReservations - 1);

        if (IsTreasurePrefab(prefab))
        {
            pendingTreasureSpawnsThisRound = Mathf.Max(0, pendingTreasureSpawnsThisRound - 1);
            treasureSpawnsThisRound++;
        }

        var e = Instantiate(prefab, pos, Quaternion.identity, enemiesRoot);
        if (xpPickupPrefabs != null && xpPickupPrefabs.Length > 0)
            e.Init(player, xpPickupPrefabs, pickupsRoot, cashPickupPrefab);
        else
            e.Init(player, xpPickupPrefab, pickupsRoot, cashPickupPrefab);

        if (GameFlowController.Instance != null)
        {
            GameFlowController.Instance.GetCurrentEnemyMultipliers(out float hpMul, out float speedMul);
            hpMul *= runtimeEnemyHpMultiplier;
            speedMul *= runtimeEnemySpeedMultiplier;
            e.ApplyRuntimeModifiers(hpMul, speedMul, GetLateRoundFlatHpBonus(archetype));
        }
        else
        {
            e.ApplyRuntimeModifiers(runtimeEnemyHpMultiplier, runtimeEnemySpeedMultiplier, GetLateRoundFlatHpBonus(archetype));
        }

        var shooter = e.GetComponent<EnemyShooter>();
        if (shooter != null)
            shooter.Init(player, projectilesRoot);

        return e;
    }

    private float GetLateRoundFlatHpBonus(EnemySpawnArchetype archetype)
    {
        int currentRound = GameFlowController.Instance != null
            ? Mathf.Max(1, GameFlowController.Instance.GetCurrentRound())
            : Mathf.Max(1, trackedRound);
        int startRound = Mathf.Max(1, lateRoundHpSurgeStartRound);
        if (currentRound < startRound)
            return 0f;

        int peakRound = Mathf.Max(startRound, lateRoundHpSurgePeakRound);
        float t = peakRound == startRound
            ? 1f
            : Mathf.Clamp01((currentRound - startRound) / (float)(peakRound - startRound));

        switch (archetype)
        {
            case EnemySpawnArchetype.Melee:
                return Mathf.Lerp(lateRoundMeleeFlatHpAtStart, lateRoundMeleeFlatHpAtPeak, t);
            case EnemySpawnArchetype.Dash:
                return Mathf.Lerp(lateRoundDashFlatHpAtStart, lateRoundDashFlatHpAtPeak, t);
            case EnemySpawnArchetype.Ranged:
                return Mathf.Lerp(lateRoundRangedFlatHpAtStart, lateRoundRangedFlatHpAtPeak, t);
            case EnemySpawnArchetype.Tank:
                return Mathf.Lerp(lateRoundTankFlatHpAtStart, lateRoundTankFlatHpAtPeak, t);
            default:
                return 0f;
        }
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
        runtimeEnemyArchetypeBuffer.Clear();

        RoundSpawnConfig config = GetBuiltInRoundConfig(currentRound);
        List<EnemyController> availablePrefabs = BuildAvailableEnemyPoolForRound(currentRound);
        List<EnemyController> searchPrefabs = availablePrefabs.Count > 0
            ? availablePrefabs
            : GetAllConfiguredEnemyPrefabs();

        EnemyController melee = FindPrefabByNameKeyword(searchPrefabs, "\u8FD1\u6218", "melee");
        EnemyController dash = FindPrefabByNameKeyword(searchPrefabs, "\u51B2\u523A", "dash", "charger");
        EnemyController ranged = FindPrefabByNameKeyword(searchPrefabs, "\u8FDC\u7A0B", "ranged");
        EnemyController tank = FindPrefabByNameKeyword(searchPrefabs, "\u8089\u76FE", "tank", "brute", "heavy");
        EnemyController treasure = FindPrefabByNameKeyword(searchPrefabs, "\u5B9D\u7BB1", "chest", "treasure");
        runtimeTreasurePrefab = treasure;

        if (melee == null)
            melee = FindFirstNonTreasurePrefab(searchPrefabs);
        if (melee == null)
            melee = FindFirstNonNullPrefab(searchPrefabs);

        AddWeighted(melee, config.wMelee, EnemySpawnArchetype.Melee);
        AddWeighted(dash, config.wDash, EnemySpawnArchetype.Dash);
        AddWeighted(ranged, config.wRanged, EnemySpawnArchetype.Ranged);
        AddWeighted(tank, config.wTank, EnemySpawnArchetype.Tank);
        AddWeighted(treasure, config.wTreasure, EnemySpawnArchetype.Treasure);

        if (runtimeEnemyPoolBuffer.Count <= 0)
        {
            runtimeEnemyPoolBuffer.Clear();
            runtimeWeightBuffer.Clear();
            runtimeEnemyArchetypeBuffer.Clear();

            for (int i = 0; i < searchPrefabs.Count; i++)
            {
                EnemyController prefab = searchPrefabs[i];
                if (prefab == null)
                    continue;

                runtimeEnemyPoolBuffer.Add(prefab);
                runtimeWeightBuffer.Add(1f);
                runtimeEnemyArchetypeBuffer.Add(DetectArchetype(prefab));
            }
        }

        runtimeEnemyPool = runtimeEnemyPoolBuffer.ToArray();
        runtimeEnemyWeights = runtimeWeightBuffer.ToArray();
        runtimeEnemyArchetypes = runtimeEnemyArchetypeBuffer.ToArray();

        RunLogger.Event(
            $"Round {currentRound} enemy pool ready: types={runtimeEnemyPool.Length}, " +
            $"config=R{currentRound}(interval={config.interval:F2}, perTick={config.perTick}, maxAlive={config.maxAlive}, " +
            $"melee={config.wMelee:F2}, dash={config.wDash:F2}, ranged={config.wRanged:F2}, tank={config.wTank:F2}, treasure={config.wTreasure:F2})");
    }

    private void AddWeighted(EnemyController prefab, float weight, EnemySpawnArchetype archetype)
    {
        if (prefab == null || weight <= 0f) return;
        runtimeEnemyPoolBuffer.Add(prefab);
        runtimeWeightBuffer.Add(weight);
        runtimeEnemyArchetypeBuffer.Add(archetype);
    }

    private void ResetRoundSpawnTracking()
    {
        bossSpawnedThisRound = false;
        treasureSpawnsThisRound = 0;
        pendingTreasureSpawnsThisRound = 0;
        pendingSpawnReservations = 0;
        selectedSpawnsThisRound = 0;
        selectedMeleeSpawnsThisRound = 0;
        selectedDashSpawnsThisRound = 0;
        selectedRangedSpawnsThisRound = 0;
        selectedTankSpawnsThisRound = 0;
        selectedTreasureSpawnsThisRound = 0;
    }

    private void RegisterSpawnSelection(EnemySpawnArchetype archetype)
    {
        selectedSpawnsThisRound++;
        switch (archetype)
        {
            case EnemySpawnArchetype.Melee:
                selectedMeleeSpawnsThisRound++;
                break;
            case EnemySpawnArchetype.Dash:
                selectedDashSpawnsThisRound++;
                break;
            case EnemySpawnArchetype.Ranged:
                selectedRangedSpawnsThisRound++;
                break;
            case EnemySpawnArchetype.Tank:
                selectedTankSpawnsThisRound++;
                break;
            case EnemySpawnArchetype.Treasure:
                selectedTreasureSpawnsThisRound++;
                break;
        }
    }

    private int GetSelectedSpawnCount(EnemySpawnArchetype archetype)
    {
        switch (archetype)
        {
            case EnemySpawnArchetype.Melee:
                return selectedMeleeSpawnsThisRound;
            case EnemySpawnArchetype.Dash:
                return selectedDashSpawnsThisRound;
            case EnemySpawnArchetype.Ranged:
                return selectedRangedSpawnsThisRound;
            case EnemySpawnArchetype.Tank:
                return selectedTankSpawnsThisRound;
            case EnemySpawnArchetype.Treasure:
                return selectedTreasureSpawnsThisRound;
            default:
                return 0;
        }
    }

    private EnemySpawnArchetype DetectArchetype(EnemyController prefab)
    {
        if (prefab == null)
            return EnemySpawnArchetype.Unknown;
        if (LooksLikeTreasurePrefab(prefab))
            return EnemySpawnArchetype.Treasure;

        string nameLower = prefab.name != null ? prefab.name.ToLowerInvariant() : string.Empty;
        if (nameLower.Contains("dash") || nameLower.Contains("charger") || nameLower.Contains("\u51b2\u523a"))
            return EnemySpawnArchetype.Dash;
        if (nameLower.Contains("ranged") || nameLower.Contains("\u8fdc\u7a0b"))
            return EnemySpawnArchetype.Ranged;
        if (nameLower.Contains("tank") || nameLower.Contains("brute") || nameLower.Contains("heavy") || nameLower.Contains("\u8089\u76fe"))
            return EnemySpawnArchetype.Tank;
        if (nameLower.Contains("melee") || nameLower.Contains("\u8fd1\u6218"))
            return EnemySpawnArchetype.Melee;

        return EnemySpawnArchetype.Unknown;
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
        return FindPrefabByNameKeyword(GetAllConfiguredEnemyPrefabs(), keywords);
    }

    private EnemyController FindPrefabByNameKeyword(IList<EnemyController> prefabs, params string[] keywords)
    {
        if (prefabs == null || prefabs.Count <= 0 || keywords == null || keywords.Length <= 0)
            return null;

        for (int i = 0; i < prefabs.Count; i++)
        {
            EnemyController prefab = prefabs[i];
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
        return FindFirstNonNullPrefab(GetAllConfiguredEnemyPrefabs());
    }

    private EnemyController FindFirstNonNullPrefab(IList<EnemyController> prefabs)
    {
        if (prefabs == null || prefabs.Count <= 0)
            return null;

        for (int i = 0; i < prefabs.Count; i++)
            if (prefabs[i] != null)
                return prefabs[i];
        return null;
    }

    private EnemyController FindFirstNonTreasurePrefab()
    {
        if (runtimeEnemyPool != null && runtimeEnemyPool.Length > 0)
            return FindFirstNonTreasurePrefab(runtimeEnemyPool);

        return FindFirstNonTreasurePrefab(GetAllConfiguredEnemyPrefabs());
    }

    private EnemyController FindFirstNonTreasurePrefab(IList<EnemyController> prefabs)
    {
        if (prefabs == null || prefabs.Count <= 0)
            return null;

        for (int i = 0; i < prefabs.Count; i++)
        {
            EnemyController prefab = prefabs[i];
            if (prefab == null || LooksLikeTreasurePrefab(prefab))
                continue;

            return prefab;
        }

        return null;
    }

    private void AddUniquePrefabs(EnemyController[] prefabs)
    {
        AddUniquePrefabs(runtimeEnemyPoolBuffer, prefabs);
    }

    private void AddUniquePrefabs(List<EnemyController> target, EnemyController[] prefabs)
    {
        if (target == null || prefabs == null || prefabs.Length == 0)
            return;

        for (int i = 0; i < prefabs.Length; i++)
        {
            EnemyController prefab = prefabs[i];
            if (prefab == null || target.Contains(prefab))
                continue;

            target.Add(prefab);
        }
    }

    private List<EnemyController> BuildAvailableEnemyPoolForRound(int currentRound)
    {
        List<EnemyController> result = new List<EnemyController>(8);

        if (useRoundEnemyPools && HasConfiguredRoundPoolEntries())
        {
            if (seedWithBaseEnemyPrefabs)
                AddUniquePrefabs(result, enemyPrefabs);

            sortedRoundEnemyPools.Clear();
            for (int i = 0; i < roundEnemyPools.Count; i++)
            {
                RoundEnemyPoolEntry entry = roundEnemyPools[i];
                if (entry == null || entry.round <= 0)
                    continue;

                sortedRoundEnemyPools.Add(entry);
            }

            sortedRoundEnemyPools.Sort((a, b) => a.round.CompareTo(b.round));
            for (int i = 0; i < sortedRoundEnemyPools.Count; i++)
            {
                RoundEnemyPoolEntry entry = sortedRoundEnemyPools[i];
                if (entry.round > currentRound)
                    break;

                if (entry.mode == RoundPoolMode.Replace)
                    result.Clear();

                AddUniquePrefabs(result, entry.prefabs);
            }
        }

        if (result.Count <= 0)
            AddUniquePrefabs(result, enemyPrefabs);

        return result;
    }

    private List<EnemyController> GetAllConfiguredEnemyPrefabs()
    {
        List<EnemyController> result = new List<EnemyController>(8);
        AddUniquePrefabs(result, enemyPrefabs);

        if (roundEnemyPools != null)
        {
            for (int i = 0; i < roundEnemyPools.Count; i++)
            {
                RoundEnemyPoolEntry entry = roundEnemyPools[i];
                if (entry == null)
                    continue;

                AddUniquePrefabs(result, entry.prefabs);
            }
        }

        return result;
    }

    private bool IsTreasurePrefab(EnemyController prefab)
    {
        return prefab != null && runtimeTreasurePrefab != null && prefab == runtimeTreasurePrefab;
    }

    private bool LooksLikeTreasurePrefab(EnemyController prefab)
    {
        if (prefab == null)
            return false;

        if (runtimeTreasurePrefab != null && prefab == runtimeTreasurePrefab)
            return true;

        string nameLower = prefab.name != null ? prefab.name.ToLowerInvariant() : string.Empty;
        return nameLower.Contains("\u5B9D\u7BB1") || nameLower.Contains("treasure") || nameLower.Contains("chest");
    }

    private bool CanSpawnTreasureThisRound()
    {
        if (runtimeTreasurePrefab == null)
            return false;

        int currentRound = GameFlowController.Instance != null ? Mathf.Max(1, GameFlowController.Instance.GetCurrentRound()) : 1;
        int limit = currentRound >= 7
            ? Mathf.Max(0, treasureSpawnLimitLateRounds)
            : Mathf.Max(0, treasureSpawnLimitMidRounds);
        return treasureSpawnsThisRound + pendingTreasureSpawnsThisRound < limit;
    }

    #endregion

    #region Runtime Settings

    private void ProcessSpawnTicks()
    {
        float interval = Mathf.Max(0.05f, runtimeSpawnInterval);
        int processedTicks = 0;
        int maxTicks = Mathf.Max(1, maxSpawnTicksPerFrame);

        while (timer <= 0f && processedTicks < maxTicks)
        {
            timer += interval;
            processedTicks++;
            ProcessSingleSpawnTick();
        }

        if (timer <= 0f)
            timer = 0f;
    }

    private void ProcessSingleSpawnTick()
    {
        int effectiveAliveCount = GetEffectiveAliveCount();
        if (effectiveAliveCount >= runtimeMaxAlive)
            return;

        int canSpawn = Mathf.Max(0, runtimeMaxAlive - effectiveAliveCount);
        int spawnCount = Mathf.Min(runtimeSpawnPerTick, canSpawn);
        for (int i = 0; i < spawnCount; i++)
        {
            if (GetEffectiveAliveCount() >= runtimeMaxAlive)
                break;

            SpawnOne();
        }
    }

    private int GetEffectiveAliveCount()
    {
        int liveCount = enemiesRoot != null ? enemiesRoot.childCount : 0;
        return Mathf.Max(0, liveCount + pendingSpawnReservations);
    }

    private void RefreshRuntimeSpawnSettings()
    {
        int currentRound = 1;
        if (GameFlowController.Instance != null)
            currentRound = Mathf.Max(1, GameFlowController.Instance.GetCurrentRound());

        // Use built-in per-round config as base
        RoundSpawnConfig config = GetBuiltInRoundConfig(currentRound);

        // Apply round curves on top of per-round config
        float t = GetRoundCurveT();
        float intervalMul = useRoundCurves ? EvaluateRoundCurve(spawnIntervalCurve, t) : 1f;
        float perTickMul = useRoundCurves ? EvaluateRoundCurve(spawnPerTickCurve, t) : 1f;
        float maxAliveMul = useRoundCurves ? EvaluateRoundCurve(maxAliveCurve, t) : 1f;
        float hpMul = useRoundCurves ? EvaluateRoundCurve(hpMultiplierCurve, t) : 1f;
        float speedMul = useRoundCurves ? EvaluateRoundCurve(speedMultiplierCurve, t) : 1f;
        float countMul = GameFlowController.Instance != null
            ? Mathf.Max(1f, GameFlowController.Instance.GetCurrentEnemyCountMultiplier())
            : 1f;

        runtimeSpawnInterval = Mathf.Max(0.05f, config.interval * Mathf.Max(0.1f, intervalMul));
        runtimeSpawnPerTick = Mathf.Max(1, Mathf.RoundToInt(config.perTick * Mathf.Max(0.1f, perTickMul) * countMul));
        runtimeMaxAlive = Mathf.Max(1, Mathf.RoundToInt(config.maxAlive * Mathf.Max(0.1f, maxAliveMul) * countMul));
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
