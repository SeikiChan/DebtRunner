using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店系统 — 商品购买 + 转盘赌博
/// 所有UI引用通过 Inspector 手动绑定（或通过 ShopPanelTemplate 自动连接）
/// </summary>
public class ShopSystem : MonoBehaviour
{
    [Serializable]
    public class ShopItemUIRefs
    {
        [LocalizedLabel("Title Text / 标题文本")]
        public TMP_Text titleText;
        [LocalizedLabel("Description Text / 描述文本")]
        public TMP_Text descText;
        [LocalizedLabel("Price Text / 价格文本")]
        public TMP_Text priceText;
        [LocalizedLabel("Buy Button / 购买按钮")]
        public Button buyButton;
        [LocalizedLabel("Buy Button Label / 购买按钮文本")]
        public TMP_Text buyButtonLabel;
        [LocalizedLabel("Icon Image / 图标")]
        public Image iconImage;
    }

    [Serializable]
    private class ShopRarityStyle
    {
        public UpgradeRarity rarity = UpgradeRarity.Common;
        [Min(0.01f)] public float priceMultiplier = 1f;
        public Color titleColor = Color.white;
        public Color priceColor = Color.white;
    }

    private class ShopOffer
    {
        public ShopItemDefinition definition;
        public bool purchased;
        public bool isFree;
    }

    [Header("SFX / 音效")]
    [LocalizedLabel("购买音效")]
    [SerializeField] private AudioClip sfxBuy;
    [LocalizedLabel("金钱不足音效")]
    [SerializeField] private AudioClip sfxCantBuy;

    [Header("Costs / 费用")]
    [LocalizedLabel("Gamble Cost / 抽奖费用")]
    [SerializeField] private int gambleCost = 90;
    [LocalizedLabel("Refresh Base Cost / 刷新基础费用")]
    [SerializeField] private int refreshCost = 50;
    [LocalizedLabel("Refresh Cost Increment / 每次刷新涨价")]
    [SerializeField] private int refreshCostIncrement = 30;

    [Header("Gamble Rewards / 赌博奖励")]
    [SerializeField] private int cashRewardMin = 180;
    [SerializeField] private int cashRewardMax = 360;
    [SerializeField] private int debtPenaltyMin = 100;
    [SerializeField] private int debtPenaltyMax = 260;
    [SerializeField] private float enemyHpBuffMultiplier = 1.22f;
    [SerializeField] private float enemySpeedBuffMultiplier = 1.08f;
    [SerializeField] private float enemyRewardBuffMultiplier = 1.5f;
    [SerializeField] private bool useDynamicGambleCost = true;
    [SerializeField, Range(0.35f, 0.95f)] private float gambleCostToOfferPriceRatio = 0.42f;
    [SerializeField, Min(1)] private int gambleCostMin = 35;
    [SerializeField, Min(1)] private int gambleCostMax = 120;
    [SerializeField] private bool enforceWheelRiskModel = true;
    [SerializeField, Range(0f, 1f)] private float wheelPositiveOutcomeChance = 0.68f;
    [SerializeField] private bool wheelCashRefundByCost = false;

    [Header("Shop Item Pool / 商品池")]
    [LocalizedLabel("Shop Item Pool Asset / 商品池资源")]
    [SerializeField] private ShopItemPoolAsset shopItemPoolAsset;

    [Header("Price Scaling / Price Curve")]
    [SerializeField, Min(0.01f)] private float itemBasePriceMultiplier = 1f;
    [SerializeField, Min(0f)] private float itemRoundStepPercent = 0f;
    [SerializeField, Min(0f)] private float refreshRoundStepPercent = 0f;
    [SerializeField] private List<ShopRarityStyle> rarityStyles = new List<ShopRarityStyle>
    {
        new ShopRarityStyle
        {
            rarity = UpgradeRarity.Common,
            priceMultiplier = 1.00f,
            titleColor = new Color(0.93f, 0.93f, 0.93f, 1f),
            priceColor = new Color(0.93f, 0.93f, 0.93f, 1f),
        },
        new ShopRarityStyle
        {
            rarity = UpgradeRarity.Uncommon,
            priceMultiplier = 1.00f,
            titleColor = new Color(0.64f, 1.00f, 0.74f, 1f),
            priceColor = new Color(0.64f, 1.00f, 0.74f, 1f),
        },
        new ShopRarityStyle
        {
            rarity = UpgradeRarity.Rare,
            priceMultiplier = 1.00f,
            titleColor = new Color(0.52f, 0.83f, 1.00f, 1f),
            priceColor = new Color(0.52f, 0.83f, 1.00f, 1f),
        },
        new ShopRarityStyle
        {
            rarity = UpgradeRarity.Epic,
            priceMultiplier = 1.00f,
            titleColor = new Color(1.00f, 0.63f, 0.97f, 1f),
            priceColor = new Color(1.00f, 0.63f, 0.97f, 1f),
        },
        new ShopRarityStyle
        {
            rarity = UpgradeRarity.Legendary,
            priceMultiplier = 1.00f,
            titleColor = new Color(1.00f, 0.84f, 0.43f, 1f),
            priceColor = new Color(1.00f, 0.84f, 0.43f, 1f),
        },
    };

