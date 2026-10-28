using System.Collections.Generic;
using UnityEngine;

public sealed class LootGenerationResult
{
    private readonly List<GeneratedLootItem>
        generatedItems =
            new();

    private readonly List<ItemData>
        items =
            new();

    public IReadOnlyList<GeneratedLootItem>
        GeneratedItems =>
            generatedItems;

    /*
     * Temporär kompatibilitetsvy.
     *
     * Befintliga system som fortfarande bara behöver
     * ItemData kan fortsätta använda Items.
     */
    public List<ItemData> Items =>
        items;

    public int Coins
    {
        get;
        private set;
    }

    public void AddItem(
        ItemData item,
        int amount,
        PlayerLootPolicy playerLootPolicy,
        string lootTableId,
        int entryIndex)
    {
        if (item == null ||
            amount <= 0)
        {
            return;
        }

        for (int i = 0;
             i < amount;
             i++)
        {
            GeneratedLootItem generatedItem =
                new GeneratedLootItem(
                    item,
                    playerLootPolicy,
                    lootTableId,
                    entryIndex
                );

            generatedItems.Add(
                generatedItem
            );

            /*
             * Kompatibilitetsvyn hålls parallellt under
             * migrationen.
             */
            items.Add(
                item
            );
        }
    }

    public void AddCoins(
        int amount)
    {
        if (amount <= 0)
            return;

        Coins +=
            amount;
    }
}

public static class LootGenerator
{
    private sealed class GenerationContext
    {
        private readonly Dictionary<LootEntry, int>
            generatedQuantities =
                new();

        public int GetGeneratedQuantity(
            LootEntry entry)
        {
            if (entry == null)
                return 0;

            return generatedQuantities.TryGetValue(
                entry,
                out int quantity)
                    ? quantity
                    : 0;
        }

        public void AddGeneratedQuantity(
            LootEntry entry,
            int quantity)
        {
            if (entry == null ||
                quantity <= 0)
            {
                return;
            }

            generatedQuantities[entry] =
                GetGeneratedQuantity(
                    entry
                ) +
                quantity;
        }
    }

    public static LootGenerationResult
    GenerateLootResult(
        List<LootTable> tables,
        int minRolls,
        int maxRolls)
    {
        LootGenerationResult result =
            new LootGenerationResult();

        if (tables == null ||
            tables.Count == 0)
        {
            return result;
        }

        GenerationContext context =
            new GenerationContext();

        int safeMinimum =
            Mathf.Max(
                0,
                minRolls
            );

        int safeMaximum =
            Mathf.Max(
                safeMinimum,
                maxRolls
            );

        int rolls =
            Random.Range(
                safeMinimum,
                safeMaximum + 1
            );

        for (int i = 0;
             i < rolls;
             i++)
        {
            foreach (LootTable table
                     in tables)
            {
                RollTable(
                    table,
                    result,
                    context
                );
            }
        }

        return result;
    }

    /*
     * Behålls för kompatibilitet med kod som endast förväntar
     * sig vanliga items.
     */
    public static List<ItemData> GenerateLoot(
        List<LootTable> tables,
        int minRolls,
        int maxRolls)
    {
        return GenerateLootResult(
            tables,
            minRolls,
            maxRolls
        ).Items;
    }

    public static LootGenerationResult
    GenerateSingleDropResult(
        List<LootTable> tables)
    {
        LootGenerationResult result =
            new LootGenerationResult();

        if (tables == null ||
            tables.Count == 0)
        {
            return result;
        }

        GenerationContext context =
            new GenerationContext();

        LootTable chosenTable =
            tables[
                Random.Range(
                    0,
                    tables.Count
                )
            ];

        RollTable(
            chosenTable,
            result,
            context
        );

        return result;
    }

    /*
     * Behålls för kompatibilitet.
     */
    public static List<ItemData>
        GenerateSingleDrop(
            List<LootTable> tables)
    {
        return GenerateSingleDropResult(
            tables
        ).Items;
    }

