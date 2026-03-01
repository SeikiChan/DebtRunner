using UnityEngine;

/// <summary>
/// 敌人蠕动动画 — 移动时弹跳+挤压+朝向翻转（类似土豆兄弟风格）
/// 挂在敌人 GameObject 上，自动创建子视觉物体来做动画（不影响物理移动）
/// </summary>
[DisallowMultipleComponent]
public class EnemyVisualWobble : MonoBehaviour
{
    [Header("Idle Breath / 待机呼吸")]
    [LocalizedLabel("呼吸幅度")]
    [SerializeField, Min(0f)] private float breathScale = 0.025f;
    [LocalizedLabel("呼吸速度")]
    [SerializeField, Min(0f)] private float breathSpeed = 2.5f;

    [Header("Move Bounce / 移动弹跳")]
    [LocalizedLabel("弹跳幅度")]
    [SerializeField, Min(0f)] private float bounceAmplitude = 0.06f;
    [LocalizedLabel("弹跳频率")]
    [SerializeField, Min(0f)] private float bounceFrequency = 8f;

    [Header("Move Squash / 移动挤压")]
    [LocalizedLabel("挤压幅度")]
    [SerializeField, Min(0f)] private float squashAmount = 0.07f;

    [Header("Lean / 倾斜")]
    [LocalizedLabel("倾斜角度")]
    [SerializeField, Min(0f)] private float leanAngle = 5f;
    [LocalizedLabel("倾斜平滑速度")]
    [SerializeField, Min(0.01f)] private float leanLerpSpeed = 8f;

    [Header("Flip / 翻转")]
    [LocalizedLabel("朝向翻转")]
    [SerializeField] private bool flipByMoveDirection = true;
    [LocalizedLabel("速度阈值")]
    [SerializeField, Min(0f)] private float moveThreshold = 0.15f;

    private SpriteRenderer spriteRenderer;
    private Transform visualChild;
    private Vector3 baseLocalScale;
    private float breathTimer;
    private float bounceTimer;
    private float currentLean;
    private Vector2 prevPos;
    private Vector2 smoothVelocity;
    private bool facingLeft;
    private float flipCooldown;

    private void Awake()
    {
        EnsureVisualChild();
        prevPos = transform.position;
    }

    private void EnsureVisualChild()
    {
        SpriteRenderer rootSR = GetComponent<SpriteRenderer>();
        SpriteRenderer childSR = null;

        // 先看有没有已有的子物体 SpriteRenderer
        foreach (Transform child in transform)
        {
            var sr = child.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                childSR = sr;
                break;
            }
        }

        if (childSR != null)
        {
            // 已有子视觉物体，直接用，确保根的关闭
            if (rootSR != null && rootSR != childSR)
                rootSR.enabled = false;
            spriteRenderer = childSR;
            visualChild = childSR.transform;
        }
        else if (rootSR != null)
        {
            // SpriteRenderer 在根物体上，创建子物体接管渲染
            GameObject child = new GameObject("EnemyVisual");
            child.transform.SetParent(transform, false);

            SpriteRenderer newSR = child.AddComponent<SpriteRenderer>();
            newSR.sprite = rootSR.sprite;
            newSR.color = rootSR.color;
            newSR.flipX = rootSR.flipX;
            newSR.flipY = rootSR.flipY;
            newSR.sortingLayerID = rootSR.sortingLayerID;
            newSR.sortingOrder = rootSR.sortingOrder;
            newSR.sharedMaterial = rootSR.sharedMaterial;
            newSR.maskInteraction = rootSR.maskInteraction;

            rootSR.enabled = false;
            spriteRenderer = newSR;
            visualChild = child.transform;
        }
        else
        {
            // 没有 SpriteRenderer
            return;
        }

        baseLocalScale = visualChild.localScale;
    }

    private void LateUpdate()
    {
        if (visualChild == null) return;

        // 通过位置差计算速度，用指数平滑过滤抖动（击退等）
        Vector2 currentPos = transform.position;
        float dt = Time.deltaTime;
        if (dt > 0.0001f)
        {
            Vector2 rawVel = (currentPos - prevPos) / dt;
            float smooth = 1f - Mathf.Exp(-6f * dt);
            smoothVelocity = Vector2.Lerp(smoothVelocity, rawVel, smooth);
        }
        prevPos = currentPos;

        Vector2 vel = smoothVelocity;
        float speed = vel.magnitude;
        bool moving = speed > moveThreshold;

        // 呼吸
        breathTimer += Time.deltaTime * breathSpeed;
        float breath = Mathf.Sin(breathTimer) * breathScale;

        // 弹跳 & 挤压
        float bounce = 0f;
        float squash = 0f;
        if (moving)
        {
            float speedScale = Mathf.Clamp(speed / 3f, 0.6f, 1.5f);
            bounceTimer += Time.deltaTime * bounceFrequency * speedScale;
            bounce = Mathf.Sin(bounceTimer) * bounceAmplitude;
            squash = Mathf.Abs(Mathf.Sin(bounceTimer)) * squashAmount;
        }

        // 倾斜
        float targetLean = moving ? -vel.x * leanAngle / Mathf.Max(speed, 0.01f) : 0f;
        float lerpT = 1f - Mathf.Exp(-leanLerpSpeed * Time.deltaTime);
        currentLean = Mathf.Lerp(currentLean, targetLean, lerpT);

        // 缩放
        float scaleX = 1f - (breath * 0.35f) + squash;
        float scaleY = 1f + breath - squash;

        visualChild.localScale = new Vector3(
            baseLocalScale.x * scaleX,
            baseLocalScale.y * scaleY,
            baseLocalScale.z);

        // 位置偏移（弹跳）— 安全地在子物体上操作，不影响根物体位置
        visualChild.localPosition = new Vector3(0f, bounce, 0f);

        // 旋转（倾斜）
        visualChild.localRotation = Quaternion.Euler(0f, 0f, currentLean);

        // 朝向翻转（平滑速度 + 死区 + 冷却防抽搐）
        if (flipCooldown > 0f)
            flipCooldown -= dt;

        if (flipByMoveDirection && spriteRenderer != null && flipCooldown <= 0f)
        {
            float absX = Mathf.Abs(vel.x);
            if (absX > 0.4f && absX > speed * 0.3f)
            {
                bool wantLeft = vel.x < 0f;
                if (wantLeft != facingLeft)
                {
                    facingLeft = wantLeft;
                    spriteRenderer.flipX = facingLeft;
                    flipCooldown = 0.15f; // 翻转后 0.15 秒内不再翻转
                }
            }
        }
    }

    private void OnDisable()
    {
        if (visualChild == null) return;

        visualChild.localScale = baseLocalScale;
        visualChild.localPosition = Vector3.zero;
        visualChild.localRotation = Quaternion.identity;
    }
}
