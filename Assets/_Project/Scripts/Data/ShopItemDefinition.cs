using System.Collections.Generic;
using System.Text;
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

    public bool TryResolveActiveItemId(out ActiveItemId itemId)
    {
        itemId = ActiveItemId.None;

        bool hasActiveItemEffect = false;
        ActiveItemId serializedItemId = ActiveItemId.None;
        if (effects != null)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                ShopItemEffect effect = effects[i];
                if (effect == null || effect.effectType != ShopItemEffectType.EquipActiveItem)
                    continue;

                hasActiveItemEffect = true;
                serializedItemId = (ActiveItemId)effect.intValue;
                break;
            }
        }

        if (!hasActiveItemEffect)
            return false;

        if (TryResolveActiveItemIdFromTitle(itemTitle, out ActiveItemId titleResolvedId))
        {
            itemId = titleResolvedId;
            return true;
        }

        itemId = serializedItemId;
        return itemId != ActiveItemId.None;
    }

    private static bool TryResolveActiveItemIdFromTitle(string title, out ActiveItemId itemId)
    {
        switch (NormalizeActiveItemTitle(title))
        {
            case "skiptraceburst":
                itemId = ActiveItemId.SkiptraceBurst;
                return true;
            case "redlineovertime":
                itemId = ActiveItemId.RedlineOvertime;
                return true;
            case "gracewindow":
                itemId = ActiveItemId.GraceWindow;
                return true;
            default:
                itemId = ActiveItemId.None;
                return false;
        }
    }

    private static string NormalizeActiveItemTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        StringBuilder builder = new StringBuilder(title.Length);
        for (int i = 0; i < title.Length; i++)
        {
            char c = title[i];
            if (char.IsLetterOrDigit(c))
                builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }
}
