using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the 3-choice level-up panel visibility and selection flow.
/// Keeps the root active and uses CanvasGroup for show/hide to avoid coroutine issues.
/// </summary>
public class LevelUpPanel : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Image dimBackground;
    [SerializeField] private UpgradeCard[] cardSlots = new UpgradeCard[3];

    [Header("Dim Settings")]
    [SerializeField] private float dimTargetAlpha = 0.5f;
    [SerializeField] private float dimFadeDuration = 0.3f;

    private Action<WeaponUpgrade> onUpgradeSelected;
    private CanvasGroup canvasGroup;
    private Coroutine fadeCo;
    private bool selectionLocked;

    private void Awake()
    {
        if (panel == null)
            panel = gameObject;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        ForceHideImmediate();
    }

    public void ForceHideImmediate()
    {
        if (fadeCo != null)
        {
            StopCoroutine(fadeCo);
            fadeCo = null;
        }

        selectionLocked = false;
        onUpgradeSelected = null;

        if (dimBackground != null)
        {
            var c = dimBackground.color;
            c.a = 0f;
            dimBackground.color = c;
        }

        if (panel != null && panel != gameObject)
            panel.SetActive(false);

        SetVisible(false);
        SetInputEnabled(false);
        SetCardsInteractable(false);
    }

    public void ShowUpgradePanel(WeaponUpgrade[] upgrades, Action<WeaponUpgrade> onSelected)
    {
        if (fadeCo != null)
        {
            StopCoroutine(fadeCo);
            fadeCo = null;
        }

        if (upgrades == null || upgrades.Length < cardSlots.Length)
        {
            Debug.LogError($"LevelUpPanel: expected at least {cardSlots.Length} upgrade options.");
            return;
        }

        selectionLocked = false;
        onUpgradeSelected = onSelected;

        if (panel != null && panel != gameObject)
            panel.SetActive(true);

        SetVisible(true);
        SetInputEnabled(true);

        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i] != null)
                cardSlots[i].SetupCard(upgrades[i], OnCardSelected);
        }

        SetCardsInteractable(true);

        if (dimBackground != null)
            StartFade(0f, dimTargetAlpha, dimFadeDuration);
    }

    public void HideUpgradePanel()
    {
        SetInputEnabled(false);
        SetCardsInteractable(false);

        if (!gameObject.activeInHierarchy)
        {
            ForceHideImmediate();
            return;
        }

        if (dimBackground != null)
        {
            if (fadeCo != null)
                StopCoroutine(fadeCo);

            fadeCo = StartCoroutine(HideRoutine());
        }
        else
        {
            ForceHideImmediate();
        }
    }

    private IEnumerator HideRoutine()
    {
        yield return FadeDim(dimTargetAlpha, 0f, dimFadeDuration);

        if (panel != null && panel != gameObject)
            panel.SetActive(false);

        SetVisible(false);
        SetInputEnabled(false);
        fadeCo = null;
    }

    private void OnCardSelected(WeaponUpgrade upgrade)
    {
        if (selectionLocked)
            return;

        selectionLocked = true;
        SetCardsInteractable(false);
        SetInputEnabled(false);

        var callback = onUpgradeSelected;
        onUpgradeSelected = null;

        HideUpgradePanel();
        callback?.Invoke(upgrade);
    }

    private void StartFade(float from, float to, float duration)
    {
        if (!gameObject.activeInHierarchy)
        {
            var c = dimBackground.color;
            c.a = to;
            dimBackground.color = c;
            return;
        }

        if (fadeCo != null)
            StopCoroutine(fadeCo);

        fadeCo = StartCoroutine(FadeDim(from, to, duration));
    }

    private IEnumerator FadeDim(float startAlpha, float endAlpha, float duration)
    {
        if (dimBackground == null)
            yield break;

        float elapsed = 0f;

        Color startColor = dimBackground.color;
        startColor.a = startAlpha;

        Color endColor = dimBackground.color;
        endColor.a = endAlpha;

        dimBackground.color = startColor;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            dimBackground.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        dimBackground.color = endColor;
        fadeCo = null;
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
    }

    private void SetInputEnabled(bool enabled)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
    }

    private void SetCardsInteractable(bool enabled)
    {
        if (cardSlots == null)
            return;

        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i] != null)
                cardSlots[i].SetInteractable(enabled);
        }
    }
}
