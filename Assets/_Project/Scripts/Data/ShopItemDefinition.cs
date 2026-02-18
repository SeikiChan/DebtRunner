using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "DebtRunner/Shop/Shop Item Definition", fileName = "ShopItem_")]
public class ShopItemDefinition : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private string itemTitle = "New Shop Item";
    [SerializeField, TextArea] private string description = "";
    [SerializeField] private Sprite icon;

    [Header("Pricing & Drop")]
    [SerializeField, Min(0)] private int price = 120;
    [SerializeField] private UpgradeRarity rarity = UpgradeRarity.Common;
    [SerializeField, Min(0f)] private float weightPercent = 10f;

    [Header("Effects")]
    [SerializeField] private List<ShopItemEffect> effects = new List<ShopItemEffect>();

    public string ItemTitle => itemTitle;
    public string Description => description;
    public Sprite Icon => icon;
    public int Price => Mathf.Max(0, price);
    public UpgradeRarity Rarity => rarity;
    public float WeightPercent => Mathf.Max(0f, weightPercent);
    public IReadOnlyList<ShopItemEffect> Effects => effects;
}
