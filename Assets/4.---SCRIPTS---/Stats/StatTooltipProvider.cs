using System.Collections.Generic;
using UnityEngine;

public sealed class StatTooltipProvider :
    ITooltipProvider
{
    private readonly CharacterStats stats;
    private readonly StatDefinition sourceDefinition;

    public StatTooltipProvider(
        CharacterStats stats,
        StatDefinition sourceDefinition)
    {
        this.stats =
            stats;

        this.sourceDefinition =
            sourceDefinition;
    }

    public TooltipData GetTooltipData(
        CharacterStats viewer = null)
    {
        TooltipData data =
            new TooltipData();

        if (stats == null ||
            sourceDefinition == null)
        {
            data.title =
                "Unknown Stat";

            return data;
        }

        data.title =
            sourceDefinition
                .DisplayName;

        data.subtitle =
            "Stat contributions";

        List<StatScalingContribution>
            contributions =
                stats.GetScalingContributions(
                    sourceDefinition.stat
                );

        if (contributions.Count == 0)
        {
            data.description =
                "This stat currently has no configured " +
                "derived-stat contributions.";

            data.showFooter =
                false;

            return data;
        }

        StatDatabase database =
            StatDatabase.Instance;

        foreach (StatScalingContribution
                 contribution
                 in contributions)
        {
            StatDefinition targetDefinition =
                database?.GetDefinition(
                    contribution.TargetStat);

            string targetName =
                targetDefinition != null
                    ? targetDefinition.DisplayName
                    : contribution
                        .TargetStat
                        .ToString();

            string contributionText =
                StatValueFormatter.FormatValue(
                    targetDefinition,
                    contribution.Contribution,
                    includeSign: true
                );

            string totalText =
                StatValueFormatter.FormatValue(
                    targetDefinition,
                    contribution.TotalValue
                );

            string contributionColor =
                StatValueFormatter
                    .GetContributionColor(
                        contribution.Contribution
                    );

            data.stats.Add(
                $"{targetName}: " +
                $"<color={contributionColor}>" +
                $"{contributionText}" +
                $"</color> " +
                $"<color=#FFFFFF>" +
                $"({totalText})" +
                $"</color>"
            );
        }

        data.showFooter =
            false;

        return data;
    }
}
