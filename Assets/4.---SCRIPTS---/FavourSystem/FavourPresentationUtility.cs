using System.Collections.Generic;
using UnityEngine;

public static class FavourPresentationUtility
{
    private const string FavourColor =
        "#ffc70f";

    private const string RequirementMetColor =
        "#ffffff";

    private const string RequirementUnmetColor =
        "#ff5555";

    private static readonly List<ItemData>
        possibleLootItems =
            new();

    // =========================================================
    // ENTITY
    // =========================================================

    public static void AppendForEntity(
        TooltipData tooltip,
        GameObject entityObject)
    {
        if (tooltip == null ||
            entityObject == null)
        {
            return;
        }

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return;

        EntityIdentity identity =
            EntityTargetUtility.GetIdentity(
                entityObject
            );

        string entityId =
            identity != null
                ? identity.Id
                : string.Empty;

        Dictionary<
            FavourRuntime,
            List<FavourObjectiveRuntime>>
            matches =
                new();

        /*
         * Först: objectives som direkt riktar sig mot entityn.
         *
         * Exempel:
         * Kill Starving Wolves
         * Interact with Ancient Remains
         */
        if (!string.IsNullOrWhiteSpace(
                entityId))
        {
            CollectEntityMatches(
                manager,
                entityId,
                matches
            );
        }

        /*
         * Därefter: Collect-objectives för items som den här
         * karaktären definitionsmässigt kan droppa.
         */
        DeathReward deathReward =
            entityObject.GetComponent<
                DeathReward>();

        if (deathReward == null)
        {
            deathReward =
                entityObject
                    .GetComponentInParent<
                        DeathReward>();
        }

        if (deathReward != null)
        {
            possibleLootItems.Clear();

            deathReward.GetPossibleLootItems(
                possibleLootItems
            );

            foreach (ItemData lootItem
                     in possibleLootItems)
            {
                CollectItemMatches(
                    manager,
                    lootItem,
                    matches
                );
            }

            possibleLootItems.Clear();
        }

        AppendMatches(
            tooltip,
            matches
        );
    }

    public static void AppendForEntity(
        TooltipData tooltip,
        string entityId)
    {
        if (tooltip == null ||
            string.IsNullOrWhiteSpace(
                entityId))
        {
            return;
        }

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return;

        Dictionary<
            FavourRuntime,
            List<FavourObjectiveRuntime>>
            matches =
                new();

        CollectEntityMatches(
            manager,
            entityId,
            matches
        );

        AppendMatches(
            tooltip,
            matches
        );
    }

    // =========================================================
    // ITEM
    // =========================================================
    private static void AppendItemFavourSource(
    TooltipData tooltip,
    ItemData item,
    PlayerFavourManager manager)
    {
        if (tooltip == null ||
            item == null ||
            manager == null ||
            !item.HasFavourInteraction)
        {
            return;
        }

        FavourRuntime runtime =
            ResolveRelevantItemFavour(
                item,
                manager
            );

        if (runtime == null)
            return;

        if (runtime.State ==
                FavourState.Active ||
            runtime.State ==
                FavourState.ReadyToTurnIn)
        {
            return;
        }

        tooltip.itemFavourSource =
    "<size=80%><color=#ffffff>" +
    "Starts a Favour</color></size>";

        AppendItemFavourRequirements(
            tooltip,
            runtime
        );
    }

    private static void AppendItemFavourRequirements(
    TooltipData tooltip,
    FavourRuntime runtime)
    {
        if (tooltip == null ||
            runtime == null)
        {
            return;
        }

        if (runtime.MinimumLevel <= 0)
            return;

        string color =
            runtime.IsMinimumLevelMet
                ? RequirementMetColor
                : RequirementUnmetColor;

        tooltip.itemFavourRequirement =
    $"<size=80%><color={color}>" +
    $"Requires Level " +
    $"{runtime.MinimumLevel}</color></size>";
    }

    public static void AppendForItem(
    TooltipData tooltip,
    ItemData item)
    {
        if (tooltip == null ||
            item == null)
        {
            return;
        }

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return;

        AppendItemFavourSource(
            tooltip,
            item,
            manager
        );


        Dictionary<
            FavourRuntime,
            List<FavourObjectiveRuntime>>
            matches =
                new();

        CollectItemMatches(
            manager,
            item,
            matches
        );

        AppendMatches(
            tooltip,
            matches
        );
    }

