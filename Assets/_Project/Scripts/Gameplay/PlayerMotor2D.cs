using UnityEngine;

public class PlayerMotor2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6.0f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastMoveDir = Vector2.right;

    private float baseMoveSpeed;
    private float moveSpeedFlatBonus;
    private float moveSpeedPercentBonus;

    public Vector2 LastMoveDir => lastMoveDir;
    public float CurrentMoveSpeed => Mathf.Max(0.1f, (baseMoveSpeed + moveSpeedFlatBonus) * Mathf.Max(0.1f, 1f + moveSpeedPercentBonus));

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseMoveSpeed = Mathf.Max(0.1f, moveSpeed);
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
        rb.linearVelocity = moveInput * CurrentMoveSpeed;
    }

    public void ResetRuntimeStats()
    {
        moveSpeedFlatBonus = 0f;
        moveSpeedPercentBonus = 0f;
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
}
