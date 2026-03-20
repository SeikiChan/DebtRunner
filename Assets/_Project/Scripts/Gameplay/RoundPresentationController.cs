using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RoundPresentationController : MonoBehaviour
{
    [Header("Round Intro / 回合开场")]
    [SerializeField, Min(0f)] private float roundIntroSeconds = 2.5f;
    [SerializeField] private CanvasGroup roundIntroOverlay;
    [SerializeField] private TMP_Text roundIntroRoundText;
    [SerializeField] private TMP_Text roundIntroDebtText;
    [SerializeField] private TMP_Text roundIntroContinueHintText;
    [SerializeField] private bool roundIntroRequireAnyKeyToContinue = true;
    [SerializeField] private string roundIntroContinueHintMessage = "Press Any Key to Continue";
    [SerializeField, Min(0f)] private float roundIntroFadeInSeconds = 0.15f;
    [SerializeField, Min(0f)] private float roundIntroFadeOutSeconds = 0.3f;

    [Header("Round Clear / 回合通关")]
    [SerializeField] private CanvasGroup roundClearOverlay;
    [SerializeField] private TMP_Text roundClearTitleText;
    [SerializeField] private TMP_Text roundClearSubText;
    [SerializeField] private bool showRoundClearTransition = true;
    [SerializeField] private string roundClearTitleMessage = "YOU PASS!";
    [SerializeField] private string roundClearSubMessage = "Round Cleared";
    [SerializeField, Min(0f)] private float roundClearSeconds = 1.2f;
    [SerializeField, Min(0f)] private float roundClearFadeInSeconds = 0.12f;
    [SerializeField, Min(0f)] private float roundClearFadeOutSeconds = 0.18f;
    [SerializeField, Range(0f, 1f)] private float roundClearOverlayAlphaDuringCollect = 0.45f;

    [Header("Times Up / 逼债")]
    [SerializeField] private CanvasGroup timesUpOverlay;
    [SerializeField] private TMP_Text timesUpTitleText;
    [SerializeField] private bool showTimesUpTransition = true;
    [SerializeField] private string timesUpTitleMessage = "TIME'S UP";
    [SerializeField, Min(0f)] private float timesUpSeconds = 1f;
    [SerializeField, Min(0f)] private float timesUpFadeInSeconds = 0.08f;
    [SerializeField, Min(0f)] private float timesUpFadeOutSeconds = 0.12f;

    [Header("Game Over / 失败演出")]
    [SerializeField] private CanvasGroup gameOverTransitionOverlay;
    [SerializeField] private TMP_Text gameOverTransitionTitleText;
    [SerializeField] private TMP_Text gameOverTransitionSubText;
    [HideInInspector, SerializeField] private bool showGameOverTransition = true;
    [SerializeField] private string gameOverTransitionTitleMessage = "YOU LOSS";
    [SerializeField] private string gameOverTransitionSubMessage = "Run Failed";
    [SerializeField, Min(0f)] private float gameOverTransitionSeconds = 1.1f;
    [SerializeField, Min(0f)] private float gameOverTransitionFadeInSeconds = 0.12f;
    [SerializeField, Min(0f)] private float gameOverTransitionFadeOutSeconds = 0.18f;

    [HideInInspector, SerializeField] private bool legacyConfigImported;

    private GameObject panelHUD;
    private bool roundIntroOverlayAutoCreated;
    private bool roundClearOverlayAutoCreated;
    private bool timesUpOverlayAutoCreated;
    private bool gameOverTransitionOverlayAutoCreated;

    private Coroutine roundIntroCo;
    private Coroutine timesUpCo;
    private Coroutine gameOverTransitionCo;

    public bool IsRoundIntroActive { get; private set; }
    public bool IsTimesUpActive { get; private set; }
    public bool IsGameOverTransitionActive { get; private set; }
    public bool ShowRoundClearTransition => showRoundClearTransition;
    public bool ShowTimesUpTransition => showTimesUpTransition;
    public float RoundClearSeconds => roundClearSeconds;
    public float RoundClearFadeInSeconds => roundClearFadeInSeconds;
    public float RoundClearFadeOutSeconds => roundClearFadeOutSeconds;

    public void BindCanvasSource(GameObject hudPanel)
    {
        panelHUD = hudPanel;
    }

    public void TryImportLegacyConfig(
        float legacyRoundIntroSeconds,
        CanvasGroup legacyRoundIntroOverlay,
        TMP_Text legacyRoundIntroRoundText,
        TMP_Text legacyRoundIntroDebtText,
        TMP_Text legacyRoundIntroContinueHintText,
        bool legacyRoundIntroRequireAnyKeyToContinue,
        string legacyRoundIntroContinueHintMessage,
        float legacyRoundIntroFadeInSeconds,
        float legacyRoundIntroFadeOutSeconds,
        CanvasGroup legacyRoundClearOverlay,
        TMP_Text legacyRoundClearTitleText,
        TMP_Text legacyRoundClearSubText,
        bool legacyShowRoundClearTransition,
        string legacyRoundClearTitleMessage,
        string legacyRoundClearSubMessage,
        float legacyRoundClearSeconds,
        float legacyRoundClearFadeInSeconds,
        float legacyRoundClearFadeOutSeconds,
        float legacyRoundClearOverlayAlphaDuringCollect,
        CanvasGroup legacyTimesUpOverlay,
        TMP_Text legacyTimesUpTitleText,
        bool legacyShowTimesUpTransition,
        string legacyTimesUpTitleMessage,
        float legacyTimesUpSeconds,
        float legacyTimesUpFadeInSeconds,
        float legacyTimesUpFadeOutSeconds,
        CanvasGroup legacyGameOverTransitionOverlay,
        TMP_Text legacyGameOverTransitionTitleText,
        TMP_Text legacyGameOverTransitionSubText,
        bool legacyShowGameOverTransition,
        string legacyGameOverTransitionTitleMessage,
        string legacyGameOverTransitionSubMessage,
        float legacyGameOverTransitionSeconds,
        float legacyGameOverTransitionFadeInSeconds,
        float legacyGameOverTransitionFadeOutSeconds)
    {
        bool importedAny = false;

        if (roundIntroOverlay == null && legacyRoundIntroOverlay != null)
        {
            roundIntroOverlay = legacyRoundIntroOverlay;
            importedAny = true;
        }

        if (roundIntroRoundText == null && legacyRoundIntroRoundText != null)
        {
            roundIntroRoundText = legacyRoundIntroRoundText;
            importedAny = true;
        }

        if (roundIntroDebtText == null && legacyRoundIntroDebtText != null)
        {
            roundIntroDebtText = legacyRoundIntroDebtText;
            importedAny = true;
        }

        if (roundIntroContinueHintText == null && legacyRoundIntroContinueHintText != null)
        {
            roundIntroContinueHintText = legacyRoundIntroContinueHintText;
            importedAny = true;
        }

        if (IsRoundIntroConfigDefault())
        {
            roundIntroSeconds = legacyRoundIntroSeconds;
            roundIntroRequireAnyKeyToContinue = legacyRoundIntroRequireAnyKeyToContinue;
            roundIntroContinueHintMessage = legacyRoundIntroContinueHintMessage;
            roundIntroFadeInSeconds = legacyRoundIntroFadeInSeconds;
            roundIntroFadeOutSeconds = legacyRoundIntroFadeOutSeconds;
            importedAny = true;
        }

        if (roundClearOverlay == null && legacyRoundClearOverlay != null)
        {
            roundClearOverlay = legacyRoundClearOverlay;
            importedAny = true;
        }

        if (roundClearTitleText == null && legacyRoundClearTitleText != null)
        {
            roundClearTitleText = legacyRoundClearTitleText;
            importedAny = true;
        }

        if (roundClearSubText == null && legacyRoundClearSubText != null)
        {
            roundClearSubText = legacyRoundClearSubText;
            importedAny = true;
        }

        if (IsRoundClearConfigDefault())
        {
            showRoundClearTransition = legacyShowRoundClearTransition;
            roundClearTitleMessage = legacyRoundClearTitleMessage;
            roundClearSubMessage = legacyRoundClearSubMessage;
            roundClearSeconds = legacyRoundClearSeconds;
            roundClearFadeInSeconds = legacyRoundClearFadeInSeconds;
            roundClearFadeOutSeconds = legacyRoundClearFadeOutSeconds;
            roundClearOverlayAlphaDuringCollect = legacyRoundClearOverlayAlphaDuringCollect;
            importedAny = true;
        }

        if (timesUpOverlay == null && legacyTimesUpOverlay != null)
        {
            timesUpOverlay = legacyTimesUpOverlay;
            importedAny = true;
        }

        if (timesUpTitleText == null && legacyTimesUpTitleText != null)
        {
            timesUpTitleText = legacyTimesUpTitleText;
            importedAny = true;
        }

        if (IsTimesUpConfigDefault())
        {
            showTimesUpTransition = legacyShowTimesUpTransition;
            timesUpTitleMessage = legacyTimesUpTitleMessage;
            timesUpSeconds = legacyTimesUpSeconds;
            timesUpFadeInSeconds = legacyTimesUpFadeInSeconds;
            timesUpFadeOutSeconds = legacyTimesUpFadeOutSeconds;
            importedAny = true;
        }

        if (gameOverTransitionOverlay == null && legacyGameOverTransitionOverlay != null)
        {
            gameOverTransitionOverlay = legacyGameOverTransitionOverlay;
            importedAny = true;
        }

        if (gameOverTransitionTitleText == null && legacyGameOverTransitionTitleText != null)
        {
            gameOverTransitionTitleText = legacyGameOverTransitionTitleText;
            importedAny = true;
        }

        if (gameOverTransitionSubText == null && legacyGameOverTransitionSubText != null)
        {
            gameOverTransitionSubText = legacyGameOverTransitionSubText;
            importedAny = true;
        }

        if (IsGameOverConfigDefault())
        {
            showGameOverTransition = legacyShowGameOverTransition;
            gameOverTransitionTitleMessage = legacyGameOverTransitionTitleMessage;
            gameOverTransitionSubMessage = legacyGameOverTransitionSubMessage;
            gameOverTransitionSeconds = legacyGameOverTransitionSeconds;
            gameOverTransitionFadeInSeconds = legacyGameOverTransitionFadeInSeconds;
            gameOverTransitionFadeOutSeconds = legacyGameOverTransitionFadeOutSeconds;
            importedAny = true;
        }

        legacyConfigImported = legacyConfigImported || importedAny;

        if (!importedAny)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private bool IsRoundIntroConfigDefault()
    {
        return Mathf.Approximately(roundIntroSeconds, 2.5f)
            && Mathf.Approximately(roundIntroFadeInSeconds, 0.15f)
            && Mathf.Approximately(roundIntroFadeOutSeconds, 0.3f)
            && roundIntroRequireAnyKeyToContinue
            && string.Equals(roundIntroContinueHintMessage, "Press Any Key to Continue", StringComparison.Ordinal);
    }

    private bool IsRoundClearConfigDefault()
    {
        return showRoundClearTransition
            && string.Equals(roundClearTitleMessage, "YOU PASS!", StringComparison.Ordinal)
            && string.Equals(roundClearSubMessage, "Round Cleared", StringComparison.Ordinal)
            && Mathf.Approximately(roundClearSeconds, 1.2f)
            && Mathf.Approximately(roundClearFadeInSeconds, 0.12f)
            && Mathf.Approximately(roundClearFadeOutSeconds, 0.18f)
            && Mathf.Approximately(roundClearOverlayAlphaDuringCollect, 0.45f);
    }

    private bool IsTimesUpConfigDefault()
    {
        return showTimesUpTransition
            && string.Equals(timesUpTitleMessage, "TIME'S UP", StringComparison.Ordinal)
            && Mathf.Approximately(timesUpSeconds, 1f)
            && Mathf.Approximately(timesUpFadeInSeconds, 0.08f)
            && Mathf.Approximately(timesUpFadeOutSeconds, 0.12f);
    }

    private bool IsGameOverConfigDefault()
    {
        return showGameOverTransition
            && string.Equals(gameOverTransitionTitleMessage, "YOU LOSS", StringComparison.Ordinal)
            && string.Equals(gameOverTransitionSubMessage, "Run Failed", StringComparison.Ordinal)
            && Mathf.Approximately(gameOverTransitionSeconds, 1.1f)
            && Mathf.Approximately(gameOverTransitionFadeInSeconds, 0.12f)
            && Mathf.Approximately(gameOverTransitionFadeOutSeconds, 0.18f);
    }

    public bool PlayRoundIntro(int roundIndex, int totalRounds, string debtDisplay, Action<bool> onStarted, Action onFinished)
    {
        StopRoundIntro();

        UpdateRoundIntroText(roundIndex, totalRounds, debtDisplay);
        bool useOverlay = EnsureRoundIntroOverlay();
        onStarted?.Invoke(useOverlay);

        IsRoundIntroActive = true;
        roundIntroCo = StartCoroutine(RoundIntroRoutine(useOverlay, onFinished));
        return true;
    }

    public void StopRoundIntro()
    {
        if (roundIntroCo != null)
        {
            StopCoroutine(roundIntroCo);
            roundIntroCo = null;
        }

        if (roundIntroOverlay != null)
        {
            roundIntroOverlay.alpha = 0f;
            roundIntroOverlay.gameObject.SetActive(false);
        }

        SetRoundIntroHintVisible(false);
        IsRoundIntroActive = false;
    }

    public bool PlayTimesUp(Action onStarted, Action onFinished)
    {
        if (!showTimesUpTransition)
        {
            onFinished?.Invoke();
            return false;
        }

        StopTimesUp();

        bool useOverlay = EnsureTimesUpOverlay();
        if (!useOverlay || timesUpOverlay == null)
        {
            onFinished?.Invoke();
            return false;
        }

        UpdateTimesUpText();
        onStarted?.Invoke();
        IsTimesUpActive = true;
        timesUpCo = StartCoroutine(TimesUpRoutine(onFinished));
        return true;
    }

    public void StopTimesUp()
    {
        if (timesUpCo != null)
        {
            StopCoroutine(timesUpCo);
            timesUpCo = null;
        }

        if (timesUpOverlay != null)
        {
            timesUpOverlay.alpha = 0f;
            timesUpOverlay.gameObject.SetActive(false);
        }

        IsTimesUpActive = false;
    }

    public bool PlayGameOverTransition(bool failedDebt, Action onStarted, Action onFinished)
    {
        StopGameOverTransition();

        UpdateGameOverTransitionText(failedDebt);
        bool useOverlay = EnsureGameOverTransitionOverlay();
        onStarted?.Invoke();

        IsGameOverTransitionActive = true;
        gameOverTransitionCo = StartCoroutine(GameOverTransitionRoutine(useOverlay, onFinished));
        return true;
    }

    public void StopGameOverTransition()
    {
        if (gameOverTransitionCo != null)
        {
            StopCoroutine(gameOverTransitionCo);
            gameOverTransitionCo = null;
        }

        if (gameOverTransitionOverlay != null)
        {
            gameOverTransitionOverlay.alpha = 0f;
            gameOverTransitionOverlay.gameObject.SetActive(false);
        }

        IsGameOverTransitionActive = false;
    }

    public bool PrepareRoundClearOverlay(int roundIndex, int totalRounds)
    {
        UpdateRoundClearText(roundIndex, totalRounds);
        bool useOverlay = EnsureRoundClearOverlay();
        if (!useOverlay || roundClearOverlay == null)
            return false;

        EnsureOverlayHierarchyActive(roundClearOverlay.transform);
        roundClearOverlay.gameObject.SetActive(true);
        roundClearOverlay.transform.SetAsLastSibling();
        roundClearOverlay.alpha = 0f;
        return true;
    }

    public void StopRoundClearOverlay()
    {
        if (roundClearOverlay != null)
        {
            roundClearOverlay.alpha = 0f;
            roundClearOverlay.gameObject.SetActive(false);
        }
    }

    public IEnumerator FadeRoundClearOverlayIn()
    {
        if (roundClearOverlay == null)
            yield break;

        yield return FadeCanvasGroup(roundClearOverlay, 0f, 1f, roundClearFadeInSeconds);
    }

    public IEnumerator FadeRoundClearOverlayOut()
    {
        if (roundClearOverlay == null)
            yield break;

        yield return FadeCanvasGroup(roundClearOverlay, roundClearOverlay.alpha, 0f, roundClearFadeOutSeconds);
        roundClearOverlay.gameObject.SetActive(false);
    }

    public void ClampRoundClearOverlayAlphaDuringCollect()
    {
        if (roundClearOverlay == null)
            return;

        roundClearOverlay.alpha = Mathf.Clamp01(Mathf.Min(roundClearOverlay.alpha, roundClearOverlayAlphaDuringCollect));
    }

    private IEnumerator RoundIntroRoutine(bool useOverlay, Action onFinished)
    {
        if (useOverlay && roundIntroOverlay != null)
        {
            EnsureOverlayHierarchyActive(roundIntroOverlay.transform);
            roundIntroOverlay.gameObject.SetActive(true);
            roundIntroOverlay.transform.SetAsLastSibling();
            roundIntroOverlay.alpha = 0f;

            yield return FadeCanvasGroup(roundIntroOverlay, 0f, 1f, roundIntroFadeInSeconds);

            if (roundIntroRequireAnyKeyToContinue)
            {
                SetRoundIntroHintVisible(true);
                yield return WaitForContinueInput();
                SetRoundIntroHintVisible(false);
            }
            else
            {
                float hold = Mathf.Max(0f, roundIntroSeconds - roundIntroFadeInSeconds - roundIntroFadeOutSeconds);
                if (hold > 0f)
                    yield return new WaitForSecondsRealtime(hold);
            }

            yield return FadeCanvasGroup(roundIntroOverlay, 1f, 0f, roundIntroFadeOutSeconds);
            roundIntroOverlay.gameObject.SetActive(false);
        }
        else
        {
            if (roundIntroRequireAnyKeyToContinue)
                yield return WaitForContinueInput();
            else if (roundIntroSeconds > 0f)
                yield return new WaitForSecondsRealtime(roundIntroSeconds);
        }

        roundIntroCo = null;
        IsRoundIntroActive = false;
        onFinished?.Invoke();
    }

    private IEnumerator TimesUpRoutine(Action onFinished)
    {
        EnsureOverlayHierarchyActive(timesUpOverlay.transform);
        timesUpOverlay.gameObject.SetActive(true);
        timesUpOverlay.transform.SetAsLastSibling();
        timesUpOverlay.alpha = 0f;

        yield return FadeCanvasGroup(timesUpOverlay, 0f, 1f, timesUpFadeInSeconds);

        if (timesUpSeconds > 0f)
            yield return new WaitForSecondsRealtime(timesUpSeconds);

        yield return FadeCanvasGroup(timesUpOverlay, 1f, 0f, timesUpFadeOutSeconds);
        timesUpOverlay.gameObject.SetActive(false);

        timesUpCo = null;
        IsTimesUpActive = false;
        onFinished?.Invoke();
    }

    private IEnumerator GameOverTransitionRoutine(bool useOverlay, Action onFinished)
    {
        if (useOverlay && gameOverTransitionOverlay != null)
        {
            EnsureOverlayHierarchyActive(gameOverTransitionOverlay.transform);
            gameOverTransitionOverlay.gameObject.SetActive(true);
            gameOverTransitionOverlay.transform.SetAsLastSibling();
            gameOverTransitionOverlay.alpha = 0f;

            yield return FadeCanvasGroup(gameOverTransitionOverlay, 0f, 1f, gameOverTransitionFadeInSeconds);

            float hold = Mathf.Max(0f, gameOverTransitionSeconds - gameOverTransitionFadeInSeconds - gameOverTransitionFadeOutSeconds);
            if (hold > 0f)
                yield return new WaitForSecondsRealtime(hold);

            yield return FadeCanvasGroup(gameOverTransitionOverlay, gameOverTransitionOverlay.alpha, 0f, gameOverTransitionFadeOutSeconds);
            gameOverTransitionOverlay.gameObject.SetActive(false);
        }
        else if (gameOverTransitionSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(gameOverTransitionSeconds);
        }

        gameOverTransitionCo = null;
        IsGameOverTransitionActive = false;
        onFinished?.Invoke();
    }

    private IEnumerator WaitForContinueInput()
    {
        yield return null;

        while (IsRoundIntroActive)
        {
            if (Input.anyKeyDown || GameInput.IsContinuePressed() || GameInput.IsBackPressed())
                yield break;

            yield return null;
        }
    }

    private void SetRoundIntroHintVisible(bool visible)
    {
        if (roundIntroContinueHintText != null)
            roundIntroContinueHintText.gameObject.SetActive(visible);
    }

    private void UpdateRoundIntroText(int roundIndex, int totalRounds, string debtDisplay)
    {
        if (roundIntroRoundText != null)
            roundIntroRoundText.text = $"ROUND {roundIndex}/{totalRounds}";

        if (roundIntroDebtText != null)
            roundIntroDebtText.text = $"DEBT   OWED\n{debtDisplay}";

        if (roundIntroContinueHintText != null)
            roundIntroContinueHintText.text = roundIntroContinueHintMessage;
    }

    private void UpdateTimesUpText()
    {
        if (timesUpTitleText != null)
            timesUpTitleText.text = timesUpTitleMessage;
    }

    private void UpdateRoundClearText(int roundIndex, int totalRounds)
    {
        if (roundClearTitleText != null)
            roundClearTitleText.text = roundClearTitleMessage;

        if (roundClearSubText == null)
            return;

        string baseText = string.IsNullOrWhiteSpace(roundClearSubMessage) ? "Round Cleared" : roundClearSubMessage;
        roundClearSubText.text = $"{baseText}\nRound {roundIndex}/{totalRounds}";
    }

    private void UpdateGameOverTransitionText(bool failedDebt)
    {
        if (gameOverTransitionTitleText != null)
            gameOverTransitionTitleText.text = gameOverTransitionTitleMessage;

        if (gameOverTransitionSubText == null)
            return;

        string causeText = failedDebt ? "Debt Unpaid" : "Killed by Monster";
        gameOverTransitionSubText.text = $"{gameOverTransitionSubMessage}\n{causeText}";
    }

    private bool EnsureRoundIntroOverlay()
    {
        if (roundIntroOverlay != null && roundIntroRoundText != null && roundIntroDebtText != null)
            return true;

        if (roundIntroOverlayAutoCreated)
            return false;

        Canvas canvas = panelHUD != null ? panelHUD.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
            return false;

        Transform parent = panelHUD != null && panelHUD.transform.parent != null
            ? panelHUD.transform.parent
            : canvas.transform;

        GameObject overlayRoot = new GameObject("RoundIntroOverlayAuto", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.SetParent(parent, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image bg = overlayRoot.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.82f);
        bg.raycastTarget = false;

        roundIntroOverlay = overlayRoot.GetComponent<CanvasGroup>();
        roundIntroOverlay.alpha = 0f;
        roundIntroOverlay.interactable = false;
        roundIntroOverlay.blocksRaycasts = false;

        Color gold = new Color(0.95f, 0.80f, 0.12f, 1f);
        CreateIntroLine(overlayRoot.transform, 70f, gold);
        CreateIntroLine(overlayRoot.transform, -190f, gold);

        roundIntroRoundText = CreateIntroText(overlayRoot.transform, "RoundText", new Vector2(0f, 165f), new Vector2(980f, 120f), 88f, gold);
        roundIntroDebtText = CreateIntroText(overlayRoot.transform, "DebtText", new Vector2(0f, -55f), new Vector2(980f, 300f), 90f, gold);
        roundIntroContinueHintText = CreateIntroText(overlayRoot.transform, "ContinueHintText", new Vector2(0f, -250f), new Vector2(980f, 80f), 36f, Color.white);
        roundIntroContinueHintText.fontStyle = FontStyles.Normal;
        roundIntroContinueHintText.gameObject.SetActive(false);

        roundIntroOverlay.gameObject.SetActive(false);
        roundIntroOverlayAutoCreated = true;
        return true;
    }

    private bool EnsureTimesUpOverlay()
    {
        if (timesUpOverlay != null && timesUpTitleText != null)
            return true;

        if (timesUpOverlayAutoCreated)
            return false;

        Canvas canvas = panelHUD != null ? panelHUD.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
            return false;

        Transform parent = panelHUD != null && panelHUD.transform.parent != null
            ? panelHUD.transform.parent
            : canvas.transform;

        GameObject overlayRoot = new GameObject("TimesUpOverlayAuto", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.SetParent(parent, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image bg = overlayRoot.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);
        bg.raycastTarget = false;

        timesUpOverlay = overlayRoot.GetComponent<CanvasGroup>();
        timesUpOverlay.alpha = 0f;
        timesUpOverlay.interactable = false;
        timesUpOverlay.blocksRaycasts = false;
        timesUpTitleText = CreateIntroText(overlayRoot.transform, "TimesUpTitleText", Vector2.zero, new Vector2(1100f, 220f), 98f, Color.white);
        timesUpOverlay.gameObject.SetActive(false);
        timesUpOverlayAutoCreated = true;
        return true;
    }

    private bool EnsureRoundClearOverlay()
    {
        if (roundClearOverlay != null && roundClearTitleText != null && roundClearSubText != null)
            return true;

        if (roundClearOverlayAutoCreated)
            return false;

        Canvas canvas = panelHUD != null ? panelHUD.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
            return false;

        Transform parent = panelHUD != null && panelHUD.transform.parent != null
            ? panelHUD.transform.parent
            : canvas.transform;

        GameObject overlayRoot = new GameObject("RoundClearOverlayAuto", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.SetParent(parent, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image bg = overlayRoot.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);
        bg.raycastTarget = false;

        roundClearOverlay = overlayRoot.GetComponent<CanvasGroup>();
        roundClearOverlay.alpha = 0f;
        roundClearOverlay.interactable = false;
        roundClearOverlay.blocksRaycasts = false;

        Color accent = new Color(0.20f, 1f, 0.72f, 1f);

        roundClearTitleText = CreateIntroText(
            overlayRoot.transform,
            "RoundClearTitleText",
            new Vector2(0f, 46f),
            new Vector2(1000f, 180f),
            96f,
            accent);

        roundClearSubText = CreateIntroText(
            overlayRoot.transform,
            "RoundClearSubText",
            new Vector2(0f, -110f),
            new Vector2(1000f, 180f),
            46f,
            Color.white);
        roundClearSubText.fontStyle = FontStyles.Normal;

        roundClearOverlay.gameObject.SetActive(false);
        roundClearOverlayAutoCreated = true;
        return true;
    }

    private bool EnsureGameOverTransitionOverlay()
    {
        if (gameOverTransitionOverlay != null && gameOverTransitionTitleText != null && gameOverTransitionSubText != null)
            return true;

        if (gameOverTransitionOverlayAutoCreated)
            return false;

        Canvas canvas = panelHUD != null ? panelHUD.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
            return false;

        Transform parent = panelHUD != null && panelHUD.transform.parent != null
            ? panelHUD.transform.parent
            : canvas.transform;

        GameObject overlayRoot = new GameObject("GameOverTransitionOverlayAuto", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.SetParent(parent, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image bg = overlayRoot.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.78f);
        bg.raycastTarget = false;

        gameOverTransitionOverlay = overlayRoot.GetComponent<CanvasGroup>();
        gameOverTransitionOverlay.alpha = 0f;
        gameOverTransitionOverlay.interactable = false;
        gameOverTransitionOverlay.blocksRaycasts = false;

        Color accent = new Color(1f, 0.26f, 0.22f, 1f);
        gameOverTransitionTitleText = CreateIntroText(overlayRoot.transform, "GameOverTransitionTitleText", new Vector2(0f, 46f), new Vector2(1000f, 180f), 96f, accent);
        gameOverTransitionSubText = CreateIntroText(overlayRoot.transform, "GameOverTransitionSubText", new Vector2(0f, -110f), new Vector2(1000f, 180f), 46f, Color.white);
        gameOverTransitionSubText.fontStyle = FontStyles.Normal;

        gameOverTransitionOverlay.gameObject.SetActive(false);
        gameOverTransitionOverlayAutoCreated = true;
        return true;
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;
        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }

    private static void EnsureOverlayHierarchyActive(Transform overlayTransform)
    {
        Transform current = overlayTransform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);

            current = current.parent;
        }
    }

    private static void CreateIntroLine(Transform parent, float y, Color color)
    {
        GameObject line = new GameObject("Line", typeof(RectTransform), typeof(Image));
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(820f, 4f);
        rect.anchoredPosition = new Vector2(0f, y);

        Image image = line.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static TMP_Text CreateIntroText(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        text.outlineWidth = 0.25f;
        text.raycastTarget = false;
        return text;
    }
}
