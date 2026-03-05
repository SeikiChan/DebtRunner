using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private float fireInterval = 1.35f;
    [SerializeField] private float projectileSpeed = 5.2f;
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
        // Avoid instant spawn shots; gives player a readable reaction window.
        timer = Random.Range(fireInterval * 0.6f, fireInterval);
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
