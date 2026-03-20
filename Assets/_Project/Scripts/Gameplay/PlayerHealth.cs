using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 60;
    [SerializeField] private float iFrameSeconds = 0.7f;
    [SerializeField, Range(0.1f, 1f)] private float reviveRestoreHealthPercent = 0.5f;
    [SerializeField, Min(0f)] private float reviveInvulnerabilitySeconds = 2f;

    [Header("Hurtbox")]
    [SerializeField] private bool autoConfigureHurtbox = true;
    [SerializeField] private bool fitHurtboxToPrimarySprite = true;
    [SerializeField] private Vector2 hurtboxSpriteScale = new Vector2(0.88f, 0.88f);
    [SerializeField] private Vector2 hurtboxSize = new Vector2(1.8f, 2.8f);
    [SerializeField] private Vector2 hurtboxOffset = new Vector2(0f, -0.06f);
    [SerializeField] private CapsuleDirection2D hurtboxDirection = CapsuleDirection2D.Vertical;

    [Header("Shield Visual")]
    [SerializeField] private Color shieldAuraColor = new Color(0.35f, 0.72f, 1f, 0.42f);
    [SerializeField, Min(0.5f)] private float shieldAuraScaleMultiplier = 2.2f;
    [SerializeField, Min(0f)] private float shieldAuraPulseAmplitude = 0.12f;
    [SerializeField, Min(0f)] private float shieldAuraPulseSpeed = 4.5f;
    [SerializeField] private bool showShieldSpriteOutline = true;
    [SerializeField] private Color shieldOutlineColor = new Color(0.32f, 0.78f, 1f, 0.92f);
    [SerializeField, Min(0.1f)] private float shieldOutlineThickness = 1.4f;
    [SerializeField, Min(0f)] private float shieldOutlinePulseAmplitude = 0.18f;
    [SerializeField, Min(0.1f)] private float periodicShieldUnlockInterval = 25f;
    [SerializeField, Min(0.1f)] private float periodicShieldMinInterval = 5f;
    [SerializeField, Min(1)] private int periodicShieldDefaultMaxCharges = 1;

    [Header("SFX / Audio")]
    [LocalizedLabel("Hurt SFX")]
    [SerializeField] private AudioClip sfxHurt;
    [LocalizedLabel("Shield Block SFX")]
    [SerializeField] private AudioClip sfxShieldBlock;
    [LocalizedLabel("Death SFX")]
    [SerializeField] private AudioClip sfxDeath;

    private int hp;
    private bool invuln;
    private float baseIFrameSeconds;

    private PlayerHitFeedback hitFeedback;
    private int shieldCharges;
    private int reviveCharges;
    private bool periodicShieldEnabled;
    private float periodicShieldInterval = 25f;
    private int periodicShieldMaxCharges = 1;
    private float periodicShieldTimer;
    private float invulnUntilUnscaledTime;
    private Coroutine invulnRoutine;
    private bool isDead;
    private CapsuleCollider2D hurtboxCollider;
    private SpriteRenderer shieldAuraRenderer;
    private SpriteRenderer[] playerSpriteRenderers;
    private SpriteRenderer shieldOutlineRenderer;
    private Material shieldOutlineMaterial;

    public int CurrentHP => hp;
    public int MaxHP => maxHP;
    public int ShieldCharges => shieldCharges;
    public int ReviveCharges => reviveCharges;
    public bool PeriodicShieldEnabled => periodicShieldEnabled;
    public float PeriodicShieldInterval => periodicShieldInterval;
    public bool IsDead => isDead;

    private void Awake()
    {
        hurtboxCollider = GetComponent<CapsuleCollider2D>();
        ConfigureHurtbox();
        hp = Mathf.Max(1, maxHP);
        baseIFrameSeconds = Mathf.Max(0f, iFrameSeconds);
        hitFeedback = GetComponent<PlayerHitFeedback>();
        playerSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        EnsureShieldAura();
        UpdateShieldAura(false);
    }

    private void ConfigureHurtbox()
    {
        if (!autoConfigureHurtbox)
            return;

        if (hurtboxCollider == null)
            hurtboxCollider = GetComponent<CapsuleCollider2D>();
        if (hurtboxCollider == null)
            return;

        Vector2 resolvedSize = new Vector2(
            Mathf.Max(0.05f, hurtboxSize.x),
            Mathf.Max(0.05f, hurtboxSize.y));

        if (fitHurtboxToPrimarySprite)
        {
            SpriteRenderer reference = GetPrimarySpriteRenderer();
            if (reference != null)
            {
                Vector2 spriteSize = ResolvePrimarySpriteLocalSize(reference);
                if (spriteSize.sqrMagnitude > 0.0001f)
                {
                    resolvedSize = Vector2.Scale(
                        spriteSize,
                        new Vector2(
                            Mathf.Max(0.1f, hurtboxSpriteScale.x),
                            Mathf.Max(0.1f, hurtboxSpriteScale.y)));
                }
            }
        }

        hurtboxCollider.size = resolvedSize;
        hurtboxCollider.offset = hurtboxOffset;
        hurtboxCollider.direction = hurtboxDirection;
    }

    private static Vector2 ResolvePrimarySpriteLocalSize(SpriteRenderer reference)
    {
        if (reference == null)
            return Vector2.zero;

        if (reference.drawMode != SpriteDrawMode.Simple)
            return reference.size;

        if (reference.sprite != null)
            return reference.sprite.bounds.size;

        return reference.size;
    }

    private void Update()
    {
        if (periodicShieldEnabled && periodicShieldInterval > 0f)
        {
            periodicShieldTimer -= Time.deltaTime;
            if (periodicShieldTimer <= 0f)
            {
                periodicShieldTimer = periodicShieldInterval;
                if (shieldCharges < periodicShieldMaxCharges)
                {
                    shieldCharges += 1;
                    RunLogger.Event($"Periodic shield granted. charges={shieldCharges}/{periodicShieldMaxCharges}");
                    UpdateShieldAura(true);
                }
            }
        }

        UpdateShieldAura(false);
    }

    private void OnDestroy()
    {
        if (shieldOutlineMaterial != null)
            Destroy(shieldOutlineMaterial);
    }

    public void RestoreHealth()
    {
        hp = Mathf.Max(1, maxHP);
        isDead = false;
        invuln = false;
        invulnUntilUnscaledTime = 0f;
        if (invulnRoutine != null)
        {
            StopCoroutine(invulnRoutine);
            invulnRoutine = null;
        }

        if (hitFeedback != null)
        {
            hitFeedback.StopBlink();
            hitFeedback.ResetVisualState();
        }

        RunLogger.Event($"Player health restored: {hp}/{maxHP}");
        UpdateShieldAura(false);
    }

    public void ResetRuntimeStats()
    {
        shieldCharges = 0;
        reviveCharges = 0;
        iFrameSeconds = Mathf.Max(0f, baseIFrameSeconds);
        periodicShieldEnabled = false;
        periodicShieldInterval = Mathf.Max(0.1f, periodicShieldUnlockInterval);
        periodicShieldMaxCharges = Mathf.Max(1, periodicShieldDefaultMaxCharges);
        periodicShieldTimer = 0f;
        UpdateShieldAura(false);
    }

    public void AddMaxHealth(int amount, bool healAddedAmount)
    {
        int add = Mathf.Max(0, amount);
        if (add == 0)
            return;

        maxHP = Mathf.Max(1, maxHP + add);
        if (healAddedAmount)
            hp = Mathf.Min(maxHP, hp + add);
        else
            hp = Mathf.Min(maxHP, hp);

        RunLogger.Event($"Player max health +{add}. hp={hp}/{maxHP}");
    }

    public void Heal(int amount)
    {
        int heal = Mathf.Max(0, amount);
        if (heal == 0)
            return;

        int before = hp;
        hp = Mathf.Min(maxHP, hp + heal);
        RunLogger.Event($"Player healed +{hp - before}. hp={hp}/{maxHP}");
    }

    public void AddShieldCharges(int amount)
    {
        int add = Mathf.Max(0, amount);
        if (add == 0)
            return;

        shieldCharges += add;
        RunLogger.Event($"Shield charges +{add}. current={shieldCharges}");
        UpdateShieldAura(true);
    }

    public void AddReviveCharges(int amount)
    {
        int add = Mathf.Max(0, amount);
        if (add == 0)
            return;

        reviveCharges += add;
        RunLogger.Event($"Revive charges +{add}. current={reviveCharges}");
    }

    public void AddInvulnerabilitySeconds(float amount)
    {
        float add = Mathf.Max(0f, amount);
        if (add <= 0f)
            return;

        iFrameSeconds = Mathf.Max(0f, iFrameSeconds + add);
        RunLogger.Event($"Player hit invulnerability +{add:F2}s. current={iFrameSeconds:F2}s");
    }

    public void EnablePeriodicShield(float intervalSeconds, int maxCharges)
    {
        periodicShieldEnabled = true;
        periodicShieldInterval = Mathf.Max(periodicShieldMinInterval, intervalSeconds);
        periodicShieldMaxCharges = Mathf.Max(1, maxCharges);
        periodicShieldTimer = periodicShieldInterval;
        RunLogger.Event($"Periodic shield enabled. interval={periodicShieldInterval:F1}s, maxCharges={periodicShieldMaxCharges}");
    }

    public void UpgradePeriodicShield(float cooldownReductionSeconds)
    {
        if (!periodicShieldEnabled)
        {
            EnablePeriodicShield(periodicShieldUnlockInterval, periodicShieldDefaultMaxCharges);
            GrantImmediateShieldCharge();
            return;
        }

        float reduction = Mathf.Max(0f, cooldownReductionSeconds);
        if (reduction > 0f)
        {
            periodicShieldInterval = Mathf.Max(periodicShieldMinInterval, periodicShieldInterval - reduction);
            if (periodicShieldTimer > periodicShieldInterval)
                periodicShieldTimer = periodicShieldInterval;

            RunLogger.Event($"Periodic shield cooldown reduced by {reduction:F1}s. currentInterval={periodicShieldInterval:F1}s");
        }

        GrantImmediateShieldCharge();
    }

    private void GrantImmediateShieldCharge()
    {
        int targetCharges = Mathf.Max(1, periodicShieldMaxCharges);
        if (shieldCharges >= targetCharges)
            return;

        shieldCharges = Mathf.Min(targetCharges, shieldCharges + 1);
        periodicShieldTimer = periodicShieldInterval;
        RunLogger.Event($"Immediate shield granted from shop. charges={shieldCharges}/{targetCharges}");
        UpdateShieldAura(true);
    }

    public void TakeDamage(int dmg)
    {
        if (invuln || isDead)
            return;

        if (shieldCharges > 0)
        {
            shieldCharges -= 1;
            RunLogger.Event($"Shield blocked damage. remaining={shieldCharges}");
            if (hitFeedback != null)
                hitFeedback.PlayHitFlash();
            if (sfxShieldBlock != null && SFXManager.Instance != null)
                SFXManager.Instance.Play(sfxShieldBlock);

            UpdateShieldAura(true);
            ApplyInvulnerabilityFor(iFrameSeconds);
            return;
        }

        int actualDamage = Mathf.Max(1, dmg);
        hp -= actualDamage;
        RunLogger.Warning($"Player took damage: -{actualDamage}, hp={Mathf.Max(hp, 0)}/{maxHP}");

        if (hitFeedback != null)
            hitFeedback.PlayHitFlash();

        if (hp <= 0)
        {
            if (TryConsumeRevive())
                return;

            isDead = true;
            invuln = false;
            invulnUntilUnscaledTime = 0f;
            if (invulnRoutine != null)
            {
                StopCoroutine(invulnRoutine);
                invulnRoutine = null;
            }

            if (hitFeedback != null)
                hitFeedback.StopBlink();

            if (sfxDeath != null && SFXManager.Instance != null)
                SFXManager.Instance.Play(sfxDeath);

            RunLogger.Warning("Player died.");
            GameFlowController.Instance.TriggerGameOver();
            return;
        }

        if (sfxHurt != null && SFXManager.Instance != null)
            SFXManager.Instance.Play(sfxHurt);

        ApplyInvulnerabilityFor(iFrameSeconds);
    }

    private bool TryConsumeRevive()
    {
        if (reviveCharges <= 0)
            return false;

        reviveCharges -= 1;
        hp = Mathf.Clamp(Mathf.RoundToInt(maxHP * Mathf.Clamp01(reviveRestoreHealthPercent)), 1, maxHP);
        invuln = false;
        invulnUntilUnscaledTime = 0f;

        if (hitFeedback != null)
        {
            hitFeedback.PlayHitFlash();
            hitFeedback.StopBlink();
        }

        GrantTemporaryInvulnerability(Mathf.Max(0f, reviveInvulnerabilitySeconds));
        RunLogger.Warning($"Revive triggered. hp={hp}/{maxHP}, remainingRevives={reviveCharges}");
        UpdateShieldAura(true);
        return true;
    }

    public void GrantTemporaryInvulnerability(float seconds)
    {
        float duration = Mathf.Max(0f, seconds);
        if (duration <= 0f)
            return;

        ApplyInvulnerabilityFor(duration);
        RunLogger.Event($"Player temporary invulnerability granted: {duration:F2}s");
    }

    private void ApplyInvulnerabilityFor(float seconds)
    {
        float duration = Mathf.Max(0f, seconds);
        if (duration <= 0f)
            return;

        float targetEndTime = Time.unscaledTime + duration;
        if (targetEndTime > invulnUntilUnscaledTime)
            invulnUntilUnscaledTime = targetEndTime;

        invuln = true;
        if (hitFeedback != null)
            hitFeedback.StartBlink();
        if (invulnRoutine == null)
            invulnRoutine = StartCoroutine(InvulnerabilityTimerRoutine());
    }

    private IEnumerator InvulnerabilityTimerRoutine()
    {
        while (Time.unscaledTime < invulnUntilUnscaledTime)
            yield return null;

        invuln = false;
        invulnRoutine = null;
        if (hitFeedback != null)
            hitFeedback.StopBlink();
    }

    private void EnsureShieldAura()
    {
        if (shieldAuraRenderer != null)
        {
            EnsureShieldOutline();
            return;
        }

        GameObject auraObject = new GameObject("ShieldAura", typeof(SpriteRenderer));
        auraObject.transform.SetParent(transform, false);
        shieldAuraRenderer = auraObject.GetComponent<SpriteRenderer>();
        shieldAuraRenderer.sprite = RuntimeSpriteFactory.GetShieldRingSprite();
        shieldAuraRenderer.color = new Color(shieldAuraColor.r, shieldAuraColor.g, shieldAuraColor.b, 0f);
        shieldAuraRenderer.enabled = false;

        SpriteRenderer reference = GetPrimarySpriteRenderer();
        if (reference != null)
        {
            shieldAuraRenderer.sortingLayerID = reference.sortingLayerID;
            shieldAuraRenderer.sortingOrder = reference.sortingOrder - 1;
        }

        shieldAuraRenderer.transform.localScale = Vector3.one * ResolveShieldAuraDiameter();
        EnsureShieldOutline();
    }

    private void UpdateShieldAura(bool snapPulse)
    {
        EnsureShieldAura();
        if (shieldAuraRenderer == null)
            return;

        if (shieldCharges <= 0)
        {
            shieldAuraRenderer.enabled = false;
            SetShieldOutlineVisible(false, 0f);
            return;
        }

        shieldAuraRenderer.enabled = true;
        float pulseT = snapPulse
            ? 1f
            : (0.5f + (0.5f * Mathf.Sin(Time.time * Mathf.Max(0.01f, shieldAuraPulseSpeed))));

        Color auraColor = shieldAuraColor;
        auraColor.a = Mathf.Clamp01(shieldAuraColor.a * (1f + (pulseT * shieldAuraPulseAmplitude)));
        shieldAuraRenderer.color = auraColor;

        float diameter = ResolveShieldAuraDiameter();
        float pulseScale = 1f + (pulseT * shieldAuraPulseAmplitude * 0.5f);
        shieldAuraRenderer.transform.localScale = Vector3.one * diameter * pulseScale;
        SetShieldOutlineVisible(showShieldSpriteOutline, pulseT);
    }

    private SpriteRenderer GetPrimarySpriteRenderer()
    {
        if (playerSpriteRenderers == null || playerSpriteRenderers.Length == 0)
            playerSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        SpriteRenderer best = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < playerSpriteRenderers.Length; i++)
        {
            SpriteRenderer sr = playerSpriteRenderers[i];
            if (sr == null)
                continue;

            string lowerName = sr.gameObject.name.ToLowerInvariant();
            if (lowerName.Contains("shadow") || lowerName.Contains("shieldaura") || lowerName.Contains("shieldoutline"))
                continue;

            int score = 0;
            if (sr.enabled && sr.gameObject.activeInHierarchy)
                score += 1000;

            if (sr.sprite != null)
                score += 500;

            if (sr.transform != transform)
                score += 250;
            else
                score -= 150;

            score += sr.sortingOrder * 10;

            if (score > bestScore)
            {
                bestScore = score;
                best = sr;
            }
        }

        return best;
    }

    private float ResolveShieldAuraDiameter()
    {
        if (playerSpriteRenderers == null || playerSpriteRenderers.Length == 0)
            playerSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        Bounds combinedBounds = new Bounds(transform.position, Vector3.one * 0.6f);
        bool initialized = false;
        for (int i = 0; i < playerSpriteRenderers.Length; i++)
        {
            SpriteRenderer sr = playerSpriteRenderers[i];
            if (sr == null || !sr.gameObject.activeInHierarchy)
                continue;

            if (!initialized)
            {
                combinedBounds = sr.bounds;
                initialized = true;
            }
            else
            {
                combinedBounds.Encapsulate(sr.bounds);
            }
        }

        float radius = initialized ? Mathf.Max(combinedBounds.extents.x, combinedBounds.extents.y) : 0.6f;
        return Mathf.Max(1f, radius * Mathf.Max(0.5f, shieldAuraScaleMultiplier) * 2f);
    }

    private void EnsureShieldOutline()
    {
        if (shieldOutlineRenderer != null)
            return;

        SpriteRenderer reference = GetPrimarySpriteRenderer();
        if (reference == null)
            return;

        GameObject outlineObject = new GameObject("ShieldOutline", typeof(SpriteRenderer));
        outlineObject.transform.SetParent(reference.transform, false);
        shieldOutlineRenderer = outlineObject.GetComponent<SpriteRenderer>();
        shieldOutlineRenderer.enabled = false;
        shieldOutlineRenderer.maskInteraction = reference.maskInteraction;
        shieldOutlineMaterial = CreateShieldOutlineMaterial();
        if (shieldOutlineMaterial != null)
            shieldOutlineRenderer.sharedMaterial = shieldOutlineMaterial;
    }

    private void SetShieldOutlineVisible(bool visible, float pulseT)
    {
        EnsureShieldOutline();
        if (shieldOutlineRenderer == null)
            return;

        SpriteRenderer reference = GetPrimarySpriteRenderer();
        if (reference == null || reference.sprite == null)
        {
            shieldOutlineRenderer.enabled = false;
            return;
        }

        if (!visible)
        {
            shieldOutlineRenderer.enabled = false;
            return;
        }

        Color outlineColor = shieldOutlineColor;
        outlineColor.a = Mathf.Clamp01(shieldOutlineColor.a * (1f + (pulseT * shieldOutlinePulseAmplitude)));
        float pulseScale = 1f + (pulseT * shieldOutlinePulseAmplitude * 0.2f);
        float outlineSize = Mathf.Max(0.1f, shieldOutlineThickness) * (1f + (pulseT * shieldOutlinePulseAmplitude * 0.35f));

        shieldOutlineRenderer.enabled = true;
        shieldOutlineRenderer.sprite = reference.sprite;
        shieldOutlineRenderer.flipX = reference.flipX;
        shieldOutlineRenderer.flipY = reference.flipY;
        shieldOutlineRenderer.drawMode = reference.drawMode;
        shieldOutlineRenderer.maskInteraction = reference.maskInteraction;
        shieldOutlineRenderer.sortingLayerID = reference.sortingLayerID;
        shieldOutlineRenderer.sortingOrder = reference.sortingOrder - 1;
        shieldOutlineRenderer.color = Color.white;

        Transform outlineTransform = shieldOutlineRenderer.transform;
        outlineTransform.localPosition = Vector3.zero;
        outlineTransform.localRotation = Quaternion.identity;
        outlineTransform.localScale = Vector3.one * (1f + (outlineSize * 0.035f)) * pulseScale;

        if (shieldOutlineMaterial != null)
        {
            shieldOutlineMaterial.SetColor("_OutlineColor", outlineColor);
            shieldOutlineMaterial.SetFloat("_OutlineSize", outlineSize);
        }
    }

    private Material CreateShieldOutlineMaterial()
    {
        Shader shader = Resources.Load<Shader>("Shaders/ShieldSpriteOutline");
        if (shader == null)
        {
            Debug.LogWarning("Shield outline shader missing at Resources/Shaders/ShieldSpriteOutline.");
            return null;
        }

        Material mat = new Material(shader);
        mat.name = "Runtime_ShieldOutline";
        mat.hideFlags = HideFlags.HideAndDontSave;
        mat.SetColor("_OutlineColor", shieldOutlineColor);
        mat.SetFloat("_OutlineSize", Mathf.Max(0.1f, shieldOutlineThickness));
        return mat;
    }
}
