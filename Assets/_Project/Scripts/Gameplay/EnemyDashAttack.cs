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
    [SerializeField, Min(0.5f)] private float normalDuration = 3.0f;
    [LocalizedLabel("追踪间隔随机偏移")]
    [SerializeField, Min(0f)] private float normalDurationJitter = 0.8f;
    [LocalizedLabel("预警持续时间")]
    [SerializeField, Min(0.1f)] private float telegraphDuration = 0.5f;
    [LocalizedLabel("冲刺持续时间")]
    [SerializeField, Min(0.05f)] private float dashDuration = 0.3f;
    [LocalizedLabel("冲刺后冷却")]
    [SerializeField, Min(0.05f)] private float cooldownDuration = 0.4f;

    [Header("Dash / 冲刺")]
    [LocalizedLabel("冲刺速度")]
    [SerializeField, Min(1f)] private float dashSpeed = 16f;

    [Header("Telegraph Visual / 预警表现")]
    [LocalizedLabel("预警颜色")]
    [SerializeField] private Color telegraphColor = new Color(1f, 0.2f, 0.15f, 0.85f);
    [LocalizedLabel("预警线宽")]
    [SerializeField, Min(0.01f)] private float telegraphWidth = 0.08f;
    [LocalizedLabel("预警线长")]
    [SerializeField, Min(1f)] private float telegraphLength = 10f;
    [LocalizedLabel("预警排序层级")]
    [SerializeField] private int telegraphSortingOrder = 240;

    private EnemyController enemyController;
    private Rigidbody2D rb;

    private DashState state;
    private float stateTimer;
    private Vector2 dashTargetPos;
    private Vector2 dashStartPos;
    private float dashElapsed;
    private GameObject telegraphLine;

    private void Awake()
    {
        enemyController = GetComponent<EnemyController>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        EnterState(DashState.Normal);
    }

    private void OnDisable()
    {
        if (enemyController != null)
            enemyController.SuppressChaseMovement = false;
        DestroyTelegraph();
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
                stateTimer = Mathf.Max(0.5f, normalDuration + Random.Range(-normalDurationJitter, normalDurationJitter));
                break;

            case DashState.Telegraph:
                if (enemyController != null)
                    enemyController.SuppressChaseMovement = true;
                stateTimer = telegraphDuration;
                LockDashTarget();
                SpawnTelegraph();
                break;

            case DashState.Dash:
                stateTimer = dashDuration;
                dashElapsed = 0f;
                dashStartPos = rb != null ? rb.position : (Vector2)transform.position;
                DestroyTelegraph();
                break;

            case DashState.Cooldown:
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
        lr.sortingOrder = telegraphSortingOrder;
        lr.positionCount = 2;

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
}
