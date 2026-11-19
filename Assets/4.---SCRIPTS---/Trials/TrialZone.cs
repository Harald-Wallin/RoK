using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PolygonCollider2D))]
public sealed class TrialZone : MonoBehaviour
{
    [SerializeField]
    private TrialContributionSettings contributionSettings;

    private TrialContributionSettings rules;

    private enum TrialDifficulty
    {
        [InspectorName("1")] OneSkull = 1,
        [InspectorName("2")] TwoSkulls = 2,
        [InspectorName("3")] ThreeSkulls = 3
    }

    [SerializeField]
    private TrialDifficulty difficulty =
        TrialDifficulty.OneSkull;

    [SerializeField]
    private string trialTitle = "The Wolf Den";

    [SerializeField]
    private TrialStageDefinition[] stages =
        Array.Empty<TrialStageDefinition>();

    [SerializeField]
    private Transform[] spawnPoints =
        Array.Empty<Transform>();

    [SerializeField, Min(0f)]
    private float resetSeconds = 30f;

    [Header("Entry Announcement")]

    [SerializeField, Min(0f)]
    private float entryAnnouncementCooldown = 20f;

    [Header("Rewards")]

    [SerializeField]
    private TrialRewards rewards = new();

    private sealed class SpawnSlot
    {
        public TrialStagePlan.Spawn Placement;
        public GameObject Instance;
        public CharacterStats Entity;
        public NPCBehavior Behavior;

        public double RespawnAt =
            double.PositiveInfinity;

        public long TotalDamage;

        public readonly Dictionary<PlayerStats, long>
            PlayerDamage = new();
    }

    public sealed class Score
    {
        internal Score(int number)
        {
            Name = $"P{number}";
            Number = number;
            TieBreaker = UnityEngine.Random.value;
        }

        public string Name { get; }

        public long Damage { get; internal set; }
        public long Healing { get; internal set; }
        public float Tanking { get; internal set; }
        public long Objectives { get; internal set; }

        public double DamagePoints { get; internal set; }
        public double HealingPoints { get; internal set; }
        public double TankingPoints { get; internal set; }
        public double SupportPoints { get; internal set; }
        public double ObjectivePoints { get; internal set; }

        public double TotalPoints =>
            DamagePoints +
            HealingPoints +
            TankingPoints +
            SupportPoints +
            ObjectivePoints;

        internal readonly int Number;
        internal readonly float TieBreaker;
        internal double ActiveUntil;
        internal CharacterActionController Actions;
    }

    private readonly List<SpawnSlot> spawnSlots = new();

    private readonly Dictionary<CharacterStats, SpawnSlot>
        livingSpawns = new();

    private readonly Dictionary<GameObject, SpawnSlot>
        interactionSpawns = new();

    private readonly Dictionary<PlayerStats, Score>
        scores = new();

    private readonly Dictionary<PlayerStats, int>
        trialWounds = new();

    private TrialStagePlan[] stagePlans;
    private PolygonCollider2D area;

    private int stageIndex = -1;
    private int[] objectiveCounts = Array.Empty<int>();

    private double deadline;
    private bool stageComplete;
    private bool coolingDown;
    private bool victory;

    private bool playerWasInside;
    private double nextEntryAnnouncement;

    private TrialStageDefinition CurrentStage =>
        stages[stageIndex];

    private TrialStagePlan CurrentPlan =>
        stagePlans[stageIndex];

    public IReadOnlyList<TrialStageDefinition> Stages =>
        stages;

    public int DifficultySkullCount =>
        Mathf.Clamp((int)difficulty, 1, 3);