    [Header("UI Binding / UI绑定")]
    [LocalizedLabel("Round Info Text / 轮次信息文本")]
    [SerializeField] private TMP_Text textRoundInfo;
    [LocalizedLabel("Cash Text / 现金文本")]
    [SerializeField] private TMP_Text textCash;
    [LocalizedLabel("Info Text / 信息文本")]
    [SerializeField] private TMP_Text textInfo;
    [LocalizedLabel("Spinning Wheel / 转盘")]
    [SerializeField] private SpinningWheelController spinningWheel;
    [LocalizedLabel("Refresh Button / 刷新按钮")]
    [SerializeField] private Button buttonRefresh;
    [LocalizedLabel("Refresh Label / 刷新按钮文本")]
    [SerializeField] private TMP_Text textRefreshLabel;

    [Header("Item Cards / 商品卡片")]
    [SerializeField] private ShopItemUIRefs[] itemUIs = new ShopItemUIRefs[3];

    private readonly ShopOffer[] currentOffers = new ShopOffer[3];

    private GameFlowController gameFlow;
    private RunProgressionState runProgression;
    private bool uiReady;
    private bool eventsBound;
    private int pendingFreeItemCharges;
    private int runtimeGambleCost;
    private int refreshTimesThisVisit;
    private Color[] defaultTitleColors;
    private Color[] defaultPriceColors;

    public void Bind(GameFlowController flow, RunProgressionState progression)
    {
        gameFlow = flow;
        runProgression = progression;

        EnsureUI();
        BindUiEvents();
        BindSpinningWheel();
        RefreshShopUI();
    }

    public void OpenShop()
    {
        EnsureUI();
        BindUiEvents();
        pendingFreeItemCharges = 0;
        refreshTimesThisVisit = 0;
        GenerateOffers();
        BindSpinningWheel();
        spinningWheel?.OnShopOpened();
        SetInfo($"Spend cash to upgrade. Roll costs ${ResolveRuntimeGambleCost()} — big rewards await!");
        RefreshShopUI();
    }

    public void OnShopClosed()
    {
        MarkOtherShopInteraction();
        if (spinningWheel != null)
            spinningWheel.CancelAndReset(true);
    }

    public void MarkOtherShopInteraction()
    {
        spinningWheel?.MarkOtherShopInteraction();
    }

    public bool IsPrizeDrawInProgress()
    {
        return spinningWheel != null && spinningWheel.IsDrawInProgress;
    }

    public void ShowPrizeInfo(string message)
    {
        SetInfo(message);
    }

    public int AddFreeItemCharges(int amount)
    {
        int v = Mathf.Max(0, amount);
        if (v == 0) return pendingFreeItemCharges;

        pendingFreeItemCharges += v;
        RefreshShopUI();
        return pendingFreeItemCharges;
    }

    public void RefreshShopUI()
    {
        if (!uiReady || gameFlow == null) return;

        if (textRoundInfo != null)
        {
            string nextDebt = gameFlow.GetNextRoundDebtDisplay();
            textRoundInfo.text = $"Round {gameFlow.GetCurrentRound()}/{gameFlow.GetTotalRounds()}    Next Debt: {nextDebt}";
        }

        if (textCash != null)
        {
            if (runProgression != null && pendingFreeItemCharges > 0)
                textCash.text = $"Cash: ${gameFlow.GetCashAmount()}    Free Item x{pendingFreeItemCharges}";
            else
                textCash.text = $"Cash: ${gameFlow.GetCashAmount()}";
        }

        if (textRefreshLabel != null) textRefreshLabel.text = $"Refresh ${GetCurrentRefreshCost()}";

        runtimeGambleCost = ResolveRuntimeGambleCost();

        if (spinningWheel != null)
        {
            spinningWheel.SetDrawCost(runtimeGambleCost);
            spinningWheel.SetRewardConfig(
                cashRewardMin, cashRewardMax,
                debtPenaltyMin, debtPenaltyMax,
                enemyHpBuffMultiplier, enemySpeedBuffMultiplier,
                enemyRewardBuffMultiplier);
            spinningWheel.SetRiskModel(
                enforceWheelRiskModel,
                wheelPositiveOutcomeChance,
                wheelCashRefundByCost);
        }

        for (int i = 0; i < currentOffers.Length; i++)
        {
            if (itemUIs == null || i >= itemUIs.Length || itemUIs[i] == null) continue;

            ShopOffer offer = currentOffers[i];
            ShopItemUIRefs ui = itemUIs[i];
            if (offer == null || offer.definition == null)
            {
                ResetOfferColors(i, ui);
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

            ApplyOfferColors(i, ui, offer.definition.Rarity, offer.purchased);
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
                continue;
            }

            int price = GetOfferPrice(offer);
            bool freeByCharge = pendingFreeItemCharges > 0 && price > 0;
            if (freeByCharge) price = 0;

            if (ui.priceText != null)
                ui.priceText.text = price == 0 ? "FREE" : $"${price}";
            if (ui.buyButtonLabel != null)
                ui.buyButtonLabel.text = price == 0 ? "CLAIM" : "BUY";
            if (ui.buyButton != null)
                ui.buyButton.interactable = true;
        }
    }

