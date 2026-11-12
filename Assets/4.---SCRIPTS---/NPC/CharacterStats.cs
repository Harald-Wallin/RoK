using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("Faction")]
    public Faction faction;

    [Header("Crime")]
    public int reputationLossOnHit = 20;
    public int reputationLossOnDeath = 100;

    [Header("Identity")]

    [SerializeField]
    public string displayName = "NPC";

    [SerializeField]
    private CharacterRole role =
        CharacterRole.None;


    [Header("Creature Classification")]

    [SerializeField]
    [Tooltip(
        "Creature categories used by Deeds and future gameplay systems. " +
        "Multiple types can be selected."
    )]
    private CreatureType creatureTypes =
        CreatureType.None;


    [Header("Level")]

    [Min(1)]
    public int level = 1;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName)
            ? gameObject.name
            : displayName;

    public CharacterRole Role =>
        role;

    public CreatureType CreatureTypes =>
    creatureTypes;
    public bool HasRole =>
    role != CharacterRole.None;

    public string RoleName =>
        role.GetDisplayName();

    /// <summary>
    /// Om karaktärens faction är avsedd att vara synlig för
    /// spelaren i UI.
    /// </summary>
    public bool HasVisibleFaction =>
        faction != null &&
        faction.showInReputationWindow &&
        !string.IsNullOrWhiteSpace(
            faction.factionName
        );

    /// <summary>
    /// Returnerar factionens namn endast när factionen är publik.
    /// Meta-factions och andra dolda factions returnerar tom text.
    /// </summary>
    public string FactionName =>
        HasVisibleFaction
            ? faction.factionName
            : string.Empty;

    [Header("Base Stats")]
    [SerializeField]
    private List<StatEntry> stats = new();

    private readonly Dictionary<StatType, float> statLookup = new();

    [Header("Derived Stats")]
    [SerializeField]
    [Tooltip(
        "Automatically generated stats based on primary stats, " +
        "stat definitions and the scaling profile."
    )]
    private List<DerivedStat> derivedStats = new();

    private readonly Dictionary<StatType, float> derivedLookup = new();

    [SerializeField]
    private int _currentHP;

    public int currentHP
    {
        get => _currentHP;
        set => _currentHP = value;
    }

    private static StatScalingProfile cachedScalingProfile;

    [Header("Advanced Stats")]
    public bool IsAlive => currentHP > 0;
    public bool IsStunned => stunCount > 0;

    private int stunCount;

    private readonly List<StatModifier> modifiers = new();

    private CharacterStateController stateController;

    private DamageContributionTracker damageContributionTracker;

    private DamageSourceContext pendingFatalDamageSource;

    private bool deathProcessed;

    public DamageContributionTracker DamageContributions =>
        damageContributionTracker;

    public event Action OnHealthChanged;
    public event Action OnStatsChanged;
    public event Action<DamageAppliedEvent>
    OnDamageApplied;
    public event Action<CharacterStats> OnDamagedBy;
    public event Action<CharacterStats> OnDied;
    public event Action<HealingResult> OnHealed;

    [Header("Death Rewards")]
    public DeathReward deathReward;

    [Header("VFX")]
    public Transform effectPoint;

    [Header("Healing Feedback")]

    [SerializeField]
    private HealingFloatingText
    healingFloatingTextPrefab;

    [SerializeField]
    private Vector3 healingTextOffset =
        new Vector3(
            0f,
            1.5f,
            0f
        );

    protected virtual void Awake()
    {
        stateController =
            GetComponent<CharacterStateController>();

        damageContributionTracker =
    GetComponent<DamageContributionTracker>();

        if (damageContributionTracker == null)
        {
            damageContributionTracker =
                gameObject.AddComponent<
                    DamageContributionTracker
                >();
        }

        deathReward =
            GetComponent<DeathReward>();

        RefreshStats();

        currentHP = GetMaxHP();
    }

    /// <summary>
    /// Synchronizes serialized stat entries with StatDatabase,
    /// rebuilds the runtime lookup and recalculates derived stats.
    /// </summary>
    public void RefreshStats()
    {
        SynchronizeWithDatabase();
        InitializeBaseStatLookup();
        RecalculateDerivedStats();
    }

    /// <summary>
    /// Rebuilds the serialized lists from StatDatabase while
    /// preserving existing primary-stat values.
    /// </summary>
    public void SynchronizeWithDatabase()
    {
        StatDatabase database =
            StatDatabase.Instance;

        if (database == null)
            return;

        Dictionary<StatType, float> existingPrimaryValues = new();

        for (int i = 0; i < stats.Count; i++)
        {
            StatEntry entry = stats[i];

            if (entry == null)
                continue;

            if (!existingPrimaryValues.ContainsKey(entry.stat))
            {
                existingPrimaryValues.Add(
                    entry.stat,
                    entry.value
                );
            }
        }

        stats.Clear();

        HashSet<StatType> addedPrimaryStats = new();

        foreach (StatDefinition definition in database.Stats)
        {
            if (definition == null)
                continue;

            if (definition.kind != StatKind.Primary)
                continue;

            if (!addedPrimaryStats.Add(definition.stat))
                continue;

            float value =
                existingPrimaryValues.TryGetValue(
                    definition.stat,
                    out float existingValue)
                    ? existingValue
                    : definition.defaultValue;

            stats.Add(new StatEntry
            {
                stat = definition.stat,
                value = value
            });
        }

        derivedStats.Clear();

        HashSet<StatType> addedDerivedStats = new();

        foreach (StatDefinition definition in database.Stats)
        {
            if (definition == null)
                continue;

            if (definition.kind != StatKind.Derived)
                continue;

            if (!addedDerivedStats.Add(definition.stat))
                continue;

            derivedStats.Add(new DerivedStat
            {
                stat = definition.stat,
                value = definition.defaultValue
            });
        }
    }

    public virtual void ResetHealth()
    {
        currentHP = GetMaxHP();
        RaiseHealthChanged();
    }

    protected void RaiseDied(CharacterStats deadCharacter)
    {
        OnDied?.Invoke(deadCharacter);
    }

    public void RaiseHealthChanged()
    {
        OnHealthChanged?.Invoke();
    }

    public int TakeDamage(
    DamageResult result,
    CharacterStats attacker)
    {
        return TakeDamage(
            result,
            DamageSourceContext.FromDirectSource(
                attacker
            )
        );
    }

    public int TakeDamage(
        DamageResult result,
        DamageSourceContext source)
    {
        if (!IsAlive)
            return 0;

        CharacterStats directAttacker =
            source.DirectSource;

        CharacterStats responsibleAttacker =
            source.CreditOwner != null
                ? source.CreditOwner
                : directAttacker;

        if (result.isMiss)
        {
            FloatingTextSpawner.Instance?.SpawnCustomText(
                transform.position,
                "Miss",
                false
            );

            OnDamagedBy?.Invoke(
                directAttacker
            );

            return 0;
        }

        if (result.isEvaded)
        {
            FloatingTextSpawner.Instance?.SpawnCustomText(
                transform.position,
                "Evade",
                false
            );

            OnDamagedBy?.Invoke(
                directAttacker
            );

            return 0;
        }

        int finalDamage =
            Mathf.Max(
                0,
                result.damage
            );

        if (result.isBlocked)
        {
            PlayerStats player =
                GetComponent<PlayerStats>();

            if (player != null)
            {
                WardSystem ward =
                    GetComponent<WardSystem>();

                ward?.AddWard(1);
            }
        }

        if (finalDamage <= 0)
        {
            stateController?.NotifyCombatActivity();

            if (directAttacker != null)
            {
                CharacterStateController attackerState =
                    directAttacker.GetComponent<
                        CharacterStateController
                    >();

                attackerState?.NotifyCombatActivity();
            }

            OnDamagedBy?.Invoke(
                directAttacker
            );

            string blockedText =
                result.isBlocked
                    ? $"0 <color=white>({result.blockedAmount} Block)</color>"
                    : "0";

            bool zeroDamageAttackerIsPlayer =
                responsibleAttacker is PlayerStats;

            FloatingTextSpawner.Instance
                ?.SpawnDamageText(
                    transform.position,
                    blockedText,
                    false,
                    !zeroDamageAttackerIsPlayer
                );

            return 0;
        }

        int healthBefore =
            currentHP;

        currentHP =
            Mathf.Max(
                0,
                currentHP - finalDamage
            );

        int appliedDamage =
            healthBefore -
            currentHP;

        if (appliedDamage <= 0)
            return 0;

        damageContributionTracker?.RegisterDamage(
            source,
            appliedDamage
        );

        OnDamageApplied?.Invoke(
            new DamageAppliedEvent(
                this,
                source,
                appliedDamage
            )
        );

        RaiseHealthChanged();

        stateController?.NotifyCombatActivity();

        if (directAttacker != null)
        {
            CharacterStateController attackerState =
                directAttacker.GetComponent<
                    CharacterStateController
                >();

            attackerState?.NotifyCombatActivity();
        }

        OnDamagedBy?.Invoke(
            directAttacker
        );

        DamageReaction reaction =
            GetComponentInChildren<DamageReaction>();

        if (reaction != null &&
            directAttacker != null)
        {
            reaction.PlayReaction(
                directAttacker.transform.position
            );
        }

        CrimeManager.HandleAttackCrime(
            responsibleAttacker,
            this
        );

        stateController?.EnterCombat();

        OnDamaged(
            directAttacker
        );

        string damageText =
            $"-{appliedDamage}";

        if (result.isBlocked)
        {
            damageText +=
                $" <color=white>({result.blockedAmount} Block)</color>";
        }

        bool attackerIsPlayer =
            responsibleAttacker is PlayerStats;

        FloatingTextSpawner.Instance?.SpawnDamageText(
            transform.position,
            damageText,
            result.isCrit,
            !attackerIsPlayer
        );

        if (currentHP <= 0)
        {
            pendingFatalDamageSource =
                source;

            Die(
                directAttacker
            );
        }

        return appliedDamage;
    }

    protected virtual void OnDamaged(
        CharacterStats attacker)
    {
    }

    public int GetAttackDamage()
    {
        float baseDamage =
            GetStat(StatType.BaseMeleeDamage);

        float weaponDamage =
            GetStat(StatType.WeaponDamage);

        return Mathf.Max(
            1,
            Mathf.RoundToInt(
                baseDamage + weaponDamage
            )
        );
    }

    public int GetMaxHP()
    {
        return Mathf.RoundToInt(
            GetStat(StatType.MaxHP)
        );
    }

    public virtual int Heal(
    int amount)
    {
        HealingResult result =
            ApplyHealing(
                amount,
                canCrit: false
            );

        return result.AppliedAmount;
    }

    public virtual HealingResult ApplyHealing(
        int amount,
        bool canCrit,
        float criticalChance = 0f,
        float criticalMultiplier = 1.5f)
    {
        if (amount <= 0 ||
            !IsAlive)
        {
            return new HealingResult(
                amount,
                0,
                false
            );
        }

        bool isCritical =
            canCrit &&
            criticalChance > 0f &&
            UnityEngine.Random.value <=
            Mathf.Clamp01(
                criticalChance
            );

        int finalAmount =
            amount;

        if (isCritical)
        {
            finalAmount =
                Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        amount *
                        Mathf.Max(
                            1f,
                            criticalMultiplier
                        )
                    )
                );
        }

        int oldHealth =
            currentHP;

        currentHP =
            Mathf.Clamp(
                currentHP +
                finalAmount,
                0,
                GetMaxHP()
            );

        int restoredHealth =
            currentHP -
            oldHealth;

        HealingResult result =
            new HealingResult(
                finalAmount,
                restoredHealth,
                isCritical
            );

        if (restoredHealth <= 0)
            return result;

        RaiseHealthChanged();

        OnHealed?.Invoke(
            result
        );

        SpawnHealingFloatingText(
            result
        );

        return result;
    }

    private void SpawnHealingFloatingText(
        HealingResult result)
    {
        if (!result.RestoredHealth)
            return;

        Vector3 spawnPosition =
            effectPoint != null
                ? effectPoint.position
                : transform.position +
                  healingTextOffset;

        if (healingFloatingTextPrefab != null)
        {
            HealingFloatingText instance =
                Instantiate(
                    healingFloatingTextPrefab,
                    spawnPosition,
                    Quaternion.identity
                );

            instance.Initialize(
                result.AppliedAmount,
                result.IsCritical
            );

            return;
        }

        /*
         * Tillfällig fallback om det nya prefabet ännu inte
         * har dragits in.
         *
         * TMP rich text gör texten grön, medan den befintliga
         * FloatingTextSpawner sköter sin vanliga animation.
         */
        FloatingTextSpawner.Instance
            ?.SpawnCustomText(
                spawnPosition,
                $"<color=#65FF7A>" +
                $"+{result.AppliedAmount}" +
                $"</color>",
                result.IsCritical
            );
    }

    public int TakeRawDamage(
    int damage,
    CharacterStats attacker)
    {
        return TakeRawDamage(
            damage,
            DamageSourceContext.FromDirectSource(
                attacker
            )
        );
    }

    public int TakeRawDamage(
        int damage,
        DamageSourceContext source)
    {
        DamageResult result =
            new DamageResult
            {
                damage = damage,
                isCrit = false,
                isMiss = false,
                isEvaded = false
            };

        return TakeDamage(
            result,
            source
        );
    }

    public virtual void ResetEncounterState()
    {
        if (!IsAlive)
            return;

        stunCount = 0;
        deathProcessed = false;

        damageContributionTracker
            ?.ResetContributions();

        currentHP =
            GetMaxHP();

        stateController
            ?.ForceLeaveCombat();

        RaiseHealthChanged();
    }

    public void AddStun()
    {
        stunCount++;
    }

    public void RemoveStun()
    {
        stunCount =
            Mathf.Max(0, stunCount - 1);
    }

    public bool CanAct()
    {
        return !IsStunned;
    }

    public void RecalculateDerivedStats()
    {
        derivedLookup.Clear();

        StatDatabase database =
            StatDatabase.Instance;

        if (database == null)
            return;

        Dictionary<StatType, DerivedStat> derivedByType = new();

        for (int i = 0; i < derivedStats.Count; i++)
        {
            DerivedStat derived = derivedStats[i];

            if (derived == null)
                continue;

            StatDefinition definition =
                database.GetDefinition(derived.stat);

            float defaultValue =
                definition != null
                    ? definition.defaultValue
                    : 0f;

            derived.value = defaultValue;

            derivedByType[derived.stat] = derived;
            derivedLookup[derived.stat] = defaultValue;
        }

        StatScalingProfile profile =
            ScalingProfile;

        if (profile == null)
            return;

        foreach (StatScalingRule rule in profile.rules)
        {
            if (rule == null)
                continue;

            StatDefinition sourceDefinition =
                database.GetDefinition(rule.source);

            if (sourceDefinition == null ||
                sourceDefinition.kind != StatKind.Primary)
            {
                continue;
            }

            // Uses the fully modified primary-stat value.
            // Equipment or talents that increase Health can
            // therefore also increase MaxHP.
            float sourceValue =
                GetStat(rule.source);

            foreach (StatScalingOutput output in rule.outputs)
            {
                if (output == null)
                    continue;

                StatDefinition outputDefinition =
                    database.GetDefinition(output.stat);

                if (outputDefinition == null ||
                    outputDefinition.kind != StatKind.Derived)
                {
                    continue;
                }

                if (!derivedByType.TryGetValue(
                        output.stat,
                        out DerivedStat derived))
                {
                    continue;
                }

                derived.value +=
                    sourceValue * output.value;

                derivedLookup[output.stat] =
                    derived.value;
            }
        }
    }

    public float GetStat(
    StatType stat)
    {
        return GetStatValueBreakdown(
            stat
        ).FinalValue;
    }

    public StatValueBreakdown GetStatValueBreakdown(StatType stat)
    {
        return GetStatValueBreakdown(stat, 0f, null);
    }

    internal StatValueBreakdown GetStatValueBreakdown(
        StatType stat,
        float equipmentFlatOverride,
        object equipmentSourceToReplace)
    {
        float rawValue = GetRawStatValue(stat);

        float talentFlat = 0f;
        float talentPercent = 0f;
        float equipmentFlat = equipmentFlatOverride;
        float equipmentPercent = 0f;
        float buffFlat = 0f;
        float buffPercent = 0f;
        float oathFlat = 0f;
        float oathPercent = 0f;

        foreach (StatModifier modifier in modifiers)
        {
            if (modifier == null || modifier.stat != stat)
                continue;

            switch (modifier.sourceType)
            {
                case ModifierSourceType.Talent:
                    AddModifierValue(modifier, ref talentFlat, ref talentPercent);
                    break;

                case ModifierSourceType.Equipment:
                    if (equipmentSourceToReplace != null &&
                        ReferenceEquals(modifier.source, equipmentSourceToReplace) &&
                        modifier.type == ModifierType.Flat)
                    {
                        break;
                    }

                    AddModifierValue(modifier, ref equipmentFlat, ref equipmentPercent);
                    break;

                case ModifierSourceType.Buff:
                    AddModifierValue(modifier, ref buffFlat, ref buffPercent);
                    break;

                case ModifierSourceType.Oath:
                    AddModifierValue(modifier, ref oathFlat, ref oathPercent);
                    break;
            }
        }

        float talentStage = (rawValue + talentFlat) * (1f + talentPercent);
        float equipmentStage = (talentStage + equipmentFlat) * (1f + equipmentPercent);
        float buffStage = (equipmentStage + buffFlat) * (1f + buffPercent);
        float oathStage = (buffStage + oathFlat) * (1f + oathPercent);

        return new StatValueBreakdown(
            rawValue, talentStage, equipmentStage, buffStage, oathStage);
    }

    private static void AddModifierValue(
        StatModifier modifier,
        ref float flat,
        ref float percent)
    {
        if (modifier.type ==
            ModifierType.Flat)
        {
            flat += modifier.value;
        }
        else
        {
            percent += modifier.value;
        }
    }

    public List<StatScalingContribution>
    GetScalingContributions(
        StatType sourceStat)
    {
        List<StatScalingContribution>
            contributions =
                new();

        StatScalingProfile profile =
            ScalingProfile;

        if (profile == null ||
            profile.rules == null)
        {
            return contributions;
        }

        float sourceValue =
            GetStat(
                sourceStat);

        foreach (StatScalingRule rule
                 in profile.rules)
        {
            if (rule == null ||
                rule.source != sourceStat ||
                rule.outputs == null)
            {
                continue;
            }

            foreach (StatScalingOutput output
                     in rule.outputs)
            {
                if (output == null)
                    continue;

                float contribution =
                    sourceValue *
                    output.value;

                float totalValue =
                    GetStat(
                        output.stat);

                contributions.Add(
                    new StatScalingContribution(
                        sourceStat,
                        output.stat,
                        contribution,
                        totalValue
                    )
                );
            }
        }

        return contributions;
    }

    public bool HasScalingContributions(
        StatType sourceStat)
    {
        StatScalingProfile profile =
            ScalingProfile;

        if (profile == null ||
            profile.rules == null)
        {
            return false;
        }

        foreach (StatScalingRule rule
                 in profile.rules)
        {
            if (rule == null ||
                rule.source != sourceStat ||
                rule.outputs == null)
            {
                continue;
            }

            if (rule.outputs.Count > 0)
                return true;
        }

        return false;
    }

    public void AddModifier(StatModifier modifier)
    {
        if (modifier == null)
            return;

        float oldMaxHP =
            GetStat(StatType.MaxHP);

        modifiers.Add(modifier);

        RecalculateDerivedStats();

        float newMaxHP =
            GetStat(StatType.MaxHP);

        PreserveCurrentHealthPercentage(
            oldMaxHP,
            newMaxHP
        );

        OnStatsChanged?.Invoke();
    }

    public void RemoveModifiersFromSource(
        object source)
    {
        float oldMaxHP =
            GetStat(StatType.MaxHP);

        modifiers.RemoveAll(
            modifier => modifier.source == source
        );

        RecalculateDerivedStats();

        float newMaxHP =
            GetStat(StatType.MaxHP);

        PreserveCurrentHealthPercentage(
            oldMaxHP,
            newMaxHP
        );

        OnStatsChanged?.Invoke();
    }

    private void PreserveCurrentHealthPercentage(
        float oldMaxHP,
        float newMaxHP)
    {
        if (Mathf.Approximately(
                oldMaxHP,
                newMaxHP))
        {
            return;
        }

        if (oldMaxHP <= 0f)
        {
            currentHP = Mathf.Clamp(
                currentHP,
                0,
                Mathf.RoundToInt(newMaxHP)
            );

            RaiseHealthChanged();
            return;
        }

        float healthPercentage =
            currentHP / oldMaxHP;

        currentHP = Mathf.Clamp(
            Mathf.RoundToInt(
                newMaxHP * healthPercentage
            ),
            0,
            Mathf.RoundToInt(newMaxHP)
        );

        RaiseHealthChanged();
    }

    protected virtual void Die(
    CharacterStats killer)
    {
        if (deathProcessed)
            return;

        deathProcessed = true;

        DamageSourceContext finalBlow =
            pendingFatalDamageSource.HasAnySource
                ? pendingFatalDamageSource
                : DamageSourceContext
                    .FromDirectSource(
                        killer
                    );

        DamageContributionSnapshot snapshot =
            damageContributionTracker != null
                ? damageContributionTracker
                    .CreateSnapshot()
                : new DamageContributionSnapshot(
                    null,
                    0
                );

        CreatureIdentity identity =
            GetComponent<CreatureIdentity>();

        CharacterDefeatedResult defeatedResult =
            new CharacterDefeatedResult(
                this,
                identity != null
                    ? identity.Definition
                    : null,
                finalBlow,
                snapshot
            );

        RaiseDied(
            this
        );

        CharacterCombatEvents
            .RaiseCharacterDefeated(
                defeatedResult
            );

        GiveDeathRewards(
            defeatedResult
        );

        HandleDeathCleanup();

        Destroy(
            gameObject
        );
    }

    protected virtual void HandleDeathCleanup()
    {
        if (!(this is PlayerStats))
        {
            Collider2D col =
                GetComponent<Collider2D>();

            if (col != null)
                col.enabled = false;
        }

        Rigidbody2D rb =
            GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        BuffSystem buffs =
            GetComponent<BuffSystem>();

        buffs?.RemoveDeathRemovableBuffs();
    }

    public bool IsHostileTo(
        CharacterStats other)
    {
        if (other == null)
            return false;

        if (other == this)
            return false;

        if (faction == null ||
            other.faction == null)
        {
            return false;
        }

        if (faction == other.faction)
            return false;

        ReputationState standing =
            faction.GetStanding(other.faction);

        if (standing == ReputationState.Hated)
            return true;

        PlayerStats player =
            other.GetComponent<PlayerStats>();

        if (player != null)
        {
            PlayerReputationManager reputation =
                player.GetComponent<PlayerReputationManager>();

            if (reputation != null)
            {
                ReputationState state =
                    reputation.GetReputationState(faction);

                if (state == ReputationState.Hated)
                    return true;
            }
        }

        return false;
    }

    public bool IsFriendlyTo(
        CharacterStats other)
    {
        if (other == null)
            return false;

        if (faction == null ||
            other.faction == null)
        {
            return false;
        }

        ReputationState standing =
            faction.GetStanding(other.faction);

        return standing != ReputationState.Hated &&
               standing != ReputationState.Indifferent;
    }

    public bool IsHostileToPlayer(
        PlayerStats player)
    {
        if (player == null)
            return false;

        PlayerReputationManager reputation =
            player.GetComponent<PlayerReputationManager>();

        if (reputation != null &&
            faction != null)
        {
            ReputationState state =
                reputation.GetReputationState(faction);

            if (state == ReputationState.Hated)
                return true;
        }

        NPCReactionController reaction =
            GetComponent<NPCReactionController>();

        return reaction != null &&
               reaction.IsTemporarilyHostile;
    }

    protected virtual void GiveDeathRewards(
    CharacterDefeatedResult result)
    {
        deathReward?.GiveRewards(
            result
        );
    }

    public void EnterCombat()
    {
        stateController?.EnterCombat();
    }

    public bool IsInCombat()
    {
        return stateController != null &&
               stateController.InCombat;
    }

    private void InitializeBaseStatLookup()
    {
        statLookup.Clear();

        foreach (StatEntry entry in stats)
        {
            if (entry == null)
                continue;

            statLookup[entry.stat] =
                entry.value;
        }
    }

    public void SetBaseStat(
        StatType stat,
        float value)
    {
        StatDatabase database =
            StatDatabase.Instance;

        StatDefinition definition =
            database?.GetDefinition(stat);

        if (definition == null)
        {
            Debug.LogWarning(
                $"Kan inte sätta {stat}: ingen StatDefinition finns.",
                this
            );

            return;
        }

        if (definition.kind != StatKind.Primary)
        {
            Debug.LogWarning(
                $"Kan inte sätta {stat} som base stat eftersom " +
                $"definitionen är {definition.kind}.",
                this
            );

            return;
        }

        StatEntry entry =
            stats.Find(candidate =>
                candidate != null &&
                candidate.stat == stat);

        if (entry == null)
        {
            entry = new StatEntry
            {
                stat = stat
            };

            stats.Add(entry);
        }

        entry.value = value;
        statLookup[stat] = value;

        RecalculateDerivedStats();

        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// Returns the stored primary value or the calculated
    /// derived value before modifiers for that same stat.
    /// </summary>
    public float GetBaseStatValue(
        StatType stat)
    {
        return GetRawStatValue(stat);
    }

    private float GetRawStatValue(
        StatType stat)
    {
        if (statLookup.TryGetValue(
                stat,
                out float baseValue))
        {
            return baseValue;
        }

        if (derivedLookup.TryGetValue(
                stat,
                out float derivedValue))
        {
            return derivedValue;
        }

        return 0f;
    }

    private StatScalingProfile ScalingProfile
    {
        get
        {
            if (cachedScalingProfile == null)
            {
                cachedScalingProfile =
                    Resources.Load<StatScalingProfile>(
                        "Stats/StatScalingProfile"
                    );

                if (cachedScalingProfile == null)
                {
                    cachedScalingProfile =
                        Resources.Load<StatScalingProfile>(
                            "StatScalingProfile"
                        );
                }

                if (cachedScalingProfile == null)
                {
                    Debug.LogError(
                        "StatScalingProfile kunde inte hittas. " +
                        "Lägg asseten i Resources/Stats eller Resources.",
                        this
                    );
                }
            }

            return cachedScalingProfile;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RefreshStats();
    }
#endif
}