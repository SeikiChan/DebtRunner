using UnityEngine;

public static class PickupMagnetUtility
{
    public static bool UpdateMagnetMotion(
        Transform pickupTransform,
        Transform player,
        float magnetRadius,
        float magnetSpeed,
        float autoCollectDistanceSqr,
        ref float funnelEndTime,
        ref bool denseFunnelCompleted)
    {
        if (pickupTransform == null || player == null)
            return false;

        Vector2 toPlayer = (Vector2)player.position - (Vector2)pickupTransform.position;
        float sqrDistToPlayer = toPlayer.sqrMagnitude;
        if (sqrDistToPlayer <= autoCollectDistanceSqr)
            return true;

        float effectiveRadius = Mathf.Max(0f, magnetRadius);
        if (sqrDistToPlayer > effectiveRadius * effectiveRadius)
            return false;

        Vector3 moveTarget = player.position;
        float moveSpeed = Mathf.Max(0f, magnetSpeed);

        GameFlowController flow = GameFlowController.Instance;
        Vector3 funnelTarget = Vector3.zero;
        float holdSeconds = 0f;
        float speedMultiplier = 1f;
        float arrivalDistance = 0f;
        bool hasDenseFunnel = flow != null &&
            flow.TryGetPickupFunnelTarget(out funnelTarget, out holdSeconds, out speedMultiplier, out arrivalDistance);

        if (hasDenseFunnel && !denseFunnelCompleted)
        {
            float now = Time.unscaledTime;
            if (funnelEndTime < 0f)
                funnelEndTime = now + holdSeconds;

            float arriveDistanceSqr = Mathf.Max(0f, arrivalDistance) * Mathf.Max(0f, arrivalDistance);
            Vector2 toFunnel = (Vector2)funnelTarget - (Vector2)pickupTransform.position;

            if (toFunnel.sqrMagnitude <= arriveDistanceSqr || now >= funnelEndTime)
            {
                denseFunnelCompleted = true;
                funnelEndTime = -1f;
            }
            else
            {
                moveTarget = funnelTarget;
                moveSpeed *= Mathf.Max(0.1f, speedMultiplier);
            }
        }
        else if (!hasDenseFunnel)
        {
            funnelEndTime = -1f;
        }

        pickupTransform.position = Vector2.MoveTowards(
            pickupTransform.position,
            moveTarget,
            moveSpeed * Time.deltaTime);

        Vector2 updatedToPlayer = (Vector2)player.position - (Vector2)pickupTransform.position;
        return updatedToPlayer.sqrMagnitude <= autoCollectDistanceSqr;
    }
}
