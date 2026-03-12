using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float lifeSeconds = 12f;
    private Rigidbody2D rb;
    private int damage = 1;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    private void OnDisable()
    {
        CancelInvoke();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (CircleBoundary.Instance == null)
            return;

        Vector2 pos = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 center = CircleBoundary.Instance.Center;
        float rx = CircleBoundary.Instance.RadiusX;
        float ry = CircleBoundary.Instance.RadiusY;
        Vector2 offset = pos - center;
        float nx = offset.x / rx;
        float ny = offset.y / ry;

        // Treat the circle boundary as the invisible wall for enemy shots.
        if (nx * nx + ny * ny > 1f)
            Expire();
    }

    public void Fire(Vector2 dir, float speed, int dmg)
    {
        damage = Mathf.Max(1, dmg);
        rb.linearVelocity = dir.normalized * speed;
        
        // 旋转子弹使其尖端面向射出方向
        // 因为子弹Sprite本身是竖向绘制，需要减90度来对齐
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
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
