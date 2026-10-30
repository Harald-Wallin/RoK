
using System.Collections.Generic;
using UnityEngine;

// =========================================================
// DEED TITLE UNLOCK
// =========================================================

public readonly struct DeedTitleUnlock
{
    public readonly string DeedId;
    public readonly string RankId;

    public readonly string Title;
    public readonly string SourceDeed;
    public readonly string RankLabel;

    public DeedTitleUnlock(
        string deedId,
        string rankId,
        string title,
        string sourceDeed,
        string rankLabel)
    {
        DeedId = deedId;
        RankId = rankId;
        Title = title;
        SourceDeed = sourceDeed;
        RankLabel = rankLabel;
    }
}

// =========================================================
// DEED UNLOCK CATALOG
// =========================================================

/// <summary>
/// Derives rewards and training eligibility from
/// completed Deed ranks.
///
/// This class does not grant experience, currency,
/// or directly teach abilities.
///
/// DeedRuntime remains authoritative for unlocks.
/// </summary>
public static class DeedUnlockCatalog
{
    // =====================================================
    // UNLOCKED TITLES
    // =====================================================

    public static List<DeedTitleUnlock>
        GetUnlockedTitles(PlayerDeedManager manager)
    {
        List<DeedTitleUnlock> result = new();

        if (manager == null)
            return result;

        foreach (DeedRuntime deed in manager.Deeds)
        {
            if (deed == null ||
                deed.Definition == null)
            {
                continue;
            }

            DeedDefinition definition =
                deed.Definition;

            foreach (DeedRankDefinition rank
                     in definition.Ranks)
            {
                if (rank == null ||
                    !deed.HasCompleted(rank) ||
                    rank.Rewards == null)
                {
                    continue;
                }

                foreach (DeedRewardPreview reward
                         in rank.Rewards)
                {
                    if (reward == null ||
                        reward.Kind !=
                            DeedRewardKind.Title)
                    {
                        continue;
                    }

                    result.Add(
                        new DeedTitleUnlock(
                            definition.PersistentId,
                            rank.RankId,
                            reward.DisplayName,
                            definition.DisplayName,
                            rank.RankLabel
                        )
                    );
                }
            }
        }

        return result;
    }

    // =====================================================
    // TRAINABLE ABILITIES
    // =====================================================

    public static List<AbilityData>
        GetTrainableAbilities(
            PlayerDeedManager manager)
    {
        List<AbilityData> result = new();

        if (manager == null)
            return result;

        HashSet<AbilityData> seen = new();

        foreach (DeedRuntime deed in manager.Deeds)
        {
            if (deed == null ||
                deed.Definition == null)
            {
                continue;
            }

            foreach (DeedRankDefinition rank
                     in deed.Definition.Ranks)
            {
                if (rank == null ||
                    !deed.HasCompleted(rank) ||
                    rank.Rewards == null)
                {
                    continue;
                }

                foreach (DeedRewardPreview reward
                         in rank.Rewards)
                {
                    if (reward == null ||
                        reward.Kind !=
                            DeedRewardKind.TrainableAbility ||
                        reward.Ability == null)
                    {
                        continue;
                    }

                    if (seen.Add(reward.Ability))
                    {
                        result.Add(reward.Ability);
                    }
                }
            }
        }

        return result;
    }

    // =====================================================
    // TRAINING ELIGIBILITY
    // =====================================================

    /// <summary>
    /// Returns true when the player completed
    /// a Deed rank that unlocks this ability.
    ///
    /// This means the player is eligible to
    /// learn it, not that it has been learned.
    /// </summary>
    public static bool CanTrainAbility(
        PlayerDeedManager manager,
        AbilityData ability)
    {
        if (manager == null ||
            ability == null)
        {
            return false;
        }

        foreach (DeedRuntime deed in manager.Deeds)
        {
            if (deed == null ||
                deed.Definition == null)
            {
                continue;
            }

            foreach (DeedRankDefinition rank
                     in deed.Definition.Ranks)
            {
                if (rank == null ||
                    !deed.HasCompleted(rank) ||
                    rank.Rewards == null)
                {
                    continue;
                }

                foreach (DeedRewardPreview reward
                         in rank.Rewards)
                {
                    if (reward == null ||
                        reward.Kind !=
                            DeedRewardKind.TrainableAbility)
                    {
                        continue;
                    }

                    if (reward.Ability == ability)
                        return true;
                }
            }
        }

        return false;
    }

    // =====================================================
    // DEBUG PRESENTATION
    // =====================================================

    public static void PrintUnlocks(
        PlayerDeedManager manager)
    {
        if (manager == null)
        {
            Debug.LogWarning(
                "[DEEDS] PlayerDeedManager missing."
            );
            return;
        }

        List<DeedTitleUnlock> titles =
            GetUnlockedTitles(manager);

        List<AbilityData> abilities =
            GetTrainableAbilities(manager);

        Debug.Log(
            $"[DEEDS] Unlocked titles: {titles.Count}"
        );

        foreach (DeedTitleUnlock title in titles)
        {
            Debug.Log(
                $"[DEEDS] Title: {title.Title} " +
                $"(Source: {title.SourceDeed} " +
                $"{title.RankLabel})"
            );
        }

        Debug.Log(
            $"[DEEDS] Trainable abilities: " +
            $"{abilities.Count}"
        );

        foreach (AbilityData ability in abilities)
        {
            Debug.Log(
                $"[DEEDS] Can train: " +
                ability.DisplayName
            );
        }
    }
}

// =========================================================
// UNITY EDITOR DEBUG MENU
// =========================================================

#if UNITY_EDITOR

public static class DeedUnlockDebugMenu
{
    [UnityEditor.MenuItem(
        "RöK/Debug/Print Deed Unlocks"
    )]
    private static void PrintDeedUnlocks()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[DEEDS] Enter Play Mode first."
            );
            return;
        }

        PlayerDeedManager manager =
            Object.FindFirstObjectByType<
                PlayerDeedManager>();

        DeedUnlockCatalog.PrintUnlocks(manager);
    }
}

#endif

