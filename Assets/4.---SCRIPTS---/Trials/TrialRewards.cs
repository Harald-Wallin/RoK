using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class TrialRewards
{
    [Serializable]
    private sealed class FactionReward
    {
        [SerializeField] private Faction faction;
        [SerializeField, Min(0)] private int baseReputation;

        public Faction Faction => faction;
        public int BaseReputation => baseReputation;
    }

    private sealed class Receipt
    {
        public readonly float Contribution;
        public readonly int Tier;
        public readonly List<string> Paid = new();

        public LootGenerationResult Loot;

        public Receipt(float contribution, int tier)
        {
            Contribution = contribution;
            Tier = tier;
        }
    }

    [Header("Progression")]
    [SerializeField, Min(0)] private int baseExperience = 1000;
    [SerializeField, Range(0f, 100f)] private float minimumContributionPercent = 5f;
    [SerializeField, Min(0f)] private float failedRewardPercent = 50f;
    [SerializeField, Min(0f)] private float completedRewardPercent = 120f;
    [SerializeField] private List<FactionReward> factionRewards = new();

    [Header("Victory Chest")]
    [SerializeField] private LootContainer rewardContainerPrefab;
    [SerializeField, HideInInspector] private Transform rewardContainerPoint;
    [SerializeField] private Sprite fullChestSprite;
    [SerializeField] private Sprite emptyChestSprite;

    [Header("Loot Tiers")]
    [SerializeField, Range(0f, 100f)] private float tierOnePercent = 10f;
    [SerializeField] private LootTable tierOneLoot;
    [SerializeField, Range(0f, 100f)] private float tierTwoPercent = 30f;
    [SerializeField] private LootTable tierTwoLoot;
    [SerializeField, Range(0f, 100f)] private float tierThreePercent = 85f;
    [SerializeField] private LootTable tierThreeLoot;

    private readonly Dictionary<PlayerStats, Receipt> receipts = new();
    private LootContainer localChest;
    private bool settled;

    internal void Settle(
        bool completed,
        IReadOnlyList<(PlayerStats Player, float Contribution)> participants)
    {
        if (settled)
            return;

        settled = true;

        // Freeze every result before awarding XP or firing reputation events.
        foreach (var participant in participants)
        {
            PlayerStats player = participant.Player;

            if (player == null || receipts.ContainsKey(player))
                continue;

            float contribution = Mathf.Clamp01(participant.Contribution);
            int tier = completed && IsEligible(contribution)
                ? GetTier(contribution)
                : 0;

            receipts.Add(player, new Receipt(contribution, tier));
        }

        float payoutPercent = completed ? completedRewardPercent : failedRewardPercent;

        foreach (var participant in participants)
        {
            PlayerStats player = participant.Player;

            if (!settled)
                return;

            if (player == null ||
                !receipts.TryGetValue(player, out Receipt receipt) ||
                !IsEligible(receipt.Contribution))
            {
                continue;
            }

            GrantProgression(player, receipt, payoutPercent);
            RollChest(receipt);
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
            UnityEngine.Object.Destroy(chest.gameObject);
        }
    }

    internal string Describe(PlayerStats player)
    {
        if (player == null || !receipts.TryGetValue(player, out Receipt receipt))
            return string.Empty;

        string contribution = $"{receipt.Contribution:P0} contribution";

        if (!IsEligible(receipt.Contribution))
            return $"{contribution} · Below reward minimum";

        string tier = receipt.Tier switch
        {
            1 => "Tier I",
            2 => "Tier II",
            3 => "Tier III",
            _ => string.Empty
        };

        if (receipt.Loot != null)
            contribution += $" · {tier}";

        return receipt.Paid.Count > 0
            ? $"{contribution}\n{string.Join(" · ", receipt.Paid)}"
            : contribution;
    }

    private bool IsEligible(float contribution)
    {
        return contribution >= minimumContributionPercent / 100f;
    }

    private int GetTier(float contribution)
    {
        if (contribution >= tierThreePercent / 100f)
            return 3;

        if (contribution >= tierTwoPercent / 100f)
            return 2;

        return contribution >= tierOnePercent / 100f ? 1 : 0;
    }

    private void GrantProgression(
        PlayerStats player, Receipt receipt, float payoutPercent)
    {
        int experience = Scale(baseExperience, receipt.Contribution, payoutPercent);

        if (experience > 0 && player.GainExp(experience))
            receipt.Paid.Add($"+{experience} XP");

        PlayerReputationManager reputation =
            player.GetComponent<PlayerReputationManager>();

        if (reputation == null)
            return;

        foreach (FactionReward reward in factionRewards)
        {
            if (reward == null || reward.Faction == null)
                continue;

            int amount = Scale(
                reward.BaseReputation, receipt.Contribution, payoutPercent);

            if (amount > 0 && reputation.AddReputation(reward.Faction, amount))
                receipt.Paid.Add($"+{amount} {reward.Faction.DisplayName} reputation");
        }
    }

    private void RollChest(Receipt receipt)
    {
        LootTable table = receipt.Tier switch
        {
            1 => tierOneLoot,
            2 => tierTwoLoot,
            3 => tierThreeLoot,
            _ => null
        };

        if (table != null)
        {
            receipt.Loot = LootGenerator.GenerateLootResult(
                new List<LootTable> { table }, 1, 1);
        }
    }

    private void SpawnLocalChest()
    {
        PlayerStats player = PlayerReference.Player;

        if (player == null ||
            !receipts.TryGetValue(player, out Receipt receipt) ||
            receipt.Loot == null)
        {
            return;
        }

        if (rewardContainerPrefab == null || rewardContainerPoint == null ||
            !rewardContainerPrefab.IsTrialManaged ||
            !rewardContainerPrefab.gameObject.activeSelf)
        {
            Debug.LogWarning(
                "Trial chest needs an active Trial Managed LootContainer prefab and a RewardContainerPoint.",
                player);
            return;
        }

        localChest = UnityEngine.Object.Instantiate(
            rewardContainerPrefab,
            rewardContainerPoint.position,
            rewardContainerPoint.rotation);

        localChest.InitializeTrialLoot(
            player, receipt.Loot, fullChestSprite, emptyChestSprite);
    }

    private static int Scale(int amount, float contribution, float payoutPercent)
    {
        double scaled =
            Math.Max(0, amount) * (double)contribution *
            Math.Max(0f, payoutPercent) / 100d;

        return (int)Math.Min(
            int.MaxValue, Math.Round(scaled, MidpointRounding.AwayFromZero));
    }
}
