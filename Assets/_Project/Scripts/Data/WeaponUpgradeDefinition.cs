using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "DebtRunner/Upgrades/Weapon Upgrade Definition", fileName = "WeaponUpgrade_")]
public class WeaponUpgradeDefinition : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private string upgradeTitle = "New Weapon Upgrade";
    [SerializeField, TextArea] private string description = "";
    [SerializeField] private Sprite icon;

    [Header("Drop")]
    [SerializeField] private UpgradeRarity rarity = UpgradeRarity.Common;
    [SerializeField, Min(0f)] private float weightPercent = 10f;

    [Header("Effects")]
    [SerializeField] private List<WeaponUpgradeEffect> effects = new List<WeaponUpgradeEffect>();

    public string UpgradeTitle => upgradeTitle;
    public string Description => description;
    public Sprite Icon => icon;
    public UpgradeRarity Rarity => rarity;
    public float WeightPercent => Mathf.Max(0f, weightPercent);
    public IReadOnlyList<WeaponUpgradeEffect> Effects => effects;

    public WeaponUpgrade CreateRuntimeUpgrade()
    {
        WeaponUpgrade upgrade = new WeaponUpgrade(upgradeTitle, description, icon);
        upgrade.rarity = rarity;
        upgrade.effects = new List<WeaponUpgradeEffect>();

        if (effects == null)
            return upgrade;

        for (int i = 0; i < effects.Count; i++)
        {
            WeaponUpgradeEffect src = effects[i];
            if (src == null) continue;

            upgrade.effects.Add(new WeaponUpgradeEffect
            {
                effectType = src.effectType,
                intValue = src.intValue,
                floatValue = src.floatValue,
            });
        }

        return upgrade;
    }
}
