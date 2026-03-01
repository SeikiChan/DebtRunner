using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 转盘控制器 — 8扇区转盘赌博系统
/// 替代旧版 PrizeGridController（跑马灯老虎机）
/// </summary>
public class SpinningWheelController : MonoBehaviour
{
    public const int SegmentCount = 8;

    [Serializable]
    public class SegmentConfig
    {
        public PrizeOutcomeType outcome;
        public Color color = Color.gray;
        [HideInInspector] public Image image;
        [HideInInspector] public TMP_Text label;
    }

    [Serializable]
    private class OutcomeWeightProfile
    {
        [Min(0f)] public float thankYou = 2f;
        [Min(0f)] public float debtUp = 1f;
        [Min(0f)] public float freeItem = 2f;
        [Min(0f)] public float cash = 2f;
        [Min(0f)] public float enemyStronger = 1f;

        public float Get(PrizeOutcomeType type)
        {
            switch (type)
            {
                case PrizeOutcomeType.ThankYou: return thankYou;
                case PrizeOutcomeType.DebtUp: return debtUp;
                case PrizeOutcomeType.FreeItem: return freeItem;
                case PrizeOutcomeType.Cash: return cash;
                case PrizeOutcomeType.EnemyStronger: return enemyStronger;
                default: return 0f;
            }
        }

        public bool HasAnyPositive()
        {
            return thankYou > 0f || debtUp > 0f || freeItem > 0f || cash > 0f || enemyStronger > 0f;
        }

        public void SetDefaultBoostFromBase(OutcomeWeightProfile baseProfile)
        {
            thankYou = Mathf.Max(0f, baseProfile.thankYou);
            debtUp = Mathf.Max(0f, baseProfile.debtUp * 0.75f);
            freeItem = Mathf.Max(0f, baseProfile.freeItem * 1.35f);
            cash = Mathf.Max(0f, baseProfile.cash * 1.5f);
            enemyStronger = Mathf.Max(0f, baseProfile.enemyStronger * 0.75f);
        }
    }

    [Header("UI Binding / UI绑定")]
    [LocalizedLabel("Wheel Transform / 转盘旋转体")]
    [SerializeField] private RectTransform wheelTransform;
    [LocalizedLabel("Draw Button / 抽奖按钮")]
    [SerializeField] private Button btnDraw;
    [LocalizedLabel("Draw Label / 抽奖按钮文本")]
    [SerializeField] private TMP_Text textDrawLabel;
    [LocalizedLabel("Animator / 转盘动画器")]
    [SerializeField] private SpinningWheelAnimator animator;

    [Header("Segments / 扇区配置")]
    [SerializeField] private SegmentConfig[] segments = new SegmentConfig[SegmentCount];

    [Header("Costs / 费用")]
    [LocalizedLabel("Draw Cost / 抽奖费用")]
    [SerializeField, Min(0)] private int drawCost = 120;

    [Header("Probability / 概率")]
    [SerializeField] private OutcomeWeightProfile baseWeights = new OutcomeWeightProfile();
    [SerializeField] private OutcomeWeightProfile boostWeights = new OutcomeWeightProfile();
    [SerializeField, Min(0f)] private float negativeOutcomeMinWeight = 2f;

    [Header("Reward Values / 奖励数值")]
    [SerializeField] private Vector2Int cashRewardRange = new Vector2Int(120, 260);
    [SerializeField] private Vector2Int debtPenaltyRange = new Vector2Int(150, 400);
    [SerializeField, Min(1f)] private float enemyHpBuffMultiplier = 1.35f;
    [SerializeField, Min(1f)] private float enemySpeedBuffMultiplier = 1.15f;

    [Header("Outcome Labels / 结果标签")]
    [SerializeField] private string labelThankYou = "THANKS";
    [SerializeField] private string labelDebtUp = "DEBT+";
    [SerializeField] private string labelFreeItem = "FREE";
    [SerializeField] private string labelCash = "CASH";
    [SerializeField] private string labelEnemyStronger = "ENEMY+";

    private readonly List<PrizeOutcomeType> recentOutcomes = new List<PrizeOutcomeType>(2);

