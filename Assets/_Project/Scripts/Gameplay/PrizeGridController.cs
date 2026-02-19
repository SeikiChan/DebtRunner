using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrizeGridController : MonoBehaviour
{
    [Serializable]
    private class OutcomeSlotCounts
    {
        [Min(0)] public int cash = 3;
        [Min(0)] public int freeItem = 2;
        [Min(0)] public int thankYou = 3;
        [Min(0)] public int debtUp = 2;
        [Min(0)] public int enemyStronger = 2;

        public int Total => cash + freeItem + thankYou + debtUp + enemyStronger;

        public int Get(PrizeOutcomeType type)
        {
            switch (type)
            {
                case PrizeOutcomeType.Cash: return cash;
                case PrizeOutcomeType.FreeItem: return freeItem;
                case PrizeOutcomeType.ThankYou: return thankYou;
                case PrizeOutcomeType.DebtUp: return debtUp;
                case PrizeOutcomeType.EnemyStronger: return enemyStronger;
                default: return 0;
            }
        }

        public void Set(PrizeOutcomeType type, int value)
        {
            value = Mathf.Max(0, value);
            switch (type)
            {
                case PrizeOutcomeType.Cash: cash = value; break;
                case PrizeOutcomeType.FreeItem: freeItem = value; break;
                case PrizeOutcomeType.ThankYou: thankYou = value; break;
                case PrizeOutcomeType.DebtUp: debtUp = value; break;
                case PrizeOutcomeType.EnemyStronger: enemyStronger = value; break;
            }
        }
    }

    [Serializable]
    private class OutcomeWeightProfile
    {
        [Min(0f)] public float thankYou = 3f;
        [Min(0f)] public float debtUp = 2f;
        [Min(0f)] public float freeItem = 2f;
        [Min(0f)] public float cash = 3f;
        [Min(0f)] public float enemyStronger = 2f;

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

        public void SetFromSlotCounts(OutcomeSlotCounts counts)
        {
            thankYou = Mathf.Max(0f, counts.thankYou);
            debtUp = Mathf.Max(0f, counts.debtUp);
            freeItem = Mathf.Max(0f, counts.freeItem);
            cash = Mathf.Max(0f, counts.cash);
            enemyStronger = Mathf.Max(0f, counts.enemyStronger);
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

    [Header("UI Binding")]
    [Tooltip(
        "Bind the 12 outer-ring cells of a 4x4 frame in clockwise order:\n" +
        "[00][01][02][03]\n" +
        "[11][  ][  ][04]\n" +
        "[10][  ][  ][05]\n" +
        "[09][08][07][06]")]
    [SerializeField] private PrizeGridCellView[] cells = new PrizeGridCellView[12];
    [SerializeField] private Button btnDraw;
    [SerializeField] private TMP_Text textDrawLabel;
    [SerializeField] private PrizeGridAnimator animator;
    [SerializeField] private bool showLabels = true;

    [Header("Costs")]
    [SerializeField, Min(0)] private int drawCost = 120;

    [Header("Slot Counts (Visual Weights)")]
    [SerializeField] private OutcomeSlotCounts slotCounts = new OutcomeSlotCounts();

    [Header("Probability")]
    [SerializeField] private bool useSlotCountsAsBaseWeights = true;
    [SerializeField] private OutcomeWeightProfile baseWeights = new OutcomeWeightProfile();
    [SerializeField] private OutcomeWeightProfile boostWeights = new OutcomeWeightProfile();
    [SerializeField, Min(0f)] private float negativeOutcomeMinWeight = 2f;

    [Header("Reward Values")]
    [SerializeField] private Vector2Int cashRewardRange = new Vector2Int(120, 260);
    [SerializeField] private Vector2Int debtPenaltyRange = new Vector2Int(150, 400);
    [SerializeField, Min(1f)] private float enemyHpBuffMultiplier = 1.35f;
    [SerializeField, Min(1f)] private float enemySpeedBuffMultiplier = 1.15f;

    [Header("Outcome Icons")]
    [SerializeField] private Sprite iconThankYou;
    [SerializeField] private Sprite iconDebtUp;
    [SerializeField] private Sprite iconFreeItem;
    [SerializeField] private Sprite iconCash;
    [SerializeField] private Sprite iconEnemyStronger;

    [Header("Outcome Labels")]
    [SerializeField] private string labelThankYou = "THANK YOU";
    [SerializeField] private string labelDebtUp = "DEBT UP";
    [SerializeField] private string labelFreeItem = "FREE ITEM";
    [SerializeField] private string labelCash = "CASH";
    [SerializeField] private string labelEnemyStronger = "ENEMY+";

    private static readonly PrizeOutcomeType[] SlotFillPriority =
    {
        PrizeOutcomeType.Cash,
        PrizeOutcomeType.ThankYou,
        PrizeOutcomeType.FreeItem,
        PrizeOutcomeType.DebtUp,
        PrizeOutcomeType.EnemyStronger,
    };

    private readonly List<PrizeOutcomeType> recentOutcomes = new List<PrizeOutcomeType>(2);
    private readonly PrizeOutcomeType[] slotOutcomes = new PrizeOutcomeType[12];

    private GameFlowController gameFlow;
    private ShopSystem shopSystem;
    private bool firstInteractionInShop = true;
    private bool hasPendingResult;
    private PrizeOutcomeType pendingOutcome = PrizeOutcomeType.ThankYou;
    private int pendingTargetIndex = -1;

    public bool IsDrawInProgress => hasPendingResult || (animator != null && animator.IsSpinning);

    public bool HasValidBinding()
    {
        if (btnDraw == null || animator == null || cells == null || cells.Length != 12)
            return false;

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == null || cells[i].root == null)
                return false;
        }

        return true;
    }

    public void ConfigureRuntimeUI(PrizeGridCellView[] runtimeCells, Button drawButton, TMP_Text drawButtonLabel, PrizeGridAnimator runtimeAnimator)
    {
        if (btnDraw != null)
            btnDraw.onClick.RemoveListener(OnDrawClicked);

        cells = runtimeCells;
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
        RebuildSlotOutcomes();
        RefreshCellVisuals();
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

    public void SetRewardConfig(int cashMin, int cashMax, int debtMin, int debtMax, float hpBuffMultiplier, float speedBuffMultiplier)
    {
        cashRewardRange = new Vector2Int(Mathf.Min(cashMin, cashMax), Mathf.Max(cashMin, cashMax));
        debtPenaltyRange = new Vector2Int(Mathf.Min(debtMin, debtMax), Mathf.Max(debtMin, debtMax));
        enemyHpBuffMultiplier = Mathf.Max(1f, hpBuffMultiplier);
        enemySpeedBuffMultiplier = Mathf.Max(1f, speedBuffMultiplier);
    }

    public void CancelAndReset(bool settlePendingResult)
    {
        if (settlePendingResult)
            ResolvePendingResult();
        else
            hasPendingResult = false;

        pendingTargetIndex = -1;

        if (animator != null)
            animator.CancelAndReset();

        if (btnDraw != null)
            btnDraw.interactable = true;
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

    private void OnValidate()
    {
        NormalizeConfig();
        if (Application.isPlaying) return;

        EnsureCellsArrayLength();
        RebuildSlotOutcomes();
        RefreshCellVisuals();
        RefreshDrawLabel();
    }

    private void EnsureRuntimeSetup()
    {
        NormalizeConfig();
        EnsureCellsArrayLength();

        if (animator == null)
            animator = GetComponent<PrizeGridAnimator>();

        if (animator != null)
            animator.BindCells(cells);

        if (btnDraw != null)
        {
            btnDraw.onClick.RemoveListener(OnDrawClicked);
            btnDraw.onClick.AddListener(OnDrawClicked);
        }

        RebuildSlotOutcomes();
        RefreshCellVisuals();
        RefreshDrawLabel();
    }

    private void EnsureCellsArrayLength()
    {
        if (cells != null && cells.Length == 12)
            return;

        PrizeGridCellView[] resized = new PrizeGridCellView[12];
        if (cells != null)
            Array.Copy(cells, resized, Mathf.Min(cells.Length, resized.Length));
        cells = resized;
    }

    private void NormalizeConfig()
    {
        if (slotCounts == null)
            slotCounts = new OutcomeSlotCounts();
        if (baseWeights == null)
            baseWeights = new OutcomeWeightProfile();
        if (boostWeights == null)
            boostWeights = new OutcomeWeightProfile();

        slotCounts.cash = Mathf.Max(0, slotCounts.cash);
        slotCounts.freeItem = Mathf.Max(0, slotCounts.freeItem);
        slotCounts.thankYou = Mathf.Max(0, slotCounts.thankYou);
        slotCounts.debtUp = Mathf.Max(0, slotCounts.debtUp);
        slotCounts.enemyStronger = Mathf.Max(0, slotCounts.enemyStronger);

        if (slotCounts.Total <= 0)
            slotCounts.thankYou = 12;

        if (slotCounts.Total < 12)
            slotCounts.thankYou += 12 - slotCounts.Total;

        if (slotCounts.Total > 12)
        {
            int overflow = slotCounts.Total - 12;
            PrizeOutcomeType[] reduceOrder =
            {
                PrizeOutcomeType.ThankYou,
                PrizeOutcomeType.Cash,
                PrizeOutcomeType.FreeItem,
                PrizeOutcomeType.DebtUp,
                PrizeOutcomeType.EnemyStronger,
            };

            for (int i = 0; i < reduceOrder.Length && overflow > 0; i++)
            {
                PrizeOutcomeType type = reduceOrder[i];
                int current = slotCounts.Get(type);
                int reduce = Mathf.Min(current, overflow);
                slotCounts.Set(type, current - reduce);
                overflow -= reduce;
            }
        }

        if (!baseWeights.HasAnyPositive() || useSlotCountsAsBaseWeights)
            baseWeights.SetFromSlotCounts(slotCounts);

        if (!boostWeights.HasAnyPositive())
            boostWeights.SetDefaultBoostFromBase(baseWeights);

        drawCost = Mathf.Max(0, drawCost);
        cashRewardRange = new Vector2Int(Mathf.Min(cashRewardRange.x, cashRewardRange.y), Mathf.Max(cashRewardRange.x, cashRewardRange.y));
        debtPenaltyRange = new Vector2Int(Mathf.Min(debtPenaltyRange.x, debtPenaltyRange.y), Mathf.Max(debtPenaltyRange.x, debtPenaltyRange.y));
        enemyHpBuffMultiplier = Mathf.Max(1f, enemyHpBuffMultiplier);
        enemySpeedBuffMultiplier = Mathf.Max(1f, enemySpeedBuffMultiplier);
        negativeOutcomeMinWeight = Mathf.Max(0f, negativeOutcomeMinWeight);
    }

    private void RebuildSlotOutcomes()
    {
        int[] remaining = new int[5];
        remaining[(int)PrizeOutcomeType.ThankYou] = slotCounts.thankYou;
        remaining[(int)PrizeOutcomeType.DebtUp] = slotCounts.debtUp;
        remaining[(int)PrizeOutcomeType.FreeItem] = slotCounts.freeItem;
        remaining[(int)PrizeOutcomeType.Cash] = slotCounts.cash;
        remaining[(int)PrizeOutcomeType.EnemyStronger] = slotCounts.enemyStronger;

        bool hasPrevious = false;
        PrizeOutcomeType previous = PrizeOutcomeType.ThankYou;

        for (int i = 0; i < slotOutcomes.Length; i++)
        {
            PrizeOutcomeType next = PickNextSlotOutcome(remaining, hasPrevious, previous);
            slotOutcomes[i] = next;
            remaining[(int)next] = Mathf.Max(0, remaining[(int)next] - 1);
            previous = next;
            hasPrevious = true;
        }
    }

    private PrizeOutcomeType PickNextSlotOutcome(int[] remaining, bool hasPrevious, PrizeOutcomeType previous)
    {
        int picked = TryPickByRemaining(remaining, hasPrevious ? previous : (PrizeOutcomeType?)null);
        if (picked < 0)
            picked = TryPickByRemaining(remaining, null);

        if (picked < 0)
            return PrizeOutcomeType.ThankYou;

        return (PrizeOutcomeType)picked;
    }

    private int TryPickByRemaining(int[] remaining, PrizeOutcomeType? excluded)
    {
        int bestIndex = -1;
        int bestRemain = -1;

        for (int i = 0; i < SlotFillPriority.Length; i++)
        {
            int idx = (int)SlotFillPriority[i];
            if (excluded.HasValue && idx == (int)excluded.Value)
                continue;

            int value = remaining[idx];
            if (value <= 0)
                continue;

            if (value > bestRemain)
            {
                bestRemain = value;
                bestIndex = idx;
            }
        }

        return bestIndex;
    }

    private void RefreshCellVisuals()
    {
        if (cells == null) return;

        for (int i = 0; i < Mathf.Min(cells.Length, slotOutcomes.Length); i++)
        {
            PrizeGridCellView cell = cells[i];
            if (cell == null) continue;

            PrizeOutcomeType outcome = slotOutcomes[i];

            if (cell.icon != null)
            {
                Sprite icon = GetIcon(outcome);
                cell.icon.sprite = icon;
                cell.icon.enabled = icon != null;
            }

            if (cell.label != null)
            {
                cell.label.text = GetLabel(outcome);
                cell.label.gameObject.SetActive(showLabels);
            }

            if (cell.highlightOverlay != null)
                cell.highlightOverlay.enabled = false;
        }

        if (animator != null)
            animator.BindCells(cells);
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

        bool useBoostWeights = firstInteractionInShop;
        firstInteractionInShop = false;

        if (!gameFlow.TrySpendCash(drawCost))
        {
            PushInfo("Not enough cash to draw.");
            return;
        }

        // Outcome + target are locked at click time; spin only reveals.
        pendingOutcome = RollOutcome(useBoostWeights);
        pendingTargetIndex = PickTargetIndex(pendingOutcome);
        hasPendingResult = true;

        if (btnDraw != null)
            btnDraw.interactable = false;

        if (animator == null)
        {
            ResolvePendingResult();
            return;
        }

        animator.PlayToTarget(pendingTargetIndex, OnSpinFinished);
    }

    private PrizeOutcomeType RollOutcome(bool useBoostWeights)
    {
        float thankWeight;
        float debtWeight;
        float freeWeight;
        float cashWeight;
        float enemyWeight;
        BuildEffectiveWeights(useBoostWeights, out thankWeight, out debtWeight, out freeWeight, out cashWeight, out enemyWeight);
        ApplyNegativeStreakProtection(ref debtWeight, ref enemyWeight, ref cashWeight, ref freeWeight);

        float total = Mathf.Max(0f, thankWeight) +
                      Mathf.Max(0f, debtWeight) +
                      Mathf.Max(0f, freeWeight) +
                      Mathf.Max(0f, cashWeight) +
                      Mathf.Max(0f, enemyWeight);

        PrizeOutcomeType result = PrizeOutcomeType.ThankYou;
        if (total > 0f)
        {
            float r = UnityEngine.Random.Range(0f, total);
            if (r < thankWeight) result = PrizeOutcomeType.ThankYou;
            else if (r < thankWeight + debtWeight) result = PrizeOutcomeType.DebtUp;
            else if (r < thankWeight + debtWeight + freeWeight) result = PrizeOutcomeType.FreeItem;
            else if (r < thankWeight + debtWeight + freeWeight + cashWeight) result = PrizeOutcomeType.Cash;
            else result = PrizeOutcomeType.EnemyStronger;
        }

        PushOutcomeHistory(result);
        return result;
    }

    private void BuildEffectiveWeights(bool useBoostWeights, out float thankWeight, out float debtWeight, out float freeWeight, out float cashWeight, out float enemyWeight)
    {
        bool useBoostProfile = useBoostWeights && boostWeights != null && boostWeights.HasAnyPositive();
        bool useBaseProfile = !useSlotCountsAsBaseWeights && baseWeights != null && baseWeights.HasAnyPositive();

        if (useBoostProfile)
        {
            thankWeight = Mathf.Max(0f, boostWeights.Get(PrizeOutcomeType.ThankYou));
            debtWeight = Mathf.Max(0f, boostWeights.Get(PrizeOutcomeType.DebtUp));
            freeWeight = Mathf.Max(0f, boostWeights.Get(PrizeOutcomeType.FreeItem));
            cashWeight = Mathf.Max(0f, boostWeights.Get(PrizeOutcomeType.Cash));
            enemyWeight = Mathf.Max(0f, boostWeights.Get(PrizeOutcomeType.EnemyStronger));
            return;
        }

        if (useBaseProfile)
        {
            thankWeight = Mathf.Max(0f, baseWeights.Get(PrizeOutcomeType.ThankYou));
            debtWeight = Mathf.Max(0f, baseWeights.Get(PrizeOutcomeType.DebtUp));
            freeWeight = Mathf.Max(0f, baseWeights.Get(PrizeOutcomeType.FreeItem));
            cashWeight = Mathf.Max(0f, baseWeights.Get(PrizeOutcomeType.Cash));
            enemyWeight = Mathf.Max(0f, baseWeights.Get(PrizeOutcomeType.EnemyStronger));
            return;
        }

        thankWeight = Mathf.Max(0f, slotCounts.thankYou);
        debtWeight = Mathf.Max(0f, slotCounts.debtUp);
        freeWeight = Mathf.Max(0f, slotCounts.freeItem);
        cashWeight = Mathf.Max(0f, slotCounts.cash);
        enemyWeight = Mathf.Max(0f, slotCounts.enemyStronger);
    }

    private void ApplyNegativeStreakProtection(ref float debtWeight, ref float enemyWeight, ref float cashWeight, ref float freeWeight)
    {
        if (recentOutcomes.Count < 2)
            return;

        PrizeOutcomeType last = recentOutcomes[recentOutcomes.Count - 1];
        PrizeOutcomeType previous = recentOutcomes[recentOutcomes.Count - 2];
        if (!IsNegativeOutcome(last) || !IsNegativeOutcome(previous))
            return;

        float oldDebt = debtWeight;
        float oldEnemy = enemyWeight;
        debtWeight = Mathf.Min(debtWeight, negativeOutcomeMinWeight);
        enemyWeight = Mathf.Min(enemyWeight, negativeOutcomeMinWeight);

        float diff = Mathf.Max(0f, oldDebt - debtWeight) + Mathf.Max(0f, oldEnemy - enemyWeight);
        if (diff <= 0f) return;

        if (cashWeight > 0f || freeWeight <= 0f)
            cashWeight += diff;
        else
            freeWeight += diff;
    }

    private int PickTargetIndex(PrizeOutcomeType outcome)
    {
        List<int> candidates = new List<int>(4);
        for (int i = 0; i < slotOutcomes.Length; i++)
        {
            if (slotOutcomes[i] == outcome)
                candidates.Add(i);
        }

        if (candidates.Count == 0)
            return 0;

        if (outcome == PrizeOutcomeType.ThankYou)
        {
            List<int> nearMissCandidates = new List<int>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                int idx = candidates[i];
                if (IsNearMissThankYouSlot(idx))
                    nearMissCandidates.Add(idx);
            }

            if (nearMissCandidates.Count > 0)
                return nearMissCandidates[UnityEngine.Random.Range(0, nearMissCandidates.Count)];
        }

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private bool IsNearMissThankYouSlot(int index)
    {
        int prev = PositiveMod(index - 1, slotOutcomes.Length);
        int next = PositiveMod(index + 1, slotOutcomes.Length);
        return IsPositiveOutcome(slotOutcomes[prev]) || IsPositiveOutcome(slotOutcomes[next]);
    }

    private void OnSpinFinished()
    {
        ResolvePendingResult();
    }

    private void ResolvePendingResult()
    {
        if (!hasPendingResult)
            return;

        PrizeOutcomeType outcome = pendingOutcome;
        hasPendingResult = false;
        pendingTargetIndex = -1;

        switch (outcome)
        {
            case PrizeOutcomeType.Cash:
            {
                int reward = RollRange(cashRewardRange);
                gameFlow?.AddCash(reward);
                PushInfo($"Draw result: CASH +${reward}.");
                break;
            }
            case PrizeOutcomeType.FreeItem:
            {
                int totalCharges = shopSystem != null ? shopSystem.AddFreeItemCharges(1) : 1;
                PushInfo($"Draw result: FREE ITEM. Next free charges: {totalCharges}.");
                break;
            }
            case PrizeOutcomeType.DebtUp:
            {
                int debtUp = RollRange(debtPenaltyRange);
                gameFlow?.AddDebtPenaltyToNextRound(debtUp);
                PushInfo($"Draw result: DEBT UP +${debtUp} next round.");
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

    private int RollRange(Vector2Int range)
    {
        return UnityEngine.Random.Range(range.x, range.y + 1);
    }

    private bool IsPositiveOutcome(PrizeOutcomeType type)
    {
        return type == PrizeOutcomeType.Cash || type == PrizeOutcomeType.FreeItem;
    }

    private bool IsNegativeOutcome(PrizeOutcomeType type)
    {
        return type == PrizeOutcomeType.DebtUp || type == PrizeOutcomeType.EnemyStronger;
    }

    private int PositiveMod(int value, int mod)
    {
        if (mod <= 0) return 0;
        int m = value % mod;
        return m < 0 ? m + mod : m;
    }

    private void PushInfo(string message)
    {
        if (shopSystem != null)
            shopSystem.ShowPrizeInfo(message);
    }

    private Sprite GetIcon(PrizeOutcomeType outcome)
    {
        switch (outcome)
        {
            case PrizeOutcomeType.Cash: return iconCash;
            case PrizeOutcomeType.FreeItem: return iconFreeItem;
            case PrizeOutcomeType.DebtUp: return iconDebtUp;
            case PrizeOutcomeType.EnemyStronger: return iconEnemyStronger;
            default: return iconThankYou;
        }
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
}
