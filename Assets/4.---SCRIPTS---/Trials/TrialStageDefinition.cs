using System;
using System.Collections.Generic;
using UnityEngine;

public enum TrialObjectiveType
{
    Kill,
    Destroy
}

[Serializable]
public sealed class TrialObjectiveDefinition
{
    [SerializeField, HideInInspector] private string id;
    [SerializeField] private TrialObjectiveType type;
    [SerializeField] private string displayName;
    [SerializeField] private EntityReference target = new();
    [SerializeField, Min(1)] private int requiredCount = 1;

    public string Id => id;
    public TrialObjectiveType Type => type;
    public string TargetId => target?.Id ?? string.Empty;
    public int RequiredCount => Mathf.Max(1, requiredCount);

    public string DisplayName => !string.IsNullOrWhiteSpace(displayName)
        ? displayName
        : $"{type} {target?.DisplayName}";

#if UNITY_EDITOR
    internal void EnsureId(HashSet<string> usedIds)
    {
        if (!string.IsNullOrWhiteSpace(id) && usedIds.Add(id))
            return;

        id = Guid.NewGuid().ToString("N");
        usedIds.Add(id);
    }
#endif
}

[Serializable]
public sealed class TrialSpawnGroup
{
    [SerializeField] private CharacterStats prefab;
    [SerializeField, Min(1)] private int count = 1;

    public CharacterStats Prefab => prefab;
    public int Count => Mathf.Max(1, count);
}

[CreateAssetMenu(fileName = "New Trial Stage", menuName = "RPG/Trials/Stage")]
public sealed class TrialStageDefinition : ScriptableObject
{
    [SerializeField] private string stageLabel = "I";

    [Tooltip("Seconds. Zero means the stage has no time limit.")]
    [SerializeField, Min(0f)] private float timeLimitSeconds;

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
    [SerializeField] private bool respawnDefeatedEnemies;
    [SerializeField, Min(0f)] private float respawnDelaySeconds = 10f;

    [Header("Contribution")]
    [SerializeField, Min(0.01f)] private float weight = 15f;
    [SerializeField, Min(1)] private int damageBenchmark = 100;
    [SerializeField, Min(1)] private int healingBenchmark = 50;
    [SerializeField, Min(1)] private int tankingBenchmark = 30;

    public string StageLabel => stageLabel;
    public float TimeLimitSeconds => timeLimitSeconds;
    public TrialObjectiveDefinition[] Objectives => objectives;
    public KillObjectiveData[] KillObjectives => killObjectives;
    public TrialSpawnGroup[] SpawnGroups => spawnGroups;

    public bool RespawnDefeatedEnemies => respawnDefeatedEnemies;
    public float RespawnDelaySeconds => Mathf.Max(0f, respawnDelaySeconds);

    public float Weight => Mathf.Max(0.01f, weight);
    public float DamageBenchmark => Mathf.Max(1, damageBenchmark);
    public float HealingBenchmark => Mathf.Max(1, healingBenchmark);
    public float TankingBenchmark => Mathf.Max(1, tankingBenchmark);

#if UNITY_EDITOR
    private void OnValidate()
    {
        HashSet<string> usedIds = new();

        foreach (TrialObjectiveDefinition objective in objectives)
            objective?.EnsureId(usedIds);
    }
#endif
}