using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gemensam routing och execution av AbilityEffect-assets.
///
/// Klassen används oavsett om effekterna körs:
/// - omedelbart vid action execution
/// - senare vid projectile impact
/// - senare av en trap
/// - senare av en ground effect
///
/// TargetMode behandlas identiskt i samtliga fall.
/// </summary>
public static class AbilityEffectExecutionRouter
{
    public static int Execute(
        IReadOnlyList<AbilityEffect> effects,
        ActionExecutionContext action,
        IReadOnlyList<AbilityTargetHitResult> targetResults,
        AbilityEffectExecutionTiming requiredTiming,
        AbilityEffectPipelineResult pipelineResult = null)
    {
        if (effects == null ||
            action == null)
        {
            return 0;
        }

        int executionCount = 0;

        for (int i = 0;
             i < effects.Count;
             i++)
        {
            AbilityEffect effect =
                effects[i];

            if (effect == null)
                continue;

            if (effect.ExecutionTiming !=
                requiredTiming)
            {
                continue;
            }

            if (pipelineResult != null)
            {
                pipelineResult.EffectsConsidered++;
            }

            executionCount +=
                ExecuteEffect(
                    effect,
                    action,
                    targetResults,
                    pipelineResult
                );
        }

        return executionCount;
    }

    private static int ExecuteEffect(
        AbilityEffect effect,
        ActionExecutionContext action,
        IReadOnlyList<AbilityTargetHitResult> targetResults,
        AbilityEffectPipelineResult pipelineResult)
    {
        switch (effect.TargetMode)
        {
            case AbilityEffectTargetMode
                .EachAffectedTarget:

                return ExecuteForAffectedTargets(
                    effect,
                    action,
                    targetResults,
                    pipelineResult
                );

            case AbilityEffectTargetMode
                .PrimaryTarget:

                return ExecuteForPrimaryTarget(
                    effect,
                    action,
                    targetResults,
                    pipelineResult
                );

            case AbilityEffectTargetMode.Caster:

                return ExecuteForCaster(
                    effect,
                    action,
                    pipelineResult
                );

            case AbilityEffectTargetMode
                .OncePerAction:

                return ExecuteOnce(
                    effect,
                    action,
                    pipelineResult
                );

            default:

                Debug.LogWarning(
                    $"Okänd AbilityEffectTargetMode på " +
                    $"effekten '{effect.name}'.",
                    effect
                );

                return 0;
        }
    }

    private static int ExecuteForAffectedTargets(
        AbilityEffect effect,
        ActionExecutionContext action,
        IReadOnlyList<AbilityTargetHitResult> targetResults,
        AbilityEffectPipelineResult pipelineResult)
    {
        if (targetResults == null)
            return 0;

        int executions = 0;

        for (int i = 0;
             i < targetResults.Count;
             i++)
        {
            AbilityTargetHitResult result =
                targetResults[i];

            if (result == null ||
                !result.WasSuccessful)
            {
                continue;
            }

            AbilityEffectExecutionContext context =
                new AbilityEffectExecutionContext(
                    action,
                    result.TargetObject,
                    result.Target,
                    result
                );

            if (ExecuteSafely(
                    effect,
                    context,
                    pipelineResult))
            {
                executions++;
            }
        }

        return executions;
    }

    private static int ExecuteForPrimaryTarget(
        AbilityEffect effect,
        ActionExecutionContext action,
        IReadOnlyList<AbilityTargetHitResult> targetResults,
        AbilityEffectPipelineResult pipelineResult)
    {
        AbilityTargetHitResult primaryResult =
            ResolveRuntimePrimaryResult(
                action,
                targetResults
            );

        if (primaryResult == null ||
            !primaryResult.WasSuccessful)
        {
            return 0;
        }

        AbilityEffectExecutionContext context =
            new AbilityEffectExecutionContext(
                action,
                primaryResult.TargetObject,
                primaryResult.Target,
                primaryResult
            );

        return ExecuteSafely(
            effect,
            context,
            pipelineResult
        )
            ? 1
            : 0;
    }

    private static int ExecuteForCaster(
        AbilityEffect effect,
        ActionExecutionContext action,
        AbilityEffectPipelineResult pipelineResult)
    {
        CharacterStats caster =
            action.Caster;

        if (caster == null)
            return 0;

        AbilityTargetHitResult casterResult =
            new AbilityTargetHitResult(
                caster.gameObject,
                caster,
                -1,
                AbilityTargetHitOutcome.NotRolled
            );

        AbilityEffectExecutionContext context =
            new AbilityEffectExecutionContext(
                action,
                caster.gameObject,
                caster,
                casterResult
            );

        return ExecuteSafely(
            effect,
            context,
            pipelineResult
        )
            ? 1
            : 0;
    }

    private static int ExecuteOnce(
        AbilityEffect effect,
        ActionExecutionContext action,
        AbilityEffectPipelineResult pipelineResult)
    {
        AbilityEffectExecutionContext context =
            new AbilityEffectExecutionContext(
                action,
                null,
                null,
                null
            );

        return ExecuteSafely(
            effect,
            context,
            pipelineResult
        )
            ? 1
            : 0;
    }

    private static AbilityTargetHitResult
        ResolveRuntimePrimaryResult(
            ActionExecutionContext action,
            IReadOnlyList<AbilityTargetHitResult> targetResults)
    {
        if (targetResults == null ||
            targetResults.Count == 0)
        {
            return null;
        }

        CharacterStats originalPrimary =
            action.PrimaryCharacterTarget;

        if (originalPrimary != null)
        {
            for (int i = 0;
                 i < targetResults.Count;
                 i++)
            {
                AbilityTargetHitResult result =
                    targetResults[i];

                if (result?.Target ==
                    originalPrimary)
                {
                    return result;
                }
            }
        }

        /*
         * Vid exempelvis FirstValidCharacter kan projektilen
         * träffa ett annat target än actionens ursprungliga
         * PrimaryTarget. Då är impacttargetet runtime-primary.
         */
        for (int i = 0;
             i < targetResults.Count;
             i++)
        {
            if (targetResults[i] != null)
                return targetResults[i];
        }

        return null;
    }

    private static bool ExecuteSafely(
        AbilityEffect effect,
        AbilityEffectExecutionContext context,
        AbilityEffectPipelineResult pipelineResult)
    {
        if (effect == null ||
            context == null)
        {
            return false;
        }

        if (effect.RequiresCharacterTarget &&
            context.Target == null)
        {
            return false;
        }

        try
        {
            effect.Execute(
                context
            );

            if (pipelineResult != null)
            {
                pipelineResult.EffectExecutions++;
            }

            return true;
        }
        catch (System.Exception exception)
        {
            if (pipelineResult != null)
            {
                pipelineResult
                    .FailedEffectExecutions++;
            }

            Debug.LogException(
                exception,
                context.Caster
            );

            return false;
        }
    }
}
