using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 3;
    [SerializeField] private float iFrameSeconds = 0.7f;

    private int hp;
    private bool invuln;

    /// <summary>Current HP.</summary>
    public int CurrentHP => hp;

    /// <summary>Max HP.</summary>
    public int MaxHP => maxHP;

    private void Awake() => hp = maxHP;

    /// <summary>Reset player HP to full.</summary>
    public void RestoreHealth()
    {
        hp = maxHP;
        invuln = false;
        RunLogger.Event($"Player health restored: {hp}/{maxHP}");
    }

    public void TakeDamage(int dmg)
    {
        if (invuln) return;

        int actualDamage = Mathf.Max(1, dmg);
        hp -= actualDamage;
        RunLogger.Warning($"Player took damage: -{actualDamage}, hp={Mathf.Max(hp, 0)}/{maxHP}");

        if (hp <= 0)
        {
            RunLogger.Warning("Player died.");
            GameFlowController.Instance.TriggerGameOver();
            return;
        }

        StartCoroutine(IFrame());
    }

    private IEnumerator IFrame()
    {
        invuln = true;
        yield return new WaitForSeconds(iFrameSeconds);
        invuln = false;
    }
}
