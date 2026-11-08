#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

internal static class RoKForgeFavourEditor
{
    private const string DialogueFolder = "Assets/ContentStudio/Favours/Dialogue";

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

        bool isSaved = AssetDatabase.Contains(favour);

        if (isSaved)
            right.Add(RoKForgeFavourObjectiveCreator.Create(favour, openAsset));
        else
            right.Add(new Label("Save the Favour to create and attach new objectives."));

        AddDialogueField<FavourDialogueSet>(
    right, serialized, favour, "dialogueSet",
    "Create & Attach Dialogue Set", "Dialogue", isSaved, openAsset);

        AddDialogueField<FavourDialogueSet>(
            right, serialized, favour, "completionDialogueSet",
            "Create & Attach Completion Dialogue Set", "Completion Dialogue",
            isSaved, openAsset);

        AddDialogueField<NPCDialogueTable>(
            right, serialized, favour, "offerAmbientDialogue",
            "Create & Attach Offer Dialogue Table", "Offer Dialogue",
            isSaved, openAsset);

        if (!isSaved)
            right.Add(new Label("Save the Favour to create new dialogue sets."));

        AddFields(right, serialized,
            "experienceReward", "minimumCurrencyReward", "maximumCurrencyReward",
            "reputationRewards", "itemRewards", "abilityRewards", "rewardChoiceGroups",
            "followUps");

        VisualElement layout = RoKForgeFormLayout.CreateColumns(workspace, left, right);
        layout.Bind(serialized);

        if (openAsset != null)
            RoKForgeLinkedField.EnableNestedNavigation(layout, favour, openAsset);

        return layout;
    }

    private static void AddDialogueField<T>(
        VisualElement column,
        SerializedObject serialized,
        FavourData favour,
        string propertyName,
        string buttonText,
        string nameSuffix,
        bool isSaved,
        Action<ScriptableObject> openAsset) where T : ScriptableObject
    {
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property == null)
            return;

        column.Add(new PropertyField(property));

        if (!isSaved)
            return;

        Button button = new(() =>
        {
            EditorApplication.delayCall += () =>
            {
                if (favour == null || !AssetDatabase.Contains(favour) ||
                    EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                ScriptableObject created;

                try
                {
                    created = RoKForgeAssetAttachment.CreateAndAttach(
                        favour,
                        propertyName,
                        typeof(T),
                        $"{favour.DisplayName} {nameSuffix}",
                        DialogueFolder,
                        "Create & Attach Favour Dialogue");
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    EditorUtility.DisplayDialog(
                        "Dialogue Creation Failed", exception.Message, "OK");
                    return;
                }

                openAsset?.Invoke(created);
            };
        })
        {
            text = buttonText
        };

        button.style.marginBottom = 6;

        void UpdateButton(SerializedProperty current)
        {
            bool hasDialogue = current.objectReferenceValue != null;
            button.SetEnabled(!hasDialogue);
            button.tooltip = hasDialogue
                ? "Double-click the linked dialogue set to open it."
                : "Create and open a dialogue set for this field.";
        }

        column.Add(button);
        UpdateButton(property);
        button.TrackPropertyValue(property, UpdateButton);
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