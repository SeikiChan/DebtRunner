using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopSystem : MonoBehaviour
{
    [Serializable]
    private class ShopItemUIRefs
    {
        public TMP_Text titleText;
        public TMP_Text descText;
        public TMP_Text priceText;
        public Button buyButton;
        public TMP_Text buyButtonLabel;
        public Image iconImage;
    }

    private class ShopOffer
    {
        public ShopItemDefinition definition;
        public bool purchased;
        public bool isFree;

        public int GetPrice()
        {
            if (definition == null) return 0;
            return isFree ? 0 : definition.Price;
        }
    }

    [Header("Costs")]
    [SerializeField] private int gambleCost = 120;
    [SerializeField] private int refreshCost = 80;

    [Header("Gamble Rewards")]
    [SerializeField] private int cashRewardMin = 120;
    [SerializeField] private int cashRewardMax = 260;
    [SerializeField] private int debtPenaltyMin = 150;
    [SerializeField] private int debtPenaltyMax = 400;
    [SerializeField] private float enemyHpBuffMultiplier = 1.35f;
    [SerializeField] private float enemySpeedBuffMultiplier = 1.15f;

    [Header("Shop Item Pool Asset")]
    [SerializeField] private ShopItemPoolAsset shopItemPoolAsset;

    [Header("UI Binding (Manual Preferred)")]
    [SerializeField] private bool allowAutoBuildUI = true;
    [SerializeField] private RectTransform autoRoot;
    [SerializeField] private TMP_Text textCash;
    [SerializeField] private TMP_Text textInfo;
    [SerializeField] private Button buttonGamble;
    [SerializeField] private TMP_Text textGambleLabel;
    [SerializeField] private Button buttonRefresh;
    [SerializeField] private TMP_Text textRefreshLabel;
    [SerializeField] private ShopItemUIRefs[] itemUIs = new ShopItemUIRefs[3];

    private readonly ShopOffer[] currentOffers = new ShopOffer[3];

    private GameFlowController gameFlow;
    private RunProgressionState runProgression;
    private bool uiReady;
    private bool eventsBound;

    public void Bind(GameFlowController flow, RunProgressionState progression)
    {
        gameFlow = flow;
        runProgression = progression;

        EnsureUI();
        BindUiEvents();
        RefreshShopUI();
    }

    public void OpenShop()
    {
        EnsureUI();
        BindUiEvents();
        GenerateOffers();
        SetInfo("Spend cash to upgrade. Gamble can help or hurt the next round.");
        RefreshShopUI();
    }

    public void RefreshShopUI()
    {
        if (!uiReady || gameFlow == null) return;

        if (textCash != null)
        {
            if (runProgression != null)
            {
                textCash.text = $"Cash: ${gameFlow.GetCashAmount()}   Next Debt +${runProgression.NextRoundDebtIncrease}   Enemy x{runProgression.NextRoundEnemyHpMultiplier:F2}/{runProgression.NextRoundEnemySpeedMultiplier:F2}";
            }
            else
            {
                textCash.text = $"Cash: ${gameFlow.GetCashAmount()}";
            }
        }

        if (textGambleLabel != null) textGambleLabel.text = $"Gamble ${gambleCost}";
        if (textRefreshLabel != null) textRefreshLabel.text = $"Refresh ${refreshCost}";

        for (int i = 0; i < currentOffers.Length; i++)
        {
            if (itemUIs == null || i >= itemUIs.Length || itemUIs[i] == null) continue;

            ShopOffer offer = currentOffers[i];
            ShopItemUIRefs ui = itemUIs[i];
            if (offer == null || offer.definition == null)
            {
                if (ui.titleText != null) ui.titleText.text = "-";
                if (ui.descText != null) ui.descText.text = "No item";
                if (ui.priceText != null) ui.priceText.text = "";
                if (ui.buyButtonLabel != null) ui.buyButtonLabel.text = "N/A";
                if (ui.buyButton != null) ui.buyButton.interactable = false;
                if (ui.iconImage != null)
                {
                    ui.iconImage.sprite = null;
                    ui.iconImage.enabled = false;
                }
                continue;
            }

            if (ui.titleText != null) ui.titleText.text = offer.definition.ItemTitle;
            if (ui.descText != null) ui.descText.text = offer.definition.Description;
            if (ui.iconImage != null)
            {
                ui.iconImage.sprite = offer.definition.Icon;
                ui.iconImage.enabled = offer.definition.Icon != null;
            }

            if (offer.purchased)
            {
                if (ui.priceText != null) ui.priceText.text = "Purchased";
                if (ui.buyButtonLabel != null) ui.buyButtonLabel.text = "OWNED";
                if (ui.buyButton != null) ui.buyButton.interactable = false;
            }
            else
            {
                int price = offer.GetPrice();
                if (ui.priceText != null) ui.priceText.text = price == 0 ? "FREE" : $"${price}";
                if (ui.buyButtonLabel != null) ui.buyButtonLabel.text = price == 0 ? "CLAIM" : "BUY";
                if (ui.buyButton != null) ui.buyButton.interactable = true;
            }
        }
    }

    private void BindUiEvents()
    {
        if (!uiReady || eventsBound) return;

        if (buttonGamble != null)
            buttonGamble.onClick.AddListener(Gamble);

        if (buttonRefresh != null)
            buttonRefresh.onClick.AddListener(RefreshOffers);

        for (int i = 0; i < itemUIs.Length; i++)
        {
            if (itemUIs[i] == null || itemUIs[i].buyButton == null) continue;
            int index = i;
            itemUIs[i].buyButton.onClick.AddListener(() => BuyOffer(index));
        }

        eventsBound = true;
    }

    private void Gamble()
    {
        if (gameFlow == null) return;
        if (!gameFlow.TrySpendCash(gambleCost))
        {
            SetInfo("Not enough cash for gambling.");
            return;
        }

        int roll = UnityEngine.Random.Range(0, 100);
        if (roll < 20)
        {
            GrantFreeOffer();
            return;
        }

        if (roll < 45)
        {
            int reward = UnityEngine.Random.Range(cashRewardMin, cashRewardMax + 1);
            gameFlow.AddCash(reward);
            SetInfo($"Gamble win! You gained ${reward}.");
            return;
        }

        if (roll < 65)
        {
            SetInfo("Thank you for participating.");
            return;
        }

        if (roll < 85)
        {
            int debtUp = UnityEngine.Random.Range(debtPenaltyMin, debtPenaltyMax + 1);
            gameFlow.AddDebtPenaltyToNextRound(debtUp);
            SetInfo($"Bad luck. Next round debt +${debtUp}.");
            return;
        }

        gameFlow.AddEnemyBuffToNextRound(enemyHpBuffMultiplier, enemySpeedBuffMultiplier);
        SetInfo($"Danger ahead. Next round enemies buffed (HP x{enemyHpBuffMultiplier:F2}, Speed x{enemySpeedBuffMultiplier:F2}).");
    }

    private void RefreshOffers()
    {
        if (gameFlow == null) return;
        if (!gameFlow.TrySpendCash(refreshCost))
        {
            SetInfo("Not enough cash to refresh.");
            return;
        }

        GenerateOffers();
        SetInfo("Shop refreshed.");
    }

    private void BuyOffer(int index)
    {
        if (gameFlow == null) return;
        if (index < 0 || index >= currentOffers.Length) return;

        ShopOffer offer = currentOffers[index];
        if (offer == null || offer.definition == null || offer.purchased) return;

        int cost = offer.GetPrice();
        if (!gameFlow.TrySpendCash(cost))
        {
            SetInfo("Not enough cash.");
            return;
        }

        gameFlow.ApplyShopItem(offer.definition);
        offer.purchased = true;
        SetInfo($"{offer.definition.ItemTitle} acquired.");
        RefreshShopUI();
    }

    private void GrantFreeOffer()
    {
        List<int> candidates = new List<int>();
        for (int i = 0; i < currentOffers.Length; i++)
        {
            ShopOffer offer = currentOffers[i];
            if (offer != null && offer.definition != null && !offer.purchased && !offer.isFree)
                candidates.Add(i);
        }

        if (candidates.Count == 0)
        {
            int fallbackReward = UnityEngine.Random.Range(cashRewardMin, cashRewardMax + 1);
            gameFlow.AddCash(fallbackReward);
            SetInfo($"No free slot available. Fallback cash +${fallbackReward}.");
            return;
        }

        int selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        currentOffers[selected].isFree = true;
        SetInfo($"Lucky! Offer #{selected + 1} is now FREE.");
        RefreshShopUI();
    }

    private void GenerateOffers()
    {
        if (shopItemPoolAsset == null || shopItemPoolAsset.Entries == null || shopItemPoolAsset.Entries.Count == 0)
        {
            SetInfo("Shop item pool asset is empty.");
            for (int i = 0; i < currentOffers.Length; i++)
                currentOffers[i] = null;
            RefreshShopUI();
            return;
        }

        List<ShopItemDefinition> picks = WeightedPickerUtility.PickUnique(
            shopItemPoolAsset.Entries,
            currentOffers.Length,
            shopItemPoolAsset.GetEffectiveWeight);

        for (int i = 0; i < currentOffers.Length; i++)
        {
            ShopItemDefinition definition = i < picks.Count ? picks[i] : PickSingleItemByWeight();
            currentOffers[i] = definition == null
                ? null
                : new ShopOffer
                {
                    definition = definition,
                    purchased = false,
                    isFree = false,
                };
        }

        RefreshShopUI();
    }

    private ShopItemDefinition PickSingleItemByWeight()
    {
        if (shopItemPoolAsset == null || shopItemPoolAsset.Entries == null || shopItemPoolAsset.Entries.Count == 0)
            return null;

        List<ShopItemDefinition> one = WeightedPickerUtility.PickUnique(shopItemPoolAsset.Entries, 1, shopItemPoolAsset.GetEffectiveWeight);
        return one.Count > 0 ? one[0] : null;
    }

    private void EnsureUI()
    {
        if (uiReady) return;

        if (!ValidateManualUI())
        {
            if (!allowAutoBuildUI)
            {
                SetInfo("Shop UI references are incomplete.");
                return;
            }

            BuildAutoUI();
            if (!ValidateManualUI())
            {
                RunLogger.Warning("Shop UI creation failed.");
                return;
            }
        }

        uiReady = true;
    }

    private bool ValidateManualUI()
    {
        if (textCash == null || textInfo == null || buttonGamble == null || textGambleLabel == null || buttonRefresh == null || textRefreshLabel == null)
            return false;

        if (itemUIs == null || itemUIs.Length < 3)
            return false;

        for (int i = 0; i < 3; i++)
        {
            ShopItemUIRefs ui = itemUIs[i];
            if (ui == null || ui.titleText == null || ui.descText == null || ui.priceText == null || ui.buyButton == null || ui.buyButtonLabel == null)
                return false;
        }

        return true;
    }

    private void BuildAutoUI()
    {
        if (autoRoot == null)
        {
            RectTransform panelRect = GetComponent<RectTransform>();
            if (panelRect == null) return;
            autoRoot = CreateRect("AutoShopRoot", panelRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(980f, 620f));
        }

        textCash = CreateText("Text_Cash", autoRoot, new Vector2(0f, 250f), new Vector2(920f, 50f), 30f, TextAlignmentOptions.Center, Color.white);
        textInfo = CreateText("Text_Info", autoRoot, new Vector2(0f, -210f), new Vector2(920f, 120f), 28f, TextAlignmentOptions.Center, new Color(0.95f, 0.9f, 0.65f, 1f));
        textInfo.enableWordWrapping = true;

        buttonGamble = CreateButton("Btn_Gamble", autoRoot, new Vector2(-210f, 195f), new Vector2(300f, 54f), out textGambleLabel);
        buttonRefresh = CreateButton("Btn_Refresh", autoRoot, new Vector2(210f, 195f), new Vector2(300f, 54f), out textRefreshLabel);

        itemUIs = new ShopItemUIRefs[3];
        Color cardColor = new Color(0.08f, 0.08f, 0.10f, 0.85f);
        for (int i = 0; i < 3; i++)
        {
            float x = -320f + i * 320f;
            RectTransform card = CreateCard(autoRoot, x, 95f, cardColor);

            Image icon = CreateImage("Icon", card, new Vector2(0f, 98f), new Vector2(72f, 72f), Color.white);
            TMP_Text title = CreateText($"Item{i + 1}_Title", card, new Vector2(0f, 48f), new Vector2(260f, 50f), 32f, TextAlignmentOptions.Center, new Color(0.98f, 0.83f, 0.22f, 1f));
            TMP_Text desc = CreateText($"Item{i + 1}_Desc", card, new Vector2(0f, -2f), new Vector2(260f, 78f), 24f, TextAlignmentOptions.Center, Color.white);
            desc.enableWordWrapping = true;
            TMP_Text price = CreateText($"Item{i + 1}_Price", card, new Vector2(0f, -58f), new Vector2(260f, 40f), 30f, TextAlignmentOptions.Center, new Color(0.62f, 1f, 0.62f, 1f));
            Button buy = CreateButton($"Item{i + 1}_Buy", card, new Vector2(0f, -108f), new Vector2(180f, 42f), out TMP_Text buyLabel);

            itemUIs[i] = new ShopItemUIRefs
            {
                iconImage = icon,
                titleText = title,
                descText = desc,
                priceText = price,
                buyButton = buy,
                buyButtonLabel = buyLabel,
            };
        }

        SetInfo("Shop ready.");
    }

    private void SetInfo(string message)
    {
        if (textInfo != null)
            textInfo.text = message;

        if (!string.IsNullOrWhiteSpace(message))
            RunLogger.Event($"Shop: {message}");
    }

    private RectTransform CreateCard(Transform parent, float x, float y, Color color)
    {
        RectTransform rect = CreateRect("Card", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(280f, 260f));
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private Button CreateButton(string name, Transform parent, Vector2 pos, Vector2 size, out TMP_Text label)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.95f, 0.95f, 0.95f, 0.92f);
        Button button = rect.gameObject.AddComponent<Button>();

        label = CreateText("Label", rect, Vector2.zero, size, 24f, TextAlignmentOptions.Center, new Color(0.16f, 0.16f, 0.16f, 1f));
        return button;
    }

    private Image CreateImage(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text CreateText(string name, Transform parent, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align, Color color)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.text = name;
        text.fontSize = fontSize;
        text.alignment = align;
        text.color = color;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;
        return text;
    }

    private RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        return rect;
    }
}
