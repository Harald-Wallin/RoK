using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ActionContext
{
    public Guid ActionId { get; }

    public CharacterStats Caster { get; }

    public AbilityData Ability { get; }

    public ActionRequest Request { get; }

    public TargetingResult Targeting
    {
        get;
        private set;
    }

    public ActionPhase Phase
    {
        get;
        internal set;
    }

    public float NormalizedProgress
    {
        get;
        internal set;
    }

    public float StartedAt
    {
        get;
        internal set;
    }

    public float ExecutedAt
    {
        get;
        internal set;
    }

    public float ChargeProgress
    {
        get;
        internal set;
    }

    public GameObject PrimaryTarget =>
        Targeting?.PrimaryTarget;

    public Vector2 Origin =>
        Targeting != null
            ? Targeting.Origin
            : Caster != null
                ? Caster.transform.position
                : Vector2.zero;

    public Vector2 RawAimPoint =>
        Targeting?.RawAimPoint ?? Origin;

    public Vector2 TargetPoint =>
        Targeting?.TargetPoint ?? Origin;

    public Vector2 AimDirection =>
        Targeting != null
            ? Targeting.Direction
            : Vector2.down;

    public float TargetDistance =>
        Targeting != null
            ? Targeting.Distance
            : 0f;

    public IReadOnlyList<GameObject> AffectedTargets =>
        Targeting != null
            ? Targeting.AffectedTargets
            : EmptyTargets;

    public bool HasValidTargeting =>
        Targeting != null &&
        Targeting.IsValid;

    private static readonly IReadOnlyList<GameObject>
        EmptyTargets =
            Array.Empty<GameObject>();

    public ActionContext(
        ActionRequest request)
    {
        Request =
            request ??
            throw new ArgumentNullException(
                nameof(request)
            );

        ActionId = Guid.NewGuid();

        Caster = request.Caster;
        Ability = request.Ability;

        Phase = ActionPhase.Idle;

        NormalizedProgress = 0f;
        ChargeProgress = 0f;

        StartedAt = Time.time;
        ExecutedAt = -1f;
    }

    public void UpdateTargeting(
        TargetingResult targetingResult)
    {
        Targeting =
            targetingResult ??
            throw new ArgumentNullException(
                nameof(targetingResult)
            );
    }

    /// <summary>
    /// Skapar actionens immutable execution-snapshot.
    ///
    /// Targeting måste vara giltig när metoden anropas.
    /// </summary>
    public ActionExecutionContext
        CreateExecutionContext()
    {
        if (Caster == null)
        {
            throw new InvalidOperationException(
                "ActionContext saknar Caster."
            );
        }

        if (Ability == null)
        {
            throw new InvalidOperationException(
                "ActionContext saknar Ability."
            );
        }

        if (Targeting == null)
        {
            throw new InvalidOperationException(
                "ActionContext saknar TargetingResult."
            );
        }

        if (!Targeting.IsValid)
        {
            throw new InvalidOperationException(
                "En execution-context kan inte skapas från " +
                "ogiltig targeting."
            );
        }

        float executionTime =
            Time.time;

        return new ActionExecutionContext(
            ActionId,
            Ability,
            Caster,
            Targeting.Settings,
            Targeting.PrimaryTarget,
            Targeting.Origin,
            Targeting.RawAimPoint,
            Targeting.TargetPoint,
            Targeting.Direction,
            Targeting.Distance,
            Targeting.AffectedTargets,
            ChargeProgress,
            StartedAt,
            executionTime
        );
    }

    internal void MarkExecuted(
        float executionTime)
    {
        ExecutedAt =
            Mathf.Max(
                StartedAt,
                executionTime
            );
    }

    internal void MarkExecuted()
    {
        MarkExecuted(
            Time.time
        );
    }
}