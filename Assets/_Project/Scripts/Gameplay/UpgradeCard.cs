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

        if (titleText != null)
            titleText.text = upgrade.title;

        if (descriptionText != null)
            descriptionText.text = upgrade.description;

        if (cardImage != null && upgrade.icon != null)
            cardImage.sprite = upgrade.icon;
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
