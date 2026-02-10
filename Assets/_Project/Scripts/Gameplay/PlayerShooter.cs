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

    private void Reset()
    {
        motor = GetComponent<PlayerMotor2D>();
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
}