    private GameFlowController gameFlow;
    private ShopSystem shopSystem;
    private bool firstInteractionInShop = true;
    private bool hasPendingResult;
    private PrizeOutcomeType pendingOutcome = PrizeOutcomeType.ThankYou;

    public bool IsDrawInProgress => hasPendingResult || (animator != null && animator.IsSpinning);

    public bool HasValidBinding()
    {
        return wheelTransform != null && btnDraw != null && animator != null
               && segments != null && segments.Length == SegmentCount;
    }

    public void ConfigureRuntimeUI(
        RectTransform wheel,
        SegmentConfig[] runtimeSegments,
        Button drawButton,
        TMP_Text drawButtonLabel,
        SpinningWheelAnimator runtimeAnimator)
    {
        if (btnDraw != null)
            btnDraw.onClick.RemoveListener(OnDrawClicked);

        wheelTransform = wheel;
        segments = runtimeSegments;
        btnDraw = drawButton;
        textDrawLabel = drawButtonLabel;
        animator = runtimeAnimator;

        EnsureRuntimeSetup();
    }

    public void Bind(GameFlowController flow, ShopSystem owner)
    {
        gameFlow = flow;
        shopSystem = owner;
        EnsureRuntimeSetup();
    }

    public void OnShopOpened()
    {
        firstInteractionInShop = true;
        CancelAndReset(false);
        RefreshSegmentVisuals();
    }

    public void MarkOtherShopInteraction()
    {
        firstInteractionInShop = false;
    }

    public void SetDrawCost(int cost)
    {
        drawCost = Mathf.Max(0, cost);
        RefreshDrawLabel();
    }

    public void SetRewardConfig(int cashMin, int cashMax, int debtMin, int debtMax, float hpBuff, float speedBuff)
    {
        cashRewardRange = new Vector2Int(Mathf.Min(cashMin, cashMax), Mathf.Max(cashMin, cashMax));
        debtPenaltyRange = new Vector2Int(Mathf.Min(debtMin, debtMax), Mathf.Max(debtMin, debtMax));
        enemyHpBuffMultiplier = Mathf.Max(1f, hpBuff);
        enemySpeedBuffMultiplier = Mathf.Max(1f, speedBuff);
    }

    public void CancelAndReset(bool settlePendingResult)
    {
        if (settlePendingResult)
            ResolvePendingResult();
        else
            hasPendingResult = false;

        if (animator != null)
            animator.CancelAndReset();

        if (btnDraw != null)
            btnDraw.interactable = true;
    }

    /// <summary>
    /// 获取指定扇区索引对应的角度（扇区中心角度，顺时针，0°=正上方）
    /// </summary>
    public static float GetSegmentCenterAngle(int segmentIndex)
    {
        float degreesPerSegment = 360f / SegmentCount;
        return segmentIndex * degreesPerSegment + degreesPerSegment * 0.5f;
    }

    private void Awake()
    {
        EnsureRuntimeSetup();
    }

    private void OnEnable()
    {
        EnsureRuntimeSetup();
    }

    private void OnDisable()
    {
        CancelAndReset(false);
    }

    private void OnDestroy()
    {
        if (btnDraw != null)
            btnDraw.onClick.RemoveListener(OnDrawClicked);
    }

    private void EnsureRuntimeSetup()
    {
        NormalizeConfig();

        if (animator == null)
            animator = GetComponentInChildren<SpinningWheelAnimator>(true);

        if (animator != null && wheelTransform != null)
            animator.BindWheel(wheelTransform);

        if (btnDraw != null)
        {
            btnDraw.onClick.RemoveListener(OnDrawClicked);
            btnDraw.onClick.AddListener(OnDrawClicked);
        }

        RefreshSegmentVisuals();
        RefreshDrawLabel();
    }

