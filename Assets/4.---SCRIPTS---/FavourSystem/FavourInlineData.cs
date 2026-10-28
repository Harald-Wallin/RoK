using System;
using System.Collections.Generic;
using UnityEngine;

public enum FavourReputationComparison
{
    AtLeast,
    AtMost,
    Exactly
}

public enum FavourRewardChoiceType
{
    Item,
    Ability
}

/// <summary>
/// Reputation som delas ut när favouren slutförs.
///
/// Amount får vara både positivt och negativt.
/// </summary>
[Serializable]
public sealed class FavourReputationReward
{
    [SerializeField]
    private Faction faction;

    [SerializeField]
    [Tooltip(
        "Positiva värden ökar reputation. " +
        "Negativa värden minskar reputation."
    )]
    private int amount;

    [SerializeField]
    [Tooltip(
        "Upptäcker factionen innan reputation delas ut."
    )]
    private bool discoverFaction = true;

    public Faction Faction =>
        faction;

    public int Amount =>
        amount;

    public bool DiscoverFaction =>
        discoverFaction;
}

/// <summary>
/// Ett fast item som delas ut när favouren slutförs.
/// </summary>
[Serializable]
public sealed class FavourItemReward
{
    [SerializeField]
    private ItemData item;

    [SerializeField]
    [Min(1)]
    private int amount = 1;

    public ItemData Item =>
        item;

    public int Amount =>
        Mathf.Max(
            1,
            amount
        );
}

/// <summary>
/// En ability som lärs ut när favouren slutförs.
///
/// Base attacks skickas senare automatiskt till
/// PlayerBaseAttackCollection.
/// Övriga abilities skickas till PlayerAbilityCollection.
/// </summary>
[Serializable]
public sealed class FavourAbilityReward
{
    [SerializeField]
    private AbilityData ability;

    public AbilityData Ability =>
        ability;
}

/// <summary>
/// Ett alternativ i en valbar reward-grupp.
///
/// Type avgör om Item eller Ability används.
/// </summary>
[Serializable]
public sealed class FavourRewardChoiceOption
{
    [SerializeField]
    private FavourRewardChoiceType type =
        FavourRewardChoiceType.Item;

    [SerializeField]
    private ItemData item;

    [SerializeField]
    [Min(1)]
    private int itemAmount = 1;

    [SerializeField]
    private AbilityData ability;

    public FavourRewardChoiceType Type =>
        type;

    public ItemData Item =>
        item;

    public int ItemAmount =>
        Mathf.Max(
            1,
            itemAmount
        );

    public AbilityData Ability =>
        ability;

    public bool IsValid
    {
        get
        {
            switch (type)
            {
                case FavourRewardChoiceType.Item:
                    return item != null;

                case FavourRewardChoiceType.Ability:
                    return ability != null;

                default:
                    return false;
            }
        }
    }

    public string DisplayName
    {
        get
        {
            switch (type)
            {
                case FavourRewardChoiceType.Item:
                    return item != null
                        ? item.DisplayName
                        : "Missing Item";

                case FavourRewardChoiceType.Ability:
                    return ability != null &&
                           !string.IsNullOrWhiteSpace(
                               ability.abilityName)
                        ? ability.abilityName
                        : "Missing Ability";

                default:
                    return "Unknown Reward";
            }
        }
    }
}

/// <summary>
/// En grupp där spelaren måste välja ett bestämt antal
/// alternativ innan favouren får lämnas in.
/// </summary>
[Serializable]
public sealed class FavourRewardChoiceGroup
{
    [SerializeField]
    private string displayName =
        "Choose a Reward";

    [SerializeField]
    [Min(1)]
    private int choicesAllowed = 1;

    [SerializeField]
    private List<FavourRewardChoiceOption>
        options =
            new();

    public string DisplayName =>
        string.IsNullOrWhiteSpace(
            displayName)
            ? "Choose a Reward"
            : displayName;

    public int ChoicesAllowed =>
        Mathf.Clamp(
            choicesAllowed,
            1,
            Mathf.Max(
                1,
                options?.Count ?? 0
            )
        );

    public IReadOnlyList<
        FavourRewardChoiceOption>
        Options =>
            options;

    public void Normalize()
    {
        options ??=
            new List<
                FavourRewardChoiceOption>();

        choicesAllowed =
            Mathf.Clamp(
                choicesAllowed,
                1,
                Mathf.Max(
                    1,
                    options.Count
                )
            );
    }
}

/// <summary>
/// Reputation som krävs för att favouren ska vara tillgänglig.
///
/// Numerisk level används medvetet så att ranknamn och antal
/// ranks kan ändras utan att favour-assets behöver byggas om.
/// </summary>
[Serializable]
public sealed class FavourReputationRequirement
{
    [SerializeField]
    private Faction faction;

    [SerializeField]
    private FavourReputationComparison
        comparison =
            FavourReputationComparison
                .AtLeast;

    [SerializeField]
    [Min(0)]
    private int requiredLevel;

    [SerializeField]
    [Tooltip(
        "Om aktiverad måste factionen även vara upptäckt."
    )]
    private bool requireDiscovered = true;

    public Faction Faction =>
        faction;

    public FavourReputationComparison
        Comparison =>
            comparison;

    public int RequiredLevel =>
        Mathf.Max(
            0,
            requiredLevel
        );

    public bool RequireDiscovered =>
        requireDiscovered;
}

/// <summary>
/// Ett item som måste finnas i inventoryt för att favouren
/// ska vara tillgänglig eller kunna lämnas in.
/// </summary>
[Serializable]
public sealed class FavourItemRequirement
{
    [SerializeField]
    private ItemData item;

    [SerializeField]
    [Min(1)]
    private int amount = 1;

    [SerializeField]
    [Tooltip(
        "Tar bort mängden atomärt när favouren slutförs."
    )]
    private bool consumeOnCompletion;

    public ItemData Item =>
        item;

    public int Amount =>
        Mathf.Max(
            1,
            amount
        );

    public bool ConsumeOnCompletion =>
        consumeOnCompletion;
}
