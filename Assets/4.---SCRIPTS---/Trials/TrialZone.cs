using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PolygonCollider2D))]
public sealed class TrialZone : MonoBehaviour
{
    private const double ActivitySeconds = 15d;

    private enum TrialDifficulty
    {
        [InspectorName("1")] OneSkull = 1,
        [InspectorName("2")] TwoSkulls = 2,
        [InspectorName("3")] ThreeSkulls = 3
    }

    [SerializeField] private TrialDifficulty difficulty = TrialDifficulty.OneSkull;
    [SerializeField] private string trialTitle = "The Wolf Den";
    [SerializeField]
    private TrialStageDefinition[] stages =
        Array.Empty<TrialStageDefinition>();
    [SerializeField] private Transform[] spawnPoints = Array.Empty<Transform>();
    [SerializeField, Min(0f)] private float resetSeconds = 30f;

    [Header("Entry Announcement")]
    [SerializeField, Min(0f)] private float entryAnnouncementCooldown = 20f;

    [Header("Rewards")]
    [SerializeField] private TrialRewards rewards = new();

    private sealed class SpawnSlot
    {
        public TrialStagePlan.Spawn Placement;
        public GameObject Instance;
        public CharacterStats Entity;
        public NPCBehavior Behavior;
        public double RespawnAt = double.PositiveInfinity;
    }

    public sealed class Score
    {
        internal Score(int number)
        {
            Name = $"P{number}";
        }

        public string Name { get; }
        public long Damage { get; internal set; }
        public long Healing { get; internal set; }
        public float Tanking { get; internal set; }
        public long Objectives { get; internal set; }

        internal long StageDamage;
        internal long StageHealing;
        internal float StageTanking;
        internal long StageObjectives;
        internal float FinishedContribution;
        internal double ActiveUntil;
    }

    private readonly List<SpawnSlot> spawnSlots = new();
    private readonly Dictionary<CharacterStats, SpawnSlot> livingSpawns = new();
    private readonly Dictionary<GameObject, SpawnSlot> interactionSpawns = new();
    private readonly Dictionary<PlayerStats, Score> scores = new();
    private readonly Dictionary<PlayerStats, int> trialWounds = new();

    private TrialStagePlan[] stagePlans;
    private PolygonCollider2D area;
    private int stageIndex = -1;
    private int[] objectiveCounts = Array.Empty<int>();
    private float totalWeight;
    private double deadline;
    private bool stageComplete;
    private bool coolingDown;
    private bool victory;

    private bool playerWasInside;
    private double nextEntryAnnouncement;

    private TrialStageDefinition CurrentStage => stages[stageIndex];
    private TrialStagePlan CurrentPlan => stagePlans[stageIndex];

    public IReadOnlyList<TrialStageDefinition> Stages => stages;
    public int DifficultySkullCount => Mathf.Clamp((int)difficulty, 1, 3);

    public string TrialName
    {
        get
        {
            string name = trialTitle?.Trim() ?? string.Empty;
            const string prefix = "Trial - ";

            return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? name.Substring(prefix.Length).Trim()
                : name;
        }
    }

    public string TrialTitle => $"Trial - {TrialName}";
    public bool IsActive => stageIndex >= 0 && !coolingDown;
    public bool IsCoolingDown => coolingDown;
    public bool HasTimer => coolingDown ||
        (IsActive && CurrentStage.TimeLimitSeconds > 0f);

    public double RemainingSeconds =>
        HasTimer ? Math.Max(0d, deadline - Time.timeAsDouble) : 0d;

    public string StageLabel =>
        stageIndex >= 0 ? CurrentStage.StageLabel : string.Empty;

    public string ResetStatus => victory
        ? "Trial complete. The area is quiet."
        : "Trial failed. The area is resetting.";

