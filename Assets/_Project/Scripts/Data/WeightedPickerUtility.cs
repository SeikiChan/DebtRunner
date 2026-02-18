using System;
using System.Collections.Generic;
using UnityEngine;

public static class WeightedPickerUtility
{
    public static List<T> PickUnique<T>(IReadOnlyList<T> source, int count, Func<T, float> getWeight) where T : class
    {
        List<T> result = new List<T>();
        if (source == null || count <= 0 || getWeight == null)
            return result;

        List<T> candidates = new List<T>();
        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] != null)
                candidates.Add(source[i]);
        }

        while (result.Count < count && candidates.Count > 0)
        {
            int pickedIndex = PickIndex(candidates, getWeight);
            if (pickedIndex < 0)
                break;

            result.Add(candidates[pickedIndex]);
            candidates.RemoveAt(pickedIndex);
        }

        return result;
    }

    private static int PickIndex<T>(IReadOnlyList<T> source, Func<T, float> getWeight) where T : class
    {
        float total = 0f;
        for (int i = 0; i < source.Count; i++)
            total += Mathf.Max(0f, getWeight(source[i]));

        if (total <= 0.0001f)
            return -1;

        float roll = UnityEngine.Random.Range(0f, total);
        float cursor = 0f;
        for (int i = 0; i < source.Count; i++)
        {
            cursor += Mathf.Max(0f, getWeight(source[i]));
            if (roll <= cursor)
                return i;
        }

        return source.Count - 1;
    }
}
