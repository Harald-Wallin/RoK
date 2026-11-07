using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName =
        "RPG/Favours/Favour"
)]
public sealed class FavourData :
    ScriptableObject
{
    // =========================================================
    // IDENTITY / PRESENTATION
    // =========================================================

    [SerializeField]
    private string displayName;

    [TextArea(3, 8)]
    [SerializeField]
    private string description;

    [SerializeField]
    private FavourType category =
        FavourType.General;

    // =========================================================
    // FLOW
    // =========================================================

    [Header("Flow")]

    [SerializeField]
    private FavourActivationPolicy
        activationPolicy =
            FavourActivationPolicy
                .ExplicitAccept;

    [SerializeField]
    private FavourCompletionPolicy
        completionPolicy =
            FavourCompletionPolicy
                .ReturnToGiver;

    [SerializeField]
    [Tooltip(
        "Courier-favours har inga vanliga objectives. " +
        "När favourn accepteras blir den omedelbart " +
        "ReadyToTurnIn och kan lämnas in hos sin " +
        "completion target."
    )]
    private bool isCourier;

    [SerializeField]
    [Tooltip(
        "Entity som favourn ska lämnas in till när en " +
        "specifik completion target används.\n\n" +
        "Du kan dra ett scene object eller prefab hit. " +
        "Endast EntityIdentity-ID:t sparas."
    )]
    private EntityReference completionTarget =
        new();

    // =========================================================
    // OBJECTIVES
    // =========================================================

    [Header("Objectives")]

    [SerializeField]
    private List<FavourObjectiveData>
        objectives =
            new();

    // =========================================================
    // DIALOGUE
    // =========================================================

    [Header("Dialogue")]

    [SerializeField]
    [Tooltip(
        "Dialog som normalt används hos den entity " +
        "som erbjuder favourn."
    )]
    private FavourDialogueSet dialogueSet;

    [SerializeField]
    [Tooltip(
        "Dialog som används av ett separat completion target, " +
        "exempelvis Hirdman Fanarik när Master Umfrin " +
        "gav favourn."
    )]
    private FavourDialogueSet
        completionDialogueSet;

    // =========================================================
    // REWARDS
    // =========================================================

    [Header("Rewards")]

    [SerializeField]
    [Min(0)]
    private int experienceReward;

    [SerializeField]
    [Min(0)]
    private int minimumCurrencyReward;

    [SerializeField]
    [Min(0)]
    private int maximumCurrencyReward;

    [SerializeField]
    private List<FavourReputationReward>
        reputationRewards =
            new();

    [SerializeField]
    private List<FavourItemReward>
        itemRewards =
            new();

    [SerializeField]
    private List<FavourAbilityReward>
        abilityRewards =
            new();

    [SerializeField]
    [Tooltip(
        "Varje grupp kräver att spelaren väljer det angivna " +
        "antalet rewards innan favouren kan lämnas in."
    )]
    private List<FavourRewardChoiceGroup>
        rewardChoiceGroups =
            new();

    // =========================================================
    // REQUIREMENTS
    // =========================================================

    [Header("Requirements")]

    [SerializeField]
    [Min(0)]
    [Tooltip(
        "0 innebär att favouren saknar level requirement."
    )]
    private int minimumLevel;

    [SerializeField]
    [Min(0)]
    [Tooltip(
        "0 innebär att favouren saknar currency requirement. " +
        "Valutan förbrukas inte."
    )]
    private int minimumCurrency;

    [SerializeField]
    private List<FavourReputationRequirement>
        reputationRequirements =
            new();

    [SerializeField]
    private List<FavourItemRequirement>
        itemRequirements =
            new();

    [SerializeField]
    [Tooltip(
        "Samtliga angivna favours måste vara completed."
    )]
    private List<FavourData>
        requiredCompletedFavours =
            new();

    [SerializeField]
    [Tooltip(
        "Favouren är otillgänglig om någon angiven favour " +
        "redan är completed."
    )]
    private List<FavourData>
        forbiddenCompletedFavours =
            new();

    // =========================================================
    // FAILURE / REPEATABILITY / FOLLOW-UPS
    // =========================================================

    [Header("Failure")]

    [SerializeField]
    private FavourFailureSettings
        failureSettings =
            new();

    [Header("Repeatability")]

    [SerializeField]
    private FavourRepeatSettings
        repeatSettings =
            new();

    [Header("Follow-ups")]

    [SerializeField]
    [Tooltip(
        "Endast UX-hjälp. Requirements avgör fortfarande " +
        "om follow-up-favouren är tillgänglig."
    )]
    private List<FavourData>
        followUps =
            new();

    [SerializeField]
    [Tooltip(
    "Om favouren accepterades via ett item måste spelaren fortsätta " +
    "äga source-itemet medan favouren är Active eller ReadyToTurnIn.\n\n" +
    "Om itemet säljs, förstörs eller på annat sätt lämnar spelarens " +
    "Inventory + Equipment failar favouren.\n\n" +
    "Påverkar endast Favours som faktiskt accepterades via ett item."
)]
    private bool requireSourceItemWhileActive = true;

    // =========================================================
    // IDENTITY API
    // =========================================================

    public string Id =>
        PersistentIdUtility
            .FromDisplayName(
                DisplayName
            );

    public string DisplayName =>
        string.IsNullOrWhiteSpace(
            displayName
        )
            ? name
            : displayName;

    public string Description =>
        description;

    public FavourType Category =>
        category;

    // =========================================================
    // FLOW API
    // =========================================================

    public FavourActivationPolicy
        ActivationPolicy =>
            activationPolicy;

    public FavourCompletionPolicy
        CompletionPolicy =>
            completionPolicy;

    public bool IsCourier =>
        isCourier;

    public bool UsesSpecificCompletionTarget =>
        completionPolicy ==
            FavourCompletionPolicy
                .CompleteAtTarget ||
        completionPolicy ==
            FavourCompletionPolicy
                .CompleteAtWorldObject;

    public string CompletionTargetId =>
        completionTarget != null
            ? completionTarget.Id
            : string.Empty;

    public string CompletionTargetDisplayName =>
        completionTarget != null
            ? completionTarget.DisplayName
            : string.Empty;

    // =========================================================
    // OBJECTIVE / DIALOGUE API
    // =========================================================

    public IReadOnlyList<
        FavourObjectiveData>
        Objectives =>
            objectives;

    public FavourDialogueSet DialogueSet =>
        dialogueSet;

    public FavourDialogueSet
        CompletionDialogueSet =>
            completionDialogueSet;

    // =========================================================
    // REWARD API
    // =========================================================

    public int ExperienceReward =>
        Mathf.Max(
            0,
            experienceReward
        );

    public int MinimumCurrencyReward =>
        Mathf.Max(
            0,
            minimumCurrencyReward
        );

    public int MaximumCurrencyReward =>
        Mathf.Max(
            MinimumCurrencyReward,
            maximumCurrencyReward
        );

    public IReadOnlyList<
        FavourReputationReward>
        ReputationRewards =>
            reputationRewards;

    public IReadOnlyList<
        FavourItemReward>
        ItemRewards =>
            itemRewards;

    public IReadOnlyList<
        FavourAbilityReward>
        AbilityRewards =>
            abilityRewards;

    public IReadOnlyList<
        FavourRewardChoiceGroup>
        RewardChoiceGroups =>
            rewardChoiceGroups;

    public bool HasRewardChoices =>
        rewardChoiceGroups != null &&
        rewardChoiceGroups.Count > 0;

    // =========================================================
    // REQUIREMENT API
    // =========================================================

    public int MinimumLevel =>
        Mathf.Max(
            0,
            minimumLevel
        );

    public int MinimumCurrency =>
        Mathf.Max(
            0,
            minimumCurrency
        );

    public IReadOnlyList<
        FavourReputationRequirement>
        ReputationRequirements =>
            reputationRequirements;

    public IReadOnlyList<
        FavourItemRequirement>
        ItemRequirements =>
            itemRequirements;

    public IReadOnlyList<FavourData>
        RequiredCompletedFavours =>
            requiredCompletedFavours;

    public IReadOnlyList<FavourData>
        ForbiddenCompletedFavours =>
            forbiddenCompletedFavours;

    // =========================================================
    // OTHER API
    // =========================================================

    public FavourFailureSettings
        FailureSettings =>
            failureSettings;

    public FavourRepeatSettings
        RepeatSettings =>
            repeatSettings;

    public IReadOnlyList<FavourData>
        FollowUps =>
            followUps;

    public bool RequireSourceItemWhileActive =>
        requireSourceItemWhileActive;

#if UNITY_EDITOR

    private void OnValidate()
    {
        experienceReward =
            Mathf.Max(
                0,
                experienceReward
            );

        minimumCurrencyReward =
            Mathf.Max(
                0,
                minimumCurrencyReward
            );

        maximumCurrencyReward =
            Mathf.Max(
                minimumCurrencyReward,
                maximumCurrencyReward
            );

        minimumLevel =
            Mathf.Max(
                0,
                minimumLevel
            );

        minimumCurrency =
            Mathf.Max(
                0,
                minimumCurrency
            );

        completionTarget ??=
            new EntityReference();

        objectives ??=
            new List<FavourObjectiveData>();

        reputationRewards ??=
            new List<
                FavourReputationReward>();

        itemRewards ??=
            new List<FavourItemReward>();

        abilityRewards ??=
            new List<
                FavourAbilityReward>();

        rewardChoiceGroups ??=
            new List<
                FavourRewardChoiceGroup>();

        reputationRequirements ??=
            new List<
                FavourReputationRequirement>();

        itemRequirements ??=
            new List<
                FavourItemRequirement>();

        requiredCompletedFavours ??=
            new List<FavourData>();

        forbiddenCompletedFavours ??=
            new List<FavourData>();

        followUps ??=
            new List<FavourData>();



        foreach (FavourRewardChoiceGroup
                 group
                 in rewardChoiceGroups)
        {
            group?.Normalize();
        }

        foreach (string warning in GetValidationWarnings())
            Debug.LogWarning(warning, this);
    }

    public IEnumerable<string> GetValidationWarnings()
    {
        int objectiveCount = objectives?.Count ?? 0;

        if (!IsCourier && objectiveCount == 0)
            yield return $"Favour '{name}' saknar objectives.";

        if (IsCourier && objectiveCount > 0)
            yield return $"Courier-favour '{name}' har vanliga objectives.";

        if (objectives?.Exists(objective => objective == null) == true)
            yield return $"Favour '{name}' har en tom objective-referens.";

        if (IsCourier && !UsesSpecificCompletionTarget)
        {
            yield return $"Courier-favour '{name}' måste använda " +
                "CompleteAtTarget eller CompleteAtWorldObject.";
        }

        if (UsesSpecificCompletionTarget &&
            string.IsNullOrWhiteSpace(CompletionTargetId))
        {
            yield return $"Favour '{name}' saknar en giltig Completion Target.";
        }

        if (reputationRewards?.Exists(reward => reward?.Faction == null) == true)
            yield return $"Favour '{name}' har en reputation reward utan Faction.";

        if (itemRewards?.Exists(reward => reward?.Item == null) == true)
            yield return $"Favour '{name}' har en item reward utan ItemData.";

        if (abilityRewards?.Exists(reward => reward?.Ability == null) == true)
            yield return $"Favour '{name}' har en ability reward utan AbilityData.";

        if (rewardChoiceGroups != null)
        {
            foreach (FavourRewardChoiceGroup group in rewardChoiceGroups)
            {
                if (group == null)
                    continue;

                int validOptions = 0;

                if (group.Options != null)
                {
                    foreach (FavourRewardChoiceOption option in group.Options)
                    {
                        if (option != null && option.IsValid)
                            validOptions++;
                    }
                }

                if (validOptions < group.ChoicesAllowed)
                {
                    yield return $"Reward-gruppen '{group.DisplayName}' kräver " +
                        $"{group.ChoicesAllowed} val men har bara " +
                        $"{validOptions} giltiga alternativ.";
                }
            }
        }

        if (reputationRequirements?.Exists(
                requirement => requirement?.Faction == null) == true)
        {
            yield return $"Favour '{name}' har ett reputation requirement utan Faction.";
        }

        if (itemRequirements?.Exists(
                requirement => requirement?.Item == null) == true)
        {
            yield return $"Favour '{name}' har ett item requirement utan ItemData.";
        }

        if (requiredCompletedFavours?.Exists(required => required == this) == true)
            yield return $"Favour '{name}' kräver sig själv.";

        if (forbiddenCompletedFavours?.Exists(forbidden => forbidden == this) == true)
            yield return $"Favour '{name}' förbjuder sig själv.";
    }



#endif
}