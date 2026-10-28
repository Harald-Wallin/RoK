using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central databas för generell interaction-presentation.
///
/// Gameplay-komponenter ska inte behöva authora generiska
/// service-repliker per NPC. Vendor, Tribute och framtida
/// interaction-typer kan istället hämta presentation härifrån.
/// </summary>
[CreateAssetMenu(
    fileName = "InteractionPresentationDatabase",
    menuName = "RöK/Interaction/Presentation Database")]
public sealed class InteractionPresentationDatabase :
    ScriptableObject
{
    [Header("Vendor")]

    [SerializeField]
    private List<string> vendorLines =
        new()
        {
            "Let me show you my wares!",
            "You have coin?",
            "Come, browse my wares!",
            "See anything you like?"
        };

    [Header("Tribute")]

    [SerializeField]
    private List<string> tributeLines =
        new()
        {
            "Aid the realm and give tribute!",
            "Make your contribution.",
            "Offer what you can."
        };

    public string GetRandomLine(
        InteractionCategory category)
    {
        IReadOnlyList<string> lines =
            GetLines(
                category
            );

        if (lines == null ||
            lines.Count == 0)
        {
            return GetFallbackText(
                category
            );
        }

        /*
         * Försök hitta en giltig rad utan att låta tomma
         * entries förstöra presentationen.
         */
        int startIndex =
            Random.Range(
                0,
                lines.Count
            );

        for (int offset = 0;
             offset < lines.Count;
             offset++)
        {
            int index =
                (startIndex + offset) %
                lines.Count;

            string line =
                lines[index];

            if (!string.IsNullOrWhiteSpace(
                    line))
            {
                return line.Trim();
            }
        }

        return GetFallbackText(
            category
        );
    }

    private IReadOnlyList<string> GetLines(
        InteractionCategory category)
    {
        switch (category)
        {
            case InteractionCategory.Vendor:
                return vendorLines;

            case InteractionCategory.Tribute:
                return tributeLines;

            default:
                return null;
        }
    }

    private static string GetFallbackText(
        InteractionCategory category)
    {
        switch (category)
        {
            case InteractionCategory.Vendor:
                return "Trade";

            case InteractionCategory.Tribute:
                return "Donate";

            default:
                return "Interact";
        }
    }
}
