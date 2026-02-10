using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float lifeSeconds = 4f;
    private Rigidbody2D rb;
    private int damage = 1;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    public void Fire(Vector2 dir, float speed, int dmg)
    {
        damage = Mathf.Max(1, dmg);
        rb.linearVelocity = dir.normalized * speed;
        CancelInvoke();
        Invoke(nameof(Expire), lifeSeconds);
    }

    private void Expire() => Destroy(gameObject);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var hp = other.GetComponent<PlayerHealth>();
        if (hp != null) hp.TakeDamage(damage);

        Destroy(gameObject);
    }
}
