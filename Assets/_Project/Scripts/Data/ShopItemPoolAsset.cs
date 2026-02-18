using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "DebtRunner/Shop/Shop Item Pool", fileName = "ShopItemPool")]
public class ShopItemPoolAsset : ScriptableObject
{
    [Header("Rarity Multipliers")]
    [SerializeField] private List<RarityWeightConfig> rarityWeights = new List<RarityWeightConfig>
    {
        new RarityWeightConfig { rarity = UpgradeRarity.Common, weightPercent = 1.00f },
        new RarityWeightConfig { rarity = UpgradeRarity.Uncommon, weightPercent = 0.60f },
        new RarityWeightConfig { rarity = UpgradeRarity.Rare, weightPercent = 0.30f },
        new RarityWeightConfig { rarity = UpgradeRarity.Epic, weightPercent = 0.14f },
        new RarityWeightConfig { rarity = UpgradeRarity.Legendary, weightPercent = 0.06f },
    };

    [Header("Entries")]
    [SerializeField] private List<ShopItemDefinition> entries = new List<ShopItemDefinition>();

    public IReadOnlyList<ShopItemDefinition> Entries => entries;

    public float GetEffectiveWeight(ShopItemDefinition entry)
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
