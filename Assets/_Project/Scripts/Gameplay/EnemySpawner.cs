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
    [SerializeField] private float spawnRadius = 10f;
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
    [LocalizedLabel("只在屏幕外刷怪")]
    [SerializeField] private bool spawnOutsideCameraView = true;
    [LocalizedLabel("屏幕外额外距离")]
    [SerializeField, Min(0f)] private float offscreenPadding = 1.8f;
    [LocalizedLabel("最小刷怪距离")]
    [SerializeField, Min(0f)] private float minSpawnDistance = 12f;

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
    [LocalizedLabel("最小刷怪距离曲线")]
    [SerializeField] private AnimationCurve minSpawnDistanceCurve = AnimationCurve.Linear(0f, 1f, 1f, 1.2f);

    [Header("XP Drop / 经验掉落")]
    [LocalizedLabel("经验掉落预制体")]
    [SerializeField] private XPPickup xpPickupPrefab;
    [LocalizedLabel("掉落物根节点")]
    [SerializeField] private Transform pickupsRoot;

    [Header("Boss Round / Boss回合")]
    [LocalizedLabel("Boss回合刷出Boss")]
    [SerializeField] private bool spawnBossOnBossRound = true;
    [LocalizedLabel("Boss预制体")]
    [SerializeField] private EnemyController bossPrefab;

    private float timer;
    private float runtimeSpawnInterval;
    private int runtimeSpawnPerTick;
    private int runtimeMaxAlive;
    private float runtimeMinSpawnDistance;
    private float runtimeEnemyHpMultiplier;
    private float runtimeEnemySpeedMultiplier;
    private int trackedRound = -1;
    private bool bossSpawnedThisRound;
    private readonly Collider2D[] spawnSpacingHits = new Collider2D[32];
    private EnemyController[] runtimeEnemyPool;
    private readonly List<EnemyController> runtimeEnemyPoolBuffer = new List<EnemyController>(16);
    private readonly List<RoundEnemyPoolEntry> sortedRoundEnemyPools = new List<RoundEnemyPoolEntry>(16);

    private void OnEnable()
    {
        timer = 0f;
        trackedRound = -1;
        bossSpawnedThisRound = false;
        RefreshRuntimeSpawnSettings();
        int currentRound = GameFlowController.Instance != null ? Mathf.Max(1, GameFlowController.Instance.GetCurrentRound()) : 1;
        RefreshRuntimeEnemyPool(currentRound);
        RunLogger.Event(
            $"EnemySpawner enabled: interval={spawnInterval:F2}s, radius={spawnRadius:F1}, maxAlive={maxAlive}, " +
            $"perTick={spawnPerTick}, outsideView={spawnOutsideCameraView}, hpX={globalEnemyHpMultiplier:F2}, speedX={globalEnemySpeedMultiplier:F2}, " +
            $"roundCurves={useRoundCurves}");
    }

    private void OnDisable()
    {
        RunLogger.Event("EnemySpawner disabled.");
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

        if (flow != null && flow.IsBossRoundActive())
        {
            TrySpawnBossForRound();
            return;
        }

        if (runtimeEnemyPool == null || runtimeEnemyPool.Length == 0) return;

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
        var prefab = runtimeEnemyPool[Random.Range(0, runtimeEnemyPool.Length)];
        SpawnEnemy(prefab);
    }

    private void SpawnEnemy(EnemyController prefab)
    {
        if (prefab == null)
            return;

        Vector3 pos = ResolveSpawnPositionWithSpacing();

        var e = Instantiate(prefab, pos, Quaternion.identity, enemiesRoot);

        e.Init(player, xpPickupPrefab, pickupsRoot);

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
        {
            shooter.Init(player, projectilesRoot);
        }
    }

    private void TrySpawnBossForRound()
    {
        if (!spawnBossOnBossRound || bossPrefab == null)
            return;
        if (bossSpawnedThisRound)
            return;

        bossSpawnedThisRound = true;
        RefreshRuntimeSpawnSettings();
        SpawnEnemy(bossPrefab);
        RunLogger.Event($"Boss spawned for round {trackedRound}. one-time spawn enforced.");
    }

    private Vector3 ResolveSpawnPosition()
    {
        if (player == null)
            return transform.position;

        if (!spawnOutsideCameraView)
            return player.position + (Vector3)RandomOffsetByRadius();

        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic)
            return player.position + (Vector3)RandomOffsetByRadius();

        float halfHeight = cam.orthographicSize + Mathf.Max(0f, offscreenPadding);
        float halfWidth = halfHeight * cam.aspect;
        float minDistSqr = runtimeMinSpawnDistance * runtimeMinSpawnDistance;

        for (int i = 0; i < 12; i++)
        {
            Vector2 edgeOffset = RandomEdgeOffset(halfWidth, halfHeight);
            if (edgeOffset.sqrMagnitude < minDistSqr)
                continue;

            return player.position + (Vector3)edgeOffset;
        }

        float fallbackDistance = Mathf.Max(runtimeMinSpawnDistance, Mathf.Max(halfWidth, halfHeight));
        Vector2 fallbackDir = Random.insideUnitCircle.normalized;
        if (fallbackDir.sqrMagnitude <= 0.0001f)
            fallbackDir = Vector2.right;
        return player.position + (Vector3)(fallbackDir * fallbackDistance);
    }

    private Vector2 RandomOffsetByRadius()
    {
        float radius = Mathf.Max(spawnRadius, runtimeMinSpawnDistance);
        Vector2 dir = Random.insideUnitCircle.normalized;
        if (dir.sqrMagnitude <= 0.0001f)
            dir = Vector2.right;
        return dir * radius;
    }

    private Vector2 RandomEdgeOffset(float halfWidth, float halfHeight)
    {
        float x;
        float y;
        switch (Random.Range(0, 4))
        {
            case 0: // Top
                x = Random.Range(-halfWidth, halfWidth);
                y = halfHeight;
                break;
            case 1: // Bottom
                x = Random.Range(-halfWidth, halfWidth);
                y = -halfHeight;
                break;
            case 2: // Left
                x = -halfWidth;
                y = Random.Range(-halfHeight, halfHeight);
                break;
            default: // Right
                x = halfWidth;
                y = Random.Range(-halfHeight, halfHeight);
                break;
        }

        return new Vector2(x, y);
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

            if (spacing <= 0f || IsSpawnPointClear(candidate, spacing))
                return candidate;

            fallback = candidate;
        }

        return fallback;
    }

    private bool IsSpawnPointClear(Vector3 point, float spacingRadius)
    {
        int queryMask = ResolveSpawnSpacingMask();
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            point,
            spacingRadius,
            spawnSpacingHits,
            queryMask);

        if (hitCount <= 0)
            return true;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = spawnSpacingHits[i];
            if (hit == null)
                continue;

            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy == null)
                enemy = hit.GetComponentInParent<EnemyController>();

            if (enemy != null && enemy.isActiveAndEnabled)
                return false;
        }

        return true;
    }

    private int ResolveSpawnSpacingMask()
    {
        int enemyLayer = enemiesRoot != null ? enemiesRoot.gameObject.layer : -1;
        if (enemyLayer < 0)
            return spawnSpacingMask.value;

        int enemyLayerMask = 1 << enemyLayer;
        if (spawnSpacingUseEnemyLayerOnly)
            return enemyLayerMask;

        int mask = spawnSpacingMask.value;
        if (mask == 0)
            return enemyLayerMask;

        return mask | enemyLayerMask;
    }

    private void RefreshRuntimeEnemyPool(int currentRound)
    {
        runtimeEnemyPoolBuffer.Clear();

        if (seedWithBaseEnemyPrefabs)
            AddUniquePrefabs(enemyPrefabs);

        if (useRoundEnemyPools && roundEnemyPools != null && roundEnemyPools.Count > 0)
        {
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
                    runtimeEnemyPoolBuffer.Clear();

                AddUniquePrefabs(entry.prefabs);
            }
        }

        if (runtimeEnemyPoolBuffer.Count <= 0)
            AddUniquePrefabs(enemyPrefabs);

        runtimeEnemyPool = runtimeEnemyPoolBuffer.ToArray();

        RunLogger.Event(
            $"Round {currentRound} enemy pool ready: types={runtimeEnemyPool.Length}, " +
            $"mode={(useRoundEnemyPools ? "round-config" : "base-only")}");
    }

    private void AddUniquePrefabs(EnemyController[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0)
            return;

        for (int i = 0; i < prefabs.Length; i++)
        {
            EnemyController prefab = prefabs[i];
            if (prefab == null)
                continue;

            if (runtimeEnemyPoolBuffer.Contains(prefab))
                continue;

            runtimeEnemyPoolBuffer.Add(prefab);
        }
    }

    private void OnValidate()
    {
        if (roundEnemyPools == null)
            return;

        for (int i = 0; i < roundEnemyPools.Count; i++)
        {
            RoundEnemyPoolEntry entry = roundEnemyPools[i];
            if (entry == null)
                continue;

            entry.round = Mathf.Max(1, entry.round);
        }
    }

    private void RefreshRuntimeSpawnSettings()
    {
        float t = GetRoundCurveT();

        float intervalMul = useRoundCurves ? EvaluateRoundCurve(spawnIntervalCurve, t) : 1f;
        float perTickMul = useRoundCurves ? EvaluateRoundCurve(spawnPerTickCurve, t) : 1f;
        float maxAliveMul = useRoundCurves ? EvaluateRoundCurve(maxAliveCurve, t) : 1f;
        float hpMul = useRoundCurves ? EvaluateRoundCurve(hpMultiplierCurve, t) : 1f;
        float speedMul = useRoundCurves ? EvaluateRoundCurve(speedMultiplierCurve, t) : 1f;
        float minDistanceMul = useRoundCurves ? EvaluateRoundCurve(minSpawnDistanceCurve, t) : 1f;

        runtimeSpawnInterval = Mathf.Max(0.05f, spawnInterval * intervalMul);
        runtimeSpawnPerTick = Mathf.Max(1, Mathf.RoundToInt(spawnPerTick * perTickMul));
        runtimeMaxAlive = Mathf.Max(1, Mathf.RoundToInt(maxAlive * maxAliveMul));
        runtimeEnemyHpMultiplier = Mathf.Max(0.1f, globalEnemyHpMultiplier * hpMul);
        runtimeEnemySpeedMultiplier = Mathf.Max(0.1f, globalEnemySpeedMultiplier * speedMul);
        runtimeMinSpawnDistance = Mathf.Max(0f, minSpawnDistance * minDistanceMul);
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
        if (curve == null || curve.length == 0)
            return 1f;

        return Mathf.Max(0.01f, curve.Evaluate(Mathf.Clamp01(t)));
    }
}
