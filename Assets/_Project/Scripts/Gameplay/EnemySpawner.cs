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

    private void OnEnable() => timer = 0f;

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
    // 1) 随机选一个敌人Prefab（支持多种敌人）
    var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

    // 2) 计算刷新位置：在玩家周围一个圆环上
    float ang = Random.Range(0f, Mathf.PI * 2f);
    Vector2 offset = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spawnRadius;
    Vector3 pos = player.position + (Vector3)offset;

    // 3) 生成敌人
    var e = Instantiate(prefab, pos, Quaternion.identity, enemiesRoot);

    // 4) 注入：玩家引用 + XP掉落引用
    e.Init(player, xpPickupPrefab, pickupsRoot);

    // 5) 如果敌人有射击组件：注入玩家引用 + 子弹容器引用
    var shooter = e.GetComponent<EnemyShooter>();
    if (shooter != null)
    {
        shooter.Init(player, projectilesRoot);
    }
}
}
