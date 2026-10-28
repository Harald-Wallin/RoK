using UnityEngine;

public sealed class ActionRequest
{
    public CharacterStats Caster
    {
        get;
    }

    public AbilityData Ability
    {
        get;
    }

    public Vector2 RequestedAimPoint
    {
        get;
    }

    public GameObject ExplicitTarget
    {
        get;
    }

    public Vector2 RequestedDirection
    {
        get;
    }

    /// <summary>
    /// Runtime-override av abilityns maximala range.
    ///
    /// Ett negativt värde innebär att abilityns vanliga
    /// TargetingSettings.Range används.
    /// </summary>
    public float RangeOverride
    {
        get;
    }

    public bool HasExplicitTarget =>
        ExplicitTarget != null;

    public bool HasRequestedDirection =>
        RequestedDirection.sqrMagnitude >
        0.0001f;

    public bool HasRangeOverride =>
        RangeOverride >= 0f;

    public ActionRequest(
        CharacterStats caster,
        AbilityData ability,
        Vector2 requestedAimPoint,
        GameObject explicitTarget = null,
        Vector2 requestedDirection = default,
        float rangeOverride = -1f)
    {
        Caster =
            caster;

        Ability =
            ability;

        RequestedAimPoint =
            requestedAimPoint;

        ExplicitTarget =
            explicitTarget;

        RequestedDirection =
            requestedDirection.sqrMagnitude >
            0.0001f
                ? requestedDirection.normalized
                : Vector2.zero;

        RangeOverride =
            rangeOverride;
    }
}