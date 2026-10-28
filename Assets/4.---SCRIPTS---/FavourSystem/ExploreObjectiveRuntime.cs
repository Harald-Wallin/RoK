using System;
using System.Collections.Generic;

public sealed class ExploreObjectiveRuntime :
    FavourObjectiveRuntime
{
    private readonly ExploreObjectiveData
        exploreData;

    private readonly HashSet<string>
        completedZoneIds =
            new(
                StringComparer.Ordinal
            );

    public ExploreObjectiveRuntime(
        ExploreObjectiveData data,
        FavourRuntime favour)
        : base(
            data,
            favour)
    {
        exploreData =
            data;
    }

    public override bool IsComplete =>
        RequiredProgress > 0 &&
        CurrentProgress >=
        RequiredProgress;

    public override int CurrentProgress =>
        completedZoneIds.Count;

    public override int RequiredProgress =>
        exploreData != null
            ? exploreData
                .RequiredExplorations
            : 0;

    protected override void OnActivated()
    {
        ExplorationEvents
            .ZoneEntered +=
            HandleZoneEntered;
    }

    protected override void OnDeactivated()
    {
        ExplorationEvents
            .ZoneEntered -=
            HandleZoneEntered;
    }

    private void HandleZoneEntered(
        string zoneId)
    {
        if (!IsActive ||
            IsComplete ||
            exploreData == null ||
            string.IsNullOrWhiteSpace(
                zoneId))
        {
            return;
        }

        if (!exploreData.ContainsZone(
                zoneId))
        {
            return;
        }

        if (!completedZoneIds.Add(
                zoneId))
        {
            return;
        }

        RaiseProgressChanged();
    }

    public override bool IsRelevantToEntity(
        string entityId)
    {
        if (exploreData == null ||
            string.IsNullOrWhiteSpace(
                entityId))
        {
            return false;
        }

        if (!exploreData.ContainsZone(
                entityId))
        {
            return false;
        }

        /*
         * När objective:t är färdigt behåller vi relevansen
         * tills favourn lämnas in, precis som med våra andra
         * presentation-targets.
         */
        if (IsComplete)
            return true;

        return !completedZoneIds.Contains(
            entityId
        );
    }

    public override void ResetProgress()
    {
        if (completedZoneIds.Count == 0)
            return;

        completedZoneIds.Clear();

        RaiseProgressChanged();
    }
}
