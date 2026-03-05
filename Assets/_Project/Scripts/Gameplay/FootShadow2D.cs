using UnityEngine;

[DisallowMultipleComponent]
public class FootShadow2D : MonoBehaviour
{
    [SerializeField] private Vector2 localOffset = new Vector2(0f, -0.42f);
    [SerializeField] private Vector2 localScale = new Vector2(0.90f, 0.35f);
    [SerializeField, Range(0f, 1f)] private float alpha = 0.32f;
    [SerializeField] private int sortingOrderOffset = -10;
    [SerializeField] private bool syncSortingLayer = true;
    [SerializeField] private SpriteRenderer targetRenderer;

    private const string ShadowObjectName = "FootShadow";
    private static Sprite cachedShadowSprite;

    private Transform shadowTransform;
    private SpriteRenderer shadowRenderer;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        EnsureShadowObject();
        ApplyVisual();
    }

    private void LateUpdate()
    {
        if (shadowTransform == null || shadowRenderer == null)
            EnsureShadowObject();

        ApplyVisual();
    }

    private void EnsureShadowObject()
    {
        Transform existing = transform.Find(ShadowObjectName);
        if (existing == null)
        {
            GameObject go = new GameObject(ShadowObjectName);
            shadowTransform = go.transform;
            shadowTransform.SetParent(transform, false);
        }
        else
        {
            shadowTransform = existing;
        }

        shadowTransform.localPosition = new Vector3(localOffset.x, localOffset.y, 0f);
        shadowTransform.localRotation = Quaternion.identity;
        shadowTransform.localScale = new Vector3(localScale.x, localScale.y, 1f);

        shadowRenderer = shadowTransform.GetComponent<SpriteRenderer>();
        if (shadowRenderer == null)
            shadowRenderer = shadowTransform.gameObject.AddComponent<SpriteRenderer>();

        if (shadowRenderer.sprite == null)
            shadowRenderer.sprite = GetOrCreateShadowSprite();
    }

    private void ApplyVisual()
    {
        if (shadowTransform != null)
        {
            shadowTransform.localPosition = new Vector3(localOffset.x, localOffset.y, 0f);
            shadowTransform.localScale = new Vector3(localScale.x, localScale.y, 1f);
        }

        if (shadowRenderer == null)
            return;

        shadowRenderer.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));

        if (targetRenderer == null)
            return;

        if (syncSortingLayer)
            shadowRenderer.sortingLayerID = targetRenderer.sortingLayerID;

        shadowRenderer.sortingOrder = targetRenderer.sortingOrder + sortingOrderOffset;
    }

    private static Sprite GetOrCreateShadowSprite()
    {
        if (cachedShadowSprite != null)
            return cachedShadowSprite;

        const int width = 64;
        const int height = 32;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float ny = ((y + 0.5f) / height) * 2f - 1f;
            for (int x = 0; x < width; x++)
            {
                float nx = ((x + 0.5f) / width) * 2f - 1f;
                float dist = (nx * nx) + (ny * ny);
                float edge = Mathf.Clamp01(1f - dist);
                float a = edge * edge;
                pixels[y * width + x] = new Color(1f, 1f, 1f, a);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        cachedShadowSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            64f);

        return cachedShadowSprite;
    }
}
