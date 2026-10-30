using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ReputationReward
{
    public Faction faction;
    public int reputation;
}

public class DeathReward :
    MonoBehaviour
{
    [Header("Experience")]

    public bool useDynamicExperience = true;
    public int experience;

    [Header("Reputation")]

    public List<ReputationReward>
        reputationRewards =
            new();

    [Header("Loot")]

    public GameObject corpsePrefab;

    [Header("Credit")]

    [SerializeField]
    [Tooltip(
        "Om exakt delad högsta contribution fortfarande ska " +
        "ge spelaren reward-credit."
    )]
    private bool allowTiedTopContribution =
        true;

    [Header("Visuals")]

    public GameObject floatingExpTextPrefab;

    [Header("World")]

    public bool unlockStartZoneWolf;

    // =========================================================
    // EXPERIENCE
    // =========================================================

    public int GetExperience(
        int targetLevel,
        int playerLevel)
    {
        if (useDynamicExperience)
        {
            return ExperienceCalculator
                .CalculateExp(
                    targetLevel,
                    playerLevel
                );
        }

        return experience;
    }

    // =========================================================
    // DEATH RESULT
    // =========================================================

    public void GiveRewards(
        CharacterDefeatedResult result)
    {
        if (result == null ||
            result.Victim == null)
        {
            return;
        }

        CharacterStats victim =
            result.Victim;

        PlayerStats player =
            PlayerReference.Player;

        bool playerHasCredit =
            HasPlayerKillCredit(
                result,
                player
            );

        /*
         * Corpse skapas ALLTID här.
         *
         * Detta är viktigt:
         *
         * tidigare skapades corpse från NPCBehavior innan
         * contribution-resultatet ens hade utvärderats.
         *
         * Nu känner corpse-generationen till reward ownership.
         */
        SpawnCorpse(
            victim.transform.position,
            victim,
            playerHasCredit
        );

        if (!playerHasCredit ||
            player == null)
        {
            return;
        }

        GiveRewardsInternal(
            victim,
            player
        );
    }

    // =========================================================
    // REWARD PAYOUT
    // =========================================================

    private void GiveRewardsInternal(
        CharacterStats victim,
        PlayerStats player)
    {
        if (victim == null ||
            player == null)
        {
            return;
        }

        int exp =
            GetExperience(
                victim.level,
                player.level
            );

        if (exp > 0)
        {
            player.GainExp(
                exp
            );

            ShowFloatingExperience(
                victim,
                exp
            );
        }

        PlayerReputationManager reputation =
            player.GetComponent<
                PlayerReputationManager>();

        if (reputation != null)
        {
            foreach (ReputationReward reward
                     in reputationRewards)
            {
                if (reward == null ||
                    reward.faction == null ||
                    reward.reputation == 0)
                {
                    continue;
                }

                reputation.AddReputation(
                    reward.faction,
                    reward.reputation
                );
            }
        }

        if (player.murderMode &&
            !victim.IsHostileToPlayer(
                player) &&
            victim.faction != null &&
            reputation != null)
        {
            reputation.AddReputation(
                victim.faction,
                -victim.reputationLossOnDeath
            );
        }
    }

    // =========================================================
    // EXPERIENCE FEEDBACK
    // =========================================================

    private void ShowFloatingExperience(
        CharacterStats victim,
        int amount)
    {
        if (floatingExpTextPrefab == null ||
            victim == null ||
            amount <= 0)
        {
            return;
        }

        GameObject text =
            Instantiate(
                floatingExpTextPrefab,
                victim.transform.position +
                Vector3.up * 1.5f,
                Quaternion.identity
            );

        TMPro.TMP_Text tmp =
            text.GetComponentInChildren<
                TMPro.TMP_Text>();

        if (tmp != null)
        {
            tmp.text =
                amount +
                " EXP";
        }
    }

    // =========================================================
    // LOOT GENERATION
    // =========================================================

    public void GetPossibleLootItems(
    List<ItemData> results)
    {
        if (results == null)
            return;

        results.Clear();

        if (corpsePrefab == null)
            return;

        LootableCorpse corpse =
            corpsePrefab.GetComponent<
                LootableCorpse>();

        if (corpse == null)
            return;

        corpse.GetPossibleLootItems(
            results
        );
    }


    // =========================================================
    // CORPSE
    // =========================================================

    public GameObject SpawnCorpse(
        Vector3 position,
        CharacterStats owner,
        bool generatePlayerLoot)
    {
        if (corpsePrefab == null)
            return null;

        GameObject corpse =
            Instantiate(
                corpsePrefab,
                position,
                Quaternion.identity
            );

        Debug.Log(
    $"[CORPSE] Spawned corpse '{corpse.name}' " +
    $"for '{owner?.DisplayName ?? "Unknown"}' " +
    $"at {position}. " +
    $"PlayerLoot={generatePlayerLoot}",
    corpse
);

        LootableCorpse lootableCorpse =
    corpse.GetComponent<
        LootableCorpse>();

        if (lootableCorpse != null)
        {
            /*
             * Corpse får bara player-loot om spelaren faktiskt
             * vann contribution.
             */
            lootableCorpse.Initialize(
                generatePlayerLoot
            );
        }
        else
        {
            Debug.LogError(
                "DeathReward: Corpse prefab saknar LootableCorpse.",
                corpse
            );
        }

        CharacterStats corpseStats =
            corpse.GetComponent<
                CharacterStats>();

        if (corpseStats != null)
        {
            corpseStats.faction =
                null;
        }

        if (owner != null)
        {
            MoveNameplateToCorpse(
                owner,
                corpse
            );
        }

        return corpse;
    }

    /// <summary>
    /// Legacy-overload.
    /// </summary>
    public GameObject SpawnCorpse(
        Vector3 position,
        CharacterStats owner)
    {
        return SpawnCorpse(
            position,
            owner,
            true
        );
    }

    private static void MoveNameplateToCorpse(
        CharacterStats owner,
        GameObject corpse)
    {
        if (owner == null ||
            corpse == null)
        {
            return;
        }

        Transform nameplate =
            owner.transform.Find(
                "Nameplate"
            );

        if (nameplate == null)
            return;

        nameplate.SetParent(
            corpse.transform,
            true
        );

        NameplateUI ui =
            nameplate.GetComponentInChildren<
                NameplateUI>();

        if (ui != null)
        {
            ui.SetCorpseMode();
        }
    }


    // =========================================================
    // SHARED KILL CREDIT
    // =========================================================

    /// <summary>
    /// Authoritative player kill-credit rule.
    ///
    /// Used by XP, loot and Deeds.
    /// </summary>
    public bool HasPlayerKillCredit(
        CharacterDefeatedResult result,
        PlayerStats player)
    {
        if (result == null ||
            result.Victim == null ||
            player == null)
        {
            return false;
        }

        return result.IsTopDamageContributor(
            player,
            allowTiedTopContribution
        );
    }

}