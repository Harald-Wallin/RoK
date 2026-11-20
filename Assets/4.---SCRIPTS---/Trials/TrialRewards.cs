using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class TrialRewards
{
    [Serializable]
    private sealed class FactionReward
    {
        [SerializeField]
        private Faction faction;

        [SerializeField, Min(0)]
        private int baseReputation;

        public Faction Faction => faction;
        public int BaseReputation => baseReputation;
    }

    private sealed class Receipt
    {
        public readonly TrialParticipantResult Result;
        public readonly int Tier;
        public readonly List<string> Paid = new();

        public LootGenerationResult Loot;

        public Receipt(
            TrialParticipantResult result,
            int tier)
        {
            Result = result;
            Tier = tier;
        }
    }

    [Header("Progression — Same Amount For Every Eligible Player")]

    [SerializeField, Min(0)]
    private int baseExperience = 1000;

    [SerializeField, Min(0f)]
    private float failedRewardPercent = 50f;

    [SerializeField, Min(0f)]
    private float completedRewardPercent = 120f;

    [SerializeField]
    private List<FactionReward> factionRewards = new();

    [Header("Victory Chest")]

    [SerializeField]
    private LootContainer rewardContainerPrefab;

    [SerializeField, HideInInspector]
    private Transform rewardContainerPoint;

    [SerializeField]
    private Sprite fullChestSprite;

    [SerializeField]
    private Sprite emptyChestSprite;

    [Header("Ordinary Loot — Thresholds Come From Contribution Settings")]

    [SerializeField]
    private LootTable tierOneLoot;

    [SerializeField]
    private LootTable tierTwoLoot;

    [SerializeField]
    private LootTable tierThreeLoot;

    [Header("Additional Placement Loot — Victory Only")]

    [SerializeField]
    private LootTable firstPlaceLoot;

    [SerializeField]
    private LootTable secondPlaceLoot;

    [SerializeField]
    private LootTable thirdPlaceLoot;

    private readonly Dictionary<PlayerStats, Receipt>
        receipts = new();

    private LootContainer localChest;
    private bool settled;

    internal void Settle(
    bool completed,
    IReadOnlyList<TrialParticipantResult> participants,
    TrialContributionSettings settings)
    {
        if (settled)
            return;

        settled = true;

        foreach (TrialParticipantResult result in participants)
        {
            if (result.Player == null ||
                receipts.ContainsKey(result.Player))
            {
                continue;
            }

            int tier =
                completed && result.Eligible
                    ? settings.GetTier(result.AverageRatio)
                    : 0;

            Receipt receipt = new(result, tier);
            receipts.Add(result.Player, receipt);

            if (completed && result.Eligible)
                RollChest(receipt, settings);
        }

        float payout = completed
            ? completedRewardPercent
            : failedRewardPercent;

        foreach (TrialParticipantResult result in participants)
        {
            if (!settled)
                return;

            if (result.Player != null &&
                result.Eligible &&
                receipts.TryGetValue(
                    result.Player,
                    out Receipt receipt))
            {
                GrantProgression(
                    result.Player,
                    receipt,
                    payout);
            }
        }

        if (settled)
            SpawnLocalChest();
    }

    internal void ResetRound()
    {
        LootContainer chest = localChest;
        localChest = null;

        receipts.Clear();
        settled = false;

        if (chest != null)
        {
            chest.ExpireTrialLoot();

            UnityEngine.Object.Destroy(
                chest.gameObject);
        }
    }

    internal string Describe(PlayerStats player)
    {
        if (player == null ||
            !receipts.TryGetValue(
                player,
                out Receipt receipt))
        {
            return string.Empty;
        }

        TrialParticipantResult result =
            receipt.Result;

        string text =
            $"{result.Points:0.#} CP";

        if (!result.Eligible)
            return $"{text} · Below participation minimum";

        text +=
            $" · {result.AverageRatio:P0} of average" +
            $" · Place {result.Placement}";

        if (receipt.Tier > 0)
            text += $" · Loot tier {receipt.Tier}";

        if (receipt.Paid.Count > 0)
        {
            text +=
                $"\n{string.Join(" · ", receipt.Paid)}";
        }

        return text;
    }

    private void GrantProgression(
        PlayerStats player,
        Receipt receipt,
        float payoutPercent)
    {
        int experience =
            Scale(baseExperience, payoutPercent);

        if (experience > 0 &&
            player.GainExp(experience))
        {
            receipt.Paid.Add(
                $"+{experience} XP");
        }

        PlayerReputationManager reputation =
            player.GetComponent<PlayerReputationManager>();

        if (reputation == null)
            return;

        foreach (FactionReward reward in factionRewards)
        {
            if (reward == null ||
                reward.Faction == null)
            {
                continue;
            }

            int amount = Scale(
                reward.BaseReputation,
                payoutPercent);

            if (amount > 0 &&
                reputation.AddReputation(
                    reward.Faction,
                    amount))
            {
                receipt.Paid.Add(
                    $"+{amount} " +
                    $"{reward.Faction.DisplayName} reputation");
            }
        }
    }

    private void RollChest(
    Receipt receipt,
    TrialContributionSettings settings)
    {
        LootTable ordinary = receipt.Tier switch
        {
            1 => tierOneLoot,
            2 => tierTwoLoot,
            3 => tierThreeLoot,
            _ => null
        };

        LootGenerationResult result =
            ordinary != null
                ? LootGenerator.GenerateLootResult(
                    new List<LootTable> { ordinary },
                    1,
                    1)
                : new LootGenerationResult();

        bool hasReward = ordinary != null;

        int placement =
            receipt.Result.Placement;

        LootTable contentsTable = placement switch
        {
            1 => firstPlaceLoot,
            2 => secondPlaceLoot,
            3 => thirdPlaceLoot,
            _ => null
        };

        if (contentsTable != null)
        {
            ItemData container =
                settings.GetPlacementContainer(placement);

            if (container == null)
            {
                Debug.LogError(
                    $"Trial placement {placement} needs a " +
                    "Container item in TrialContributionSettings.",
                    settings);
            }
            else
            {
                LootGenerationResult contents =
                    LootGenerator.GenerateLootResult(
                        new List<LootTable> { contentsTable },
                        1,
                        1);

                result.AddContainer(container, contents);
                hasReward = true;
            }
        }

        receipt.Loot = hasReward ? result : null;
    }

    private void SpawnLocalChest()
    {
        PlayerStats player =
            PlayerReference.Player;

        if (player == null ||
            !receipts.TryGetValue(
                player,
                out Receipt receipt) ||
            receipt.Loot == null)
        {
            return;
        }

        if (rewardContainerPrefab == null ||
            rewardContainerPoint == null ||
            !rewardContainerPrefab.IsTrialManaged ||
            !rewardContainerPrefab.gameObject.activeSelf)
        {
            Debug.LogWarning(
                "Trial chest needs an active Trial Managed " +
                "LootContainer prefab and a RewardContainerPoint.",
                player);

            return;
        }

        localChest =
            UnityEngine.Object.Instantiate(
                rewardContainerPrefab,
                rewardContainerPoint.position,
                rewardContainerPoint.rotation);

        localChest.InitializeTrialLoot(
            player,
            receipt.Loot,
            fullChestSprite,
            emptyChestSprite);
    }

    private static int Scale(
        int amount,
        float payoutPercent)
    {
        double scaled =
            Math.Max(0, amount) *
            Math.Max(0f, payoutPercent) /
            100d;

        return (int)Math.Min(
            int.MaxValue,
            Math.Round(
                scaled,
                MidpointRounding.AwayFromZero));
    }
}