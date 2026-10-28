using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LootContainerLifecycleMode
{
    DespawnAndRespawn,
    RefillInPlace
}

[DisallowMultipleComponent]
public sealed class LootContainer :
    MonoBehaviour,
    ILootSource,
    IInteractionOption
{
    [System.Serializable]
    private sealed class WeightedRespawnNode
    {
        [SerializeField]
        private Transform node;

        [SerializeField, Min(0f)]
        private float weight = 1f;

        public Transform Node =>
            node;

        public float Weight =>
            Mathf.Max(
                0f,
                weight
            );

#if UNITY_EDITOR
        public void Normalize()
        {
            weight =
                Mathf.Max(
                    0f,
                    weight
                );
        }
#endif
    }


    // =====================================================
    // IDENTITY
    // =====================================================

    [Header("Identity")]

    [SerializeField]
    private string containerName;


    // =====================================================
    // LOOT
    // =====================================================

    [Header("Loot")]

    [SerializeField]
    private List<LootTable> lootTables =
        new();

    [SerializeField, Min(0)]
    private int minLootRolls = 1;

    [SerializeField, Min(0)]
    private int maxLootRolls = 1;


    // =====================================================
    // VISUALS
    // =====================================================

    [Header("Visuals")]

    [SerializeField]
    private GameObject lootShimmer;


    // =====================================================
    // LIFECYCLE
    // =====================================================

    [Header("Lifecycle")]

    [Tooltip(
    "Despawn And Respawn: objektet lämnar världen mellan loot-cykler. " +
    "Refill In Place: objektet stannar kvar och får ny loot på samma plats.")]
    [SerializeField]
    private LootContainerLifecycleMode lifecycleMode =
    LootContainerLifecycleMode.DespawnAndRespawn;

    [Tooltip(
    "Sekunder från första öppningen tills den aktuella loot-cykeln avslutas, " +
    "även om loot finns kvar. 0 stänger av denna timer.")]
    [SerializeField, Min(0f)]
    private float lifetimeAfterFirstOpen =
        1800f;

    [Tooltip(
    "Hur länge en tom container väntar innan den aktuella loot-cykeln avslutas.")]
    [SerializeField, Min(0f)]
    private float emptyDespawnDelay =
        7f;


    // =====================================================
    // RESPAWN
    // =====================================================

    [Header("Respawn")]

    [SerializeField]
    [InspectorName("Can Respawn / Refill")]
    private bool canRespawn;

    [Tooltip(
    "Sekunder mellan avslutad loot-cykel och nästa spawn/refill.")]
    [SerializeField, Min(0f)]
    private float respawnTime =
        3600f;

    [Tooltip(
        "Valfria spawnpunkter. Om listan saknar giltiga punkter " +
        "används objektets ursprungliga position. Vikter behöver " +
        "inte summera till 100.")]
    [SerializeField]
    private List<WeightedRespawnNode> respawnNodes =
        new();


    // =====================================================
    // SPAWN VALIDATION
    // =====================================================

    [Header("Spawn Validation")]

    [Tooltip(
    "Containerns fysiska collider. " +
    "Kan vara exempelvis EdgeCollider2D eller BoxCollider2D. " +
    "InteractionHitbox ska INTE användas här. " +
    "Om fältet lämnas tomt används automatiskt den första " +
    "icke-trigger Collider2D på samma GameObject.")]
    [SerializeField]
    private Collider2D spawnFootprintCollider;

    [Tooltip(
        "Extra säkerhetsmarginal runt den fysiska colliderns bounds " +
        "vid spawn-validation. Särskilt användbart för EdgeCollider2D.")]
    [SerializeField]
    private Vector2 spawnFootprintPadding =
        new Vector2(
            0.05f,
            0.05f
        );

    [Tooltip(
        "Vilka layers som får blockera en spawn. " +
        "Default är Everything. Trigger-colliders ignoreras alltid.")]
    [SerializeField]
    private LayerMask spawnBlockingMask =
        ~0;

    [Tooltip(
        "Hur många sekunder containern väntar innan den " +
        "försöker hitta en ledig spawnpunkt igen.")]
    [SerializeField, Min(0.1f)]
    private float blockedRespawnRetryDelay =
        2f;


    // =====================================================
    // RUNTIME CONTENTS
    // =====================================================

    private readonly LootContents contents =
        new();

    private GameObject shimmerInstance;


    // =====================================================
    // PRESENTATION STATE
    // =====================================================

    private Renderer[] cachedRenderers;
    private bool[] rendererInitialStates;

    private Collider2D[] cachedColliders;
    private bool[] colliderInitialStates;


    // =====================================================
    // SPAWN VALIDATION STATE
    // =====================================================

    private readonly List<WeightedRespawnNode>
        availableRespawnNodes =
            new();

    private readonly Collider2D[]
        spawnOverlapResults =
            new Collider2D[8];

    private Vector2 spawnFootprintCenterOffset;
    private Vector2 spawnFootprintSize;

    private bool spawnFootprintReady;


    // =====================================================
    // LIFECYCLE STATE
    // =====================================================

    private Vector3 originalPosition;

    private bool initialized;
    private bool isSpawned;
    private bool lifetimeStarted;

    private Coroutine lifetimeCoroutine;
    private Coroutine emptyDespawnCoroutine;
    private Coroutine respawnCoroutine;
    private Coroutine refillCoroutine;


    // =====================================================
    // LOOT SOURCE
    // =====================================================

    public string LootTitle =>
        !string.IsNullOrWhiteSpace(
            containerName)
            ? containerName
            : gameObject.name;


    public IReadOnlyList<ItemData> LootItems =>
        contents.Items;


    public int CoinAmount =>
        contents.CoinAmount;


    public bool HasLoot =>
        contents.HasLoot;


    // =====================================================
    // INTERACTION OPTION
    // =====================================================

    public InteractionCategory Category =>
        InteractionCategory.Other;


    public InteractionPresentation
        GetPresentation()
    {
        return new InteractionPresentation(
            Category,
            "Loot"
        );
    }


    public string GetStatusText()
    {
        return string.Empty;
    }


    public bool CanInteract(
        in InteractionContext context)
    {
        return
            context.IsValid &&
            initialized &&
            isSpawned &&
            HasLoot &&
            LootUI.Instance != null;
    }


    public void Interact(
        in InteractionContext context)
    {
        if (!CanInteract(
                context))
        {
            return;
        }

        StartLifetimeIfNeeded();

        LootUI.Instance.Show(
            this
        );
    }


    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        originalPosition =
            transform.position;

        CachePresentationState();

        CacheSpawnFootprint();

        /*
         * Objektet hålls dolt tills Start har verifierat
         * att den initiala spawnpositionen faktiskt är fri.
         */
        SetWorldPresentationVisible(
            false
        );
    }


    private void Start()
    {
        initialized = true;

        /*
         * Även den första spawnen valideras.
         *
         * Om exempelvis spelaren redan står på platsen
         * väntar containern tills ytan är fri.
         */
        if (!TrySpawnAtAvailablePosition())
        {
            StartSpawnAttemptRoutine(
                blockedRespawnRetryDelay
            );
        }
    }


    private void OnDestroy()
    {
        DestroyShimmer();
    }


    // =====================================================
    // SPAWN LIFECYCLE
    // =====================================================

    private void BeginSpawnLifecycle()
    {
        StopSpawnTimers();

        contents.Clear();

        lifetimeStarted = false;
        isSpawned = true;

        SetWorldPresentationVisible(
            true
        );

        /*
         * Ny lifecycle = ny loot-generation.
         *
         * Öppna/stäng aldrig rerollar loot.
         */
        GenerateLoot();

        UpdateShimmer();

        /*
         * En generation kan legitimt resultera i noll loot.
         */
        if (!HasLoot)
        {
            ScheduleEmptyLifecycleEnd();
        }
    }


    private void GenerateLoot()
    {
        if (lootTables == null ||
            lootTables.Count == 0)
        {
            return;
        }

        LootGenerationResult result =
            LootGenerator.GenerateLootResult(
                lootTables,
                minLootRolls,
                maxLootRolls
            );

        contents.Apply(
            result
        );
    }


    private void StartLifetimeIfNeeded()
    {
        if (!isSpawned ||
            lifetimeStarted)
        {
            return;
        }

        lifetimeStarted = true;

        /*
         * 0 betyder att containern endast despawnar
         * när den blir tom.
         */
        if (lifetimeAfterFirstOpen <= 0f)
            return;

        lifetimeCoroutine =
            StartCoroutine(
                LifetimeRoutine()
            );
    }


    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(
            lifetimeAfterFirstOpen
        );

        lifetimeCoroutine = null;

        EndCurrentLifecycle();
    }

    private void ScheduleEmptyLifecycleEnd()
    {
        if (!isSpawned ||
            HasLoot ||
            emptyDespawnCoroutine != null ||
            refillCoroutine != null)
        {
            return;
        }

        if (emptyDespawnDelay <= 0f)
        {
            EndCurrentLifecycle();
            return;
        }

        emptyDespawnCoroutine =
            StartCoroutine(
                EmptyLifecycleEndRoutine()
            );
    }


    private IEnumerator EmptyLifecycleEndRoutine()
    {
        yield return new WaitForSeconds(
            emptyDespawnDelay
        );

        emptyDespawnCoroutine = null;

        if (!HasLoot)
        {
            EndCurrentLifecycle();
        }
    }


    // =====================================================
    // DESPAWN
    // =====================================================

    private void EndCurrentLifecycle()
    {
        if (!isSpawned)
            return;

        lifetimeStarted = false;

        StopSpawnTimers();

        DestroyShimmer();

        /*
         * All kvarvarande loot hörde till den lifecycle
         * som nu avslutas.
         */
        contents.Clear();


        /*
         * LootUI kan fortfarande visa denna source när exempelvis
         * lifetime-timern löper ut.
         *
         * Refresh gör att den gamla loot-vyn stängs/uppdateras.
         */
        if (LootUI.Instance != null)
        {
            LootUI.Instance.Refresh();
        }


        switch (lifecycleMode)
        {
            case LootContainerLifecycleMode.RefillInPlace:

                /*
                 * World-objektet är fortfarande spawnat.
                 *
                 * Renderer, fysisk collider och InteractionHitbox
                 * lämnas helt orörda.
                 *
                 * Eftersom HasLoot nu är false kan LootContainer
                 * inte interageras med förrän ny loot genereras.
                 */
                if (canRespawn)
                {
                    StartRefillTimer();
                }

                break;


            case LootContainerLifecycleMode.DespawnAndRespawn:
            default:

                isSpawned = false;

                /*
                 * Här behåller vi exakt det gamla beteendet:
                 * world-presentationen försvinner helt.
                 */
                SetWorldPresentationVisible(
                    false
                );

                if (canRespawn)
                {
                    StartSpawnAttemptRoutine(
                        respawnTime
                    );
                }

                break;
        }
    }


    private void StopSpawnTimers()
    {
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(
                lifetimeCoroutine
            );

            lifetimeCoroutine = null;
        }

        if (emptyDespawnCoroutine != null)
        {
            StopCoroutine(
                emptyDespawnCoroutine
            );

            emptyDespawnCoroutine = null;
        }
    }


    // =====================================================
    // RESPAWN
    // =====================================================

    private void StartSpawnAttemptRoutine(
        float delayBeforeFirstAttempt)
    {
        if (respawnCoroutine != null)
            return;

        respawnCoroutine =
            StartCoroutine(
                SpawnAttemptRoutine(
                    delayBeforeFirstAttempt
                )
            );
    }


    private IEnumerator SpawnAttemptRoutine(
        float delayBeforeFirstAttempt)
    {
        if (delayBeforeFirstAttempt > 0f)
        {
            yield return new WaitForSeconds(
                delayBeforeFirstAttempt
            );
        }
        else
        {
            /*
             * Undvik despawn + respawn under exakt samma frame.
             */
            yield return null;
        }

        while (!isSpawned)
        {
            if (TrySpawnAtAvailablePosition())
            {
                respawnCoroutine = null;
                yield break;
            }

            yield return new WaitForSeconds(
                Mathf.Max(
                    0.1f,
                    blockedRespawnRetryDelay
                )
            );
        }

        respawnCoroutine = null;
    }

    // =====================================================
    // REFILL IN PLACE
    // =====================================================

    private void StartRefillTimer()
    {
        if (refillCoroutine != null)
            return;

        refillCoroutine =
            StartCoroutine(
                RefillRoutine()
            );
    }


    private IEnumerator RefillRoutine()
    {
        if (respawnTime > 0f)
        {
            yield return new WaitForSeconds(
                respawnTime
            );
        }
        else
        {
            /*
             * Även instant refill väntar minst en frame.
             *
             * Det förhindrar en same-frame loop om en
             * loot-generation skulle ge noll loot.
             */
            yield return null;
        }

        refillCoroutine = null;

        if (!isSpawned ||
            lifecycleMode !=
                LootContainerLifecycleMode.RefillInPlace)
        {
            yield break;
        }

        BeginRefillLifecycle();
    }


    private void BeginRefillLifecycle()
    {
        if (!isSpawned)
            return;

        contents.Clear();

        lifetimeStarted = false;

        /*
         * Viktigt:
         * ingen position ändras,
         * ingen spawn-validation görs,
         * inga renderers/colliders togglas.
         *
         * Objektet har stått kvar hela tiden.
         */
        GenerateLoot();

        UpdateShimmer();

        /*
         * Även en refill får legitimt rolla noll loot.
         *
         * Då avslutas den tomma cykeln på samma generella sätt
         * och kan därefter försöka refill:a igen.
         */
        if (!HasLoot)
        {
            ScheduleEmptyLifecycleEnd();
        }
    }

    private bool TrySpawnAtAvailablePosition()
    {
        if (isSpawned)
            return true;

        if (!TryGetAvailableSpawnPosition(
                out Vector3 spawnPosition))
        {
            return false;
        }

        transform.position =
            spawnPosition;

        BeginSpawnLifecycle();

        return true;
    }


    // =====================================================
    // SPAWN POSITION SELECTION
    // =====================================================

    private bool TryGetAvailableSpawnPosition(
        out Vector3 spawnPosition)
    {
        spawnPosition =
            originalPosition;

        availableRespawnNodes.Clear();

        bool hasValidAuthoredNode =
            false;

        /*
         * Om riktiga respawn nodes finns:
         * filtrera först bort blockerade punkter.
         *
         * Weight används EFTER spatial validation.
         */
        if (respawnNodes != null)
        {
            for (int i = 0;
                 i < respawnNodes.Count;
                 i++)
            {
                WeightedRespawnNode node =
                    respawnNodes[i];

                if (node == null ||
                    node.Node == null ||
                    node.Weight <= 0f)
                {
                    continue;
                }

                hasValidAuthoredNode =
                    true;

                if (!IsSpawnPositionClear(
                        node.Node.position))
                {
                    continue;
                }

                availableRespawnNodes.Add(
                    node
                );
            }
        }

        /*
         * Authorade nodes finns, men alla är blockerade.
         *
         * Då får vi INTE falla tillbaka till originalpositionen.
         * Vi väntar istället tills en node blir ledig.
         */
        if (hasValidAuthoredNode)
        {
            if (availableRespawnNodes.Count == 0)
            {
                return false;
            }

            spawnPosition =
                SelectWeightedAvailableNode();

            return true;
        }

        /*
         * Inga giltiga authored nodes:
         * originalpositionen är spawnpunkten.
         */
        if (!IsSpawnPositionClear(
                originalPosition))
        {
            return false;
        }

        spawnPosition =
            originalPosition;

        return true;
    }


    private Vector3 SelectWeightedAvailableNode()
    {
        float totalWeight = 0f;

        WeightedRespawnNode lastNode =
            null;

        for (int i = 0;
             i < availableRespawnNodes.Count;
             i++)
        {
            WeightedRespawnNode node =
                availableRespawnNodes[i];

            if (node == null ||
                node.Node == null ||
                node.Weight <= 0f)
            {
                continue;
            }

            totalWeight +=
                node.Weight;

            lastNode =
                node;
        }

        if (lastNode == null ||
            totalWeight <= 0f)
        {
            return originalPosition;
        }

        float roll =
            Random.Range(
                0f,
                totalWeight
            );

        for (int i = 0;
             i < availableRespawnNodes.Count;
             i++)
        {
            WeightedRespawnNode node =
                availableRespawnNodes[i];

            if (node == null ||
                node.Node == null ||
                node.Weight <= 0f)
            {
                continue;
            }

            roll -=
                node.Weight;

            if (roll <= 0f)
            {
                return node.Node.position;
            }
        }

        /*
         * Floating-point fallback.
         */
        return lastNode.Node.position;
    }


    // =====================================================
    // SPAWN VALIDATION
    // =====================================================

    private void CacheSpawnFootprint()
    {
        ResolveSpawnFootprintCollider();

        if (spawnFootprintCollider == null)
        {
            Debug.LogError(
                $"LootContainer '{name}' saknar en fysisk " +
                "Collider2D för Spawn Validation.",
                this
            );

            spawnFootprintReady = false;
            return;
        }

        Bounds bounds =
            spawnFootprintCollider.bounds;

        spawnFootprintCenterOffset =
            (Vector2)bounds.center -
            (Vector2)transform.position;

        /*
         * Collider2D.bounds fungerar för både exempelvis
         * BoxCollider2D och EdgeCollider2D.
         *
         * EdgeCollider2D kan ha nästan noll tjocklek på en axel,
         * därför lägger vi på authorable padding.
         */
        spawnFootprintSize =
            new Vector2(
                Mathf.Max(
                    0.01f,
                    bounds.size.x +
                    spawnFootprintPadding.x * 2f
                ),
                Mathf.Max(
                    0.01f,
                    bounds.size.y +
                    spawnFootprintPadding.y * 2f
                )
            );

        spawnFootprintReady =
            spawnFootprintSize.x > 0f &&
            spawnFootprintSize.y > 0f;
    }

    private void ResolveSpawnFootprintCollider()
    {
        if (spawnFootprintCollider != null)
            return;

        Collider2D[] rootColliders =
            GetComponents<Collider2D>();

        for (int i = 0;
             i < rootColliders.Length;
             i++)
        {
            Collider2D candidate =
                rootColliders[i];

            if (candidate == null ||
                candidate.isTrigger)
            {
                continue;
            }

            spawnFootprintCollider =
                candidate;

            return;
        }
    }


    private bool IsSpawnPositionClear(
        Vector3 candidatePosition)
    {
        if (!spawnFootprintReady)
            return false;

        Vector2 footprintCenter =
            (Vector2)candidatePosition +
            spawnFootprintCenterOffset;


        ContactFilter2D filter =
            new ContactFilter2D();

        filter.SetLayerMask(
            spawnBlockingMask
        );

        /*
         * InteractionHitbox, CombatHitbox och andra
         * trigger-volymer ska inte blockera en fysisk spawn.
         */
        filter.useTriggers =
            false;


        int hitCount =
            Physics2D.OverlapBox(
            footprintCenter,
            spawnFootprintSize,
            0f,
            filter,
            spawnOverlapResults
            );

        return hitCount == 0;
    }


    // =====================================================
    // LOOT CONTENTS
    // =====================================================

    public int GetItemQuantity(
        ItemData item)
    {
        return contents.GetItemQuantity(
            item
        );
    }


    public bool TryTakeItems(
        ItemData item,
        int quantity)
    {
        if (!isSpawned)
            return false;

        bool taken =
            contents.TryTakeItems(
                item,
                quantity
            );

        if (taken)
        {
            HandleContentsChanged();
        }

        return taken;
    }


    public int TakeAllCoins()
    {
        if (!isSpawned)
            return 0;

        int taken =
            contents.TakeAllCoins();

        if (taken > 0)
        {
            HandleContentsChanged();
        }

        return taken;
    }


    private void HandleContentsChanged()
    {
        UpdateShimmer();

        if (!HasLoot)
        {
            ScheduleEmptyLifecycleEnd();
        }
    }


    // =====================================================
    // WORLD PRESENTATION
    // =====================================================

    private void CachePresentationState()
    {
        cachedRenderers =
            GetComponentsInChildren<
                Renderer>(
                    true
                );

        rendererInitialStates =
            new bool[
                cachedRenderers.Length
            ];

        for (int i = 0;
             i < cachedRenderers.Length;
             i++)
        {
            Renderer renderer =
                cachedRenderers[i];

            rendererInitialStates[i] =
                renderer != null &&
                renderer.enabled;
        }


        cachedColliders =
            GetComponentsInChildren<
                Collider2D>(
                    true
                );

        colliderInitialStates =
            new bool[
                cachedColliders.Length
            ];

        for (int i = 0;
             i < cachedColliders.Length;
             i++)
        {
            Collider2D collider =
                cachedColliders[i];

            colliderInitialStates[i] =
                collider != null &&
                collider.enabled;
        }
    }


    private void SetWorldPresentationVisible(
        bool visible)
    {
        if (cachedRenderers != null)
        {
            for (int i = 0;
                 i < cachedRenderers.Length;
                 i++)
            {
                Renderer renderer =
                    cachedRenderers[i];

                if (renderer == null)
                    continue;

                renderer.enabled =
                    visible &&
                    rendererInitialStates[i];
            }
        }

        if (cachedColliders != null)
        {
            for (int i = 0;
                 i < cachedColliders.Length;
                 i++)
            {
                Collider2D collider =
                    cachedColliders[i];

                if (collider == null)
                    continue;

                collider.enabled =
                    visible &&
                    colliderInitialStates[i];
            }
        }
    }


    // =====================================================
    // VISUALS
    // =====================================================

    public void RefreshLootVisuals()
    {
        HandleContentsChanged();
    }


    private void UpdateShimmer()
    {
        if (!isSpawned ||
            !HasLoot)
        {
            DestroyShimmer();
            return;
        }

        if (lootShimmer == null ||
            shimmerInstance != null)
        {
            return;
        }

        shimmerInstance =
            Instantiate(
                lootShimmer,
                transform
            );

        shimmerInstance.transform.localPosition =
            Vector3.zero;

        shimmerInstance.transform.localRotation =
            Quaternion.identity;
    }


    private void DestroyShimmer()
    {
        if (shimmerInstance == null)
            return;

        Destroy(
            shimmerInstance
        );

        shimmerInstance = null;
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        minLootRolls =
            Mathf.Max(
                0,
                minLootRolls
            );

        maxLootRolls =
            Mathf.Max(
                minLootRolls,
                maxLootRolls
            );

        lifetimeAfterFirstOpen =
            Mathf.Max(
                0f,
                lifetimeAfterFirstOpen
            );

        emptyDespawnDelay =
            Mathf.Max(
                0f,
                emptyDespawnDelay
            );

        respawnTime =
            Mathf.Max(
                0f,
                respawnTime
            );

        blockedRespawnRetryDelay =
            Mathf.Max(
                0.1f,
                blockedRespawnRetryDelay
            );

        /*
         * Auto-hitta rootens fysiska collider.
         *
         * Eftersom InteractionHitbox ligger på ett child-object
         * kommer den inte råka väljas här.
         */
        if (spawnFootprintCollider == null)
        {
            Collider2D[] rootColliders =
                GetComponents<Collider2D>();

            for (int i = 0;
                 i < rootColliders.Length;
                 i++)
            {
                Collider2D candidate =
                    rootColliders[i];

                if (candidate == null ||
                    candidate.isTrigger)
                {
                    continue;
                }

                spawnFootprintCollider =
                    candidate;

                break;
            }
        }

        spawnFootprintPadding =
            new Vector2(
                Mathf.Max(
                    0f,
                    spawnFootprintPadding.x
                ),
                Mathf.Max(
                    0f,
                    spawnFootprintPadding.y
                )
            );

        if (respawnNodes == null)
            return;

        for (int i = 0;
             i < respawnNodes.Count;
             i++)
        {
            respawnNodes[i]
                ?.Normalize();
        }
    }
#endif
}