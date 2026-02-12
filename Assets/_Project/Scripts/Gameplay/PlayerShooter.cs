using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private PlayerMotor2D motor;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform projectilesRoot;

    [Header("Weapon (temp)")]
    [SerializeField] private float fireInterval = 0.25f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private int damage = 1;

    [Header("Optional")]
    [SerializeField] private Transform muzzle;

    private float timer;
    
    // 初始武器参数（用于计算升级倍数）
    private float baseFireInterval;
    private float baseProjectileSpeed;
    private int baseDamage;

    private void Reset()
    {
        motor = GetComponent<PlayerMotor2D>();
    }

    private void Awake()
    {
        // 保存初始参数
        baseFireInterval = fireInterval;
        baseProjectileSpeed = projectileSpeed;
        baseDamage = damage;
    }

    private void OnEnable()
    {
        timer = 0f;
    }

    private void Update()
    {
        if (motor == null || projectilePrefab == null) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        Vector2 dir = motor.LastMoveDir;
        if (dir.sqrMagnitude < 0.001f) return;

        timer = fireInterval;

        Vector3 spawnPos = muzzle ? muzzle.position : transform.position;
        var proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity, projectilesRoot);
        proj.Fire(dir, projectileSpeed, damage);
    }

    /// <summary>
    /// 应用武器升级
    /// </summary>
    public void ApplyUpgrade(WeaponUpgrade upgrade)
    {
        if (upgrade == null) return;

        damage += upgrade.upgradePower;

        if (upgrade.upgradeFireRate > 0)
            fireInterval -= upgrade.upgradeFireRate; // 升级攻速后间隔减少（更快）

        if (upgrade.upgradeSpeed > 0)
            projectileSpeed += upgrade.upgradeSpeed;

        Debug.Log($"升级应用: 伤害={damage}, 攻速={1f / fireInterval:F2}/秒, 弹速={projectileSpeed}");
    }

    /// <summary>
    /// 获取当前伤害
    /// </summary>
    public int GetDamage() => damage;

    /// <summary>
    /// 获取当前攻速
    /// </summary>
    public float GetFireRate() => 1f / fireInterval;
}
