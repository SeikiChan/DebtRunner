using TMPro;
using UnityEngine;

public class WorldPopupText : MonoBehaviour
{
    [SerializeField, Min(0f)] private float riseSpeed = 1.5f;
    [SerializeField, Min(0.05f)] private float lifetime = 0.9f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.35f;
    [SerializeField, Min(0.1f)] private float textScale = 0.12f;
    [SerializeField, Min(0.1f)] private float textSize = 6f;
    [SerializeField, Range(0f, 1f)] private float outlineWidth = 0.22f;
    [SerializeField] private Color outlineColor = new Color(0.08f, 0.03f, 0.03f, 0.95f);

    private TextMeshPro textMesh;
    private Color baseColor;
    private Color baseOutlineColor;
    private float timer;
    private Camera cachedCamera;
    private bool initialized;
    private Material textMaterial;

    public static void Spawn(string content, Vector3 worldPos, Color color)
    {
        Spawn(content, worldPos, color, null, 6f, 0.12f, 1.5f, 0.9f, 0.35f);
    }

    public static void Spawn(
        string content,
        Vector3 worldPos,
        Color color,
        TMP_FontAsset font,
        float fontSize,
        float scale,
        float rise,
        float life,
        float fadeOut)
    {
        if (string.IsNullOrEmpty(content))
            return;

        GameObject popupObject = new GameObject("WorldPopupText");
        popupObject.transform.position = worldPos;

        WorldPopupText popup = popupObject.AddComponent<WorldPopupText>();
        popup.Initialize(content, color, font, fontSize, scale, rise, life, fadeOut);
    }

    private void Awake()
    {
        EnsureInitializedFromExistingText();
    }

    private void Initialize(
        string content,
        Color color,
        TMP_FontAsset font,
        float fontSize,
        float scale,
        float rise,
        float life,
        float fadeOut)
    {
        riseSpeed = Mathf.Max(0f, rise);
        lifetime = Mathf.Max(0.05f, life);
        fadeOutDuration = Mathf.Clamp(fadeOut, 0.01f, lifetime);
        textScale = Mathf.Max(0.01f, scale);
        textSize = Mathf.Max(0.1f, fontSize);

        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null)
            textMesh = gameObject.AddComponent<TextMeshPro>();

        textMesh.text = content;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.enableWordWrapping = false;
        textMesh.overflowMode = TextOverflowModes.Overflow;
        textMesh.fontSize = textSize;
        textMesh.sortingOrder = 300;

        if (font != null)
            textMesh.font = font;
        else if (TMP_Settings.defaultFontAsset != null)
            textMesh.font = TMP_Settings.defaultFontAsset;

        baseColor = color;
        baseOutlineColor = outlineColor;
        ApplyStyle();
        ApplyFaceColor(baseColor);
        transform.localScale = Vector3.one * textScale;
        initialized = true;
        RefreshFacing();
    }

    private void LateUpdate()
    {
        EnsureInitializedFromExistingText();
        if (textMesh == null || !initialized)
            return;

        timer += Time.deltaTime;
        transform.position += Vector3.up * (riseSpeed * Time.deltaTime);
        RefreshFacing();

        float fadeStartTime = Mathf.Max(0f, lifetime - fadeOutDuration);
        if (timer >= fadeStartTime)
        {
            float alpha = Mathf.InverseLerp(lifetime, fadeStartTime, timer);
            Color c = baseColor;
            c.a *= alpha;
            ApplyFaceColor(c);
        }

        if (timer >= lifetime)
            Destroy(gameObject);
    }

    private void RefreshFacing()
    {
        if (cachedCamera == null)
            cachedCamera = Camera.main;

        if (cachedCamera == null)
            return;

        transform.rotation = cachedCamera.transform.rotation;
    }

    private void EnsureInitializedFromExistingText()
    {
        if (initialized)
            return;

        if (textMesh == null)
            textMesh = GetComponent<TextMeshPro>();

        if (textMesh == null)
            return;

        if (string.IsNullOrWhiteSpace(textMesh.text))
            textMesh.text = "+$0";

        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.enableWordWrapping = false;
        textMesh.overflowMode = TextOverflowModes.Overflow;

        baseColor = textMesh.color;
        if (baseColor.a <= 0f)
            baseColor = Color.white;

        baseOutlineColor = outlineColor;
        ApplyStyle();
        ApplyFaceColor(baseColor);
        if (transform.localScale == Vector3.zero)
            transform.localScale = Vector3.one * textScale;

        initialized = true;
    }

    private void ApplyStyle()
    {
        if (textMesh == null)
            return;

        textMaterial = textMesh.fontMaterial;
        if (textMaterial == null)
            return;

        if (textMaterial.HasProperty(ShaderUtilities.ID_OutlineWidth))
            textMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, Mathf.Clamp01(outlineWidth));
    }

    private void ApplyFaceColor(Color faceColor)
    {
        if (textMesh == null)
            return;

        textMesh.color = faceColor;

        if (textMaterial == null)
            textMaterial = textMesh.fontMaterial;
        if (textMaterial == null)
            return;

        if (textMaterial.HasProperty(ShaderUtilities.ID_OutlineColor))
        {
            Color currentOutline = baseOutlineColor;
            currentOutline.a *= faceColor.a;
            textMaterial.SetColor(ShaderUtilities.ID_OutlineColor, currentOutline);
        }
    }
}
