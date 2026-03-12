using UnityEngine;

public static class RuntimeSpriteFactory
{
    private static Sprite shieldRingSprite;
    private static Sprite hitPulseSprite;

    public static Sprite GetShieldRingSprite()
    {
        if (shieldRingSprite == null)
            shieldRingSprite = CreateRingSprite("RuntimeShieldRing", 96, 0.62f, 0.94f);

        return shieldRingSprite;
    }

    public static Sprite GetHitPulseSprite()
    {
        if (hitPulseSprite == null)
            hitPulseSprite = CreateSoftDiscSprite("RuntimeHitPulse", 64, 0.92f);

        return hitPulseSprite;
    }

    private static Sprite CreateRingSprite(string spriteName, int size, float innerRadiusNormalized, float outerRadiusNormalized)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) * 0.5f;
        float maxRadius = size * 0.5f;
        float inner = Mathf.Clamp01(innerRadiusNormalized) * maxRadius;
        float outer = Mathf.Clamp(Mathf.Max(innerRadiusNormalized + 0.02f, outerRadiusNormalized), 0f, 1f) * maxRadius;

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt((dx * dx) + (dy * dy));
                float alpha = 0f;
                if (dist <= outer)
                {
                    float outerFade = Mathf.InverseLerp(outer, outer - 3f, dist);
                    float innerFade = dist < inner ? Mathf.InverseLerp(inner - 3f, inner, dist) : 1f;
                    alpha = Mathf.Clamp01(outerFade * innerFade);
                }

                pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
        sprite.name = spriteName;
        return sprite;
    }

    private static Sprite CreateSoftDiscSprite(string spriteName, int size, float radiusNormalized)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) * 0.5f;
        float radius = Mathf.Clamp01(radiusNormalized) * size * 0.5f;

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt((dx * dx) + (dy * dy));
                float alpha = Mathf.Clamp01(1f - (dist / Mathf.Max(1f, radius)));
                alpha *= alpha;
                pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
        sprite.name = spriteName;
        return sprite;
    }
}
