using System;
using System.Collections.Generic;

public sealed class DeedRuntime
{
    private readonly HashSet<string> completedRankIds = new();
    private bool discovered;
    private bool tracked;

    public DeedDefinition Definition { get; }
    public DeedObjectiveRuntime Objective { get; private set; }
    public int Progress => Objective.Progress;
    public bool IsDiscovered => discovered;
    public bool IsTracked => tracked;
    public int CompletedRankCount => completedRankIds.Count;

    public bool AllRanksComplete =>
        completedRankIds.Count >= Definition.Ranks.Count;

    public bool ShouldShowCard =>
        Definition.Visibility != DeedVisibility.Secret ||
        CompletedRankCount > 0;

    public bool IsRevealed =>
        Definition.Visibility == DeedVisibility.Visible ||
        discovered || CompletedRankCount > 0;

    public DeedRankDefinition CurrentRank
    {
        get
        {
            foreach (DeedRankDefinition rank in Definition.Ranks)
            {
                if (!completedRankIds.Contains(rank.RankId))
                    return rank;
            }

            return Definition.Ranks[Definition.Ranks.Count - 1];
        }
    }

    public event Action<DeedRuntime> ProgressChanged;
    public event Action<DeedRuntime> DiscoveryChanged;
    public event Action<DeedRuntime> TrackingChanged;
    public event Action<DeedRuntime, DeedRankDefinition> RankCompleted;

    public DeedRuntime(DeedDefinition definition)
    {
        Definition = definition;
        Objective = CreateCurrentObjective();
        discovered = definition.Visibility == DeedVisibility.Visible;
    }

    public bool HasCompleted(DeedRankDefinition rank) =>
        rank != null && completedRankIds.Contains(rank.RankId);

    public int GetProgressForRank(DeedRankDefinition rank)
    {
        if (rank == null)
            return 0;

        if (HasCompleted(rank))
            return rank.RequiredTotalProgress;

        if (Definition.UseRankObjectives && rank != CurrentRank)
            return 0;

        return Math.Min(Progress, rank.RequiredTotalProgress);
    }

    public void Receive(DeedProgressEvent progressEvent)
    {
        if (progressEvent == null || AllRanksComplete)
            return;

        int before = Progress;
        Objective.Receive(progressEvent);

        if (Progress <= before)
            return;

        if (Definition.Visibility == DeedVisibility.Hidden &&
            Definition.RevealHiddenOnFirstProgress && !discovered)
        {
            discovered = true;
            DiscoveryChanged?.Invoke(this);
        }

        if (Definition.UseRankObjectives)
        {
            DeedRankDefinition rank = CurrentRank;

            if (Progress >= rank.RequiredTotalProgress &&
                completedRankIds.Add(rank.RankId))
            {
                if (!AllRanksComplete)
                    Objective = CreateCurrentObjective();

                RankCompleted?.Invoke(this, rank);
            }
        }
        else
        {
            foreach (DeedRankDefinition rank in Definition.Ranks)
            {
                if (Progress < rank.RequiredTotalProgress ||
                    !completedRankIds.Add(rank.RankId))
                {
                    continue;
                }

                RankCompleted?.Invoke(this, rank);
            }
        }

        ProgressChanged?.Invoke(this);
    }

    public void Reveal()
    {
        if (discovered)
            return;

        discovered = true;
        DiscoveryChanged?.Invoke(this);
    }

    public bool SetTracked(bool value)
    {
        if (tracked == value || (value && !ShouldShowCard))
            return false;

        tracked = value;
        TrackingChanged?.Invoke(this);
        return true;
    }

    public DeedStateSnapshot CaptureState()
    {
        return new DeedStateSnapshot
        {
            deedId = Definition.PersistentId,
            progress = Progress,
            progressRankId = Definition.UseRankObjectives
                ? CurrentRank.RankId
                : null,
            discovered = discovered,
            tracked = tracked,
            completedRankIds = new List<string>(completedRankIds)
        };
    }

    public void RestoreState(DeedStateSnapshot snapshot)
    {
        if (snapshot == null || snapshot.deedId != Definition.PersistentId)
            return;

        bool useRankObjectives = Definition.UseRankObjectives;
        bool savedSharedProgress = string.IsNullOrEmpty(snapshot.progressRankId);

        completedRankIds.Clear();

        HashSet<string> saved = new(
            snapshot.completedRankIds ?? new List<string>());

        foreach (DeedRankDefinition rank in Definition.Ranks)
        {
            if (saved.Contains(rank.RankId) ||
                (!useRankObjectives && savedSharedProgress &&
                 snapshot.progress >= rank.RequiredTotalProgress))
            {
                completedRankIds.Add(rank.RankId);
            }
        }

        Objective = CreateCurrentObjective();

        if (useRankObjectives)
        {
            DeedRankDefinition rank = CurrentRank;

            if (AllRanksComplete)
                Objective.RestoreProgress(rank.RequiredTotalProgress);
            else if (snapshot.progressRankId == rank.RankId)
                Objective.RestoreProgress(snapshot.progress);
        }
        else if (savedSharedProgress)
        {
            Objective.RestoreProgress(snapshot.progress);
        }

        discovered = snapshot.discovered ||
                     Definition.Visibility == DeedVisibility.Visible ||
                     CompletedRankCount > 0;

        tracked = snapshot.tracked && ShouldShowCard;

        ProgressChanged?.Invoke(this);
        DiscoveryChanged?.Invoke(this);
        TrackingChanged?.Invoke(this);
    }

    private DeedObjectiveRuntime CreateCurrentObjective()
    {
        DeedObjectiveDefinition objective = Definition.UseRankObjectives
            ? CurrentRank.Objective
            : Definition.Objective;

        return objective.CreateRuntime();
    }
}