    private void NormalizeConfig()
    {
        if (segments == null || segments.Length != SegmentCount)
        {
            SegmentConfig[] resized = new SegmentConfig[SegmentCount];
            if (segments != null)
                Array.Copy(segments, resized, Mathf.Min(segments.Length, resized.Length));
            for (int i = 0; i < resized.Length; i++)
            {
                if (resized[i] == null)
                    resized[i] = new SegmentConfig { outcome = PrizeOutcomeType.ThankYou, color = Color.gray };
            }
            segments = resized;
        }

        if (baseWeights == null)
            baseWeights = new OutcomeWeightProfile();
        if (boostWeights == null)
            boostWeights = new OutcomeWeightProfile();

        if (!boostWeights.HasAnyPositive())
            boostWeights.SetDefaultBoostFromBase(baseWeights);

        drawCost = Mathf.Max(0, drawCost);
        cashRewardRange = new Vector2Int(Mathf.Min(cashRewardRange.x, cashRewardRange.y), Mathf.Max(cashRewardRange.x, cashRewardRange.y));
        debtPenaltyRange = new Vector2Int(Mathf.Min(debtPenaltyRange.x, debtPenaltyRange.y), Mathf.Max(debtPenaltyRange.x, debtPenaltyRange.y));
        negativeOutcomeMinWeight = Mathf.Max(0f, negativeOutcomeMinWeight);
    }

    private void RefreshSegmentVisuals()
    {
        if (segments == null) return;

        for (int i = 0; i < segments.Length; i++)
        {
            SegmentConfig seg = segments[i];
            if (seg == null) continue;

            if (seg.image != null)
                seg.image.color = seg.color;

            if (seg.label != null)
                seg.label.text = GetLabel(seg.outcome);
        }
    }

    private void RefreshDrawLabel()
    {
        if (textDrawLabel != null)
            textDrawLabel.text = $"DRAW\n${drawCost}";
    }

    private void OnDrawClicked()
    {
        if (gameFlow == null || hasPendingResult)
            return;

        if (animator != null && animator.IsSpinning)
            return;

        bool useBoost = firstInteractionInShop;
        firstInteractionInShop = false;

        if (!gameFlow.TrySpendCash(drawCost))
        {
            PushInfo("Not enough cash to draw.");
            return;
        }

        pendingOutcome = RollOutcome(useBoost);
        int targetIndex = PickTargetIndex(pendingOutcome);
        hasPendingResult = true;

        if (btnDraw != null)
            btnDraw.interactable = false;

        if (animator == null)
        {
            ResolvePendingResult();
            return;
        }

        float targetAngle = GetSegmentCenterAngle(targetIndex);
        animator.SpinToAngle(targetAngle, OnSpinFinished);
    }

    private PrizeOutcomeType RollOutcome(bool useBoost)
    {
        float thankW, debtW, freeW, cashW, enemyW;
        BuildEffectiveWeights(useBoost, out thankW, out debtW, out freeW, out cashW, out enemyW);
        ApplyNegativeStreakProtection(ref debtW, ref enemyW, ref cashW, ref freeW);

        float total = Mathf.Max(0f, thankW) + Mathf.Max(0f, debtW) +
                      Mathf.Max(0f, freeW) + Mathf.Max(0f, cashW) + Mathf.Max(0f, enemyW);

        PrizeOutcomeType result = PrizeOutcomeType.ThankYou;
        if (total > 0f)
        {
            float r = UnityEngine.Random.Range(0f, total);
            if (r < thankW) result = PrizeOutcomeType.ThankYou;
            else if (r < thankW + debtW) result = PrizeOutcomeType.DebtUp;
            else if (r < thankW + debtW + freeW) result = PrizeOutcomeType.FreeItem;
            else if (r < thankW + debtW + freeW + cashW) result = PrizeOutcomeType.Cash;
            else result = PrizeOutcomeType.EnemyStronger;
        }

        PushOutcomeHistory(result);
        return result;
    }

    private void BuildEffectiveWeights(bool useBoost, out float thankW, out float debtW, out float freeW, out float cashW, out float enemyW)
    {
        OutcomeWeightProfile profile = (useBoost && boostWeights != null && boostWeights.HasAnyPositive())
            ? boostWeights : baseWeights;

        thankW = Mathf.Max(0f, profile.Get(PrizeOutcomeType.ThankYou));
        debtW = Mathf.Max(0f, profile.Get(PrizeOutcomeType.DebtUp));
        freeW = Mathf.Max(0f, profile.Get(PrizeOutcomeType.FreeItem));
        cashW = Mathf.Max(0f, profile.Get(PrizeOutcomeType.Cash));
        enemyW = Mathf.Max(0f, profile.Get(PrizeOutcomeType.EnemyStronger));
    }

