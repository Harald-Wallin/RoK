#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal static class RoKForgeDeedReview
{
    internal static List<ScriptableObject> GetLinks(DeedDefinition deed)
    {
        List<ScriptableObject> links = new();

        AddLink(links, deed.Category);

        using SerializedObject serialized = new(deed);
        SerializedProperty useRankObjectives =
            serialized.FindProperty("useRankObjectives");

        if (useRankObjectives != null && useRankObjectives.boolValue)
        {
            SerializedProperty ranks = serialized.FindProperty("ranks");

            for (int i = 0; i < ranks.arraySize; i++)
            {
                AddRankObjectiveLinks(ranks.GetArrayElementAtIndex(i), links);
            }
        }
        else
        {
            AddLink(links, deed.Objective);
        }

        if (deed.Ranks == null)
            return links;

        foreach (DeedRankDefinition rank in deed.Ranks)
        {
            if (rank?.Rewards == null)
                continue;

            foreach (DeedRewardPreview reward in rank.Rewards)
            {
                ScriptableObject target = reward?.Kind switch
                {
                    DeedRewardKind.TrainableAbility => reward.Ability,
                    DeedRewardKind.GeneralTrait => reward.GeneralTrait,
                    DeedRewardKind.Item => reward.Item,
                    _ => null
                };

                AddLink(links, target);
            }
        }

        return links;
    }

    internal static List<string> Validate(DeedDefinition deed)
    {
        List<string> issues = new();

        string path = AssetDatabase.GetAssetPath(deed);

        if (path.IndexOf("/Resources/Deeds/", StringComparison.Ordinal) < 0)
        {
            issues.Add(
                "FEL: Spelet laddar deeds från Resources/Deeds. " +
                "Flytta detta asset till den mappen eller en undermapp.");
        }

        if (!deed.IsValid)
        {
            issues.Add(
                "FEL: Deeden kan inte laddas. Kontrollera att den har minst en rank " +
                "och att rätt objectives är ifyllda. Om rankerna delar objective " +
                "måste Required Total Progress stiga för varje rank.");
        }

        foreach (string issue in GetRewardIssues(deed))
        {
            issues.Add($"FEL: {issue}");
        }

        return issues;
    }

    internal static List<string> GetRewardIssues(DeedDefinition deed)
    {
        List<string> issues = new();

        if (deed.Ranks == null)
            return issues;

        using SerializedObject serialized = new(deed);

        for (int rankIndex = 0; rankIndex < deed.Ranks.Count; rankIndex++)
        {
            IReadOnlyList<DeedRewardPreview> rewards =
                deed.Ranks[rankIndex]?.Rewards;

            if (rewards == null)
                continue;

            for (int rewardIndex = 0; rewardIndex < rewards.Count; rewardIndex++)
            {
                DeedRewardPreview reward = rewards[rewardIndex];

                string problem = reward?.Kind switch
                {
                    null => "Reward-posten är tom.",
                    DeedRewardKind.TrainableAbility when reward.Ability == null =>
                        "Välj en Ability.",
                    DeedRewardKind.GeneralTrait when reward.GeneralTrait == null =>
                        "Välj ett General Trait.",
                    DeedRewardKind.Item when reward.Item == null =>
                        "Välj ett Item.",
                    DeedRewardKind.Currency when reward.Amount <= 0 =>
                        "Ange ett Amount större än 0 för Currency.",
                    DeedRewardKind.Experience when reward.Amount <= 0 =>
                        "Ange ett Amount större än 0 för Experience.",
                    _ => null
                };

                if (reward != null &&
                    (reward.Kind == DeedRewardKind.Title ||
                     reward.Kind == DeedRewardKind.Cosmetic))
                {
                    string fieldPath =
                        $"ranks.Array.data[{rankIndex}].rewards.Array.data[{rewardIndex}].displayName";

                    SerializedProperty displayName = serialized.FindProperty(fieldPath);

                    if (string.IsNullOrWhiteSpace(displayName?.stringValue))
                        problem = $"Fyll i Display Name för {reward.Kind}.";
                }

                if (problem != null)
                {
                    issues.Add(
                        $"Rank {rankIndex + 1}, reward {rewardIndex + 1}. {problem}");
                }
            }
        }

        return issues;
    }

    private static void AddRankObjectiveLinks(
        SerializedProperty rank,
        List<ScriptableObject> links)
    {
        using SerializedProperty field = rank.Copy();

        // Walk the direct rank fields and skip nested data such as Rewards.
        bool hasField = field.Next(true);

        while (hasField && field.depth > rank.depth)
        {
            if (field.propertyType == SerializedPropertyType.ObjectReference &&
                field.objectReferenceValue is DeedObjectiveDefinition objective)
            {
                AddLink(links, objective);
            }

            hasField = field.Next(false);
        }
    }

    private static void AddLink(
        List<ScriptableObject> links,
        ScriptableObject asset)
    {
        if (asset != null && !links.Contains(asset))
            links.Add(asset);
    }
}

#endif
