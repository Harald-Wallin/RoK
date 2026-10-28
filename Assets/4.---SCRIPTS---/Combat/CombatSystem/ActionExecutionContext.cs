using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Immutable runtime-snapshot av en action vid execution.
///
/// Contexten förändras inte efter att den skapats.
/// Effekter kan därför tryggt använda samma targetingdata även om
/// den ursprungliga ActionContext senare avslutas eller rensas.
/// </summary>
public sealed class ActionExecutionContext
{
    private static readonly IReadOnlyList<GameObject>
        EmptyGameObjects =
            Array.Empty<GameObject>();

    private static readonly IReadOnlyList<CharacterStats>
        EmptyCharacters =
            Array.Empty<CharacterStats>();

    private readonly GameObject[] affectedTargets;
    private readonly CharacterStats[] affectedCharacters;

    public Guid ActionId { get; }

    public AbilityData Ability { get; }

    public CharacterStats Caster { get; }

    public GameObject CasterObject =>
        Caster != null
            ? Caster.gameObject
            : null;

    public AbilityTargetingSettings TargetingSettings
    {
        get;
    }

    public GameObject PrimaryTarget { get; }

    public CharacterStats PrimaryCharacterTarget { get; }

    public Vector2 Origin { get; }

    public Vector2 RawAimPoint { get; }

    public Vector2 TargetPoint { get; }

    public Vector2 Direction { get; }

    public float Distance { get; }

    public float ChargeProgress { get; }

    public float StartedAt { get; }

    public float ExecutedAt { get; }

    public IReadOnlyList<GameObject> AffectedTargets =>
        affectedTargets ??
        EmptyGameObjects;

    public IReadOnlyList<CharacterStats>
        AffectedCharacters =>
            affectedCharacters ??
            EmptyCharacters;

    public bool HasPrimaryTarget =>
        PrimaryTarget != null;

    public bool HasAffectedTargets =>
        affectedTargets != null &&
        affectedTargets.Length > 0;

    public ActionExecutionContext(
        Guid actionId,
        AbilityData ability,
        CharacterStats caster,
        AbilityTargetingSettings targetingSettings,
        GameObject primaryTarget,
        Vector2 origin,
        Vector2 rawAimPoint,
        Vector2 targetPoint,
        Vector2 direction,
        float distance,
        IReadOnlyList<GameObject> sourceTargets,
        float chargeProgress,
        float startedAt,
        float executedAt)
    {
        ActionId = actionId;

        Ability =
            ability ??
            throw new ArgumentNullException(
                nameof(ability)
            );

        Caster =
            caster ??
            throw new ArgumentNullException(
                nameof(caster)
            );

        TargetingSettings =
            targetingSettings;

        PrimaryTarget =
            primaryTarget;

        PrimaryCharacterTarget =
            TargetUtility.GetCharacterStats(
                primaryTarget
            );

        Origin = origin;
        RawAimPoint = rawAimPoint;
        TargetPoint = targetPoint;

        Direction =
            direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector2.down;

        Distance =
            Mathf.Max(
                0f,
                distance
            );

        ChargeProgress =
            Mathf.Clamp01(
                chargeProgress
            );

        StartedAt = startedAt;
        ExecutedAt = executedAt;

        affectedTargets =
            CopyGameObjectTargets(
                sourceTargets
            );

        affectedCharacters =
            BuildCharacterTargets(
                affectedTargets
            );
    }

    public bool ContainsTarget(
        GameObject target)
    {
        if (target == null ||
            affectedTargets == null)
        {
            return false;
        }

        for (int i = 0;
             i < affectedTargets.Length;
             i++)
        {
            if (affectedTargets[i] == target)
                return true;
        }

        return false;
    }

    public bool ContainsCharacter(
        CharacterStats target)
    {
        if (target == null ||
            affectedCharacters == null)
        {
            return false;
        }

        for (int i = 0;
             i < affectedCharacters.Length;
             i++)
        {
            if (affectedCharacters[i] == target)
                return true;
        }

        return false;
    }

    private static GameObject[] CopyGameObjectTargets(
        IReadOnlyList<GameObject> source)
    {
        if (source == null ||
            source.Count == 0)
        {
            return Array.Empty<GameObject>();
        }

        List<GameObject> targets =
            new List<GameObject>(
                source.Count
            );

        for (int i = 0;
             i < source.Count;
             i++)
        {
            GameObject target =
                source[i];

            if (target == null)
                continue;

            if (targets.Contains(target))
                continue;

            targets.Add(target);
        }

        return targets.ToArray();
    }

    private static CharacterStats[]
        BuildCharacterTargets(
            IReadOnlyList<GameObject> source)
    {
        if (source == null ||
            source.Count == 0)
        {
            return
                Array.Empty<CharacterStats>();
        }

        List<CharacterStats> targets =
            new List<CharacterStats>(
                source.Count
            );

        for (int i = 0;
             i < source.Count;
             i++)
        {
            CharacterStats target =
                TargetUtility.GetCharacterStats(
                    source[i]
                );

            if (target == null)
                continue;

            if (targets.Contains(target))
                continue;

            targets.Add(target);
        }

        return targets.ToArray();
    }
}
