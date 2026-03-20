using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerActiveItemController : MonoBehaviour
{
    [Serializable]
    private class ActiveItemProfile
    {
        public ActiveItemId itemId = ActiveItemId.None;
        public string displayName = "Active Item";
        [Min(0f)] public float cooldownSeconds = 10f;
        [Min(0f)] public float activeDurationSeconds = 0f;

        [Header("Dash")]
        [Min(0f)] public float dashImpulse = 0f;
        [Min(0f)] public float dashInvulnerabilitySeconds = 0f;

        [Header("Timed Buffs")]
        public float moveSpeedPercent = 0f;
        public float damagePercent = 0f;
        public float fireRatePercent = 0f;
        public float projectileSpeedPercent = 0f;
        [Min(0f)] public float buffInvulnerabilitySeconds = 0f;
    }

    [Header("References")]
    [SerializeField] private GameFlowController gameFlow;
    [SerializeField] private GameObject panelHUD;
    [SerializeField] private PlayerMotor2D playerMotor;
    [SerializeField] private PlayerShooter playerShooter;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("HUD Panel")]
    [SerializeField] private GameObject activeItemPanelRoot;
    [SerializeField, HideInInspector] private CanvasGroup activeItemOverlay;
    [SerializeField, HideInInspector] private Image activeItemFrameImage;
    [SerializeField, HideInInspector] private TMP_Text activeItemNameText;
    [SerializeField, HideInInspector] private TMP_Text activeItemStatusText;
    [SerializeField, HideInInspector] private TMP_Text activeItemCooldownValueText;
    [SerializeField, HideInInspector] private Image activeItemCooldownWheelImage;
    [SerializeField, HideInInspector] private Image activeItemReadyGlowImage;

    [Header("HUD Style")]
    [SerializeField] private bool autoCreateOverlay = true;
    [SerializeField] private Vector2 fallbackPanelSize = new Vector2(334f, 96f);
    [SerializeField] private Vector2 fallbackAnchoredPosition = new Vector2(-28f, 94f);
    [SerializeField] private Color fallbackFrameColor = new Color(0.09f, 0.09f, 0.11f, 0.96f);
    [SerializeField] private Color fallbackNameColor = new Color(0.99f, 0.95f, 0.86f, 1f);
    [SerializeField] private Color fallbackStatusColor = new Color(0.88f, 0.90f, 0.93f, 1f);
    [SerializeField] private Color cooldownWheelColor = new Color(0f, 0f, 0f, 0.82f);
    [SerializeField] private Color activeWheelColor = new Color(1f, 0.56f, 0.16f, 0.92f);
    [SerializeField] private Color frameCoolingColor = new Color(0.12f, 0.12f, 0.15f, 0.98f);
    [SerializeField] private Color frameActiveColor = new Color(0.95f, 0.50f, 0.14f, 1f);
    [SerializeField] private Color frameReadyColor = new Color(1f, 0.85f, 0.22f, 1f);
    [SerializeField] private Color readyGlowColor = new Color(1f, 0.90f, 0.34f, 0.72f);
    [SerializeField] private bool pulseReadyGlow = true;
    [SerializeField, Min(0.01f)] private float readyGlowPulseSpeed = 3.8f;
    [SerializeField, Range(0f, 1f)] private float readyGlowPulseMinAlpha = 0.18f;
    [SerializeField, Range(0f, 1f)] private float readyGlowPulseMaxAlpha = 0.78f;
    [SerializeField] private string readyStatus = "[SPACE] READY";
    [SerializeField] private string cooldownStatus = "COOLDOWN";
    [SerializeField] private string activeStatus = "ACTIVE";

    [Header("Starter Item")]
    [SerializeField] private bool equipStarterItemOnRunReset = true;
    [SerializeField] private ActiveItemId starterItem = ActiveItemId.SkiptraceBurst;

    [Header("Profiles")]
    [SerializeField] private List<ActiveItemProfile> itemProfiles = new List<ActiveItemProfile>
    {
        new ActiveItemProfile
        {
            itemId = ActiveItemId.SkiptraceBurst,
            displayName = "Skiptrace Burst",
            cooldownSeconds = 8f,
            dashImpulse = 18f,
            dashInvulnerabilitySeconds = 0.28f,
        },
        new ActiveItemProfile
        {
            itemId = ActiveItemId.RedlineOvertime,
            displayName = "Redline Overtime",
            cooldownSeconds = 18f,
            activeDurationSeconds = 6f,
            damagePercent = 35f,
            fireRatePercent = 45f,
            projectileSpeedPercent = 25f,
        },
        new ActiveItemProfile
        {
            itemId = ActiveItemId.GraceWindow,
            displayName = "Grace Window",
            cooldownSeconds = 16f,
            activeDurationSeconds = 4f,
            moveSpeedPercent = 60f,
            buffInvulnerabilitySeconds = 1.2f,
        },
    };

    private ActiveItemId equippedItem = ActiveItemId.None;
    private ActiveItemProfile equippedProfile;
    private float cooldownRemaining;
    private float activeDurationRemaining;
    private float appliedMoveSpeedPercent;
    private bool overlayAutoCreated;
    private bool equippedItemIsStarter;
    private int totalUses;

    public ActiveItemId EquippedItem => equippedItem;
    public bool HasEquippedItem => equippedProfile != null && equippedItem != ActiveItemId.None;
    public bool HasPurchasedEquippedItem => HasEquippedItem && !equippedItemIsStarter;
    public bool IsStarterItemEquipped => HasEquippedItem && equippedItemIsStarter;
    public bool IsStarterDashEquipped => IsStarterItemEquipped && equippedItem == ActiveItemId.SkiptraceBurst;
    public int TotalUses => totalUses;

    public void Bind(
        GameFlowController flow,
        GameObject hudPanel,
        PlayerMotor2D motor,
        PlayerShooter shooter,
        PlayerHealth health)
    {
        gameFlow = flow;
        panelHUD = hudPanel;
        playerMotor = motor;
        playerShooter = shooter;
        playerHealth = health;
        ResolveScenePanelBindings();
        RefreshHUD();
    }

    public void ResetRuntimeState()
    {
        ClearActiveEffect();
        equippedItem = ActiveItemId.None;
        equippedProfile = null;
        cooldownRemaining = 0f;
        activeDurationRemaining = 0f;
        equippedItemIsStarter = false;
        totalUses = 0;

        if (!TryEquipStarterItem())
            RefreshHUD();
    }

    public bool Equip(ActiveItemId itemId)
    {
        return EquipInternal(itemId, false);
    }

    private bool EquipInternal(ActiveItemId itemId, bool isStarter)
    {
        ActiveItemProfile profile = GetProfile(itemId);
        if (profile == null)
        {
            RunLogger.Warning($"Active item equip ignored. Unknown item id={itemId}.");
            return false;
        }

        ClearActiveEffect();
        equippedItem = itemId;
        equippedProfile = profile;
        cooldownRemaining = 0f;
        activeDurationRemaining = 0f;
        equippedItemIsStarter = isStarter;
        EnsureOverlay();
        RefreshHUD();
        RunLogger.Event(isStarter
            ? $"Starter active item equipped: {profile.displayName}"
            : $"Active item equipped: {profile.displayName}");
        return true;
    }

    private bool TryEquipStarterItem()
    {
        if (!equipStarterItemOnRunReset || starterItem == ActiveItemId.None)
            return false;

        return EquipInternal(starterItem, true);
    }

    private void Update()
    {
        ResolveMissingRefs();
        ResolveScenePanelBindings();

        if (gameFlow != null && !gameFlow.IsInGameplayState && activeDurationRemaining > 0f)
            ClearActiveEffect();

        if (ShouldTickGameplayTimers())
        {
            TickRuntime(Time.deltaTime);

            if (HasEquippedItem && cooldownRemaining <= 0f && GameInput.IsActiveItemPressed())
                TryUseEquippedItem();
        }

        RefreshHUD();
    }

    private void TickRuntime(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        if (cooldownRemaining > 0f)
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - deltaTime);

        if (activeDurationRemaining > 0f)
        {
            activeDurationRemaining = Mathf.Max(0f, activeDurationRemaining - deltaTime);
            if (activeDurationRemaining <= 0f)
                ClearActiveEffect();
        }
    }

    private bool TryUseEquippedItem()
    {
        if (!HasEquippedItem || equippedProfile == null)
            return false;

        if (playerHealth != null && playerHealth.IsDead)
            return false;

        ClearActiveEffect();
        ExecuteInstantEffects(equippedProfile);

        if (equippedProfile.activeDurationSeconds > 0f)
            ApplyTimedEffects(equippedProfile);

        totalUses++;
        cooldownRemaining = Mathf.Max(0f, equippedProfile.cooldownSeconds);
        RefreshHUD();
        RunLogger.Event($"Active item used: {equippedProfile.displayName}");
        return true;
    }

    private void ExecuteInstantEffects(ActiveItemProfile profile)
    {
        if (profile == null)
            return;

        if (profile.dashImpulse > 0f && playerMotor != null)
            playerMotor.ApplyExternalImpulse(ResolveDashDirection(), profile.dashImpulse);

        if (profile.dashInvulnerabilitySeconds > 0f && playerHealth != null)
            playerHealth.GrantTemporaryInvulnerability(profile.dashInvulnerabilitySeconds);
    }

    private void ApplyTimedEffects(ActiveItemProfile profile)
    {
        if (profile == null)
            return;

        activeDurationRemaining = Mathf.Max(0f, profile.activeDurationSeconds);
        appliedMoveSpeedPercent = Mathf.Max(0f, profile.moveSpeedPercent);

        if (appliedMoveSpeedPercent > 0f && playerMotor != null)
            playerMotor.AddMoveSpeedPercent(appliedMoveSpeedPercent);

        if (playerShooter != null)
        {
            playerShooter.SetTemporaryCombatBuff(
                profile.damagePercent,
                profile.fireRatePercent,
                profile.projectileSpeedPercent);
        }

        if (profile.buffInvulnerabilitySeconds > 0f && playerHealth != null)
            playerHealth.GrantTemporaryInvulnerability(profile.buffInvulnerabilitySeconds);
    }

    private void ClearActiveEffect()
    {
        if (appliedMoveSpeedPercent > 0f && playerMotor != null)
            playerMotor.AddMoveSpeedPercent(-appliedMoveSpeedPercent);

        appliedMoveSpeedPercent = 0f;
        activeDurationRemaining = 0f;

        if (playerShooter != null)
            playerShooter.ClearTemporaryCombatBuff();
    }

    private Vector2 ResolveDashDirection()
    {
        if (playerMotor == null)
            return Vector2.right;

        if (playerMotor.CurrentMoveInput.sqrMagnitude > 0.001f)
            return playerMotor.CurrentMoveInput.normalized;

        if (playerMotor.LastMoveDir.sqrMagnitude > 0.001f)
            return playerMotor.LastMoveDir.normalized;

        return Vector2.right;
    }

    private bool ShouldTickGameplayTimers()
    {
        return gameFlow != null
            && gameFlow.IsInGameplayState
            && Time.timeScale > 0.0001f;
    }

    private ActiveItemProfile GetProfile(ActiveItemId itemId)
    {
        for (int i = 0; i < itemProfiles.Count; i++)
        {
            ActiveItemProfile profile = itemProfiles[i];
            if (profile == null)
                continue;

            if (profile.itemId == itemId)
                return profile;
        }

        return null;
    }

    private void RefreshHUD()
    {
        bool shouldShow = HasEquippedItem
            && gameFlow != null
            && gameFlow.IsInGameplayState
            && panelHUD != null
            && panelHUD.activeInHierarchy
            && EnsureOverlay();

        if (activeItemOverlay != null)
            activeItemOverlay.gameObject.SetActive(shouldShow);

        if (!shouldShow || equippedProfile == null)
            return;

        bool isActive = activeDurationRemaining > 0.001f;
        bool isCooling = !isActive && cooldownRemaining > 0.001f;
        bool isReady = !isActive && !isCooling;

        if (activeItemNameText != null)
            activeItemNameText.text = equippedProfile.displayName;

        if (activeItemStatusText != null)
        {
            if (isActive)
                activeItemStatusText.text = activeStatus;
            else if (isCooling)
                activeItemStatusText.text = cooldownStatus;
            else
                activeItemStatusText.text = readyStatus;
        }

        if (activeItemCooldownValueText != null)
        {
            if (isActive)
                activeItemCooldownValueText.text = $"{activeDurationRemaining:0.0}";
            else if (isCooling)
                activeItemCooldownValueText.text = $"{cooldownRemaining:0.0}";
            else
                activeItemCooldownValueText.text = string.Empty;
        }

        if (activeItemFrameImage != null)
        {
            activeItemFrameImage.color = isActive
                ? frameActiveColor
                : isReady
                    ? frameReadyColor
                    : frameCoolingColor;
        }

        if (activeItemCooldownWheelImage != null)
        {
            activeItemCooldownWheelImage.enabled = isActive || isCooling;
            activeItemCooldownWheelImage.color = isActive ? activeWheelColor : cooldownWheelColor;

            if (isActive)
            {
                float duration = Mathf.Max(0.01f, equippedProfile.activeDurationSeconds);
                activeItemCooldownWheelImage.fillAmount = Mathf.Clamp01(activeDurationRemaining / duration);
            }
            else if (isCooling)
            {
                float duration = Mathf.Max(0.01f, equippedProfile.cooldownSeconds);
                activeItemCooldownWheelImage.fillAmount = Mathf.Clamp01(cooldownRemaining / duration);
            }
            else
            {
                activeItemCooldownWheelImage.fillAmount = 0f;
            }
        }

        if (activeItemReadyGlowImage != null)
        {
            activeItemReadyGlowImage.enabled = isReady;
            if (isReady)
            {
                Color glow = readyGlowColor;
                if (pulseReadyGlow)
                {
                    float t = Mathf.PingPong(Time.unscaledTime * Mathf.Max(0.01f, readyGlowPulseSpeed), 1f);
                    glow.a = Mathf.Lerp(readyGlowPulseMinAlpha, readyGlowPulseMaxAlpha, t);
                }

                activeItemReadyGlowImage.color = glow;
            }
        }
    }

    private bool EnsureOverlay()
    {
        ResolveScenePanelBindings();

        bool hasSceneBindings = activeItemOverlay != null
            && activeItemNameText != null
            && activeItemStatusText != null
            && activeItemCooldownValueText != null
            && activeItemCooldownWheelImage != null;
        if (hasSceneBindings)
            return true;

        if (!autoCreateOverlay || overlayAutoCreated)
            return false;

        Canvas canvas = panelHUD != null ? panelHUD.GetComponentInParent<Canvas>() : FindObjectOfType<Canvas>();
        if (canvas == null)
            return false;

        GameObject root = new GameObject("Panel_ActiveItemHUD", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(canvas.transform, false);
        rootRect.anchorMin = new Vector2(1f, 0f);
        rootRect.anchorMax = new Vector2(1f, 0f);
        rootRect.pivot = new Vector2(1f, 0f);
        rootRect.sizeDelta = fallbackPanelSize;
        rootRect.anchoredPosition = fallbackAnchoredPosition;

        activeItemPanelRoot = root;
        activeItemOverlay = root.GetComponent<CanvasGroup>();
        activeItemOverlay.alpha = 1f;
        activeItemOverlay.interactable = false;
        activeItemOverlay.blocksRaycasts = false;

        GameObject frameObject = new GameObject("Image_Frame", typeof(RectTransform), typeof(Image));
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        frameRect.SetParent(rootRect, false);
        Stretch(frameRect);

        activeItemFrameImage = frameObject.GetComponent<Image>();
        activeItemFrameImage.color = fallbackFrameColor;
        activeItemFrameImage.raycastTarget = false;

        activeItemNameText = CreateText(
            "Text_ItemName",
            frameRect,
            new Vector2(16f, -14f),
            new Vector2(190f, 30f),
            "ACTIVE ITEM",
            24f,
            FontStyles.Bold,
            fallbackNameColor,
            TextAlignmentOptions.Left);

        activeItemStatusText = CreateText(
            "Text_Status",
            frameRect,
            new Vector2(16f, -50f),
            new Vector2(190f, 24f),
            readyStatus,
            18f,
            FontStyles.Bold,
            fallbackStatusColor,
            TextAlignmentOptions.Left);

        GameObject cooldownRoot = new GameObject("Panel_Cooldown", typeof(RectTransform));
        RectTransform cooldownRootRect = cooldownRoot.GetComponent<RectTransform>();
        cooldownRootRect.SetParent(frameRect, false);
        cooldownRootRect.anchorMin = new Vector2(1f, 0.5f);
        cooldownRootRect.anchorMax = new Vector2(1f, 0.5f);
        cooldownRootRect.pivot = new Vector2(1f, 0.5f);
        cooldownRootRect.sizeDelta = new Vector2(68f, 68f);
        cooldownRootRect.anchoredPosition = new Vector2(-12f, 0f);

        Image cooldownBackplate = CreateDiscImage(
            "Image_CooldownBackplate",
            cooldownRootRect,
            Color.black,
            0.18f);
        cooldownBackplate.transform.SetAsFirstSibling();

        activeItemReadyGlowImage = CreateDiscImage(
            "Image_ReadyGlow",
            cooldownRootRect,
            readyGlowColor,
            1f);

        activeItemCooldownWheelImage = CreateDiscImage(
            "Image_CooldownWheel",
            cooldownRootRect,
            cooldownWheelColor,
            1f);
        activeItemCooldownWheelImage.type = Image.Type.Filled;
        activeItemCooldownWheelImage.fillMethod = Image.FillMethod.Radial360;
        activeItemCooldownWheelImage.fillOrigin = (int)Image.Origin360.Top;
        activeItemCooldownWheelImage.fillClockwise = true;
        activeItemCooldownWheelImage.fillAmount = 0f;

        activeItemCooldownValueText = CreateText(
            "Text_CooldownValue",
            cooldownRootRect,
            new Vector2(0f, -3f),
            new Vector2(58f, 24f),
            string.Empty,
            20f,
            FontStyles.Bold,
            fallbackNameColor,
            TextAlignmentOptions.Center);

        activeItemOverlay.gameObject.SetActive(false);
        overlayAutoCreated = true;
        return true;
    }

    private void ResolveScenePanelBindings()
    {
        if (activeItemPanelRoot == null)
        {
            if (activeItemOverlay != null)
                activeItemPanelRoot = activeItemOverlay.gameObject;
            else
                return;
        }

        if (activeItemOverlay == null)
        {
            activeItemOverlay = activeItemPanelRoot.GetComponent<CanvasGroup>();
            if (activeItemOverlay == null)
                activeItemOverlay = activeItemPanelRoot.AddComponent<CanvasGroup>();
        }

        Transform root = activeItemPanelRoot.transform;
        if (activeItemFrameImage == null)
            activeItemFrameImage = FindChildComponent<Image>(root, "Image_Frame");
        if (activeItemNameText == null)
            activeItemNameText = FindChildComponent<TMP_Text>(root, "Text_ItemName");
        if (activeItemStatusText == null)
            activeItemStatusText = FindChildComponent<TMP_Text>(root, "Text_Status");
        if (activeItemCooldownValueText == null)
            activeItemCooldownValueText = FindChildComponent<TMP_Text>(root, "Text_CooldownValue");
        if (activeItemCooldownWheelImage == null)
            activeItemCooldownWheelImage = FindChildComponent<Image>(root, "Image_CooldownWheel");
        if (activeItemReadyGlowImage == null)
            activeItemReadyGlowImage = FindChildComponent<Image>(root, "Image_ReadyGlow");

        if (activeItemCooldownWheelImage != null)
        {
            activeItemCooldownWheelImage.type = Image.Type.Filled;
            activeItemCooldownWheelImage.fillMethod = Image.FillMethod.Radial360;
            activeItemCooldownWheelImage.fillOrigin = (int)Image.Origin360.Top;
            activeItemCooldownWheelImage.fillClockwise = true;
            activeItemCooldownWheelImage.raycastTarget = false;
        }

        if (activeItemReadyGlowImage != null)
            activeItemReadyGlowImage.raycastTarget = false;
        if (activeItemFrameImage != null)
            activeItemFrameImage.raycastTarget = false;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        string value,
        float fontSize,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        text.outlineWidth = 0.2f;
        return text;
    }

    private static Image CreateDiscImage(string name, Transform parent, Color color, float scale)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.one * (64f * Mathf.Max(0.1f, scale));

        Image image = go.GetComponent<Image>();
        image.sprite = RuntimeSpriteFactory.GetHitPulseSprite();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component != null && component.gameObject.name == childName)
                return component;
        }

        return null;
    }

    private void ResolveMissingRefs()
    {
        if (gameFlow == null)
            gameFlow = GetComponent<GameFlowController>() ?? GameFlowController.Instance;

        if (playerMotor == null)
            playerMotor = FindObjectOfType<PlayerMotor2D>();

        if (playerShooter == null)
            playerShooter = FindObjectOfType<PlayerShooter>();

        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();
    }
}
