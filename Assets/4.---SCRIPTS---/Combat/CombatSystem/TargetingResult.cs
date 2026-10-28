using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Innehåller hela resultatet från en targetingberäkning.
///
/// Resultatet byggs stegvis:
///
/// GeometryTargets
///     Targets som ligger inom targetingformen.
///
/// ValidTargets
///     GeometryTargets som klarat gameplayvalideringen.
///
/// AffectedTargets
///     De targets som faktiskt valts ut och kommer påverkas.
/// </summary>
public sealed class TargetingResult
{
    private readonly List<GameObject> geometryTargets =
        new();

    private readonly List<GameObject> validTargets =
        new();

    private readonly List<GameObject> affectedTargets =
        new();

    /// <summary>
    /// De targetinginställningar som användes för att skapa
    /// resultatet.
    ///
    /// Preview och andra system kan läsa form, range och
    /// urvalsregler härifrån utan att resultatet duplicerar datan.
    /// </summary>
    public AbilityTargetingSettings Settings
    {
        get;
        internal set;
    }

    /// <summary>
    /// Om actionen kan bekräftas och utföras med detta resultat.
    /// </summary>
    public bool IsValid
    {
        get;
        internal set;
    }

    /// <summary>
    /// Varför targetingen är ogiltig.
    ///
    /// Är None när resultatet är giltigt.
    /// </summary>
    public TargetingFailureReason FailureReason
    {
        get;
        internal set;
    }

    /// <summary>
    /// Den logiska startpunkten för targetingen.
    ///
    /// Detta är vanligtvis casterns världsposition.
    /// </summary>
    public Vector2 Origin
    {
        get;
        internal set;
    }

    /// <summary>
    /// Den ursprungliga aim-punkten från input eller AI.
    ///
    /// Punkten kan ligga utanför actionens tillåtna range.
    /// </summary>
    public Vector2 RawAimPoint
    {
        get;
        internal set;
    }

    /// <summary>
    /// Den slutgiltiga aim-punkten efter att rangebegränsningen
    /// har applicerats.
    /// </summary>
    public Vector2 TargetPoint
    {
        get;
        internal set;
    }

    /// <summary>
    /// Den normaliserade riktningen från Origin mot TargetPoint.
    /// </summary>
    public Vector2 Direction
    {
        get;
        internal set;
    }

    /// <summary>
    /// Avståndet mellan Origin och TargetPoint.
    /// </summary>
    public float Distance
    {
        get;
        internal set;
    }

    /// <summary>
    /// Actionens huvudsakliga target, om ett sådant finns.
    ///
    /// Efter targeturvalet är detta normalt det första objektet
    /// i AffectedTargets.
    /// </summary>
    /// 
    /// <summary>
    /// Den maximala range som användes i just denna
    /// targetingberäkning.
    ///
    /// Kan vara kortare än abilityns vanliga range vid charge.
    /// </summary>
    public float EffectiveRange
    {
        get;
        internal set;
    }
    public GameObject PrimaryTarget
    {
        get;
        internal set;
    }

    /// <summary>
    /// Samtliga unika targets som befinner sig inom den
    /// geometriska targetingformen innan gameplayfilter används.
    /// </summary>
    public IReadOnlyList<GameObject> GeometryTargets =>
        geometryTargets;

    /// <summary>
    /// Targets som har klarat de generella valideringsreglerna,
    /// exempelvis targettyp, relation, livsstatus, range och LoS.
    /// </summary>
    public IReadOnlyList<GameObject> ValidTargets =>
        validTargets;

    /// <summary>
    /// Targets som faktiskt kommer påverkas när actionen
    /// exekveras.
    ///
    /// Samma lista ska användas av preview och execution.
    /// </summary>
    public IReadOnlyList<GameObject> AffectedTargets =>
        affectedTargets;

    public TargetingResult()
    {
        Reset();
    }

    /// <summary>
    /// Återställer resultatet så att samma instans kan
    /// återanvändas för en ny targetingberäkning.
    /// </summary>
    internal void Reset()
    {
        Settings = null;

        IsValid = false;

        FailureReason =
            TargetingFailureReason.None;

        Origin = Vector2.zero;
        RawAimPoint = Vector2.zero;
        TargetPoint = Vector2.zero;
        Direction = Vector2.down;
        Distance = 0f;
        EffectiveRange = 0f;

        PrimaryTarget = null;

        geometryTargets.Clear();
        validTargets.Clear();
        affectedTargets.Clear();
    }

    internal void AddGeometryTarget(
        GameObject target)
    {
        AddUnique(
            geometryTargets,
            target
        );
    }

    internal void AddValidTarget(
        GameObject target)
    {
        AddUnique(
            validTargets,
            target
        );
    }

    internal void AddAffectedTarget(
        GameObject target)
    {
        AddUnique(
            affectedTargets,
            target
        );
    }

    internal void ClearAffectedTargets()
    {
        affectedTargets.Clear();
        PrimaryTarget = null;
    }

    internal void SetInvalid(
        TargetingFailureReason reason)
    {
        IsValid = false;
        FailureReason = reason;
    }

    internal void SetValid()
    {
        IsValid = true;

        FailureReason =
            TargetingFailureReason.None;
    }

    private static void AddUnique(
        List<GameObject> collection,
        GameObject target)
    {
        if (target == null)
            return;

        if (collection.Contains(target))
            return;

        collection.Add(target);
    }
}