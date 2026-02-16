using UnityEngine;

public class RunProgressionState
{
    public int CurrentRoundDebtIncrease { get; private set; }
    public int NextRoundDebtIncrease { get; private set; }

    public float CurrentRoundEnemyHpMultiplier { get; private set; } = 1f;
    public float CurrentRoundEnemySpeedMultiplier { get; private set; } = 1f;
    public float NextRoundEnemyHpMultiplier { get; private set; } = 1f;
    public float NextRoundEnemySpeedMultiplier { get; private set; } = 1f;

    public void Reset()
    {
        CurrentRoundDebtIncrease = 0;
        NextRoundDebtIncrease = 0;
        CurrentRoundEnemyHpMultiplier = 1f;
        CurrentRoundEnemySpeedMultiplier = 1f;
        NextRoundEnemyHpMultiplier = 1f;
        NextRoundEnemySpeedMultiplier = 1f;
    }

    public void BeginRound()
    {
        CurrentRoundDebtIncrease = NextRoundDebtIncrease;
        NextRoundDebtIncrease = 0;

        CurrentRoundEnemyHpMultiplier = Mathf.Max(1f, NextRoundEnemyHpMultiplier);
        CurrentRoundEnemySpeedMultiplier = Mathf.Max(1f, NextRoundEnemySpeedMultiplier);
        NextRoundEnemyHpMultiplier = 1f;
        NextRoundEnemySpeedMultiplier = 1f;
    }

    public void AddDebtIncreaseToNextRound(int amount)
    {
        NextRoundDebtIncrease += Mathf.Max(0, amount);
    }

    public void AddEnemyBuffToNextRound(float hpMultiplier, float speedMultiplier)
    {
        NextRoundEnemyHpMultiplier *= Mathf.Max(1f, hpMultiplier);
        NextRoundEnemySpeedMultiplier *= Mathf.Max(1f, speedMultiplier);
    }
}
