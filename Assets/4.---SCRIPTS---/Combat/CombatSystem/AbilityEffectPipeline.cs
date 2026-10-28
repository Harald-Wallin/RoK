using System.Collections.Generic;

/// <summary>
/// Auktoritativ execution-pipeline för omedelbara
/// AbilityEffect-assets.
///
/// Ansvar:
/// 1. Resolve target hit results.
/// 2. Registrera combat activity.
/// 3. Presentera miss och evade.
/// 4. Route och exekvera Immediate-effekter.
/// </summary>
public static class AbilityEffectPipeline
{
    public static AbilityEffectPipelineResult Execute(
        ActionExecutionContext context)
    {
        AbilityEffectPipelineResult result =
            new AbilityEffectPipelineResult();

        if (context == null ||
            context.Ability == null ||
            context.Caster == null)
        {
            return result;
        }

        List<AbilityTargetHitResult> targetResults =
            AbilityHitResolver
                .ResolveActionTargets(
                    context
                );

        for (int i = 0;
             i < targetResults.Count;
             i++)
        {
            AbilityTargetHitResult targetResult =
                targetResults[i];

            result.AddTargetResult(
                targetResult
            );

            AbilityHitFeedback.Display(
                context.Caster,
                targetResult
            );
        }

        CombatActivityUtility.Notify(
            context,
            targetResults
        );

        AbilityEffectExecutionRouter.Execute(
            context.Ability.Effects,
            context,
            targetResults,
            AbilityEffectExecutionTiming.Immediate,
            result
        );

        return result;
    }
}