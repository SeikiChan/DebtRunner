using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 升级奖励卡牌
/// </summary>
public class UpgradeCard : MonoBehaviour
{
    [SerializeField] private Image cardImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button selectButton;
    [SerializeField, Range(0.5f, 1f)] private float iconFitPadding = 0.86f;

    private WeaponUpgrade upgradeData;
    private System.Action<WeaponUpgrade> onSelected;
    private bool interactable = true;
    private UpgradeCardHoverLight hoverLight;
    private RectTransform cardImageRect;
    private Vector2 cardImageBaseSize;
    private bool hasCachedCardImageSize;

    private void Awake()
    {
        hoverLight = GetComponent<UpgradeCardHoverLight>();
        CacheCardImageLayout();
    }

    private void Start()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(OnCardSelected);
    }

    public void SetupCard(WeaponUpgrade upgrade, System.Action<WeaponUpgrade> onSelect)
    {
        upgradeData = upgrade;
        onSelected = onSelect;
        SetInteractable(true);

        if (hoverLight == null)
            hoverLight = GetComponent<UpgradeCardHoverLight>();

        if (hoverLight != null)
            hoverLight.SetRarity(upgrade != null ? upgrade.rarity : UpgradeRarity.Common);

        if (titleText != null)
            titleText.text = upgrade != null ? upgrade.title : string.Empty;

        if (descriptionText != null)
            descriptionText.text = upgrade != null ? FormatUpgradeDescription(upgrade) : string.Empty;

        ApplyCardIcon(upgrade != null ? upgrade.icon : null);
    }

    private void OnCardSelected()
    {
        if (!interactable)
            return;

        onSelected?.Invoke(upgradeData);
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        if (selectButton != null)
            selectButton.interactable = value;
    }

    private void CacheCardImageLayout()
    {
        if (cardImage == null)
            return;

        cardImageRect = cardImage.rectTransform;
        if (cardImageRect == null)
            return;

        if (hasCachedCardImageSize)
            return;

        cardImageBaseSize = cardImageRect.sizeDelta;
        hasCachedCardImageSize = true;
    }

    private void ApplyCardIcon(Sprite icon)
    {
        if (cardImage == null)
            return;

        CacheCardImageLayout();

        cardImage.sprite = icon;
        cardImage.enabled = icon != null;
        cardImage.preserveAspect = true;

        if (cardImageRect == null || !hasCachedCardImageSize)
            return;

        cardImageRect.sizeDelta = cardImageBaseSize;
        if (icon == null)
            return;

        float spriteWidth = Mathf.Max(1f, icon.rect.width);
        float spriteHeight = Mathf.Max(1f, icon.rect.height);
        float aspect = spriteWidth / spriteHeight;

        float maxWidth = Mathf.Max(1f, cardImageBaseSize.x * Mathf.Clamp(iconFitPadding, 0.5f, 1f));
        float maxHeight = Mathf.Max(1f, cardImageBaseSize.y * Mathf.Clamp(iconFitPadding, 0.5f, 1f));

        float fittedWidth = maxWidth;
        float fittedHeight = fittedWidth / aspect;
        if (fittedHeight > maxHeight)
        {
            fittedHeight = maxHeight;
            fittedWidth = fittedHeight * aspect;
        }

        cardImageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fittedWidth);
        cardImageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fittedHeight);
    }

    private string FormatUpgradeDescription(WeaponUpgrade upgrade)
    {
        if (upgrade == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(upgrade.description))
            return upgrade.description;

        if (upgrade.effects == null || upgrade.effects.Count == 0)
            return upgrade.description ?? string.Empty;

        List<string> tokens = new List<string>(4);
        for (int i = 0; i < upgrade.effects.Count; i++)
        {
            WeaponUpgradeEffect effect = upgrade.effects[i];
            if (effect == null)
                continue;

            string token = GetEffectToken(effect.effectType);
            if (string.IsNullOrEmpty(token) || tokens.Contains(token))
                continue;

            tokens.Add(token);
        }

        return tokens.Count > 0 ? string.Join(" / ", tokens) : (upgrade.description ?? string.Empty);
    }

    private string GetEffectToken(WeaponUpgradeEffectType effectType)
    {
        switch (effectType)
        {
            case WeaponUpgradeEffectType.DamageAdd:
                return "ATK+";
            case WeaponUpgradeEffectType.FireRateAdd:
                return "ASPD UP";
            case WeaponUpgradeEffectType.ProjectileSpeedAdd:
                return "SPD UP";
            case WeaponUpgradeEffectType.ExtraProjectilesAdd:
                return "SHOT+";
            case WeaponUpgradeEffectType.SpreadAngleAdd:
                return "WIDE UP";
            case WeaponUpgradeEffectType.PierceAdd:
                return "PIERCE+";
            case WeaponUpgradeEffectType.KnockbackMultiplierAdd:
                return "KB UP";
            case WeaponUpgradeEffectType.OnHitScatterCountAdd:
                return "BLOOM+";
            case WeaponUpgradeEffectType.OnHitScatterAngleAdd:
                return "BLOOM UP";
            case WeaponUpgradeEffectType.OrbitProjectileCountAdd:
                return "RING+";
            case WeaponUpgradeEffectType.OrbitRadiusAdd:
            case WeaponUpgradeEffectType.OrbitAngularSpeedAdd:
                return "RING UP";
            case WeaponUpgradeEffectType.NovaProjectileCountAdd:
            case WeaponUpgradeEffectType.NovaIntervalAdd:
                return "WAVE+";
            case WeaponUpgradeEffectType.ReturnEnable:
            case WeaponUpgradeEffectType.ReturnSpeedMultiplierAdd:
                return "RETURN";
            default:
                return string.Empty;
        }
    }
}
