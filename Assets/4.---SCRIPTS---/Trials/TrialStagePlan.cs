using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class TrialStagePlan
{
    internal readonly struct Goal
    {
        public readonly TrialObjectiveType Type;
        public readonly string Label;
        public readonly string TargetId;
        public readonly int RequiredCount;
        public readonly InteractionTimingSettings Timing;

        public Goal(
            TrialObjectiveType type,
            string label,
            string targetId,
            int count,
            InteractionTimingSettings timing = null)
        {
            Type = type;
            Label = label;
            TargetId = targetId;
            RequiredCount = count;
            Timing = timing;
        }
    }

    internal readonly struct Spawn
    {
        public readonly GameObject Prefab;
        public readonly Transform Point;
        public readonly int ObjectiveIndex;
        public readonly string EntityId;

        public Spawn(GameObject prefab, Transform point, int objectiveIndex)
        {
            Prefab = prefab;
            Point = point;
            ObjectiveIndex = objectiveIndex;
            EntityId = EntityTargetUtility.GetId(prefab);
        }
    }

    internal readonly List<Goal> Goals = new();
    internal readonly List<Spawn> Spawns = new();

    internal bool Matches(int objectiveIndex, Spawn spawn)
    {
        return (spawn.ObjectiveIndex < 0 || spawn.ObjectiveIndex == objectiveIndex) &&
               EntityTargetUtility.Matches(Goals[objectiveIndex].TargetId, spawn.EntityId);
    }

    internal string Build(
        TrialStageDefinition stage,
        IReadOnlyList<TrialSpawnPoint> markers,
        IReadOnlyList<Transform> legacyPoints)
    {
        string error = stage.Objectives.Length > 0
            ? BuildFromMarkers(stage, markers)
            : BuildFromLegacy(stage, legacyPoints);

        if (error != null)
            return error;

        if (Goals.Count == 0)
            return "Add at least one objective.";

        for (int index = 0; index < Goals.Count; index++)
        {
            Goal goal = Goals[index];
            int matchingSpawns = 0;

            foreach (Spawn spawn in Spawns)
            {
                if (Matches(index, spawn))
                    matchingSpawns++;
            }

            bool canRespawn =
                goal.Type == TrialObjectiveType.Kill && stage.RespawnDefeatedEnemies;

            int requiredSpawns = canRespawn ? 1 : goal.RequiredCount;

            if (matchingSpawns < requiredSpawns)
                return $"'{goal.Label}' needs at least {requiredSpawns} matching spawn(s).";
        }

        return null;
    }

   

    private string BuildFromLegacy(
        TrialStageDefinition stage,
        IReadOnlyList<Transform> legacyPoints)
    {
        foreach (KillObjectiveData objective in stage.KillObjectives)
        {
            if (objective == null || string.IsNullOrWhiteSpace(objective.TargetEntityId))
                return "A legacy kill objective is missing its target.";

            Goals.Add(new Goal(
                TrialObjectiveType.Kill,
                objective.DisplayName,
                objective.TargetEntityId,
                objective.RequiredKills));
        }

        int pointIndex = 0;

        foreach (TrialSpawnGroup group in stage.SpawnGroups)
        {
            if (group == null || group.Prefab == null)
                return "A legacy spawn group needs a prefab.";

            for (int i = 0; i < group.Count; i++)
            {
                if (pointIndex >= legacyPoints.Count)
                    return "There are too few legacy Spawn Points.";

                string error = AddSpawn(
                    group.Prefab.gameObject,
                    legacyPoints[pointIndex++],
                    -1,
                    TrialObjectiveType.Kill);

                if (error != null)
                    return error;
            }
        }

        return null;
    }

    private string BuildFromMarkers(
    TrialStageDefinition stage,
    IReadOnlyList<TrialSpawnPoint> markers)
    {
        HashSet<string> ids = new(StringComparer.Ordinal);

        foreach (TrialObjectiveDefinition objective in stage.Objectives)
        {
            if (objective == null || string.IsNullOrWhiteSpace(objective.Id))
                return "An objective is missing its ID.";

            if (!ids.Add(objective.Id))
                return "Two objectives have the same ID. Re-save the stage.";

            if (string.IsNullOrWhiteSpace(objective.TargetId))
                return $"'{objective.DisplayName}' needs a target or a Spawn Prefab with EntityIdentity.";

            int index = Goals.Count;

            Goals.Add(new Goal(
                objective.Type,
                objective.DisplayName,
                objective.TargetId,
                objective.RequiredCount,
                objective.Timing));

            foreach (TrialSpawnPoint marker in markers)
            {
                if (marker.Stage != stage || marker.ObjectiveId != objective.Id)
                    continue;

                GameObject prefab = marker.Prefab != null
                    ? marker.Prefab
                    : objective.SpawnPrefab;

                string error = AddSpawn(
                    prefab, marker.transform, index, objective.Type);

                if (error != null)
                    return error;

                if (!Matches(index, Spawns[Spawns.Count - 1]))
                    return $"'{marker.name}' does not match the target of '{objective.DisplayName}'.";
            }
        }

        return null;
    }

    private string AddSpawn(
    GameObject prefab,
    Transform point,
    int objectiveIndex,
    TrialObjectiveType type)
    {
        if (point == null || prefab == null)
            return $"'{point?.name ?? "Spawn"}' needs a Spawn Prefab.";

        if (prefab.scene.IsValid() ||
            prefab.transform.parent != null ||
            prefab.GetComponent<TrialSpawnPoint>() != null)
        {
            return $"'{point.name}' must reference a prefab root from Project.";
        }

        if (!prefab.activeSelf)
            return $"'{prefab.name}' must be active on the prefab root.";

        CharacterStats character = prefab.GetComponent<CharacterStats>();
        bool hasNpcBehavior = prefab.GetComponent<NPCBehavior>() != null;

        if (type == TrialObjectiveType.Interact)
        {
            if (character != null || hasNpcBehavior)
                return $"'{prefab.name}' should be an interaction prop.";

            if (prefab.GetComponentInChildren<InteractionTarget>(true) == null)
                return $"'{prefab.name}' needs an InteractionTarget.";

            foreach (MonoBehaviour component in prefab.GetComponents<MonoBehaviour>())
            {
                if (component is IInteractionOption &&
                    component is not WorldInteractable &&
                    component is not TrialObjectiveInteraction)
                {
                    return $"'{prefab.name}' has another interaction option. Use a dedicated Trial prop.";
                }
            }
        }
        else
        {
            if (character == null)
                return $"'{prefab.name}' needs CharacterStats.";

            if (type == TrialObjectiveType.Kill && !hasNpcBehavior)
                return $"'{prefab.name}' needs NPCBehavior.";

            if (type == TrialObjectiveType.Destroy && hasNpcBehavior)
                return $"'{prefab.name}' needs to be a destructible prop.";
        }

        Spawn spawn = new(prefab, point, objectiveIndex);

        if (string.IsNullOrWhiteSpace(spawn.EntityId))
            return $"'{prefab.name}' needs an EntityIdentity.";

        Spawns.Add(spawn);
        return null;
    }
}