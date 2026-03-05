using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Card hover visual for level-up choices: top lamp glow + border highlight.
/// </summary>
public class UpgradeCardHoverLight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [System.Serializable]
    private class RarityLampStyle
    {
        public UpgradeRarity rarity = UpgradeRarity.Common;
        public Color lampOff = new Color(1f, 0.88f, 0.30f, 0.10f);
        public Color lampOn = new Color(1f, 0.95f, 0.45f, 0.96f);
        public Color frameOff = new Color(0.70f, 0.70f, 0.74f, 1f);
        public Color frameOn = new Color(1f, 0.95f, 0.62f, 1f);
    }

    [SerializeField] private Image lampGlow;
    [SerializeField] private Image cardFrame;
    [SerializeField] private RectTransform scaleTarget;

    [Header("Rarity Styles")]
    [SerializeField] private List<RarityLampStyle> rarityStyles = new List<RarityLampStyle>
    {
        new RarityLampStyle
        {
            rarity = UpgradeRarity.Common,
            lampOff = new Color(0.78f, 0.78f, 0.78f, 0.08f),
            lampOn = new Color(0.95f, 0.95f, 0.95f, 0.85f),
            frameOff = new Color(0.62f, 0.62f, 0.64f, 1f),
            frameOn = new Color(0.90f, 0.90f, 0.92f, 1f),
        },
        new RarityLampStyle
        {
            rarity = UpgradeRarity.Uncommon,
            lampOff = new Color(0.52f, 1f, 0.62f, 0.10f),
            lampOn = new Color(0.64f, 1f, 0.70f, 0.92f),
            frameOff = new Color(0.42f, 0.76f, 0.49f, 1f),
            frameOn = new Color(0.62f, 1f, 0.72f, 1f),
        },
        new RarityLampStyle
        {
            rarity = UpgradeRarity.Rare,
            lampOff = new Color(0.47f, 0.72f, 1f, 0.10f),
            lampOn = new Color(0.58f, 0.80f, 1f, 0.94f),
            frameOff = new Color(0.35f, 0.58f, 0.90f, 1f),
            frameOn = new Color(0.58f, 0.80f, 1f, 1f),
        },
        new RarityLampStyle
        {
            rarity = UpgradeRarity.Epic,
            lampOff = new Color(0.78f, 0.58f, 1f, 0.10f),
            lampOn = new Color(0.86f, 0.68f, 1f, 0.95f),
            frameOff = new Color(0.63f, 0.42f, 0.88f, 1f),
            frameOn = new Color(0.86f, 0.68f, 1f, 1f),
        },
        new RarityLampStyle
        {
            rarity = UpgradeRarity.Legendary,
            lampOff = new Color(1f, 0.70f, 0.28f, 0.12f),
            lampOn = new Color(1f, 0.84f, 0.42f, 0.98f),
            frameOff = new Color(0.88f, 0.56f, 0.22f, 1f),
            frameOn = new Color(1f, 0.84f, 0.42f, 1f),
        },
    };

    [Header("Scale")]
    [SerializeField, Min(1f)] private float hoverScale = 1.035f;
    [SerializeField, Min(1f)] private float scaleLerpSpeed = 14f;

    private bool isHovered;
    private Vector3 baseScale = Vector3.one;
    private Color lampOffColor;
    private Color lampOnColor;
    private Color frameOffColor;
    private Color frameOnColor;

    private void Awake()
    {
        if (scaleTarget == null)
            scaleTarget = transform as RectTransform;

        if (scaleTarget != null)
            baseScale = scaleTarget.localScale;

        SetRarity(UpgradeRarity.Common);
        ApplyVisualState(false, true);
    }

    private void OnEnable()
    {
        isHovered = false;
        ApplyVisualState(false, true);
    }

    private void OnDisable()
    {
        isHovered = false;
        ApplyVisualState(false, true);
    }

    private void Update()
    {
        if (scaleTarget == null)
            return;

        float targetMul = isHovered ? hoverScale : 1f;
        Vector3 targetScale = baseScale * targetMul;
        float t = 1f - Mathf.Exp(-scaleLerpSpeed * Time.unscaledDeltaTime);
        scaleTarget.localScale = Vector3.Lerp(scaleTarget.localScale, targetScale, t);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        ApplyVisualState(true, false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        ApplyVisualState(false, false);
    }

    public void SetRarity(UpgradeRarity rarity)
    {
        RarityLampStyle style = ResolveStyle(rarity);
        lampOffColor = style.lampOff;
        lampOnColor = style.lampOn;
        frameOffColor = style.frameOff;
        frameOnColor = style.frameOn;
        ApplyVisualState(isHovered, false);
    }

    private RarityLampStyle ResolveStyle(UpgradeRarity rarity)
    {
        if (rarityStyles != null)
        {
            for (int i = 0; i < rarityStyles.Count; i++)
            {
                RarityLampStyle style = rarityStyles[i];
                if (style == null)
                    continue;
                if (style.rarity == rarity)
                    return style;
            }
        }

        // Fallback if style list is empty or missing an entry.
        return new RarityLampStyle
        {
            rarity = rarity,
            lampOff = new Color(1f, 0.88f, 0.30f, 0.10f),
            lampOn = new Color(1f, 0.95f, 0.45f, 0.96f),
            frameOff = new Color(0.70f, 0.70f, 0.74f, 1f),
            frameOn = new Color(1f, 0.95f, 0.62f, 1f),
        };
    }

    private void ApplyVisualState(bool hovered, bool immediateScale)
    {
        if (lampGlow != null)
            lampGlow.color = hovered ? lampOnColor : lampOffColor;

        if (cardFrame != null)
            cardFrame.color = hovered ? frameOnColor : frameOffColor;

        if (immediateScale && scaleTarget != null)
            scaleTarget.localScale = baseScale * (hovered ? hoverScale : 1f);
    }
}
