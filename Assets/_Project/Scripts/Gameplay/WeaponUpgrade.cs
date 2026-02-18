using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponUpgrade
{
    public string title;
    public string description;
    public Sprite icon;
    public UpgradeRarity rarity = UpgradeRarity.Common;
    public List<WeaponUpgradeEffect> effects = new List<WeaponUpgradeEffect>();

    // Legacy fields kept for inspector backward-compatibility.
    public int upgradePower = 0;
    public float upgradeFireRate = 0f;
    public float upgradeSpeed = 0f;

    public WeaponUpgrade(string title, string desc, Sprite icon, int power = 0, float fireRate = 0f, float speed = 0f)
    {
        this.title = title;
        this.description = desc;
        this.icon = icon;
        this.upgradePower = power;
        this.upgradeFireRate = fireRate;
        this.upgradeSpeed = speed;
        ConvertLegacyStatsToEffects();
    }

    public void ConvertLegacyStatsToEffects()
    {
        if (effects == null)
            effects = new List<WeaponUpgradeEffect>();

        if (effects.Count > 0)
            return;

        if (upgradePower != 0)
            effects.Add(new WeaponUpgradeEffect { effectType = WeaponUpgradeEffectType.DamageAdd, intValue = upgradePower });
        if (upgradeFireRate != 0f)
            effects.Add(new WeaponUpgradeEffect { effectType = WeaponUpgradeEffectType.FireRateAdd, floatValue = upgradeFireRate });
        if (upgradeSpeed != 0f)
            effects.Add(new WeaponUpgradeEffect { effectType = WeaponUpgradeEffectType.ProjectileSpeedAdd, floatValue = upgradeSpeed });
    }
}
