using UnityEngine;

/// <summary>
/// 武器升级数据结构
/// </summary>
[System.Serializable]
public class WeaponUpgrade
{
    public string title;              // 升级名称
    public string description;        // 描述
    public Sprite icon;               // 图标
    public int upgradePower = 1;      // 伤害增加
    public float upgradeFireRate = 0; // 攻速增加（0表示无变化）
    public float upgradeSpeed = 0;    // 弹速增加（0表示无变化）

    public WeaponUpgrade(string title, string desc, Sprite icon, int power = 1, float fireRate = 0, float speed = 0)
    {
        this.title = title;
        this.description = desc;
        this.icon = icon;
        this.upgradePower = power;
        this.upgradeFireRate = fireRate;
        this.upgradeSpeed = speed;
    }
}
