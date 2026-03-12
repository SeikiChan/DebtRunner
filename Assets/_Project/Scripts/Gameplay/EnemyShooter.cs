using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private float fireInterval = 2.5f;
    [SerializeField, Min(0.05f)] private float preFireStopDuration = 0.45f;
    [SerializeField, Min(0f)] private float postFireRecoveryDuration = 0.2f;
    [SerializeField] private float projectileSpeed = 5.2f;
    [SerializeField] private int damage = 1;

    private Transform player;
    private Transform projectilesRoot;
    private EnemyController enemyController;
    private Rigidbody2D rb;
    private float timer;
    private float windupTimer;
    private float recoveryTimer;
    private bool preparingShot;
    private Vector2 lockedShotDirection;

    private void Awake()
    {
        enemyController = GetComponent<EnemyController>();
        rb = GetComponent<Rigidbody2D>();
        SanitizeValues();
    }

    public void Init(Transform playerTf, Transform projectilesParent)
    {
        player = playerTf;
        projectilesRoot = projectilesParent;
    }

    private void OnEnable()
    {
        SanitizeValues();
        // Avoid instant spawn shots; gives player a readable reaction window.
        timer = Random.Range(fireInterval * 0.6f, fireInterval);
        windupTimer = 0f;
        recoveryTimer = 0f;
        preparingShot = false;
        SetMovementSuppressed(false);
    }

    private void OnDisable()
    {
        SetMovementSuppressed(false);
    }

    private void Update()
    {
        if (player == null || projectilePrefab == null || projectilesRoot == null) return;

        if (preparingShot)
        {
            SetMovementSuppressed(true);
            windupTimer -= Time.deltaTime;
            if (windupTimer <= 0f)
            {
                FireLockedShot();
                preparingShot = false;
                recoveryTimer = postFireRecoveryDuration;
            }
            return;
        }

        if (recoveryTimer > 0f)
        {
            SetMovementSuppressed(true);
            recoveryTimer -= Time.deltaTime;
            if (recoveryTimer <= 0f)
                SetMovementSuppressed(false);
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position);
        if (dir.sqrMagnitude <= 0.0001f)
            dir = Vector2.right;

        lockedShotDirection = dir.normalized;
        preparingShot = true;
        windupTimer = preFireStopDuration;
        timer = fireInterval;
        SetMovementSuppressed(true);
    }

    private void FireLockedShot()
    {
        Vector2 dir = lockedShotDirection.sqrMagnitude > 0.0001f ? lockedShotDirection : Vector2.right;
        var p = Instantiate(projectilePrefab, transform.position, Quaternion.identity, projectilesRoot);
        p.Fire(dir, projectileSpeed, damage);
    }

    private void SetMovementSuppressed(bool suppressed)
    {
        if (enemyController != null)
            enemyController.SuppressChaseMovement = suppressed;

        if (suppressed && rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void SanitizeValues()
    {
        fireInterval = Mathf.Max(0.25f, fireInterval);
        preFireStopDuration = Mathf.Max(0.05f, preFireStopDuration);
        postFireRecoveryDuration = Mathf.Max(0f, postFireRecoveryDuration);
        projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
        damage = Mathf.Max(1, damage);
    }
}
