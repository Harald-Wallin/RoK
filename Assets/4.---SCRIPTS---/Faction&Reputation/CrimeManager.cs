using UnityEngine;

public static class CrimeManager
{
    public static void HandleAttackCrime(
        CharacterStats attacker,
        CharacterStats victim
    )
    {
        if (attacker == null || victim == null)
            return;

        PlayerStats player =
            attacker.GetComponent<PlayerStats>();

        if (player == null)
            return;

        if (victim.faction == null)
            return;

        PlayerReputationManager rep =
            player.GetComponent<PlayerReputationManager>();

        if (rep == null)
            return;

        // Murder mode måste vara enabled
        if (!rep.IsMurderEnabled(victim.faction))
            return;

        int penalty = victim.reputationLossOnHit;

        if (penalty != 0)
        {
            rep.AddReputation(
                victim.faction,
                -penalty
            );
        }
    }

    public static void HandleMurderCrime(
        CharacterStats killer,
        CharacterStats victim
    )
    {
        if (killer == null || victim == null)
            return;

        PlayerStats player =
            killer.GetComponent<PlayerStats>();

        if (player == null)
            return;

        if (victim.faction == null)
            return;

        PlayerReputationManager rep =
            player.GetComponent<PlayerReputationManager>();

        if (rep == null)
            return;

        if (!rep.IsMurderEnabled(victim.faction))
            return;

        int penalty = victim.reputationLossOnDeath;

        if (penalty != 0)
        {

            //Debug.Log(
            //$"Crime triggered against {victim.faction.factionName}");

            rep.AddReputation(
                victim.faction,
                -penalty
            );
        }
    }
}