    public int ObjectiveCount => IsActive ? objectiveCounts.Length : 0;

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
        playerWasInside = false;
        CharacterCombatEvents.CharacterDefeated += HandleDefeat;
        CharacterCombatEvents.DamageApplied += HandleDamage;
        CharacterCombatEvents.HealingApplied += HandleHealing;
    }

    private void OnDisable()
    {
        CharacterCombatEvents.CharacterDefeated -= HandleDefeat;
        CharacterCombatEvents.DamageApplied -= HandleDamage;
        CharacterCombatEvents.HealingApplied -= HandleHealing;
        ClearStage();
        ResetRound();
    }

    public bool Contains(PlayerStats player)
    {
        return player != null && area != null && area.enabled &&
               area.OverlapPoint(player.transform.position);
    }

    public string GetObjectiveText(int index)
    {
        TrialStagePlan.Goal goal = CurrentPlan.Goals[index];
        return $"{goal.Label}: {objectiveCounts[index]}/{goal.RequiredCount}";
    }

    private void LateUpdate()
    {
        bool inside = Contains(PlayerReference.Player);

        if (inside && !playerWasInside &&
            Time.unscaledTimeAsDouble >= nextEntryAnnouncement &&
            AnnouncementSpawner.Instance != null)
        {
            AnnouncementSpawner.Instance.ShowTrialEntered(TrialName);
            nextEntryAnnouncement =
                Time.unscaledTimeAsDouble + entryAnnouncementCooldown;
        }

        playerWasInside = inside;
    }

    private void Update()
    {
        if (coolingDown)
        {
            if (Time.timeAsDouble < deadline)
                return;

            ResetRound();
        }

        if (stageIndex < 0)
        {
            StartStage(0);
            return;
        }

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
        ProcessRespawns();
    }

    private void StartStage(int index)
    {
        ClearStage();
        stageIndex = index;
        stageComplete = false;
        objectiveCounts = new int[CurrentPlan.Goals.Count];
        deadline = Time.timeAsDouble + CurrentStage.TimeLimitSeconds;

        foreach (TrialStagePlan.Spawn placement in CurrentPlan.Spawns)
        {
            SpawnSlot slot = new() { Placement = placement };
            spawnSlots.Add(slot);
            SpawnEntity(slot, isRespawn: false);
        }
    }

    private void SpawnEntity(SpawnSlot slot, bool isRespawn)
    {
        Transform point = slot.Placement.Point;
        GameObject instance = Instantiate(
            slot.Placement.Prefab, point.position, point.rotation);

        slot.Instance = instance;
        slot.Entity = instance.GetComponent<CharacterStats>();
        slot.Behavior = instance.GetComponent<NPCBehavior>();
        slot.RespawnAt = double.PositiveInfinity;

        int objectiveIndex = slot.Placement.ObjectiveIndex;

        if (objectiveIndex >= 0 &&
            CurrentPlan.Goals[objectiveIndex].Type == TrialObjectiveType.Interact)
        {
            TrialObjectiveInteraction interaction =
                instance.GetComponent<TrialObjectiveInteraction>();

            if (interaction == null)
                interaction = instance.AddComponent<TrialObjectiveInteraction>();

            TrialStagePlan.Goal goal = CurrentPlan.Goals[objectiveIndex];
            interaction.Initialize(this, goal.Label, goal.Timing);
            interactionSpawns.Add(instance, slot);
            return;
        }

        if (slot.Behavior != null)
        {
            slot.Behavior.DisableAutomaticRespawn();

            if (isRespawn)
                slot.Behavior.PrepareForRespawn();
        }

        livingSpawns.Add(slot.Entity, slot);
    }

    internal bool CanUseInteraction(GameObject instance, PlayerStats player)
    {
        if (!CanRecord() || player == null || !player.IsAlive ||
            !Contains(player) || instance == null || !instance.activeInHierarchy ||
            !interactionSpawns.TryGetValue(instance, out SpawnSlot slot))
        {
            return false;
        }

        int index = slot.Placement.ObjectiveIndex;
        return objectiveCounts[index] < CurrentPlan.Goals[index].RequiredCount;
    }

    internal void CompleteInteraction(GameObject instance, PlayerStats player)
    {
        if (!CanUseInteraction(instance, player))
            return;

        SpawnSlot slot = interactionSpawns[instance];
        interactionSpawns.Remove(instance);

        int index = slot.Placement.ObjectiveIndex;
        objectiveCounts[index]++;

        Score score = GetOrCreateScore(player);
        score.Objectives++;
        score.StageObjectives++;
        score.ActiveUntil = Time.timeAsDouble + ActivitySeconds;

        stageComplete = AreObjectivesComplete();

        instance.SetActive(false);
        Destroy(instance);
    }

    private void ProcessRespawns()
    {
        if (!CurrentStage.RespawnDefeatedEnemies)
            return;

        double now = Time.timeAsDouble;

        foreach (SpawnSlot slot in spawnSlots)
        {
            if (now < slot.RespawnAt)
                continue;

            slot.RespawnAt = double.PositiveInfinity;

            if (NeedsRespawn(slot))
                SpawnEntity(slot, isRespawn: true);
        }
    }

    private bool NeedsRespawn(SpawnSlot slot)
    {
        for (int index = 0; index < CurrentPlan.Goals.Count; index++)
        {
            TrialStagePlan.Goal goal = CurrentPlan.Goals[index];

            if (goal.Type == TrialObjectiveType.Kill &&
                objectiveCounts[index] < goal.RequiredCount &&
                CurrentPlan.Matches(index, slot.Placement))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanRecord()
    {
        return IsActive && !stageComplete &&
               (!HasTimer || Time.timeAsDouble < deadline);
    }

    private bool AreObjectivesComplete()
    {
        for (int index = 0; index < CurrentPlan.Goals.Count; index++)
        {
            if (objectiveCounts[index] < CurrentPlan.Goals[index].RequiredCount)
                return false;
        }

        return true;
    }

    private void HandleDefeat(CharacterDefeatedResult result)
    {
        if (!CanRecord() || result == null || result.Victim == null ||
            !livingSpawns.Remove(result.Victim, out SpawnSlot slot))
        {
            return;
        }

        for (int index = 0; index < CurrentPlan.Goals.Count; index++)
        {
            TrialStagePlan.Goal goal = CurrentPlan.Goals[index];

            if (goal.Type != TrialObjectiveType.Interact &&
                objectiveCounts[index] < goal.RequiredCount &&
                CurrentPlan.Matches(index, slot.Placement) &&
                EntityTargetUtility.Matches(goal.TargetId, result.VictimEntityId))
            {
                objectiveCounts[index]++;
            }
        }

        stageComplete = AreObjectivesComplete();

        if (!stageComplete && CurrentStage.RespawnDefeatedEnemies && NeedsRespawn(slot))
            slot.RespawnAt = Time.timeAsDouble + CurrentStage.RespawnDelaySeconds;
    }

    private void HandleDamage(DamageAppliedEvent damage)
    {
        if (!CanRecord() || damage.AppliedDamage <= 0 || damage.Victim == null)
            return;

        CharacterStats attacker = damage.Source.DirectSource;

        if (damage.Victim is PlayerStats wounded && Contains(wounded) &&
            attacker != null &&
            livingSpawns.TryGetValue(attacker, out SpawnSlot sourceSlot) &&
            sourceSlot.Behavior != null)
        {
            TrackWound(wounded, damage.AppliedDamage);
        }

        if (livingSpawns.ContainsKey(damage.Victim) &&
            damage.Source.CreditOwner is PlayerStats player && Contains(player))
        {
            Score score = GetOrCreateScore(player);
            score.Damage += damage.AppliedDamage;
            score.StageDamage += damage.AppliedDamage;
            score.ActiveUntil = Time.timeAsDouble + ActivitySeconds;
        }
    }

    private void TrackWound(PlayerStats target, int amount)
    {
        if (!trialWounds.TryGetValue(target, out int existing))
            target.OnDied += ForgetWounds;

        int missingHealth = Mathf.Max(0, target.GetMaxHP() - target.currentHP);
        trialWounds[target] = (int)Math.Min((long)existing + amount, missingHealth);
    }

    private void HandleHealing(
        CharacterStats healer, CharacterStats target, HealingResult result)
    {
        if (target is not PlayerStats wounded ||
            !trialWounds.TryGetValue(wounded, out int outstanding))
        {
            return;
        }

        long missingBeforeHeal =
            (long)wounded.GetMaxHP() - wounded.currentHP + result.AppliedAmount;

        outstanding = (int)Math.Min(outstanding, Math.Max(0L, missingBeforeHeal));
        int restoredTrialHealth = Math.Min(outstanding, result.AppliedAmount);
        int remaining = outstanding - restoredTrialHealth;

        if (remaining == 0)
            ForgetWounds(wounded);
        else
            trialWounds[wounded] = remaining;

        if (restoredTrialHealth <= 0 || !CanRecord() ||
            healer is not PlayerStats player || !Contains(player) || !Contains(wounded))
        {
            return;
        }

        Score score = GetOrCreateScore(player);
        score.Healing += restoredTrialHealth;
        score.StageHealing += restoredTrialHealth;
        score.ActiveUntil = Time.timeAsDouble + ActivitySeconds;
    }

    private void ForgetWounds(CharacterStats target)
    {
        if (target is PlayerStats player && trialWounds.Remove(player))
            player.OnDied -= ForgetWounds;
    }

    private void RecordTanking(float seconds)
    {
        if (seconds <= 0f)
            return;

        foreach (SpawnSlot slot in livingSpawns.Values)
        {
            NPCBehavior enemy = slot.Behavior;

            if (enemy == null || !enemy.IsInCombat ||
                enemy.CurrentTarget is not PlayerStats player ||
                !Contains(player) || !player.IsAlive ||
                !scores.TryGetValue(player, out Score score) ||
                Time.timeAsDouble >= score.ActiveUntil)
            {
                continue;
            }

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
        TrialStageDefinition stage = CurrentStage;

        float fraction = Mathf.Clamp01(
            score.StageDamage / stage.DamageBenchmark +
            score.StageHealing / stage.HealingBenchmark +
            score.StageTanking / stage.TankingBenchmark +
            score.StageObjectives / stage.ObjectiveBenchmark);

        return fraction * (stage.Weight / totalWeight);
    }

    private void FinishStage()
    {
        foreach (Score score in scores.Values)
        {
            score.FinishedContribution += GetStageContribution(score);
            score.StageDamage = 0;
            score.StageHealing = 0;
            score.StageTanking = 0f;
            score.StageObjectives = 0;
        }
    }

    private void BeginCooldown(bool completed)
    {
        if (coolingDown)
            return;

        victory = completed;
        coolingDown = true;
        deadline = Time.timeAsDouble + resetSeconds;

        List<(PlayerStats Player, float Contribution)> participants = new();

        foreach (var entry in scores)
        {
            if (entry.Key != null)
                participants.Add((entry.Key, GetContribution(entry.Value)));
        }

        ClearStage();
        rewards.Settle(completed, participants);
    }

    private void ResetRound()
    {
        rewards.ResetRound();
        scores.Clear();
        stageIndex = -1;
        objectiveCounts = Array.Empty<int>();
        stageComplete = false;
        coolingDown = false;
        victory = false;
        deadline = 0d;
    }

    public string GetRewardSummary(PlayerStats player)
    {
        return rewards.Describe(player);
    }

    private void ClearStage()
    {
        livingSpawns.Clear();
        interactionSpawns.Clear();

        foreach (SpawnSlot slot in spawnSlots)
        {
            if (slot.Instance == null)
                continue;

            slot.Instance.SetActive(false);
            Destroy(slot.Instance);
        }

        spawnSlots.Clear();

        foreach (PlayerStats player in trialWounds.Keys)
        {
            if (player != null)
                player.OnDied -= ForgetWounds;
        }

        trialWounds.Clear();
    }

    private bool ValidateConfiguration()
    {
        if (!area.isTrigger || stages.Length == 0)
            return ConfigurationError("Set Is Trigger and assign at least one stage.");

        List<TrialSpawnPoint> markers = new();

        foreach (TrialSpawnPoint marker in GetComponentsInChildren<TrialSpawnPoint>(true))
        {
            if (marker.Zone == this)
                markers.Add(marker);
        }

        stagePlans = new TrialStagePlan[stages.Length];
        totalWeight = 0f;

        for (int index = 0; index < stages.Length; index++)
        {
            TrialStageDefinition stage = stages[index];

            if (stage == null)
                return ConfigurationError($"Stage {index + 1} is empty.");

            TrialStagePlan plan = new();
            string error = plan.Build(stage, markers, spawnPoints);

            if (error != null)
                return ConfigurationError($"{stage.name}: {error}");

            stagePlans[index] = plan;
            totalWeight += stage.Weight;
        }

        return true;
    }

    private bool ConfigurationError(string message)
    {
        Debug.LogError($"Trial Zone '{name}': {message}", this);
        return false;
    }
}