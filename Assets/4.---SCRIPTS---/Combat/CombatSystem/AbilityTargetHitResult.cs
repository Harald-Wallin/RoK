using UnityEngine;

public enum AbilityTargetHitOutcome
{
    NotRolled,
    Hit,
    Miss,
    Evaded
}

/// <summary>
/// Auktoritativt targetresultat för en action.
///
/// Samma resultat återanvänds av samtliga effekter som körs
/// mot targetet. Därmed görs inte separata hit/dodge-slag för
/// exempelvis damage, bleed och stun.
/// </summary>
public sealed class AbilityTargetHitResult
{
    public GameObject TargetObject { get; }

    public CharacterStats Target { get; }

    public int TargetIndex { get; }

    public AbilityTargetHitOutcome Outcome { get; }

    public bool WasRolled =>
        Outcome != AbilityTargetHitOutcome.NotRolled;

    public bool WasSuccessful =>
        Outcome == AbilityTargetHitOutcome.Hit ||
        Outcome == AbilityTargetHitOutcome.NotRolled;

    public bool IsMiss =>
        Outcome == AbilityTargetHitOutcome.Miss;

    public bool IsEvaded =>
        Outcome == AbilityTargetHitOutcome.Evaded;

    public AbilityTargetHitResult(
        GameObject targetObject,
        CharacterStats target,
        int targetIndex,
        AbilityTargetHitOutcome outcome)
    {
        TargetObject = targetObject;
        Target = target;
        TargetIndex = targetIndex;
        Outcome = outcome;
    }
}
