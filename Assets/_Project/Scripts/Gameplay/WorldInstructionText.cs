using TMPro;
using UnityEngine;

public class WorldInstructionText : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float lifetime = 6f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.15f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.45f;
    [SerializeField, Min(0.01f)] private float textScale = 0.18f;
    [SerializeField, Min(0.1f)] private float textSize = 5.5f;
    [SerializeField, Min(0f)] private float pulseAmplitude = 0.035f;
    [SerializeField, Min(0f)] private float pulseSpeed = 2.4f;
    [SerializeField, Range(0f, 1f)] private float outlineWidth = 0.24f;
    [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.95f);

    private TextMeshPro textMesh;
    private Material textMaterial;
    private Color baseColor;
    private Color baseOutlineColor;
    private float timer;
    private bool completing;
    private float completeTimer;
    private float completeDuration = 0.75f;
    private Color completeColor = new Color(0.32f, 1f, 0.42f, 1f);
    private Camera cachedCamera;
    private Vector3 baseScale = Vector3.one;
    private Transform followTarget;
    private Vector3 followOffset;
    private bool persistUntilCompleted;

    public bool IsCompleting => completing;

    public static WorldInstructionText Spawn(
        string content,
        Vector3 worldPos,
        Color color,
        TMP_FontAsset font,
        float fontSize,
        float scale,
        float life,
        Transform anchorTarget = null,
        Vector3 anchorOffset = default,
        bool persistUntilComplete = false)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        GameObject go = new GameObject("WorldInstructionText");
        go.transform.position = worldPos;

        WorldInstructionText instruction = go.AddComponent<WorldInstructionText>();
        instruction.Initialize(content, color, font, fontSize, scale, life, anchorTarget, anchorOffset, persistUntilComplete);
        return instruction;
    }

    private void Initialize(
        string content,
        Color color,
        TMP_FontAsset font,
        float fontSize,
        float scale,
        float life,
        Transform anchorTarget,
        Vector3 anchorOffset,
        bool persistUntilComplete)
    {
        lifetime = Mathf.Max(0.05f, life);
        textScale = Mathf.Max(0.01f, scale);
        textSize = Mathf.Max(0.1f, fontSize);
        persistUntilCompleted = persistUntilComplete;
        followTarget = anchorTarget;
        followOffset = anchorOffset;
        fadeOutDuration = Mathf.Clamp(fadeOutDuration, 0.01f, lifetime);
        fadeInDuration = Mathf.Clamp(fadeInDuration, 0f, lifetime);

        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null)
            textMesh = gameObject.AddComponent<TextMeshPro>();

        textMesh.text = content;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;
        textMesh.overflowMode = TextOverflowModes.Overflow;
        textMesh.fontSize = textSize;
        textMesh.sortingOrder = 280;
        textMesh.richText = false;

        if (font != null)
            textMesh.font = font;
        else if (TMP_Settings.defaultFontAsset != null)
            textMesh.font = TMP_Settings.defaultFontAsset;

        baseColor = color;
        baseOutlineColor = outlineColor;
        baseScale = Vector3.one * textScale;
        transform.localScale = baseScale;
        RefreshFollowPosition();

        ApplyStyle();
        ApplyColor(baseColor, 0f);
        RefreshFacing();
    }

    private void LateUpdate()
    {
        if (textMesh == null)
            return;

        float dt = Time.deltaTime;
        timer += dt;
        RefreshFollowPosition();
        RefreshFacing();
        UpdatePulse();
        UpdateVisual(dt);

        if (ShouldDestroyAfterLifetime())
            Destroy(gameObject);
    }

    private bool ShouldDestroyAfterLifetime()
    {
        if (persistUntilCompleted && !completing)
            return false;

        return timer >= lifetime;
    }

    private void RefreshFollowPosition()
    {
        if (followTarget != null)
            transform.position = followTarget.position + followOffset;
    }

    private void RefreshFacing()
    {
        if (cachedCamera == null)
            cachedCamera = Camera.main;

        if (cachedCamera == null)
            return;

        transform.rotation = cachedCamera.transform.rotation;
    }

    private void UpdatePulse()
    {
        float pulse = 1f + (Mathf.Sin(timer * pulseSpeed) * pulseAmplitude);
        transform.localScale = baseScale * pulse;
    }

    public void Complete(Color color, float fadeDuration)
    {
        if (completing)
            return;

        completing = true;
        completeTimer = 0f;
        completeColor = color;
        completeDuration = Mathf.Max(0.05f, fadeDuration);
        lifetime = Mathf.Max(timer + completeDuration, timer + 0.01f);
    }

    private void UpdateVisual(float dt)
    {
        if (completing)
        {
            completeTimer += dt;
            float t = Mathf.Clamp01(completeTimer / completeDuration);
            Color color = Color.Lerp(baseColor, completeColor, t);
            ApplyColor(color, 1f - t);
            return;
        }

        float alpha = 1f;

        if (fadeInDuration > 0f && timer < fadeInDuration)
            alpha *= Mathf.Clamp01(timer / fadeInDuration);

        if (!persistUntilCompleted)
        {
            float fadeStart = Mathf.Max(0f, lifetime - fadeOutDuration);
            if (timer >= fadeStart)
                alpha *= Mathf.InverseLerp(lifetime, fadeStart, timer);
        }

        ApplyColor(baseColor, alpha);
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

    private void ApplyColor(Color color, float alphaMultiplier)
    {
        if (textMesh == null)
            return;

        Color faceColor = color;
        faceColor.a *= Mathf.Clamp01(alphaMultiplier);
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
