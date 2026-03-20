using UnityEngine;

public class RunProgressionState
{
    public int CurrentRoundDebtIncrease { get; private set; }
    public int NextRoundDebtIncrease { get; private set; }

    public float CurrentRoundEnemyHpMultiplier { get; private set; } = 1f;
    public float CurrentRoundEnemySpeedMultiplier { get; private set; } = 1f;
    public float CurrentRoundEnemyCountMultiplier { get; private set; } = 1f;
    public float NextRoundEnemyHpMultiplier { get; private set; } = 1f;
    public float NextRoundEnemySpeedMultiplier { get; private set; } = 1f;
    public float NextRoundEnemyCountMultiplier { get; private set; } = 1f;

    public float CurrentRoundRewardMultiplier { get; private set; } = 1f;
    public float NextRoundRewardMultiplier { get; private set; } = 1f;

    public void Reset()
    {
        CurrentRoundDebtIncrease = 0;
        NextRoundDebtIncrease = 0;
        CurrentRoundEnemyHpMultiplier = 1f;
        CurrentRoundEnemySpeedMultiplier = 1f;
        CurrentRoundEnemyCountMultiplier = 1f;
        NextRoundEnemyHpMultiplier = 1f;
        NextRoundEnemySpeedMultiplier = 1f;
        NextRoundEnemyCountMultiplier = 1f;
        CurrentRoundRewardMultiplier = 1f;
        NextRoundRewardMultiplier = 1f;
    }

    public void BeginRound()
    {
        CurrentRoundDebtIncrease = NextRoundDebtIncrease;
        NextRoundDebtIncrease = 0;

        CurrentRoundEnemyHpMultiplier = Mathf.Max(1f, NextRoundEnemyHpMultiplier);
        CurrentRoundEnemySpeedMultiplier = Mathf.Max(1f, NextRoundEnemySpeedMultiplier);
        CurrentRoundEnemyCountMultiplier = Mathf.Max(1f, NextRoundEnemyCountMultiplier);
        NextRoundEnemyHpMultiplier = 1f;
        NextRoundEnemySpeedMultiplier = 1f;
        NextRoundEnemyCountMultiplier = 1f;

        CurrentRoundRewardMultiplier = Mathf.Max(1f, NextRoundRewardMultiplier);
        NextRoundRewardMultiplier = 1f;
    }

    public void AddDebtIncreaseToNextRound(int amount)
    {
        NextRoundDebtIncrease += Mathf.Max(0, amount);
    }

    public void AddEnemyBuffToNextRound(float hpMultiplier, float speedMultiplier, float countMultiplier = 1f)
    {
        NextRoundEnemyHpMultiplier *= Mathf.Max(1f, hpMultiplier);
        NextRoundEnemySpeedMultiplier *= Mathf.Max(1f, speedMultiplier);
        NextRoundEnemyCountMultiplier *= Mathf.Max(1f, countMultiplier);
    }

    public void AddRewardBuffToNextRound(float rewardMultiplier)
    {
        NextRoundRewardMultiplier *= Mathf.Max(1f, rewardMultiplier);
    }

    public void ClampNextRoundEnemyBuff(float maxHpMultiplier, float maxSpeedMultiplier, float maxCountMultiplier = 1f)
    {
        NextRoundEnemyHpMultiplier = Mathf.Clamp(NextRoundEnemyHpMultiplier, 1f, Mathf.Max(1f, maxHpMultiplier));
        NextRoundEnemySpeedMultiplier = Mathf.Clamp(NextRoundEnemySpeedMultiplier, 1f, Mathf.Max(1f, maxSpeedMultiplier));
        NextRoundEnemyCountMultiplier = Mathf.Clamp(NextRoundEnemyCountMultiplier, 1f, Mathf.Max(1f, maxCountMultiplier));
    }
}
