using TMPro;
using UnityEngine;

/// <summary>
/// 冲刺敌人行为 — 平时慢速追踪，周期性红线预警+快速冲刺
/// 状态机: Normal → Telegraph → Dash → Cooldown → Normal
/// 需要搭配 EnemyController + EnemyContactDamage 使用
/// </summary>
[DisallowMultipleComponent]
public class EnemyDashAttack : MonoBehaviour
{
    private enum DashState { Normal, Telegraph, Dash, Cooldown }

    [Header("Timing / 时间")]
    [LocalizedLabel("普通追踪间隔")]
    [SerializeField, Min(0.5f)] private float normalDuration = 5.0f;
    [LocalizedLabel("追踪间隔随机偏移")]
    [SerializeField, Min(0f)] private float normalDurationJitter = 0.8f;
    [LocalizedLabel("预警持续时间")]
    [SerializeField, Min(0.1f)] private float telegraphDuration = 0.75f;
    [LocalizedLabel("冲刺持续时间")]
    [SerializeField, Min(0.05f)] private float dashDuration = 0.28f;
    [LocalizedLabel("冲刺后冷却")]
    [SerializeField, Min(0.05f)] private float cooldownDuration = 1.0f;

    [Header("Dash / 冲刺")]
    [LocalizedLabel("冲刺速度")]
    [SerializeField, Min(1f)] private float dashSpeed = 14f;

