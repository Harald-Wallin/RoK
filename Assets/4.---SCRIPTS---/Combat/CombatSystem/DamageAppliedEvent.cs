public readonly struct DamageAppliedEvent
{
    public CharacterStats Victim
    {
        get;
    }

    public DamageSourceContext Source
    {
        get;
    }

    public int AppliedDamage
    {
        get;
    }

    public bool IsCriticalHit { get; }



    public DamageAppliedEvent(
        CharacterStats victim,
        DamageSourceContext source,
        int appliedDamage, bool isCriticalHit = false)
    {
        Victim =
            victim;

        Source =
            source;

        AppliedDamage =
            UnityEngine.Mathf.Max(
                0,
                appliedDamage
            );

        IsCriticalHit = isCriticalHit;

    }

}
