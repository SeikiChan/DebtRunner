using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuitConfirmDialog : MonoBehaviour
{
    [Serializable]
    private sealed class PanelRefs
    {
        public string debugName;
        public GameObject root;
        public CanvasGroup canvasGroup;
        public TMP_Text titleText;
        public TMP_Text bodyText;
        public Button confirmButton;
        public Button cancelButton;
    }

    [Header("Panel Roots / 面板根节点")]
    [SerializeField] private GameObject pauseQuitPanelRoot;
    [SerializeField] private GameObject titleExitPanelRoot;

    [Header("Fallback Copy / 默认文案")]
    [SerializeField] private string pauseQuitConfirmTitleMessage = "QUIT RUN?";
    [SerializeField, TextArea(2, 3)] private string pauseQuitConfirmBodyMessage = "Return to the title screen?\nYour current run progress will be lost.";
    [SerializeField] private string titleExitConfirmTitleMessage = "EXIT GAME?";
    [SerializeField, TextArea(1, 2)] private string titleExitConfirmBodyMessage = "Close the game now?";
    [SerializeField] private string confirmButtonLabel = "QUIT";
    [SerializeField] private string cancelButtonLabel = "CANCEL";
    [SerializeField] private bool useSceneAuthoredPauseText = true;
    [SerializeField] private bool useSceneAuthoredTitleText = true;
    [SerializeField] private bool useSceneAuthoredButtonLabels = true;

    [Header("Shared Style / 共用样式")]
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.72f);
    [SerializeField] private Color panelColor = new Color(0.11f, 0.12f, 0.14f, 0.97f);
    [SerializeField] private Color titleColor = new Color(0.98f, 0.95f, 0.88f, 1f);
    [SerializeField] private Color bodyColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private Color confirmButtonColor = new Color(0.73f, 0.22f, 0.18f, 1f);
    [SerializeField] private Color cancelButtonColor = new Color(0.23f, 0.27f, 0.32f, 1f);
    [SerializeField] private Color buttonTextColor = new Color(0.98f, 0.97f, 0.93f, 1f);
    [SerializeField] private float titleFontSize = 40f;
    [SerializeField] private float bodyFontSize = 26f;
    [SerializeField] private float buttonFontSize = 28f;

    [Header("Legacy Migration / 旧字段迁移")]
    [SerializeField, HideInInspector] private CanvasGroup pauseQuitOverlay;
    [SerializeField, HideInInspector] private TMP_Text pauseQuitTitleText;
    [SerializeField, HideInInspector] private TMP_Text pauseQuitBodyText;
    [SerializeField, HideInInspector] private Button pauseQuitConfirmButton;
    [SerializeField, HideInInspector] private Button pauseQuitCancelButton;
    [SerializeField, HideInInspector] private CanvasGroup titleExitOverlay;
    [SerializeField, HideInInspector] private TMP_Text titleExitTitleText;
    [SerializeField, HideInInspector] private TMP_Text titleExitBodyText;
    [SerializeField, HideInInspector] private Button titleExitConfirmButton;
    [SerializeField, HideInInspector] private Button titleExitCancelButton;

    private readonly PanelRefs pausePanel = new PanelRefs { debugName = "PauseQuit" };
    private readonly PanelRefs titlePanel = new PanelRefs { debugName = "TitleExit" };

    private PanelRefs activePanel;
    private Action confirmAction;
    private Action cancelAction;

    public bool IsOpen => activePanel != null && activePanel.root != null && activePanel.root.activeSelf;
    public GameObject NavigationRoot => IsOpen ? activePanel.root : null;

    private void Awake()
    {
        SyncPanels();
        HideAllPanelsImmediate();
    }

    private void OnDisable()
    {
        HideAllPanelsImmediate();
        activePanel = null;
        confirmAction = null;
        cancelAction = null;
    }

    public void Bind(GameObject pauseMenuPanelRef)
    {
        SyncPanels();
        HideAllPanelsImmediate();
    }

    public bool OpenPauseQuitConfirm(Action onConfirm, Action onCancel)
    {
        SyncPanels();
        return OpenPanel(
            pausePanel,
            pauseQuitConfirmTitleMessage,
            pauseQuitConfirmBodyMessage,
            applyConfiguredText: !useSceneAuthoredPauseText,
            applyConfiguredButtonLabels: !useSceneAuthoredButtonLabels,
            onConfirm,
            onCancel);
    }

    public bool OpenTitleExitConfirm(Action onConfirm, Action onCancel)
    {
        SyncPanels();
        return OpenPanel(
            titlePanel,
            titleExitConfirmTitleMessage,
            titleExitConfirmBodyMessage,
            applyConfiguredText: !useSceneAuthoredTitleText,
            applyConfiguredButtonLabels: !useSceneAuthoredButtonLabels,
            onConfirm,
            onCancel);
    }

    public void Close()
    {
        HideAllPanelsImmediate();
        activePanel = null;
        confirmAction = null;
        cancelAction = null;
    }

    private void SyncPanels()
    {
        MigrateLegacyRoots();
        SyncPanel(
            pausePanel,
            pauseQuitPanelRoot,
            pauseQuitOverlay,
            pauseQuitTitleText,
            pauseQuitBodyText,
            pauseQuitConfirmButton,
            pauseQuitCancelButton);
        SyncPanel(
            titlePanel,
            titleExitPanelRoot,
            titleExitOverlay,
            titleExitTitleText,
            titleExitBodyText,
            titleExitConfirmButton,
            titleExitCancelButton);
    }

    private void MigrateLegacyRoots()
    {
        if (pauseQuitPanelRoot == null && pauseQuitOverlay != null)
            pauseQuitPanelRoot = pauseQuitOverlay.gameObject;

        if (titleExitPanelRoot == null && titleExitOverlay != null)
            titleExitPanelRoot = titleExitOverlay.gameObject;
    }

    private void SyncPanel(
        PanelRefs target,
        GameObject root,
        CanvasGroup legacyOverlay,
        TMP_Text legacyTitle,
        TMP_Text legacyBody,
        Button legacyConfirm,
        Button legacyCancel)
    {
        target.root = root;
        target.canvasGroup = null;
        target.titleText = null;
        target.bodyText = null;
        target.confirmButton = null;
        target.cancelButton = null;

        if (target.root == null)
            return;

        target.canvasGroup = target.root.GetComponent<CanvasGroup>();
        if (target.canvasGroup == null)
            target.canvasGroup = target.root.AddComponent<CanvasGroup>();

        target.titleText = legacyTitle != null ? legacyTitle : FindChildComponent<TMP_Text>(target.root.transform, "Title");
        target.bodyText = legacyBody != null ? legacyBody : FindChildComponent<TMP_Text>(target.root.transform, "Body");
        target.confirmButton = legacyConfirm != null ? legacyConfirm : FindChildComponent<Button>(target.root.transform, "Btn_QuitConfirm");
        target.cancelButton = legacyCancel != null ? legacyCancel : FindChildComponent<Button>(target.root.transform, "Btn_QuitCancel");

        if (legacyOverlay != null && legacyOverlay.gameObject == target.root)
            target.canvasGroup = legacyOverlay;
    }

    private bool OpenPanel(
        PanelRefs panel,
        string title,
        string body,
        bool applyConfiguredText,
        bool applyConfiguredButtonLabels,
        Action onConfirm,
        Action onCancel)
    {
        if (!IsPanelBound(panel))
        {
            Debug.LogWarning($"QuitConfirmDialog: {panel.debugName} panel is not fully bound. Assign the panel root and keep Title/Body/Btn_QuitConfirm/Btn_QuitCancel inside it.", this);
            return false;
        }

        HideAllPanelsImmediate();
        BringToFront(panel);
        BindButtons(panel);
        ApplyVisualConfig(panel, title, body, applyConfiguredText, applyConfiguredButtonLabels);

        confirmAction = onConfirm;
        cancelAction = onCancel;
        activePanel = panel;
        SetPanelVisible(panel, true);
        return true;
    }

    private void BindButtons(PanelRefs panel)
    {
        BindButton(panel.confirmButton, HandleConfirmClicked);
        BindButton(panel.cancelButton, HandleCancelClicked);
        ConfigureNavigation(panel.confirmButton, panel.cancelButton);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void HandleConfirmClicked()
    {
        confirmAction?.Invoke();
    }

    private void HandleCancelClicked()
    {
        cancelAction?.Invoke();
    }

    private void ApplyVisualConfig(
        PanelRefs panel,
        string title,
        string body,
        bool applyConfiguredText,
        bool applyConfiguredButtonLabels)
    {
        Image backdrop = panel.root.GetComponent<Image>();
        if (backdrop != null)
        {
            backdrop.color = backdropColor;
            backdrop.raycastTarget = true;
        }

        ApplyTextVisual(panel.titleText, titleColor, titleFontSize, title, applyConfiguredText);
        ApplyTextVisual(panel.bodyText, bodyColor, bodyFontSize, body, applyConfiguredText);

        Image panelImage = FindChildComponent<Image>(panel.root.transform, "Panel");
        if (panelImage != null)
        {
            panelImage.color = panelColor;
            panelImage.raycastTarget = true;
        }

        ApplyButtonVisual(panel.confirmButton, confirmButtonColor, confirmButtonLabel, applyConfiguredButtonLabels);
        ApplyButtonVisual(panel.cancelButton, cancelButtonColor, cancelButtonLabel, applyConfiguredButtonLabels);
    }

    private static void ApplyTextVisual(TMP_Text text, Color color, float size, string value, bool overwriteText)
    {
        if (text == null)
            return;

        if (overwriteText)
            text.text = value;

        text.color = color;
        text.fontSize = size;
        text.raycastTarget = false;
    }

    private void ApplyButtonVisual(Button button, Color fillColor, string label, bool overwriteLabel)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = fillColor;
            image.raycastTarget = true;
            if (button.targetGraphic == null)
                button.targetGraphic = image;
        }

        button.interactable = true;

        TMP_Text labelText = button.GetComponentInChildren<TMP_Text>(true);
        if (labelText != null)
        {
            if (overwriteLabel)
                labelText.text = label;

            labelText.color = buttonTextColor;
            labelText.fontSize = buttonFontSize;
            labelText.raycastTarget = false;
        }
    }

    private static void ConfigureNavigation(Button confirmButton, Button cancelButton)
    {
        if (confirmButton != null)
        {
            Navigation navigation = confirmButton.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnRight = cancelButton;
            navigation.selectOnLeft = cancelButton;
            confirmButton.navigation = navigation;
        }

        if (cancelButton != null)
        {
            Navigation navigation = cancelButton.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnLeft = confirmButton;
            navigation.selectOnRight = confirmButton;
            cancelButton.navigation = navigation;
        }
    }

    private static void BringToFront(PanelRefs panel)
    {
        if (panel == null || panel.root == null)
            return;

        panel.root.transform.SetAsLastSibling();
    }

    private static void SetPanelVisible(PanelRefs panel, bool visible)
    {
        if (panel == null || panel.root == null || panel.canvasGroup == null)
            return;

        panel.root.SetActive(visible);
        panel.canvasGroup.alpha = visible ? 1f : 0f;
        panel.canvasGroup.interactable = visible;
        panel.canvasGroup.blocksRaycasts = visible;
    }

    private void HideAllPanelsImmediate()
    {
        SetPanelVisible(pausePanel, false);
        SetPanelVisible(titlePanel, false);
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null || child.name != childName)
                continue;

            T component = child.GetComponent<T>();
            if (component != null)
                return component;
        }

        return null;
    }

    private static bool IsPanelBound(PanelRefs panel)
    {
        return panel != null &&
               panel.root != null &&
               panel.canvasGroup != null &&
               panel.titleText != null &&
               panel.bodyText != null &&
               panel.confirmButton != null &&
               panel.cancelButton != null;
    }
}
