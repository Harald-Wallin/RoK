using System;
using UnityEngine;

/// <summary>
/// Runtime-context för en enskild AbilityEffect-execution.
/// </summary>
public sealed class AbilityEffectExecutionContext
{
    public ActionExecutionContext Action { get; }

    public AbilityData Ability =>
        Action.Ability;

    public CharacterStats Caster =>
        Action.Caster;

    public DamageSourceContext DamageSource =>
    new DamageSourceContext(
        Caster,
        Caster,
        Ability
    );

    public GameObject TargetObject { get; }

    public CharacterStats Target { get; }

    public AbilityTargetHitResult TargetResult { get; }

    public int TargetIndex =>
        TargetResult != null
            ? TargetResult.TargetIndex
            : -1;

    public bool HasTargetObject =>
        TargetObject != null;

    public bool HasCharacterTarget =>
        Target != null;

    public bool TargetWasSuccessful =>
        TargetResult == null ||
        TargetResult.WasSuccessful;

    public Vector2 Origin =>
        Action.Origin;

    public Vector2 RawAimPoint =>
        Action.RawAimPoint;

    public Vector2 TargetPoint =>
        Action.TargetPoint;

    public Vector2 Direction =>
        Action.Direction;

    public float Distance =>
        Action.Distance;

    public float ChargeProgress =>
        Action.ChargeProgress;

    public AbilityEffectExecutionContext(
        ActionExecutionContext action,
        GameObject targetObject,
        CharacterStats target,
        AbilityTargetHitResult targetResult = null)
    {
        Action =
            action ??
            throw new ArgumentNullException(
                nameof(action)
            );

        TargetObject = targetObject;
        Target = target;
        TargetResult = targetResult;
    }
}