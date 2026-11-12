using System;
using System.Collections.Generic;
using UnityEngine;

public enum DeedVisibility
{
    Visible,
    Hidden,
    Secret
}


public enum DeedRewardKind
{
    Title = 0,
    TrainableAbility = 1,
    Item = 2,
    Cosmetic = 3,

    // Numeric rewards
    Currency = 4,
    Experience = 5,
    GeneralTrait = 6
}


/// <summary>
/// V1: reward metadata for UI and later delivery.
/// This foundation does NOT grant rewards automatically.
/// </summary>
[Serializable]
public sealed class DeedRewardPreview
{
    [SerializeField] private DeedRewardKind kind;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private AbilityData ability;
    [SerializeField] private ItemData item;
    [SerializeField] private GeneralTraitDefinition generalTrait;
    [SerializeField]
    [Min(0)]
    private int amount;

    public DeedRewardKind Kind => kind;
    public AbilityData Ability => ability;
    public ItemData Item => item;
    public GeneralTraitDefinition GeneralTrait => generalTrait;

    public Sprite Icon =>
        icon != null
            ? icon
            : kind == DeedRewardKind.GeneralTrait && generalTrait != null
                ? generalTrait.Icon
                : null;

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(displayName) ? displayName :
        kind == DeedRewardKind.GeneralTrait && generalTrait != null ? generalTrait.DisplayName :
        kind == DeedRewardKind.TrainableAbility && ability != null ? ability.abilityName :
        kind == DeedRewardKind.Item && item != null ? item.DisplayName :
        kind.ToString();
    public int Amount => Mathf.Max(0, amount);
}

[Serializable]
public sealed class DeedRankDefinition
{
    [SerializeField, HideInInspector] private string rankId;
    [SerializeField] private string rankLabel = "I";
    [SerializeField, Min(1)] private int requiredTotalProgress = 10;
    [SerializeField] private string shortDescription;
    [SerializeField, TextArea(3, 7)] private string longDescription;
    [SerializeField] private Sprite imageOverride;
    [SerializeField] private List<DeedRewardPreview> rewards = new();

    public string RankId => rankId;
    public string RankLabel => rankLabel;
    public int RequiredTotalProgress => Mathf.Max(1, requiredTotalProgress);
    public string ShortDescription => shortDescription;
    public string LongDescription => longDescription;
    public Sprite ImageOverride => imageOverride;
    public IReadOnlyList<DeedRewardPreview> Rewards => rewards;

#if UNITY_EDITOR
    internal void EnsureIdentity(HashSet<string> used)
    {
        if (string.IsNullOrWhiteSpace(rankId) || !used.Add(rankId))
        {
            rankId = Guid.NewGuid().ToString("N");
            used.Add(rankId);
        }
    }
#endif
}

[CreateAssetMenu(menuName = "RöK/Deeds/Deed")]
public sealed class DeedDefinition : ScriptableObject
{
    [SerializeField, HideInInspector] private string persistentId;

    [Header("Identity")]
    [SerializeField] private string displayName;
    [SerializeField] private DeedCategoryDefinition category;
    [SerializeField] private DeedVisibility visibility = DeedVisibility.Visible;
    [SerializeField] private bool revealHiddenOnFirstProgress = true;
    [SerializeField] private Sprite image;
    [SerializeField] private string shortDescription;
    [SerializeField, TextArea(3, 7)] private string longDescription;

    [Header("Objective")]
    [SerializeField] private DeedObjectiveDefinition objective;

    [Header("Cumulative Ranks (ascending thresholds)")]
    [SerializeField] private List<DeedRankDefinition> ranks = new();

    public string PersistentId => persistentId;
    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public DeedCategoryDefinition Category => category;
    public DeedVisibility Visibility => visibility;
    public bool RevealHiddenOnFirstProgress => revealHiddenOnFirstProgress;
    public Sprite Image => image;
    public string ShortDescription => shortDescription;
    public string LongDescription => longDescription;
    public DeedObjectiveDefinition Objective => objective;
    public IReadOnlyList<DeedRankDefinition> Ranks => ranks;

    public bool IsValid
    {
        get
        {
            if (string.IsNullOrWhiteSpace(persistentId) ||
                objective == null || ranks == null || ranks.Count == 0)
                return false;

            int previous = 0;
            HashSet<string> seen = new();
            foreach (DeedRankDefinition rank in ranks)
            {
                if (rank == null ||
                    string.IsNullOrWhiteSpace(rank.RankId) ||
                    !seen.Add(rank.RankId) ||
                    rank.RequiredTotalProgress <= previous)
                    return false;
                previous = rank.RequiredTotalProgress;
            }
            return true;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Unity's asset GUID is stable on rename/move and unique on duplication.
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
        if (!string.IsNullOrWhiteSpace(assetPath))
        {
            string assetGuid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
            if (!string.IsNullOrWhiteSpace(assetGuid))
                persistentId = assetGuid;
        }

        if (ranks == null)
            ranks = new List<DeedRankDefinition>();

        HashSet<string> used = new();
        foreach (DeedRankDefinition rank in ranks)
            rank?.EnsureIdentity(used);

        if (ranks.Count > 0 && !IsValid)
        {
            Debug.LogWarning(
                $"Deed '{name}': assign an objective and ensure strictly " +
                "increasing rank thresholds and valid IDs.", this);
        }
    }
#endif
}
