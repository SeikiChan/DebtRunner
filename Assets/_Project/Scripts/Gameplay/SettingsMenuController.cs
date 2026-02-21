using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    private const string KeyMasterVolume = "settings.master_volume";
    private const string KeyResolutionWidth = "settings.resolution_width";
    private const string KeyResolutionHeight = "settings.resolution_height";

    [Header("Flow")]
    [SerializeField] private GameFlowController gameFlow;

    [Header("Volume UI")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeValueText;
    [SerializeField, Range(0f, 1f)] private float defaultVolume = 0.8f;

    [Header("Resolution UI (assign one dropdown)")]
    [SerializeField] private TMP_Dropdown resolutionTMPDropdown;
    [SerializeField] private Dropdown resolutionLegacyDropdown;
    [SerializeField] private TMP_Text resolutionHintText;
    [SerializeField] private bool forceWindowedOnResolutionChange = true;

    [Header("Buttons")]
    [SerializeField] private Button backButton;

    private readonly List<Vector2Int> availableResolutions = new List<Vector2Int>();

    private bool initialized;
    private bool listenersBound;
    private bool suppressCallbacks;
    private bool referenceWarningLogged;

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
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        BuildResolutionList();
        BindEvents();
        initialized = true;
    }

    private void BindEvents()
    {
        if (listenersBound)
            return;

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        if (resolutionTMPDropdown != null)
            resolutionTMPDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (resolutionLegacyDropdown != null)
            resolutionLegacyDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        listenersBound = true;
    }

    private void LoadSavedAndApply()
    {
        float savedVolume = PlayerPrefs.GetFloat(KeyMasterVolume, defaultVolume);
        ApplyVolume(savedVolume, save: false);

        BuildResolutionList();
        int savedWidth = PlayerPrefs.GetInt(KeyResolutionWidth, Screen.width);
        int savedHeight = PlayerPrefs.GetInt(KeyResolutionHeight, Screen.height);
        int savedIndex = FindClosestResolutionIndex(savedWidth, savedHeight);
        ApplyResolutionByIndex(savedIndex, save: false);
    }

    private void RefreshUIFromCurrent()
    {
        EnsureReferenceWarnings();

        suppressCallbacks = true;

        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(Mathf.Clamp01(AudioListener.volume));
        UpdateVolumeText(Mathf.Clamp01(AudioListener.volume));

        BuildResolutionList();
        RebuildResolutionOptions();

        int currentIndex = FindClosestResolutionIndex(Screen.width, Screen.height);
        if (resolutionTMPDropdown != null)
            resolutionTMPDropdown.SetValueWithoutNotify(currentIndex);
        if (resolutionLegacyDropdown != null)
            resolutionLegacyDropdown.SetValueWithoutNotify(currentIndex);

        UpdateResolutionText(currentIndex, applied: false);

        suppressCallbacks = false;
    }

    private void EnsureReferenceWarnings()
    {
        if (referenceWarningLogged)
            return;

        bool missingVolume = volumeSlider == null;
        bool missingResolution = resolutionTMPDropdown == null && resolutionLegacyDropdown == null;
        bool missingBack = backButton == null;

        if (!missingVolume && !missingResolution && !missingBack)
            return;

        referenceWarningLogged = true;
        RunLogger.Warning(
            "SettingsMenuController missing UI refs. " +
            $"volumeSliderMissing={missingVolume}, resolutionDropdownMissing={missingResolution}, backButtonMissing={missingBack}");
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

    private void OnVolumeChanged(float value)
    {
        if (suppressCallbacks)
            return;

        ApplyVolume(value, save: true);
    }

    private void OnResolutionChanged(int index)
    {
        if (suppressCallbacks)
            return;

        ApplyResolutionByIndex(index, save: true);
    }

    private void ApplyVolume(float value, bool save)
    {
        float clamped = Mathf.Clamp01(value);
        AudioListener.volume = clamped;
        UpdateVolumeText(clamped);

        if (!save)
            return;

        PlayerPrefs.SetFloat(KeyMasterVolume, clamped);
        PlayerPrefs.Save();
    }

    private void ApplyResolutionByIndex(int index, bool save)
    {
        if (availableResolutions.Count == 0)
            return;

        int safeIndex = Mathf.Clamp(index, 0, availableResolutions.Count - 1);
        Vector2Int target = availableResolutions[safeIndex];

        bool fullscreen = forceWindowedOnResolutionChange ? false : Screen.fullScreen;
        if (Screen.width != target.x || Screen.height != target.y || Screen.fullScreen != fullscreen)
            Screen.SetResolution(target.x, target.y, fullscreen);

        UpdateResolutionText(safeIndex, applied: true);

        if (!save)
            return;

        PlayerPrefs.SetInt(KeyResolutionWidth, target.x);
        PlayerPrefs.SetInt(KeyResolutionHeight, target.y);
        PlayerPrefs.Save();
    }

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

    private void UpdateVolumeText(float value)
    {
        if (volumeValueText == null)
            return;

        int pct = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
        volumeValueText.text = pct + "%";
    }

    private void UpdateResolutionText(int index, bool applied)
    {
        if (resolutionHintText == null || availableResolutions.Count == 0)
            return;

        int safeIndex = Mathf.Clamp(index, 0, availableResolutions.Count - 1);
        Vector2Int res = availableResolutions[safeIndex];

#if UNITY_EDITOR
        resolutionHintText.text = "Selected: " + res.x + " x " + res.y + " (Editor may not resize Game view)";
#else
        resolutionHintText.text = applied
            ? "Applied: " + res.x + " x " + res.y
            : "Current: " + res.x + " x " + res.y;
#endif
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
