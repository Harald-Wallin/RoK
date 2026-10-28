public sealed class CharacterDefeatedResult
{
    public CharacterStats Victim { get; }

    public CreatureDefinition Creature { get; }

    public string VictimEntityId { get; }

    public DamageSourceContext FinalBlow { get; }

    public DamageContributionSnapshot Contributions { get; }

    public CharacterStats FinalBlowSource =>
        FinalBlow.DirectSource;

    public CharacterStats FinalBlowCreditOwner =>
        FinalBlow.CreditOwner;

    public bool IsTopDamageContributor(
        CharacterStats creditOwner,
        bool allowTies = true)
    {
        return Contributions
            .IsTopContributor(
                creditOwner,
                allowTies
            );
    }

    public CharacterDefeatedResult(
        CharacterStats victim,
        CreatureDefinition creature,
        DamageSourceContext finalBlow,
        DamageContributionSnapshot contributions)
    {
        Victim = victim;
        Creature = creature;
        FinalBlow = finalBlow;

        VictimEntityId =
            ResolveVictimEntityId(
                victim
            );

        Contributions =
            contributions ??
            new DamageContributionSnapshot(
                null,
                0
            );
    }

    private static string ResolveVictimEntityId(
        CharacterStats victim)
    {
        if (victim == null)
            return string.Empty;

        EntityIdentity identity =
            EntityTargetUtility
                .GetIdentity(
                    victim.gameObject
                );

        return identity != null
            ? identity.Id
            : string.Empty;
    }

    public float GetDamageShare(
        CharacterStats creditOwner)
    {
        return Contributions.GetDamageShare(
            creditOwner
        );
    }

    public bool HasMinimumDamageShare(
        CharacterStats creditOwner,
        float minimumShare)
    {
        return Contributions.HasMinimumShare(
            creditOwner,
            minimumShare
        );
    }
}