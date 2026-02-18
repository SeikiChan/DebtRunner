using UnityEngine;

[System.Serializable]
public class RarityWeightConfig
{
    public UpgradeRarity rarity = UpgradeRarity.Common;
    [Min(0f)] public float weightPercent = 1f;
}
