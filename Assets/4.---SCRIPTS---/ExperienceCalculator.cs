using UnityEngine;

public static class ExperienceCalculator
{
    public static int CalculateExp(int targetLevel, int playerLevel)
    {
        float exp = 10f
            + targetLevel * 1.5f
            - playerLevel * 1.1f;

        exp = Mathf.Max(0, Mathf.RoundToInt(exp));
        return (int)exp;
    }
}

