using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 3;
    [SerializeField] private float iFrameSeconds = 0.7f;

    private int hp;
    private bool invuln;

    private int shieldCharges;
    private bool periodicShieldEnabled;
    private float periodicShieldInterval = 12f;
    private int periodicShieldMaxCharges = 1;
    private float periodicShieldTimer;
    private float invulnUntilUnscaledTime;
    private Coroutine invulnRoutine;

    public int CurrentHP => hp;
    public int MaxHP => maxHP;
    public int ShieldCharges => shieldCharges;

    private void Awake() => hp = Mathf.Max(1, maxHP);

    private void Update()
    {
        if (!periodicShieldEnabled || periodicShieldInterval <= 0f)
            return;

        periodicShieldTimer -= Time.deltaTime;
        if (periodicShieldTimer > 0f)
            return;

        periodicShieldTimer = periodicShieldInterval;
        if (shieldCharges < periodicShieldMaxCharges)
        {
            shieldCharges += 1;
            RunLogger.Event($"Periodic shield granted. charges={shieldCharges}/{periodicShieldMaxCharges}");
        }
    }

    public void RestoreHealth()
    {
        hp = Mathf.Max(1, maxHP);
        invuln = false;
        invulnUntilUnscaledTime = 0f;
        if (invulnRoutine != null)
        {
            StopCoroutine(invulnRoutine);
            invulnRoutine = null;
        }
        RunLogger.Event($"Player health restored: {hp}/{maxHP}");
    }

    public void ResetRuntimeStats()
    {
        shieldCharges = 0;
        periodicShieldEnabled = false;
        periodicShieldInterval = 12f;
        periodicShieldMaxCharges = 1;
        periodicShieldTimer = 0f;
    }

    public void AddMaxHealth(int amount, bool healAddedAmount)
    {
        int add = Mathf.Max(0, amount);
        if (add == 0) return;

        maxHP = Mathf.Max(1, maxHP + add);
        if (healAddedAmount)
            hp = Mathf.Min(maxHP, hp + add);
        else
            hp = Mathf.Min(maxHP, hp);

        RunLogger.Event($"Player max health +{add}. hp={hp}/{maxHP}");
    }

    public void Heal(int amount)
    {
        int heal = Mathf.Max(0, amount);
        if (heal == 0) return;

        int before = hp;
        hp = Mathf.Min(maxHP, hp + heal);
        RunLogger.Event($"Player healed +{hp - before}. hp={hp}/{maxHP}");
    }

    public void AddShieldCharges(int amount)
    {
        int add = Mathf.Max(0, amount);
        if (add == 0) return;

        shieldCharges += add;
        RunLogger.Event($"Shield charges +{add}. current={shieldCharges}");
    }

    public void EnablePeriodicShield(float intervalSeconds, int maxCharges)
    {
        periodicShieldEnabled = true;
        periodicShieldInterval = Mathf.Max(0.1f, intervalSeconds);
        periodicShieldMaxCharges = Mathf.Max(1, maxCharges);
        periodicShieldTimer = periodicShieldInterval;
        RunLogger.Event($"Periodic shield enabled. interval={periodicShieldInterval:F1}s, maxCharges={periodicShieldMaxCharges}");
    }

    public void TakeDamage(int dmg)
    {
        if (invuln) return;

        if (shieldCharges > 0)
        {
            shieldCharges -= 1;
            RunLogger.Event($"Shield blocked damage. remaining={shieldCharges}");
            ApplyInvulnerabilityFor(iFrameSeconds);
            return;
        }

        int actualDamage = Mathf.Max(1, dmg);
        hp -= actualDamage;
        RunLogger.Warning($"Player took damage: -{actualDamage}, hp={Mathf.Max(hp, 0)}/{maxHP}");

        if (hp <= 0)
        {
            RunLogger.Warning("Player died.");
            GameFlowController.Instance.TriggerGameOver();
            return;
        }

        ApplyInvulnerabilityFor(iFrameSeconds);
    }

    public void GrantTemporaryInvulnerability(float seconds)
    {
        float duration = Mathf.Max(0f, seconds);
        if (duration <= 0f)
            return;

        ApplyInvulnerabilityFor(duration);
        RunLogger.Event($"Player temporary invulnerability granted: {duration:F2}s");
    }

    private void ApplyInvulnerabilityFor(float seconds)
    {
        float duration = Mathf.Max(0f, seconds);
        if (duration <= 0f)
            return;

        float targetEndTime = Time.unscaledTime + duration;
        if (targetEndTime > invulnUntilUnscaledTime)
            invulnUntilUnscaledTime = targetEndTime;

        invuln = true;
        if (invulnRoutine == null)
            invulnRoutine = StartCoroutine(InvulnerabilityTimerRoutine());
    }

    private IEnumerator InvulnerabilityTimerRoutine()
    {
        while (Time.unscaledTime < invulnUntilUnscaledTime)
            yield return null;

        invuln = false;
        invulnRoutine = null;
    }
}
