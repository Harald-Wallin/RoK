using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class InteractObjectiveRuntime : FavourObjectiveRuntime
{
    private readonly InteractObjectiveData interactData;
    private readonly HashSet<string> completedTargetIds = new(StringComparer.Ordinal);
    private readonly HashSet<GameObject> completedInstances = new();

    public InteractObjectiveRuntime(
        InteractObjectiveData data,
        FavourRuntime favour)
        : base(data, favour)
    {
        interactData = data;
    }

    public override bool IsComplete =>
        RequiredProgress > 0 && CurrentProgress >= RequiredProgress;

    public override int CurrentProgress =>
        interactData.CountMode == InteractionCountMode.ObjectInstances
            ? completedInstances.Count
            : completedTargetIds.Count;

    public override int RequiredProgress => interactData.RequiredInteractions;

    protected override void OnActivated()
    {
        InteractionTimingQuery.Resolving += ResolveTiming;
        InteractionEvents.InteractionCommitted += HandleInteraction;
    }

    protected override void OnDeactivated()
    {
        InteractionTimingQuery.Resolving -= ResolveTiming;
        InteractionEvents.InteractionCommitted -= HandleInteraction;
    }

    public bool RequiresTarget(string entityId)
    {
        return interactData.ContainsTarget(entityId);
    }

    public override bool IsRelevantToEntity(string entityId)
    {
        if (!interactData.ContainsTarget(entityId))
            return false;

        return IsComplete ||
               interactData.CountMode == InteractionCountMode.ObjectInstances ||
               !completedTargetIds.Contains(entityId);
    }

    private bool Matches(InteractionContext context)
    {
        return IsActive &&
               !IsComplete &&
               context.IsValid &&
               context.Player == Favour?.Manager?.Player &&
               context.Target.Identity != null &&
               interactData.ContainsTarget(context.Target.Identity.Id);
    }

    private void ResolveTiming(InteractionTimingQuery query)
    {
        if (Matches(query.Context))
            query.Use(interactData.Timing, DisplayName);
    }

    private void HandleInteraction(InteractionContext context)
    {
        if (!Matches(context))
            return;

        bool added = interactData.CountMode == InteractionCountMode.ObjectInstances
            ? completedInstances.Add(context.Target.InteractionOwner)
            : completedTargetIds.Add(context.Target.Identity.Id);

        if (added)
            RaiseProgressChanged();
    }

    public override void ResetProgress()
    {
        if (CurrentProgress == 0)
            return;

        completedTargetIds.Clear();
        completedInstances.Clear();
        RaiseProgressChanged();
    }
}