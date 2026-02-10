using UnityEngine;

public class PlayerMotor2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6.0f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastMoveDir = Vector2.right;

    public Vector2 LastMoveDir => lastMoveDir;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // 原型期用旧输入最快（WASD/方向键）
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(x, y).normalized;

        if (moveInput.sqrMagnitude > 0.001f)
            lastMoveDir = moveInput;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }
}
