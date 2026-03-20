using UnityEngine;

public static class PickupSortingUtility
{
    public static void ApplyGameplayPickupSorting(GameObject pickupRoot, SpriteRenderer preferredRenderer = null)
    {
        if (pickupRoot == null)
            return;

        SpriteRenderer[] renderers = pickupRoot.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0)
            return;

        int targetLayerId;
        int targetOrder;
        if (!TryResolvePickupBaselineSorting(out targetLayerId, out targetOrder))
        {
            SpriteRenderer fallback = preferredRenderer != null ? preferredRenderer : renderers[0];
            targetLayerId = fallback != null ? fallback.sortingLayerID : 0;
            targetOrder = fallback != null ? Mathf.Min(fallback.sortingOrder, -1) : -1;
        }

        int minExistingOrder = int.MaxValue;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null)
                continue;

            minExistingOrder = Mathf.Min(minExistingOrder, sr.sortingOrder);
        }

        if (minExistingOrder == int.MaxValue)
            minExistingOrder = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null)
                continue;

            int relativeOrder = sr.sortingOrder - minExistingOrder;
            sr.sortingLayerID = targetLayerId;
            sr.sortingOrder = targetOrder + relativeOrder;
        }
    }

    private static bool TryResolvePickupBaselineSorting(out int sortingLayerId, out int sortingOrder)
    {
        bool found = false;
        sortingLayerId = 0;
        sortingOrder = 0;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (TryGetPrimarySorting(player != null ? player.transform : null, out int playerLayerId, out int playerOrder))
        {
            sortingLayerId = playerLayerId;
            sortingOrder = playerOrder;
            found = true;
        }

        var enemies = EnemyController.ActiveEnemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyController enemy = enemies[i] as EnemyController;
            if (enemy == null)
                continue;

            if (!TryGetPrimarySorting(enemy.transform, out int enemyLayerId, out int enemyOrder))
                continue;

            if (!found || IsBackmost(enemyLayerId, enemyOrder, sortingLayerId, sortingOrder))
            {
                sortingLayerId = enemyLayerId;
                sortingOrder = enemyOrder;
                found = true;
            }
        }

        if (!found)
            return false;

        sortingOrder -= 1;
        return true;
    }

    private static bool TryGetPrimarySorting(Transform root, out int sortingLayerId, out int sortingOrder)
    {
        sortingLayerId = 0;
        sortingOrder = 0;
        if (root == null)
            return false;

        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer best = null;
        float bestArea = float.MinValue;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null || sr.sprite == null)
                continue;

            Bounds bounds = sr.bounds;
            float area = bounds.size.x * bounds.size.y;
            if (area > bestArea)
            {
                best = sr;
                bestArea = area;
            }
        }

        if (best == null)
            return false;

        sortingLayerId = best.sortingLayerID;
        sortingOrder = best.sortingOrder;
        return true;
    }

    private static bool IsBackmost(int candidateLayerId, int candidateOrder, int currentLayerId, int currentOrder)
    {
        int candidateLayerValue = SortingLayer.GetLayerValueFromID(candidateLayerId);
        int currentLayerValue = SortingLayer.GetLayerValueFromID(currentLayerId);

        if (candidateLayerValue != currentLayerValue)
            return candidateLayerValue < currentLayerValue;

        return candidateOrder < currentOrder;
    }
}
