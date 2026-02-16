using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyController[] enemyPrefabs;
    [SerializeField] private Transform enemiesRoot;
    [SerializeField] private Transform player;
    [SerializeField] private Transform projectilesRoot;

    [Header("Spawn")]
    [SerializeField] private float spawnInterval = 1.0f;
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private int maxAlive = 30;

    [Header("XP Drop")]
    [SerializeField] private XPPickup xpPickupPrefab;
    [SerializeField] private Transform pickupsRoot;

    private float timer;

    private void OnEnable()
    {
        timer = 0f;
        RunLogger.Event($"EnemySpawner enabled: interval={spawnInterval:F2}s, radius={spawnRadius:F1}, maxAlive={maxAlive}");
    }

    private void OnDisable()
    {
        RunLogger.Event("EnemySpawner disabled.");
    }

    private void Update()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;
        if (enemiesRoot == null || player == null) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = spawnInterval;

        if (enemiesRoot.childCount >= maxAlive) return;
        SpawnOne();
    }

    private void SpawnOne()
    {
        var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        float ang = Random.Range(0f, Mathf.PI * 2f);
        Vector2 offset = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spawnRadius;
        Vector3 pos = player.position + (Vector3)offset;

        var e = Instantiate(prefab, pos, Quaternion.identity, enemiesRoot);

        e.Init(player, xpPickupPrefab, pickupsRoot);

        if (GameFlowController.Instance != null)
        {
            GameFlowController.Instance.GetCurrentEnemyMultipliers(out float hpMul, out float speedMul);
            e.ApplyRuntimeModifiers(hpMul, speedMul);
        }

        var shooter = e.GetComponent<EnemyShooter>();
        if (shooter != null)
        {
            shooter.Init(player, projectilesRoot);
        }
    }
}
