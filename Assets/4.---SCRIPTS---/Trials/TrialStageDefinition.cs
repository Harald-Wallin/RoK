using System;
using UnityEngine;

[Serializable]
public sealed class TrialSpawnGroup
{
    [SerializeField] private CharacterStats prefab;
    [SerializeField, Min(1)] private int count = 1;

    public CharacterStats Prefab => prefab;
    public int Count => Mathf.Max(1, count);
}

[CreateAssetMenu(
    fileName = "New Trial Stage",
    menuName = "RPG/Trials/Stage")]
public sealed class TrialStageDefinition : ScriptableObject
{
    [SerializeField] private string stageLabel = "I";

    [Tooltip("Seconds. Zero means the stage has no time limit.")]
    [SerializeField, Min(0f)] private float timeLimitSeconds;

    [SerializeField]
    private KillObjectiveData[] killObjectives =
        Array.Empty<KillObjectiveData>();

    [SerializeField]
    private TrialSpawnGroup[] spawnGroups =
        Array.Empty<TrialSpawnGroup>();

    [Header("Contribution")]
    [SerializeField, Min(0.01f)] private float weight = 15f;
    [SerializeField, Min(1)] private int damageBenchmark = 100;
    [SerializeField, Min(1)] private int tankingBenchmark = 30;

    public string StageLabel => stageLabel;
    public float TimeLimitSeconds => timeLimitSeconds;
    public KillObjectiveData[] KillObjectives => killObjectives;
    public TrialSpawnGroup[] SpawnGroups => spawnGroups;

    public float Weight => Mathf.Max(0.01f, weight);
    public float DamageBenchmark => Mathf.Max(1, damageBenchmark);
    public float TankingBenchmark => Mathf.Max(1, tankingBenchmark);
}
