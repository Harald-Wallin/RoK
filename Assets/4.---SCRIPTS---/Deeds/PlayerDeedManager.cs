using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One component on the Player.
/// Automatically discovers DeedDefinition assets under Resources/Deeds.
/// Routes existing combat events to data-driven deed objectives.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
public sealed class PlayerDeedManager : MonoBehaviour
{
    private const string ResourcesFolder = "Deeds";
    private const int MaximumTrackedDeeds = 5;

    private readonly List<DeedRuntime> deeds = new();
    private readonly Dictionary<string, DeedRuntime> deedsById = new();

    private PlayerStats player;

    public IReadOnlyList<DeedRuntime> Deeds => deeds;

    public event Action<DeedRuntime> DeedProgressChanged;
    public event Action<DeedRuntime> DeedDiscovered;
    public event Action<DeedRuntime> DeedTrackingChanged;
    public event Action<DeedRuntime, DeedRankDefinition>
        DeedRankCompleted;

    private void Awake()
    {
        player = GetComponent<PlayerStats>();
        InitializeFromResources();
    }

    private void OnEnable()
    {
        CharacterCombatEvents.CharacterDefeated +=
            HandleCharacterDefeated;
    }

    private void OnDisable()
    {
        CharacterCombatEvents.CharacterDefeated -=
            HandleCharacterDefeated;
    }

    private void InitializeFromResources()
    {
        deeds.Clear();
        deedsById.Clear();

        DeedDefinition[] definitions =
            Resources.LoadAll<DeedDefinition>(ResourcesFolder);

        foreach (DeedDefinition definition in definitions)
        {
            if (definition == null)
                continue;

            if (!definition.IsValid)
            {
                Debug.LogWarning(
                    $"Skipping invalid deed asset '{definition.name}'. " +
                    "Assign an objective and ascending ranks.",
                    definition);
                continue;
            }

            if (deedsById.ContainsKey(definition.PersistentId))
            {
                Debug.LogError(
                    $"Duplicate Deed ID: {definition.PersistentId}. " +
                    $"Asset '{definition.name}' was skipped.",
                    definition);
                continue;
            }

            DeedRuntime runtime = new(definition);
            runtime.ProgressChanged += HandleProgressChanged;
            runtime.DiscoveryChanged += HandleDiscoveryChanged;
            runtime.TrackingChanged += HandleTrackingChanged;
            runtime.RankCompleted += HandleRankCompleted;

            deeds.Add(runtime);
            deedsById.Add(definition.PersistentId, runtime);
        }

        if (definitions.Length == 0)
        {
            Debug.LogWarning(
                "No DeedDefinition assets found under " +
                "Assets/Resources/Deeds.", this);
        }
    }

    private void HandleCharacterDefeated(CharacterDefeatedResult result)
    {
        if (result == null || player == null)
            return;

        Report(new DeedCharacterDefeatedEvent(player, result));
    }

    /// <summary>
    /// Future road, favour and weapon systems can report new event
    /// types here without modifying PlayerDeedManager.
    /// </summary>
    public void Report(DeedProgressEvent progressEvent)
    {
        if (!isActiveAndEnabled || progressEvent == null ||
            !ReferenceEquals(progressEvent.Actor, player))
            return;

        foreach (DeedRuntime deed in deeds)
            deed.Receive(progressEvent);
    }

    public bool TryGetDeed(string deedId, out DeedRuntime deed) =>
        deedsById.TryGetValue(deedId, out deed);

    public bool SetTracked(DeedDefinition definition, bool tracked)
    {
        if (definition == null ||
            !deedsById.TryGetValue(definition.PersistentId,
                out DeedRuntime runtime))
            return false;

        if (tracked)
        {
            int count = 0;
            foreach (DeedRuntime deed in deeds)
                if (deed.IsTracked) count++;

            if (count >= MaximumTrackedDeeds && !runtime.IsTracked)
                return false;
        }

        return runtime.SetTracked(tracked);
    }

    // =====================================================
    // SAVE SYSTEM INTEGRATION POINTS
    // =====================================================

    public List<DeedStateSnapshot> CaptureStates()
    {
        List<DeedStateSnapshot> snapshots = new();
        foreach (DeedRuntime deed in deeds)
            snapshots.Add(deed.CaptureState());
        return snapshots;
    }

    public void RestoreStates(IEnumerable<DeedStateSnapshot> snapshots)
    {
        if (snapshots == null)
            return;

        foreach (DeedStateSnapshot state in snapshots)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.deedId))
                continue;

            if (deedsById.TryGetValue(state.deedId, out DeedRuntime deed))
                deed.RestoreState(state);
        }

        // Prevent malformed save data exceeding the track limit.
        int trackedCount = 0;
        foreach (DeedRuntime deed in deeds)
        {
            if (!deed.IsTracked)
                continue;

            trackedCount++;
            if (trackedCount > MaximumTrackedDeeds)
                deed.SetTracked(false);
        }
    }

    private void HandleProgressChanged(DeedRuntime deed) =>
        DeedProgressChanged?.Invoke(deed);

    private void HandleDiscoveryChanged(DeedRuntime deed) =>
        DeedDiscovered?.Invoke(deed);

    private void HandleTrackingChanged(DeedRuntime deed) =>
        DeedTrackingChanged?.Invoke(deed);

    private void HandleRankCompleted(
        DeedRuntime deed, DeedRankDefinition rank)
    {
        DeedRankCompleted?.Invoke(deed, rank);

        Debug.Log(
            $"Deed completed: {deed.Definition.DisplayName} " +
            $"{rank.RankLabel} ({deed.Progress} progress)", this);
    }
}
