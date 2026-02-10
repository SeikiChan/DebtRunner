using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 3;
    [SerializeField] private float iFrameSeconds = 0.7f;

    private int hp;
    private bool invuln;

    private void Awake() => hp = maxHP;

    public void TakeDamage(int dmg)
    {
        if (invuln) return;

        hp -= Mathf.Max(1, dmg);
        if (hp <= 0)
        {
            // 先直接触发失败（你后面会接UI/动画）
            GameFlowController.Instance.BackToMenu(); // 或者 Switch到GameOver（你已有逻辑的话）
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
