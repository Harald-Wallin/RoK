using UnityEngine;

[System.Serializable]
public class StatConversion
{
    [Tooltip("Stat som används som källa, t.ex Strength.")]
    public StatType from;

    [Tooltip("Stat som påverkas.")]
    public StatType to;

    [Tooltip("Hur mycket varje poäng av käll-stat ger.\nExempel:\n0.3 = 1 Strength ger +0.3 Damage\n-0.01 = 1 Armor ger -0.01 Movement Speed")]
    public float multiplier = 1f;
}