using System;
using UnityEngine;

[Serializable]
public sealed class AbilityChargeSettings
{
    [SerializeField]
    [Tooltip(
        "Bestämmer vilka värden som påverkas av charge."
    )]
    private ChargeScalingMode scalingMode =
        ChargeScalingMode.None;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "Damage multiplier vid 0% charge. " +
        "0.25 innebär 25% av full damage."
    )]
    private float minimumDamageMultiplier =
        0.25f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "Range multiplier vid 0% charge. " +
        "0.25 innebär 25% av maximal range."
    )]
    private float minimumRangeMultiplier =
        0.25f;

    public ChargeScalingMode ScalingMode =>
        scalingMode;

    public bool ScalesDamage =>
        (scalingMode &
         ChargeScalingMode.Damage) != 0;

    public bool ScalesRange =>
        (scalingMode &
         ChargeScalingMode.Range) != 0;

    public float MinimumDamageMultiplier =>
        Mathf.Clamp01(
            minimumDamageMultiplier
        );

    public float MinimumRangeMultiplier =>
        Mathf.Clamp01(
            minimumRangeMultiplier
        );

    public float GetDamageMultiplier(
        float chargeProgress)
    {
        if (!ScalesDamage)
            return 1f;

        return Mathf.Lerp(
            MinimumDamageMultiplier,
            1f,
            Mathf.Clamp01(
                chargeProgress
            )
        );
    }

    public float GetRangeMultiplier(
        float chargeProgress)
    {
        if (!ScalesRange)
            return 1f;

        return Mathf.Lerp(
            MinimumRangeMultiplier,
            1f,
            Mathf.Clamp01(
                chargeProgress
            )
        );
    }

#if UNITY_EDITOR
    public void Validate(
        UnityEngine.Object context)
    {
        minimumDamageMultiplier =
            Mathf.Clamp01(
                minimumDamageMultiplier
            );

        minimumRangeMultiplier =
            Mathf.Clamp01(
                minimumRangeMultiplier
            );

        if (ScalesDamage &&
            ScalesRange &&
            !Mathf.Approximately(
                minimumDamageMultiplier,
                minimumRangeMultiplier))
        {
            Debug.LogWarning(
                "Abilityn skalar både damage och range, men " +
                "deras minimum multipliers skiljer sig. " +
                "En gemensam charge-fill kommer därför inte " +
                "representera båda exakt.",
                context
            );
        }
    }
#endif
}