    private static FavourRuntime
    ResolveRelevantItemFavour(
        ItemData item,
        PlayerFavourManager manager)
    {
        if (item == null ||
            manager == null ||
            !item.HasFavourInteraction)
        {
            return null;
        }

        FavourRuntime firstLocked =
            null;

        foreach (FavourData favour
                 in item.LinkedFavours)
        {
            if (favour == null)
                continue;

            FavourRuntime runtime =
                manager.RegisterFavour(
                    favour
                );

            if (runtime == null)
                continue;

            runtime.RefreshAvailability();

            if (runtime.State ==
                    FavourState.Active ||
                runtime.State ==
                    FavourState.ReadyToTurnIn)
            {
                return runtime;
            }

            if (runtime.State ==
                FavourState.Available)
            {
                return runtime;
            }

            if (runtime.State ==
                    FavourState.Completed &&
                runtime.HasBeenCompleted)
            {
                continue;
            }

            if (firstLocked == null &&
                runtime.State ==
                    FavourState.Unavailable)
            {
                firstLocked =
                    runtime;
            }
        }

        return firstLocked;
    }

    // =========================================================
    // MATCHING
    // =========================================================

    private static void CollectEntityMatches(
        PlayerFavourManager manager,
        string entityId,
        Dictionary<
            FavourRuntime,
            List<FavourObjectiveRuntime>>
            matches)
    {
        if (manager == null ||
            matches == null ||
            string.IsNullOrWhiteSpace(
                entityId))
        {
            return;
        }

        foreach (FavourRuntime favour
                 in manager.Runtimes)
        {
            if (!CanPresentFavour(
                    favour))
            {
                continue;
            }

            foreach (FavourObjectiveRuntime objective
                     in favour.Objectives)
            {
                if (objective == null)
                    continue;

                if (!objective.IsRelevantToEntity(
                        entityId))
                {
                    continue;
                }

                AddMatch(
                    matches,
                    favour,
                    objective
                );
            }
        }
    }

    private static void CollectItemMatches(
        PlayerFavourManager manager,
        ItemData item,
        Dictionary<
            FavourRuntime,
            List<FavourObjectiveRuntime>>
            matches)
    {
        if (manager == null ||
            item == null ||
            matches == null)
        {
            return;
        }

        foreach (FavourRuntime favour
                 in manager.Runtimes)
        {
            if (!CanPresentFavour(
                    favour))
            {
                continue;
            }

            foreach (FavourObjectiveRuntime objective
                     in favour.Objectives)
            {
                if (objective == null)
                    continue;

                if (!objective.IsRelevantToItem(
                        item))
                {
                    continue;
                }

                AddMatch(
                    matches,
                    favour,
                    objective
                );
            }
        }
    }

    private static void AddMatch(
        Dictionary<
            FavourRuntime,
            List<FavourObjectiveRuntime>>
            matches,
        FavourRuntime favour,
        FavourObjectiveRuntime objective)
    {
        if (matches == null ||
            favour == null ||
            objective == null)
        {
            return;
        }

        if (!matches.TryGetValue(
                favour,
                out List<FavourObjectiveRuntime>
                    objectives))
        {
            objectives =
                new List<
                    FavourObjectiveRuntime>();

            matches.Add(
                favour,
                objectives
            );
        }

        if (!objectives.Contains(
                objective))
        {
            objectives.Add(
                objective
            );
        }
    }

    // =========================================================
    // PRESENTATION STATE
    // =========================================================

    private static bool CanPresentFavour(
        FavourRuntime favour)
    {
        if (favour == null)
            return false;

        return
            favour.State ==
                FavourState.Active ||
            favour.State ==
                FavourState.ReadyToTurnIn;
    }

    // =========================================================
    // BUILD TOOLTIP
    // =========================================================

    private static void AppendMatches(
        TooltipData tooltip,
        Dictionary<
            FavourRuntime,
            List<FavourObjectiveRuntime>>
            matches)
    {
        if (tooltip == null ||
            matches == null)
        {
            return;
        }

        foreach (KeyValuePair<
                     FavourRuntime,
                     List<FavourObjectiveRuntime>>
                 pair in matches)
        {
            FavourRuntime favour =
                pair.Key;

            List<FavourObjectiveRuntime>
                objectives =
                    pair.Value;

            if (favour == null ||
                objectives == null ||
                objectives.Count == 0)
            {
                continue;
            }

            string text =
                $"<color={FavourColor}>" +
                $"{favour.DisplayName}</color>";

            foreach (FavourObjectiveRuntime objective
                     in objectives)
            {
                if (objective == null)
                    continue;

                text +=
                    "\n" +
                    objective.DisplayName +
                    " " +
                    objective.ProgressText;
            }

            tooltip.favourContext.Add(
                text
            );
        }
    }
}