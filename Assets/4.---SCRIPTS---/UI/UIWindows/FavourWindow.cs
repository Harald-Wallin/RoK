using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FavourWindow :
    MonoBehaviour, IUIWindow
{
    private readonly List<
        FavourObjectiveRowUI>
        objectiveRows =
            new();

    private readonly List<
        FavourRewardEntryUI>
        fixedRewardViews =
            new();

    private readonly List<GameObject>
    rewardChoiceObjects =
        new();

    private readonly List<FavourRewardEntryUI>
        rewardChoiceEntries =
            new();

    [Header("Root")]

    [SerializeField]
    private GameObject windowRoot;

    [SerializeField]
    private RectTransform windowPanel;

    [Header("Header")]

    [SerializeField]
    private TMP_Text statusText;

    [SerializeField]
    private TMP_Text favourNameText;

    [SerializeField]
    private TMP_Text giverNameText;

    [SerializeField]
    private TMP_Text dialogueText;

    [Header("Objectives")]

    [SerializeField]
    private Transform objectiveContainer;

    [SerializeField]
    private FavourObjectiveRowUI
        objectiveRowPrefab;

    [Header("Rewards")]

    [SerializeField]
    private GameObject fixedRewardsSection;

    [SerializeField]
    private Transform fixedRewardContainer;

    [SerializeField]
    private FavourRewardEntryUI
        fixedRewardPrefab;

    [SerializeField]
    private GameObject rewardChoiceSection;

    [SerializeField]
    private Transform rewardChoiceContent;

    [SerializeField]
    private TMP_Text rewardChoiceTitlePrefab;

    [SerializeField]
    private RectTransform rewardChoiceRowPrefab;

    [SerializeField]
    private FavourRewardEntryUI
        rewardChoiceEntryPrefab;

    [SerializeField]
    private TMP_Text experienceRewardText;

    [SerializeField]
    private TMP_Text reputationRewardText;

    [Header("Buttons")]

    [SerializeField]
    private Button acceptButton;

    [SerializeField]
    private Button completeButton;

    [SerializeField]
    private Button closeButton;

    [Header("Main Sections")]

    [SerializeField]
    private GameObject objectivesSection;

    [SerializeField]
    private GameObject rewardsSection;

    public static FavourWindow Instance
    {
        get;
        private set;
    }

    public bool IsOpen =>
        windowRoot != null &&
        windowRoot.activeSelf;

    public FavourRuntime CurrentRuntime
    {
        get;
        private set;
    }

    public FavourGiver CurrentGiver
    {
        get;
        private set;
    }

    public ItemFavourSource CurrentItemSource
    {
        get;
        private set;
    }

    public bool IsOpenedFromItem =>
        CurrentItemSource != null;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogWarning(
                "Flera FavourWindow hittades. " +
                "Den nya komponenten stängs av.",
                this
            );

            enabled = false;
            return;
        }

        Instance = this;

        ConfigureButtons();

        SetOpen(
            false
        );
    }

    private void OnDestroy()
    {
        UnsubscribeFromRuntime();

        if (acceptButton != null)
        {
            acceptButton.onClick
                .RemoveListener(
                    HandleAcceptClicked
                );
        }

        if (completeButton != null)
        {
            completeButton.onClick
                .RemoveListener(
                    HandleCompleteClicked
                );
        }

        if (closeButton != null)
        {
            closeButton.onClick
                .RemoveListener(
                    Close
                );
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void ConfigureButtons()
    {
        if (acceptButton != null)
        {
            acceptButton.onClick
                .AddListener(
                    HandleAcceptClicked
                );
        }

        if (completeButton != null)
        {
            completeButton.onClick
                .AddListener(
                    HandleCompleteClicked
                );
        }

        if (closeButton != null)
        {
            closeButton.onClick
                .AddListener(
                    Close
                );
        }
    }

    public void Open(FavourGiver giver)
    {
        Open(
            giver,
            (InteractionTarget)null
        );
    }

    public void Open(
        FavourGiver giver,
        InteractionTarget interactionTarget)
    {
        if (giver == null)
            return;

        List<FavourRuntime> visibleFavours =
            giver.GetVisibleFavours();

        if (visibleFavours == null ||
            visibleFavours.Count == 0)
        {
            Debug.Log(
                $"{giver.GiverName} har inga synliga favours.",
                giver
            );

            return;
        }

        Open(
            giver,
            visibleFavours[0],
            interactionTarget
        );
    }

    public void Open(
        FavourGiver giver,
        FavourRuntime runtime)
    {
        Open(
            giver,
            runtime,
            null
        );
    }

    public void Open(
        FavourGiver giver,
        FavourRuntime runtime,
        InteractionTarget interactionTarget)
    {
        if (giver == null ||
            runtime == null)
        {
            return;
        }

        UnsubscribeFromRuntime();

        /*
         * Exakt en source får vara aktiv.
         */
        CurrentGiver =
            giver;

        CurrentItemSource =
            null;

        CurrentRuntime =
            runtime;

        SubscribeToRuntime();

        SetOpen(
            true
        );

        /*
         * NPC/world-fönster är distance-bundna.
         *
         * Om ett InteractionTarget skickades in använder vi det.
         * Annars använder vi samma fallback som tidigare.
         */
        if (interactionTarget != null)
        {
            GlobalUIManager.Instance?
                .RegisterInteractionWindow(
                    this,
                    interactionTarget.InteractionTransform,
                    interactionTarget.WindowCloseDistance
                );
        }
        else
        {
            RegisterAsInteractionWindow(
                giver
            );
        }

        RebuildAll();
    }

    public void Open(
    ItemData item)
    {
        if (item == null ||
            !item.HasFavourInteraction)
        {
            return;
        }

        /*
         * Item-interaktion är INTE en world-interaktion.
         *
         * Om ett gammalt NPC/vendor/etc-fönster fortfarande är
         * registrerat ska den registreringen bort innan itemets
         * FavourWindow öppnas.
         */
        GlobalUIManager.Instance?
            .ClearInteractionWindow();

        ItemFavourSource itemSource =
            new(
                item
            );

        List<FavourRuntime> visibleFavours =
            itemSource.GetVisibleFavours();

        if (visibleFavours == null ||
            visibleFavours.Count == 0)
        {
            return;
        }

        FavourRuntime runtime =
            ResolvePreferredItemRuntime(
                visibleFavours
            );

        if (runtime == null)
            return;

        UnsubscribeFromRuntime();

        /*
         * Exakt en source får vara aktiv.
         */
        CurrentGiver =
            null;

        CurrentItemSource =
            itemSource;

        CurrentRuntime =
            runtime;

        SubscribeToRuntime();

        SetOpen(
            true
        );

        /*
         * Ingen RegisterInteractionWindow här.
         *
         * Ett item i inventory/equipment har ingen world-position
         * som spelaren kan gå för långt ifrån.
         */

        RebuildAll();
    }

    private static FavourRuntime
    ResolvePreferredItemRuntime(
        List<FavourRuntime> runtimes)
    {
        if (runtimes == null ||
            runtimes.Count == 0)
        {
            return null;
        }

        /*
         * Pågående item-favour först.
         */
        foreach (FavourRuntime runtime
                 in runtimes)
        {
            if (runtime == null)
                continue;

            if (runtime.State ==
                    FavourState.Active ||
                runtime.State ==
                    FavourState.ReadyToTurnIn)
            {
                return runtime;
            }
        }

        /*
         * Sedan nästa faktiskt tillgängliga del.
         */
        foreach (FavourRuntime runtime
                 in runtimes)
        {
            if (runtime != null &&
                runtime.State ==
                    FavourState.Available)
            {
                return runtime;
            }
        }

        /*
         * Därefter locked/cooldown/failed presentation.
         *
         * Listordningen i ItemData fungerar som authored
         * chain-prioritet.
         */
        foreach (FavourRuntime runtime
                 in runtimes)
        {
            if (runtime == null)
                continue;

            if (runtime.State ==
                    FavourState.Unavailable ||
                runtime.State ==
                    FavourState.Cooldown ||
                runtime.State ==
                    FavourState.Failed)
            {
                return runtime;
            }
        }

        return null;
    }

    private string GetCurrentSourceName()
    {
        if (CurrentGiver != null)
        {
            return CurrentGiver
                .GiverName;
        }

        if (CurrentItemSource != null)
        {
            return CurrentItemSource
                .SourceName;
        }

        return string.Empty;
    }

    private string GetCurrentDialogue(
        FavourRuntime runtime)
    {
        if (runtime == null)
            return string.Empty;

        if (CurrentGiver != null)
        {
            return CurrentGiver
                .GetDialogueFor(
                    runtime
                );
        }

        if (CurrentItemSource != null)
        {
            return CurrentItemSource
                .GetDialogueFor(
                    runtime
                );
        }

        return runtime.CurrentDialogue;
    }

    private bool TryAcceptCurrentFavour(
        FavourData favour)
    {
        if (favour == null)
            return false;

        if (CurrentGiver != null)
        {
            return CurrentGiver
                .TryAccept(
                    favour
                );
        }

        if (CurrentItemSource != null)
        {
            return CurrentItemSource
                .TryAccept(
                    favour
                );
        }

        return false;
    }

    private void RegisterAsInteractionWindow(
    FavourGiver giver)
    {
        if (giver == null)
            return;

        InteractionTarget target =
            giver.GetComponentInChildren<
                InteractionTarget>();

        if (target == null)
        {
            Debug.LogWarning(
                $"FavourGiver '{giver.name}' saknar " +
                "InteractionTarget i sin hierarchy.",
                giver);

            return;
        }

        GlobalUIManager.Instance?
            .RegisterInteractionWindow(
                this,
                target.InteractionTransform,
                target.WindowCloseDistance);
    }

    public void Close()
    {
        UnsubscribeFromRuntime();

        CurrentRuntime =
            null;

        CurrentGiver =
            null;

        CurrentItemSource =
            null;

        ClearDynamicContent();
        ClearRewardChoices();

        SetOpen(
            false
        );

        GlobalUIManager.Instance?
            .ClearInteractionWindow(
                this
            );
    }



    private void SubscribeToRuntime()
    {
        if (CurrentRuntime == null)
            return;

        CurrentRuntime.StateChanged +=
            HandleRuntimeStateChanged;

        CurrentRuntime.ProgressChanged +=
            HandleRuntimeProgressChanged;

        CurrentRuntime
            .RewardSelectionChanged +=
            HandleRewardSelectionChanged;
    }

    private void UnsubscribeFromRuntime()
    {
        if (CurrentRuntime == null)
            return;

        CurrentRuntime.StateChanged -=
            HandleRuntimeStateChanged;

        CurrentRuntime.ProgressChanged -=
            HandleRuntimeProgressChanged;

        CurrentRuntime
            .RewardSelectionChanged -=
            HandleRewardSelectionChanged;
    }


    private void HandleRuntimeStateChanged(
    FavourRuntime runtime)
    {
        if (runtime != CurrentRuntime)
            return;

        /*
         * State kan ändra hela presentationen.
         * Completed ska exempelvis ta bort objectives och rewards.
         */
        RebuildAll();
    }

    private void RefreshLayout()
    {
        Canvas.ForceUpdateCanvases();

        if (windowPanel != null)
        {
            LayoutRebuilder
                .ForceRebuildLayoutImmediate(
                    windowPanel
                );
        }
    }

    private void HandleRuntimeProgressChanged(
    FavourRuntime runtime)
    {
        if (runtime != CurrentRuntime)
            return;

        RebuildObjectives();
        RefreshRewardChoices();
    }

    private void HandleRewardSelectionChanged(
    FavourRuntime runtime)
    {
        if (runtime != CurrentRuntime)
            return;

        RefreshRewardChoices();
    }

    private void RebuildAll()
    {
        RefreshStaticText();

        bool completed =
            CurrentRuntime != null &&
            CurrentRuntime.State ==
            FavourState.Completed;

        if (completed)
        {
            ApplyCompletedPresentation();
            RefreshButtons();
            RefreshLayout();

            return;
        }

        ApplyStandardPresentation();

        RebuildObjectives();
        RebuildRewards();

        RefreshButtons();
        RefreshLayout();
    }

    private void ApplyCompletedPresentation()
    {
        /*
         * Rensa först de instansierade objekten så att de inte
         * ligger kvar osynligt och återanvänds av misstag.
         */
        ClearObjectiveRows();
        ClearFixedRewards();
        ClearRewardChoices();

        if (objectivesSection != null)
        {
            objectivesSection.SetActive(
                false
            );
        }

        if (rewardsSection != null)
        {
            rewardsSection.SetActive(
                false
            );
        }

        if (statusText != null)
        {
            statusText.text =
                string.Empty;

            statusText.gameObject.SetActive(
                false
            );
        }

        /*
         * Close ska alltid finnas i det färdiga läget.
         */
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(
                true
            );

            closeButton.interactable =
                true;
        }
    }

    private void ApplyStandardPresentation()
    {
        if (objectivesSection != null)
        {
            objectivesSection.SetActive(
                true
            );
        }

        if (rewardsSection != null)
        {
            rewardsSection.SetActive(
                true
            );
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(
                true
            );

            closeButton.interactable =
                true;
        }
    }

    private void RefreshStaticText()
    {
        if (CurrentRuntime == null)
            return;

        if (favourNameText != null)
        {
            favourNameText.text =
                CurrentRuntime.DisplayName;
        }

        if (giverNameText != null)
        {
            giverNameText.text =
                GetCurrentSourceName();
        }

        if (dialogueText != null)
        {
            string dialogue =
                GetCurrentDialogue(
                    CurrentRuntime
                );

            if (string.IsNullOrWhiteSpace(
                    dialogue))
            {
                dialogue =
                    CurrentRuntime.Description;
            }

            dialogueText.text =
                dialogue;
        }

        RefreshStatusText();
    }

    private void RefreshStatusText()
    {
        if (statusText == null ||
            CurrentRuntime == null)
        {
            return;
        }

        switch (CurrentRuntime.State)
        {
            case FavourState.Unavailable:
                statusText.text =
                    BuildUnavailableStatusText();

                statusText.gameObject
                    .SetActive(
                        !string.IsNullOrWhiteSpace(
                            statusText.text
                        )
                    );

                break;

            case FavourState.Failed:
                statusText.text =
                    "This favour has failed.";

                statusText.gameObject
                    .SetActive(
                        true
                    );

                break;

            case FavourState.Cooldown:
                statusText.text =
                    "This favour is currently on cooldown.";

                statusText.gameObject
                    .SetActive(
                        true
                    );

                break;

            default:
                statusText.text =
                    string.Empty;

                statusText.gameObject
                    .SetActive(
                        false
                    );

                break;
        }
    }

    private string BuildUnavailableStatusText()
    {
        if (CurrentRuntime?.Data == null)
            return "This favour is not yet available.";

        int minimumLevel =
            CurrentRuntime.Data.MinimumLevel;

        PlayerStats player =
            PlayerReference.Player;

        if (minimumLevel > 0 &&
            player != null &&
            player.Level < minimumLevel)
        {
            return $"Requires Level {minimumLevel}";
        }

        return "This favour is not yet available.";
    }

    private void RebuildObjectives()
    {
        ClearObjectiveRows();

        if (CurrentRuntime == null ||
            objectiveContainer == null ||
            objectiveRowPrefab == null)
        {
            return;
        }

        /*
         * Vanliga gameplay-objectives:
         * Kill, Collect, Interact osv.
         */
        foreach (FavourObjectiveRuntime objective
                 in CurrentRuntime.Objectives)
        {
            if (objective == null)
                continue;

            FavourObjectiveRowUI row =
                Instantiate(
                    objectiveRowPrefab,
                    objectiveContainer
                );

            row.Bind(
                objective
            );

            objectiveRows.Add(
                row
            );
        }

        if (!CurrentRuntime
                .ShouldShowTurnInObjective)
        {
            return;
        }

        string targetName;

        if (CurrentRuntime
            .UsesSpecificCompletionTarget)
        {
            targetName =
                CurrentRuntime
                    .CompletionTargetDisplayName;
        }
        else
        {
            targetName =
                CurrentGiver != null
                    ? CurrentGiver.GiverName
                    : string.Empty;
        }

        FavourObjectiveRowUI returnRow =
            Instantiate(
                objectiveRowPrefab,
                objectiveContainer
            );

        returnRow.BindReturnToGiver(
            targetName,
            CurrentRuntime.State ==
                FavourState.ReadyToTurnIn
        );

        objectiveRows.Add(
            returnRow
        );
    }

    private void ClearRewardChoices()
    {
        foreach (FavourRewardEntryUI entry
                 in rewardChoiceEntries)
        {
            if (entry != null)
            {
                entry.gameObject.SetActive(
                    false
                );
            }
        }

        rewardChoiceEntries.Clear();

        foreach (GameObject choiceObject
                 in rewardChoiceObjects)
        {
            if (choiceObject == null)
                continue;

            /*
             * Inaktivering tar bort objektet från layouten direkt.
             * Destroy sker vid slutet av framen.
             */
            choiceObject.SetActive(
                false
            );

            Destroy(
                choiceObject
            );
        }

        rewardChoiceObjects.Clear();

        if (rewardChoiceSection != null)
        {
            rewardChoiceSection.SetActive(
                false
            );
        }
    }

    private void RebuildRewards()
    {
        ClearFixedRewards();
        ClearRewardChoices();

        if (CurrentRuntime == null)
        {
            SetRewardSectionsVisible(
                false,
                false
            );

            RefreshExperienceAndCurrency();
            RefreshReputationRewards();

            return;
        }

        BuildFixedItemRewards();
        BuildFixedAbilityRewards();
        BuildFixedCurrencyReward();
        BuildRewardChoices();

        bool hasFixedRewards =
            fixedRewardViews.Count > 0;

        bool hasRewardChoices =
            rewardChoiceEntries.Count > 0;

        SetRewardSectionsVisible(
            hasFixedRewards,
            hasRewardChoices
        );

        RefreshExperienceAndCurrency();
        RefreshReputationRewards();
    }

    private void SetRewardSectionsVisible(
    bool showFixedRewards,
    bool showRewardChoices)
    {
        if (fixedRewardsSection != null)
        {
            fixedRewardsSection.SetActive(
                showFixedRewards
            );
        }

        if (rewardChoiceSection != null)
        {
            rewardChoiceSection.SetActive(
                showRewardChoices
            );
        }
    }

    private void BuildFixedItemRewards()
    {
        if (CurrentRuntime == null ||
            fixedRewardContainer == null ||
            fixedRewardPrefab == null ||
            CurrentRuntime.ItemRewards == null)
        {
            return;
        }

        foreach (FavourItemReward reward
                 in CurrentRuntime.ItemRewards)
        {
            if (reward == null ||
                reward.Item == null ||
                reward.Amount <= 0)
            {
                continue;
            }

            FavourRewardEntryUI view =
                Instantiate(
                    fixedRewardPrefab,
                    fixedRewardContainer
                );

            view.BindFixedItem(
                reward
            );

            fixedRewardViews.Add(
                view
            );
        }
    }

    private void BuildFixedAbilityRewards()
    {
        if (CurrentRuntime == null ||
            fixedRewardContainer == null ||
            fixedRewardPrefab == null ||
            CurrentRuntime.AbilityRewards == null)
        {
            return;
        }

        foreach (FavourAbilityReward reward
                 in CurrentRuntime.AbilityRewards)
        {
            if (reward == null ||
                reward.Ability == null)
            {
                continue;
            }

            FavourRewardEntryUI view =
                Instantiate(
                    fixedRewardPrefab,
                    fixedRewardContainer
                );

            view.BindFixedAbility(
                reward
            );

            fixedRewardViews.Add(
                view
            );
        }
    }

    private void BuildFixedCurrencyReward()
    {
        if (CurrentRuntime == null ||
            CurrentRuntime.CurrencyReward <= 0 ||
            fixedRewardContainer == null ||
            fixedRewardPrefab == null)
        {
            return;
        }

        PlayerCurrency playerCurrency =
            CurrentRuntime.Manager != null
                ? CurrentRuntime.Manager
                    .PlayerCurrency
                : PlayerCurrency.Instance;

        CurrencyData currency =
            playerCurrency != null
                ? playerCurrency
                    .CurrencyDefinition
                : null;

        if (currency == null)
        {
            Debug.LogWarning(
                $"Kan inte visa currency reward för " +
                $"'{CurrentRuntime.DisplayName}': " +
                $"{nameof(CurrencyData)} saknas.",
                this
            );

            return;
        }

        FavourRewardEntryUI view =
            Instantiate(
                fixedRewardPrefab,
                fixedRewardContainer
            );

        view.BindFixedCurrency(
            currency,
            CurrentRuntime.CurrencyReward
        );

        fixedRewardViews.Add(
            view
        );
    }

    private void BuildRewardChoices()
    {
        if (CurrentRuntime == null ||
            rewardChoiceContent == null ||
            rewardChoiceTitlePrefab == null ||
            rewardChoiceRowPrefab == null ||
            rewardChoiceEntryPrefab == null)
        {
            return;
        }

        foreach (
            FavourRewardChoiceRuntime choiceGroup
            in CurrentRuntime.RewardChoices)
        {
            if (choiceGroup == null)
                continue;

            int validOptionCount =
                CountValidRewardOptions(
                    choiceGroup
                );

            if (validOptionCount == 0)
                continue;

            TMP_Text title =
                Instantiate(
                    rewardChoiceTitlePrefab,
                    rewardChoiceContent
                );

            title.text =
                BuildRewardChoiceTitle(
                    choiceGroup
                );

            rewardChoiceObjects.Add(
                title.gameObject
            );

            RectTransform optionRow =
                Instantiate(
                    rewardChoiceRowPrefab,
                    rewardChoiceContent
                );

            rewardChoiceObjects.Add(
                optionRow.gameObject
            );

            foreach (
                FavourRewardChoiceOptionRuntime option
                in choiceGroup.Options)
            {
                if (option == null ||
                    !option.IsValid)
                {
                    continue;
                }

                FavourRewardEntryUI entry =
                    Instantiate(
                        rewardChoiceEntryPrefab,
                        optionRow
                    );

                entry.BindChoice(
                    option
                );

                rewardChoiceEntries.Add(
                    entry
                );
            }
        }
    }

    private static string BuildRewardChoiceTitle(
    FavourRewardChoiceRuntime choiceGroup)
    {
        int amount =
            Mathf.Max(
                1,
                choiceGroup.RequiredSelections
            );

        return $"Choose {amount}:";
    }

    private static int CountValidRewardOptions(
    FavourRewardChoiceRuntime choiceGroup)
    {
        if (choiceGroup?.Options == null)
            return 0;

        int count = 0;

        foreach (
            FavourRewardChoiceOptionRuntime option
            in choiceGroup.Options)
        {
            if (option != null &&
                option.IsValid)
            {
                count++;
            }
        }

        return count;
    }

    private void RefreshExperienceAndCurrency()
    {
        if (experienceRewardText == null ||
            CurrentRuntime == null)
        {
            return;
        }

        int experience =
            CurrentRuntime.ExperienceReward;

        bool hasExperience =
            experience > 0;

        experienceRewardText.gameObject
            .SetActive(
                hasExperience
            );

        experienceRewardText.text =
            hasExperience
                ? $"Experience: {experience}"
                : string.Empty;
    }

    private void RefreshRewardChoices()
    {
        foreach (
            FavourRewardEntryUI entry
            in rewardChoiceEntries)
        {
            entry?.RefreshChoiceVisual();
        }

        RefreshButtons();
    }

    private void RefreshReputationRewards()
    {
        if (reputationRewardText == null ||
            CurrentRuntime == null)
        {
            return;
        }

        IReadOnlyList<
            FavourReputationReward>
            rewards =
                CurrentRuntime
                    .ReputationRewards;

        if (rewards == null ||
            rewards.Count == 0)
        {
            reputationRewardText.text =
                string.Empty;

            reputationRewardText.gameObject
                .SetActive(
                    false
                );

            return;
        }

        StringBuilder builder =
            new();

        foreach (FavourReputationReward reward
                 in rewards)
        {
            if (reward == null ||
                reward.Faction == null ||
                reward.Amount == 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(
                    ", "
                );
            }

            string factionName =
                string.IsNullOrWhiteSpace(
                    reward.Faction.factionName)
                    ? reward.Faction.name
                    : reward.Faction
                        .factionName;

            builder.Append(
                reward.Amount > 0
                    ? "+"
                    : string.Empty
            );

            builder.Append(
                reward.Amount
            );

            builder.Append(
                " "
            );

            builder.Append(
                factionName
            );
        }

        bool hasText =
            builder.Length > 0;

        reputationRewardText.gameObject
            .SetActive(
                hasText
            );

        reputationRewardText.text =
            hasText
                ? builder.ToString()
                : string.Empty;
    }

    private void RefreshButtons()
    {
        if (CurrentRuntime == null)
            return;

        if (acceptButton != null)
        {
            bool visible =
                CurrentRuntime.State ==
                    FavourState.Available;

            acceptButton.gameObject.SetActive(
                visible
            );

            acceptButton.interactable =
                visible &&
                CurrentRuntime.CanAccept;
        }

        if (completeButton != null)
        {
            bool canStartEscort =
                CurrentRuntime.State ==
                    FavourState.Active &&
                CurrentGiver != null &&
                CurrentGiver.CanStartEscort(
                    CurrentRuntime
                );

            bool canDeliver =
                CurrentRuntime.State ==
                    FavourState.Active &&
                CurrentGiver != null &&
                CurrentGiver.CanDeliver(
                    CurrentRuntime
                );

            bool canTurnIn =
                CurrentRuntime.State ==
                    FavourState.ReadyToTurnIn &&
                CurrentGiver != null &&
                CurrentGiver.CanTurnIn(
                    CurrentRuntime.Data
                );

            bool visible =
                canStartEscort ||
                canDeliver ||
                canTurnIn;

            completeButton.gameObject.SetActive(
                visible
            );

            completeButton.interactable =
                visible;

            TMP_Text buttonText =
                completeButton.GetComponentInChildren<
                    TMP_Text>();

            if (buttonText != null)
            {
                if (canStartEscort)
                {
                    buttonText.text =
                        "Start Escort";
                }
                else if (canDeliver)
                {
                    buttonText.text =
                        "Deliver";
                }
                else
                {
                    buttonText.text =
                        "Complete";
                }
            }
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(
                true
            );

            closeButton.interactable =
                true;
        }
    }

    private void HandleAcceptClicked()
    {
        if (CurrentRuntime == null)
            return;

        FavourData favour =
            CurrentRuntime.Data;

        bool accepted =
            TryAcceptCurrentFavour(
                favour
            );

        if (!accepted)
            return;

        /*
         * Escort är uttryckligen world/NPC-specifikt.
         *
         * Om Favourn accepterades genom sin riktiga FavourGiver
         * och nu väntar på Start Escort ska fönstret ligga kvar.
         */
        if (CurrentGiver != null &&
            CurrentGiver.CanStartEscort(
                CurrentRuntime))
        {
            RebuildAll();

            return;
        }

        /*
         * Vanlig Favour, inklusive item-startad Favour:
         * accept → stäng fönstret.
         */
        Close();
    }

    private void HandleCompleteClicked()
    {
        if (CurrentRuntime == null ||
            CurrentGiver == null)
        {
            return;
        }

        /*
         * ---------------------------------------------------------
         * ESCORT START
         * ---------------------------------------------------------
         */
        if (CurrentRuntime.State ==
                FavourState.Active &&
            CurrentGiver.CanStartEscort(
                CurrentRuntime))
        {
            bool started =
                CurrentGiver.TryStartEscort(
                    CurrentRuntime
                );

            if (!started)
                return;

            Close();

            return;
        }

        /*
         * ---------------------------------------------------------
         * DELIVERY
         * ---------------------------------------------------------
         */
        if (CurrentRuntime.State ==
                FavourState.Active &&
            CurrentGiver.CanDeliver(
                CurrentRuntime))
        {
            bool delivered =
                CurrentGiver.TryDeliver(
                    CurrentRuntime
                );

            if (!delivered)
                return;

            RebuildAll();

            return;
        }

        /*
         * ---------------------------------------------------------
         * NORMAL FAVOUR TURN-IN
         * ---------------------------------------------------------
         */

        FavourRuntime completedRuntime =
            CurrentRuntime;

        FavourGiver giver =
            CurrentGiver;

        FavourData favour =
            completedRuntime.Data;

        bool completed =
            giver.TryTurnIn(
                favour
            );

        if (!completed)
            return;

        if (TryShowNextFavour(
                giver,
                completedRuntime))
        {
            return;
        }

        RebuildAll();
    }

    private bool TryShowNextFavour(
    FavourGiver giver,
    FavourRuntime completedRuntime)
    {
        if (giver == null ||
            completedRuntime == null)
        {
            Debug.LogError(
                "[FavourWindow] TryShowNextFavour saknar giver eller completedRuntime."
            );

            return false;
        }

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        Debug.Log(
            $"[FavourWindow] SEARCH NEXT FAVOUR\n" +
            $"Giver: {giver.GiverName}\n" +
            $"Completed: {completedRuntime.DisplayName}\n" +
            $"Manager: {(manager != null ? "OK" : "NULL")}"
        );

        /*
         * --------------------------------------------------------
         * 1. EXPLICITA FOLLOW-UPS
         * --------------------------------------------------------
         */

        IReadOnlyList<FavourData> followUps =
            completedRuntime.Data != null
                ? completedRuntime.Data.FollowUps
                : null;

        Debug.Log(
            $"[FavourWindow] FollowUps count: " +
            $"{(followUps != null ? followUps.Count : 0)}"
        );

        if (followUps != null &&
            manager != null)
        {
            foreach (FavourData followUp
                     in followUps)
            {
                if (followUp == null)
                {
                    Debug.Log(
                        "[FavourWindow] FollowUp entry = NULL"
                    );

                    continue;
                }

                Debug.Log(
                    $"[FavourWindow] Checking FollowUp: {followUp.DisplayName}"
                );

                bool foundRuntime =
                    manager.TryGetRuntime(
                        followUp,
                        out FavourRuntime runtime
                    );

                Debug.Log(
                    $"[FavourWindow]   Runtime registered: {foundRuntime}\n" +
                    $"[FavourWindow]   Runtime state: " +
                    $"{(runtime != null ? runtime.State.ToString() : "NULL")}"
                );

                if (!foundRuntime ||
                    runtime == null)
                {
                    continue;
                }

                if (runtime == completedRuntime)
                {
                    Debug.LogWarning(
                        "[FavourWindow] FollowUp pekar på samma runtime som precis completed."
                    );

                    continue;
                }

                bool visible =
                    giver.TryGetVisibleRuntime(
                        followUp,
                        out FavourRuntime visibleRuntime
                    );

                Debug.Log(
                    $"[FavourWindow]   Visible at {giver.GiverName}: {visible}\n" +
                    $"[FavourWindow]   Visible runtime: " +
                    $"{(visibleRuntime != null ? visibleRuntime.DisplayName : "NULL")}"
                );

                if (!visible ||
                    visibleRuntime == null)
                {
                    continue;
                }

                Debug.Log(
                    $"[FavourWindow] SWITCHING TO FOLLOW-UP: " +
                    $"{visibleRuntime.DisplayName}"
                );

                SwitchRuntime(
                    visibleRuntime
                );

                return true;
            }
        }

        /*
         * --------------------------------------------------------
         * 2. ALLA FAVOURS SOM GIVAREN JUST NU ANSER SYNLIGA
         * --------------------------------------------------------
         */

        List<FavourRuntime> visibleFavours =
            giver.GetVisibleFavours();

        Debug.Log(
            $"[FavourWindow] GetVisibleFavours() count: " +
            $"{(visibleFavours != null ? visibleFavours.Count : 0)}"
        );

        if (visibleFavours == null)
            return false;

        foreach (FavourRuntime runtime
                 in visibleFavours)
        {
            if (runtime == null)
            {
                Debug.Log(
                    "[FavourWindow]   Visible entry = NULL"
                );

                continue;
            }

            Debug.Log(
                $"[FavourWindow]   Visible: " +
                $"{runtime.DisplayName} | State: {runtime.State} | " +
                $"Same as completed: {runtime == completedRuntime}"
            );

            if (runtime == completedRuntime ||
                runtime.State ==
                    FavourState.Completed)
            {
                continue;
            }

            Debug.Log(
                $"[FavourWindow] SWITCHING TO VISIBLE FAVOUR: " +
                $"{runtime.DisplayName}"
            );

            SwitchRuntime(
                runtime
            );

            return true;
        }

        Debug.LogWarning(
            "[FavourWindow] Ingen nästa favour kunde hittas."
        );

        return false;
    }

    private void SwitchRuntime(
        FavourRuntime runtime)
    {
        if (runtime == null ||
            runtime == CurrentRuntime)
        {
            return;
        }

        UnsubscribeFromRuntime();

        CurrentRuntime =
            runtime;

        SubscribeToRuntime();

        RebuildAll();
    }

    private void ClearDynamicContent()
    {
        ClearObjectiveRows();
        ClearFixedRewards();
    }

    private void ClearObjectiveRows()
    {
        foreach (FavourObjectiveRowUI row
                 in objectiveRows)
        {
            if (row != null)
            {
                Destroy(
                    row.gameObject
                );
            }
        }

        objectiveRows.Clear();
    }

    private void ClearFixedRewards()
    {
        foreach (FavourRewardEntryUI view
                 in fixedRewardViews)
        {
            if (view == null)
                continue;

            view.gameObject.SetActive(
                false
            );

            Destroy(
                view.gameObject
            );
        }

        fixedRewardViews.Clear();

        if (fixedRewardsSection != null)
        {
            fixedRewardsSection.SetActive(
                false
            );
        }
    }

    private void SetOpen(
        bool open)
    {
        if (windowRoot != null)
        {
            windowRoot.SetActive(
                open
            );
        }
        else
        {
            gameObject.SetActive(
                open
            );
        }
    }
}
