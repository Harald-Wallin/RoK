using System;
using UnityEngine;

[Serializable]
public sealed class AbilityTargetingSettings
{
    [Header("Mode")]

    [SerializeField]
    private TargetingMode targetingMode =
        TargetingMode.SingleTarget;

    [SerializeField]
    [Tooltip(
        "Vilka relationer som får räknas som giltiga mål."
    )]
    private TargetRelation allowedRelations =
        TargetRelation.Hostile;

    [Header("Range")]

    [SerializeField]
    [Min(0f)]
    private float range = 2f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Minsta tillåtna avstånd från castern. " +
        "För Line skapar detta ett tomt område närmast castern."
    )]
    private float minimumRange;

    [Header("Shape")]

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Används av Circle och andra radiusbaserade former."
    )]
    private float radius = 1f;

    [SerializeField]
    [Range(0f, 360f)]
    [Tooltip(
        "Konens totala vinkel i grader."
    )]
    private float coneAngle = 90f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Bredden på Line-targeting."
    )]
    private float lineWidth = 0.5f;

    [SerializeField]
    [Tooltip(
        "To Cursor: linjen slutar vid muspekaren, begränsad " +
        "av Range.\n\n" +
        "Full Range: muspekaren bestämmer endast riktningen " +
        "och linjen använder alltid full Range."
    )]
    private LineLengthMode lineLengthMode =
        LineLengthMode.ToCursor;

    [Header("Target selection")]

    [SerializeField]
    private TargetSelectionMode selectionMode =
        TargetSelectionMode.All;

    [SerializeField]
    [Min(0)]
    [Tooltip(
        "Maximalt antal targets som faktiskt påverkas. " +
        "Värdet 0 betyder obegränsat antal."
    )]
    private int maximumTargets;

    [SerializeField]
    [Tooltip(
        "Kräver att minst ett giltigt target hittas för att " +
        "actionen ska kunna bekräftas."
    )]
    private bool requiresAffectedTarget = true;

    [Header("Physics")]

    [SerializeField]
    [Tooltip(
        "Lager som TargetResolver söker efter targetbara objekt på."
    )]
    private LayerMask targetLayers;

    [SerializeField]
    private AbilityLineOfSightSettings lineOfSight =
        new();

    public TargetingMode TargetingMode =>
        targetingMode;

    public TargetRelation AllowedRelations =>
        allowedRelations;

    public float Range =>
        Mathf.Max(
            0f,
            range
        );

    public float MinimumRange =>
        Mathf.Clamp(
            minimumRange,
            0f,
            Range
        );

    public float Radius =>
        Mathf.Max(
            0f,
            radius
        );

    public float ConeAngle =>
        Mathf.Clamp(
            coneAngle,
            0f,
            360f
        );

    public float LineWidth =>
        Mathf.Max(
            0f,
            lineWidth
        );

    public LineLengthMode LineLengthMode =>
        lineLengthMode;

    public TargetSelectionMode SelectionMode =>
        selectionMode;

    /// <summary>
    /// Noll betyder obegränsat antal.
    /// </summary>
    public int MaximumTargets =>
        Mathf.Max(
            0,
            maximumTargets
        );

    public bool HasTargetLimit =>
        MaximumTargets > 0;

    public bool RequiresAffectedTarget =>
        requiresAffectedTarget;

    public LayerMask TargetLayers =>
        targetLayers;

    public AbilityLineOfSightSettings LineOfSight =>
        lineOfSight;

    public bool AllowsRelation(
        TargetRelation relation)
    {
        return
            relation != TargetRelation.None &&
            (
                allowedRelations &
                relation
            ) != 0;
    }
}