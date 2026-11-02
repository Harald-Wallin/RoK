
/// <summary>
/// Aggregation query for additional Ward capacity.
///
/// WardSystem remains authoritative for
/// CurrentWard and MaxWard.
/// </summary>
public sealed class WardCapacityQuery
{
    public CharacterStats Owner { get; }

    public int AdditionalWards
    {
        get;
        private set;
    }

    public WardCapacityQuery(
        CharacterStats owner)
    {
        Owner = owner;
    }

    public void Add(int amount)
    {
        if (amount > 0)
        {
            AdditionalWards += amount;
        }
    }
}

