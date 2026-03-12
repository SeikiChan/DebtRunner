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

    private WeaponUpgrade upgradeData;
    private System.Action<WeaponUpgrade> onSelected;
    private bool interactable = true;
    private UpgradeCardHoverLight hoverLight;

    private void Awake()
    {
        hoverLight = GetComponent<UpgradeCardHoverLight>();
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

        if (cardImage != null)
            cardImage.sprite = upgrade != null ? upgrade.icon : null;
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

    private string FormatUpgradeDescription(WeaponUpgrade upgrade)
    {
        if (upgrade == null)
            return string.Empty;

        upgrade.ConvertLegacyStatsToEffects();
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
                return "RATE UP";
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
                return "WAVE+";
            default:
                return string.Empty;
        }
    }
}
