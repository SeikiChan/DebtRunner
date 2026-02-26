using UnityEngine;

public class PlayerMotor2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6.0f;
    [SerializeField, Min(0f)] private float externalVelocityRecoveryPerSecond = 15f;
    [SerializeField, Min(0f)] private float maxExternalVelocity = 10f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastMoveDir = Vector2.right;
    private Vector2 externalVelocity;

    private float baseMoveSpeed;
    private float moveSpeedFlatBonus;
    private float moveSpeedPercentBonus;

    public Vector2 LastMoveDir => lastMoveDir;
    public Vector2 CurrentMoveInput => moveInput;
    public float CurrentMoveSpeed => Mathf.Max(0.1f, (baseMoveSpeed + moveSpeedFlatBonus) * Mathf.Max(0.1f, 1f + moveSpeedPercentBonus));

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseMoveSpeed = Mathf.Max(0.1f, moveSpeed);

        if (GetComponent<PlayerVisualAnim>() == null)
            gameObject.AddComponent<PlayerVisualAnim>();
    }

    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(x, y).normalized;

        if (moveInput.sqrMagnitude > 0.001f)
            lastMoveDir = moveInput;
    }

    private void FixedUpdate()
    {
        if (externalVelocityRecoveryPerSecond > 0f)
        {
            externalVelocity = Vector2.MoveTowards(
                externalVelocity,
                Vector2.zero,
                externalVelocityRecoveryPerSecond * Time.fixedDeltaTime);
        }

        rb.linearVelocity = (moveInput * CurrentMoveSpeed) + externalVelocity;
    }

    public void ResetRuntimeStats()
    {
        moveSpeedFlatBonus = 0f;
        moveSpeedPercentBonus = 0f;
        externalVelocity = Vector2.zero;
    }

    public void AddMoveSpeedFlat(float amount)
    {
        moveSpeedFlatBonus += amount;
        RunLogger.Event($"Player speed flat bonus {moveSpeedFlatBonus:+0.##;-0.##;0}. current={CurrentMoveSpeed:F2}");
    }

    public void AddMoveSpeedPercent(float amount)
    {
        moveSpeedPercentBonus += amount;
        moveSpeedPercentBonus = Mathf.Max(-0.9f, moveSpeedPercentBonus);
        RunLogger.Event($"Player speed percent bonus {moveSpeedPercentBonus:+0.##;-0.##;0}. current={CurrentMoveSpeed:F2}");
    }

    public void ApplyExternalImpulse(Vector2 direction, float impulse)
    {
        if (direction.sqrMagnitude <= 0.0001f || impulse <= 0f)
            return;

        externalVelocity += direction.normalized * impulse;
        float maxSq = maxExternalVelocity * maxExternalVelocity;
        if (externalVelocity.sqrMagnitude > maxSq)
            externalVelocity = externalVelocity.normalized * maxExternalVelocity;
    }
}
