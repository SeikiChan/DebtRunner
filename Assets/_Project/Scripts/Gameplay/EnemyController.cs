using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 2.5f;

    [Header("HP")]
    [SerializeField] private int maxHP = 3;
    private int hp;

    [Header("Rewards (fixed per enemy type)")]
    [SerializeField] private int cashValue = 20;   // 该敌人固定价格：击杀自动加钱
    [SerializeField] private int xpDrop = 1;       // 掉落XP数量（生成拾取物）

    private Rigidbody2D rb;
    private Transform player;

    private XPPickup xpPrefab;
    private Transform pickupsRoot;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hp = maxHP;
    }

    public void Init(Transform playerTf, XPPickup xpPickupPrefab, Transform pickupsParent)
    {
        player = playerTf;
        xpPrefab = xpPickupPrefab;
        pickupsRoot = pickupsParent;

        var shooter = GetComponent<EnemyShooter>();
        if (shooter != null) shooter.Init(playerTf, GameObject.Find("World/Projectiles").transform);
    }

    private void FixedUpdate()
    {
        if (player == null) return;
        Vector2 pos = rb.position;
        Vector2 dir = ((Vector2)player.position - pos).normalized;
        rb.MovePosition(pos + dir * moveSpeed * Time.fixedDeltaTime);
    }

    public void TakeDamage(int dmg)
    {
        hp -= dmg;
        if (hp <= 0) Die();
    }

    private void Die()
    {
        // 1) 自动加钱（不掉钱、不拾取）
        GameFlowController.Instance.AddCash(cashValue);

        // 2) 掉XP拾取物
        if (xpPrefab != null)
        {
            var p = Instantiate(xpPrefab, transform.position, Quaternion.identity, pickupsRoot);
            p.SetAmount(xpDrop);
        }

        Destroy(gameObject);
    }
}
