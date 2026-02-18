using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "DebtRunner/Upgrades/Weapon Upgrade Pool", fileName = "WeaponUpgradePool")]
public class WeaponUpgradePoolAsset : ScriptableObject
{
    [Header("Rarity Multipliers")]
    [SerializeField] private List<RarityWeightConfig> rarityWeights = new List<RarityWeightConfig>
    {
        new RarityWeightConfig { rarity = UpgradeRarity.Common, weightPercent = 1.00f },
        new RarityWeightConfig { rarity = UpgradeRarity.Uncommon, weightPercent = 0.55f },
        new RarityWeightConfig { rarity = UpgradeRarity.Rare, weightPercent = 0.28f },
        new RarityWeightConfig { rarity = UpgradeRarity.Epic, weightPercent = 0.12f },
        new RarityWeightConfig { rarity = UpgradeRarity.Legendary, weightPercent = 0.05f },
    };

    [Header("Entries")]
    [SerializeField] private List<WeaponUpgradeDefinition> entries = new List<WeaponUpgradeDefinition>();

    public IReadOnlyList<WeaponUpgradeDefinition> Entries => entries;

    public float GetEffectiveWeight(WeaponUpgradeDefinition entry)
    {
        if (entry == null) return 0f;
        float rarityWeight = GetRarityWeight(entry.Rarity);
        return Mathf.Max(0f, entry.WeightPercent) * rarityWeight;
    }

    private float GetRarityWeight(UpgradeRarity rarity)
    {
        if (rarityWeights == null || rarityWeights.Count == 0)
            return 1f;

        for (int i = 0; i < rarityWeights.Count; i++)
        {
            RarityWeightConfig config = rarityWeights[i];
            if (config == null) continue;
            if (config.rarity == rarity)
                return Mathf.Max(0f, config.weightPercent);
        }

        return 1f;
    }
}
