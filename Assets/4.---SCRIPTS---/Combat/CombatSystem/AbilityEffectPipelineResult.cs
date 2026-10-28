using System.Collections.Generic;

/// <summary>
/// Sammanfattning av en genomförd AbilityEffectPipeline.
///
/// Resultatet används främst för:
/// - debugging
/// - tester
/// - combat-loggning
/// - framtida VFX- och eventintegration
/// </summary>
public sealed class AbilityEffectPipelineResult
{
    private readonly List<AbilityTargetHitResult>
        targetResults =
            new();

    private readonly List<AbilityTargetHitResult>
        successfulTargets =
            new();

    private readonly List<AbilityTargetHitResult>
        missedTargets =
            new();

    private readonly List<AbilityTargetHitResult>
        evadedTargets =
            new();

    public IReadOnlyList<AbilityTargetHitResult>
        TargetResults =>
            targetResults;

    public IReadOnlyList<AbilityTargetHitResult>
        SuccessfulTargets =>
            successfulTargets;

    public IReadOnlyList<AbilityTargetHitResult>
        MissedTargets =>
            missedTargets;

    public IReadOnlyList<AbilityTargetHitResult>
        EvadedTargets =>
            evadedTargets;

    public int EffectsConsidered
    {
        get;
        internal set;
    }

    public int EffectExecutions
    {
        get;
        internal set;
    }

    public int FailedEffectExecutions
    {
        get;
        internal set;
    }

    public int TotalTargets =>
        targetResults.Count;

    public int SuccessfulTargetCount =>
        successfulTargets.Count;

    public int MissedTargetCount =>
        missedTargets.Count;

    public int EvadedTargetCount =>
        evadedTargets.Count;

    public bool HasTargets =>
        targetResults.Count > 0;

    public bool HasSuccessfulTargets =>
        successfulTargets.Count > 0;

    public bool HadExecutionFailures =>
        FailedEffectExecutions > 0;

    internal void AddTargetResult(
        AbilityTargetHitResult result)
    {
        if (result == null)
            return;

        targetResults.Add(result);

        switch (result.Outcome)
        {
            case AbilityTargetHitOutcome.NotRolled:
            case AbilityTargetHitOutcome.Hit:
                successfulTargets.Add(result);
                break;

            case AbilityTargetHitOutcome.Miss:
                missedTargets.Add(result);
                break;

            case AbilityTargetHitOutcome.Evaded:
                evadedTargets.Add(result);
                break;
        }
    }
}