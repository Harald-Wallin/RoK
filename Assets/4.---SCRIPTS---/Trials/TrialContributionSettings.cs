using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "TrialContributionSettings",
    menuName = "RPG/Trials/Contribution Settings")]
public sealed class TrialContributionSettings : ScriptableObject
{
    [Header("Contribution Points")]

    [SerializeField, Min(0f)]
    private float damagePerHealthPercent = 1f;

    [SerializeField, Min(0f)]
    private float healingPerHealthPercent = 1f;

    [SerializeField, Min(0f)]
    private float tankingPerEnemySecond = 2f;

    [SerializeField, Min(0f)]
    private float buffPoints = 15f;

    [SerializeField, Min(0f)]
    private float dispelPoints = 15f;

    [SerializeField, Min(0f)]
    private float interactionPoints = 10f;

    [SerializeField, Min(0f)]
    private float destructionPoints = 10f;

    [Header("Participation")]

    [SerializeField, Min(0.1f)]
    private float activitySeconds = 15f;

    [Tooltip(
        "Provisional global minimum. Smaller contributions do not " +
        "affect the reward average or qualify for rewards.")]
    [SerializeField, Min(0.01f)]
    private float minimumPoints = 10f;

    [Header("Loot Thresholds — Percent Of Average")]

    [SerializeField, Min(0f)]
    private float tierOnePercent = 10f;

    [SerializeField, Min(0f)]
    private float tierTwoPercent = 50f;

    [SerializeField, Min(0f)]
    private float tierThreePercent = 80f;

    public float ActivitySeconds =>
        Mathf.Max(0.1f, activitySeconds);

    public float TankingPerEnemySecond =>
        Mathf.Max(0f, tankingPerEnemySecond);

    public float BuffPoints =>
        Mathf.Max(0f, buffPoints);

    public float DispelPoints =>
        Mathf.Max(0f, dispelPoints);

    public float InteractionPoints =>
        Mathf.Max(0f, interactionPoints);

    public float DestructionPoints =>
        Mathf.Max(0f, destructionPoints);

    public float MinimumPoints =>
        Mathf.Max(0.01f, minimumPoints);

    public double DamagePoints(
        int applied,
        int maximumHealth)
    {
        return HealthPoints(
            applied,
            maximumHealth,
            damagePerHealthPercent);
    }

    public double HealingPoints(
        int applied,
        int maximumHealth)
    {
        return HealthPoints(
            applied,
            maximumHealth,
            healingPerHealthPercent);
    }

    public bool Qualifies(double points)
    {
        return points >= MinimumPoints;
    }

    public int GetTier(double ratio)
    {
        double one =
            Math.Max(0f, tierOnePercent) / 100d;

        double two =
            Math.Max(tierOnePercent, tierTwoPercent) / 100d;

        double three =
            Math.Max(
                Math.Max(tierOnePercent, tierTwoPercent),
                tierThreePercent) / 100d;

        if (ratio <= 0d || ratio < one)
            return 0;

        if (ratio >= three)
            return 3;

        return ratio >= two ? 2 : 1;
    }

    [Header("Placement Reward Containers — Shared By All Trials")]

    [SerializeField]
    private ItemData firstPlaceContainer;

    [SerializeField]
    private ItemData secondPlaceContainer;

    [SerializeField]
    private ItemData thirdPlaceContainer;

    public ItemData GetPlacementContainer(int placement)
    {
        ItemData item = placement switch
        {
            1 => firstPlaceContainer,
            2 => secondPlaceContainer,
            3 => thirdPlaceContainer,
            _ => null
        };

        return item != null && item.IsContainer
            ? item
            : null;
    }

    private static double HealthPoints(
        int applied,
        int maximumHealth,
        float multiplier)
    {
        return applied > 0 && maximumHealth > 0
            ? 100d * applied / maximumHealth *
              Mathf.Max(0f, multiplier)
            : 0d;
    }
}

public readonly struct TrialParticipantResult
{
    public readonly PlayerStats Player;
    public readonly double Points;
    public readonly double AverageRatio;
    public readonly double TotalShare;
    public readonly int Placement;
    public readonly bool Eligible;

    public TrialParticipantResult(
        PlayerStats player,
        double points,
        double averageRatio,
        double totalShare,
        int placement,
        bool eligible)
    {
        Player = player;
        Points = points;
        AverageRatio = averageRatio;
        TotalShare = totalShare;
        Placement = placement;
        Eligible = eligible;
    }
}
