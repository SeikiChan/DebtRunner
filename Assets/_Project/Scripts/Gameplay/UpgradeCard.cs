using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
            descriptionText.text = upgrade != null ? upgrade.description : string.Empty;

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
}
