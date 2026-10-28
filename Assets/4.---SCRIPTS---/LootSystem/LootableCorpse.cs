using System.Collections.Generic;
using UnityEngine;

public sealed class LootableCorpse :
    MonoBehaviour,
    ILootSource
{
    [Header("Identity")]

    [SerializeField]
    private string corpseName;

    [Header("Loot")]

    [SerializeField]
    private List<LootTable> lootTables =
        new();

    [SerializeField]
    [Min(0)]
    private int minLootRolls;

    [SerializeField]
    [Min(0)]
    private int maxLootRolls = 3;

    [Header("Interaction")]

    [SerializeField]
    private float lootRange = 2f;

    [Header("Visuals")]

    [SerializeField]
    private GameObject lootShimmer;

    [Header("Lifetime")]

    [SerializeField]
    private float emptyCorpseLifetime = 10f;

    [SerializeField]
    private float lootCorpseLifetime = 60f;

    private readonly LootContents contents =
        new();

    private Transform player;
    private GameObject shimmerInstance;

    private bool initialized;

    public string CorpseName =>
        corpseName;

    public string LootTitle =>
        !string.IsNullOrWhiteSpace(
            corpseName)
            ? corpseName
            : gameObject.name;

    public IReadOnlyList<ItemData> LootItems =>
        contents.Items;

    public int CoinAmount =>
        contents.CoinAmount;

    public bool HasLoot =>
        contents.HasLoot;

    private void Awake()
    {
        PlayerMovement playerMovement =
            FindFirstObjectByType<
                PlayerMovement>();

        if (playerMovement != null)
        {
            player =
                playerMovement.transform;
        }
        else
        {
            Debug.LogError(
                "LootableCorpse: PlayerMovement not found.",
                this
            );
        }
    }

    private void Start()
    {
        /*
         * Normalt initialiseras corpset av DeathReward direkt
         * efter Instantiate.
         *
         * Fallbacken gör att ett corpse som placeras direkt i
         * scenen fortfarande får ett giltigt tomt state.
         */
        if (!initialized)
        {
            Initialize(
                false
            );
        }

        float lifetime =
            HasLoot
                ? lootCorpseLifetime
                : emptyCorpseLifetime;

        Destroy(
            gameObject,
            lifetime
        );

        UpdateShimmer();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(
                1))
        {
            TryLoot();
        }
    }

    public void Initialize(
        bool generatePlayerLoot)
    {
        contents.Clear();

        if (generatePlayerLoot)
        {
            GenerateLoot();
        }

        initialized =
            true;

        UpdateShimmer();
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

    public void GetPossibleLootItems(
    List<ItemData> results)
    {
        if (results == null)
            return;

        results.Clear();

        if (lootTables == null)
            return;

        foreach (LootTable table
                 in lootTables)
        {
            if (table == null ||
                table.entries == null)
            {
                continue;
            }

            foreach (LootEntry entry
                     in table.entries)
            {
                if (entry == null ||
                    !entry.IsValid ||
                    entry.Type !=
                    LootEntryType.Item)
                {
                    continue;
                }

                ItemData item =
                    entry.Item;

                if (item == null)
                    continue;

                bool alreadyAdded =
                    false;

                foreach (ItemData existing
                         in results)
                {
                    if (Inventory.ItemsMatch(
                            existing,
                            item))
                    {
                        alreadyAdded =
                            true;

                        break;
                    }
                }

                if (!alreadyAdded)
                {
                    results.Add(
                        item
                    );
                }
            }
        }
    }

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
        bool taken =
            contents.TryTakeItems(
                item,
                quantity
            );

        if (taken)
        {
            RefreshLootVisuals();
        }

        return taken;
    }

    public int TakeAllCoins()
    {
        int taken =
            contents.TakeAllCoins();

        if (taken > 0)
        {
            RefreshLootVisuals();
        }

        return taken;
    }

    public void RefreshLootVisuals()
    {
        UpdateShimmer();
    }

    private void UpdateShimmer()
    {
        if (HasLoot)
        {
            if (shimmerInstance == null &&
                lootShimmer != null)
            {
                shimmerInstance =
                    Instantiate(
                        lootShimmer,
                        transform
                    );

                shimmerInstance
                    .transform
                    .localPosition =
                    Vector3.zero;
            }

            return;
        }

        if (shimmerInstance != null)
        {
            Destroy(
                shimmerInstance
            );

            shimmerInstance =
                null;
        }
    }

    private void TryLoot()
    {
        if (player == null ||
            LootUI.Instance == null)
        {
            return;
        }

        Camera mainCamera =
            Camera.main;

        if (mainCamera == null)
            return;

        Vector2 mouseWorld =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        Collider2D collider =
            GetComponent<Collider2D>();

        if (collider == null ||
            !collider.OverlapPoint(
                mouseWorld))
        {
            return;
        }

        if (Vector2.Distance(
                transform.position,
                player.position) >
            lootRange)
        {
            Debug.Log(
                "Too far away to loot.",
                this
            );

            return;
        }

        if (!HasLoot)
            return;

        LootUI.Instance.Show(
            this
        );
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        lootTables ??=
            new List<LootTable>();

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

        lootRange =
            Mathf.Max(
                0f,
                lootRange
            );

        emptyCorpseLifetime =
            Mathf.Max(
                0f,
                emptyCorpseLifetime
            );

        lootCorpseLifetime =
            Mathf.Max(
                0f,
                lootCorpseLifetime
            );
    }

#endif
}