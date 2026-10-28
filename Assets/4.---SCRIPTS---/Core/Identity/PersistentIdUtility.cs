using System.Globalization;
using System.Text;

public static class PersistentIdUtility
{
    public static string FromDisplayName(
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(
                displayName))
        {
            return string.Empty;
        }

        string normalized =
            displayName
                .Trim()
                .ToLowerInvariant()
                .Normalize(
                    NormalizationForm.FormD
                );

        StringBuilder builder =
            new();

        bool previousWasSeparator =
            false;

        foreach (char character
                 in normalized)
        {
            UnicodeCategory category =
                CharUnicodeInfo
                    .GetUnicodeCategory(
                        character
                    );

            if (category ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(
                    character))
            {
                builder.Append(
                    character
                );

                previousWasSeparator =
                    false;

                continue;
            }

            if (!previousWasSeparator &&
                builder.Length > 0)
            {
                builder.Append('_');

                previousWasSeparator =
                    true;
            }
        }

        return builder
            .ToString()
            .Trim('_')
            .Normalize(
                NormalizationForm.FormC
            );
    }
}