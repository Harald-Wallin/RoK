using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Talent"
)]
public sealed class TalentData :
    ScriptableObject
{
    // =========================================================
    // BASIC
    // =========================================================

    [Header("Basic")]

    public string talentName;

    public string Id =>
    PersistentIdUtility
        .FromDisplayName(
            DisplayName
        );

    public string DisplayName =>
        string.IsNullOrWhiteSpace(
            talentName
        )
            ? name
            : talentName;

    [TextArea]
    public string description;

    public Sprite icon;

    // =========================================================
    // SCALING
    // =========================================================

    [Header("Scaling")]

    [Min(1)]
    public int maxPoints = 5;

    // =========================================================
    // EFFECTS
    // =========================================================

    [Header("Effects")]

    public AbilityEffect[] effects;

    // =========================================================
    // UNLOCKS
    // =========================================================

    [Header("Unlocks")]

    [Tooltip(
        "Ability som låses upp när första poängen läggs i " +
        "talenten.\n\n" +
        "Kan vara både en vanlig Ability och en BaseAttack. " +
        "AbilityData.UsageType avgör senare vilka slots den " +
        "kan utrustas i."
    )]
    public AbilityData unlockedAbility;

    // =========================================================
    // WARD
    // =========================================================

    [Header("Ward")]

    public bool unlocksWardSystem;

    // =========================================================
    // TIER
    // =========================================================

    [Header("Tier")]

    [Min(1)]
    public int tier = 1;

    // =========================================================
    // REQUIREMENTS
    // =========================================================

    [Header("Requirements")]

    public TalentRequirement[] requirements;

    // =========================================================
    // TOOLTIP
    // =========================================================

    public TooltipData GetTooltipData(
        CharacterStats caster,
        int currentPoints)
    {
        TooltipData data =
            new TooltipData
            {
                title =
                    talentName,

                description =
                    description,

                showFooter =
                    false
            };

        AddCurrentEffectsTooltip(
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

        AddRequirementsTooltip(
            data
        );

        return data;
    }

    // =========================================================
    // CURRENT EFFECTS
    // =========================================================

    private void AddCurrentEffectsTooltip(
        TooltipData data,
        int currentPoints)
    {
        if (data == null ||
            currentPoints <= 0 ||
            effects == null)
        {
            return;
        }

        for (int i = 0;
             i < effects.Length;
             i++)
        {
            AbilityEffect effect =
                effects[i];

            if (effect is not
                StatModifierEffect statEffect)
            {
                continue;
            }

            float value =
                statEffect.value *
                currentPoints;

            data.stats.Add(
                StatFormatting.FormatModifier(
                    statEffect.stat,
                    statEffect.type,
                    value
                )
            );
        }
    }

    // =========================================================
    // ABILITY UNLOCK
    // =========================================================

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

        data.stats.Add(
            string.Empty
        );

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

        for (int i = 0;
             i < abilityTooltip.stats.Count;
             i++)
        {
            string line =
                abilityTooltip.stats[i];

            if (string.IsNullOrEmpty(
                    line))
            {
                continue;
            }

            data.stats.Add(
                line
            );
        }
    }

    // =========================================================
    // NEXT RANK
    // =========================================================

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
            data.stats.Add(
                string.Empty
            );
        }

        data.stats.Add(
            "<color=yellow>" +
            "Next Rank:" +
            "</color>"
        );

        if (effects == null)
            return;

        int nextRank =
            currentPoints + 1;

        for (int i = 0;
             i < effects.Length;
             i++)
        {
            AbilityEffect effect =
                effects[i];

            if (effect is not
                StatModifierEffect statEffect)
            {
                continue;
            }

            float value =
                statEffect.value *
                nextRank;

            data.stats.Add(
                StatFormatting.FormatModifier(
                    statEffect.stat,
                    statEffect.type,
                    value
                )
            );
        }
    }

    // =========================================================
    // REQUIREMENTS
    // =========================================================

    private void AddRequirementsTooltip(
        TooltipData data)
    {
        if (data == null ||
            requirements == null ||
            requirements.Length == 0)
        {
            return;
        }

        data.stats.Add(
            string.Empty
        );

        data.stats.Add(
            "<color=yellow>" +
            "Requirements:" +
            "</color>"
        );

        for (int i = 0;
             i < requirements.Length;
             i++)
        {
            TalentRequirement requirement =
                requirements[i];

            if (requirement == null ||
                requirement.talent == null)
            {
                continue;
            }

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

        return TalentManager
            .Instance
            .talents
            .Find(
                runtime =>
                    runtime != null &&
                    runtime.data ==
                    talent
            );
    }

    // =========================================================
    // VALIDATION
    // =========================================================

#if UNITY_EDITOR

    private void OnValidate()
    {
        maxPoints =
            Mathf.Max(
                1,
                maxPoints
            );

        tier =
            Mathf.Max(
                1,
                tier
            );
    }

#endif
}