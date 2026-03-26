using UnityEngine;

public class PlayerMotor2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6.0f;
    [SerializeField, Min(0f)] private float externalVelocityRecoveryPerSecond = 15f;
    [SerializeField, Min(0f)] private float maxExternalVelocity = 10f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastMoveDir = Vector2.right;
    private int horizontalFacing = 1;
    private Vector2 externalVelocity;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private float baseMoveSpeed;
    private float moveSpeedFlatBonus;
    private float moveSpeedPercentBonus;

    public Vector2 LastMoveDir => lastMoveDir;
    public Vector2 CurrentMoveInput => moveInput;
    public int HorizontalFacing => horizontalFacing < 0 ? -1 : 1;
    public bool IsFacingLeft => HorizontalFacing < 0;
    public float CurrentMoveSpeed => Mathf.Max(0.1f, (baseMoveSpeed + moveSpeedFlatBonus) * Mathf.Max(0.1f, 1f + moveSpeedPercentBonus));

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseMoveSpeed = Mathf.Max(0.1f, moveSpeed);
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        if (GetComponent<PlayerVisualAnim>() == null)
            gameObject.AddComponent<PlayerVisualAnim>();

        if (GetComponent<FootShadow2D>() == null)
            gameObject.AddComponent<FootShadow2D>();
    }

    private void Update()
    {
        moveInput = GameInput.ReadGameplayMoveInput();

        if (moveInput.sqrMagnitude > 0.001f)
            lastMoveDir = moveInput.normalized;

        if (moveInput.x < -0.02f)
            horizontalFacing = -1;
        else if (moveInput.x > 0.02f)
            horizontalFacing = 1;
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

    public void ResetForNewRun()
    {
        ResetRuntimeStats();
        moveInput = Vector2.zero;
        lastMoveDir = Vector2.right;
        horizontalFacing = 1;

        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        if (rb != null)
        {
            rb.position = spawnPosition;
            rb.rotation = spawnRotation.eulerAngles.z;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
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
