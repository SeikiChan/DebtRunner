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

    [Header("Costs / 费用")]
    [LocalizedLabel("Gamble Cost / 抽奖费用")]
    [SerializeField] private int gambleCost = 120;
    [LocalizedLabel("Refresh Cost / 刷新费用")]
    [SerializeField] private int refreshCost = 80;

    [Header("Gamble Rewards / 赌博奖励")]
    [SerializeField] private int cashRewardMin = 120;
    [SerializeField] private int cashRewardMax = 260;
    [SerializeField] private int debtPenaltyMin = 150;
    [SerializeField] private int debtPenaltyMax = 400;
    [SerializeField] private float enemyHpBuffMultiplier = 1.35f;
    [SerializeField] private float enemySpeedBuffMultiplier = 1.15f;

    [Header("Shop Item Pool / 商品池")]
    [LocalizedLabel("Shop Item Pool Asset / 商品池资源")]
    [SerializeField] private ShopItemPoolAsset shopItemPoolAsset;

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
        GenerateOffers();
        BindSpinningWheel();
        spinningWheel?.OnShopOpened();
        SetInfo("Spend cash to upgrade. Draw can help or hurt the next round.");
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

        if (textRefreshLabel != null) textRefreshLabel.text = $"Refresh ${refreshCost}";

        if (spinningWheel != null)
        {
            spinningWheel.SetDrawCost(gambleCost);
            spinningWheel.SetRewardConfig(
                cashRewardMin, cashRewardMax,
                debtPenaltyMin, debtPenaltyMax,
                enemyHpBuffMultiplier, enemySpeedBuffMultiplier);
        }

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
                continue;
            }

            int price = offer.GetPrice();
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

    private void RefreshOffers()
    {
        MarkOtherShopInteraction();
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
        MarkOtherShopInteraction();
        if (gameFlow == null) return;
        if (index < 0 || index >= currentOffers.Length) return;

        ShopOffer offer = currentOffers[index];
        if (offer == null || offer.definition == null || offer.purchased) return;

        int cost = offer.GetPrice();
        bool consumeFreeCharge = pendingFreeItemCharges > 0 && cost > 0;
        int finalCost = consumeFreeCharge ? 0 : cost;
        if (!gameFlow.TrySpendCash(finalCost))
        {
            SetInfo("Not enough cash.");
            return;
        }

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

        spinningWheel.Bind(gameFlow, this);
        spinningWheel.SetDrawCost(gambleCost);
        spinningWheel.SetRewardConfig(
            cashRewardMin, cashRewardMax,
            debtPenaltyMin, debtPenaltyMax,
            enemyHpBuffMultiplier, enemySpeedBuffMultiplier);
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

        uiReady = true;
    }

    private void SetInfo(string message)
    {
        if (textInfo != null)
            textInfo.text = message;

        if (!string.IsNullOrWhiteSpace(message))
            RunLogger.Event($"Shop: {message}");
    }
}
