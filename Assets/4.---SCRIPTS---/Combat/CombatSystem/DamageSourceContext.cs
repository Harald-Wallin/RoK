public readonly struct DamageSourceContext
{
    /// <summary>
    /// Karaktären som direkt skapade skadan.
    ///
    /// Exempel:
    /// - spelaren
    /// - en guard
    /// - en summon
    /// </summary>
    public CharacterStats DirectSource { get; }

    /// <summary>
    /// Karaktären som ska få progression och belöningscredit.
    ///
    /// För vanliga attacker är detta samma som DirectSource.
    /// För en framtida summon kan detta vara summonens ägare.
    /// </summary>
    public CharacterStats CreditOwner { get; }

    public AbilityData Ability { get; }

    public bool HasDirectSource =>
        DirectSource != null;

    public bool HasCreditOwner =>
        CreditOwner != null;

    public bool HasAnySource =>
        HasDirectSource ||
        HasCreditOwner;

    public DamageSourceContext(
        CharacterStats directSource,
        CharacterStats creditOwner = null,
        AbilityData ability = null)
    {
        DirectSource = directSource;

        CreditOwner =
            creditOwner != null
                ? creditOwner
                : directSource;

        Ability = ability;
    }

    public static DamageSourceContext FromDirectSource(
        CharacterStats source,
        AbilityData ability = null)
    {
        return new DamageSourceContext(
            source,
            source,
            ability
        );
    }
}
