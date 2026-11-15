using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PolygonCollider2D))]
public sealed class TrialZone : MonoBehaviour
{
    [SerializeField] private string trialTitle = "Trial - The Wolf Den";
    [SerializeField]
    private TrialStageDefinition[] stages =
        Array.Empty<TrialStageDefinition>();
    [SerializeField] private Transform[] spawnPoints = Array.Empty<Transform>();
    [SerializeField, Min(0f)] private float resetSeconds = 30f;

    private readonly List<CharacterStats> spawnedEnemies = new();
    private readonly Dictionary<CharacterStats, NPCBehavior> livingEnemies = new();
    private readonly Dictionary<PlayerStats, Score> scores = new();

    private PolygonCollider2D area;
    private int stageIndex = -1;
    private int[] killCounts = Array.Empty<int>();
    private float totalWeight;
    private double deadline;
    private bool stageComplete;
    private bool coolingDown;
    private bool victory;

    public string TrialTitle => trialTitle;
    public bool IsActive => stageIndex >= 0 && !coolingDown;
    public bool IsCoolingDown => coolingDown;
    public bool HasTimer => coolingDown ||
        (IsActive && stages[stageIndex].TimeLimitSeconds > 0f);
    public double RemainingSeconds =>
        HasTimer ? Math.Max(0d, deadline - Time.timeAsDouble) : 0d;

    public string StageLabel =>
        stageIndex >= 0 ? stages[stageIndex].StageLabel : string.Empty;

    public string ResetStatus =>
        victory ? "Trial complete. The den is quiet." : "Trial failed. The den is resetting.";

    public int ObjectiveCount => IsActive ? killCounts.Length : 0;

    public sealed class Score
    {
        internal Score(int number)
        {
            Name = $"P{number}";
        }

        public string Name { get; }
        public long Damage { get; internal set; }
        public float Tanking { get; internal set; }

        internal long StageDamage;
        internal float StageTanking;
        internal float FinishedContribution;
    }

    private void Reset()
    {
        GetComponent<PolygonCollider2D>().isTrigger = true;
    }

    private void Awake()
    {
        area = GetComponent<PolygonCollider2D>();

        if (!ValidateConfiguration())
            enabled = false;
    }

    private void OnEnable()
    {
        CharacterCombatEvents.CharacterDefeated += HandleDefeat;
        CharacterCombatEvents.DamageApplied += HandleDamage;
    }

    private void OnDisable()
    {
        CharacterCombatEvents.CharacterDefeated -= HandleDefeat;
        CharacterCombatEvents.DamageApplied -= HandleDamage;
        ClearEnemies();
        ResetRound();
    }

    public bool Contains(PlayerStats player)
    {
        return player != null &&
               area != null &&
               area.enabled &&
               area.OverlapPoint(player.transform.position);
    }

