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
        [NonSerialized] public RectTransform iconRect;
        [NonSerialized] public Vector2 iconBaseSize;
        [NonSerialized] public bool hasCachedIconSize;
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
    [LocalizedLabel("Refresh Cost Multiplier / 刷新费用倍率")]
    [SerializeField, Min(1f)] private float refreshCostMultiplier = 2f;

    [Header("Gamble Rewards / 赌博奖励")]
    [SerializeField] private int cashRewardMin = 180;
    [SerializeField] private int cashRewardMax = 360;
    [SerializeField] private int debtPenaltyMin = 100;
    [SerializeField] private int debtPenaltyMax = 260;
    [SerializeField] private float enemyHpBuffMultiplier = 1.22f;
    [SerializeField] private float enemySpeedBuffMultiplier = 1.08f;
    [SerializeField] private float enemyRewardBuffMultiplier = 1.25f;
    [SerializeField] private bool useDynamicGambleCost = true;
    [SerializeField, Range(0.35f, 0.95f)] private float gambleCostToOfferPriceRatio = 0.42f;
    [SerializeField, Min(1)] private int gambleCostMin = 35;
    [SerializeField, Min(1)] private int gambleCostMax = 120;
    [SerializeField, Min(0f)] private float gambleCostPerDrawPercent = 0.25f;
    [SerializeField, Min(0)] private int gambleCostFlatIncreasePerDraw = 15;
    [SerializeField, Min(1)] private int gambleCostVisitHardCap = 900;
    [SerializeField] private bool enforceWheelRiskModel = true;
    [SerializeField, Range(0f, 1f)] private float wheelPositiveOutcomeChance = 0.68f;
    [SerializeField] private bool wheelCashRefundByCost = false;
    [Header("Wheel Round Scaling / 杞缁忔祹缂╂斁")]
    [SerializeField, Range(0.01f, 1f)] private float wheelCashRewardMinDebtRatioEarly = 0.18f;
    [SerializeField, Range(0.01f, 1f)] private float wheelCashRewardMaxDebtRatioEarly = 0.30f;
    [SerializeField, Range(0.01f, 1f)] private float wheelCashRewardMinDebtRatioLate = 0.24f;
    [SerializeField, Range(0.01f, 1f)] private float wheelCashRewardMaxDebtRatioLate = 0.40f;
    [SerializeField, Range(0.01f, 1f)] private float wheelDebtPenaltyMinDebtRatioEarly = 0.05f;
    [SerializeField, Range(0.01f, 1f)] private float wheelDebtPenaltyMaxDebtRatioEarly = 0.09f;
    [SerializeField, Range(0.01f, 1f)] private float wheelDebtPenaltyMinDebtRatioLate = 0.09f;
    [SerializeField, Range(0.01f, 1f)] private float wheelDebtPenaltyMaxDebtRatioLate = 0.15f;
    [SerializeField, Min(2)] private int wheelScalingPeakRound = 8;
    [SerializeField, Min(1)] private int wheelDebtPenaltyAbsoluteMin = 25;

    [Header("Shop Item Pool / 商品池")]
    [LocalizedLabel("Shop Item Pool Asset / 商品池资源")]
    [SerializeField] private ShopItemPoolAsset shopItemPoolAsset;
    [SerializeField] private ShopItemPoolAsset preBossInvestmentPoolAsset;

    [Header("Price Scaling / Price Curve")]
    [SerializeField, Min(0.01f)] private float itemBasePriceMultiplier = 1f;
    [SerializeField, Min(0f)] private float itemRoundStepPercent = 0.12f;
    [SerializeField, Min(0f)] private float refreshRoundStepPercent = 0f;
    [SerializeField, Min(1)] private int lateRoundPriceSurgeStartRound = 6;
    [SerializeField, Min(0f)] private float lateRoundPriceSurgePercent = 0.25f;
    [SerializeField, Min(1)] private int lateRoundRefreshSurgeStartRound = 6;
    [SerializeField, Min(0f)] private float lateRoundRefreshSurgePercent = 0.18f;
    [Header("Pre-Boss Investment Pricing")]
    [SerializeField] private bool useFixedPreBossInvestmentPricing = true;
    [SerializeField, Min(0.01f)] private float preBossInvestmentPriceMultiplier = 1f;
    [SerializeField, Min(1)] private int preBossInvestmentMinPrice = 3500;
    [SerializeField, Min(1)] private int preBossInvestmentMaxPrice = 9800;
    [SerializeField] private bool usePreBossInvestmentShop = true;
    [SerializeField] private string preBossShopInfoMessage = "趁头目战开始前，用现金强化装备，做好准备！";
    [SerializeField] private string preBossRoundLabel = "头目战准备";
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
    [SerializeField, Min(0f)] private float cashIconTextGap = 14f;
    [SerializeField, Min(0f)] private float roundInfoIconSectionGap = 26f;
    [SerializeField, Min(0f)] private float roundInfoIconTextGap = 12f;
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

    [Header("Offer Text Layout / 商品文案布局")]
    [SerializeField, Min(80f)] private float offerTitleMaxWidth = 220f;
    [SerializeField, Min(80f)] private float offerDescriptionMaxWidth = 220f;
    [SerializeField, Min(8f)] private float offerTitleMinFontSize = 15f;
    [SerializeField, Min(8f)] private float offerTitleMaxFontSize = 29f;
    [SerializeField, Min(8f)] private float offerDescriptionMinFontSize = 12f;
    [SerializeField, Min(8f)] private float offerDescriptionMaxFontSize = 22f;
    [SerializeField, Range(0.5f, 1f)] private float offerIconFitPadding = 0.86f;

    private readonly ShopOffer[] currentOffers = new ShopOffer[3];

    private GameFlowController gameFlow;
    private bool uiReady;
    private bool eventsBound;
    private int pendingFreeItemCharges;
    private int pendingFreeRefreshCharges;
    private bool receivedFreeItemThisVisit;
    private int runtimeGambleCost;
    private int gambleDrawsThisVisit;
    private int refreshTimesThisVisit;
    private Color[] defaultTitleColors;
    private Color[] defaultPriceColors;
    private RectTransform cashIconRect;
    private RectTransform roundInfoIconRect;
    private TextMeshProUGUI nextRoundDebtText;

    private static string L(string key, string fallback) => LocalizationTable.Get(key, fallback);
    private static string F(string key, string fallback, params object[] args) =>
        string.Format(L(key, fallback), args);

    private void OnEnable()
    {
        LocalizationTable.LocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationTable.LocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged()
    {
        RefreshShopUI();
        if (gameObject.activeInHierarchy)
            SetInfo(L("shop.info.browse", "花费现金购买强化。"));
    }

    public void Bind(GameFlowController flow)
    {
        gameFlow = flow;

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
        pendingFreeRefreshCharges = 0;
        receivedFreeItemThisVisit = false;
        gambleDrawsThisVisit = 0;
        refreshTimesThisVisit = 0;
        GenerateOffers();
        bool isPreBossShop = IsPreBossInvestmentShopActive();
        ShopItemPoolAsset activePool = GetActiveShopPool();
        SetSpinningWheelVisible(!isPreBossShop);
        if (!isPreBossShop)
        {
            BindSpinningWheel();
            spinningWheel?.OnShopOpened();
        }
        RunLogger.Event($"Shop opened. round={gameFlow?.GetCurrentRound() ?? -1}, bossPrep={isPreBossShop}, pool={(activePool != null ? activePool.name : "<null>")}");
        if (isPreBossShop)
            SetInfo(L("shop.info.pre_boss", preBossShopInfoMessage));
        else if (gameFlow != null && gameFlow.GetCurrentRound() == 1)
            SetInfo(L("shop.info.first_draw", "花费现金购买强化。本次商店的首次抽奖免费。"));
        else
            SetInfo(F("shop.info.draw_cost", "花费现金购买强化。抽奖需 ${0}，每次抽奖后价格都会上涨。", ResolveRuntimeGambleCost()));
        RefreshShopUI();
    }

    public void OnShopClosed()
    {
        MarkOtherShopInteraction();
        if (spinningWheel != null)
        {
            spinningWheel.CancelAndReset(true);
            SetSpinningWheelVisible(true);
        }
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

    public bool TryCommitWheelDraw(bool isFreeDraw, out int chargedCost)
    {
        chargedCost = 0;
        if (gameFlow == null)
            return false;

        if (!isFreeDraw)
        {
            chargedCost = GetCurrentGambleCost();
            if (chargedCost > 0 && gameFlow.GetCashAmount() < chargedCost)
                return false;
        }

        gambleDrawsThisVisit++;
        runtimeGambleCost = ResolveRuntimeGambleCost();

        if (chargedCost <= 0)
        {
            RefreshShopUI();
            return true;
        }

        if (gameFlow.TrySpendCash(chargedCost))
            return true;

        gambleDrawsThisVisit = Mathf.Max(0, gambleDrawsThisVisit - 1);
        runtimeGambleCost = ResolveRuntimeGambleCost();
        RefreshShopUI();
        return false;
    }

    public int GetCurrentGambleCost()
    {
        runtimeGambleCost = ResolveRuntimeGambleCost();
        return runtimeGambleCost;
    }

    public int AddFreeItemCharges(int amount)
    {
        int v = Mathf.Max(0, amount);
        if (v == 0) return pendingFreeItemCharges;

        pendingFreeItemCharges += v;
        receivedFreeItemThisVisit = true;
        TryGrantDeadlockFreeRefresh();
        RefreshShopUI();
        return pendingFreeItemCharges;
    }

    public void RefreshShopUI()
    {
        if (!uiReady || gameFlow == null) return;

        Transform nextRoundButton = transform.Find("Content/Btn_NextRound");
        if (nextRoundButton != null)
            LocalizationTable.SetText(nextRoundButton.GetComponentInChildren<TMP_Text>(true), "shop.next_round", "下一回合");

        ApplyCashTextLayout();

        if (textRoundInfo != null)
        {
            string nextDebt = gameFlow.GetNextRoundDebtDisplay();
            string leftSegment = IsPreBossInvestmentShopActive()
                ? L("shop.round.pre_boss", preBossRoundLabel)
                : F("shop.round.normal", "第 {0}/{1} 回合", gameFlow.GetCurrentRound(), gameFlow.GetTotalRounds());
            string rightSegment = IsPreBossInvestmentShopActive()
                ? L("shop.next.boss", "下一战：头目战")
                : F("shop.next.debt", "下回合债务：{0}", nextDebt);
            ApplyRoundInfoTextLayout(leftSegment, rightSegment);
        }

        if (textCash != null)
        {
            List<string> extras = new List<string>(2);
            if (pendingFreeItemCharges > 0)
                extras.Add(F("shop.free_items", "免费商品 ×{0}", pendingFreeItemCharges));
            if (pendingFreeRefreshCharges > 0)
                extras.Add(F("shop.free_refreshes", "免费刷新 ×{0}", pendingFreeRefreshCharges));

            textCash.text = extras.Count > 0
                ? $"${gameFlow.GetCashAmount()}    {string.Join("    ", extras)}"
                : $"${gameFlow.GetCashAmount()}";
        }

        if (textRefreshLabel != null)
            textRefreshLabel.text = pendingFreeRefreshCharges > 0 ? L("shop.free", "免费") : $" ${GetCurrentRefreshCost()}";

        runtimeGambleCost = ResolveRuntimeGambleCost();

        if (spinningWheel != null)
        {
            spinningWheel.SetDrawCost(runtimeGambleCost);
            ApplySpinningWheelRuntimeConfig();
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
                if (ui.descText != null) ui.descText.text = L("shop.empty", "暂无商品");
                ApplyOfferTextLayout(ui);
                if (ui.priceText != null) ui.priceText.text = "";
                if (ui.buyButtonLabel != null) ui.buyButtonLabel.text = L("shop.unavailable", "不可用");
                if (ui.buyButton != null) ui.buyButton.interactable = false;
                ApplyOfferIcon(ui, null);
                continue;
            }

            ApplyOfferColors(i, ui, offer.definition.Rarity, offer.purchased);
            if (ui.titleText != null) ui.titleText.text = offer.definition.LocalizedTitle;
            if (ui.descText != null) ui.descText.text = offer.definition.LocalizedDescription;
            ApplyOfferTextLayout(ui);
            ApplyOfferIcon(ui, offer.definition.Icon);

            if (offer.purchased)
            {
                if (ui.priceText != null) ui.priceText.text = L("shop.purchased", "已购买");
                if (ui.buyButtonLabel != null) ui.buyButtonLabel.text = L("shop.bought", "已购");
                if (ui.buyButton != null) ui.buyButton.interactable = false;
                continue;
            }

            int price = GetOfferPrice(offer);
            bool freeByCharge = pendingFreeItemCharges > 0 && price > 0;
            if (freeByCharge) price = 0;

            if (ui.priceText != null)
                ui.priceText.text = price == 0 ? L("shop.free", "免费") : $"${price}";
            if (ui.buyButtonLabel != null)
                ui.buyButtonLabel.text = price == 0 ? L("shop.claim", "领取") : L("shop.buy", "购买");
            if (ui.buyButton != null)
                ui.buyButton.interactable = true;
        }
    }

    private void ApplyOfferTextLayout(ShopItemUIRefs ui)
    {
        if (ui == null)
            return;

        ConfigureOfferText(
            ui.titleText,
            offerTitleMaxWidth,
            offerTitleMinFontSize,
            offerTitleMaxFontSize);
        ConfigureOfferText(
            ui.descText,
            offerDescriptionMaxWidth,
            offerDescriptionMinFontSize,
            offerDescriptionMaxFontSize);
    }

    private void ApplyOfferIcon(ShopItemUIRefs ui, Sprite icon)
    {
        if (ui == null || ui.iconImage == null)
            return;

        CacheOfferIconLayout(ui);

        ui.iconImage.sprite = icon;
        ui.iconImage.enabled = icon != null;
        ui.iconImage.preserveAspect = true;

        if (ui.iconRect == null || !ui.hasCachedIconSize)
            return;

        ui.iconRect.sizeDelta = ui.iconBaseSize;
        if (icon == null)
            return;

        float spriteWidth = Mathf.Max(1f, icon.rect.width);
        float spriteHeight = Mathf.Max(1f, icon.rect.height);
        float aspect = spriteWidth / spriteHeight;

        float maxWidth = Mathf.Max(1f, ui.iconBaseSize.x * Mathf.Clamp(offerIconFitPadding, 0.5f, 1f));
        float maxHeight = Mathf.Max(1f, ui.iconBaseSize.y * Mathf.Clamp(offerIconFitPadding, 0.5f, 1f));

        float fittedWidth = maxWidth;
        float fittedHeight = fittedWidth / aspect;
        if (fittedHeight > maxHeight)
        {
            fittedHeight = maxHeight;
            fittedWidth = fittedHeight * aspect;
        }

        ui.iconRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fittedWidth);
        ui.iconRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fittedHeight);
    }

    private static void CacheOfferIconLayout(ShopItemUIRefs ui)
    {
        if (ui == null || ui.iconImage == null || ui.hasCachedIconSize)
            return;

        ui.iconRect = ui.iconImage.rectTransform;
        if (ui.iconRect == null)
            return;

        ui.iconBaseSize = ui.iconRect.sizeDelta;
        ui.hasCachedIconSize = true;
    }

    private static void ConfigureOfferText(TMP_Text text, float maxWidth, float minFontSize, float maxFontSize)
    {
        if (text == null)
            return;

        RectTransform rect = text.rectTransform;
        if (rect != null)
        {
            Vector2 size = rect.sizeDelta;
            if (size.x > maxWidth)
                rect.sizeDelta = new Vector2(maxWidth, size.y);
        }

        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Min(minFontSize, maxFontSize);
        text.fontSizeMax = Mathf.Max(minFontSize, maxFontSize);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
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
        double baseCost = Mathf.Max(0, refreshCost);
        double multiplier = Mathf.Max(1f, refreshCostMultiplier);
        double visitScale = Math.Pow(multiplier, Mathf.Max(0, refreshTimesThisVisit));
        double scaledCost = baseCost * visitScale * GetRefreshRoundMultiplier();

        if (scaledCost >= int.MaxValue)
            return int.MaxValue;

        return Mathf.Max(0, Mathf.RoundToInt((float)scaledCost));
    }

    private void RefreshOffers()
    {
        MarkOtherShopInteraction();
        if (gameFlow == null) return;
        int cost = pendingFreeRefreshCharges > 0 ? 0 : GetCurrentRefreshCost();
        if (!gameFlow.TrySpendCash(cost))
        {
            SetInfo(L("shop.info.insufficient_refresh", "现金不足，无法刷新。"));
            return;
        }

        bool consumedFreeRefresh = pendingFreeRefreshCharges > 0;
        if (consumedFreeRefresh)
            pendingFreeRefreshCharges = Mathf.Max(0, pendingFreeRefreshCharges - 1);

        refreshTimesThisVisit++;
        GenerateOffers();
        SetInfo(consumedFreeRefresh
            ? F("shop.info.free_refresh_used", "已使用免费刷新次数。下次刷新需 ${0}。", GetCurrentRefreshCost())
            : F("shop.info.refreshed", "商店已刷新。下次刷新需 ${0}。", GetCurrentRefreshCost()));
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
            SetInfo(L("shop.info.insufficient_cash", "现金不足。"));
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
            SetInfo(F("shop.info.claimed", "已免费领取「{0}」。剩余免费领取次数：{1}。", offer.definition.LocalizedTitle, pendingFreeItemCharges));
        }
        else
        {
            SetInfo(F("shop.info.purchased", "已购买「{0}」。", offer.definition.LocalizedTitle));
        }

        if (consumeFreeCharge)
            receivedFreeItemThisVisit = true;

        TryGrantDeadlockFreeRefresh();
        RefreshShopUI();
    }

    private void GenerateOffers()
    {
        ShopItemPoolAsset activePool = GetActiveShopPool();
        if (activePool == null || activePool.Entries == null || activePool.Entries.Count == 0)
        {
            SetInfo(L("shop.info.empty_pool", "商店商品池为空。"));
            for (int i = 0; i < currentOffers.Length; i++)
                currentOffers[i] = null;
            RefreshShopUI();
            return;
        }

        List<ShopItemDefinition> picks = new List<ShopItemDefinition>(currentOffers.Length);
        List<ShopItemDefinition> remainingEntries = new List<ShopItemDefinition>(activePool.Entries.Count);
        ActiveItemId equippedActiveItem = gameFlow != null ? gameFlow.GetEquippedActiveItemId() : ActiveItemId.None;
        for (int i = 0; i < activePool.Entries.Count; i++)
        {
            ShopItemDefinition entry = activePool.Entries[i];
            if (entry != null && !ShouldExcludeDefinitionFromOfferPool(entry, equippedActiveItem))
                remainingEntries.Add(entry);
        }

        if (ShouldGuaranteeActiveItemOffer(equippedActiveItem))
        {
            List<ShopItemDefinition> activeCandidates = remainingEntries.FindAll(IsShopOfferableActiveItemDefinition);
            List<ShopItemDefinition> guaranteedActivePick = WeightedPickerUtility.PickUnique(
                activeCandidates,
                1,
                activePool.GetEffectiveWeight);

            if (guaranteedActivePick.Count > 0)
            {
                picks.Add(guaranteedActivePick[0]);
                remainingEntries.Remove(guaranteedActivePick[0]);
            }
        }

        if (picks.Count < currentOffers.Length)
        {
            List<ShopItemDefinition> extraPicks = WeightedPickerUtility.PickUnique(
                remainingEntries,
                currentOffers.Length - picks.Count,
                activePool.GetEffectiveWeight);
            picks.AddRange(extraPicks);
            for (int i = 0; i < extraPicks.Count; i++)
                remainingEntries.Remove(extraPicks[i]);
        }

        for (int i = 0; i < currentOffers.Length; i++)
        {
            ShopItemDefinition definition = i < picks.Count ? picks[i] : PickSingleItemByWeight(remainingEntries, activePool);
            if (definition != null)
                remainingEntries.Remove(definition);
            currentOffers[i] = definition == null
                ? null
                : new ShopOffer
                {
                    definition = definition,
                    purchased = false,
                };
        }

        RefreshShopUI();
    }

    private bool ShouldGuaranteeActiveItemOffer(ActiveItemId equippedActiveItem)
    {
        return !IsPreBossInvestmentShopActive() && equippedActiveItem == ActiveItemId.None;
    }

    private static bool IsActiveItemDefinition(ShopItemDefinition definition)
    {
        return TryGetActiveItemId(definition, out _);
    }

    private static bool IsShopOfferableActiveItemDefinition(ShopItemDefinition definition)
    {
        return TryGetActiveItemId(definition, out ActiveItemId itemId)
            && IsSupportedShopActiveItem(itemId);
    }

    private static bool IsSupportedShopActiveItem(ActiveItemId itemId)
    {
        return itemId == ActiveItemId.SkiptraceBurst;
    }

    private static bool TryGetActiveItemId(ShopItemDefinition definition, out ActiveItemId itemId)
    {
        itemId = ActiveItemId.None;
        if (definition == null)
            return false;

        return definition.TryResolveActiveItemId(out itemId);
    }

    private static bool ShouldExcludeDefinitionFromOfferPool(ShopItemDefinition definition, ActiveItemId equippedActiveItem)
    {
        if (!TryGetActiveItemId(definition, out ActiveItemId definitionItemId))
            return false;

        if (!IsSupportedShopActiveItem(definitionItemId))
            return true;

        return equippedActiveItem != ActiveItemId.None
            && definitionItemId == equippedActiveItem;
    }

    private static ShopItemDefinition PickSingleItemByWeight(List<ShopItemDefinition> candidates, ShopItemPoolAsset activePool)
    {
        if (activePool == null || candidates == null || candidates.Count == 0)
            return null;

        List<ShopItemDefinition> one = WeightedPickerUtility.PickUnique(candidates, 1, activePool.GetEffectiveWeight);
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
        ApplySpinningWheelRuntimeConfig();
        spinningWheel.SetRiskModel(
            enforceWheelRiskModel,
            wheelPositiveOutcomeChance,
            wheelCashRefundByCost);
        SetSpinningWheelVisible(!IsPreBossInvestmentShopActive());
    }

    private ShopItemPoolAsset GetActiveShopPool()
    {
        if (IsPreBossInvestmentShopActive()
            && preBossInvestmentPoolAsset != null
            && preBossInvestmentPoolAsset.Entries != null
            && preBossInvestmentPoolAsset.Entries.Count > 0)
        {
            return preBossInvestmentPoolAsset;
        }

        return shopItemPoolAsset;
    }

    private bool IsPreBossInvestmentShopActive()
    {
        if (!usePreBossInvestmentShop || gameFlow == null)
            return false;

        int nextRound = gameFlow.GetCurrentRound() + 1;
        return nextRound == gameFlow.GetBossRoundNumber();
    }

    private void SetSpinningWheelVisible(bool visible)
    {
        if (spinningWheel == null)
            return;

        spinningWheel.SetWheelUIVisible(visible);
    }

    private void ApplySpinningWheelRuntimeConfig()
    {
        if (spinningWheel == null)
            return;

        ResolveWheelRewardConfig(out int runtimeCashMin, out int runtimeCashMax, out int runtimeDebtMin, out int runtimeDebtMax);
        spinningWheel.SetRewardConfig(
            runtimeCashMin, runtimeCashMax,
            runtimeDebtMin, runtimeDebtMax,
            enemyHpBuffMultiplier, enemySpeedBuffMultiplier,
            enemyRewardBuffMultiplier);
    }

    private void ResolveWheelRewardConfig(out int runtimeCashMin, out int runtimeCashMax, out int runtimeDebtMin, out int runtimeDebtMax)
    {
        runtimeCashMin = Mathf.Max(0, Mathf.Min(cashRewardMin, cashRewardMax));
        runtimeCashMax = Mathf.Max(runtimeCashMin, Mathf.Max(cashRewardMin, cashRewardMax));
        runtimeDebtMin = Mathf.Max(0, Mathf.Min(debtPenaltyMin, debtPenaltyMax));
        runtimeDebtMax = Mathf.Max(runtimeDebtMin, Mathf.Max(debtPenaltyMin, debtPenaltyMax));

        if (gameFlow == null)
            return;

        int currentRound = Mathf.Max(1, gameFlow.GetCurrentRound());
        int nextRound = currentRound + 1;
        int referenceDebt = Mathf.Max(1, gameFlow.GetProjectedDebtForRound(nextRound, false));
        float cashMinRatioEarly = wheelCashRewardMinDebtRatioEarly > 0f ? wheelCashRewardMinDebtRatioEarly : 0.18f;
        float cashMaxRatioEarly = wheelCashRewardMaxDebtRatioEarly > 0f ? wheelCashRewardMaxDebtRatioEarly : 0.30f;
        float cashMinRatioLate = wheelCashRewardMinDebtRatioLate > 0f ? wheelCashRewardMinDebtRatioLate : 0.24f;
        float cashMaxRatioLate = wheelCashRewardMaxDebtRatioLate > 0f ? wheelCashRewardMaxDebtRatioLate : 0.40f;
        float debtMinRatioEarly = wheelDebtPenaltyMinDebtRatioEarly > 0f ? wheelDebtPenaltyMinDebtRatioEarly : 0.05f;
        float debtMaxRatioEarly = wheelDebtPenaltyMaxDebtRatioEarly > 0f ? wheelDebtPenaltyMaxDebtRatioEarly : 0.09f;
        float debtMinRatioLate = wheelDebtPenaltyMinDebtRatioLate > 0f ? wheelDebtPenaltyMinDebtRatioLate : 0.09f;
        float debtMaxRatioLate = wheelDebtPenaltyMaxDebtRatioLate > 0f ? wheelDebtPenaltyMaxDebtRatioLate : 0.15f;
        float peakRound = wheelScalingPeakRound >= 2 ? wheelScalingPeakRound : 8f;
        int debtAbsoluteMin = wheelDebtPenaltyAbsoluteMin > 0 ? wheelDebtPenaltyAbsoluteMin : 25;
        float lateT = Mathf.InverseLerp(1f, peakRound, currentRound);

        float cashMinRatio = Mathf.Lerp(cashMinRatioEarly, cashMinRatioLate, lateT);
        float cashMaxRatio = Mathf.Lerp(cashMaxRatioEarly, cashMaxRatioLate, lateT);
        float debtMinRatio = Mathf.Lerp(debtMinRatioEarly, debtMinRatioLate, lateT);
        float debtMaxRatio = Mathf.Lerp(debtMaxRatioEarly, debtMaxRatioLate, lateT);

        int scaledCashMin = Mathf.RoundToInt(referenceDebt * Mathf.Min(cashMinRatio, cashMaxRatio));
        int scaledCashMax = Mathf.RoundToInt(referenceDebt * Mathf.Max(cashMinRatio, cashMaxRatio));
        runtimeCashMin = Mathf.Max(runtimeCashMin, scaledCashMin);
        runtimeCashMax = Mathf.Max(runtimeCashMin, Mathf.Max(runtimeCashMax, scaledCashMax));

        int scaledDebtMin = Mathf.RoundToInt(referenceDebt * Mathf.Min(debtMinRatio, debtMaxRatio));
        int scaledDebtMax = Mathf.RoundToInt(referenceDebt * Mathf.Max(debtMinRatio, debtMaxRatio));
        runtimeDebtMin = Mathf.Max(debtAbsoluteMin, scaledDebtMin);
        runtimeDebtMax = Mathf.Max(runtimeDebtMin, scaledDebtMax);
    }

    private int ResolveRuntimeGambleCost()
    {
        int baseCost = ResolveBaseRuntimeGambleCost();
        if (baseCost <= 0 || gambleDrawsThisVisit <= 0)
            return Mathf.Max(0, baseCost);

        double multiplier = 1d + Mathf.Max(0f, gambleCostPerDrawPercent);
        double scaledCost = baseCost * Math.Pow(multiplier, gambleDrawsThisVisit);
        scaledCost += Mathf.Max(0, gambleCostFlatIncreasePerDraw) * gambleDrawsThisVisit;

        int visitHardCap = gambleCostVisitHardCap > 0 ? gambleCostVisitHardCap : int.MaxValue;
        if (scaledCost >= visitHardCap)
            return visitHardCap;
        if (scaledCost >= int.MaxValue)
            return int.MaxValue;

        return Mathf.Max(baseCost, Mathf.RoundToInt((float)scaledCost));
    }

    private int ResolveBaseRuntimeGambleCost()
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

        ResolveCashIconRect();
        ResolveRoundInfoIconRect();
        CacheDefaultItemColors();
        ApplyCashTextLayout();
        uiReady = true;
    }

    private void ResolveCashIconRect()
    {
        if (cashIconRect != null || textCash == null)
            return;

        Transform iconTransform = textCash.transform.Find("Image_CashSlotIcon");
        if (iconTransform == null)
            iconTransform = textCash.transform.Find("Image_HudCashIcon");

        cashIconRect = iconTransform as RectTransform;
    }

    private void ResolveRoundInfoIconRect()
    {
        if (roundInfoIconRect != null || textRoundInfo == null)
            return;

        Transform iconTransform = textRoundInfo.transform.Find("Image_NextDebtSlotIcon");
        if (iconTransform == null)
            iconTransform = textRoundInfo.transform.Find("Image_HudDebtIcon");

        roundInfoIconRect = iconTransform as RectTransform;
    }

    private void ApplyCashTextLayout()
    {
        ResolveCashIconRect();
        if (textCash == null || cashIconRect == null)
            return;

        RectTransform cashTextRect = textCash.rectTransform;
        float textLeftEdge = -cashTextRect.rect.width * cashTextRect.pivot.x;
        float iconHalfWidth = cashIconRect.rect.width * cashIconRect.localScale.x * 0.5f;
        float iconRightEdge = cashIconRect.anchoredPosition.x + iconHalfWidth;
        float desiredTextStart = iconRightEdge + Mathf.Max(0f, cashIconTextGap);
        float desiredLeftMargin = desiredTextStart - textLeftEdge;

        Vector4 margin = textCash.margin;
        if (!Mathf.Approximately(margin.x, desiredLeftMargin))
        {
            margin.x = desiredLeftMargin;
            textCash.margin = margin;
        }

        if (textCash.alignment != TextAlignmentOptions.MidlineLeft)
            textCash.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private void ApplyRoundInfoTextLayout(string leftSegment, string rightSegment)
    {
        if (textRoundInfo == null)
            return;

        textRoundInfo.enableAutoSizing = false;
        textRoundInfo.fontSize = IsPreBossInvestmentShopActive() && LocalizationTable.CurrentLocale == "en"
            ? 28f
            : 38f;
        textRoundInfo.alignment = TextAlignmentOptions.MidlineLeft;
        textRoundInfo.textWrappingMode = TextWrappingModes.NoWrap;
        textRoundInfo.overflowMode = TextOverflowModes.Overflow;

        ResolveRoundInfoIconRect();
        if (roundInfoIconRect == null)
        {
            if (nextRoundDebtText != null)
                nextRoundDebtText.gameObject.SetActive(false);
            textRoundInfo.text = $"{leftSegment}    {rightSegment}";
            return;
        }

        // Fixed slots keep the icon and both labels stable when the font changes.
        // Measuring the left TMP string here included its margin, so every locale
        // change pushed the icon into the debt label.
        textRoundInfo.text = leftSegment;
        float startX = -textRoundInfo.rectTransform.rect.width * textRoundInfo.rectTransform.pivot.x
            + textRoundInfo.margin.x;
        const float roundLabelSlotWidth = 294f;
        float iconCenterOffset = roundLabelSlotWidth + Mathf.Max(0f, roundInfoIconSectionGap);
        float iconHalfWidth = roundInfoIconRect.rect.width * roundInfoIconRect.localScale.x * 0.5f;
        float rightTextOffset = iconCenterOffset + iconHalfWidth + Mathf.Max(0f, roundInfoIconTextGap);
        float desiredIconCenterX = startX + iconCenterOffset;

        Vector2 anchoredPosition = roundInfoIconRect.anchoredPosition;
        if (!Mathf.Approximately(anchoredPosition.x, desiredIconCenterX))
        {
            anchoredPosition.x = desiredIconCenterX;
            roundInfoIconRect.anchoredPosition = anchoredPosition;
        }

        if (nextRoundDebtText == null)
        {
            Transform existing = textRoundInfo.transform.Find("Text_NextRoundDebt");
            if (existing != null)
                nextRoundDebtText = existing.GetComponent<TextMeshProUGUI>();
            if (nextRoundDebtText == null)
            {
                GameObject label = new GameObject("Text_NextRoundDebt", typeof(RectTransform), typeof(TextMeshProUGUI));
                label.transform.SetParent(textRoundInfo.transform, false);
                nextRoundDebtText = label.GetComponent<TextMeshProUGUI>();
                nextRoundDebtText.raycastTarget = false;
            }
        }

        RectTransform rightRect = nextRoundDebtText.rectTransform;
        rightRect.anchorMin = rightRect.anchorMax = new Vector2(0.5f, 0.5f);
        rightRect.pivot = new Vector2(0f, 0.5f);
        rightRect.anchoredPosition = new Vector2(startX + rightTextOffset, 0f);
        rightRect.sizeDelta = new Vector2(
            Mathf.Max(100f, textRoundInfo.rectTransform.rect.width - textRoundInfo.margin.x - rightTextOffset),
            textRoundInfo.rectTransform.rect.height);
        nextRoundDebtText.gameObject.SetActive(true);
        nextRoundDebtText.font = textRoundInfo.font;
        nextRoundDebtText.fontSharedMaterial = textRoundInfo.fontSharedMaterial;
        nextRoundDebtText.color = textRoundInfo.color;
        nextRoundDebtText.fontSize = 38f;
        nextRoundDebtText.enableAutoSizing = false;
        nextRoundDebtText.alignment = TextAlignmentOptions.MidlineLeft;
        nextRoundDebtText.textWrappingMode = TextWrappingModes.NoWrap;
        nextRoundDebtText.overflowMode = TextOverflowModes.Overflow;
        nextRoundDebtText.text = rightSegment;
    }

    private int GetOfferPrice(ShopOffer offer)
    {
        if (offer == null || offer.definition == null)
            return 0;

        ShopRarityStyle style = ResolveRarityStyle(offer.definition.Rarity);
        float basePrice = Mathf.Max(0, offer.definition.Price) * style.priceMultiplier;

        if (IsPreBossInvestmentShopActive())
        {
            int scaledPreBossPrice = Mathf.Max(0, Mathf.RoundToInt(basePrice * Mathf.Max(0.01f, preBossInvestmentPriceMultiplier)));
            if (!useFixedPreBossInvestmentPricing)
                return scaledPreBossPrice;

            int minPrice = Mathf.Max(1, preBossInvestmentMinPrice);
            int maxPrice = Mathf.Max(minPrice, preBossInvestmentMaxPrice);
            return Mathf.Clamp(scaledPreBossPrice, minPrice, maxPrice);
        }

        float scaledPrice = basePrice * GetItemPriceScaleForCurrentRound();
        return Mathf.Max(0, Mathf.RoundToInt(scaledPrice));
    }

    private float GetItemPriceScaleForCurrentRound()
    {
        int currentRound = gameFlow != null ? Mathf.Max(1, gameFlow.GetCurrentRound()) : 1;
        float baseScale = Mathf.Max(0.01f, itemBasePriceMultiplier) * (1f + Mathf.Max(0f, itemRoundStepPercent) * (currentRound - 1));
        return baseScale * GetLateRoundSurgeMultiplier(currentRound, lateRoundPriceSurgeStartRound, lateRoundPriceSurgePercent);
    }

    private float GetRefreshRoundMultiplier()
    {
        int currentRound = gameFlow != null ? Mathf.Max(1, gameFlow.GetCurrentRound()) : 1;
        float baseScale = 1f + Mathf.Max(0f, refreshRoundStepPercent) * (currentRound - 1);
        return baseScale * GetLateRoundSurgeMultiplier(currentRound, lateRoundRefreshSurgeStartRound, lateRoundRefreshSurgePercent);
    }

    private static float GetLateRoundSurgeMultiplier(int currentRound, int startRound, float surgePercent)
    {
        int safeStartRound = Mathf.Max(1, startRound);
        int lateSteps = Mathf.Max(0, currentRound - safeStartRound + 1);
        if (lateSteps <= 0 || surgePercent <= 0f)
            return 1f;

        return 1f + (Mathf.Max(0f, surgePercent) * lateSteps * lateSteps);
    }

    private void TryGrantDeadlockFreeRefresh()
    {
        if (!receivedFreeItemThisVisit)
            return;
        if (pendingFreeRefreshCharges > 0)
            return;
        if (!AreAllOffersPurchased())
            return;

        int refreshCostNow = GetCurrentRefreshCost();
        if (refreshCostNow <= 0)
            return;
        if (gameFlow == null || gameFlow.GetCashAmount() >= refreshCostNow)
            return;

        pendingFreeRefreshCharges = 1;
        SetInfo(L("shop.info.free_refresh_granted", "商品售罄且已获得免费商品，系统赠送 1 次免费刷新。"));
    }

    private bool AreAllOffersPurchased()
    {
        bool hasAnyOffer = false;
        for (int i = 0; i < currentOffers.Length; i++)
        {
            ShopOffer offer = currentOffers[i];
            if (offer == null || offer.definition == null)
                continue;

            hasAnyOffer = true;
            if (!offer.purchased)
                return false;
        }

        return hasAnyOffer;
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
