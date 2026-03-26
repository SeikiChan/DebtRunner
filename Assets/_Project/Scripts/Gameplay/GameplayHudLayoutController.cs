using TMPro;
using UnityEngine;

[ExecuteAlways]
public sealed class GameplayHudLayoutController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform hudRoot;
    [SerializeField] private RectTransform moneyBaseLeft;
    [SerializeField] private RectTransform timerBaseRight;
    [SerializeField] private RectTransform cashBlock;
    [SerializeField] private RectTransform debtBlock;
    [SerializeField] private RectTransform cashIcon;
    [SerializeField] private RectTransform debtIcon;
    [SerializeField] private TMP_Text cashText;
    [SerializeField] private TMP_Text debtText;
    [SerializeField] private TMP_Text countdownText;

    [Header("Layout")]
    [SerializeField, Min(0f)] private float contentLeftPadding = 36f;
    [SerializeField, Min(0f)] private float contentRightPadding = 28f;
    [SerializeField, Min(0f)] private float cashDebtGap = 26f;
    [SerializeField, Min(0f)] private float minimumBaseWidth = 470f;
    [SerializeField] private bool lockRightEdgeToInitialLayout = true;
    [SerializeField, Min(-100f)] private float timerBaseOverlap = 19f;
    [SerializeField] private bool hideEconomyHudDuringBossRound = true;

    private float rightEdgeLocalX;
    private float lastAppliedWidth = -1f;
    private string lastCashText;
    private string lastDebtText;
    private bool lastBossRoundHudMode;
    private bool bossRoundHudModeInitialized;

    private void Awake()
    {
        ResolveReferences();
        CaptureRightEdge();
        RefreshLayout(force: true);
    }

    private void OnEnable()
    {
        ResolveReferences();
        CaptureRightEdge();
        RefreshLayout(force: true);
    }

    private void LateUpdate()
    {
        RefreshLayout(force: false);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        CaptureRightEdge();
        RefreshLayout(force: true);
    }

    [ContextMenu("Auto Bind HUD Refs")]
    private void AutoBindHudRefs()
    {
        ResolveReferences(forceSearch: true);
        CaptureRightEdge();
        RefreshLayout(force: true);
    }

    private void ResolveReferences(bool forceSearch = false)
    {
        if (forceSearch || hudRoot == null)
        {
            hudRoot = FindRect("Panel_HUD");
        }

        if (hudRoot == null)
        {
            return;
        }

        if (forceSearch || moneyBaseLeft == null)
        {
            moneyBaseLeft = FindChildRect(hudRoot, "Image_MoneyBaseLeft");
        }

        if (forceSearch || timerBaseRight == null)
        {
            timerBaseRight = FindChildRect(hudRoot, "Image_TimerBaseRight");
        }

        if (forceSearch || cashBlock == null)
        {
            cashBlock = FindChildRect(hudRoot, "Text_Cash");
        }

        if (forceSearch || debtBlock == null)
        {
            debtBlock = FindChildRect(hudRoot, "Text_HudDebt");
        }

        if (forceSearch || cashText == null)
        {
            cashText = cashBlock != null ? cashBlock.GetComponent<TMP_Text>() : null;
        }

        if (forceSearch || cashIcon == null)
        {
            cashIcon = FindChildRect(cashBlock, "Image_HudCashIcon");
            if (cashIcon == null)
            {
                cashIcon = FindChildRect(cashBlock, "Image_CashSlotIcon");
            }
        }

        if (forceSearch || debtText == null)
        {
            debtText = debtBlock != null ? debtBlock.GetComponent<TMP_Text>() : null;
        }

        if (forceSearch || countdownText == null)
        {
            RectTransform countdownRect = FindChildRect(hudRoot, "textCountdown");
            countdownText = countdownRect != null ? countdownRect.GetComponent<TMP_Text>() : null;
        }

        if (forceSearch || debtIcon == null)
        {
            debtIcon = FindChildRect(debtBlock, "Image_HudDebtIcon");
            if (debtIcon == null)
            {
                debtIcon = FindChildRect(debtBlock, "Image_NextDebtSlotIcon");
            }
        }
    }

    private void CaptureRightEdge()
    {
        if (moneyBaseLeft == null)
        {
            return;
        }

        if (!lockRightEdgeToInitialLayout || rightEdgeLocalX == 0f || !Application.isPlaying)
        {
            if (timerBaseRight != null)
            {
                float timerLeftEdge = timerBaseRight.anchoredPosition.x - (timerBaseRight.rect.width * timerBaseRight.pivot.x);
                rightEdgeLocalX = timerLeftEdge + timerBaseOverlap;
            }
            else
            {
                rightEdgeLocalX = moneyBaseLeft.anchoredPosition.x + (moneyBaseLeft.rect.width * (1f - moneyBaseLeft.pivot.x));
            }
        }
    }

    private void RefreshLayout(bool force)
    {
        bool bossRoundHudMode = ShouldUseBossRoundHudMode();
        ApplyBossRoundHudMode(bossRoundHudMode);

        if (bossRoundHudMode)
        {
            lastCashText = string.Empty;
            lastDebtText = string.Empty;
            lastAppliedWidth = -1f;
            return;
        }

        if (!HasValidLayout())
        {
            return;
        }

        string currentCashText = cashText != null ? cashText.text : string.Empty;
        string currentDebtText = debtText != null ? debtText.text : string.Empty;

        if (!force && currentCashText == lastCashText && currentDebtText == lastDebtText)
        {
            return;
        }

        float availableRightEdge = rightEdgeLocalX;
        float debtCenterX = ComputeDebtCenterX(availableRightEdge - contentRightPadding);
        float debtLeftEdge = debtCenterX + GetLeftExtentRelative(debtText, debtIcon);
        float cashCenterX = ComputeCashCenterX(debtLeftEdge - cashDebtGap);
        float cashLeftEdge = cashCenterX + GetLeftExtentRelative(cashText, cashIcon);

        SetAnchoredPosX(debtBlock, debtCenterX);
        SetAnchoredPosX(cashBlock, cashCenterX);

        float requiredLeftEdge = Mathf.Min(cashLeftEdge, debtLeftEdge) - contentLeftPadding;
        float requiredWidth = Mathf.Max(minimumBaseWidth, availableRightEdge - requiredLeftEdge);

        if (!Mathf.Approximately(requiredWidth, lastAppliedWidth))
        {
            Vector2 sizeDelta = moneyBaseLeft.sizeDelta;
            sizeDelta.x = requiredWidth;
            moneyBaseLeft.sizeDelta = sizeDelta;

            Vector2 anchoredPosition = moneyBaseLeft.anchoredPosition;
            anchoredPosition.x = availableRightEdge - (requiredWidth * (1f - moneyBaseLeft.pivot.x));
            moneyBaseLeft.anchoredPosition = anchoredPosition;

            lastAppliedWidth = requiredWidth;
        }

        lastCashText = currentCashText;
        lastDebtText = currentDebtText;
    }

    private void ApplyBossRoundHudMode(bool bossRoundHudMode)
    {
        if (bossRoundHudModeInitialized && lastBossRoundHudMode == bossRoundHudMode)
            return;

        SetObjectActive(moneyBaseLeft, !bossRoundHudMode);
        SetObjectActive(cashBlock, !bossRoundHudMode);
        SetObjectActive(debtBlock, !bossRoundHudMode);
        SetObjectActive(timerBaseRight, !bossRoundHudMode);
        SetObjectActive(countdownText != null ? countdownText.rectTransform : null, !bossRoundHudMode);

        bossRoundHudModeInitialized = true;
        lastBossRoundHudMode = bossRoundHudMode;
    }

    private bool HasValidLayout()
    {
        return hudRoot != null
            && moneyBaseLeft != null
            && cashBlock != null
            && debtBlock != null
            && cashText != null
            && debtText != null;
    }

    private bool ShouldUseBossRoundHudMode()
    {
        if (!hideEconomyHudDuringBossRound || !Application.isPlaying)
            return false;

        GameFlowController flow = GameFlowController.Instance;
        return flow != null && flow.IsInGameplayState && flow.IsBossRoundActive();
    }

    private float ComputeDebtCenterX(float desiredRightEdge)
    {
        return desiredRightEdge - GetRightExtentRelative(debtText, debtIcon);
    }

    private float ComputeCashCenterX(float desiredRightEdge)
    {
        return desiredRightEdge - GetRightExtentRelative(cashText, cashIcon);
    }

    private static float GetLeftExtentRelative(TMP_Text text, RectTransform icon)
    {
        float textHalfWidth = GetTextWidth(text) * 0.5f;
        float left = -textHalfWidth;

        if (icon != null)
        {
            left = Mathf.Min(left, icon.anchoredPosition.x - GetScaledHalfWidth(icon));
        }

        return left;
    }

    private static float GetRightExtentRelative(TMP_Text text, RectTransform icon)
    {
        float textHalfWidth = GetTextWidth(text) * 0.5f;
        float right = textHalfWidth;

        if (icon != null)
        {
            right = Mathf.Max(right, icon.anchoredPosition.x + GetScaledHalfWidth(icon));
        }

        return right;
    }

    private static float GetTextWidth(TMP_Text text)
    {
        if (text == null)
        {
            return 0f;
        }

        Vector2 preferred = text.GetPreferredValues(text.text);
        return Mathf.Ceil(preferred.x);
    }

    private static float GetScaledHalfWidth(RectTransform rect)
    {
        return rect == null ? 0f : rect.rect.width * rect.localScale.x * 0.5f;
    }

    private static void SetAnchoredPosX(RectTransform rect, float x)
    {
        if (rect == null)
        {
            return;
        }

        Vector2 anchoredPosition = rect.anchoredPosition;
        if (Mathf.Approximately(anchoredPosition.x, x))
        {
            return;
        }

        anchoredPosition.x = x;
        rect.anchoredPosition = anchoredPosition;
    }

    private static void SetObjectActive(RectTransform rect, bool active)
    {
        if (rect == null || rect.gameObject.activeSelf == active)
            return;

        rect.gameObject.SetActive(active);
    }

    private static RectTransform FindRect(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        return target != null ? target.GetComponent<RectTransform>() : null;
    }

    private static RectTransform FindChildRect(RectTransform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == childName)
            {
                return child as RectTransform;
            }
        }

        return null;
    }
}
