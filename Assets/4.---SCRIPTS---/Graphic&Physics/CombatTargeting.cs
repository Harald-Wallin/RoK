using UnityEngine;

public static class CombatTargeting
{
    public static bool CanAttack(
        CharacterStats attacker,
        CharacterStats target)
    {
        if (attacker == null ||
            target == null)
        {
            return false;
        }

        if (attacker == target)
        {
            return false;
        }

        NPCBehavior attackerAI =
            attacker.GetComponent<NPCBehavior>();

        if (attackerAI != null &&
            attackerAI.IsEncounterResetting)
        {
            return false;
        }

        NPCBehavior targetAI =
            target.GetComponent<NPCBehavior>();

        if (targetAI != null &&
            targetAI.IsEncounterResetting)
        {
            return false;
        }

        // =========================
        // PLAYER RULES
        // =========================

        PlayerStats player =
            attacker.GetComponent<PlayerStats>();

        if (player != null)
        {
            return CanPlayerAttack(
                player,
                target
            );
        }

        // =========================
        // NPC RULES
        // =========================

        if (attackerAI != null &&
            attackerAI.CurrentTarget == target)
        {
            return true;
        }

        return attacker.IsHostileTo(
            target
        );
    }

    private static bool CanPlayerAttack(
        PlayerStats player,
        CharacterStats target)
    {
        if (target.faction == null)
            return true;

        if (player.IsHostileTo(
                target))
        {
            return true;
        }

        PlayerReputationManager reputation =
            player.GetComponent<
                PlayerReputationManager
            >();

        if (reputation == null)
            return false;

        return reputation.IsMurderEnabled(
            target.faction
        );
    }
}