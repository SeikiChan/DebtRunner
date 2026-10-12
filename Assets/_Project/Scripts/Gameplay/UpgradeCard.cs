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
    private TMP_FontAsset originalTitleFont;
    private Material originalTitleMaterial;
    private FontStyles originalTitleStyle;
    private float originalTitleSpacing;
    private TMP_FontAsset originalDescriptionFont;
    private Material originalDescriptionMaterial;
    private FontStyles originalDescriptionStyle;
    private float originalDescriptionSpacing;

    private void Awake()
    {
        hoverLight = GetComponent<UpgradeCardHoverLight>();
        CacheCardImageLayout();
        if (titleText != null)
        {
            originalTitleFont = titleText.font;
            originalTitleMaterial = titleText.fontSharedMaterial;
            originalTitleStyle = titleText.fontStyle;
            originalTitleSpacing = titleText.characterSpacing;
        }
        if (descriptionText != null)
        {
            originalDescriptionFont = descriptionText.font;
            originalDescriptionMaterial = descriptionText.fontSharedMaterial;
            originalDescriptionStyle = descriptionText.fontStyle;
            originalDescriptionSpacing = descriptionText.characterSpacing;
        }
    }

    private void Start()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(OnCardSelected);
    }

    private void OnEnable()
    {
        LocalizationTable.LocaleChanged += RefreshLocalizedText;
    }

    private void OnDisable()
    {
        LocalizationTable.LocaleChanged -= RefreshLocalizedText;
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

        RefreshLocalizedText();

        ApplyCardIcon(upgrade != null ? upgrade.icon : null);
    }

    private void RefreshLocalizedText()
    {
        if (titleText != null)
        {
            string title = upgradeData != null ? upgradeData.title : string.Empty;
            titleText.text = upgradeData != null && !string.IsNullOrWhiteSpace(upgradeData.titleLocalizationKey)
                ? LocalizationTable.Get(upgradeData.titleLocalizationKey, title)
                : title;
            ApplyTitleTypography();
        }

        if (descriptionText != null)
        {
            string description = upgradeData != null ? FormatUpgradeDescription(upgradeData) : string.Empty;
            descriptionText.text = upgradeData != null && !string.IsNullOrWhiteSpace(upgradeData.descriptionLocalizationKey)
                ? LocalizationTable.Get(upgradeData.descriptionLocalizationKey, description)
                : description;
            ApplyDescriptionTypography();
        }
    }

    private void ApplyTitleTypography()
    {
        if (LocalizationTable.CurrentLocale != "zh-CN")
        {
            titleText.font = originalTitleFont;
            titleText.fontSharedMaterial = originalTitleMaterial;
            titleText.fontStyle = originalTitleStyle;
            titleText.characterSpacing = originalTitleSpacing;
            return;
        }

        TMP_FontAsset displayFont = Resources.Load<TMP_FontAsset>("Localization/ZixiaohunLiLiangCuHei SDF");
        if (displayFont != null)
        {
            titleText.font = displayFont;
            titleText.fontSharedMaterial = displayFont.material;
        }
        titleText.fontStyle = FontStyles.Normal;
        titleText.characterSpacing = 4f;
    }

    private void ApplyDescriptionTypography()
    {
        if (LocalizationTable.CurrentLocale != "zh-CN")
        {
            descriptionText.font = originalDescriptionFont;
            descriptionText.fontSharedMaterial = originalDescriptionMaterial;
            descriptionText.fontStyle = originalDescriptionStyle;
            descriptionText.characterSpacing = originalDescriptionSpacing;
            return;
        }

        TMP_FontAsset chineseFont = Resources.Load<TMP_FontAsset>("Localization/ZixiaohunLiLiangCuHei SDF");
        if (chineseFont != null)
        {
            descriptionText.font = chineseFont;
            descriptionText.fontSharedMaterial = chineseFont.material;
        }
        descriptionText.fontStyle = FontStyles.Normal;
        descriptionText.characterSpacing = 2f;
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
                return LocalizationTable.Get("upgrade.effect.damage", "攻击+");
            case WeaponUpgradeEffectType.FireRateAdd:
                return LocalizationTable.Get("upgrade.effect.fire_rate", "攻速↑");
            case WeaponUpgradeEffectType.ProjectileSpeedAdd:
                return LocalizationTable.Get("upgrade.effect.projectile_speed", "移速↑");
            case WeaponUpgradeEffectType.ExtraProjectilesAdd:
                return LocalizationTable.Get("upgrade.effect.projectiles", "弹幕+");
            case WeaponUpgradeEffectType.SpreadAngleAdd:
                return LocalizationTable.Get("upgrade.effect.spread", "扩散↑");
            case WeaponUpgradeEffectType.PierceAdd:
                return LocalizationTable.Get("upgrade.effect.pierce", "穿透+");
            case WeaponUpgradeEffectType.KnockbackMultiplierAdd:
                return LocalizationTable.Get("upgrade.effect.knockback", "击退↑");
            case WeaponUpgradeEffectType.OnHitScatterCountAdd:
                return LocalizationTable.Get("upgrade.effect.scatter_count", "爆裂+");
            case WeaponUpgradeEffectType.OnHitScatterAngleAdd:
                return LocalizationTable.Get("upgrade.effect.scatter_angle", "爆裂↑");
            case WeaponUpgradeEffectType.OrbitProjectileCountAdd:
                return LocalizationTable.Get("upgrade.effect.orbit_count", "环绕+");
            case WeaponUpgradeEffectType.OrbitRadiusAdd:
            case WeaponUpgradeEffectType.OrbitAngularSpeedAdd:
                return LocalizationTable.Get("upgrade.effect.orbit_power", "环绕↑");
            case WeaponUpgradeEffectType.NovaProjectileCountAdd:
            case WeaponUpgradeEffectType.NovaIntervalAdd:
                return LocalizationTable.Get("upgrade.effect.nova", "波动+");
            case WeaponUpgradeEffectType.ReturnEnable:
            case WeaponUpgradeEffectType.ReturnSpeedMultiplierAdd:
                return LocalizationTable.Get("upgrade.effect.return", "回旋");
            default:
                return string.Empty;
        }
    }
}