    [Header("Telegraph Visual / 预警表现")]
    [LocalizedLabel("预警颜色")]
    [SerializeField] private Color telegraphColor = new Color(1f, 0.2f, 0.15f, 0.80f);
    [LocalizedLabel("预警线宽")]
    [SerializeField, Min(0.01f)] private float telegraphWidth = 0.16f;
    [LocalizedLabel("预警线长")]
    [SerializeField, Min(1f)] private float telegraphLength = 8f;
    [LocalizedLabel("预警排序层级")]
    [SerializeField] private int telegraphSortingOrder = 240;
    [Header("Telegraph Feedback / 起手反馈")]
    [SerializeField] private bool showTelegraphIndicator = true;
    [SerializeField] private string telegraphIndicatorText = "!";
    [SerializeField] private Color telegraphIndicatorColor = new Color(1f, 0.92f, 0.2f, 1f);
    [SerializeField, Min(0.1f)] private float telegraphIndicatorHeight = 1.15f;
    [SerializeField, Min(1f)] private float telegraphIndicatorFontSize = 5.5f;
    [SerializeField] private bool flashBodyDuringTelegraph = true;
    [SerializeField] private Color telegraphFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField, Min(0f)] private float telegraphFlashFrequency = 10f;

    private EnemyController enemyController;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color baseSpriteColor = Color.white;
    private SpriteRenderer telegraphFlashOverlay;

    private DashState state;
    private float stateTimer;
    private Vector2 dashTargetPos;
    private Vector2 dashStartPos;
    private float dashElapsed;
    private GameObject telegraphLine;
    private TextMeshPro telegraphIndicator;

    private void Awake()
    {
        SanitizeValues();
        enemyController = GetComponent<EnemyController>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
            baseSpriteColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        SanitizeValues();
        EnterState(DashState.Normal);
    }

    private void OnValidate()
    {
        SanitizeValues();
    }

    private void OnDisable()
    {
        if (enemyController != null)
            enemyController.SuppressChaseMovement = false;
        RestoreSpriteColor();
        HideTelegraphFlashOverlay();
        DestroyTelegraph();
        DestroyTelegraphIndicator();
    }

    private void FixedUpdate()
    {
        stateTimer -= Time.fixedDeltaTime;

        switch (state)
        {
            case DashState.Normal:
                if (stateTimer <= 0f)
                    EnterState(DashState.Telegraph);
                break;

            case DashState.Telegraph:
                UpdateTelegraphVisual();
                UpdateTelegraphFeedback();
                if (stateTimer <= 0f)
                    EnterState(DashState.Dash);
                break;

            case DashState.Dash:
                TickDash();
                break;

            case DashState.Cooldown:
                if (stateTimer <= 0f)
                    EnterState(DashState.Normal);
                break;
        }
    }

    private void EnterState(DashState newState)
    {
        state = newState;

        switch (newState)
        {
            case DashState.Normal:
                if (enemyController != null)
                    enemyController.SuppressChaseMovement = false;
                RestoreSpriteColor();
                HideTelegraphFlashOverlay();
                DestroyTelegraphIndicator();
                stateTimer = Mathf.Max(0.5f, normalDuration + Random.Range(-normalDurationJitter, normalDurationJitter));
                break;

            case DashState.Telegraph:
                if (enemyController != null)
                    enemyController.SuppressChaseMovement = true;
                RestoreSpriteColor();
                stateTimer = telegraphDuration;
                LockDashTarget();
                SpawnTelegraph();
                SpawnTelegraphIndicator();
                break;

            case DashState.Dash:
                stateTimer = dashDuration;
                dashElapsed = 0f;
                dashStartPos = rb != null ? rb.position : (Vector2)transform.position;
                RestoreSpriteColor();
                HideTelegraphFlashOverlay();
                DestroyTelegraph();
                DestroyTelegraphIndicator();
                break;

            case DashState.Cooldown:
                RestoreSpriteColor();
                HideTelegraphFlashOverlay();
                DestroyTelegraphIndicator();
                stateTimer = cooldownDuration;
                break;
        }
    }

    private void LockDashTarget()
    {
        Transform player = enemyController != null ? enemyController.Player : null;
        Vector2 selfPos = rb != null ? rb.position : (Vector2)transform.position;

        if (player != null)
        {
            Vector2 toPlayer = (Vector2)player.position - selfPos;
            if (toPlayer.sqrMagnitude < 0.01f)
                toPlayer = Vector2.right;

            float dashDistance = dashSpeed * dashDuration;
            dashTargetPos = selfPos + toPlayer.normalized * dashDistance;
        }
        else
        {
            dashTargetPos = selfPos;
        }
    }

    private void TickDash()
    {
        dashElapsed += Time.fixedDeltaTime;
        float t = dashDuration > 0f ? Mathf.Clamp01(dashElapsed / dashDuration) : 1f;
        Vector2 nextPos = Vector2.Lerp(dashStartPos, dashTargetPos, t);

        if (CircleBoundary.Instance != null)
            nextPos = CircleBoundary.Instance.ClampPosition(nextPos);

        if (rb != null)
            rb.MovePosition(nextPos);
        else
            transform.position = (Vector3)nextPos;

        if (t >= 1f)
            EnterState(DashState.Cooldown);
    }

    private void SpawnTelegraph()
    {
        DestroyTelegraph();

        telegraphLine = new GameObject("DashTelegraph");
        telegraphLine.transform.SetParent(transform, false);

        LineRenderer lr = telegraphLine.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.alignment = LineAlignment.View;
        lr.textureMode = LineTextureMode.Stretch;
        lr.numCapVertices = 4;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.startWidth = telegraphWidth;
        lr.endWidth = telegraphWidth;
        lr.startColor = telegraphColor;
        lr.endColor = telegraphColor;
        lr.positionCount = 2;

        if (spriteRenderer != null)
        {
            lr.sortingLayerID = spriteRenderer.sortingLayerID;
            lr.sortingOrder = spriteRenderer.sortingOrder - 1;
        }
        else
        {
            lr.sortingOrder = telegraphSortingOrder;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            lr.material = new Material(shader);

        UpdateTelegraphVisual();
    }

    private void UpdateTelegraphVisual()
    {
        if (telegraphLine == null) return;
        LineRenderer lr = telegraphLine.GetComponent<LineRenderer>();
        if (lr == null) return;

        Vector2 origin = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 dir = dashTargetPos - origin;
        if (dir.sqrMagnitude < 0.01f)
            dir = Vector2.right;
        dir = dir.normalized;

        lr.SetPosition(0, (Vector3)origin);
        lr.SetPosition(1, (Vector3)(origin + dir * telegraphLength));
    }

    private void DestroyTelegraph()
    {
        if (telegraphLine != null)
        {
            Destroy(telegraphLine);
            telegraphLine = null;
        }
    }

    private void SpawnTelegraphIndicator()
    {
        if (!showTelegraphIndicator || telegraphIndicator != null)
            return;

        GameObject indicatorObject = new GameObject("DashTelegraphIndicator");
        indicatorObject.transform.SetParent(transform, false);
        indicatorObject.transform.localPosition = new Vector3(0f, telegraphIndicatorHeight, 0f);

        telegraphIndicator = indicatorObject.AddComponent<TextMeshPro>();
        telegraphIndicator.text = string.IsNullOrWhiteSpace(telegraphIndicatorText) ? "!" : telegraphIndicatorText;
        telegraphIndicator.fontSize = telegraphIndicatorFontSize;
        telegraphIndicator.alignment = TextAlignmentOptions.Center;
        telegraphIndicator.color = telegraphIndicatorColor;
        telegraphIndicator.raycastTarget = false;
        if (spriteRenderer != null)
        {
            telegraphIndicator.sortingLayerID = spriteRenderer.sortingLayerID;
            telegraphIndicator.sortingOrder = spriteRenderer.sortingOrder + 1;
        }
        else
        {
            telegraphIndicator.sortingOrder = telegraphSortingOrder + 5;
        }
    }

    private void UpdateTelegraphFeedback()
    {
        if (telegraphIndicator != null)
        {
            telegraphIndicator.transform.localPosition = new Vector3(0f, telegraphIndicatorHeight, 0f);
            float pulse = 0.82f + (Mathf.Abs(Mathf.Sin(Time.time * 8f)) * 0.28f);
            telegraphIndicator.transform.localScale = Vector3.one * pulse;
        }

        if (!flashBodyDuringTelegraph || spriteRenderer == null)
            return;

        float blend = 0.35f + (0.65f * Mathf.Abs(Mathf.Sin(Time.time * Mathf.Max(0.1f, telegraphFlashFrequency))));
        spriteRenderer.color = Color.Lerp(baseSpriteColor, telegraphFlashColor, blend);
        UpdateTelegraphFlashOverlay(blend);
    }

    private void DestroyTelegraphIndicator()
    {
        if (telegraphIndicator != null)
        {
            Destroy(telegraphIndicator.gameObject);
            telegraphIndicator = null;
        }
    }

    private void RestoreSpriteColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = baseSpriteColor;
    }

    private void EnsureTelegraphFlashOverlay()
    {
        if (telegraphFlashOverlay != null || spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        GameObject overlayObject = new GameObject("DashTelegraphFlashOverlay");
        overlayObject.transform.SetParent(spriteRenderer.transform, false);

        telegraphFlashOverlay = overlayObject.AddComponent<SpriteRenderer>();
        telegraphFlashOverlay.sprite = spriteRenderer.sprite;
        telegraphFlashOverlay.sortingLayerID = spriteRenderer.sortingLayerID;
        telegraphFlashOverlay.sortingOrder = spriteRenderer.sortingOrder + 1;
        telegraphFlashOverlay.maskInteraction = spriteRenderer.maskInteraction;
        telegraphFlashOverlay.drawMode = spriteRenderer.drawMode;
        telegraphFlashOverlay.size = spriteRenderer.size;
        telegraphFlashOverlay.flipX = spriteRenderer.flipX;
        telegraphFlashOverlay.flipY = spriteRenderer.flipY;
        telegraphFlashOverlay.enabled = false;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            telegraphFlashOverlay.material = new Material(shader);
    }

    private void UpdateTelegraphFlashOverlay(float blend)
    {
        EnsureTelegraphFlashOverlay();
        if (telegraphFlashOverlay == null || spriteRenderer == null)
            return;

        telegraphFlashOverlay.sprite = spriteRenderer.sprite;
        telegraphFlashOverlay.flipX = spriteRenderer.flipX;
        telegraphFlashOverlay.flipY = spriteRenderer.flipY;
        telegraphFlashOverlay.drawMode = spriteRenderer.drawMode;
        telegraphFlashOverlay.size = spriteRenderer.size;
        telegraphFlashOverlay.transform.localScale = Vector3.one;
        telegraphFlashOverlay.enabled = true;

        Color overlayColor = telegraphFlashColor;
        overlayColor.a *= Mathf.Clamp01(0.2f + (0.8f * blend));
        telegraphFlashOverlay.color = overlayColor;
    }

    private void HideTelegraphFlashOverlay()
    {
        if (telegraphFlashOverlay != null)
            telegraphFlashOverlay.enabled = false;
    }

    private void SanitizeValues()
    {
        normalDuration = Mathf.Max(0.5f, normalDuration);
        normalDurationJitter = Mathf.Clamp(normalDurationJitter, 0f, Mathf.Max(0f, normalDuration - 0.1f));
        telegraphDuration = Mathf.Clamp(telegraphDuration, 0.1f, 2f);
        dashDuration = Mathf.Clamp(dashDuration, 0.05f, 1f);
        cooldownDuration = Mathf.Clamp(cooldownDuration, 0.05f, 3f);
        dashSpeed = Mathf.Clamp(dashSpeed, 1f, 24f);
        telegraphWidth = Mathf.Clamp(telegraphWidth, 0.02f, 0.35f);
        telegraphLength = Mathf.Clamp(telegraphLength, 1f, 12f);
        telegraphColor.a = Mathf.Clamp(telegraphColor.a, 0.15f, 1f);
        telegraphIndicatorHeight = Mathf.Clamp(telegraphIndicatorHeight, 0.1f, 4f);
        telegraphIndicatorFontSize = Mathf.Clamp(telegraphIndicatorFontSize, 1f, 12f);
        telegraphFlashFrequency = Mathf.Clamp(telegraphFlashFrequency, 0f, 24f);
        telegraphFlashColor.a = Mathf.Clamp01(telegraphFlashColor.a);
    }
}
