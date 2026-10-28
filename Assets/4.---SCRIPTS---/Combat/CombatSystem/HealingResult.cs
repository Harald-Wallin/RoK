public readonly struct HealingResult
{
    public HealingResult(
        int requestedAmount,
        int appliedAmount,
        bool isCritical)
    {
        RequestedAmount =
            requestedAmount;

        AppliedAmount =
            appliedAmount;

        IsCritical =
            isCritical;
    }

    public int RequestedAmount
    {
        get;
    }

    public int AppliedAmount
    {
        get;
    }

    public bool IsCritical
    {
        get;
    }

    public bool RestoredHealth =>
        AppliedAmount > 0;
}
