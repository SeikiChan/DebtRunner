using UnityEngine;

/// <summary>
/// 椭圆/圆形边界 — 把玩家和敌人限制在椭圆区域内
/// </summary>
[DefaultExecutionOrder(100)]
public class CircleBoundary : MonoBehaviour
{
    public static CircleBoundary Instance { get; private set; }

    [LocalizedLabel("水平半径 (X)")]
    [SerializeField, Min(0.1f)] private float radiusX = 12f;
    [LocalizedLabel("垂直半径 (Y)")]
    [SerializeField, Min(0.1f)] private float radiusY = 8f;
    [LocalizedLabel("限制 Tag")]
    [SerializeField] private string targetTag = "Player";

    public float RadiusX => radiusX;
    public float RadiusY => radiusY;
    public Vector2 Center => (Vector2)transform.position;

    private Transform target;
    private Rigidbody2D targetRb;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void FixedUpdate()
    {
        // 限制玩家
        if (target == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag(targetTag);
            if (go == null) return;
            target = go.transform;
            targetRb = go.GetComponent<Rigidbody2D>();
        }

        if (targetRb == null) return;

        ClampRigidbody(targetRb);
    }

    /// <summary>
    /// 把任意 Rigidbody2D 限制在椭圆内，超出则拉回并消除朝外速度
    /// </summary>
    public void ClampRigidbody(Rigidbody2D rb)
    {
        Vector2 center = Center;
        Vector2 pos = rb.position;
        Vector2 offset = pos - center;

        float nx = offset.x / radiusX;
        float ny = offset.y / radiusY;
        float ellipseDist = nx * nx + ny * ny;

        if (ellipseDist <= 1f) return;

        float scale = 1f / Mathf.Sqrt(ellipseDist);
        rb.position = center + offset * scale;

        Vector2 normal = new Vector2(
            offset.x / (radiusX * radiusX),
            offset.y / (radiusY * radiusY)).normalized;

        float outDot = Vector2.Dot(rb.linearVelocity, normal);
        if (outDot > 0f)
            rb.linearVelocity -= normal * outDot;
    }

    /// <summary>
    /// 把任意位置限制在椭圆内（用于 MovePosition 的敌人）
    /// 返回限制后的位置
    /// </summary>
    public Vector2 ClampPosition(Vector2 pos)
    {
        Vector2 center = Center;
        Vector2 offset = pos - center;

        float nx = offset.x / radiusX;
        float ny = offset.y / radiusY;
        float ellipseDist = nx * nx + ny * ny;

        if (ellipseDist <= 1f) return pos;

        float scale = 1f / Mathf.Sqrt(ellipseDist);
        return center + offset * scale;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        DrawEllipseGizmo(transform.position, radiusX, radiusY, 64);
    }

    private static void DrawEllipseGizmo(Vector3 center, float rx, float ry, int segments)
    {
        float step = 2f * Mathf.PI / segments;
        Vector3 prev = center + new Vector3(rx, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = step * i;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * rx, Mathf.Sin(angle) * ry, 0f);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
