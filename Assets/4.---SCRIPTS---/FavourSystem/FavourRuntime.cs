using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class FavourRuntime
{

    private readonly List<
        FavourObjectiveRuntime>
        objectives =
            new();

    private readonly List<FavourItemCost>
        rawCompletionCosts =
            new();

    private readonly List<FavourItemCost>
        mergedCompletionCosts =
            new();

    private readonly List<InventoryItemAmount>
        inventoryRemovals =
            new();

    private readonly List<InventoryItemAmount>
        rawInventoryRewards =
            new();

    private readonly List<InventoryItemAmount>
        mergedInventoryRewards =
            new();

    private readonly List<HashSet<int>>
        selectedRewardOptions =
            new();

    private readonly List<
        FavourRewardChoiceRuntime>
        rewardChoices =
            new();

    private readonly List<InventoryItemAmount>
    activationInventoryAdditions =
        new();

    private int
    rolledCurrencyReward;

    private float
        cooldownRemaining;

    private bool
        hasBeenCompleted;

    private bool
        isFinalizingCompletion;

    private ItemData sourceItem;

    // =========================================================
    // CORE STATE API
    // =========================================================

    public ItemData SourceItem =>
    sourceItem;

    public bool HasSourceItem =>
        sourceItem != null;

    public bool RequiresSourceItemWhileActive =>
        sourceItem != null &&
        Data != null &&
        Data.RequireSourceItemWhileActive;

    public bool IsFinalizingCompletion =>
        isFinalizingCompletion;

    public bool AreObjectivesComplete =>
        AreAllObjectivesComplete();

    public bool CanAccept =>
        State == FavourState.Available &&
        AreRequirementsMet() &&
        HasValidObjectiveConfiguration;

    public bool CanTurnIn =>
        State == FavourState.ReadyToTurnIn &&
        AreAllObjectivesComplete() &&
        AreRequirementsMet() &&
        ValidateRewardSelections();

    public bool CanChangeRewardSelections =>
        State == FavourState.Active ||
        State == FavourState.ReadyToTurnIn;

    public bool RequiresReturnToGiver =>
        CompletionPolicy ==
        FavourCompletionPolicy.ReturnToGiver;

    public bool ShouldShowReturnToGiverObjective =>
        RequiresReturnToGiver &&
        State != FavourState.Completed &&
        State != FavourState.Failed &&
        State != FavourState.Cooldown;

    public bool UsesSpecificCompletionTarget =>
        Data != null &&
        Data.UsesSpecificCompletionTarget;

    public bool IsCourier =>
        Data != null &&
        Data.IsCourier;

    public string CompletionTargetId =>
        Data != null
            ? Data.CompletionTargetId
            : string.Empty;

    public string CompletionTargetDisplayName =>
        Data != null
            ? Data.CompletionTargetDisplayName
            : string.Empty;

    public bool ShouldShowTurnInObjective =>
        CompletionPolicy !=
            FavourCompletionPolicy.Automatic &&
        State !=
            FavourState.Completed &&
        State !=
            FavourState.Failed &&
        State !=
            FavourState.Cooldown;

    public bool HasBeenCompleted =>
    hasBeenCompleted;

    public float CooldownRemaining =>
        Mathf.Max(
            0f,
            cooldownRemaining
        );

    // =========================================================
    // CONSTRUCTION
    // =========================================================

    public FavourRuntime(
        FavourData data,
        PlayerFavourManager manager)
    {
        Data = data;
        Manager = manager;

        rolledCurrencyReward =
            RollCurrencyReward();

        BuildObjectives();
        BuildRewardChoiceState();

        State =
            FavourState.Unavailable;
    }

    private bool HasValidObjectiveConfiguration
    {
        get
        {
            if (!IsCourier)
            {
                return
                    objectives.Count > 0;
            }

            return
                UsesSpecificCompletionTarget &&
                !string.IsNullOrWhiteSpace(
                    CompletionTargetId
                );
        }
    }

    private int RollCurrencyReward()
    {
        if (Data == null)
            return 0;

        int minimum =
            Data.MinimumCurrencyReward;

        int maximum =
            Data.MaximumCurrencyReward;

        if (maximum <= 0)
            return 0;

        return UnityEngine.Random.Range(
            minimum,
            maximum + 1
        );
    }

    public FavourData Data
    {
        get;
    }

    public PlayerFavourManager Manager
    {
        get;
    }

    public FavourState State
    {
        get;
        private set;
    }

    public IReadOnlyList<
        FavourObjectiveRuntime>
        Objectives =>
            objectives;

    public bool AreRewardChoicesComplete =>
        ValidateRewardSelections();

    public event Action<FavourRuntime>
        StateChanged;

    public event Action<FavourRuntime>
        ProgressChanged;

    public event Action<
    FavourRuntime,
    FavourObjectiveRuntime>
    ObjectiveProgressChanged;

    public event Action<FavourRuntime>
        RewardSelectionChanged;

    // =========================================================
    // BUILD
    // =========================================================

    private void BuildObjectives()
    {
        objectives.Clear();

        if (Data?.Objectives == null)
            return;

        foreach (FavourObjectiveData data
                 in Data.Objectives)
        {
            if (data == null)
                continue;

            FavourObjectiveRuntime runtime =
                data.CreateRuntime(
                    this
                );

            if (runtime == null)
                continue;

            runtime.ProgressChanged +=
                HandleObjectiveProgressChanged;

            objectives.Add(
                runtime
            );
        }
    }

    private void BuildRewardChoiceState()
    {
        selectedRewardOptions.Clear();
        rewardChoices.Clear();

        IReadOnlyList<
            FavourRewardChoiceGroup>
            groups =
                Data?.RewardChoiceGroups;

        if (groups == null)
            return;

        for (int groupIndex = 0;
             groupIndex < groups.Count;
             groupIndex++)
        {
            selectedRewardOptions.Add(
                new HashSet<int>()
            );

            FavourRewardChoiceGroup group =
                groups[groupIndex];

            if (group == null)
                continue;

            rewardChoices.Add(
                new FavourRewardChoiceRuntime(
                    this,
                    group,
                    groupIndex
                )
            );
        }
    }

    // =========================================================
    // AVAILABILITY / REQUIREMENTS
    // =========================================================

    public void RefreshAvailability()
    {
        if (State !=
                FavourState.Unavailable &&
            State !=
                FavourState.Available)
        {
            return;
        }

        SetState(
            AreRequirementsMet()
                ? FavourState.Available
                : FavourState.Unavailable
        );
    }

    public bool AreRequirementsMet()
    {
        if (Data == null ||
            Manager == null)
        {
            return false;
        }

        if (!IsLevelRequirementMet())
            return false;

        if (!IsCurrencyRequirementMet())
            return false;

        if (!AreReputationRequirementsMet())
            return false;

        if (!AreItemRequirementsMet())
            return false;

        if (!AreCompletedFavourRequirementsMet())
            return false;

        return true;
    }

    private bool IsLevelRequirementMet()
    {
        return IsMinimumLevelMet;
    }

    private bool IsCurrencyRequirementMet()
    {
        if (Data.MinimumCurrency <= 0)
            return true;

        PlayerCurrency currency =
            Manager.PlayerCurrency;

        return currency != null &&
               currency.HasCoins(
                   Data.MinimumCurrency
               );
    }

    private bool AreReputationRequirementsMet()
    {
        IReadOnlyList<
            FavourReputationRequirement>
            requirements =
                Data.ReputationRequirements;

        if (requirements == null ||
            requirements.Count == 0)
        {
            return true;
        }

        PlayerReputationManager reputationManager =
            Manager.PlayerReputation;

        if (reputationManager == null)
            return false;

        foreach (FavourReputationRequirement
                 requirement
                 in requirements)
        {
            if (requirement == null)
                continue;

            if (requirement.Faction == null)
                return false;

            FactionReputationData reputation =
                reputationManager
                    .GetReputation(
                        requirement.Faction
                    );

            bool discovered =
                reputation != null &&
                reputation.discovered;

            if (requirement.RequireDiscovered &&
                !discovered)
            {
                return false;
            }

            int currentLevel =
                reputation != null
                    ? Mathf.Max(
                        1,
                        reputation.level
                    )
                    : 0;

            bool met;

            switch (requirement.Comparison)
            {
                case FavourReputationComparison.AtLeast:

                    met =
                        currentLevel >=
                        requirement.RequiredLevel;

                    break;

                case FavourReputationComparison.AtMost:

                    met =
                        currentLevel <=
                        requirement.RequiredLevel;

                    break;

                case FavourReputationComparison.Exactly:

                    met =
                        currentLevel ==
                        requirement.RequiredLevel;

                    break;

                default:

                    met = false;
                    break;
            }

            if (!met)
                return false;
        }

        return true;
    }

    private bool AreItemRequirementsMet()
    {
        IReadOnlyList<
            FavourItemRequirement>
            requirements =
                Data.ItemRequirements;

        if (requirements == null ||
            requirements.Count == 0)
        {
            return true;
        }

        PlayerItemOwnership items =
            Manager?.PlayerItems;

        if (items == null)
            return false;

        foreach (FavourItemRequirement
                 requirement
                 in requirements)
        {
            if (requirement == null)
                continue;

            if (requirement.Item == null)
                return false;

            if (items.GetItemCount(
                    requirement.Item) <
                requirement.Amount)
            {
                return false;
            }
        }

        return true;
    }

    private bool AreCompletedFavourRequirementsMet()
    {
        IReadOnlyList<FavourData>
            requiredFavours =
                Data.RequiredCompletedFavours;

        if (requiredFavours != null)
        {
            foreach (FavourData required
                     in requiredFavours)
            {
                if (required == null)
                    continue;

                if (!Manager.IsCompleted(
                        required))
                {
                    return false;
                }
            }
        }

        IReadOnlyList<FavourData>
            forbiddenFavours =
                Data.ForbiddenCompletedFavours;

        if (forbiddenFavours != null)
        {
            foreach (FavourData forbidden
                     in forbiddenFavours)
            {
                if (forbidden == null)
                    continue;

                if (Manager.IsCompleted(
                        forbidden))
                {
                    return false;
                }
            }
        }

        return true;
    }
    public void BindSourceItem(
    ItemData item)
    {
        sourceItem = item;
    }

    private void ClearSourceItem()
    {
        sourceItem = null;
    }

    public bool ValidateSourceItemOwnership()
    {
        if (!RequiresSourceItemWhileActive)
            return true;

        if (State != FavourState.Active &&
            State != FavourState.ReadyToTurnIn)
        {
            return true;
        }

        /*
         * Completion kan legitimt konsumera source-itemet.
         * Ownership-eventet från den transaktionen får därför
         * inte faila favouren mitt under completion.
         */
        if (isFinalizingCompletion)
            return true;

        PlayerItemOwnership items =
            Manager?.PlayerItems;

        if (items == null)
        {
            Debug.LogError(
                $"Favour '{DisplayName}' kräver sitt source-item, " +
                $"men PlayerItemOwnership saknas."
            );

            return true;
        }

        if (items.Contains(sourceItem))
            return true;

        TryFail();

        return false;
    }


    // =========================================================
    // ACTIVATION / COMPLETION
    // =========================================================

    public bool TryActivate()
    {
        RefreshAvailability();

        if (State !=
            FavourState.Available)
        {
            return false;
        }

        if (!HasValidObjectiveConfiguration)
        {
            return false;
        }

        /*
         * Delivery-objectives kan behöva ge spelaren items
         * vid accept.
         *
         * Hela utdelningen sker atomiskt innan favourn
         * faktiskt aktiveras.
         */
        if (!TryGrantActivationItems())
        {
            return false;
        }

        SetState(
            FavourState.Active
        );

        if (IsCourier)
        {
            SetState(
                FavourState.ReadyToTurnIn
            );

            return true;
        }

        ActivateObjectives();

        ProgressChanged?.Invoke(
            this
        );

        EvaluateCompletion();

        return true;
    }

    private bool TryGrantActivationItems()
    {
        activationInventoryAdditions.Clear();

        Inventory inventory =
            ResolveInventory();

        bool hasDeliveryObjective =
            false;

        foreach (FavourObjectiveRuntime objective
                 in objectives)
        {
            if (objective is not
                DeliverObjectiveRuntime deliver)
            {
                continue;
            }

            hasDeliveryObjective =
                true;

            if (inventory == null)
            {
                Debug.LogError(
                    $"Favour '{DisplayName}' har ett Deliver-objective, " +
                    $"men inget Inventory kunde hittas."
                );

                return false;
            }

            deliver.CollectMissingStartingItems(
                activationInventoryAdditions,
                inventory
            );
        }

        if (!hasDeliveryObjective)
            return true;

        if (activationInventoryAdditions.Count == 0)
            return true;

        return inventory.TryApplyTransaction(
            Array.Empty<InventoryItemAmount>(),
            activationInventoryAdditions,
            notifyIfInventoryFull: true
        );
    }

    public bool TryTurnIn()
    {
        if (State !=
            FavourState.ReadyToTurnIn)
        {
            return false;
        }

        if (!AreAllObjectivesComplete())
        {
            SetState(
                FavourState.Active
            );

            ActivateObjectives();

            return false;
        }

        return TryFinalizeCompletion();
    }

    private void EvaluateCompletion()
    {
        if (State != FavourState.Active &&
            State != FavourState.ReadyToTurnIn)
        {
            return;
        }

        bool allComplete =
            AreAllObjectivesComplete();

        if (!allComplete)
        {
            if (State ==
                FavourState.ReadyToTurnIn)
            {
                SetState(
                    FavourState.Active
                );
            }

            return;
        }

        /*
         * Automatic favours kan endast slutföras automatiskt
         * när samtliga eventuella reward-val redan har gjorts.
         */
        if (Data.CompletionPolicy ==
                FavourCompletionPolicy.Automatic &&
            ValidateRewardSelections())
        {
            if (TryFinalizeCompletion())
                return;
        }

        SetState(
            FavourState.ReadyToTurnIn
        );
    }

    private bool AreAllObjectivesComplete()
    {
        if (objectives.Count == 0)
        {
            return IsCourier;
        }

        foreach (FavourObjectiveRuntime objective
                 in objectives)
        {
            if (objective == null ||
                !objective.IsComplete)
            {
                return false;
            }
        }

        return true;
    }

    private static void MergeInventoryAmount(
    List<InventoryItemAmount> amounts,
    ItemData item,
    int amount)
    {
        if (amounts == null ||
            item == null ||
            amount <= 0)
        {
            return;
        }

        for (int i = 0;
             i < amounts.Count;
             i++)
        {
            InventoryItemAmount existing =
                amounts[i];

            if (!Inventory.ItemsMatch(
                    existing.Item,
                    item))
            {
                continue;
            }

            amounts[i] =
                new InventoryItemAmount(
                    existing.Item,
                    existing.Amount +
                    amount
                );

            return;
        }

        amounts.Add(
            new InventoryItemAmount(
                item,
                amount
            )
        );
    }

    private bool TryFinalizeCompletion()
    {
        if (State ==
            FavourState.Completed)
        {
            return false;
        }

        if (!AreAllObjectivesComplete())
            return false;

        /*
         * Requirements kontrolleras igen vid turn-in.
         */
        if (!AreRequirementsMet())
            return false;

        if (!ValidateRewardSelections())
            return false;

        if (!CanGrantNonInventoryRewards())
            return false;

        BuildMergedCompletionCosts();
        BuildInventoryRemovals();
        BuildMergedInventoryRewards();

        PlayerItemOwnership items =
    Manager?.PlayerItems;

        if (!CanPayCompletionCosts(
                items))
        {
            return false;
        }

        bool requiresItemTransaction =
            inventoryRemovals.Count > 0 ||
            mergedInventoryRewards.Count > 0;

        if (requiresItemTransaction &&
            items == null)
        {
            Debug.LogError(
                $"Favour '{Data?.DisplayName}' kräver player items, " +
                $"men inget PlayerItemOwnership kunde hittas."
            );

            return false;
        }

        isFinalizingCompletion =
            true;

        try
        {
            if (requiresItemTransaction)
            {
                bool transactionSucceeded =
                    items.TryApplyTransaction(
                        inventoryRemovals,
                        mergedInventoryRewards,
                        notifyIfInventoryFull: true
                    );

                if (!transactionSucceeded)
                {
                    return false;
                }
            }

            GrantNonInventoryRewards();

            Complete();

            return true;
        }
        finally
        {
            isFinalizingCompletion =
                false;
        }
    }

    private void Complete()
    {
        if (State ==
                FavourState.Completed ||
            State ==
                FavourState.Cooldown)
        {
            return;
        }

        DeactivateObjectives();

        ClearSourceItem();

        hasBeenCompleted =
            true;

        SetState(
            FavourState.Completed
        );

        RegisterFollowUps();

        FavourRepeatSettings repeat =
            Data?.RepeatSettings;

        if (repeat == null ||
            !repeat.Repeatable)
        {
            return;
        }

        BeginCooldown(
            repeat.RepeatCooldownSeconds
        );
    }

    // =========================================================
    // OBJECTIVES
    // =========================================================

    internal void HandleCharacterDefeated(
        CharacterDefeatedResult result)
    {
        if (State !=
            FavourState.Active)
        {
            return;
        }

        foreach (FavourObjectiveRuntime objective
                 in objectives)
        {
            objective.HandleCharacterDefeated(
                result
            );
        }
    }

    private void HandleObjectiveProgressChanged(
    FavourObjectiveRuntime objective)
    {
        ProgressChanged?.Invoke(
            this
        );

        ObjectiveProgressChanged?.Invoke(
            this,
            objective
        );

        if (isFinalizingCompletion)
            return;

        EvaluateCompletion();
    }

    private void ActivateObjectives()
    {
        foreach (FavourObjectiveRuntime objective
                 in objectives)
        {
            objective?.Activate();
        }
    }

    private void DeactivateObjectives()
    {
        foreach (FavourObjectiveRuntime objective
                 in objectives)
        {
            objective?.Deactivate();
        }
    }

    public void ResetObjectives()
    {
        foreach (FavourObjectiveRuntime objective
                 in objectives)
        {
            objective?.ResetProgress();
        }

        ClearRewardSelections();

        EvaluateCompletion();
    }

    // =========================================================
    // REWARD CHOICES
    // =========================================================

    public bool SetRewardChoiceSelected(
        int groupIndex,
        int optionIndex,
        bool selected)
    {
        if (!CanChangeRewardSelections)
            return false;

        if (!TryGetRewardChoiceGroup(
                groupIndex,
                out FavourRewardChoiceGroup group))
        {
            return false;
        }

        if (group.Options == null ||
            optionIndex < 0 ||
            optionIndex >=
            group.Options.Count)
        {
            return false;
        }

        FavourRewardChoiceOption option =
            group.Options[optionIndex];

        if (option == null ||
            !option.IsValid)
        {
            return false;
        }

        int choicesAllowed =
            Mathf.Max(
                0,
                group.ChoicesAllowed
            );

        if (choicesAllowed <= 0)
            return false;

        HashSet<int> selections =
            selectedRewardOptions[
                groupIndex
            ];

        bool changed = false;

        if (selected)
        {
            if (selections.Contains(
                    optionIndex))
            {
                return true;
            }

            if (choicesAllowed == 1)
            {
                if (selections.Count > 0)
                {
                    selections.Clear();
                    changed = true;
                }

                if (selections.Add(
                        optionIndex))
                {
                    changed = true;
                }
            }
            else
            {
                if (selections.Count >=
                    choicesAllowed)
                {
                    return false;
                }

                changed =
                    selections.Add(
                        optionIndex
                    );
            }
        }
        else
        {
            changed =
                selections.Remove(
                    optionIndex
                );
        }

        if (!changed)
            return false;

        RewardSelectionChanged?.Invoke(
            this
        );

        ProgressChanged?.Invoke(
            this
        );

        if (!isFinalizingCompletion)
        {
            EvaluateCompletion();
        }

        return true;
    }

    public bool IsRewardChoiceSelected(
        int groupIndex,
        int optionIndex)
    {
        if (groupIndex < 0 ||
            groupIndex >=
            selectedRewardOptions.Count)
        {
            return false;
        }

        return selectedRewardOptions[
            groupIndex
        ].Contains(
            optionIndex
        );
    }

    public int GetSelectedRewardCount(
        int groupIndex)
    {
        if (groupIndex < 0 ||
            groupIndex >=
            selectedRewardOptions.Count)
        {
            return 0;
        }

        return selectedRewardOptions[
            groupIndex
        ].Count;
    }

    public void ClearRewardSelections()
    {
        bool changed = false;

        foreach (HashSet<int> selections
                 in selectedRewardOptions)
        {
            if (selections.Count == 0)
                continue;

            selections.Clear();
            changed = true;
        }

        if (!changed)
            return;

        RewardSelectionChanged?.Invoke(
            this
        );

        ProgressChanged?.Invoke(
            this
        );

        if (!isFinalizingCompletion)
        {
            EvaluateCompletion();
        }
    }

    private bool ValidateRewardSelections()
    {
        IReadOnlyList<
            FavourRewardChoiceGroup>
            groups =
                Data?.RewardChoiceGroups;

        if (groups == null ||
            groups.Count == 0)
        {
            return true;
        }

        if (selectedRewardOptions.Count !=
            groups.Count)
        {
            return false;
        }

        for (int groupIndex = 0;
             groupIndex < groups.Count;
             groupIndex++)
        {
            FavourRewardChoiceGroup group =
                groups[groupIndex];

            if (group == null ||
                group.Options == null)
            {
                return false;
            }

            HashSet<int> selections =
                selectedRewardOptions[
                    groupIndex
                ];

            if (selections.Count !=
                group.ChoicesAllowed)
            {
                return false;
            }

            foreach (int optionIndex
                     in selections)
            {
                if (optionIndex < 0 ||
                    optionIndex >=
                    group.Options.Count)
                {
                    return false;
                }

                FavourRewardChoiceOption option =
                    group.Options[
                        optionIndex
                    ];

                if (option == null ||
                    !option.IsValid)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool TryGetRewardChoiceGroup(
        int groupIndex,
        out FavourRewardChoiceGroup group)
    {
        group = null;

        IReadOnlyList<
            FavourRewardChoiceGroup>
            groups =
                Data?.RewardChoiceGroups;

        if (groups == null ||
            groupIndex < 0 ||
            groupIndex >= groups.Count ||
            groupIndex >=
            selectedRewardOptions.Count)
        {
            return false;
        }

        group =
            groups[groupIndex];

        return group != null;
    }

    // =========================================================
    // INVENTORY TRANSACTION
    // =========================================================

    private void BuildMergedCompletionCosts()
    {
        rawCompletionCosts.Clear();
        mergedCompletionCosts.Clear();

        foreach (FavourObjectiveRuntime objective
                 in objectives)
        {
            objective?.CollectTurnInCosts(
                rawCompletionCosts
            );
        }

        IReadOnlyList<
            FavourItemRequirement>
            itemRequirements =
                Data?.ItemRequirements;

        if (itemRequirements != null)
        {
            foreach (FavourItemRequirement
                     requirement
                     in itemRequirements)
            {
                if (requirement == null ||
                    !requirement
                        .ConsumeOnCompletion ||
                    requirement.Item == null ||
                    requirement.Amount <= 0)
                {
                    continue;
                }

                rawCompletionCosts.Add(
                    new FavourItemCost(
                        requirement.Item,
                        requirement.Amount
                    )
                );
            }
        }

        foreach (FavourItemCost rawCost
                 in rawCompletionCosts)
        {
            if (rawCost.Item == null ||
                rawCost.Amount <= 0)
            {
                continue;
            }

            MergeCompletionCost(
                rawCost.Item,
                rawCost.Amount
            );
        }
    }

    private void MergeCompletionCost(
        ItemData item,
        int amount)
    {
        for (int i = 0;
             i < mergedCompletionCosts.Count;
             i++)
        {
            FavourItemCost existing =
                mergedCompletionCosts[i];

            if (!Inventory.ItemsMatch(
                    existing.Item,
                    item))
            {
                continue;
            }

            mergedCompletionCosts[i] =
                new FavourItemCost(
                    existing.Item,
                    existing.Amount +
                    amount
                );

            return;
        }

        mergedCompletionCosts.Add(
            new FavourItemCost(
                item,
                amount
            )
        );
    }

    private void BuildInventoryRemovals()
    {
        inventoryRemovals.Clear();

        foreach (FavourItemCost cost
                 in mergedCompletionCosts)
        {
            if (cost.Item == null ||
                cost.Amount <= 0)
            {
                continue;
            }

            inventoryRemovals.Add(
                new InventoryItemAmount(
                    cost.Item,
                    cost.Amount
                )
            );
        }
    }

    private void BuildMergedInventoryRewards()
    {
        rawInventoryRewards.Clear();
        mergedInventoryRewards.Clear();

        CollectFixedItemRewards();
        CollectSelectedItemRewards();

        foreach (InventoryItemAmount reward
                 in rawInventoryRewards)
        {
            if (!reward.IsValid)
                continue;

            MergeInventoryReward(
                reward.Item,
                reward.Amount
            );
        }
    }

    private void CollectFixedItemRewards()
    {
        IReadOnlyList<FavourItemReward>
            rewards =
                Data?.ItemRewards;

        if (rewards == null)
            return;

        foreach (FavourItemReward reward
                 in rewards)
        {
            if (reward == null ||
                reward.Item == null ||
                reward.Amount <= 0)
            {
                continue;
            }

            rawInventoryRewards.Add(
                new InventoryItemAmount(
                    reward.Item,
                    reward.Amount
                )
            );
        }
    }

    private void CollectSelectedItemRewards()
    {
        ForEachSelectedReward(
            option =>
            {
                if (option.Type !=
                    FavourRewardChoiceType.Item)
                {
                    return;
                }

                if (option.Item == null ||
                    option.ItemAmount <= 0)
                {
                    return;
                }

                rawInventoryRewards.Add(
                    new InventoryItemAmount(
                        option.Item,
                        option.ItemAmount
                    )
                );
            }
        );
    }

    private void MergeInventoryReward(
        ItemData item,
        int amount)
    {
        for (int i = 0;
             i < mergedInventoryRewards.Count;
             i++)
        {
            InventoryItemAmount existing =
                mergedInventoryRewards[i];

            if (!Inventory.ItemsMatch(
                    existing.Item,
                    item))
            {
                continue;
            }

            mergedInventoryRewards[i] =
                new InventoryItemAmount(
                    existing.Item,
                    existing.Amount +
                    amount
                );

            return;
        }

        mergedInventoryRewards.Add(
            new InventoryItemAmount(
                item,
                amount
            )
        );
    }

    private bool CanPayCompletionCosts(
    PlayerItemOwnership items)
    {
        if (mergedCompletionCosts.Count == 0)
            return true;

        if (items == null)
            return false;

        foreach (FavourItemCost cost
                 in mergedCompletionCosts)
        {
            if (cost.Item == null ||
                cost.Amount <= 0)
            {
                continue;
            }

            if (items.GetItemCount(
                    cost.Item) <
                cost.Amount)
            {
                return false;
            }
        }

        return true;
    }

    // =========================================================
    // NON-INVENTORY REWARDS
    // =========================================================

    private bool CanGrantNonInventoryRewards()
    {
        if (Data.ExperienceReward > 0 &&
            Manager?.Player == null)
        {
            Debug.LogError(
                $"Favour '{Data.DisplayName}' delar ut XP, " +
                $"men PlayerStats saknas."
            );

            return false;
        }

        if (CurrencyReward > 0 &&
            Manager?.PlayerCurrency == null)
        {
            Debug.LogError(
                $"Favour '{Data.DisplayName}' delar ut valuta, " +
                $"men PlayerCurrency saknas."
            );

            return false;
        }

        if (!CanGrantReputationRewards())
            return false;

        if (!CanGrantAbilityRewards())
            return false;

        return true;
    }

    private bool CanGrantReputationRewards()
    {
        IReadOnlyList<
            FavourReputationReward>
            rewards =
                Data.ReputationRewards;

        if (rewards == null)
            return true;

        foreach (FavourReputationReward reward
                 in rewards)
        {
            if (reward == null ||
                reward.Faction == null ||
                reward.Amount == 0)
            {
                continue;
            }

            if (Manager?.PlayerReputation == null)
            {
                Debug.LogError(
                    $"Favour '{Data.DisplayName}' delar ut " +
                    $"reputation, men PlayerReputationManager saknas."
                );

                return false;
            }

            if (Manager.PlayerReputation
                    .LevelDefinition == null)
            {
                Debug.LogError(
                    $"Favour '{Data.DisplayName}' delar ut " +
                    $"reputation, men ReputationLevelDefinition saknas."
                );

                return false;
            }
        }

        return true;
    }

    private bool CanGrantAbilityRewards()
    {
        IReadOnlyList<FavourAbilityReward>
            fixedRewards =
                Data.AbilityRewards;

        if (fixedRewards != null)
        {
            foreach (FavourAbilityReward reward
                     in fixedRewards)
            {
                if (reward == null ||
                    reward.Ability == null)
                {
                    continue;
                }

                if (!Manager.CanGrantAbility(
                        reward.Ability))
                {
                    return false;
                }
            }
        }

        bool canGrantSelected =
            true;

        ForEachSelectedReward(
            option =>
            {
                if (!canGrantSelected ||
                    option.Type !=
                    FavourRewardChoiceType.Ability)
                {
                    return;
                }

                if (option.Ability == null ||
                    !Manager.CanGrantAbility(
                        option.Ability))
                {
                    canGrantSelected = false;
                }
            }
        );

        return canGrantSelected;
    }

    private void GrantNonInventoryRewards()
    {
        GrantExperience();
        GrantCurrency();
        GrantReputation();
        GrantAbilities();
    }

    private void GrantExperience()
    {
        if (Data.ExperienceReward <= 0)
            return;

        Manager.Player.GainExp(
            Data.ExperienceReward
        );
    }

    private void GrantCurrency()
    {
        if (CurrencyReward <= 0)
            return;

        Manager.PlayerCurrency.AddCoins(
            CurrencyReward
        );
    }

    private void GrantReputation()
    {
        IReadOnlyList<
            FavourReputationReward>
            rewards =
                Data.ReputationRewards;

        if (rewards == null)
            return;

        foreach (FavourReputationReward reward
                 in rewards)
        {
            if (reward == null ||
                reward.Faction == null ||
                reward.Amount == 0)
            {
                continue;
            }

            if (reward.DiscoverFaction)
            {
                Manager.PlayerReputation
                    .DiscoverFaction(
                        reward.Faction
                    );
            }

            Manager.PlayerReputation
                .AddReputation(
                    reward.Faction,
                    reward.Amount
                );
        }
    }

    private void GrantAbilities()
    {
        IReadOnlyList<FavourAbilityReward>
            fixedRewards =
                Data.AbilityRewards;

        if (fixedRewards != null)
        {
            foreach (FavourAbilityReward reward
                     in fixedRewards)
            {
                if (reward?.Ability == null)
                    continue;

                Manager.TryGrantAbility(
                    reward.Ability
                );
            }
        }

        ForEachSelectedReward(
            option =>
            {
                if (option.Type !=
                        FavourRewardChoiceType.Ability ||
                    option.Ability == null)
                {
                    return;
                }

                Manager.TryGrantAbility(
                    option.Ability
                );
            }
        );
    }

    private void ForEachSelectedReward(
        Action<FavourRewardChoiceOption>
            callback)
    {
        if (callback == null)
            return;

        IReadOnlyList<
            FavourRewardChoiceGroup>
            groups =
                Data?.RewardChoiceGroups;

        if (groups == null)
            return;

        int count =
            Mathf.Min(
                groups.Count,
                selectedRewardOptions.Count
            );

        for (int groupIndex = 0;
             groupIndex < count;
             groupIndex++)
        {
            FavourRewardChoiceGroup group =
                groups[groupIndex];

            if (group?.Options == null)
                continue;

            foreach (int optionIndex
                     in selectedRewardOptions[
                         groupIndex
                     ])
            {
                if (optionIndex < 0 ||
                    optionIndex >=
                    group.Options.Count)
                {
                    continue;
                }

                FavourRewardChoiceOption option =
                    group.Options[
                        optionIndex
                    ];

                if (option == null ||
                    !option.IsValid)
                {
                    continue;
                }

                callback.Invoke(
                    option
                );
            }
        }
    }

    // =========================================================
    // FOLLOW-UPS / STATE
    // =========================================================

    private void RegisterFollowUps()
    {
        if (Data?.FollowUps == null ||
            Manager == null)
        {
            return;
        }

        foreach (FavourData followUp
                 in Data.FollowUps)
        {
            if (followUp == null)
                continue;

            Manager.RegisterFavour(
                followUp
            );
        }
    }

    private Inventory ResolveInventory()
    {
        Inventory inventory =
            Manager?.PlayerInventory;

        if (inventory == null)
        {
            inventory =
                Inventory.Instance;
        }

        return inventory;
    }

    private void SetState(
        FavourState newState)
    {
        if (State == newState)
            return;

        State = newState;

        StateChanged?.Invoke(
            this
        );
    }

    public bool TryFail()
    {
        if (State !=
                FavourState.Active &&
            State !=
                FavourState.ReadyToTurnIn)
        {
            return false;
        }

        DeactivateObjectives();

        SetState(
            FavourState.Failed
        );

        FavourFailureSettings failure =
            Data?.FailureSettings;

        FavourFailurePolicy policy =
            failure != null
                ? failure.Policy
                : FavourFailurePolicy.None;

        switch (policy)
        {

            case FavourFailurePolicy.None:

            case FavourFailurePolicy.PermanentFailure:
                return true;

            case FavourFailurePolicy.RetryImmediately:

                ResetForNewRun();

                return true;

            case FavourFailurePolicy.RetryAfterCooldown:

                BeginCooldown(
                    failure != null
                        ? failure.RetryCooldownSeconds
                        : 0f
                );

                return true;

            case FavourFailurePolicy.ResetObjectivesAndRetry:

                RestartAcceptedAttempt();

                return true;

            default:
                return true;
        }
    }

    private void RestartAcceptedAttempt()
    {
        DeactivateObjectives();

        SetState(
            FavourState.Unavailable
        );

        foreach (FavourObjectiveRuntime objective
                 in objectives)
        {
            objective?.ResetProgress();
        }

        ClearRewardSelections();

        if (IsCourier)
        {
            SetState(
                FavourState.ReadyToTurnIn
            );

            return;
        }

        SetState(
            FavourState.Active
        );

        ActivateObjectives();

        EvaluateCompletion();
    }

    internal void Tick(
    float deltaTime)
    {
        if (State !=
            FavourState.Cooldown)
        {
            return;
        }

        if (cooldownRemaining <=
            0f)
        {
            ResetForNewRun();
            return;
        }

        cooldownRemaining -=
            Mathf.Max(
                0f,
                deltaTime
            );

        if (cooldownRemaining >
            0f)
        {
            return;
        }

        cooldownRemaining =
            0f;

        ResetForNewRun();
    }

    private void BeginCooldown(
        float duration)
    {
        cooldownRemaining =
            Mathf.Max(
                0f,
                duration
            );

        SetState(
            FavourState.Cooldown
        );

        if (cooldownRemaining <=
            0f)
        {
            ResetForNewRun();
        }
    }

    private void ResetForNewRun()
    {
        DeactivateObjectives();

        ClearSourceItem();

        cooldownRemaining =
            0f;

        SetState(
            FavourState.Unavailable
        );

        foreach (FavourObjectiveRuntime objective
                 in objectives)
        {
            objective?.ResetProgress();
        }

        ClearRewardSelections();

        rolledCurrencyReward =
            RollCurrencyReward();

        RefreshAvailability();
    }

    internal void ResetForReaccept()
    {
        if (State !=
                FavourState.Active &&
            State !=
                FavourState.ReadyToTurnIn)
        {
            return;
        }

        ResetForNewRun();
    }

    // =========================================================
    // PRESENTATION API
    // =========================================================

    public int MinimumLevel =>
    Data != null
        ? Data.MinimumLevel
        : 0;

    public bool IsMinimumLevelMet
    {
        get
        {
            if (MinimumLevel <= 0)
                return true;

            PlayerStats player =
                Manager?.Player;

            return player != null &&
                   player.Level >=
                   MinimumLevel;
        }
    }

    public int ExperienceReward =>
        Data != null
            ? Data.ExperienceReward
            : 0;

    public int CurrencyReward =>
        rolledCurrencyReward;

    public IReadOnlyList<
        FavourItemReward>
        ItemRewards =>
            Data?.ItemRewards;

    public IReadOnlyList<
        FavourAbilityReward>
        AbilityRewards =>
            Data?.AbilityRewards;

    public IReadOnlyList<
        FavourReputationReward>
        ReputationRewards =>
            Data?.ReputationRewards;

    public IReadOnlyList<
        FavourRewardChoiceRuntime>
        RewardChoices =>
            rewardChoices;

    public bool HasRewardChoices =>
        rewardChoices.Count > 0;

    public bool HasFixedItemOrAbilityRewards
    {
        get
        {
            bool hasItems =
                Data?.ItemRewards != null &&
                Data.ItemRewards.Count > 0;

            bool hasAbilities =
                Data?.AbilityRewards != null &&
                Data.AbilityRewards.Count > 0;

            return hasItems ||
                   hasAbilities;
        }
    }

    public string Id =>
        Data != null
            ? Data.Id
            : string.Empty;

    public string DisplayName =>
        Data != null
            ? Data.DisplayName
            : "Missing Favour";

    public string Description =>
        Data != null
            ? Data.Description
            : string.Empty;

    public FavourType Category =>
        Data != null
            ? Data.Category
            : FavourType.General;

    public FavourActivationPolicy
        ActivationPolicy =>
            Data != null
                ? Data.ActivationPolicy
                : FavourActivationPolicy
                    .ExplicitAccept;

    public FavourCompletionPolicy
        CompletionPolicy =>
            Data != null
                ? Data.CompletionPolicy
                : FavourCompletionPolicy
                    .ReturnToGiver;

    public string CurrentDialogue
    {
        get
        {
            FavourDialogueSet dialogueSet =
                Data?.DialogueSet;

            return dialogueSet != null
                ? dialogueSet.GetDialogue(
                    State
                )
                : string.Empty;
        }
    }
}