    private static void RollTable(
    LootTable table,
    LootGenerationResult result,
    GenerationContext context)
    {
        if (table == null ||
            result == null ||
            context == null)
        {
            return;
        }

        if (table.mode ==
            LootTableMode.SingleDrop)
        {
            RollSingleDrop(
                table,
                result,
                context
            );
        }
        else
        {
            RollMultiDrop(
                table,
                result,
                context
            );
        }
    }

    private static void RollSingleDrop(
    LootTable table,
    LootGenerationResult result,
    GenerationContext context)
    {
        if (table?.entries == null)
            return;

        for (int entryIndex = 0;
             entryIndex < table.entries.Count;
             entryIndex++)
        {
            LootEntry entry =
                table.entries[
                    entryIndex
                ];

            if (!CanRollEntry(
                    entry))
            {
                continue;
            }

            if (context.GetGeneratedQuantity(
                    entry) >=
                entry.MaxQuantity)
            {
                continue;
            }

            if (Random.value >
                entry.DropChance)
            {
                continue;
            }

            AddRolledEntry(
                table,
                entry,
                entryIndex,
                result,
                context
            );

            /*
             * SingleDrop tillåter högst en lyckad entry
             * per table och roll.
             */
            return;
        }
    }

    private static void RollMultiDrop(
    LootTable table,
    LootGenerationResult result,
    GenerationContext context)
    {
        if (table?.entries == null)
            return;

        for (int entryIndex = 0;
             entryIndex < table.entries.Count;
             entryIndex++)
        {
            LootEntry entry =
                table.entries[
                    entryIndex
                ];

            if (!CanRollEntry(
                    entry))
            {
                continue;
            }

            if (context.GetGeneratedQuantity(
                    entry) >=
                entry.MaxQuantity)
            {
                continue;
            }

            if (Random.value >
                entry.DropChance)
            {
                continue;
            }

            AddRolledEntry(
                table,
                entry,
                entryIndex,
                result,
                context
            );
        }
    }

    private static bool CanRollEntry(
        LootEntry entry)
    {
        if (entry == null ||
            !entry.IsValid)
        {
            return false;
        }

        if (entry.Type !=
            LootEntryType.Item)
        {
            return true;
        }

        ItemData item =
            entry.Item;

        return item != null &&
               item.CanDropForPlayer(
                   PlayerFavourManager.Instance
               );
    }

    private static void AddRolledEntry(
    LootTable table,
    LootEntry entry,
    int entryIndex,
    LootGenerationResult result,
    GenerationContext context)
    {
        if (table == null ||
            entry == null ||
            result == null ||
            context == null)
        {
            return;
        }

        int alreadyGenerated =
            context.GetGeneratedQuantity(
                entry
            );

        int remainingMaximum =
            entry.MaxQuantity -
            alreadyGenerated;

        if (remainingMaximum <= 0)
            return;

        int minimum =
            Mathf.Min(
                entry.MinQuantity,
                remainingMaximum
            );

        int maximum =
            Mathf.Min(
                entry.MaxQuantity,
                remainingMaximum
            );

        int amount =
            Random.Range(
                minimum,
                maximum + 1
            );

        if (amount <= 0)
            return;

        switch (entry.Type)
        {
            case LootEntryType.Item:

                string lootTableId =
                    PersistentIdUtility
                        .FromDisplayName(
                            table.name
                        );

                result.AddItem(
                    entry.Item,
                    amount,
                    entry.PlayerLootPolicy,
                    lootTableId,
                    entryIndex
                );

                context.AddGeneratedQuantity(
                    entry,
                    amount
                );

                break;

            case LootEntryType.Coins:

                result.AddCoins(
                    amount
                );

                context.AddGeneratedQuantity(
                    entry,
                    amount
                );

                break;
        }
    }
}