public readonly struct StatScalingContribution
{
    public StatScalingContribution(
        StatType sourceStat,
        StatType targetStat,
        float contribution,
        float totalValue)
    {
        SourceStat =
            sourceStat;

        TargetStat =
            targetStat;

        Contribution =
            contribution;

        TotalValue =
            totalValue;
    }

    public StatType SourceStat
    {
        get;
    }

    public StatType TargetStat
    {
        get;
    }

    public float Contribution
    {
        get;
    }

    public float TotalValue
    {
        get;
    }
}
