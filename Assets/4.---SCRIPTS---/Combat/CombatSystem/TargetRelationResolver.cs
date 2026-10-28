using UnityEngine;

public static class TargetRelationResolver
{
    /// <summary>
    /// Beräknar den faktiska runtime-relationen från source
    /// till target.
    ///
    /// Relation kan komma från:
    /// - self
    /// - aktiv combat-relation
    /// - temporary hostility
    /// - player reputation
    /// - faction standing
    ///
    /// Murder mode behandlas INTE här.
    /// Murder är ett targeting-undantag som låter spelaren
    /// INITIERA våld mot ett annars icke-hostile target.
    /// </summary>
    public static TargetRelation Resolve(
        CharacterStats source,
        GameObject target)
    {
        if (source == null ||
            target == null)
        {
            return TargetRelation.None;
        }

        CharacterStats targetStats =
            TargetUtility.GetCharacterStats(
                target
            );

        if (targetStats == null)
        {
            /*
             * Destructibles och andra framtida
             * icke-character-targets är tills vidare neutrala.
             */
            return TargetRelation.Neutral;
        }

        if (targetStats == source)
        {
            return TargetRelation.Self;
        }

        PlayerStats sourcePlayer =
            source as PlayerStats;

        if (sourcePlayer != null)
        {
            return ResolvePlayerToCharacter(
                sourcePlayer,
                targetStats
            );
        }

        PlayerStats targetPlayer =
            targetStats as PlayerStats;

        if (targetPlayer != null)
        {
            return ResolveCharacterToPlayer(
                source,
                targetPlayer
            );
        }

        return ResolveCharacterToCharacter(
            source,
            targetStats
        );
    }

    // =========================================================
    // PLAYER -> CHARACTER
    // =========================================================

    private static TargetRelation
        ResolvePlayerToCharacter(
            PlayerStats player,
            CharacterStats target)
    {
        if (player == null ||
            target == null)
        {
            return TargetRelation.None;
        }

        /*
         * Runtime hostility mot spelaren.
         *
         * Täcker bland annat:
         * - Hated reputation
         * - local temporary hostility
         * - faction temporary hostility
         */
        if (target.IsHostileToPlayer(
                player))
        {
            return TargetRelation.Hostile;
        }

        if (target.faction != null &&
            FactionHostilitySystem.Instance != null &&
            FactionHostilitySystem.Instance
                .IsHostileToPlayer(
                    target.faction,
                    player
                ))
        {
            return TargetRelation.Hostile;
        }

        PlayerReputationManager reputation =
            player.GetComponent<
                PlayerReputationManager>();

        if (reputation != null &&
            target.faction != null)
        {
            ReputationState standing =
                reputation.GetReputationState(
                    target.faction
                );

            return ConvertStandingToRelation(
                standing
            );
        }

        if (player.faction != null &&
            player.faction ==
            target.faction)
        {
            return TargetRelation.Friendly;
        }

        return ResolveFactionRelation(
            player,
            target
        );
    }

    // =========================================================
    // CHARACTER -> PLAYER
    // =========================================================

    private static TargetRelation
        ResolveCharacterToPlayer(
            CharacterStats source,
            PlayerStats player)
    {
        if (source == null ||
            player == null)
        {
            return TargetRelation.None;
        }

        /*
         * VIKTIGT:
         *
         * Ett explicit pågående combat-target är alltid hostile
         * ur ability-targetingens perspektiv.
         *
         * Exempel:
         *
         * Guard har Player som CurrentTarget efter att spelaren
         * attackerat honom.
         *
         * Då får inte faction/reputation senare klassificera
         * spelaren som Neutral och blockera guardens attack.
         */
        if (CombatTargeting.CanAttack(
                source,
                player))
        {
            return TargetRelation.Hostile;
        }

        if (source.IsHostileToPlayer(
                player))
        {
            return TargetRelation.Hostile;
        }

        if (source.faction != null &&
            FactionHostilitySystem.Instance != null &&
            FactionHostilitySystem.Instance
                .IsHostileToPlayer(
                    source.faction,
                    player
                ))
        {
            return TargetRelation.Hostile;
        }

        if (source.faction != null &&
            source.faction ==
            player.faction)
        {
            return TargetRelation.Friendly;
        }

        if (source.IsFriendlyTo(
                player))
        {
            return TargetRelation.Friendly;
        }

        if (source.IsHostileTo(
                player))
        {
            return TargetRelation.Hostile;
        }

        return TargetRelation.Neutral;
    }

    // =========================================================
    // CHARACTER -> CHARACTER
    // =========================================================

    private static TargetRelation
        ResolveCharacterToCharacter(
            CharacterStats source,
            CharacterStats target)
    {
        if (source == null ||
            target == null)
        {
            return TargetRelation.None;
        }

        /*
         * Samma faction är fortfarande Friendly som grundregel.
         *
         * ReactionSystem ignorerar redan vanlig same-faction
         * friendly fire, så detta förblir vår normala sociala
         * relation.
         */
        if (source.faction != null &&
            source.faction ==
            target.faction)
        {
            return TargetRelation.Friendly;
        }

        /*
         * =====================================================
         * ACTIVE COMBAT RELATION
         * =====================================================
         *
         * Detta är den viktiga nya regeln.
         *
         * CombatTargeting känner till runtime combat-state:
         *
         * NPCBehavior.CurrentTarget == target
         *
         * betyder att target redan är ett legitimt combat-target.
         *
         * Ability-targeting måste acceptera samma sanning.
         *
         * Annars får vi exakt buggen:
         *
         * Wolf:
         *   Aggro
         *   CurrentTarget = Guard
         *
         * TargetResolver:
         *   Guard = Neutral
         *
         * => TargetNotAllowed
         */
        if (CombatTargeting.CanAttack(
                source,
                target))
        {
            return TargetRelation.Hostile;
        }

        /*
         * Permanent faction hostility.
         */
        if (source.IsHostileTo(
                target))
        {
            return TargetRelation.Hostile;
        }

        if (source.IsFriendlyTo(
                target))
        {
            return TargetRelation.Friendly;
        }

        return TargetRelation.Neutral;
    }

    // =========================================================
    // FACTION FALLBACK
    // =========================================================

    private static TargetRelation
        ResolveFactionRelation(
            CharacterStats source,
            CharacterStats target)
    {
        if (source == null ||
            target == null)
        {
            return TargetRelation.None;
        }

        if (source.faction == null ||
            target.faction == null)
        {
            return TargetRelation.Neutral;
        }

        if (source.faction ==
            target.faction)
        {
            return TargetRelation.Friendly;
        }

        ReputationState standing =
            source.faction.GetStanding(
                target.faction
            );

        return ConvertStandingToRelation(
            standing
        );
    }

    // =========================================================
    // REPUTATION -> TARGET RELATION
    // =========================================================

    private static TargetRelation
        ConvertStandingToRelation(
            ReputationState standing)
    {
        switch (standing)
        {
            case ReputationState.Hated:

                return TargetRelation.Hostile;

            case ReputationState.Indifferent:

                return TargetRelation.Neutral;

            default:

                return TargetRelation.Friendly;
        }
    }
}