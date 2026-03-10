using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class FootShadow2D : MonoBehaviour
{
    [SerializeField] private Vector2 localOffset = new Vector2(0f, -0.48f);
    [SerializeField] private Vector2 localScale = new Vector2(0.92f, 0.34f);
    [SerializeField, Range(0f, 1f)] private float alpha = 0.24f;
    [SerializeField] private int sortingOrderOffset = -1;
    [SerializeField] private bool syncSortingLayer = true;
    [SerializeField] private bool autoFitToTargetRenderer = true;
    [SerializeField, Min(0f)] private float autoWidthFactor = 0.78f;
    [SerializeField, Min(0f)] private float autoHeightFactor = 0.24f;
    [SerializeField] private float autoFootLift = 0.015f;
    [SerializeField] private string fallbackSortingLayerName = "Default";
    [SerializeField] private int fallbackSortingOrder = 0;
    [SerializeField] private SpriteRenderer targetRenderer;

    private const string ShadowObjectName = "FootShadow";
    private static Sprite cachedShadowSprite;
    private static Material cachedUnlitShadowMaterial;

    private Transform shadowTransform;
    private SpriteRenderer shadowRenderer;

    private void OnEnable()
    {
        NormalizeSettings();
        ResolveTargetRenderer();
        EnsureShadowObject();
        ApplyVisual();
    }

    private void Awake()
    {
        NormalizeSettings();
        ResolveTargetRenderer();

        EnsureShadowObject();
        ApplyVisual();
    }

    private void LateUpdate()
    {
        NormalizeSettings();
        ResolveTargetRenderer();

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

        Material unlit = GetOrCreateShadowMaterial();
        if (unlit != null && shadowRenderer.sharedMaterial != unlit)
            shadowRenderer.sharedMaterial = unlit;
    }

    private void ApplyVisual()
    {
        if (shadowTransform != null)
        {
            if (autoFitToTargetRenderer && targetRenderer != null)
            {
                // Align shadow to the actual feet position even when sprite pivot is centered.
                Bounds b = targetRenderer.bounds;
                Vector3 worldPos = new Vector3(b.center.x, b.min.y + autoFootLift, transform.position.z);
                shadowTransform.position = worldPos;

                float absScaleX = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
                float absScaleY = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
                float fitLocalX = Mathf.Max(0.05f, (b.size.x * autoWidthFactor) / absScaleX);
                float fitLocalY = Mathf.Max(0.05f, (b.size.y * autoHeightFactor) / absScaleY);
                shadowTransform.localScale = new Vector3(fitLocalX, fitLocalY, 1f);
            }
            else
            {
                shadowTransform.localPosition = new Vector3(localOffset.x, localOffset.y, 0f);
                shadowTransform.localScale = new Vector3(localScale.x, localScale.y, 1f);
            }
        }

        if (shadowRenderer == null)
            return;

        shadowRenderer.enabled = true;
        shadowRenderer.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));

        if (targetRenderer != null)
        {
            if (syncSortingLayer)
                shadowRenderer.sortingLayerID = targetRenderer.sortingLayerID;

            shadowRenderer.sortingOrder = targetRenderer.sortingOrder + sortingOrderOffset;
            return;
        }

        if (!string.IsNullOrWhiteSpace(fallbackSortingLayerName))
            shadowRenderer.sortingLayerName = fallbackSortingLayerName;
        shadowRenderer.sortingOrder = fallbackSortingOrder;
    }

    private void ResolveTargetRenderer()
    {
        if (targetRenderer != null)
        {
            bool validHierarchy = targetRenderer.transform == transform || targetRenderer.transform.IsChildOf(transform);
            if (!validHierarchy)
            {
                // Guard against accidentally assigning unrelated renderers (e.g. world/background sprite).
                targetRenderer = null;
            }
            else if (targetRenderer.enabled && targetRenderer.gameObject.activeInHierarchy)
            {
                return;
            }
        }

        SpriteRenderer[] all = GetComponentsInChildren<SpriteRenderer>(true);
        if (all == null || all.Length == 0)
        {
            targetRenderer = null;
            return;
        }

        SpriteRenderer best = null;
        int bestSortingLayer = int.MinValue;
        int bestSortingOrder = int.MinValue;
        float bestArea = -1f;
        for (int i = 0; i < all.Length; i++)
        {
            SpriteRenderer sr = all[i];
            if (sr == null)
                continue;
            if (shadowRenderer != null && sr == shadowRenderer)
                continue;

            if (!sr.enabled || !sr.gameObject.activeInHierarchy)
                continue;

            // Prefer the visually-dominant renderer: higher sorting layer/order, then larger area.
            int sortingLayer = sr.sortingLayerID;
            int sortingOrder = sr.sortingOrder;
            Bounds b = sr.bounds;
            float area = b.size.x * b.size.y;

            bool better = false;
            if (sortingLayer > bestSortingLayer)
                better = true;
            else if (sortingLayer == bestSortingLayer && sortingOrder > bestSortingOrder)
                better = true;
            else if (sortingLayer == bestSortingLayer && sortingOrder == bestSortingOrder && area > bestArea)
                better = true;

            if (better)
            {
                best = sr;
                bestSortingLayer = sortingLayer;
                bestSortingOrder = sortingOrder;
                bestArea = area;
            }
        }

        targetRenderer = best;
    }

    private void OnValidate()
    {
        NormalizeSettings();

        if (!Application.isPlaying)
        {
            ResolveTargetRenderer();
            EnsureShadowObject();
            ApplyVisual();
        }
    }

    private void NormalizeSettings()
    {
        alpha = Mathf.Clamp01(alpha);
        localScale.x = Mathf.Max(0.05f, localScale.x);
        localScale.y = Mathf.Max(0.05f, localScale.y);
        autoWidthFactor = Mathf.Max(0.05f, autoWidthFactor);
        autoHeightFactor = Mathf.Max(0.05f, autoHeightFactor);

        // Old scenes might still carry a very low value like -10 that hides shadow under ground.
        if (sortingOrderOffset < -5)
            sortingOrderOffset = -1;
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

    private static Material GetOrCreateShadowMaterial()
    {
        if (cachedUnlitShadowMaterial != null)
            return cachedUnlitShadowMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            return null;

        cachedUnlitShadowMaterial = new Material(shader)
        {
            name = "Runtime_FootShadow_Unlit"
        };
        return cachedUnlitShadowMaterial;
    }
}
