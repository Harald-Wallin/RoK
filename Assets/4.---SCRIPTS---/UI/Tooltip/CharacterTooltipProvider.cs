
using System;
using System.Collections.Generic;

public sealed class CharacterTooltipProvider :
    ITooltipProvider
{
    private readonly CharacterStats target;

    public CharacterTooltipProvider(
        CharacterStats target)
    {
        this.target = target;
    }

    public TooltipData GetTooltipData(
        CharacterStats viewer = null)
    {
        TooltipData data =
            new TooltipData();

        if (target == null)
        {
            data.title =
                "Unknown Character";

            return data;
        }

        // =====================================================
        // NAME
        // =====================================================

        data.title =
            target.DisplayName;

        // =====================================================
        // CREATURE TYPES + LEVEL
        // =====================================================

        string creatureTypes =
            GetCreatureTypeDisplayNames(
                target.CreatureTypes
            );

        if (!string.IsNullOrEmpty(creatureTypes))
        {
            data.subtitle =
                $"{creatureTypes}\n" +
                $"Level {target.level}";
        }
        else
        {
            data.subtitle =
                $"Level {target.level}";
        }

        // =====================================================
        // NPC ROLE
        // =====================================================

        string roleName =
            target.RoleName;

        if (!string.IsNullOrWhiteSpace(roleName))
        {
            data.description =
                roleName;
        }

        // =====================================================
        // FACTION
        // =====================================================

        if (target.HasVisibleFaction)
        {
            data.stats.Add(
                $"Faction: {target.FactionName}"
            );
        }

        // =====================================================
        // HEALTH
        // =====================================================

        data.stats.Add(
            $"Health: {target.currentHP} / " +
            $"{target.GetMaxHP()}"
        );

        // =====================================================
        // FAVOURS
        // =====================================================

        FavourPresentationUtility
            .AppendForEntity(
                data,
                target.gameObject
            );

        data.showFooter =
            false;

        return data;
    }

    // =========================================================
    // CREATURE TYPE PRESENTATION
    // =========================================================

    private static string GetCreatureTypeDisplayNames(
        CreatureType types)
    {
        if (types == CreatureType.None)
            return string.Empty;

        List<string> names =
            new List<string>();

        foreach (CreatureType type in
                 Enum.GetValues(
                     typeof(CreatureType)))
        {
            if (type == CreatureType.None)
                continue;

            // Only show enabled flags.
            if ((types & type) != type)
                continue;

            names.Add(
                type.ToString()
            );
        }

        // Alphabetical presentation.
        names.Sort(
            StringComparer.OrdinalIgnoreCase
        );

        return string.Join(
            " · ",
            names
        );
    }
}
