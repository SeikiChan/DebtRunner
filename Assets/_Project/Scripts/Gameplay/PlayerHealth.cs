using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 3;
    [SerializeField] private float iFrameSeconds = 0.7f;

    private int hp;
    private bool invuln;

    /// <summary>当前生命值</summary>
    public int CurrentHP => hp;
    
    /// <summary>最大生命值</summary>
    public int MaxHP => maxHP;

    private void Awake() => hp = maxHP;

    /// <summary>重置血量为满血</summary>
    public void RestoreHealth()
    {
        hp = maxHP;
        invuln = false;
    }

    public void TakeDamage(int dmg)
    {
        if (invuln) return;

        hp -= Mathf.Max(1, dmg);
        if (hp <= 0)
        {
            // 玩家死亡 - 触发游戏结束
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