    private void BindUiEvents()
    {
        if (!uiReady || eventsBound) return;

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

    private int GetCurrentRefreshCost()
    {
        int baseCost = refreshCost + refreshCostIncrement * refreshTimesThisVisit;
        float scaledCost = baseCost * GetRefreshRoundMultiplier();
        return Mathf.Max(0, Mathf.RoundToInt(scaledCost));
    }

    private void RefreshOffers()
    {
        MarkOtherShopInteraction();
        if (gameFlow == null) return;
        int cost = GetCurrentRefreshCost();
        if (!gameFlow.TrySpendCash(cost))
        {
            SetInfo("Not enough cash to refresh.");
            return;
        }

        refreshTimesThisVisit++;
        GenerateOffers();
        SetInfo($"Shop refreshed. Next refresh: ${GetCurrentRefreshCost()}");
    }

    private void BuyOffer(int index)
    {
        MarkOtherShopInteraction();
        if (gameFlow == null) return;
        if (index < 0 || index >= currentOffers.Length) return;

        ShopOffer offer = currentOffers[index];
        if (offer == null || offer.definition == null || offer.purchased) return;

        int cost = GetOfferPrice(offer);
        bool consumeFreeCharge = pendingFreeItemCharges > 0 && cost > 0;
        int finalCost = consumeFreeCharge ? 0 : cost;
        if (!gameFlow.TrySpendCash(finalCost))
        {
            SetInfo("Not enough cash.");
            if (sfxCantBuy != null && SFXManager.Instance != null)
                SFXManager.Instance.Play(sfxCantBuy);
            return;
        }

        if (sfxBuy != null && SFXManager.Instance != null)
            SFXManager.Instance.Play(sfxBuy);
        gameFlow.ApplyShopItem(offer.definition);
        offer.purchased = true;

        if (consumeFreeCharge)
        {
            pendingFreeItemCharges = Mathf.Max(0, pendingFreeItemCharges - 1);
            SetInfo($"{offer.definition.ItemTitle} claimed for FREE. Remaining free charges: {pendingFreeItemCharges}.");
        }
        else
        {
            SetInfo($"{offer.definition.ItemTitle} acquired.");
        }

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

    private void BindSpinningWheel()
    {
        if (spinningWheel == null)
            spinningWheel = GetComponentInChildren<SpinningWheelController>(true);

        if (spinningWheel == null)
            return;

        runtimeGambleCost = ResolveRuntimeGambleCost();
        spinningWheel.Bind(gameFlow, this);
        spinningWheel.SetDrawCost(runtimeGambleCost);
        spinningWheel.SetRewardConfig(
            cashRewardMin, cashRewardMax,
            debtPenaltyMin, debtPenaltyMax,
            enemyHpBuffMultiplier, enemySpeedBuffMultiplier,
            enemyRewardBuffMultiplier);
        spinningWheel.SetRiskModel(
            enforceWheelRiskModel,
            wheelPositiveOutcomeChance,
            wheelCashRefundByCost);
    }

    private int ResolveRuntimeGambleCost()
    {
        float itemScale = GetItemPriceScaleForCurrentRound();
        int floor = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, gambleCostMin) * itemScale));
        int ceiling = Mathf.Max(floor, Mathf.RoundToInt(Mathf.Max(gambleCostMin, gambleCostMax) * itemScale));
        int baseCost = Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0, gambleCost) * itemScale));
        if (!useDynamicGambleCost)
            return Mathf.Clamp(baseCost, floor, ceiling);

        int sum = 0;
        int count = 0;
        for (int i = 0; i < currentOffers.Length; i++)
        {
            ShopOffer offer = currentOffers[i];
            if (offer == null || offer.definition == null)
                continue;

            sum += Mathf.Max(0, GetOfferPrice(offer));
            count++;
        }

        if (count <= 0)
            return Mathf.Clamp(baseCost, floor, ceiling);

        float avgPrice = sum / (float)count;
        int scaledCost = Mathf.RoundToInt(avgPrice * Mathf.Clamp(gambleCostToOfferPriceRatio, 0.1f, 2f));
        return Mathf.Clamp(scaledCost, floor, ceiling);
    }

    private void EnsureUI()
    {
        if (uiReady) return;

        // 检查关键UI引用是否存在
        if (textCash == null || textInfo == null)
        {
            RunLogger.Warning("ShopSystem: textCash or textInfo not assigned. Check Inspector.");
            return;
        }

        if (itemUIs == null || itemUIs.Length < 3)
        {
            RunLogger.Warning("ShopSystem: itemUIs not fully assigned. Need 3 item card references.");
            return;
        }

        CacheDefaultItemColors();
        uiReady = true;
    }

    private int GetOfferPrice(ShopOffer offer)
    {
        if (offer == null || offer.definition == null)
            return 0;
        if (offer.isFree)
            return 0;

        ShopRarityStyle style = ResolveRarityStyle(offer.definition.Rarity);
        float scaledPrice = Mathf.Max(0, offer.definition.Price) * GetItemPriceScaleForCurrentRound() * style.priceMultiplier;
        return Mathf.Max(0, Mathf.RoundToInt(scaledPrice));
    }

    private float GetItemPriceScaleForCurrentRound()
    {
        int currentRound = gameFlow != null ? Mathf.Max(1, gameFlow.GetCurrentRound()) : 1;
        return Mathf.Max(0.01f, itemBasePriceMultiplier) * (1f + Mathf.Max(0f, itemRoundStepPercent) * (currentRound - 1));
    }

    private float GetRefreshRoundMultiplier()
    {
        int currentRound = gameFlow != null ? Mathf.Max(1, gameFlow.GetCurrentRound()) : 1;
        return 1f + Mathf.Max(0f, refreshRoundStepPercent) * (currentRound - 1);
    }

    private ShopRarityStyle ResolveRarityStyle(UpgradeRarity rarity)
    {
        if (rarityStyles != null)
        {
            for (int i = 0; i < rarityStyles.Count; i++)
            {
                ShopRarityStyle style = rarityStyles[i];
                if (style != null && style.rarity == rarity)
                    return style;
            }
        }

        return new ShopRarityStyle
        {
            rarity = rarity,
            priceMultiplier = 1f,
            titleColor = Color.white,
            priceColor = Color.white,
        };
    }

    private void CacheDefaultItemColors()
    {
        int count = itemUIs != null ? itemUIs.Length : 0;
        defaultTitleColors = new Color[count];
        defaultPriceColors = new Color[count];

        for (int i = 0; i < count; i++)
        {
            ShopItemUIRefs ui = itemUIs[i];
            defaultTitleColors[i] = ui != null && ui.titleText != null ? ui.titleText.color : Color.white;
            defaultPriceColors[i] = ui != null && ui.priceText != null ? ui.priceText.color : Color.white;
        }
    }

    private void ResetOfferColors(int index, ShopItemUIRefs ui)
    {
        if (ui == null)
            return;

        if (ui.titleText != null && defaultTitleColors != null && index >= 0 && index < defaultTitleColors.Length)
            ui.titleText.color = defaultTitleColors[index];
        if (ui.priceText != null && defaultPriceColors != null && index >= 0 && index < defaultPriceColors.Length)
            ui.priceText.color = defaultPriceColors[index];
    }

    private void ApplyOfferColors(int index, ShopItemUIRefs ui, UpgradeRarity rarity, bool purchased)
    {
        if (ui == null)
            return;

        ShopRarityStyle style = ResolveRarityStyle(rarity);
        if (ui.titleText != null)
            ui.titleText.color = style.titleColor;

        if (ui.priceText != null)
        {
            Color priceColor = style.priceColor;
            if (purchased)
                priceColor = Color.Lerp(priceColor, Color.gray, 0.45f);
            ui.priceText.color = priceColor;
        }
    }

    private void SetInfo(string message)
    {
        if (textInfo != null)
            textInfo.text = message;

        if (!string.IsNullOrWhiteSpace(message))
            RunLogger.Event($"Shop: {message}");
    }
}
