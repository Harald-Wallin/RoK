
using UnityEngine;

/// <summary>
/// Handles immediate Deed rank rewards.
///
/// Currently supported:
/// - Experience
/// - Currency
///
/// Title and TrainableAbility rewards represent
/// unlocks, not automatic ability learning.
///
/// Item and Cosmetic delivery are reserved
/// for future reward adapters.
/// </summary>
public static class DeedRewardDelivery
{
    public static void GrantImmediateRewards(
        PlayerStats player,
        DeedRuntime deed,
        DeedRankDefinition rank)
    {
        if (player == null ||
            deed == null ||
            rank == null ||
            rank.Rewards == null)
        {
            return;
        }

        foreach (DeedRewardPreview reward
                 in rank.Rewards)
        {
            if (reward == null)
                continue;

            switch (reward.Kind)
            {
                case DeedRewardKind.Experience:
                    GrantExperience(
                        player,
                        reward.Amount
                    );
                    break;

                case DeedRewardKind.Currency:
                    GrantCurrency(
                        player,
                        reward.Amount
                    );
                    break;

                // Unlocks are derived from the
                // completed Deed rank.
                case DeedRewardKind.Title:
                case DeedRewardKind.TrainableAbility:
                    break;

                // Future delivery adapters.
                case DeedRewardKind.Item:
                case DeedRewardKind.Cosmetic:
                    break;
            }
        }
    }

    private static void GrantExperience(
        PlayerStats player,
        int amount)
    {
        if (amount <= 0)
            return;

        player.GainExp(amount);
    }

    private static void GrantCurrency(
        PlayerStats player,
        int amount)
    {
        if (amount <= 0)
            return;

        PlayerCurrency currency =
            PlayerCurrency.Instance;

        if (currency == null)
        {
            Debug.LogWarning(
                "Deed reward could not grant currency: " +
                "PlayerCurrency is missing.",
                player
            );

            return;
        }

        currency.AddCoins(amount);
    }
}

