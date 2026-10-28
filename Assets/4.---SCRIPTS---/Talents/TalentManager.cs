using System.Collections.Generic;
using UnityEngine;

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

    private PlayerStats Player =>
        PlayerReference.Player;

    private void Awake()
    {
        Instance = this;

        Initialize(
            allTalents
        );
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Initialize(
        List<TalentData> talentData)
    {
        talents.Clear();

        if (talentData == null)
            return;

        foreach (TalentData data in talentData)
        {
            if (data == null)
                continue;

            talents.Add(
                new TalentRuntime(data)
            );
        }
    }

    public int GetTierRequirement(
        int tier)
    {
        if (tier <= 1)
            return 0;

        int index =
            tier - 1;

        if (tierRequirements == null ||
            index < 0 ||
            index >= tierRequirements.Count)
        {
            return int.MaxValue;
        }

        return tierRequirements[index];
    }

    public bool TrySpendPoint(
        TalentRuntime talent)
    {
        if (!CanLearnTalent(talent))
            return false;

        talent.currentPoints++;
        availablePoints--;

        ApplyTalent(
            talent
        );

        HandleUnlocks(
            talent
        );

        return true;
    }

    private void ApplyTalent(
        TalentRuntime talent)
    {
        PlayerStats player =
            Player;

        if (player == null ||
            talent == null ||
            talent.data == null)
        {
            return;
        }

        player.RemoveModifiersFromSource(
            talent
        );

        WardSystem ward =
            player.GetComponent<WardSystem>();

        if (ward != null &&
            talent.data.unlocksWardSystem)
        {
            ward.SetWardGeneration(
                talent.currentPoints > 0
            );
        }

        if (ward != null)
        {
            int maxWard = 5;

            foreach (TalentRuntime runtime in talents)
            {
                if (runtime == null ||
                    runtime.data == null ||
                    runtime.data.effects == null)
                {
                    continue;
                }

                foreach (AbilityEffect effect in
                         runtime.data.effects)
                {
                    if (effect is not
                        WardCapacityEffect wardEffect)
                    {
                        continue;
                    }

                    maxWard +=
                        wardEffect.wardsPerPoint *
                        runtime.currentPoints;
                }
            }

            ward.SetMaxWard(
                maxWard
            );
        }

        if (talent.data.effects == null)
            return;

        foreach (AbilityEffect effect in
                 talent.data.effects)
        {
            if (effect is not
                StatModifierEffect statEffect)
            {
                continue;
            }

            float scaledValue =
                statEffect.value *
                talent.currentPoints;

            player.AddModifier(
                new StatModifier(
                    statEffect.stat,
                    scaledValue,
                    statEffect.type,
                    talent,
                    ModifierSourceType.Talent
                )
            );
        }
    }

    private void HandleUnlocks(
    TalentRuntime talent)
    {
        PlayerStats player =
            Player;

        if (player == null ||
            talent == null ||
            talent.data == null)
        {
            return;
        }

        /*
         * Unlocks appliceras endast när
         * första poängen köps.
         */
        if (talent.currentPoints != 1)
            return;

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
                    $"{nameof(PlayerAbilityCollection)} saknas " +
                    $"på spelaren. '{unlockedAbility.abilityName}' " +
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

    public bool CanLearnTalent(
        TalentRuntime talent)
    {
        if (talent == null ||
            talent.data == null)
        {
            return false;
        }

        if (availablePoints <= 0)
            return false;

        if (talent.currentPoints >=
            talent.data.maxPoints)
        {
            return false;
        }

        TalentRequirement[] requirements =
            talent.data.requirements;

        if (requirements != null)
        {
            foreach (TalentRequirement requirement in
                     requirements)
            {
                if (requirement == null ||
                    requirement.talent == null)
                {
                    continue;
                }

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

        int talentTier =
            talent.data.tier;

        if (talentTier <= 1)
            return true;

        int requiredPoints =
            GetTierRequirement(
                talentTier
            );

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

        return
            spentPointsInPreviousTiers >=
            requiredPoints;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        availablePoints =
            Mathf.Max(
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