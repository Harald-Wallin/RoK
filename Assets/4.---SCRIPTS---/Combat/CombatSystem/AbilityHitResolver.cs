using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Auktoritativ hit- och dodge-resolution för actionsystemet.
///
/// Samma resolver används av:
/// - melee
/// - spells
/// - projectiles
/// - traps
/// - delayed impacts
/// - secondary effects
///
/// Varje faktisk impact får exakt ETT gemensamt
/// hit/dodge-resultat som återanvänds av samtliga effects.
/// </summary>
public static class AbilityHitResolver
{
    // =========================================================
    // ACTION TARGETS
    // =========================================================

    public static List<AbilityTargetHitResult>
        ResolveActionTargets(
            ActionExecutionContext action)
    {
        List<AbilityTargetHitResult> results =
            new();

        if (action == null ||
            action.Ability == null)
        {
            return results;
        }

        IReadOnlyList<CharacterStats> targets =
            action.AffectedCharacters;

        if (targets == null)
            return results;

        bool shouldResolveImmediately =
            action.Ability.ExecutionSettings == null ||
            action.Ability
                .ExecutionSettings
                .ResolvesTargetHitImmediately;

        for (int i = 0;
             i < targets.Count;
             i++)
        {
            CharacterStats target =
                targets[i];

            if (target == null)
                continue;

            AbilityTargetHitResult result =
                ResolveTarget(
                    action,
                    target.gameObject,
                    target,
                    i,
                    shouldResolveImmediately
                );

            if (result != null)
            {
                results.Add(
                    result
                );
            }
        }

        return results;
    }

    // =========================================================
    // SINGLE TARGET
    // =========================================================

    public static AbilityTargetHitResult ResolveTarget(
        ActionExecutionContext action,
        GameObject targetObject,
        CharacterStats target,
        int targetIndex = 0,
        bool resolveHitNow = true)
    {
        if (action == null ||
            action.Ability == null ||
            action.Caster == null ||
            target == null)
        {
            return null;
        }

        targetObject =
            targetObject != null
                ? TargetUtility
                    .ResolveCharacterTarget(
                        targetObject
                    )
                : target.gameObject;

        AbilityTargetHitOutcome outcome =
            ResolveOutcome(
                action,
                target,
                resolveHitNow
            );

        return new AbilityTargetHitResult(
            targetObject,
            target,
            targetIndex,
            outcome
        );
    }

    // =========================================================
    // HIT RESOLUTION
    // =========================================================

    /// <summary>
    /// Avgör resultatet av EN faktisk träffhändelse.
    ///
    /// ACTIONSYSTEMETS REGLER:
    ///
    /// alwaysHits
    ///     -> automatisk success
    ///
    /// canMiss == false
    ///     -> automatisk success
    ///
    /// canMiss == true
    ///     -> CombatResolver.RollHit
    ///     -> CombatResolver.RollDodge
    ///
    /// requiresHitCheck används INTE här.
    ///
    /// Det fältet tillhör legacy-systemet och ska inte kunna
    /// råka stänga av hit-resolution för nya actions.
    /// </summary>
    public static AbilityTargetHitOutcome ResolveOutcome(
        ActionExecutionContext action,
        CharacterStats target,
        bool resolveHitNow = true)
    {
        if (action == null ||
            action.Ability == null ||
            action.Caster == null ||
            target == null)
        {
            return AbilityTargetHitOutcome.Miss;
        }

        /*
         * Exempelvis projectile:
         *
         * Actionen skapas nu, men själva hit-roll sker först
         * när projektilen faktiskt kolliderar.
         */
        if (!resolveHitNow)
        {
            return AbilityTargetHitOutcome.NotRolled;
        }

        AbilityData ability =
            action.Ability;

        // -----------------------------------------------------
        // AUTOMATIC HIT
        // -----------------------------------------------------

        if (ability.alwaysHits ||
            !ability.canMiss)
        {
            return AbilityTargetHitOutcome.Hit;
        }

        // -----------------------------------------------------
        // HIT
        // -----------------------------------------------------

        if (!CombatResolver.RollHit(
                action.Caster,
                target))
        {
            return AbilityTargetHitOutcome.Miss;
        }

        // -----------------------------------------------------
        // DODGE
        // -----------------------------------------------------

        if (CombatResolver.RollDodge(
                action.Caster,
                target))
        {
            return AbilityTargetHitOutcome.Evaded;
        }

        return AbilityTargetHitOutcome.Hit;
    }
}