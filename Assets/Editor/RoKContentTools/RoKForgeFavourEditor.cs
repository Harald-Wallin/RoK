#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

internal static class RoKForgeFavourEditor
{
    internal static VisualElement Create(
        ScrollView workspace,
        FavourData favour,
        Action<ScriptableObject> openAsset = null)
    {
        SerializedObject serialized = new(favour);
        VisualElement left = new();
        VisualElement right = new();

        left.Add(new Label("FAVOUR SETTINGS"));
        AddFields(left, serialized,
            "displayName", "description", "category",
            "activationPolicy", "completionPolicy", "isCourier", "completionTarget",
            "minimumLevel", "minimumCurrency",
            "reputationRequirements", "itemRequirements",
            "requiredCompletedFavours", "forbiddenCompletedFavours",
            "failureSettings", "repeatSettings", "requireSourceItemWhileActive");

        AddFields(right, serialized, "objectives");

        if (AssetDatabase.Contains(favour))
        {
            right.Add(RoKForgeFavourObjectiveCreator.Create(favour, openAsset));
        }
        else
        {
            right.Add(new Label("Save the Favour to create and attach new objectives."));
        }

        AddFields(right, serialized,
            "dialogueSet", "completionDialogueSet",
            "experienceReward", "minimumCurrencyReward", "maximumCurrencyReward",
            "reputationRewards", "itemRewards", "abilityRewards", "rewardChoiceGroups",
            "followUps");

        VisualElement layout = RoKForgeFormLayout.CreateColumns(workspace, left, right);
        layout.Bind(serialized);

        if (openAsset != null)
            RoKForgeLinkedField.EnableNestedNavigation(layout, favour, openAsset);

        return layout;
    }

    private static void AddFields(
        VisualElement column,
        SerializedObject serialized,
        params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);

            if (property != null)
                column.Add(new PropertyField(property));
        }
    }
}

#endif
