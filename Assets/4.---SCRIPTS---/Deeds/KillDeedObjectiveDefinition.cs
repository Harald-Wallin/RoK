
using System;
using UnityEngine;

public enum KillDeedTargetMode
{
    SpecificCreature,
    CreatureTypes
}

/// <summary>
/// Deed objective for defeating creatures.
///
/// Supports:
/// - Specific creature definitions
/// - Legacy EntityReference matching
/// - One or multiple creature types
///
/// Kill credit uses DeathReward rules.
/// </summary>
[CreateAssetMenu(
    menuName = "RöK/Deeds/Objectives/Kill Entity"
)]
public sealed class KillDeedObjectiveDefinition :
    DeedObjectiveDefinition
{
    // =====================================================
    // TARGET MODE
    // =====================================================

    [Header("Target Matching")]

    [SerializeField]
    private KillDeedTargetMode targetMode =
        KillDeedTargetMode.SpecificCreature;

    // =====================================================
    // SPECIFIC CREATURE
    // =====================================================

    [Header("Specific Creature")]

    [SerializeField]
    [Tooltip(
        "Preferred exact creature definition. " +
        "Matches NPCs referencing this CreatureDefinition."
    )]
    private CreatureDefinition specificCreature;

    [SerializeField]
    [Tooltip(
        "Legacy EntityReference matching. " +
        "Used when Specific Creature is not assigned."
    )]
    private EntityReference target = new();

    // =====================================================
    // CREATURE TYPES
    // =====================================================

    [Header("Creature Types")]

    [SerializeField]
    [Tooltip(
        "Select one or multiple creature types. " +
        "An NPC only needs to match ONE selected type."
    )]
    private CreatureType creatureTypes =
        CreatureType.None;

    // =====================================================
    // PUBLIC API
    // =====================================================

    public KillDeedTargetMode TargetMode =>
        targetMode;

    public CreatureDefinition SpecificCreature =>
        specificCreature;

    public CreatureType CreatureTypes =>
        creatureTypes;

    public string TargetEntityId =>
        target != null
            ? target.Id
            : string.Empty;

    public override DeedObjectiveRuntime CreateRuntime()
    {
        return new KillDeedObjectiveRuntime(this);
    }

    // =====================================================
    // TARGET MATCHING
    // =====================================================

    private bool MatchesTarget(
        CharacterDefeatedResult defeat)
    {
        if (defeat == null ||
            defeat.Victim == null)
        {
            return false;
        }

        switch (targetMode)
        {
            case KillDeedTargetMode.SpecificCreature:
                return MatchesSpecificCreature(defeat);

            case KillDeedTargetMode.CreatureTypes:
                return MatchesCreatureTypes(defeat);

            default:
                return false;
        }
    }

    private bool MatchesSpecificCreature(
        CharacterDefeatedResult defeat)
    {
        // Preferred: use the existing
        // authoritative CreatureDefinition.
        if (specificCreature != null)
        {
            return defeat.Creature ==
                specificCreature;
        }

        // Backwards compatibility with
        // existing Wolfslayer objectives.
        string expectedId = TargetEntityId;

        if (string.IsNullOrWhiteSpace(expectedId))
            return false;

        return string.Equals(
            expectedId,
            defeat.VictimEntityId,
            StringComparison.Ordinal
        );
    }

    private bool MatchesCreatureTypes(
        CharacterDefeatedResult defeat)
    {
        if (creatureTypes == CreatureType.None)
            return false;

        CreatureType victimTypes =
            defeat.Victim.CreatureTypes;

        // ANY matching flag qualifies.
        return (victimTypes & creatureTypes) !=
            CreatureType.None;
    }

    // =====================================================
    // KILL CREDIT
    // =====================================================

    private static bool HasKillCredit(
        CharacterDefeatedResult defeat,
        PlayerStats player)
    {
        if (defeat == null ||
            defeat.Victim == null ||
            player == null)
        {
            return false;
        }

        DeathReward reward =
            defeat.Victim.deathReward;

        if (reward != null)
        {
            // Exactly the same credit policy
            // used by XP and loot.
            return reward.HasPlayerKillCredit(
                defeat,
                player
            );
        }

        // Fallback for a creature without
        // a DeathReward component.
        return defeat.IsTopDamageContributor(
            player
        );
    }

    // =====================================================
    // RUNTIME
    // =====================================================

    private sealed class KillDeedObjectiveRuntime :
        DeedObjectiveRuntime
    {
        private readonly KillDeedObjectiveDefinition
            definition;

        public KillDeedObjectiveRuntime(
            KillDeedObjectiveDefinition definition)
        {
            this.definition = definition;
        }

        public override void Receive(
            DeedProgressEvent progressEvent)
        {
            if (progressEvent is not
                    DeedCharacterDefeatedEvent kill ||
                kill.Actor == null ||
                kill.Defeat == null ||
                definition == null)
            {
                return;
            }

            if (!definition.MatchesTarget(
                    kill.Defeat))
            {
                return;
            }

            if (!HasKillCredit(
                    kill.Defeat,
                    kill.Actor))
            {
                return;
            }

            AddProgress(1);
        }
    }
}
