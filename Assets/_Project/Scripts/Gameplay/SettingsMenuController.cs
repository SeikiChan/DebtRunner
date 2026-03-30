using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    private const string KeyMasterVolume = "settings.master_volume";
    private const string KeyBGMVolume = "settings.bgm_volume";
    private const string KeySFXVolume = "settings.sfx_volume";
    private const string KeyResolutionWidth = "settings.resolution_width";
    private const string KeyResolutionHeight = "settings.resolution_height";
    private const string KeyFullscreen = "settings.fullscreen";

    [Header("Flow")]
    [SerializeField] private GameFlowController gameFlow;

    [Header("Master Volume UI / 总音量")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeValueText;
    [SerializeField, Range(0f, 1f)] private float defaultVolume = 0.8f;

    [Header("BGM Volume UI / 背景音乐音量")]
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private TMP_Text bgmVolumeValueText;
    [SerializeField, Range(0f, 1f)] private float defaultBGMVolume = 0.8f;

    [Header("SFX Volume UI / 音效音量")]
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text sfxVolumeValueText;
    [SerializeField, Range(0f, 1f)] private float defaultSFXVolume = 1.0f;

    [Header("Resolution UI (assign one dropdown)")]
    [SerializeField] private TMP_Dropdown resolutionTMPDropdown;
    [SerializeField] private Dropdown resolutionLegacyDropdown;
    [SerializeField] private TMP_Text resolutionHintText;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private bool defaultFullscreen = true;
    [SerializeField] private FullScreenMode fullscreenMode = FullScreenMode.FullScreenWindow;

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField, Min(0.001f)] private float keyboardSliderStep = 0.05f;

    private readonly List<Vector2Int> availableResolutions = new List<Vector2Int>();

    private bool initialized;
    private bool listenersBound;
    private bool suppressCallbacks;
    private bool referenceWarningLogged;
    private Coroutine pendingDisplayApplyCo;

    public static void ApplyStartupDisplaySettings()
    {
        Resolution current = Screen.currentResolution;
        int savedWidth = PlayerPrefs.GetInt(KeyResolutionWidth, current.width);
        int savedHeight = PlayerPrefs.GetInt(KeyResolutionHeight, current.height);
        bool fullscreen = GetSavedFullscreen(true);
        ApplyDisplay(savedWidth, savedHeight, fullscreen, FullScreenMode.FullScreenWindow);
    }

    public void Bind(GameFlowController flowController)
    {
        gameFlow = flowController;
    }

    public void ShowMenu()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        transform.SetAsLastSibling();
        EnsureInitialized();
        RefreshUIFromCurrent();
        QueueRestoreSelection();
    }

    public void HideMenu()
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void Awake()
    {
        EnsureInitialized();
        LoadSavedAndApply();
        RefreshUIFromCurrent();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        RefreshUIFromCurrent();
        QueueRestoreSelection();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        BuildResolutionList();
        EnsureFullscreenToggleExists();
        BindEvents();
        ConfigureKeyboardNavigation();
        initialized = true;
    }

    private void BindEvents()
    {
        if (listenersBound)
            return;

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        if (resolutionTMPDropdown != null)
            resolutionTMPDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (resolutionLegacyDropdown != null)
            resolutionLegacyDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        listenersBound = true;
    }

    private void LoadSavedAndApply()
    {
        float savedVolume = PlayerPrefs.GetFloat(KeyMasterVolume, defaultVolume);
        ApplyVolume(savedVolume, save: false);

        float savedBGM = PlayerPrefs.GetFloat(KeyBGMVolume, defaultBGMVolume);
        ApplyBGMVolume(savedBGM, save: false);

        float savedSFX = PlayerPrefs.GetFloat(KeySFXVolume, defaultSFXVolume);
        ApplySFXVolume(savedSFX, save: false);

        BuildResolutionList();
        Resolution current = Screen.currentResolution;
        int savedWidth = PlayerPrefs.GetInt(KeyResolutionWidth, current.width);
        int savedHeight = PlayerPrefs.GetInt(KeyResolutionHeight, current.height);
        bool savedFullscreen = GetSavedFullscreen(defaultFullscreen);
        int savedIndex = FindClosestResolutionIndex(savedWidth, savedHeight);
        ApplyResolutionByIndex(savedIndex, save: false, overrideFullscreen: savedFullscreen);
        ApplyFullscreen(savedFullscreen, save: false);
    }

    private void RefreshUIFromCurrent()
    {
        EnsureReferenceWarnings();

        suppressCallbacks = true;

        // Master volume
        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(Mathf.Clamp01(AudioListener.volume));
        UpdateVolumeText(Mathf.Clamp01(AudioListener.volume));

        // BGM volume
        float bgmVol = BGMManager.Instance != null ? BGMManager.Instance.Volume : defaultBGMVolume;
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.SetValueWithoutNotify(Mathf.Clamp01(bgmVol));
        UpdateBGMVolumeText(bgmVol);

        // SFX volume
        float sfxVol = SFXManager.Instance != null ? SFXManager.Instance.GetMasterVolume() : defaultSFXVolume;
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(Mathf.Clamp01(sfxVol));
        UpdateSFXVolumeText(sfxVol);

        // Resolution
        BuildResolutionList();
        RebuildResolutionOptions();

        int currentIndex = FindClosestResolutionIndex(Screen.width, Screen.height);
        if (resolutionTMPDropdown != null)
            resolutionTMPDropdown.SetValueWithoutNotify(currentIndex);
        if (resolutionLegacyDropdown != null)
            resolutionLegacyDropdown.SetValueWithoutNotify(currentIndex);
        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);

        UpdateResolutionText(currentIndex, applied: false);
        ConfigureKeyboardNavigation();

        suppressCallbacks = false;
    }

    public bool TryHandleKeyboardMove(MoveDirection moveDir, GameObject selected)
    {
        if (selected == null)
            return false;

        bool horizontal = moveDir == MoveDirection.Left || moveDir == MoveDirection.Right;
        if (!horizontal)
            return false;

        int direction = moveDir == MoveDirection.Left ? -1 : 1;

        if (volumeSlider != null && selected == volumeSlider.gameObject)
        {
            StepSlider(volumeSlider, direction);
            return true;
        }

        if (bgmVolumeSlider != null && selected == bgmVolumeSlider.gameObject)
        {
            StepSlider(bgmVolumeSlider, direction);
            return true;
        }

        if (sfxVolumeSlider != null && selected == sfxVolumeSlider.gameObject)
        {
            StepSlider(sfxVolumeSlider, direction);
            return true;
        }

        if (resolutionTMPDropdown != null && selected == resolutionTMPDropdown.gameObject)
        {
            StepResolution(direction);
            return true;
        }

        if (resolutionLegacyDropdown != null && selected == resolutionLegacyDropdown.gameObject)
        {
            StepResolution(direction);
            return true;
        }

        if (fullscreenToggle != null && selected == fullscreenToggle.gameObject)
        {
            fullscreenToggle.isOn = !fullscreenToggle.isOn;
            RestoreSelection(fullscreenToggle);
            return true;
        }

        return false;
    }

    private void StepSlider(Slider slider, int direction)
    {
        if (slider == null || direction == 0)
            return;

        float step = slider.wholeNumbers
            ? Mathf.Max(1f, keyboardSliderStep)
            : Mathf.Max(0.001f, keyboardSliderStep);

        float nextValue = Mathf.Clamp(slider.value + (step * direction), slider.minValue, slider.maxValue);
        if (Mathf.Approximately(nextValue, slider.value))
            return;

        slider.value = nextValue;
        RestoreSelection(slider);
    }

    public bool TryHandleKeyboardSubmit(GameObject selected)
    {
        if (selected == null)
            return false;

        if (resolutionTMPDropdown != null && selected == resolutionTMPDropdown.gameObject)
        {
            StepResolution(1);
            return true;
        }

        if (resolutionLegacyDropdown != null && selected == resolutionLegacyDropdown.gameObject)
        {
            StepResolution(1);
            return true;
        }

        if (fullscreenToggle != null && selected == fullscreenToggle.gameObject)
        {
            fullscreenToggle.isOn = !fullscreenToggle.isOn;
            RestoreSelection(fullscreenToggle);
            return true;
        }

        return false;
    }

    private void EnsureReferenceWarnings()
    {
        if (referenceWarningLogged)
            return;

        bool missingVolume = volumeSlider == null;
        bool missingResolution = resolutionTMPDropdown == null && resolutionLegacyDropdown == null;
        bool missingFullscreen = fullscreenToggle == null;
        bool missingBack = backButton == null;

        if (!missingVolume && !missingResolution && !missingFullscreen && !missingBack)
            return;

        referenceWarningLogged = true;
        RunLogger.Warning(
            "SettingsMenuController missing UI refs. " +
            $"volumeSliderMissing={missingVolume}, resolutionDropdownMissing={missingResolution}, fullscreenToggleMissing={missingFullscreen}, backButtonMissing={missingBack}");
    }

    private void BuildResolutionList()
    {
        availableResolutions.Clear();

        Resolution[] source = Screen.resolutions;
        HashSet<long> seen = new HashSet<long>();

        if (source != null)
        {
            for (int i = 0; i < source.Length; i++)
            {
                int width = source[i].width;
                int height = source[i].height;
                long key = ((long)width << 32) | (uint)height;
                if (!seen.Add(key))
                    continue;

                availableResolutions.Add(new Vector2Int(width, height));
            }
        }

        Vector2Int current = new Vector2Int(Screen.width, Screen.height);
        long currentKey = ((long)current.x << 32) | (uint)current.y;
        if (seen.Add(currentKey))
            availableResolutions.Add(current);

        if (availableResolutions.Count == 0)
            availableResolutions.Add(new Vector2Int(1280, 720));

        availableResolutions.Sort((a, b) =>
        {
            int areaCompare = (a.x * a.y).CompareTo(b.x * b.y);
            if (areaCompare != 0)
                return areaCompare;

            int widthCompare = a.x.CompareTo(b.x);
            if (widthCompare != 0)
                return widthCompare;

            return a.y.CompareTo(b.y);
        });
    }

    private void RebuildResolutionOptions()
    {
        List<string> labels = new List<string>(availableResolutions.Count);
        for (int i = 0; i < availableResolutions.Count; i++)
        {
            Vector2Int res = availableResolutions[i];
            labels.Add(res.x + " x " + res.y);
        }

        if (resolutionTMPDropdown != null)
        {
            List<TMP_Dropdown.OptionData> tmpOptions = new List<TMP_Dropdown.OptionData>(labels.Count);
            for (int i = 0; i < labels.Count; i++)
                tmpOptions.Add(new TMP_Dropdown.OptionData(labels[i]));

            resolutionTMPDropdown.ClearOptions();
            resolutionTMPDropdown.AddOptions(tmpOptions);
            resolutionTMPDropdown.RefreshShownValue();
        }

        if (resolutionLegacyDropdown != null)
        {
            List<Dropdown.OptionData> legacyOptions = new List<Dropdown.OptionData>(labels.Count);
            for (int i = 0; i < labels.Count; i++)
                legacyOptions.Add(new Dropdown.OptionData(labels[i]));

            resolutionLegacyDropdown.ClearOptions();
            resolutionLegacyDropdown.AddOptions(legacyOptions);
            resolutionLegacyDropdown.RefreshShownValue();
        }
    }

    // ====== Volume Callbacks ======

    private void OnVolumeChanged(float value)
    {
        if (suppressCallbacks) return;
        ApplyVolume(value, save: true);
    }

    private void OnBGMVolumeChanged(float value)
    {
        if (suppressCallbacks) return;
        ApplyBGMVolume(value, save: true);
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (suppressCallbacks) return;
        ApplySFXVolume(value, save: true);
    }

    private void OnResolutionChanged(int index)
    {
        if (suppressCallbacks) return;
        QueueResolutionApply(index, save: true);
    }

    private void OnFullscreenChanged(bool value)
    {
        if (suppressCallbacks) return;
        QueueFullscreenApply(value, save: true);
    }

    // ====== Apply ======

    private void ApplyVolume(float value, bool save)
    {
        float clamped = Mathf.Clamp01(value);
        AudioListener.volume = clamped;
        UpdateVolumeText(clamped);

        if (!save) return;
        PlayerPrefs.SetFloat(KeyMasterVolume, clamped);
        PlayerPrefs.Save();
    }

    private void ApplyBGMVolume(float value, bool save)
    {
        float clamped = Mathf.Clamp01(value);
        if (BGMManager.Instance != null)
            BGMManager.Instance.SetVolume(clamped);
        UpdateBGMVolumeText(clamped);

        if (!save) return;
        PlayerPrefs.SetFloat(KeyBGMVolume, clamped);
        PlayerPrefs.Save();
    }

    private void ApplySFXVolume(float value, bool save)
    {
        float clamped = Mathf.Clamp01(value);
        if (SFXManager.Instance != null)
            SFXManager.Instance.SetMasterVolume(clamped);
        UpdateSFXVolumeText(clamped);

        if (!save) return;
        PlayerPrefs.SetFloat(KeySFXVolume, clamped);
        PlayerPrefs.Save();
    }

    private void ApplyResolutionByIndex(int index, bool save, bool? overrideFullscreen = null)
    {
        if (availableResolutions.Count == 0)
            return;

        int safeIndex = Mathf.Clamp(index, 0, availableResolutions.Count - 1);
        Vector2Int target = availableResolutions[safeIndex];

        bool fullscreen = overrideFullscreen ?? Screen.fullScreen;
        ApplyDisplay(target.x, target.y, fullscreen, fullscreenMode);

        UpdateResolutionText(safeIndex, applied: true);

        if (!save) return;
        PlayerPrefs.SetInt(KeyResolutionWidth, target.x);
        PlayerPrefs.SetInt(KeyResolutionHeight, target.y);
        PlayerPrefs.Save();
    }

    private void ApplyFullscreen(bool value, bool save)
    {
        ApplyDisplay(Screen.width, Screen.height, value, fullscreenMode);

        if (!save) return;
        PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void QueueResolutionApply(int index, bool save)
    {
        if (availableResolutions.Count == 0)
            return;

        int safeIndex = Mathf.Clamp(index, 0, availableResolutions.Count - 1);
        Vector2Int target = availableResolutions[safeIndex];
        bool fullscreen = Screen.fullScreen;

        if (save)
        {
            PlayerPrefs.SetInt(KeyResolutionWidth, target.x);
            PlayerPrefs.SetInt(KeyResolutionHeight, target.y);
            PlayerPrefs.Save();
        }

        QueueDisplayApply(target.x, target.y, fullscreen, GetPreferredResolutionSelectable());
    }

    private void StepResolution(int direction)
    {
        if (availableResolutions.Count == 0)
            return;

        int currentIndex = FindClosestResolutionIndex(Screen.width, Screen.height);
        int nextIndex = Mathf.Clamp(currentIndex + direction, 0, availableResolutions.Count - 1);
        if (nextIndex == currentIndex)
        {
            RestoreSelection(GetPreferredResolutionSelectable());
            return;
        }

        if (resolutionTMPDropdown != null)
            resolutionTMPDropdown.SetValueWithoutNotify(nextIndex);
        if (resolutionLegacyDropdown != null)
            resolutionLegacyDropdown.SetValueWithoutNotify(nextIndex);

        QueueResolutionApply(nextIndex, save: true);
    }

    private void QueueFullscreenApply(bool value, bool save)
    {
        if (save)
        {
            PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        QueueDisplayApply(Screen.width, Screen.height, value, GetPreferredFullscreenSelectable());
    }

    private void QueueDisplayApply(int width, int height, bool fullscreen, Selectable preferredSelection)
    {
        if (pendingDisplayApplyCo != null)
            StopCoroutine(pendingDisplayApplyCo);

        pendingDisplayApplyCo = StartCoroutine(ApplyDisplayDeferred(width, height, fullscreen, preferredSelection));
    }

    private IEnumerator ApplyDisplayDeferred(int width, int height, bool fullscreen, Selectable preferredSelection)
    {
        HideResolutionPopups();
        yield return null;

        ApplyDisplay(width, height, fullscreen, fullscreenMode);
        yield return null;

        RefreshUIFromCurrent();
        RestoreSelection(preferredSelection);
        pendingDisplayApplyCo = null;
    }

    // ====== Resolution Helpers ======

    private int FindClosestResolutionIndex(int width, int height)
    {
        if (availableResolutions.Count == 0)
            return 0;

        int bestIndex = 0;
        long bestScore = long.MaxValue;
        for (int i = 0; i < availableResolutions.Count; i++)
        {
            long dx = availableResolutions[i].x - width;
            long dy = availableResolutions[i].y - height;
            long score = (dx * dx) + (dy * dy);
            if (score >= bestScore)
                continue;

            bestScore = score;
            bestIndex = i;
        }

        return bestIndex;
    }

    // ====== UI Text Updates ======

    private void UpdateVolumeText(float value)
    {
        if (volumeValueText == null) return;
        int pct = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
        volumeValueText.text = pct + "%";
    }

    private void UpdateBGMVolumeText(float value)
    {
        if (bgmVolumeValueText == null) return;
        int pct = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
        bgmVolumeValueText.text = pct + "%";
    }

    private void UpdateSFXVolumeText(float value)
    {
        if (sfxVolumeValueText == null) return;
        int pct = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
        sfxVolumeValueText.text = pct + "%";
    }

    private void UpdateResolutionText(int index, bool applied)
    {
        if (resolutionHintText == null || availableResolutions.Count == 0)
            return;

        int safeIndex = Mathf.Clamp(index, 0, availableResolutions.Count - 1);
        Vector2Int res = availableResolutions[safeIndex];

#if UNITY_EDITOR
        resolutionHintText.text = "Selected: " + res.x + " x " + res.y + " | " + (Screen.fullScreen ? "Fullscreen" : "Windowed") + " (Editor may not resize Game view)";
#else
        resolutionHintText.text = applied
            ? "Applied: " + res.x + " x " + res.y + " | " + (Screen.fullScreen ? "Fullscreen" : "Windowed")
            : "Current: " + res.x + " x " + res.y + " | " + (Screen.fullScreen ? "Fullscreen" : "Windowed");
#endif
    }

    private void EnsureFullscreenToggleExists()
    {
        if (fullscreenToggle != null)
            return;

        Transform content = transform.Find("Content");
        if (content == null)
            return;

        RectTransform contentRect = content as RectTransform;
        if (contentRect != null)
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, Mathf.Max(contentRect.sizeDelta.y, 560f));

        GameObject labelObject = new GameObject("Label_Fullscreen", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(content, false);
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta = new Vector2(200f, 30f);
        labelRect.anchoredPosition = new Vector2(-130f, -120f);

        TextMeshProUGUI labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.text = "Fullscreen";
        labelText.fontSize = 22f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = new Color(0.9f, 0.9f, 0.9f);
        labelText.raycastTarget = false;

        GameObject toggleObject = new GameObject("Toggle_Fullscreen", typeof(RectTransform), typeof(Image), typeof(Toggle));
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.SetParent(content, false);
        toggleRect.anchorMin = new Vector2(0.5f, 0.5f);
        toggleRect.anchorMax = new Vector2(0.5f, 0.5f);
        toggleRect.sizeDelta = new Vector2(220f, 36f);
        toggleRect.anchoredPosition = new Vector2(80f, -120f);

        Image toggleBackground = toggleObject.GetComponent<Image>();
        toggleBackground.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        GameObject checkmarkObject = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        RectTransform checkmarkRect = checkmarkObject.GetComponent<RectTransform>();
        checkmarkRect.SetParent(toggleRect, false);
        checkmarkRect.anchorMin = new Vector2(0f, 0.5f);
        checkmarkRect.anchorMax = new Vector2(0f, 0.5f);
        checkmarkRect.sizeDelta = new Vector2(24f, 24f);
        checkmarkRect.anchoredPosition = new Vector2(18f, 0f);

        Image checkmarkImage = checkmarkObject.GetComponent<Image>();
        checkmarkImage.color = new Color(0.3f, 0.7f, 1f, 1f);

        GameObject toggleLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform toggleLabelRect = toggleLabelObject.GetComponent<RectTransform>();
        toggleLabelRect.SetParent(toggleRect, false);
        toggleLabelRect.anchorMin = Vector2.zero;
        toggleLabelRect.anchorMax = Vector2.one;
        toggleLabelRect.offsetMin = new Vector2(44f, 0f);
        toggleLabelRect.offsetMax = new Vector2(-8f, 0f);

        TextMeshProUGUI toggleLabel = toggleLabelObject.GetComponent<TextMeshProUGUI>();
        toggleLabel.text = "Use Fullscreen";
        toggleLabel.fontSize = 20f;
        toggleLabel.alignment = TextAlignmentOptions.Left;
        toggleLabel.color = Color.white;
        toggleLabel.raycastTarget = false;

        fullscreenToggle = toggleObject.GetComponent<Toggle>();
        fullscreenToggle.targetGraphic = toggleBackground;
        fullscreenToggle.graphic = checkmarkImage;
        fullscreenToggle.isOn = GetSavedFullscreen(defaultFullscreen);

        RepositionOptionalChild(content, "ResolutionHint", new Vector2(0f, -155f));
        if (backButton != null)
            ((RectTransform)backButton.transform).anchoredPosition = new Vector2(0f, -220f);
    }

    private static void RepositionOptionalChild(Transform parent, string childName, Vector2 anchoredPosition)
    {
        if (parent == null)
            return;

        Transform child = parent.Find(childName);
        if (child == null)
            return;

        RectTransform rect = child as RectTransform;
        if (rect == null)
            return;

        rect.anchoredPosition = anchoredPosition;
    }

    private static bool GetSavedFullscreen(bool fallback)
    {
        if (!PlayerPrefs.HasKey(KeyFullscreen))
            return fallback;

        return PlayerPrefs.GetInt(KeyFullscreen, fallback ? 1 : 0) != 0;
    }

    private static void ApplyDisplay(int width, int height, bool fullscreen, FullScreenMode preferredMode)
    {
        int safeWidth = Mathf.Max(640, width);
        int safeHeight = Mathf.Max(360, height);
        FullScreenMode mode = fullscreen ? preferredMode : FullScreenMode.Windowed;

        if (Screen.width == safeWidth &&
            Screen.height == safeHeight &&
            Screen.fullScreen == fullscreen &&
            Screen.fullScreenMode == mode)
        {
            return;
        }

        Screen.SetResolution(safeWidth, safeHeight, mode);
    }

    private void HideResolutionPopups()
    {
        if (resolutionTMPDropdown != null)
            resolutionTMPDropdown.Hide();
        if (resolutionLegacyDropdown != null)
            resolutionLegacyDropdown.Hide();
    }

    private Selectable GetPreferredResolutionSelectable()
    {
        if (resolutionTMPDropdown != null)
            return resolutionTMPDropdown;
        if (resolutionLegacyDropdown != null)
            return resolutionLegacyDropdown;
        if (fullscreenToggle != null)
            return fullscreenToggle;
        return backButton;
    }

    private Selectable GetPreferredFullscreenSelectable()
    {
        if (fullscreenToggle != null)
            return fullscreenToggle;
        return GetPreferredResolutionSelectable();
    }

    private void QueueRestoreSelection()
    {
        StartCoroutine(RestoreSelectionNextFrame());
    }

    private IEnumerator RestoreSelectionNextFrame()
    {
        yield return null;
        RestoreSelection(GetPreferredResolutionSelectable());
    }

    private void RestoreSelection(Selectable preferredSelection)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return;

        Selectable target = preferredSelection;
        if (target == null || !target.IsActive() || !target.IsInteractable())
            target = backButton;
        if (target == null || !target.IsActive() || !target.IsInteractable())
            return;

        eventSystem.SetSelectedGameObject(null);
        eventSystem.SetSelectedGameObject(target.gameObject);
    }

    private void ConfigureKeyboardNavigation()
    {
        List<Selectable> chain = new List<Selectable>(6);
        AddSelectable(chain, volumeSlider);
        AddSelectable(chain, bgmVolumeSlider);
        AddSelectable(chain, sfxVolumeSlider);
        AddSelectable(chain, resolutionTMPDropdown);
        AddSelectable(chain, resolutionLegacyDropdown);
        AddSelectable(chain, fullscreenToggle);
        AddSelectable(chain, backButton);

        for (int i = 0; i < chain.Count; i++)
        {
            Selectable current = chain[i];
            if (current == null)
                continue;

            Navigation nav = current.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = i > 0 ? chain[i - 1] : chain[i];
            nav.selectOnDown = i < chain.Count - 1 ? chain[i + 1] : chain[i];
            nav.selectOnLeft = current;
            nav.selectOnRight = current;
            current.navigation = nav;
        }
    }

    private static void AddSelectable(List<Selectable> chain, Selectable selectable)
    {
        if (selectable == null)
            return;
        if (chain.Contains(selectable))
            return;
        chain.Add(selectable);
    }


    private void OnBackClicked()
    {
        if (gameFlow != null)
        {
            gameFlow.BackFromPauseSettings();
            return;
        }

        HideMenu();
    }
}
