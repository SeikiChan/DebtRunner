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

        enemy.TakeDamage(damage);
        Destroy(gameObject);
    }
}
