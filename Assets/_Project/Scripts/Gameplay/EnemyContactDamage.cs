using UnityEngine;

[DisallowMultipleComponent]
public class EnemyContactDamage : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Min(0f)] private float hitCooldown = 0.7f;
    [SerializeField] private bool requirePlayerTag = true;

    private float nextHitAt;

    private void OnDisable()
    {
        nextHitAt = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDealDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDealDamage(other);
    }

    private void TryDealDamage(Collider2D other)
    {
        if (other == null)
            return;
        if (hitCooldown > 0f && Time.time < nextHitAt)
            return;
        if (requirePlayerTag && !other.CompareTag("Player"))
            return;

        PlayerHealth hp = other.GetComponent<PlayerHealth>();
        if (hp == null)
            hp = other.GetComponentInParent<PlayerHealth>();
        if (hp == null)
            return;

        hp.TakeDamage(Mathf.Max(1, damage));
        nextHitAt = Time.time + Mathf.Max(0f, hitCooldown);
    }
}
