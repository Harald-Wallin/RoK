
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Talent"
)]
public sealed class TalentData :
    ScriptableObject
{
    // =====================================================
    // BASIC
    // =====================================================

    [Header("Basic")]

    public string talentName;

    public string Id =>
        PersistentIdUtility.FromDisplayName(
            DisplayName
        );

    public string DisplayName =>
        string.IsNullOrWhiteSpace(talentName)
            ? name
            : talentName;

    [TextArea]
    public string description;

    public Sprite icon;

    // =====================================================
    // SCALING
    // =====================================================

    [Header("Scaling")]

    [Min(1)]
    public int maxPoints = 5;

    // =====================================================
    // SHARED PASSIVE EFFECTS
    // =====================================================

    [Header("Shared Passive Effects")]

    [SerializeField]
    [Tooltip(
        "Effects applied permanently while the talent " +
        "has at least one invested point.\n\n" +
        "Each effect receives the current talent points " +
        "as its stack count.\n\n" +
        "Use reusable EffectDefinition assets, " +
        "not legacy AbilityEffect assets."
    )]
    private EffectDefinition[] passiveEffects =
        new EffectDefinition[0];

    public IReadOnlyList<EffectDefinition>
        PassiveEffects => passiveEffects;

    public bool HasSharedEffects =>
        passiveEffects != null &&
        passiveEffects.Length > 0;

    // =====================================================
    // LEGACY EFFECTS
    // =====================================================

    /// <summary>
    /// Keep this field name unchanged.
    ///
    /// Existing talent assets store references
    /// under the serialized name "effects".
    ///
    /// These references are preserved until
    /// migration has been verified.
    /// </summary>
    [HideInInspector]
    public AbilityEffect[] effects;

    // =====================================================
    // ABILITY UNLOCKS
    // =====================================================

    [Header("Unlocks")]

    [Tooltip(
        "Ability unlocked when the first point " +
        "is invested in this talent.\n\n" +
        "The ability is learned but not " +
        "automatically equipped."
    )]
    public AbilityData unlockedAbility;

    // =====================================================
    // WARD SYSTEM
    // =====================================================

    [Header("Ward")]

    [Tooltip(
        "Unlocks Ward generation after purchasing " +
        "the first talent point.\n\n" +
        "Ward capacity bonuses should use " +
        "WardCapacityEffectDefinition."
    )]
    public bool unlocksWardSystem;

    // =====================================================
    // TIER
    // =====================================================

    [Header("Tier")]

    [Min(1)]
    public int tier = 1;

    // =====================================================
    // REQUIREMENTS
    // =====================================================

    [Header("Requirements")]

    public TalentRequirement[] requirements;

    // =====================================================
    // TOOLTIP
    // =====================================================

    public TooltipData GetTooltipData(
        CharacterStats caster,
        int currentPoints)
    {
        TooltipData data =
            new TooltipData
            {
                title = talentName,
                description = description,
                showFooter = false
            };

        AddEffectsTooltip(
            data,
            currentPoints
        );

        AddAbilityUnlockTooltip(
            data,
            caster,
            currentPoints
        );

        AddNextRankTooltip(
            data,
            currentPoints
        );

        AddRequirementsTooltip(data);

        return data;
    }

    // =====================================================
    // CURRENT EFFECTS
    // =====================================================

    private void AddEffectsTooltip(
        TooltipData data,
        int currentPoints)
    {
        if (data == null ||
            currentPoints <= 0)
        {
            return;
        }

        if (HasSharedEffects)
        {
            foreach (
                EffectDefinition effect
                in passiveEffects)
            {
                if (effect == null)
                    continue;

                string text =
                    effect.GetDescriptionForStacks(
                        currentPoints
                    );

                if (!string.IsNullOrWhiteSpace(
                        text))
                {
                    data.stats.Add(text);
                }
            }

            return;
        }

        // Legacy tooltip compatibility.

        if (effects == null)
            return;

        foreach (AbilityEffect effect in effects)
        {
            if (effect is not
                StatModifierEffect statEffect)
            {
                continue;
            }

            data.stats.Add(
                StatFormatting.FormatModifier(
                    statEffect.stat,
                    statEffect.type,
                    statEffect.value *
                    currentPoints
                )
            );
        }
    }

    // =====================================================
    // ABILITY UNLOCK TOOLTIP
    // =====================================================

    private void AddAbilityUnlockTooltip(
        TooltipData data,
        CharacterStats caster,
        int currentPoints)
    {
        if (data == null ||
            unlockedAbility == null)
        {
            return;
        }

        string prefix =
            currentPoints > 0
                ? "Unlocked"
                : "Unlocks";

        data.stats.Add(
            $"<color=#66FFAA>" +
            $"{prefix}: " +
            $"{unlockedAbility.abilityName}" +
            $"</color>"
        );

        data.stats.Add(string.Empty);

        TooltipData abilityTooltip =
            unlockedAbility.GetTooltipData(
                caster
            );

        if (abilityTooltip == null)
            return;

        if (!string.IsNullOrEmpty(
                abilityTooltip.description))
        {
            data.stats.Add(
                abilityTooltip.description
            );
        }

        if (abilityTooltip.stats == null)
            return;

        foreach (
            string line
            in abilityTooltip.stats)
        {
            if (!string.IsNullOrEmpty(line))
            {
                data.stats.Add(line);
            }
        }
    }

    // =====================================================
    // NEXT RANK TOOLTIP
    // =====================================================

    private void AddNextRankTooltip(
        TooltipData data,
        int currentPoints)
    {
        if (data == null ||
            currentPoints >= maxPoints)
        {
            return;
        }

        if (currentPoints > 0)
        {
            data.stats.Add(string.Empty);
        }

        data.stats.Add(
            "<color=yellow>" +
            "Next Rank:" +
            "</color>"
        );

        int nextRank =
            currentPoints + 1;

        if (HasSharedEffects)
        {
            foreach (
                EffectDefinition effect
                in passiveEffects)
            {
                if (effect == null)
                    continue;

                string text =
                    effect.GetDescriptionForStacks(
                        nextRank
                    );

                if (!string.IsNullOrWhiteSpace(
                        text))
                {
                    data.stats.Add(text);
                }
            }

            return;
        }

        // Legacy preview compatibility.

        if (effects == null)
            return;

        foreach (AbilityEffect effect in effects)
        {
            if (effect is not
                StatModifierEffect statEffect)
            {
                continue;
            }

            data.stats.Add(
                StatFormatting.FormatModifier(
                    statEffect.stat,
                    statEffect.type,
                    statEffect.value * nextRank
                )
            );
        }
    }

    // =====================================================
    // REQUIREMENTS TOOLTIP
    // =====================================================

    private void AddRequirementsTooltip(
        TooltipData data)
    {
        if (data == null ||
            requirements == null ||
            requirements.Length == 0)
        {
            return;
        }

        data.stats.Add(string.Empty);

        data.stats.Add(
            "<color=yellow>" +
            "Requirements:" +
            "</color>"
        );

        foreach (
            TalentRequirement requirement
            in requirements)
        {
            if (requirement?.talent == null)
                continue;

            TalentRuntime runtime =
                FindTalentRuntime(
                    requirement.talent
                );

            int current =
                runtime != null
                    ? runtime.currentPoints
                    : 0;

            bool fulfilled =
                current >=
                requirement.requiredPoints;

            string color =
                fulfilled
                    ? "#66FF66"
                    : "#FF6666";

            data.stats.Add(
                $"<color={color}>" +
                $"{requirement.talent.talentName} " +
                $"({current}/" +
                $"{requirement.requiredPoints})" +
                $"</color>"
            );
        }
    }

    // =====================================================
    // RUNTIME LOOKUP
    // =====================================================

    private static TalentRuntime
        FindTalentRuntime(
            TalentData talent)
    {
        if (talent == null ||
            TalentManager.Instance == null ||
            TalentManager.Instance.talents == null)
        {
            return null;
        }

        return TalentManager.Instance
            .talents
            .Find(
                runtime =>
                    runtime != null &&
                    runtime.data == talent
            );
    }

    // =====================================================
    // VALIDATION
    // =====================================================

#if UNITY_EDITOR

    private void OnValidate()
    {
        maxPoints = Mathf.Max(
            1,
            maxPoints
        );

        tier = Mathf.Max(
            1,
            tier
        );
    }

#endif
}
