using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Reputation Levels"
)]
public sealed class ReputationLevelDefinition :
    ScriptableObject
{
    [Min(1)]
    public int baseXPRequired = 100;

    [Min(1f)]
    public float scalingMultiplier = 1.5f;

    [Min(1)]
    public int maxLevel = 9;

    [System.Serializable]
    public sealed class ReputationTier
    {
        public string tierName;

        [TextArea]
        public string description;

        public AudioClip rankReachedSound;
    }

    public List<ReputationTier> tiers =
        new();

    public int GetXPRequired(
        int level)
    {
        int safeLevel =
            Mathf.Max(
                1,
                level
            );

        return Mathf.Max(
            1,
            Mathf.RoundToInt(
                baseXPRequired *
                Mathf.Pow(
                    scalingMultiplier,
                    safeLevel - 1
                )
            )
        );
    }

    public string GetTierName(
        int level)
    {
        if (tiers == null ||
            tiers.Count == 0)
        {
            return $"Rank {Mathf.Max(1, level)}";
        }

        int index =
            Mathf.Clamp(
                level - 1,
                0,
                tiers.Count - 1
            );

        ReputationTier tier =
            tiers[index];

        if (tier == null ||
            string.IsNullOrWhiteSpace(
                tier.tierName))
        {
            return $"Rank {Mathf.Max(1, level)}";
        }

        return tier.tierName;
    }

    public AudioClip GetTierSound(
        int level)
    {
        if (tiers == null ||
            tiers.Count == 0)
        {
            return null;
        }

        int index =
            Mathf.Clamp(
                level - 1,
                0,
                tiers.Count - 1
            );

        return tiers[index]
            ?.rankReachedSound;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        baseXPRequired =
            Mathf.Max(
                1,
                baseXPRequired
            );

        scalingMultiplier =
            Mathf.Max(
                1f,
                scalingMultiplier
            );

        maxLevel =
            Mathf.Max(
                1,
                maxLevel
            );
    }
#endif
}