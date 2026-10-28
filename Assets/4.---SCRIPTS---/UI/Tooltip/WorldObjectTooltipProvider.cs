using UnityEngine;

public sealed class WorldObjectTooltipProvider :
    ITooltipProvider
{
    private readonly InteractionTarget target;

    public WorldObjectTooltipProvider(
        InteractionTarget target)
    {
        this.target =
            target;
    }

    public TooltipData GetTooltipData(
        CharacterStats viewer = null)
    {
        TooltipData data =
            new TooltipData();

        if (target == null)
        {
            data.title =
                "Unknown";

            return data;
        }

        EntityIdentity identity =
            target.Identity;

        if (identity != null)
        {
            data.title =
                identity.DisplayName;

            FavourPresentationUtility
                .AppendForEntity(
                    data,
                    identity.Id
                );
        }
        else
        {
            GameObject owner =
                target.InteractionOwner;

            data.title =
                owner != null
                    ? owner.name
                    : "Unknown";
        }

        data.showFooter =
            false;

        return data;
    }
}