    public string TrialName
    {
        get
        {
            string name =
                trialTitle?.Trim() ?? string.Empty;

            const string prefix = "Trial - ";

            return name.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase)
                ? name.Substring(prefix.Length).Trim()
                : name;
        }
    }

    public string TrialTitle =>
        $"Trial - {TrialName}";

    public bool IsActive =>
        stageIndex >= 0 && !coolingDown;

    public bool IsCoolingDown =>
        coolingDown;

    public bool HasTimer =>
        coolingDown ||
        (IsActive && CurrentStage.TimeLimitSeconds > 0f);

    public double RemainingSeconds =>
        HasTimer
            ? Math.Max(0d, deadline - Time.timeAsDouble)
            : 0d;

    public string StageLabel =>
        stageIndex >= 0
            ? CurrentStage.StageLabel
            : string.Empty;

    public string ResetStatus => victory
        ? "Trial complete. The area is quiet."
        : "Trial failed. The area is resetting.";

    public int ObjectiveCount =>
        IsActive ? objectiveCounts.Length : 0;

    private void Reset()
    {
        GetComponent<PolygonCollider2D>().isTrigger = true;
    }

    private void Awake()
    {
        area = GetComponent<PolygonCollider2D>();

        rules = contributionSettings != null
            ? contributionSettings
            : Resources.Load<TrialContributionSettings>(
                "Trials/TrialContributionSettings");

        if (rules == null)
        {
            Debug.LogError(
                "Create Resources/Trials/TrialContributionSettings, " +
                "or assign Contribution Settings.",
                this);

            enabled = false;
            return;
        }

        if (!ValidateConfiguration())
            enabled = false;
    }

    private void OnEnable()
    {
        playerWasInside = false;

        CharacterCombatEvents.CharacterDefeated += HandleDefeat;
        CharacterCombatEvents.DamageApplied += HandleDamage;
        CharacterCombatEvents.HealingApplied += HandleHealing;

        BuffApplicationEvents.Applied += HandleSupport;
    }

    private void OnDisable()
    {
        CharacterCombatEvents.CharacterDefeated -= HandleDefeat;
        CharacterCombatEvents.DamageApplied -= HandleDamage;
        CharacterCombatEvents.HealingApplied -= HandleHealing;

        BuffApplicationEvents.Applied -= HandleSupport;

        ClearStage();
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
        TrialStagePlan.Goal goal =
            CurrentPlan.Goals[index];

        return $"{goal.Label}: " +
               $"{objectiveCounts[index]}/{goal.RequiredCount}";
    }

    private void LateUpdate()
    {
        bool inside = Contains(PlayerReference.Player);

        if (inside &&
            !playerWasInside &&
            Time.unscaledTimeAsDouble >= nextEntryAnnouncement &&
            AnnouncementSpawner.Instance != null)
        {
            AnnouncementSpawner.Instance.ShowTrialEntered(
                TrialName);

            nextEntryAnnouncement =
                Time.unscaledTimeAsDouble +
                entryAnnouncementCooldown;
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
            if (stageIndex + 1 < stages.Length)
                StartStage(stageIndex + 1);
            else
                BeginCooldown(true);

            return;
        }

        if (!CanRecord())
        {
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

        objectiveCounts =
            new int[CurrentPlan.Goals.Count];

        deadline =
            Time.timeAsDouble +
            CurrentStage.TimeLimitSeconds;

        foreach (TrialStagePlan.Spawn placement in CurrentPlan.Spawns)
        {
            SpawnSlot slot = new()
            {
                Placement = placement
            };

            spawnSlots.Add(slot);
            SpawnEntity(slot, isRespawn: false);
        }
    }

    private void SpawnEntity(
        SpawnSlot slot,
        bool isRespawn)
    {
        Transform point = slot.Placement.Point;

        GameObject instance = Instantiate(
            slot.Placement.Prefab,
            point.position,
            point.rotation);

        slot.Instance = instance;
        slot.Entity = instance.GetComponent<CharacterStats>();
        slot.Behavior = instance.GetComponent<NPCBehavior>();

        slot.RespawnAt = double.PositiveInfinity;
        slot.TotalDamage = 0;
        slot.PlayerDamage.Clear();

        int objectiveIndex =
            slot.Placement.ObjectiveIndex;

        if (objectiveIndex >= 0 &&
            CurrentPlan.Goals[objectiveIndex].Type ==
                TrialObjectiveType.Interact)
        {
            TrialObjectiveInteraction interaction =
                instance.GetComponent<TrialObjectiveInteraction>();

            if (interaction == null)
            {
                interaction =
                    instance.AddComponent<TrialObjectiveInteraction>();
            }

            TrialStagePlan.Goal goal =
                CurrentPlan.Goals[objectiveIndex];

            interaction.Initialize(
                this,
                goal.Label,
                goal.Timing);

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

    internal bool CanUseInteraction(
        GameObject instance,
        PlayerStats player)
    {
        if (!CanRecord() ||
            player == null ||
            !player.IsAlive ||
            !Contains(player) ||
            instance == null ||
            !instance.activeInHierarchy ||
            !interactionSpawns.TryGetValue(
                instance,
                out SpawnSlot slot))
        {
            return false;
        }

        int index = slot.Placement.ObjectiveIndex;

        return objectiveCounts[index] <
               CurrentPlan.Goals[index].RequiredCount;
    }

    internal void CompleteInteraction(
        GameObject instance,
        PlayerStats player)
    {
        if (!CanUseInteraction(instance, player))
            return;

        SpawnSlot slot = interactionSpawns[instance];
        interactionSpawns.Remove(instance);

        int index = slot.Placement.ObjectiveIndex;
        objectiveCounts[index]++;

        Score score = GetOrCreateScore(player);

        score.Objectives++;
        score.ObjectivePoints += rules.InteractionPoints;

        MarkActive(score);

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
        for (int index = 0;
             index < CurrentPlan.Goals.Count;
             index++)
        {
            TrialStagePlan.Goal goal =
                CurrentPlan.Goals[index];

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
        return IsActive &&
               !stageComplete &&
               (!HasTimer || Time.timeAsDouble < deadline);
    }

    private bool AreObjectivesComplete()
    {
        for (int index = 0;
             index < CurrentPlan.Goals.Count;
             index++)
        {
            if (objectiveCounts[index] <
                CurrentPlan.Goals[index].RequiredCount)
            {
                return false;
            }
        }

        return true;
    }

    private void HandleDefeat(
        CharacterDefeatedResult result)
    {
        if (!CanRecord() ||
            result == null ||
            result.Victim == null ||
            !livingSpawns.Remove(
                result.Victim,
                out SpawnSlot slot))
        {
            return;
        }

        bool destroyedObjective = false;

        for (int index = 0;
             index < CurrentPlan.Goals.Count;
             index++)
        {
            TrialStagePlan.Goal goal =
                CurrentPlan.Goals[index];

            if (goal.Type != TrialObjectiveType.Interact &&
                objectiveCounts[index] < goal.RequiredCount &&
                CurrentPlan.Matches(index, slot.Placement) &&
                EntityTargetUtility.Matches(
                    goal.TargetId,
                    result.VictimEntityId))
            {
                objectiveCounts[index]++;

                destroyedObjective |=
                    goal.Type == TrialObjectiveType.Destroy;
            }
        }

        if (destroyedObjective && slot.TotalDamage > 0)
        {
            foreach (var entry in slot.PlayerDamage)
            {
                if (entry.Key == null)
                    continue;

                Score score =
                    GetOrCreateScore(entry.Key);

                score.Objectives++;

                score.ObjectivePoints +=
                    rules.DestructionPoints *
                    ((double)entry.Value / slot.TotalDamage);
            }
        }

        stageComplete = AreObjectivesComplete();

        if (!stageComplete &&
            CurrentStage.RespawnDefeatedEnemies &&
            NeedsRespawn(slot))
        {
            slot.RespawnAt =
                Time.timeAsDouble +
                CurrentStage.RespawnDelaySeconds;
        }
    }

    private void HandleDamage(
        DamageAppliedEvent damage)
    {
        if (!CanRecord() ||
            damage.AppliedDamage <= 0 ||
            damage.Victim == null)
        {
            return;
        }

        CharacterStats attacker =
            damage.Source.DirectSource;

        if (damage.Victim is PlayerStats wounded &&
            Contains(wounded) &&
            attacker != null &&
            livingSpawns.TryGetValue(
                attacker,
                out SpawnSlot sourceSlot) &&
            sourceSlot.Behavior != null)
        {
            TrackWound(wounded, damage.AppliedDamage);
        }

        if (!livingSpawns.TryGetValue(
                damage.Victim,
                out SpawnSlot slot) ||
            !HasOutstandingGoal(slot))
        {
            return;
        }

        slot.TotalDamage += damage.AppliedDamage;

        if (damage.Source.CreditOwner is not PlayerStats player ||
            !Contains(player))
        {
            return;
        }

        Score score = GetOrCreateScore(player);
        score.Damage += damage.AppliedDamage;

        bool destructible =
            slot.Placement.ObjectiveIndex >= 0 &&
            CurrentPlan.Goals[
                slot.Placement.ObjectiveIndex].Type ==
            TrialObjectiveType.Destroy;

        if (destructible)
        {
            slot.PlayerDamage.TryGetValue(
                player,
                out long previous);

            slot.PlayerDamage[player] =
                previous + damage.AppliedDamage;
        }
        else
        {
            score.DamagePoints += rules.DamagePoints(
                damage.AppliedDamage,
                damage.Victim.GetMaxHP());
        }

        MarkActive(score);
    }

    private void TrackWound(
        PlayerStats target,
        int amount)
    {
        if (!trialWounds.TryGetValue(
                target,
                out int existing))
        {
            target.OnDied += ForgetWounds;
        }

        int missingHealth = Mathf.Max(
            0,
            target.GetMaxHP() - target.currentHP);

        trialWounds[target] = (int)Math.Min(
            (long)existing + amount,
            missingHealth);
    }

    private void HandleHealing(
        CharacterStats healer,
        CharacterStats target,
        HealingResult result)
    {
        if (target is not PlayerStats wounded ||
            !trialWounds.TryGetValue(
                wounded,
                out int outstanding))
        {
            return;
        }

        long missingBeforeHeal =
            (long)wounded.GetMaxHP() -
            wounded.currentHP +
            result.AppliedAmount;

        outstanding = (int)Math.Min(
            outstanding,
            Math.Max(0L, missingBeforeHeal));

        int restoredTrialHealth =
            Math.Min(outstanding, result.AppliedAmount);

        int remaining =
            outstanding - restoredTrialHealth;

        if (remaining == 0)
            ForgetWounds(wounded);
        else
            trialWounds[wounded] = remaining;

        if (restoredTrialHealth <= 0 ||
            !CanRecord() ||
            healer is not PlayerStats player ||
            !Contains(player) ||
            !Contains(wounded))
        {
            return;
        }

        Score score = GetOrCreateScore(player);

        score.Healing += restoredTrialHealth;

        score.HealingPoints += rules.HealingPoints(
            restoredTrialHealth,
            wounded.GetMaxHP());

        MarkActive(score);
    }

    private void ForgetWounds(
        CharacterStats target)
    {
        if (target is PlayerStats player &&
            trialWounds.Remove(player))
        {
            player.OnDied -= ForgetWounds;
        }
    }

    private void RecordTanking(float seconds)
    {
        if (seconds <= 0f)
            return;

        foreach (SpawnSlot slot in livingSpawns.Values)
        {
            NPCBehavior enemy = slot.Behavior;

            if (!HasOutstandingGoal(slot) ||
                enemy == null ||
                !enemy.IsInCombat ||
                enemy.CurrentTarget is not PlayerStats player ||
                !Contains(player) ||
                !player.IsAlive)
            {
                continue;
            }

            Score score = GetOrCreateScore(player);

            if (Time.timeAsDouble >= score.ActiveUntil)
                continue;

            score.Tanking += seconds;

            score.TankingPoints +=
                seconds * rules.TankingPerEnemySecond;
        }
    }

    private Score GetOrCreateScore(
    PlayerStats player)
    {
        if (!scores.TryGetValue(
                player,
                out Score score))
        {
            score = new Score(scores.Count + 1);

            score.Actions =
                player.GetComponent<CharacterActionController>();

            if (score.Actions != null)
            {
                score.Actions.OnExecutionStarted +=
                    HandleParticipantExecution;
            }

            scores.Add(player, score);
        }

        return score;
    }

    private void HandleParticipantExecution(
    ActionExecutionContext action)
    {
        if (action == null ||
            !CanRecord() ||
            action.Caster is not PlayerStats player ||
            !player.IsAlive ||
            !Contains(player))
        {
            return;
        }

        foreach (CharacterStats target in action.AffectedCharacters)
        {
            if (target == null ||
                !target.IsAlive ||
                !livingSpawns.TryGetValue(
                    target,
                    out SpawnSlot slot) ||
                slot.Behavior == null ||
                !HasOutstandingGoal(slot))
            {
                continue;
            }

            MarkActive(GetOrCreateScore(player));
            return;
        }
    }

    public float GetContribution(Score score)
    {
        double average = GetRewardAverage();

        return score != null &&
               rules.Qualifies(score.TotalPoints) &&
               average > 0d
            ? (float)(score.TotalPoints / average)
            : 0f;
    }

    public void CopyScoresTo(
        List<Score> destination)
    {
        destination.Clear();
        destination.AddRange(scores.Values);
        destination.Sort(CompareScores);
    }

    private void BeginCooldown(bool completed)
    {
        if (coolingDown)
            return;

        victory = completed;
        coolingDown = true;

        deadline =
            Time.timeAsDouble + resetSeconds;

        double average = GetRewardAverage();
        double total = 0d;

        List<KeyValuePair<PlayerStats, Score>> ordered = new();

        foreach (var entry in scores)
        {
            if (entry.Key == null)
                continue;

            ordered.Add(entry);
            total += entry.Value.TotalPoints;
        }

        ordered.Sort(
            (left, right) =>
                CompareScores(left.Value, right.Value));

        List<TrialParticipantResult> participants = new();
        int place = 0;

        foreach (var entry in ordered)
        {
            double points =
                entry.Value.TotalPoints;

            bool eligible =
                rules.Qualifies(points);

            participants.Add(
                new TrialParticipantResult(
                    entry.Key,
                    points,
                    eligible && average > 0d
                        ? points / average
                        : 0d,
                    total > 0d
                        ? points / total
                        : 0d,
                    eligible ? ++place : 0,
                    eligible));
        }

        ClearStage();

        rewards.Settle(
            completed,
            participants,
            rules);
    }

    private void ResetRound()
    {
        foreach (Score score in scores.Values)
        {
            if (score.Actions != null)
            {
                score.Actions.OnExecutionStarted -=
                    HandleParticipantExecution;
            }
        }

        rewards.ResetRound();
        scores.Clear();

        stageIndex = -1;
        objectiveCounts = Array.Empty<int>();

        stageComplete = false;
        coolingDown = false;
        victory = false;
        deadline = 0d;
    }

    public string GetRewardSummary(
        PlayerStats player)
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
        {
            return ConfigurationError(
                "Set Is Trigger and assign at least one stage.");
        }

        List<TrialSpawnPoint> markers = new();

        foreach (TrialSpawnPoint marker in
                 GetComponentsInChildren<TrialSpawnPoint>(true))
        {
            if (marker.Zone == this)
                markers.Add(marker);
        }

        stagePlans =
            new TrialStagePlan[stages.Length];

        for (int index = 0;
             index < stages.Length;
             index++)
        {
            TrialStageDefinition stage =
                stages[index];

            if (stage == null)
            {
                return ConfigurationError(
                    $"Stage {index + 1} is empty.");
            }

            TrialStagePlan plan = new();

            string error = plan.Build(
                stage,
                markers,
                spawnPoints);

            if (error != null)
            {
                return ConfigurationError(
                    $"{stage.name}: {error}");
            }

            stagePlans[index] = plan;
        }

        return true;
    }

    private void MarkActive(Score score)
    {
        score.ActiveUntil =
            Time.timeAsDouble + rules.ActivitySeconds;
    }

    private bool HasOutstandingGoal(
        SpawnSlot slot)
    {
        for (int index = 0;
             index < CurrentPlan.Goals.Count;
             index++)
        {
            if (objectiveCounts[index] <
                    CurrentPlan.Goals[index].RequiredCount &&
                CurrentPlan.Matches(index, slot.Placement))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsEngaged(PlayerStats player)
    {
        if (scores.TryGetValue(player, out Score score) &&
            Time.timeAsDouble < score.ActiveUntil)
        {
            return true;
        }

        foreach (SpawnSlot slot in livingSpawns.Values)
        {
            if (slot.Behavior != null &&
                slot.Behavior.IsInCombat &&
                slot.Behavior.CurrentTarget == player &&
                HasOutstandingGoal(slot))
            {
                return true;
            }
        }

        return false;
    }

    private void HandleSupport(
        BuffApplicationEvent result)
    {
        if (!CanRecord() ||
            result.Source.CreditOwner is not PlayerStats player ||
            !Contains(player) ||
            !player.IsAlive ||
            result.Target == null ||
            result.Definition == null)
        {
            return;
        }

        bool valid;

        if (result.Target is PlayerStats ally)
        {
            valid =
                Contains(ally) &&
                ally.IsAlive &&
                IsEngaged(ally) &&
                TargetRelationResolver.Resolve(
                    player,
                    ally.gameObject) != TargetRelation.Hostile &&
                (result.WasDispelled
                    ? result.Definition.IsDebuff
                    : !result.Definition.IsDebuff);
        }
        else
        {
            valid =
                livingSpawns.TryGetValue(
                    result.Target,
                    out SpawnSlot slot) &&
                slot.Behavior != null &&
                slot.Behavior.IsInCombat &&
                HasOutstandingGoal(slot) &&
                (result.WasDispelled
                    ? !result.Definition.IsDebuff
                    : result.Definition.IsDebuff);
        }

        if (!valid)
            return;

        double points = result.WasDispelled
            ? rules.DispelPoints
            : rules.BuffPoints * result.AddedDurationFraction;

        if (points <= 0d)
            return;

        Score score = GetOrCreateScore(player);

        score.SupportPoints += points;
        MarkActive(score);
    }

    public double GetRewardAverage()
    {
        if (rules == null)
            return 0d;

        double total = 0d;
        int count = 0;

        foreach (var entry in scores)
        {
            if (entry.Key == null ||
                !rules.Qualifies(entry.Value.TotalPoints))
            {
                continue;
            }

            total += entry.Value.TotalPoints;
            count++;
        }

        return count > 0 ? total / count : 0d;
    }

    private static int CompareScores(
        Score left,
        Score right)
    {
        int points =
            right.TotalPoints.CompareTo(left.TotalPoints);

        if (points != 0)
            return points;

        int tie =
            right.TieBreaker.CompareTo(left.TieBreaker);

        return tie != 0
            ? tie
            : left.Number.CompareTo(right.Number);
    }

    private bool ConfigurationError(
        string message)
    {
        Debug.LogError(
            $"Trial Zone '{name}': {message}",
            this);

        return false;
    }
}