    public string GetObjectiveText(int index)
    {
        KillObjectiveData objective = stages[stageIndex].KillObjectives[index];
        return $"{objective.DisplayName}: {killCounts[index]}/{objective.RequiredKills}";
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryStartRound(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryStartRound(other);
    }

    private void TryStartRound(Collider2D other)
    {
        if (!isActiveAndEnabled || stageIndex >= 0)
            return;

        PlayerStats player = other.GetComponentInParent<PlayerStats>();

        if (Contains(player) && player.IsAlive)
            StartStage(0);
    }

    private void Update()
    {
        if (coolingDown)
        {
            if (Time.timeAsDouble >= deadline)
                ResetRound();

            return;
        }

        if (!IsActive)
            return;

        if (stageComplete)
        {
            FinishStage();

            if (stageIndex + 1 < stages.Length)
                StartStage(stageIndex + 1);
            else
                BeginCooldown(true);

            return;
        }

        if (!CanRecord())
        {
            FinishStage();
            BeginCooldown(false);
            return;
        }

        RecordTanking(Time.deltaTime);
    }

    private void StartStage(int index)
    {
        ClearEnemies();

        stageIndex = index;
        stageComplete = false;

        TrialStageDefinition stage = stages[index];
        killCounts = new int[stage.KillObjectives.Length];
        deadline = Time.timeAsDouble + stage.TimeLimitSeconds;

        int pointIndex = 0;

        foreach (TrialSpawnGroup group in stage.SpawnGroups)
        {
            for (int i = 0; i < group.Count; i++)
            {
                Transform point = spawnPoints[pointIndex++];
                CharacterStats enemy = Instantiate(
                    group.Prefab, point.position, point.rotation);

                spawnedEnemies.Add(enemy);
                livingEnemies.Add(enemy, enemy.GetComponent<NPCBehavior>());
            }
        }
    }

    private bool CanRecord()
    {
        return IsActive && !stageComplete &&
               (!HasTimer || Time.timeAsDouble < deadline);
    }

    private void HandleDefeat(CharacterDefeatedResult result)
    {
        if (!CanRecord() || result == null || result.Victim == null ||
            !livingEnemies.Remove(result.Victim))
        {
            return;
        }

        KillObjectiveData[] objectives = stages[stageIndex].KillObjectives;
        bool allComplete = true;

        for (int i = 0; i < objectives.Length; i++)
        {
            KillObjectiveData objective = objectives[i];

            if (killCounts[i] < objective.RequiredKills &&
                EntityTargetUtility.Matches(
                    objective.TargetEntityId, result.VictimEntityId))
            {
                killCounts[i]++;
            }

            allComplete &= killCounts[i] >= objective.RequiredKills;
        }

        // Update handles the transition after the combat event has finished.
        stageComplete = allComplete;
    }

    private void HandleDamage(DamageAppliedEvent damage)
    {
        if (!CanRecord() || damage.AppliedDamage <= 0 ||
            damage.Victim == null || !livingEnemies.ContainsKey(damage.Victim) ||
            damage.Source.CreditOwner is not PlayerStats player ||
            !Contains(player))
        {
            return;
        }

        Score score = GetOrCreateScore(player);
        score.Damage += damage.AppliedDamage;
        score.StageDamage += damage.AppliedDamage;
    }

    private void RecordTanking(float seconds)
    {
        if (seconds <= 0f)
            return;

        foreach (NPCBehavior enemy in livingEnemies.Values)
        {
            if (enemy == null || !enemy.IsInCombat ||
                enemy.CurrentTarget is not PlayerStats player ||
                !Contains(player) || !player.IsAlive)
            {
                continue;
            }

            Score score = GetOrCreateScore(player);
            score.Tanking += seconds;
            score.StageTanking += seconds;
        }
    }

    private Score GetOrCreateScore(PlayerStats player)
    {
        if (!scores.TryGetValue(player, out Score score))
        {
            score = new Score(scores.Count + 1);
            scores.Add(player, score);
        }

        return score;
    }

    public float GetContribution(Score score)
    {
        float current = IsActive ? GetStageContribution(score) : 0f;
        return Mathf.Clamp01(score.FinishedContribution + current);
    }

    public void CopyScoresTo(List<Score> destination)
    {
        destination.Clear();
        destination.AddRange(scores.Values);

        destination.Sort((left, right) =>
        {
            int byContribution =
                GetContribution(right).CompareTo(GetContribution(left));

            return byContribution != 0
                ? byContribution
                : string.CompareOrdinal(left.Name, right.Name);
        });
    }

    private float GetStageContribution(Score score)
    {
        TrialStageDefinition stage = stages[stageIndex];

        float fraction = Mathf.Clamp01(
            score.StageDamage / stage.DamageBenchmark +
            score.StageTanking / stage.TankingBenchmark);

        return fraction * (stage.Weight / totalWeight);
    }

    private void FinishStage()
    {
        foreach (Score score in scores.Values)
        {
            score.FinishedContribution += GetStageContribution(score);
            score.StageDamage = 0;
            score.StageTanking = 0f;
        }
    }

    private void BeginCooldown(bool completed)
    {
        victory = completed;
        coolingDown = true;
        deadline = Time.timeAsDouble + resetSeconds;
        ClearEnemies();
    }

    private void ResetRound()
    {
        scores.Clear();
        stageIndex = -1;
        killCounts = Array.Empty<int>();
        stageComplete = false;
        coolingDown = false;
        victory = false;
        deadline = 0d;
    }

    private void ClearEnemies()
    {
        livingEnemies.Clear();

        foreach (CharacterStats enemy in spawnedEnemies)
        {
            if (enemy == null)
                continue;

            enemy.gameObject.SetActive(false);
            Destroy(enemy.gameObject);
        }

        spawnedEnemies.Clear();
    }

    private bool ValidateConfiguration()
    {
        if (!area.isTrigger || stages.Length == 0)
            return ConfigurationError("Set Is Trigger and assign at least one stage.");

        totalWeight = 0f;

        foreach (TrialStageDefinition stage in stages)
        {
            if (stage == null || stage.KillObjectives.Length == 0)
                return ConfigurationError("Each stage needs at least one kill objective.");

            totalWeight += stage.Weight;

            Dictionary<string, int> countsByEntity = new();
            int spawnCount = 0;

            foreach (TrialSpawnGroup group in stage.SpawnGroups)
            {
                if (group == null || group.Prefab == null ||
                    group.Prefab.GetComponent<NPCBehavior>() == null)
                {
                    return ConfigurationError(
                        $"{stage.name} needs enemy prefabs with CharacterStats and NPCBehavior on the root.");
                }

                string id = EntityTargetUtility.GetId(group.Prefab.gameObject);

                if (string.IsNullOrWhiteSpace(id))
                    return ConfigurationError($"{group.Prefab.name} needs an EntityIdentity.");

                countsByEntity.TryGetValue(id, out int existing);
                countsByEntity[id] = existing + group.Count;
                spawnCount += group.Count;
            }

            if (spawnCount > spawnPoints.Length)
                return ConfigurationError($"{stage.name} needs {spawnCount} spawn points.");

            for (int i = 0; i < spawnCount; i++)
            {
                if (spawnPoints[i] == null)
                    return ConfigurationError($"Spawn Point {i + 1} is empty.");
            }

            foreach (KillObjectiveData objective in stage.KillObjectives)
            {
                if (objective == null ||
                    !countsByEntity.TryGetValue(objective.TargetEntityId, out int count) ||
                    count < objective.RequiredKills)
                {
                    return ConfigurationError(
                        $"{stage.name} needs enough matching spawns for every kill objective.");
                }
            }
        }

        return true;
    }

    private bool ConfigurationError(string message)
    {
        Debug.LogError($"Trial Zone '{name}': {message}", this);
        return false;
    }
}
