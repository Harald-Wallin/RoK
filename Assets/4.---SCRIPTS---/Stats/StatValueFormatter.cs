using System;
using UnityEngine;

public static class StatValueFormatter
{
    public static string FormatValue(
        StatDefinition definition,
        float value,
        bool includeSign = false)
    {
        bool percentage =
            IsPercentage(
                definition);

        string sign =
            includeSign &&
            value > 0f
                ? "+"
                : string.Empty;

        if (percentage)
        {
            float percentageValue =
                value *
                100f;

            return
                $"{sign}{percentageValue:0.##}%";
        }

        return
            $"{sign}{value:0.##}";
    }

    public static bool IsPercentage(
        StatDefinition definition)
    {
        if (definition == null)
            return false;

        string formatName =
            definition
                .displayFormat
                .ToString();

        return formatName.IndexOf(
                   "percent",
                   StringComparison
                       .OrdinalIgnoreCase) >= 0;
    }

    public static string GetColoredValue(
        string formattedValue,
        float temporaryDelta)
    {
        const float tolerance =
            0.0001f;

        if (temporaryDelta >
            tolerance)
        {
            return
                $"<color=#65FF7A>" +
                $"{formattedValue}" +
                $"</color>";
        }

        if (temporaryDelta <
            -tolerance)
        {
            return
                $"<color=#FF6666>" +
                $"{formattedValue}" +
                $"</color>";
        }

        return
            $"<color=#FFFFFF>" +
            $"{formattedValue}" +
            $"</color>";
    }

    public static string
        GetContributionColor(
            float contribution)
    {
        const float tolerance =
            0.0001f;

        if (contribution >
            tolerance)
        {
            return "#65FF7A";
        }

        if (contribution <
            -tolerance)
        {
            return "#FF6666";
        }

        return "#FFFFFF";
    }
}
