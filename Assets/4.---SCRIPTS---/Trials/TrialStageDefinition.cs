using System;
using System.Collections.Generic;
using UnityEngine;

public enum TrialObjectiveType
{
    Kill = 0,
    Destroy = 1,
    Interact = 2
}

[Serializable]
public sealed class TrialObjectiveDefinition
{
    [SerializeField, HideInInspector]
    private string id;

    [SerializeField]
    private TrialObjectiveType type;

    [SerializeField]
    private string displayName;

    [Tooltip(
        "Default prefab used when adding spawn points for this objective.")]
    [SerializeField]
    private GameObject spawnPrefab;

    [Tooltip(
        "Optional target override. If empty, " +
        "the Spawn Prefab's EntityIdentity is used.")]
    [SerializeField]
    private EntityReference target = new();

    [SerializeField, Min(1)]
    private int requiredCount = 1;

    [SerializeField]
    private InteractionTimingSettings timing = new();

    public string Id => id;
    public TrialObjectiveType Type => type;
    public GameObject SpawnPrefab => spawnPrefab;

    public int RequiredCount =>
        Mathf.Max(1, requiredCount);

    public InteractionTimingSettings Timing =>
        timing;

    public string TargetId =>
        target != null && target.IsValid
            ? target.Id
            : EntityTargetUtility.GetId(spawnPrefab);

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            string targetName =
                target != null && target.IsValid
                    ? target.DisplayName
                    : EntityTargetUtility.GetDisplayName(spawnPrefab);

            return $"{type} {targetName}";
        }
    }

#if UNITY_EDITOR
    internal void EnsureId(
        HashSet<string> usedIds)
    {
        if (!string.IsNullOrWhiteSpace(id) &&
            usedIds.Add(id))
        {
            return;
        }

        id = Guid.NewGuid().ToString("N");
        usedIds.Add(id);
    }
#endif
}

[Serializable]
public sealed class TrialSpawnGroup
{
    [SerializeField]
    private CharacterStats prefab;

    [SerializeField, Min(1)]
    private int count = 1;

    public CharacterStats Prefab => prefab;
    public int Count => Mathf.Max(1, count);
}

[CreateAssetMenu(
    fileName = "New Trial Stage",
    menuName = "RPG/Trials/Stage")]
public sealed class TrialStageDefinition : ScriptableObject
{
    [SerializeField]
    private string stageLabel = "I";

    [Tooltip(
        "Seconds. Zero means the stage has no time limit.")]
    [SerializeField, Min(0f)]
    private float timeLimitSeconds;

    [SerializeField]
    private TrialObjectiveDefinition[] objectives =
        Array.Empty<TrialObjectiveDefinition>();

    [Header("Legacy Setup")]

    [SerializeField]
    private KillObjectiveData[] killObjectives =
        Array.Empty<KillObjectiveData>();

    [SerializeField]
    private TrialSpawnGroup[] spawnGroups =
        Array.Empty<TrialSpawnGroup>();

    [Header("Respawn")]

    [SerializeField]
    private bool respawnDefeatedEnemies;

    [SerializeField, Min(0f)]
    private float respawnDelaySeconds = 10f;

    public string StageLabel => stageLabel;
    public float TimeLimitSeconds => timeLimitSeconds;

    public TrialObjectiveDefinition[] Objectives =>
        objectives;

    public KillObjectiveData[] KillObjectives =>
        killObjectives;

    public TrialSpawnGroup[] SpawnGroups =>
        spawnGroups;

    public bool RespawnDefeatedEnemies =>
        respawnDefeatedEnemies;

    public float RespawnDelaySeconds =>
        Mathf.Max(0f, respawnDelaySeconds);

#if UNITY_EDITOR
    private void OnValidate()
    {
        HashSet<string> usedIds = new();

        foreach (TrialObjectiveDefinition objective in objectives)
            objective?.EnsureId(usedIds);
    }
#endif
}