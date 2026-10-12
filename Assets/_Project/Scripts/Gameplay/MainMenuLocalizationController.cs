using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Swaps the baked menu button artwork when the locale changes.
/// English keeps the scene's original sprites and Chinese uses matching art.
/// </summary>
public sealed class MainMenuLocalizationController : MonoBehaviour
{
    private sealed class ButtonVisualState
    {
        public Image Background;
        public Sprite Sprite;
        public Image.Type ImageType;
        public Color ImageColor;
        public bool PreserveAspect;
        public bool RaycastTarget;
        public Graphic TargetGraphic;
        public ColorBlock Colors;
    }

    private readonly Dictionary<Button, ButtonVisualState> originalVisuals =
        new Dictionary<Button, ButtonVisualState>();
    private readonly HashSet<string> missingSprites = new HashSet<string>();

    private GameObject panelRoot;

    public void Bind(GameObject root)
    {
        panelRoot = root;
        ApplyLocalization();
    }

    private void OnEnable()
    {
        LocalizationTable.LocaleChanged += ApplyLocalization;
        ApplyLocalization();
    }

    private void OnDisable()
    {
        LocalizationTable.LocaleChanged -= ApplyLocalization;
    }

    private void ApplyLocalization()
    {
        if (panelRoot == null)
            return;

        Button[] buttons = panelRoot.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null || !TryGetChineseSpritePath(button.name, out string spritePath))
                continue;

            ButtonVisualState visual = GetOrCaptureVisual(button);
            if (visual == null)
                continue;

            RestoreOriginalArtwork(button, visual);
            HideOldTextOverlay(button);

            if (LocalizationTable.CurrentLocale != "zh-CN")
                continue;

            Sprite chineseSprite = Resources.Load<Sprite>(spritePath);
            if (chineseSprite == null)
            {
                if (missingSprites.Add(spritePath))
                    Debug.LogError($"Chinese menu button sprite is missing: Resources/{spritePath}");
                continue;
            }

            visual.Background.sprite = chineseSprite;
            visual.Background.type = Image.Type.Simple;
        }
    }

    private static bool TryGetChineseSpritePath(string buttonName, out string path)
    {
        switch (buttonName.ToLowerInvariant())
        {
            case "btn_setting":
            case "btn_settings":
                path = "Localization/MenuButtons/settings_zhCN";
                return true;
            case "btn_start":
            case "btn_startrun":
                path = "Localization/MenuButtons/start_zhCN";
                return true;
            case "btn_credit":
            case "btn_credits":
                path = "Localization/MenuButtons/credits_zhCN";
                return true;
            case "btn_quit":
            case "btn_exit":
                path = "Localization/MenuButtons/exit_zhCN";
                return true;
            default:
                path = null;
                return false;
        }
    }

    private ButtonVisualState GetOrCaptureVisual(Button button)
    {
        if (originalVisuals.TryGetValue(button, out ButtonVisualState visual))
            return visual;

        Image background = button.GetComponent<Image>();
        if (background == null)
            return null;

        visual = new ButtonVisualState
        {
            Background = background,
            Sprite = background.sprite,
            ImageType = background.type,
            ImageColor = background.color,
            PreserveAspect = background.preserveAspect,
            RaycastTarget = background.raycastTarget,
            TargetGraphic = button.targetGraphic,
            Colors = button.colors,
        };
        originalVisuals.Add(button, visual);
        return visual;
    }

    private static void RestoreOriginalArtwork(Button button, ButtonVisualState visual)
    {
        visual.Background.sprite = visual.Sprite;
        visual.Background.type = visual.ImageType;
        visual.Background.color = visual.ImageColor;
        visual.Background.preserveAspect = visual.PreserveAspect;
        visual.Background.raycastTarget = visual.RaycastTarget;
        button.targetGraphic = visual.TargetGraphic;
        button.colors = visual.Colors;
    }

    private static void HideOldTextOverlay(Button button)
    {
        Transform mask = button.transform.Find("LocalizedTextMask");
        if (mask != null)
            mask.gameObject.SetActive(false);

        Transform label = button.transform.Find("LocalizedLabel");
        if (label != null)
            label.gameObject.SetActive(false);
    }
}
