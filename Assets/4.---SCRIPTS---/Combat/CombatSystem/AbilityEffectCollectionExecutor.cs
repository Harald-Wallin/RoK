using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Publik entry point för att exekvera en uttrycklig samling
/// AbilityEffect-assets senare än actionögonblicket.
///
/// Används av:
/// - projektiler
/// - traps
/// - delayed effects
/// - ground effects
/// - framtida summons
/// </summary>
public static class AbilityEffectCollectionExecutor
{
    private static readonly
        IReadOnlyList<AbilityTargetHitResult>
        EmptyTargetResults =
            Array.Empty<AbilityTargetHitResult>();

    public static int Execute(
        IReadOnlyList<AbilityEffect> effects,
        ActionExecutionContext action,
        IReadOnlyList<AbilityTargetHitResult> targetResults,
        AbilityEffectExecutionTiming requiredTiming =
            AbilityEffectExecutionTiming.Deferred)
    {
        return
            AbilityEffectExecutionRouter.Execute(
                effects,
                action,
                targetResults ??
                EmptyTargetResults,
                requiredTiming
            );
    }

    /// <summary>
    /// Bekvämlighets-overload för en enda impacttarget.
    /// </summary>
    public static int Execute(
        IReadOnlyList<AbilityEffect> effects,
        ActionExecutionContext action,
        GameObject targetObject,
        CharacterStats target,
        AbilityTargetHitResult targetResult = null,
        AbilityEffectExecutionTiming requiredTiming =
            AbilityEffectExecutionTiming.Deferred)
    {
        IReadOnlyList<AbilityTargetHitResult>
            targetResults;

        if (targetResult != null)
        {
            targetResults =
                new[]
                {
                    targetResult
                };
        }
        else if (target != null)
        {
            targetResults =
                new[]
                {
                    new AbilityTargetHitResult(
                        targetObject != null
                            ? targetObject
                            : target.gameObject,
                        target,
                        0,
                        AbilityTargetHitOutcome.NotRolled
                    )
                };
        }
        else
        {
            targetResults =
                EmptyTargetResults;
        }

        return Execute(
            effects,
            action,
            targetResults,
            requiredTiming
        );
    }
}