
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns talent point progression.
///
/// Shared passive effects use the same
/// CharacterEffectController as buffs,
/// titles and equipment.
///
/// Legacy talents remain supported
/// until migrated.
/// </summary>
public sealed class TalentManager :
    MonoBehaviour
{
    public static TalentManager Instance;

    public int availablePoints;

    [SerializeField]
    private List<TalentData> allTalents =
        new();

    [SerializeField]
    private List<int> tierRequirements =
        new();

    public IReadOnlyList<TalentData> AllTalents =>
        allTalents;

    public List<TalentRuntime> talents =
        new();

    // =====================================================
    // RUNTIME
    // =====================================================

    private const int BaseWardCapacity = 5;

    private readonly Dictionary<
        TalentRuntime,
        EffectHandle> passiveHandles =
            new();

    private CharacterEffectController
        effectController;

    private bool suppressEffectEvents;

    private PlayerStats Player =>
        PlayerReference.Player;

    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    private void Awake()
    {
        Instance = this;

        Initialize(allTalents);
    }

    private void OnDestroy()
    {
        if (effectController != null)
        {
            effectController.OnEffectsChanged -=
                HandleEffectsChanged;
        }

        suppressEffectEvents = true;

        CleanupPassiveEffects();

        suppressEffectEvents = false;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // =====================================================
    // INITIALIZATION
    // =====================================================

    public void Initialize(
        List<TalentData> talentData)
    {
        suppressEffectEvents = true;

        CleanupPassiveEffects();

        PlayerStats player = Player;

        if (player != null)
        {
            foreach (TalentRuntime runtime
                     in talents)
            {
                if (runtime != null)
                {
                    player.RemoveModifiersFromSource(
                        runtime
                    );
                }
            }
        }

        talents.Clear();

        if (talentData != null)
        {
            foreach (TalentData data
                     in talentData)
            {
                if (data == null)
                    continue;

                talents.Add(
                    new TalentRuntime(data)
                );
            }
        }

        suppressEffectEvents = false;

        RefreshWardState();
    }

    // =====================================================
    // TIER REQUIREMENTS
    // =====================================================

    public int GetTierRequirement(int tier)
    {
        if (tier <= 1)
            return 0;

        int index = tier - 1;

        if (tierRequirements == null ||
            index < 0 ||
            index >= tierRequirements.Count)
        {
            return int.MaxValue;
        }

        return tierRequirements[index];
    }

    // =====================================================
    // SPEND TALENT POINT
    // =====================================================

    public bool TrySpendPoint(
        TalentRuntime talent)
    {
        if (!CanLearnTalent(talent))
            return false;

        int previousPoints =
            talent.currentPoints;

        talent.currentPoints++;

        if (!ApplyTalent(talent))
        {
            talent.currentPoints =
                previousPoints;

            ApplyTalent(talent);

            Debug.LogError(
                $"Failed to apply talent " +
                $"'{talent.data.DisplayName}'. " +
                $"Point not spent.",
                this
            );

            return false;
        }

        availablePoints--;

        HandleUnlocks(talent);

        return true;
    }

    // =====================================================
    // REAPPLY EFFECTS
    // =====================================================

    /// <summary>
    /// Reapplies assigned talent points without
    /// spending additional points.
    ///
    /// Intended for future save/restore integration.
    /// </summary>
    public void ReapplyAllTalentEffects()
    {
        foreach (TalentRuntime talent in talents)
        {
            if (talent != null)
            {
                ApplyTalent(talent);
            }
        }

        RefreshWardState();
    }

    // =====================================================
    // APPLY TALENT
    // =====================================================

    private bool ApplyTalent(
        TalentRuntime talent)
    {
        PlayerStats player = Player;

        if (player == null ||
            talent?.data == null)
        {
            return false;
        }

        // Remove any legacy modifiers
        // belonging to this talent.
        player.RemoveModifiersFromSource(
            talent
        );

        if (talent.currentPoints <= 0)
        {
            RemovePassiveHandle(talent);

            RefreshWardState();

            return true;
        }

        // =================================================
        // SHARED EFFECT PATH
        // =================================================

        if (talent.data.HasSharedEffects)
        {
            if (!EnsureEffectController(player))
                return false;

            if (passiveHandles.TryGetValue(
                    talent,
                    out EffectHandle existing))
            {
                if (!effectController.SetStacks(
                        existing,
                        talent.currentPoints))
                {
                    passiveHandles.Remove(talent);

                    return false;
                }
            }
            else
            {
                EffectHandle handle =
                    effectController.Activate(
                        talent.data.PassiveEffects,
                        DamageSourceContext
                            .FromDirectSource(player),
                        ModifierSourceType.Talent,
                        talent.currentPoints
                    );

                if (handle == null)
                    return false;

                passiveHandles.Add(
                    talent,
                    handle
                );
            }
        }

        // =================================================
        // LEGACY EFFECT PATH
        // =================================================

        else
        {
            RemovePassiveHandle(talent);

            if (talent.data.effects != null)
            {
                foreach (AbilityEffect effect
                         in talent.data.effects)
                {
                    if (effect is not
                        StatModifierEffect statEffect)
                    {
                        continue;
                    }

                    float value =
                        statEffect.value *
                        talent.currentPoints;

                    player.AddModifier(
                        new StatModifier(
                            statEffect.stat,
                            value,
                            statEffect.type,
                            talent,
                            ModifierSourceType.Talent
                        )
                    );
                }
            }
        }

        RefreshWardState();

        return true;
    }

    // =====================================================
    // EFFECT CONTROLLER
    // =====================================================

    private bool EnsureEffectController(
        PlayerStats player)
    {
        CharacterEffectController resolved =
            CharacterEffectController.GetOrCreate(
                player
            );

        if (resolved == null)
            return false;

        if (effectController == resolved)
            return true;

        if (effectController != null)
        {
            effectController.OnEffectsChanged -=
                HandleEffectsChanged;
        }

        effectController = resolved;

        effectController.OnEffectsChanged +=
            HandleEffectsChanged;

        return true;
    }

    // =====================================================
    // EFFECT EVENTS
    // =====================================================

    private void HandleEffectsChanged()
    {
        if (!suppressEffectEvents)
        {
            RefreshWardState();
        }
    }

    // =====================================================
    // WARD STATE
    // =====================================================

    private void RefreshWardState()
    {
        PlayerStats player = Player;

        if (player == null)
            return;

        WardSystem ward =
            player.GetComponent<WardSystem>();

        if (ward == null)
            return;

        int maximum = BaseWardCapacity;

        bool unlockGeneration = false;

        // =================================================
        // TALENT PROGRESSION & LEGACY WARD EFFECTS
        // =================================================

        foreach (TalentRuntime runtime in talents)
        {
            if (runtime?.data == null ||
                runtime.currentPoints <= 0)
            {
                continue;
            }

            if (runtime.data.unlocksWardSystem)
            {
                unlockGeneration = true;
            }

            // Only unmigrated talents use this path.
            if (runtime.data.HasSharedEffects ||
                runtime.data.effects == null)
            {
                continue;
            }

            foreach (AbilityEffect effect
                     in runtime.data.effects)
            {
                if (effect is WardCapacityEffect legacy)
                {
                    maximum +=
                        legacy.wardsPerPoint *
                        runtime.currentPoints;
                }
            }
        }

        // =================================================
        // SHARED WARD EFFECTS
        // =================================================

        if (effectController == null)
        {
            EnsureEffectController(player);
        }

        if (effectController != null)
        {
            WardCapacityQuery query =
                new WardCapacityQuery(player);

            effectController.ApplyTo(query);

            maximum +=
                query.AdditionalWards;
        }

        // =================================================
        // WARD GENERATION
        // =================================================

        if (ward.IsUnlocked != unlockGeneration)
        {
            ward.SetWardGeneration(
                unlockGeneration
            );
        }

        // =================================================
        // WARD CAPACITY
        // =================================================

        if (ward.MaxWard != maximum)
        {
            ward.SetMaxWard(maximum);
        }
    }

    // =====================================================
    // CLEANUP PASSIVE EFFECTS
    // =====================================================

    private void CleanupPassiveEffects()
    {
        if (effectController != null)
        {
            foreach (EffectHandle handle
                     in passiveHandles.Values)
            {
                effectController.Deactivate(
                    handle
                );
            }
        }

        passiveHandles.Clear();
    }

    // =====================================================
    // REMOVE TALENT EFFECT HANDLE
    // =====================================================

    private void RemovePassiveHandle(
        TalentRuntime talent)
    {
        if (!passiveHandles.TryGetValue(
                talent,
                out EffectHandle handle))
        {
            return;
        }

        passiveHandles.Remove(talent);

        if (effectController != null)
        {
            effectController.Deactivate(
                handle
            );
        }
    }

    // =====================================================
    // ABILITY UNLOCKS
    // =====================================================

    private void HandleUnlocks(
        TalentRuntime talent)
    {
        PlayerStats player = Player;

        if (player == null ||
            talent?.data == null ||
            talent.currentPoints != 1)
        {
            return;
        }

        AbilityData unlockedAbility =
            talent.data.unlockedAbility;

        if (unlockedAbility != null)
        {
            PlayerAbilityCollection abilities =
                player.GetComponent<
                    PlayerAbilityCollection>();

            if (abilities == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerAbilityCollection)} " +
                    $"saknas på spelaren. " +
                    $"'{unlockedAbility.abilityName}' " +
                    $"kunde inte låsas upp.",
                    player
                );
            }
            else
            {
                abilities.LearnAbility(
                    unlockedAbility
                );
            }
        }

        SpellbookUI spellbook =
            FindFirstObjectByType<
                SpellbookUI>();

        spellbook?.Refresh();
    }

    // =====================================================
    // CAN LEARN TALENT
    // =====================================================

    public bool CanLearnTalent(
        TalentRuntime talent)
    {
        if (talent?.data == null ||
            availablePoints <= 0 ||
            talent.currentPoints >=
                talent.data.maxPoints)
        {
            return false;
        }

        // =================================================
        // INDIVIDUAL TALENT REQUIREMENTS
        // =================================================

        TalentRequirement[] requirements =
            talent.data.requirements;

        if (requirements != null)
        {
            foreach (TalentRequirement requirement
                     in requirements)
            {
                if (requirement?.talent == null)
                    continue;

                TalentRuntime runtime =
                    talents.Find(
                        candidate =>
                            candidate != null &&
                            candidate.data ==
                                requirement.talent
                    );

                if (runtime == null)
                    return false;

                if (runtime.currentPoints <
                    requirement.requiredPoints)
                {
                    return false;
                }
            }
        }

        // =================================================
        // TIER REQUIREMENTS
        // =================================================

        int talentTier =
            talent.data.tier;

        if (talentTier <= 1)
            return true;

        int requiredPoints =
            GetTierRequirement(talentTier);

        int spentPointsInPreviousTiers = 0;

        foreach (TalentRuntime runtime in talents)
        {
            if (runtime == null ||
                runtime.data == null)
            {
                continue;
            }

            if (runtime.data.tier <
                talentTier)
            {
                spentPointsInPreviousTiers +=
                    runtime.currentPoints;
            }
        }

        return spentPointsInPreviousTiers >=
               requiredPoints;
    }

    // =====================================================
    // VALIDATION
    // =====================================================

#if UNITY_EDITOR

    private void OnValidate()
    {
        availablePoints = Mathf.Max(
            0,
            availablePoints
        );

        allTalents ??=
            new List<TalentData>();

        tierRequirements ??=
            new List<int>();

        for (int i = 0;
             i < tierRequirements.Count;
             i++)
        {
            tierRequirements[i] =
                Mathf.Max(
                    0,
                    tierRequirements[i]
                );
        }
    }

#endif
}
