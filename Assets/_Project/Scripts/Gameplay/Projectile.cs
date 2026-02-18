using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifeSeconds = 2f;

    private Rigidbody2D rb;
    private int damage;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Fire(Vector2 dir, float speed, int dmg)
    {
        damage = dmg;
        rb.linearVelocity = dir.normalized * speed;
        
        // 旋转子弹使其尖端面向射出方向
        // 因为子弹Sprite本身是竖向绘制，需要减90度来对齐
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        CancelInvoke();
        Invoke(nameof(Expire), lifeSeconds);
    }

    private void Expire()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null) return;

        Vector2 hitDirection = rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f
            ? rb.linearVelocity.normalized
            : ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;

        enemy.TakeDamage(damage, hitDirection);
        Destroy(gameObject);
    }
}
