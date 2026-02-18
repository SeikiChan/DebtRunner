using UnityEngine;

public class EnemyHitKnockback : MonoBehaviour
{
    [Header("Knockback")]
    [SerializeField, Min(0f)] private float hitSpeed = 2.5f;
    [SerializeField, Min(0f)] private float maxKnockbackSpeed = 5f;
    [SerializeField, Min(0f)] private float recoveryPerSecond = 10f;

    private Vector2 knockbackVelocity;

    public void ApplyHit(Vector2 hitDirection, float forceMultiplier = 1f)
    {
        if (hitDirection.sqrMagnitude <= 0.0001f)
            return;

        float scale = Mathf.Max(0f, forceMultiplier);
        knockbackVelocity += hitDirection.normalized * hitSpeed * scale;
        float maxSq = maxKnockbackSpeed * maxKnockbackSpeed;
        if (knockbackVelocity.sqrMagnitude > maxSq)
            knockbackVelocity = knockbackVelocity.normalized * maxKnockbackSpeed;
    }

    public Vector2 GetCurrentVelocity(float fixedDeltaTime)
    {
        if (fixedDeltaTime <= 0f)
            return knockbackVelocity;

        if (recoveryPerSecond > 0f)
            knockbackVelocity = Vector2.MoveTowards(knockbackVelocity, Vector2.zero, recoveryPerSecond * fixedDeltaTime);

        return knockbackVelocity;
    }
}
