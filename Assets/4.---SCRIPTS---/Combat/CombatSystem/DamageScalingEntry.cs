using System;
using UnityEngine;

[Serializable]
public sealed class DamageScalingEntry
{
    [SerializeField]
    private StatType stat;

    [SerializeField]
    private float multiplier = 1f;

    public StatType Stat =>
        stat;

    public float Multiplier =>
        multiplier;

    public float Evaluate(
        CharacterStats caster)
    {
        if (caster == null)
            return 0f;

        return caster.GetStat(stat) * multiplier;
    }
}
