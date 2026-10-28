using UnityEngine;

public static class StatFormatting
{
    public static string GetDisplayName(
        StatType stat)
    {
        StatDefinition definition =
            StatDatabase.Instance?
                .GetDefinition(stat);

        if (definition != null &&
            !string.IsNullOrWhiteSpace(
                definition.displayName))
        {
            return definition.displayName;
        }

        return stat.ToString();
    }

    public static StatDisplayFormat GetDisplayFormat(
        StatType stat)
    {
        StatDefinition definition =
            StatDatabase.Instance?
                .GetDefinition(stat);

        if (definition == null)
        {
            return StatDisplayFormat.Number;
        }

        return definition.displayFormat;
    }

    public static string FormatModifier(
        StatType stat,
        ModifierType modifierType,
        float value)
    {
        string displayName =
            GetDisplayName(stat);

        string formattedValue =
            FormatModifierValue(
                stat,
                modifierType,
                value
            );

        return $"{formattedValue} {displayName}";
    }

    public static string FormatValue(
        StatType stat,
        float value)
    {
        string displayName =
            GetDisplayName(stat);

        string formattedValue =
            FormatRawValue(
                stat,
                value
            );

        return $"{formattedValue} {displayName}";
    }

    public static string FormatRawValue(
        StatType stat,
        float value)
    {
        StatDisplayFormat format =
            GetDisplayFormat(stat);

        return FormatByDisplayType(
            value,
            format,
            false
        );
    }

    public static string FormatModifierValue(
        StatType stat,
        ModifierType modifierType,
        float value)
    {
        StatDisplayFormat displayFormat =
            GetDisplayFormat(stat);

        float displayedValue = value;

        /*
         * ModifierType.Percent betyder en relativ modifier:
         *
         * 0.1 = öka nuvarande värde med 10%.
         *
         * Den ska därför alltid presenteras som procent,
         * oavsett statens normala display format.
         */
        if (modifierType == ModifierType.Percent)
        {
            return FormatSignedPercentage(
                displayedValue
            );
        }

        /*
         * Flat modifier använder statens visningsformat.
         *
         * BlockChance Flat 0.1:
         * Percentage-format -> +10%
         *
         * Strength Flat 5:
         * Number-format -> +5
         */
        return FormatByDisplayType(
            displayedValue,
            displayFormat,
            true
        );
    }

    private static string FormatByDisplayType(
        float value,
        StatDisplayFormat format,
        bool includeSign)
    {
        switch (format)
        {
            case StatDisplayFormat.Percentage:
                return includeSign
                    ? FormatSignedPercentage(value)
                    : FormatPercentage(value);

            case StatDisplayFormat.Decimal:
                return includeSign
                    ? FormatSignedDecimal(value)
                    : value.ToString("0.##");

            case StatDisplayFormat.Seconds:
                return includeSign
                    ? FormatSignedSeconds(value)
                    : $"{value:0.##}s";

            case StatDisplayFormat.Number:
            default:
                return includeSign
                    ? FormatSignedNumber(value)
                    : value.ToString("0.##");
        }
    }

    private static string FormatPercentage(
        float value)
    {
        return $"{value * 100f:0.##}%";
    }

    private static string FormatSignedPercentage(
        float value)
    {
        string sign =
            GetSign(value);

        return
            $"{sign}{value * 100f:0.##}%";
    }

    private static string FormatSignedNumber(
        float value)
    {
        string sign =
            GetSign(value);

        return $"{sign}{value:0.##}";
    }

    private static string FormatSignedDecimal(
        float value)
    {
        string sign =
            GetSign(value);

        return $"{sign}{value:0.##}";
    }

    private static string FormatSignedSeconds(
        float value)
    {
        string sign =
            GetSign(value);

        return $"{sign}{value:0.##}s";
    }

    private static string GetSign(
        float value)
    {
        if (value > 0f)
            return "+";

        return "";
    }
}