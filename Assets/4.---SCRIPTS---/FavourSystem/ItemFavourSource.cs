using System.Collections.Generic;

public sealed class ItemFavourSource
{
    private readonly ItemData item;

    public ItemData Item =>
        item;

    public string SourceName =>
        item != null
            ? item.itemName
            : string.Empty;


    public ItemFavourSource(
        ItemData item)
    {
        this.item =
            item;
    }


    public List<FavourRuntime>
        GetVisibleFavours()
    {
        List<FavourRuntime> result =
            new();

        if (item == null ||
            !item.HasFavourInteraction)
        {
            return result;
        }

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return result;

        /*
         * Itemet är en explicit interaction source.
         *
         * Därför får interaktionen registrera dess länkade
         * Favours på samma sätt som en FavourGiver kan göra
         * när spelaren faktiskt interagerar med den.
         */
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

            if (!ShouldShow(
                    runtime))
            {
                continue;
            }

            result.Add(
                runtime
            );
        }

        return result;
    }


    public bool TryGetVisibleRuntime(
        FavourData favour,
        out FavourRuntime runtime)
    {
        runtime = null;

        if (item == null ||
            favour == null ||
            !ContainsFavour(
                favour))
        {
            return false;
        }

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return false;

        runtime =
            manager.RegisterFavour(
                favour
            );

        if (runtime == null)
            return false;

        runtime.RefreshAvailability();

        if (!ShouldShow(
                runtime))
        {
            runtime = null;

            return false;
        }

        return true;
    }


    public bool TryAccept(FavourData favour)
    {
        if (!ContainsFavour(favour))
            return false;

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        return manager != null &&
               manager.TryAccept(
                   favour,
                   item
               );
    }


    public string GetDialogueFor(
        FavourRuntime runtime)
    {
        if (runtime?.Data == null ||
            !ContainsFavour(
                runtime.Data) ||
            runtime.Data.DialogueSet == null)
        {
            return string.Empty;
        }

        return runtime.Data
            .DialogueSet
            .GetDialogue(
                runtime.State
            );
    }


    private bool ContainsFavour(
        FavourData favour)
    {
        if (item == null ||
            favour == null)
        {
            return false;
        }

        foreach (FavourData linkedFavour
                 in item.LinkedFavours)
        {
            if (linkedFavour ==
                favour)
            {
                return true;
            }
        }

        return false;
    }


    private static bool ShouldShow(
        FavourRuntime runtime)
    {
        if (runtime == null)
            return false;

        /*
         * Completed-delar av en item-chain ska inte ligga kvar
         * och blockera nästa del.
         *
         * Repeatable/cooldown hanteras fortfarande av runtime.
         */
        if (runtime.State ==
                FavourState.Completed &&
            runtime.HasBeenCompleted)
        {
            return false;
        }

        return
            runtime.State !=
                FavourState.Unavailable ||
            !runtime.HasBeenCompleted;
    }
}