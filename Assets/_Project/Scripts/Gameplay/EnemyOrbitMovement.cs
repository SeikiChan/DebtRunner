using UnityEngine;

/// <summary>
/// 宝箱怪环绕运动 — 绕玩家公转，不追踪不攻击
/// 永久覆盖 EnemyController 的默认追踪移动
/// </summary>
[DisallowMultipleComponent]
public class EnemyOrbitMovement : MonoBehaviour
{
    [Header("Orbit / 环绕")]
    [LocalizedLabel("环绕半径")]
    [SerializeField, Min(0.5f)] private float orbitRadius = 4f;
    [LocalizedLabel("环绕角速度 (度/秒)")]
    [SerializeField] private float angularSpeed = 75f;
    [LocalizedLabel("半径趋近速度")]
    [SerializeField, Min(0.1f)] private float radiusApproachSpeed = 3f;
    [LocalizedLabel("随机起始角度")]
    [SerializeField] private bool randomStartAngle = true;

    private EnemyController enemyController;
    private Rigidbody2D rb;
    private float currentAngle;

    private void Awake()
    {
        enemyController = GetComponent<EnemyController>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (enemyController != null)
            enemyController.SuppressChaseMovement = true;

        Transform player = enemyController != null ? enemyController.Player : null;
        if (player != null)
        {
            Vector2 offset = (Vector2)transform.position - (Vector2)player.position;
            currentAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        }
        else if (randomStartAngle)
        {
            currentAngle = Random.Range(0f, 360f);
        }
    }

    private void OnDisable()
    {
        if (enemyController != null)
            enemyController.SuppressChaseMovement = false;
    }

    private void FixedUpdate()
    {
        Transform player = enemyController != null ? enemyController.Player : null;
        if (player == null) return;

        currentAngle += angularSpeed * Time.fixedDeltaTime;
        if (currentAngle > 360f) currentAngle -= 360f;
        if (currentAngle < 0f) currentAngle += 360f;

        float rad = currentAngle * Mathf.Deg2Rad;
        Vector2 idealPos = (Vector2)player.position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;

        Vector2 currentPos = rb != null ? rb.position : (Vector2)transform.position;

        // 确保移动速度足以维持轨道（取趋近速度和角位移的较大值）
        float angularDisplacement = orbitRadius * Mathf.Abs(angularSpeed) * Mathf.Deg2Rad * Time.fixedDeltaTime;
        float effectiveSpeed = Mathf.Max(radiusApproachSpeed * Time.fixedDeltaTime, angularDisplacement);
        Vector2 nextPos = Vector2.MoveTowards(currentPos, idealPos, effectiveSpeed);

        if (rb != null)
            rb.MovePosition(nextPos);
        else
            transform.position = (Vector3)nextPos;
    }
}
