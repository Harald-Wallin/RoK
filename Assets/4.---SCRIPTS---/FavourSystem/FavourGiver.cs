using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Äger en uppsättning favours och exponerar dem genom det
/// gemensamma interaktionssystemet.
/// </summary>
public sealed class FavourGiver :
    MonoBehaviour,
    IInteractionOptionProvider
{
    [Header("Identity")]

    [SerializeField]
    [Tooltip(
        "Valfritt presentationsnamn. Om tomt används " +
        "CharacterStats.DisplayName eller GameObject-namnet.")]
    private string giverName;

    [Header("Favours")]

    [SerializeField]
    private List<FavourData> favours =
        new();

    [Header("Interaction")]

    [SerializeField]
    [Tooltip(
        "Registrerar ExplicitAccept-favours när spelaren " +
        "interagerar med objektet.")]
    private bool registerOnInteraction = true;

    public string GiverName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(
                    giverName))
            {
                return giverName;
            }

            CharacterStats character =
                GetComponent<CharacterStats>();

            if (character != null)
            {
                return character.DisplayName;
            }

            return gameObject.name;
        }
    }

    public IReadOnlyList<FavourData> Favours =>
        favours;

    private void Start()
    {
        RegisterBackgroundFavours();

        RegisterMarkerTrackedFavours();

        TrySubscribeToManager();

        RefreshMarker();
    }

    private void OnEnable()
    {
        TrySubscribeToManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromManager();
    }


    // ========================================================
    // IDENTITY
    // =======================================================

    private EntityIdentity entityIdentity;

    public EntityIdentity EntityIdentity
    {
        get
        {
            if (entityIdentity == null)
            {
                entityIdentity =
                    GetComponent<
                        EntityIdentity>();
            }

            return entityIdentity;
        }
    }

    public string EntityId =>
        EntityIdentity != null
            ? EntityIdentity.Id
            : string.Empty;

    // =========================================================
    // INTERACTION
    // =========================================================


    private bool HasLocalInteraction(
    PlayerFavourManager manager)
    {
        if (manager == null)
            return false;

        foreach (FavourData favour
                 in favours)
        {
            if (favour == null)
                continue;

            /*
             * Oregistrerad favour måste kunna interageras med
             * så att ExplicitAccept/DiscoverOnInteraction kan
             * registrera den.
             */
            if (!manager.TryGetRuntime(
                    favour,
                    out FavourRuntime runtime))
            {
                return true;
            }

            if (runtime != null &&
                ShouldShowRuntimeHere(
                    runtime))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasRelevantRuntime(
    PlayerFavourManager manager)
    {
        if (manager == null ||
            string.IsNullOrWhiteSpace(
                EntityId))
        {
            return false;
        }

        foreach (FavourRuntime runtime
                 in manager.Runtimes)
        {
            if (runtime == null)
                continue;

            if (runtime.State ==
                    FavourState.ReadyToTurnIn &&
                IsCompletionTargetFor(
                    runtime))
            {
                return true;
            }

            if (runtime.State ==
                    FavourState.Completed &&
                IsCompletionTargetFor(
                    runtime) &&
                !string.IsNullOrWhiteSpace(
                    GetDialogueFor(
                        runtime)))
            {
                return true;
            }

            if (runtime.State !=
                FavourState.Active)
            {
                continue;
            }

            foreach (FavourObjectiveRuntime objective
                     in runtime.Objectives)
            {
                if (objective is
                        InteractObjectiveRuntime interact &&
                    interact.RequiresTarget(
                        EntityId))
                {
                    return true;
                }

                if (objective is
                        DeliverObjectiveRuntime deliver &&
                    deliver.RequiresTarget(
                        EntityId))
                {
                    return true;
                }

                if (objective is
                        EscortObjectiveRuntime escort &&
                    escort.IsWaitingToStart &&
                    escort.RequiresEscortNpc(
                        EntityId))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool IsCompletionTargetFor(
    FavourRuntime runtime)
    {
        if (runtime == null ||
            runtime.Data == null)
        {
            return false;
        }

        switch (runtime.CompletionPolicy)
        {
            case FavourCompletionPolicy
                .ReturnToGiver:

                return ContainsFavour(
                    runtime.Data
                );

            case FavourCompletionPolicy
                .CompleteAtTarget:

            case FavourCompletionPolicy
                .CompleteAtWorldObject:

                return EntityTargetUtility
                    .Matches(
                        EntityIdentity,
                        runtime
                            .CompletionTargetId
                    );

            default:

                return false;
        }
    }

    public bool CanTurnIn(
    FavourData favour)
    {
        if (favour == null)
            return false;

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null ||
            !manager.TryGetRuntime(
                favour,
                out FavourRuntime runtime))
        {
            return false;
        }

        return runtime != null &&
               runtime.State ==
                   FavourState.ReadyToTurnIn &&
               IsCompletionTargetFor(
                   runtime
               );
    }

    /// <summary>
    /// Producerar ett separat interaction-alternativ för varje
    /// FavourRuntime som är relevant hos denna entity just nu.
    /// </summary>
    public void GetInteractionOptions(
        in InteractionContext context,
        List<IInteractionOption> results)
    {
        if (results == null ||
            !context.IsValid)
        {
            return;
        }

        NPCBehavior npcBehavior =
            GetComponentInParent<
                NPCBehavior>();

        if (npcBehavior != null &&
            npcBehavior
                .IsFavourInteractionSuppressed)
        {
            return;
        }

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return;

        /*
         * Viktigt:
         *
         * DiscoverOnInteraction kan behöva registreras först
         * när spelaren faktiskt interagerar med givaren.
         *
         * Vi behåller därför FavourGivers befintliga
         * registreringspolicy innan vi frågar efter synliga
         * runtimes.
         */
        RegisterInteractionFavours(
            manager
        );

        List<FavourRuntime> visibleFavours =
            GetVisibleFavours();

        for (int i = 0;
             i < visibleFavours.Count;
             i++)
        {
            FavourRuntime runtime =
                visibleFavours[i];

            if (runtime == null ||
                runtime.Data == null)
            {
                continue;
            }

            results.Add(
                new FavourInteractionOption(
                    this,
                    runtime
                )
            );
        }
    }

    [Header("Favour Marker")]

    [SerializeField]
    [Tooltip(
    "Marker-prefaben som automatiskt skapas under denna " +
    "FavourGiver om ingen FavourMarker redan finns.")]
    private FavourMarker markerPrefab;

    [SerializeField]
    [Tooltip(
        "Valfri parent för markern. Om tom används FavourGiver-rooten.")]
    private Transform markerParent;

    [SerializeField]
    private bool autoCreateMarker =
        true;

    [SerializeField]
    [Tooltip(
        "Om aktiverad visas Bronze även för konfigurerade " +
        "favours som ännu inte registrerats hos spelaren. " +
        "Bra för vanliga quest/favour-givers.")]
    private bool showUnregisteredFavours =
        true;

    private FavourMarker marker;

    private PlayerFavourManager
        subscribedManager;

    // =========================================================
    // REGISTRATION
    // =========================================================

    /// <summary>
    /// Registrerar favours som ska följas redan innan spelaren
    /// har upptäckt eller interagerat med givaren.
    /// </summary>
    private void RegisterBackgroundFavours()
    {
        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return;

        foreach (FavourData favour in favours)
        {
            if (favour == null)
                continue;

            if (favour.ActivationPolicy !=
                FavourActivationPolicy.TrackBeforeDiscovery)
            {
                continue;
            }

            FavourRuntime runtime =
                manager.RegisterFavour(
                    favour);

            if (runtime != null &&
                runtime.State ==
                FavourState.Available)
            {
                runtime.TryActivate();
            }
        }
    }

    private void RegisterMarkerTrackedFavours()
    {
        if (!showUnregisteredFavours)
            return;

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return;

        foreach (FavourData favour
                 in favours)
        {
            if (favour == null)
                continue;

            /*
             * TrackBeforeDiscovery hanteras redan av
             * RegisterBackgroundFavours().
             */
            if (favour.ActivationPolicy ==
                FavourActivationPolicy.TrackBeforeDiscovery)
            {
                continue;
            }
            if (favour.ActivationPolicy ==
                FavourActivationPolicy.ExplicitAccept)
            {
                manager.RegisterFavour(
                    favour
                );
            }
        }
    }

    /// <summary>
    /// Applicerar varje favours aktiveringspolicy när spelaren
    /// interagerar med givaren.
    /// </summary>
    private void RegisterInteractionFavours(
        PlayerFavourManager manager)
    {
        if (manager == null)
            return;

        foreach (FavourData favour in favours)
        {
            if (favour == null)
                continue;

            switch (favour.ActivationPolicy)
            {
                case FavourActivationPolicy.ExplicitAccept:
                    RegisterExplicitAcceptFavour(
                        manager,
                        favour);

                    break;

                case FavourActivationPolicy.DiscoverOnInteraction:
                    RegisterDiscoveredFavour(
                        manager,
                        favour);

                    break;

                case FavourActivationPolicy.TrackBeforeDiscovery:
                    manager.RegisterFavour(
                        favour);

                    break;
            }
        }
    }

    private void RegisterExplicitAcceptFavour(
        PlayerFavourManager manager,
        FavourData favour)
    {
        if (!registerOnInteraction)
            return;

        manager.RegisterFavour(
            favour);
    }

    private static void RegisterDiscoveredFavour(
        PlayerFavourManager manager,
        FavourData favour)
    {
        FavourRuntime runtime =
            manager.RegisterFavour(
                favour);

        if (runtime != null &&
            runtime.State ==
            FavourState.Available)
        {
            runtime.TryActivate();
        }
    }

    // =========================================================
    // FAVOUR MARKER
    // =========================================================

    private void EnsureMarker()
    {
        if (marker != null)
        {
            RefreshMarkerAnchor();
            return;
        }

        marker =
            GetComponentInChildren<
                FavourMarker>(
                    true
                );

        if (marker != null)
        {
            RefreshMarkerAnchor();
            return;
        }

        if (!autoCreateMarker ||
            markerPrefab == null)
        {
            return;
        }

        Transform parent =
            markerParent != null
                ? markerParent
                : transform;

        marker =
            Instantiate(
                markerPrefab,
                parent
            );

        marker.transform.localPosition =
            Vector3.zero;

        marker.transform.localRotation =
            Quaternion.identity;

        RefreshMarkerAnchor();
    }

    private void TrySubscribeToManager()
    {
        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return;

        if (subscribedManager ==
            manager)
        {
            return;
        }

        UnsubscribeFromManager();

        subscribedManager =
            manager;

        subscribedManager.FavourRegistered +=
            HandleMarkerFavourChanged;

        subscribedManager.FavourStateChanged +=
            HandleMarkerFavourChanged;

        subscribedManager.FavourProgressChanged +=
            HandleMarkerFavourChanged;
    }

    private void UnsubscribeFromManager()
    {
        if (subscribedManager == null)
            return;

        subscribedManager.FavourRegistered -=
            HandleMarkerFavourChanged;

        subscribedManager.FavourStateChanged -=
            HandleMarkerFavourChanged;

        subscribedManager.FavourProgressChanged -=
            HandleMarkerFavourChanged;

        subscribedManager =
            null;
    }

    private void HandleMarkerFavourChanged(
    FavourRuntime runtime)
    {
        RefreshMarker();
    }

    private void RefreshMarker()
    {
        FavourMarkerVisualState state =
            GetMarkerState();

        /*
         * Skapa aldrig en marker enbart för att hålla den Hidden.
         */
        if (state ==
                FavourMarkerVisualState.Hidden &&
            marker == null)
        {
            return;
        }

        EnsureMarker();

        if (marker == null)
            return;

        marker.SetState(
            state
        );
    }

    private FavourMarkerVisualState
GetMarkerState()
    {
        NPCBehavior npcBehavior =
        GetComponentInParent<
            NPCBehavior>();

        if (npcBehavior != null &&
            npcBehavior
                .IsFavourInteractionSuppressed)
        {
            return
                FavourMarkerVisualState.Hidden;
        }

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
        {
            return
                FavourMarkerVisualState.Hidden;
        }

        FavourMarkerVisualState
            strongestState =
                FavourMarkerVisualState.Hidden;

        /*
         * Favours som denna entity själv erbjuder.
         */
        foreach (FavourData favour
                 in favours)
        {
            if (favour == null)
                continue;

            if (!manager.TryGetRuntime(
                favour,
                out FavourRuntime runtime))
            {
                continue;
            }

            if (runtime == null)
                continue;

            switch (runtime.State)
            {
                case FavourState.Available:

                    strongestState =
                        GetStrongerMarkerState(
                            strongestState,
                            FavourMarkerVisualState
                                .Bronze
                        );

                    break;

                case FavourState.Active:

                    if (CanDeliver(
                            runtime))
                    {
                        return
                            FavourMarkerVisualState
                                .Gold;
                    }

                    /*
                     * En Escort-favour som nu väntar hos en ANNAN NPC
                     * ska inte lämna kvar Silver på originalgivaren.
                     */
                    if (ShouldSuppressLocalEscortPresentation(
                            runtime))
                    {
                        break;
                    }

                    strongestState =
                        GetStrongerMarkerState(
                            strongestState,
                            FavourMarkerVisualState
                                .Silver
                        );

                    break;

                case FavourState.ReadyToTurnIn:

                    /*
                     * GOLD visas där favourn faktiskt kan
                     * lämnas in.
                     */
                    if (IsCompletionTargetFor(
                            runtime))
                    {
                        return
                            FavourMarkerVisualState
                                .Gold;
                    }

                    /*
                     * Courier är tekniskt ReadyToTurnIn
                     * direkt, men hos originalgivaren är
                     * den fortfarande en aktiv favour.
                     */
                    if (runtime.IsCourier &&
                        ContainsFavour(
                            runtime.Data))
                    {
                        strongestState =
                            GetStrongerMarkerState(
                                strongestState,
                                FavourMarkerVisualState
                                    .Silver
                            );
                    }

                    break;
            }
        }

        foreach (FavourRuntime runtime
                 in manager.Runtimes)
        {
            if (runtime == null)
                continue;


            if (runtime.State ==
                    FavourState.ReadyToTurnIn &&
                IsCompletionTargetFor(
                    runtime))
            {
                return
                    FavourMarkerVisualState
                        .Gold;
            }

            if (runtime.State ==
                 FavourState.Active &&
               IsEscortStartTargetFor(
                 runtime))
            {
                strongestState =
                    GetStrongerMarkerState(
                        strongestState,
                        FavourMarkerVisualState.Silver
                    );
            }

            if (runtime.State ==
                    FavourState.Active &&
                CanDeliver(
                    runtime))
            {
                return
                    FavourMarkerVisualState
                        .Gold;
            }
        }

        return strongestState;
    }


    private static FavourMarkerVisualState
        GetStrongerMarkerState(
            FavourMarkerVisualState current,
            FavourMarkerVisualState candidate)
    {
        return GetMarkerPriority(candidate) >
               GetMarkerPriority(current)
            ? candidate
            : current;
    }

    private static int GetMarkerPriority(
        FavourMarkerVisualState state)
    {
        switch (state)
        {
            case FavourMarkerVisualState
                .Gold:

                return 3;

            case FavourMarkerVisualState
                .Silver:

                return 2;

            case FavourMarkerVisualState
                .Bronze:

                return 1;

            default:
                return 0;
        }
    }

    private void RefreshMarkerAnchor()
    {
        if (marker == null)
            return;

        InteractionTarget target =
            GetComponentInChildren<
                InteractionTarget>(
                    true
                );

        if (target == null ||
            target.InteractionCollider == null)
        {
            return;
        }

        marker.AnchorToColliderBottom(
            target.InteractionCollider
        );
    }

    // =========================================================
    // FAVOUR ACCESS
    // =========================================================

    /// <summary>
    /// Öppnar FavourWindow med denna giver och väljer
    /// en specifik FavourRuntime.
    ///
    /// Används av FavourInteractionOption när spelaren
    /// väljer en Favour i den generella interaction-menyn.
    /// </summary>
    public void OpenFavour(
        FavourRuntime runtime,
        InteractionTarget target)
    {
        if (runtime == null ||
            runtime.Data == null ||
            target == null)
        {
            return;
        }

        if (!TryGetVisibleRuntime(
                runtime.Data,
                out FavourRuntime visibleRuntime) ||
            visibleRuntime != runtime)
        {
            return;
        }

        FavourWindow window =
            FavourWindow.Instance;

        if (window == null)
        {
            Debug.LogWarning(
                $"'{name}' försökte öppna FavourWindow, men " +
                "inget aktivt FavourWindow hittades.",
                this
            );

            return;
        }

        window.Open(
            this,
            runtime,
            target
        );

        RefreshMarker();
    }

    public bool TryAccept(
        FavourData favour)
    {
        if (!ContainsFavour(
                favour))
        {
            return false;
        }

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        return manager != null &&
               manager.TryAccept(
                   favour);
    }

    public bool TryTurnIn(
    FavourData favour)
    {
        if (!CanTurnIn(
                favour))
        {
            return false;
        }

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return false;

        bool completed =
            manager.TryTurnIn(
                favour
            );

        if (!completed)
            return false;

        RegisterInteractionFavours(
            manager
        );

        RefreshMarker();

        return true;
    }

    public List<FavourRuntime>
    GetVisibleFavours()
    {
        List<FavourRuntime> result =
            new();

        HashSet<string> addedIds =
            new(
                System.StringComparer.Ordinal
            );

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return result;

        /*
         * Favours som denna entity själv erbjuder.
         */
        foreach (FavourData favour
                 in favours)
        {
            if (favour == null)
                continue;

            if (!manager.TryGetRuntime(
                    favour,
                    out FavourRuntime runtime))
            {
                continue;
            }

            if (!ShouldShowRuntimeHere(
                    runtime))
            {
                continue;
            }

            if (addedIds.Add(
                    runtime.Id))
            {
                result.Add(
                    runtime
                );
            }
        }

        /*
         * Favours som denna entity är target/recipient för,
         * utan att själv behöva äga FavourData i Inspector.
         */
        foreach (FavourRuntime runtime
                 in manager.Runtimes)
        {
            if (runtime == null ||
                addedIds.Contains(
                    runtime.Id))
            {
                continue;
            }

            if (!ShouldShowRuntimeHere(
                    runtime))
            {
                continue;
            }

            if (addedIds.Add(
                    runtime.Id))
            {
                result.Add(
                    runtime
                );
            }
        }

        return result;
    }

    private bool ShouldSuppressLocalEscortPresentation(
    FavourRuntime runtime)
    {
        if (runtime == null ||
            runtime.State !=
                FavourState.Active ||
            !ContainsFavour(
                runtime.Data) ||
            string.IsNullOrWhiteSpace(
                EntityId))
        {
            return false;
        }

        foreach (FavourObjectiveRuntime objective
                 in runtime.Objectives)
        {
            if (objective is not
                EscortObjectiveRuntime escort)
            {
                continue;
            }

            if (escort.RequiresEscortNpc(
                    EntityId))
            {
                return false;
            }

            if (!escort.IsComplete)
            {
                return true;
            }
        }

        return false;
    }

    private bool ShouldShowRuntimeHere(
    FavourRuntime runtime)
    {
        if (runtime == null ||
            runtime.Data == null ||
            runtime.State ==
                FavourState.Unavailable)
        {
            return false;
        }

        bool localGiver =
            ContainsFavour(
                runtime.Data
            );

        bool completionTarget =
            IsCompletionTargetFor(
                runtime
            );

        bool deliveryTarget =
            IsDeliveryTargetFor(
                runtime
            );

        bool escortStartTarget =
            IsEscortStartTargetFor(
                runtime
            );

        if (runtime.State ==
            FavourState.ReadyToTurnIn)
        {
            if (completionTarget)
                return true;

            return
                runtime.IsCourier &&
                localGiver;
        }

        if (runtime.State ==
            FavourState.Completed)
        {
            if (runtime.UsesSpecificCompletionTarget)
            {
                if (!completionTarget)
                    return false;
            }
            else if (!localGiver)
            {
                return false;
            }

            string dialogue =
                GetDialogueFor(
                    runtime
                );

            return
                !string.IsNullOrWhiteSpace(
                    dialogue
                );
        }

        if (runtime.State ==
            FavourState.Active)
        {
            bool showLocalGiver =
                localGiver &&
                !ShouldSuppressLocalEscortPresentation(
                    runtime
                );

            return
                showLocalGiver ||
                deliveryTarget ||
                escortStartTarget;
        }

        return localGiver;
    }

    public bool TryGetVisibleRuntime(
    FavourData favour,
    out FavourRuntime runtime)
    {
        runtime = null;

        if (favour == null)
            return false;

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null ||
            !manager.TryGetRuntime(
                favour,
                out runtime))
        {
            runtime = null;
            return false;
        }

        if (!ShouldShowRuntimeHere(
                runtime))
        {
            runtime = null;
            return false;
        }

        return true;
    }

    private bool ContainsFavour(
        FavourData favour)
    {
        return favour != null &&
               favours.Contains(
                   favour);
    }

    private bool HasConfiguredFavour()
    {
        foreach (FavourData favour in favours)
        {
            if (favour != null)
                return true;
        }

        return false;
    }


    // =========================================================
    // Dialogue
    // =========================================================
    public string GetDialogueFor(
    FavourRuntime runtime)
    {
        if (runtime?.Data == null)
            return string.Empty;

        bool localGiver =
            ContainsFavour(
                runtime.Data
            );

        bool completionTarget =
            IsCompletionTargetFor(
                runtime
            );

        /*
         * Completion target äger ReadyToTurnIn-
         * och Completed-dialogen.
         */
        if (completionTarget &&
            runtime.Data
                .CompletionDialogueSet != null)
        {
            if (runtime.State ==
                    FavourState.ReadyToTurnIn ||
                runtime.State ==
                    FavourState.Completed)
            {
                string completionDialogue =
                    runtime.Data
                        .CompletionDialogueSet
                        .GetDialogue(
                            runtime.State
                        );

                if (!string.IsNullOrWhiteSpace(
                        completionDialogue))
                {
                    return completionDialogue;
                }
            }
        }

        /*
         * Courier blir ReadyToTurnIn direkt i runtime,
         * men hos originalgivaren ska den presenteras
         * som Active.
         *
         * Exempel:
         * "Did you talk to Hirdman Fanarik yet?"
         */
        if (runtime.IsCourier &&
            runtime.State ==
                FavourState.ReadyToTurnIn &&
            localGiver &&
            !completionTarget &&
            runtime.Data.DialogueSet != null)
        {
            return runtime.Data
                .DialogueSet
                .GetDialogue(
                    FavourState.Active
                );
        }

        /*
         * Har favourn ett separat completion target
         * får originalgivaren ingen Completed-dialog.
         */
        if (runtime.State ==
                FavourState.Completed &&
            runtime.UsesSpecificCompletionTarget &&
            !completionTarget)
        {
            return string.Empty;
        }

        if (localGiver &&
            runtime.Data.DialogueSet != null)
        {
            return runtime.Data
                .DialogueSet
                .GetDialogue(
                    runtime.State
                );
        }

        if (completionTarget &&
            runtime.Data
                .CompletionDialogueSet != null)
        {
            return runtime.Data
                .CompletionDialogueSet
                .GetDialogue(
                    runtime.State
                );
        }

        return string.Empty;
    }

    public bool IsDeliveryTargetFor(
    FavourRuntime runtime)
    {
        if (runtime == null ||
            runtime.State !=
                FavourState.Active ||
            string.IsNullOrWhiteSpace(
                EntityId))
        {
            return false;
        }

        foreach (FavourObjectiveRuntime objective
                 in runtime.Objectives)
        {
            if (objective is not
                DeliverObjectiveRuntime deliver)
            {
                continue;
            }

            if (deliver.RequiresTarget(
                    EntityId))
            {
                return true;
            }
        }

        return false;
    }

    public bool CanDeliver(
    FavourRuntime runtime)
    {
        if (runtime == null ||
            runtime.State !=
                FavourState.Active ||
            string.IsNullOrWhiteSpace(
                EntityId))
        {
            return false;
        }

        foreach (FavourObjectiveRuntime objective
                 in runtime.Objectives)
        {
            if (objective is
                    DeliverObjectiveRuntime deliver &&
                deliver.CanDeliverTo(
                    EntityId))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryDeliver(
        FavourRuntime runtime)
    {
        if (runtime == null ||
            runtime.State !=
                FavourState.Active ||
            string.IsNullOrWhiteSpace(
                EntityId))
        {
            return false;
        }

        bool delivered =
            false;

        foreach (FavourObjectiveRuntime objective
                 in runtime.Objectives)
        {
            if (objective is not
                DeliverObjectiveRuntime deliver)
            {
                continue;
            }

            if (!deliver.TryDeliverTo(
                    EntityId))
            {
                continue;
            }

            delivered = true;
        }

        if (delivered)
        {
            RefreshMarker();
        }

        return delivered;
    }

    public bool IsEscortStartTargetFor(
    FavourRuntime runtime)
    {
        if (runtime == null ||
            runtime.State !=
                FavourState.Active ||
            string.IsNullOrWhiteSpace(
                EntityId))
        {
            return false;
        }

        foreach (FavourObjectiveRuntime objective
                 in runtime.Objectives)
        {
            if (objective is not
                EscortObjectiveRuntime escort)
            {
                continue;
            }

            if (escort.IsWaitingToStart &&
                escort.RequiresEscortNpc(
                    EntityId))
            {
                return true;
            }
        }

        return false;
    }


    public bool CanStartEscort(
        FavourRuntime runtime)
    {
        return IsEscortStartTargetFor(
            runtime
        );
    }


    public bool TryStartEscort(
        FavourRuntime runtime)
    {
        if (runtime == null ||
            runtime.State !=
                FavourState.Active ||
            string.IsNullOrWhiteSpace(
                EntityId))
        {
            return false;
        }

        bool started =
            false;

        foreach (FavourObjectiveRuntime objective
                 in runtime.Objectives)
        {
            if (objective is not
                EscortObjectiveRuntime escort)
            {
                continue;
            }

            if (!escort.TryStartEscort(
                    EntityId))
            {
                continue;
            }

            started = true;

            /*
             * En favour ska normalt bara ha en aktiv escort-session.
             * Stoppa efter den objective som faktiskt startades.
             */
            break;
        }

        if (started)
        {
            RefreshMarker();
        }

        return started;
    }
}