    private void ApplyNegativeStreakProtection(ref float debtW, ref float enemyW, ref float cashW, ref float freeW)
    {
        if (recentOutcomes.Count < 2) return;

        PrizeOutcomeType last = recentOutcomes[recentOutcomes.Count - 1];
        PrizeOutcomeType prev = recentOutcomes[recentOutcomes.Count - 2];
        if (!IsNegative(last) || !IsNegative(prev)) return;

        float oldDebt = debtW;
        float oldEnemy = enemyW;
        debtW = Mathf.Min(debtW, negativeOutcomeMinWeight);
        enemyW = Mathf.Min(enemyW, negativeOutcomeMinWeight);

        float diff = Mathf.Max(0f, oldDebt - debtW) + Mathf.Max(0f, oldEnemy - enemyW);
        if (diff <= 0f) return;

        if (cashW > 0f || freeW <= 0f)
            cashW += diff;
        else
            freeW += diff;
    }

    private int PickTargetIndex(PrizeOutcomeType outcome)
    {
        List<int> candidates = new List<int>(4);
        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] != null && segments[i].outcome == outcome)
                candidates.Add(i);
        }

        if (candidates.Count == 0)
            return 0;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private void OnSpinFinished()
    {
        ResolvePendingResult();
    }

    private void ResolvePendingResult()
    {
        if (!hasPendingResult) return;

        PrizeOutcomeType outcome = pendingOutcome;
        hasPendingResult = false;

        switch (outcome)
        {
            case PrizeOutcomeType.Cash:
            {
                int reward = UnityEngine.Random.Range(cashRewardRange.x, cashRewardRange.y + 1);
                gameFlow?.AddCash(reward);
                PushInfo($"Draw result: CASH +${reward}.");
                break;
            }
            case PrizeOutcomeType.FreeItem:
            {
                int total = shopSystem != null ? shopSystem.AddFreeItemCharges(1) : 1;
                PushInfo($"Draw result: FREE ITEM. Free charges: {total}.");
                break;
            }
            case PrizeOutcomeType.DebtUp:
            {
                int penalty = UnityEngine.Random.Range(debtPenaltyRange.x, debtPenaltyRange.y + 1);
                gameFlow?.AddDebtPenaltyToNextRound(penalty);
                PushInfo($"Draw result: DEBT UP +${penalty} next round.");
                break;
            }
            case PrizeOutcomeType.EnemyStronger:
                gameFlow?.AddEnemyBuffToNextRound(enemyHpBuffMultiplier, enemySpeedBuffMultiplier);
                PushInfo($"Draw result: ENEMY STRONGER (HP x{enemyHpBuffMultiplier:F2}, Speed x{enemySpeedBuffMultiplier:F2}).");
                break;
            default:
                PushInfo("Draw result: Thank you for participating.");
                break;
        }

        if (btnDraw != null)
            btnDraw.interactable = true;

        if (shopSystem != null)
            shopSystem.RefreshShopUI();
    }

    private void PushOutcomeHistory(PrizeOutcomeType outcome)
    {
        if (recentOutcomes.Count >= 2)
            recentOutcomes.RemoveAt(0);
        recentOutcomes.Add(outcome);
    }

    private bool IsNegative(PrizeOutcomeType type)
    {
        return type == PrizeOutcomeType.DebtUp || type == PrizeOutcomeType.EnemyStronger;
    }

    private string GetLabel(PrizeOutcomeType outcome)
    {
        switch (outcome)
        {
            case PrizeOutcomeType.Cash: return labelCash;
            case PrizeOutcomeType.FreeItem: return labelFreeItem;
            case PrizeOutcomeType.DebtUp: return labelDebtUp;
            case PrizeOutcomeType.EnemyStronger: return labelEnemyStronger;
            default: return labelThankYou;
        }
    }

    private void PushInfo(string message)
    {
        if (shopSystem != null)
            shopSystem.ShowPrizeInfo(message);
    }
}
