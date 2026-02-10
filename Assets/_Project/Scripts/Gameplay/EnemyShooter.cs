using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private float fireInterval = 1.2f;
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private int damage = 1;

    private Transform player;
    private Transform projectilesRoot;
    private float timer;

    public void Init(Transform playerTf, Transform projectilesParent)
    {
        player = playerTf;
        projectilesRoot = projectilesParent;
    }

    private void OnEnable()
    {
        timer = Random.Range(0f, fireInterval);
    }

    private void Update()
    {
        if (player == null || projectilePrefab == null || projectilesRoot == null) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = fireInterval;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        var p = Instantiate(projectilePrefab, transform.position, Quaternion.identity, projectilesRoot);
        p.Fire(dir, projectileSpeed, damage);
    }
}
