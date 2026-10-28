using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public sealed class PlayerFavourManager :
    MonoBehaviour
{
    private readonly Dictionary<
        string,
        FavourRuntime>
        runtimesById =
            new();

    private readonly List<FavourRuntime>
        runtimeSnapshot =
            new();

    public event Action<FavourRuntime>
    FavourRewardSelectionChanged;

    [Header("Optional Starting Favours")]

    [SerializeField]
    private List<FavourData>
        startingFavours =
            new();

    public static PlayerFavourManager Instance
    {
        get;
        private set;
    }

    public PlayerStats Player
    {
        get;
        private set;
    }

    public Inventory PlayerInventory
    {
        get;
        private set;
    }

    public PlayerItemOwnership PlayerItems
    {
        get;
        private set;
    }

    public PlayerCurrency PlayerCurrency
    {
        get;
        private set;
    }

    public PlayerReputationManager PlayerReputation
    {
        get;
        private set;
    }

    public PlayerAbilityCollection PlayerAbilities
    {
        get;
        private set;
    }


    public event Action<FavourRuntime>
        FavourRegistered;

    public event Action<FavourRuntime>
        FavourStateChanged;

    public event Action<FavourRuntime>
        FavourProgressChanged;

    public event Action<
    FavourRuntime,
    FavourObjectiveRuntime>
    FavourObjectiveProgressChanged;

    public IEnumerable<FavourRuntime> Runtimes =>
        runtimesById.Values;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogWarning(
                "Flera PlayerFavourManager hittades. " +
                "Den nya komponenten stängs av.",
                this
            );

            enabled = false;
            return;
        }

        Instance = this;

        ResolveLocalReferences();
    }

    private void Update()
    {
        if (runtimesById.Count == 0)
            return;

        CreateRuntimeSnapshot();

        float deltaTime =
            Time.deltaTime;

        foreach (FavourRuntime runtime
                 in runtimeSnapshot)
        {
            runtime?.Tick(
                deltaTime
            );
        }
    }

    private void OnEnable()
    {
        CharacterCombatEvents
            .CharacterDefeated +=
            HandleCharacterDefeated;
    }

    private void Start()
    {
        ResolveReferences();

        ResolveItemOwnership();

        SubscribeToPlayerSystems();

        RegisterStartingFavours();

        RefreshAllAvailability();
    }

    private void OnDisable()
    {
        CharacterCombatEvents
            .CharacterDefeated -=
            HandleCharacterDefeated;

        UnsubscribeFromPlayerSystems();
    }

    private void OnDestroy()
    {
        UnsubscribeFromRuntimes();

        PlayerItems?.Dispose();

        PlayerItems = null;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void ResolveLocalReferences()
    {
        Player =
            GetComponent<PlayerStats>();

        PlayerInventory =
            GetComponent<Inventory>();

        if (PlayerInventory == null)
        {
            PlayerInventory =
                GetComponentInChildren<
                    Inventory>(
                    true
                );
        }

        PlayerCurrency =
            GetComponent<PlayerCurrency>();

        if (PlayerCurrency == null)
        {
            PlayerCurrency =
                GetComponentInChildren<
                    PlayerCurrency>(
                    true
                );
        }

        PlayerReputation =
            GetComponent<
                PlayerReputationManager>();

        if (PlayerReputation == null)
        {
            PlayerReputation =
                GetComponentInChildren<
                    PlayerReputationManager>(
                    true
                );
        }

        PlayerAbilities =
            GetComponent<
                PlayerAbilityCollection>();

        if (PlayerAbilities == null)
        {
            PlayerAbilities =
                GetComponentInChildren<
                    PlayerAbilityCollection>(
                    true
                );
        }
    }

    private void ResolveReferences()
    {
        if (Player == null)
        {
            Player =
                GetComponent<PlayerStats>();
        }

        if (PlayerInventory == null)
        {
            PlayerInventory =
                Inventory.Instance;
        }

        if (PlayerCurrency == null)
        {
            PlayerCurrency =
                global::PlayerCurrency.Instance;
        }

        if (PlayerReputation == null &&
            Player != null)
        {
            PlayerReputation =
                Player.GetComponent<
                    PlayerReputationManager>();
        }

        if (PlayerAbilities == null &&
            Player != null)
        {
            PlayerAbilities =
                Player.GetComponentInChildren<
                    PlayerAbilityCollection>(
                    true
                );
        }
    }

    private void ResolveItemOwnership()
    {
        PlayerItems?.Dispose();

        PlayerItems =
            new PlayerItemOwnership(
                PlayerInventory,
                EquipmentManager.Instance
            );
    }

    private void SubscribeToPlayerSystems()
    {
        UnsubscribeFromPlayerSystems();

        if (PlayerItems != null)
        {
            PlayerItems.Changed +=
                HandlePlayerItemsChanged;
        }

        if (PlayerCurrency != null)
        {
            PlayerCurrency
                .OnCoinsChanged +=
                HandleRequirementSourceChanged;
        }

        if (Player != null)
        {
            Player.OnLevelChanged +=
                HandleRequirementSourceChanged;
        }

        if (PlayerReputation != null)
        {
            PlayerReputation
                .OnReputationChanged +=
                HandleReputationChanged;
        }
    }

    private void UnsubscribeFromPlayerSystems()
    {
            if (PlayerItems != null)
            {
            PlayerItems.Changed -=
                HandlePlayerItemsChanged;
        }

        if (PlayerCurrency != null)
        {
            PlayerCurrency
                .OnCoinsChanged -=
                HandleRequirementSourceChanged;
        }

        if (Player != null)
        {
            Player.OnLevelChanged -=
                HandleRequirementSourceChanged;
        }

        if (PlayerReputation != null)
        {
            PlayerReputation
                .OnReputationChanged -=
                HandleReputationChanged;
        }
    }

    private void HandlePlayerItemsChanged()
    {
        RefreshAllAvailability();
        ValidateActiveSourceItems();
    }

    private void ValidateActiveSourceItems()
    {
        if (PlayerItems == null ||
            runtimesById.Count == 0)
        {
            return;
        }

        CreateRuntimeSnapshot();

        foreach (FavourRuntime runtime
                 in runtimeSnapshot)
        {
            runtime?.ValidateSourceItemOwnership();
        }
    }

    private void HandleRequirementSourceChanged()
    {
        RefreshAllAvailability();
    }

    private void HandleReputationChanged(
        FactionReputationData reputation)
    {
        RefreshAllAvailability();
    }

    private void RegisterStartingFavours()
    {
        foreach (FavourData favour
                 in startingFavours)
        {
            RegisterFavour(
                favour
            );
        }
    }

    public FavourRuntime RegisterFavour(
        FavourData favour)
    {
        if (favour == null)
            return null;

        if (string.IsNullOrWhiteSpace(
                favour.Id))
        {
            Debug.LogError(
                $"Favour '{favour.name}' saknar permanent ID.",
                favour
            );

            return null;
        }

        if (runtimesById.TryGetValue(
                favour.Id,
                out FavourRuntime existing))
        {
            existing.RefreshAvailability();

            return existing;
        }

        FavourRuntime runtime =
            new FavourRuntime(
                favour,
                this
            );

        runtimesById.Add(
            favour.Id,
            runtime
        );

        runtime.StateChanged +=
            HandleRuntimeStateChanged;

        runtime.ProgressChanged +=
            HandleRuntimeProgressChanged;

        runtime.ObjectiveProgressChanged +=
            HandleRuntimeObjectiveProgressChanged;

        runtime.RefreshAvailability();

        runtime.RewardSelectionChanged +=
            HandleRuntimeRewardSelectionChanged;

        FavourRegistered?.Invoke(
            runtime
        );

        return runtime;
    }

    private void HandleRuntimeRewardSelectionChanged(
    FavourRuntime runtime)
    {
        FavourRewardSelectionChanged?.Invoke(
            runtime
        );
    }

    private void HandleRuntimeObjectiveProgressChanged(
    FavourRuntime favour,
    FavourObjectiveRuntime objective)
    {
        FavourObjectiveProgressChanged?.Invoke(
            favour,
            objective
        );
    }

    public bool TryGetRuntime(
        FavourData favour,
        out FavourRuntime runtime)
    {
        runtime = null;

        if (favour == null ||
            string.IsNullOrWhiteSpace(
                favour.Id))
        {
            return false;
        }

        return runtimesById.TryGetValue(
            favour.Id,
            out runtime
        );
    }

    public bool TryGetRuntime(
        string favourId,
        out FavourRuntime runtime)
    {
        runtime = null;

        if (string.IsNullOrWhiteSpace(
                favourId))
        {
            return false;
        }

        return runtimesById.TryGetValue(
            favourId,
            out runtime
        );
    }

    public bool IsCompleted(
     FavourData favour)
    {
        return TryGetRuntime(
                   favour,
                   out FavourRuntime runtime
               ) &&
               runtime != null &&
               runtime.HasBeenCompleted;
    }

    public bool HasAccepted(FavourData favour)
    {
        if (!TryGetRuntime(
                favour,
                out FavourRuntime runtime))
        {
            return false;
        }

        if (runtime == null)
            return false;

        return runtime.State !=
                   FavourState.Unavailable &&
               runtime.State !=
                   FavourState.Available;
    }

    public bool TryAccept(
    FavourData favour)
    {
        return TryAccept(
            favour,
            null
        );
    }

    public bool TryAccept(
    FavourData favour,
    ItemData sourceItem)
    {
        FavourRuntime runtime =
            RegisterFavour(
                favour
            );

        if (runtime == null)
            return false;

        if (sourceItem != null &&
            favour != null &&
            favour.RequireSourceItemWhileActive)
        {
            if (PlayerItems == null ||
                !PlayerItems.Contains(sourceItem))
            {
                return false;
            }
        }

        bool activated =
            runtime.TryActivate();

        if (!activated)
            return false;

        runtime.BindSourceItem(
            sourceItem
        );

        return true;
    }

    public bool TryTurnIn(
        FavourData favour)
    {
        if (!TryGetRuntime(
                favour,
                out FavourRuntime runtime))
        {
            return false;
        }

        return runtime.TryTurnIn();
    }

    public bool CanGrantAbility(
    AbilityData ability)
    {
        return
            ability != null &&
            PlayerAbilities != null;
    }

    public bool TryGrantAbility(
    AbilityData ability)
    {
        if (ability == null)
            return false;

        if (PlayerAbilities == null)
        {
            ResolveReferences();
        }

        if (PlayerAbilities == null)
        {
            Debug.LogError(
                $"Kan inte dela ut ability " +
                $"'{ability.name}': spelaren saknar " +
                $"{nameof(PlayerAbilityCollection)}.",
                this
            );

            return false;
        }

        return PlayerAbilities.LearnAbility(
            ability
        );
    }

    private void HandleCharacterDefeated(
        CharacterDefeatedResult result)
    {
        CreateRuntimeSnapshot();

        foreach (FavourRuntime runtime
                 in runtimeSnapshot)
        {
            runtime.HandleCharacterDefeated(
                result
            );
        }
    }

    private void HandleRuntimeStateChanged(
    FavourRuntime runtime)
    {
        if (runtime == null)
            return;

        if (runtime.State ==
            FavourState.Failed)
        {
            AnnouncementSpawner.Instance
                ?.ShowFavourFailed(
                    runtime.DisplayName
                );
        }

        FavourStateChanged?.Invoke(
            runtime
        );

        RefreshAllAvailability();
    }

    private void HandleRuntimeProgressChanged(
        FavourRuntime runtime)
    {
        FavourProgressChanged?.Invoke(
            runtime
        );
    }

    public void RefreshAllAvailability()
    {
        List<FavourRuntime> snapshot =
            new List<FavourRuntime>(
                runtimesById.Values
            );

        foreach (FavourRuntime runtime
                 in snapshot)
        {
            if (runtime == null)
                continue;

            runtime.RefreshAvailability();
        }
    }

    private void CreateRuntimeSnapshot()
    {
        runtimeSnapshot.Clear();

        foreach (FavourRuntime runtime
                 in runtimesById.Values)
        {
            runtimeSnapshot.Add(
                runtime
            );
        }
    }

    private void UnsubscribeFromRuntimes()
    {
        foreach (FavourRuntime runtime
                 in runtimesById.Values)
        {
            if (runtime == null)
                continue;

            runtime.StateChanged -=
                HandleRuntimeStateChanged;

            runtime.ProgressChanged -=
                HandleRuntimeProgressChanged;

            runtime.ObjectiveProgressChanged -=
                HandleRuntimeObjectiveProgressChanged;

            runtime.RewardSelectionChanged -=
                HandleRuntimeRewardSelectionChanged;
        }
    }

    // =========================================================
    // FAVOUR ITEM / COLLECT QUERIES
    // =========================================================

    /// <summary>
    /// Returnerar true om spelaren just nu har minst ett aktivt,
    /// ofärdigt Collect-objective som fortfarande behöver itemet.
    /// </summary>
    public bool IsCollectObjectiveActive(
        ItemData item)
    {
        if (item == null)
            return false;

        foreach (FavourRuntime favour
                 in runtimesById.Values)
        {
            if (CanCollectItemForFavour(
                    favour,
                    item))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returnerar true endast om den angivna favouren just nu
    /// aktivt behöver det angivna itemet.
    ///
    /// Detta är den auktoritativa loot-regeln för FavourItems.
    /// </summary>
    public bool CanDropFavourItem(
        FavourData requiredFavour,
        ItemData item)
    {
        if (requiredFavour == null ||
            item == null)
        {
            return false;
        }

        if (!TryGetRuntime(
                requiredFavour,
                out FavourRuntime runtime))
        {
            return false;
        }

        return CanCollectItemForFavour(
            runtime,
            item
        );
    }

    private static bool CanCollectItemForFavour(
        FavourRuntime favour,
        ItemData item)
    {
        if (favour == null ||
            item == null)
        {
            return false;
        }


        if (favour.State !=
            FavourState.Active)
        {
            return false;
        }

        foreach (FavourObjectiveRuntime objective
                 in favour.Objectives)
        {
            if (objective is not
                CollectObjectiveRuntime collect)
            {
                continue;
            }

            if (!collect.IsActive ||
                collect.IsComplete)
            {
                continue;
            }

            if (!Inventory.ItemsMatch(
                    collect.RequiredItem,
                    item))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public int GetRemainingCollectAmount(
    ItemData item)
    {
        if (item == null)
            return 0;

        int totalRemaining =
            0;

        foreach (FavourRuntime favour
                 in runtimesById.Values)
        {
            if (favour == null ||
                favour.State !=
                    FavourState.Active)
            {
                continue;
            }

            foreach (FavourObjectiveRuntime objective
                     in favour.Objectives)
            {
                if (objective is not
                    CollectObjectiveRuntime collect)
                {
                    continue;
                }

                if (!collect.IsActive ||
                    collect.IsComplete)
                {
                    continue;
                }

                if (!Inventory.ItemsMatch(
                        collect.RequiredItem,
                        item))
                {
                    continue;
                }

                totalRemaining +=
                    Mathf.Max(
                        0,
                        collect.RequiredProgress -
                        collect.CurrentProgress
                    );
            }
        }

        return totalRemaining;
    }

    // =========================================================
    // ITEM FAVOUR INTERACTION
    // =========================================================
    public bool TryResolveItemFavour(
    ItemData item,
    out FavourRuntime runtime)
    {
        runtime = null;

        if (item == null ||
            !item.HasFavourInteraction)
        {
            return false;
        }

        IReadOnlyList<FavourData> linkedFavours =
            item.LinkedFavours;

        if (linkedFavours == null ||
            linkedFavours.Count == 0)
        {
            return false;
        }

        /*
         * Alla item-kopplade Favours registreras när itemet
         * faktiskt används.
         *
         * Vi registrerar dem INTE bara för att spelaren hoverar
         * över itemet i inventoryt.
         */
        List<FavourRuntime> candidates =
            new();

        foreach (FavourData favour
                 in linkedFavours)
        {
            if (favour == null)
                continue;

            FavourRuntime candidate =
                RegisterFavour(
                    favour
                );

            if (candidate == null)
                continue;

            candidates.Add(
                candidate
            );
        }

        if (candidates.Count == 0)
            return false;

        /*
         * 1. Pågående item-favour har högst prioritet.
         *
         * Om spelaren exempelvis redan startat en ring-favour
         * ska högerklick fortsätta öppna samma favour.
         */
        foreach (FavourRuntime candidate
                 in candidates)
        {
            if (candidate.State ==
                    FavourState.Active ||
                candidate.State ==
                    FavourState.ReadyToTurnIn)
            {
                runtime =
                    candidate;

                return true;
            }
        }

        /*
         * 2. Därefter första faktiskt tillgängliga favouren.
         *
         * Requirements i FavourRuntime bestämmer om den är
         * Available. Listordningen i ItemData avgör vilken
         * som väljs om flera samtidigt är möjliga.
         */
        foreach (FavourRuntime candidate
                 in candidates)
        {
            candidate.RefreshAvailability();

            if (candidate.State !=
                FavourState.Available)
            {
                continue;
            }

            runtime =
                candidate;

            return true;
        }

        /*
         * 3. Failed/Cooldown kan fortfarande vara relevant
         * presentation för itemet.
         */
        foreach (FavourRuntime candidate
                 in candidates)
        {
            if (candidate.State ==
                    FavourState.Failed ||
                candidate.State ==
                    FavourState.Cooldown)
            {
                runtime =
                    candidate;

                return true;
            }
        }

        /*
         * 4. Till sist väljer vi första ännu inte historiskt
         * completed favouren.
         *
         * Detta gör att en level-gated framtida favour fortfarande
         * kan presenteras i tooltip/window som Locked/Unavailable.
         */
        foreach (FavourRuntime candidate
                 in candidates)
        {
            if (candidate.HasBeenCompleted)
                continue;

            runtime =
                candidate;

            return true;
        }

        /*
         * Alla itemets Favours är färdiga och ingen är repeatable/
         * aktiv/cooldown-relevant.
         */
        return false;
    }

    public FavourData GetNextItemFavourForPresentation(
    ItemData item)
    {
        if (item == null ||
            !item.HasFavourInteraction)
        {
            return null;
        }

        IReadOnlyList<FavourData> linkedFavours =
            item.LinkedFavours;

        if (linkedFavours == null ||
            linkedFavours.Count == 0)
        {
            return null;
        }

        /*
         * Om en runtime redan finns och är aktiv/relevant
         * använder vi den först.
         */
        foreach (FavourData favour
                 in linkedFavours)
        {
            if (favour == null)
                continue;

            if (!TryGetRuntime(
                    favour,
                    out FavourRuntime runtime) ||
                runtime == null)
            {
                continue;
            }

            if (runtime.State ==
                    FavourState.Active ||
                runtime.State ==
                    FavourState.ReadyToTurnIn ||
                runtime.State ==
                    FavourState.Available ||
                runtime.State ==
                    FavourState.Failed ||
                runtime.State ==
                    FavourState.Cooldown)
            {
                return favour;
            }
        }

        /*
         * Därefter första länkade favouren som inte historiskt
         * redan är completed.
         */
        foreach (FavourData favour
                 in linkedFavours)
        {
            if (favour == null)
                continue;

            if (IsCompleted(
                    favour))
            {
                continue;
            }

            return favour;
        }

        return null;
    }
}