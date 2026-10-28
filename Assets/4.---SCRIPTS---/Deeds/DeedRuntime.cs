using System;
using System.Collections.Generic;

/// <summary>
/// One player's independent progress for one deed definition.
/// Progress thresholds are cumulative, e.g. 10 / 100 / 500.
/// </summary>
public sealed class DeedRuntime
{
    private readonly HashSet<string> completedRankIds = new();
    private bool discovered;
    private bool tracked;

    public DeedDefinition Definition { get; }
    public DeedObjectiveRuntime Objective { get; }
    public int Progress => Objective.Progress;
    public bool IsDiscovered => discovered;
    public bool IsTracked => tracked;
    public int CompletedRankCount => completedRankIds.Count;
    public bool AllRanksComplete =>
        completedRankIds.Count >= Definition.Ranks.Count;

    // Hidden: the UI can show a mystery card.
    // Secret: no card appears until at least one rank is completed.
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
            return Definition.Ranks.Count > 0
                ? Definition.Ranks[Definition.Ranks.Count - 1]
                : null;
        }
    }

    public event Action<DeedRuntime> ProgressChanged;
    public event Action<DeedRuntime> DiscoveryChanged;
    public event Action<DeedRuntime> TrackingChanged;
    public event Action<DeedRuntime, DeedRankDefinition> RankCompleted;

    public DeedRuntime(DeedDefinition definition)
    {
        Definition = definition;
        Objective = definition.Objective.CreateRuntime();
        discovered = definition.Visibility == DeedVisibility.Visible;
    }

    public bool HasCompleted(DeedRankDefinition rank) =>
        rank != null && completedRankIds.Contains(rank.RankId);

    public void Receive(DeedProgressEvent progressEvent)
    {
        if (progressEvent == null || AllRanksComplete)
            return;

        int before = Objective.Progress;
        Objective.Receive(progressEvent);
        if (Objective.Progress <= before)
            return;

        if (Definition.Visibility == DeedVisibility.Hidden &&
            Definition.RevealHiddenOnFirstProgress && !discovered)
        {
            discovered = true;
            DiscoveryChanged?.Invoke(this);
        }

        foreach (DeedRankDefinition rank in Definition.Ranks)
        {
            if (Objective.Progress < rank.RequiredTotalProgress ||
                !completedRankIds.Add(rank.RankId))
                continue;

            RankCompleted?.Invoke(this, rank);
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
            discovered = discovered,
            tracked = tracked,
            completedRankIds = new List<string>(completedRankIds)
        };
    }

    /// <summary>
    /// Restoration never grants rewards or replays completion notifications.
    /// Rank IDs survive rank reordering and threshold adjustments.
    /// </summary>
    public void RestoreState(DeedStateSnapshot snapshot)
    {
        if (snapshot == null ||
            snapshot.deedId != Definition.PersistentId)
            return;

        Objective.RestoreProgress(snapshot.progress);

        completedRankIds.Clear();
        HashSet<string> saved = new(
            snapshot.completedRankIds ?? new List<string>());

        foreach (DeedRankDefinition rank in Definition.Ranks)
        {
            if (saved.Contains(rank.RankId) ||
                Progress >= rank.RequiredTotalProgress)
                completedRankIds.Add(rank.RankId);
        }

        discovered = snapshot.discovered ||
                     Definition.Visibility == DeedVisibility.Visible ||
                     CompletedRankCount > 0;
        tracked = snapshot.tracked && ShouldShowCard;

        ProgressChanged?.Invoke(this);
        DiscoveryChanged?.Invoke(this);
        TrackingChanged?.Invoke(this);
    }
}
