using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponUpgrade
{
    public string title;
    public string description;
    public Sprite icon;
    public UpgradeRarity rarity = UpgradeRarity.Common;
    public WeaponUpgradeTrackType trackType = WeaponUpgradeTrackType.Legacy;
    public WeaponModeId modeId = WeaponModeId.None;
    public WeaponBaseUpgradeId baseUpgradeId = WeaponBaseUpgradeId.None;
    public int rank = 0;
    public List<WeaponUpgradeEffect> effects = new List<WeaponUpgradeEffect>();

    public WeaponUpgrade(string title, string desc, Sprite icon)
    {
        this.title = title;
        this.description = desc;
        this.icon = icon;
    }
}
