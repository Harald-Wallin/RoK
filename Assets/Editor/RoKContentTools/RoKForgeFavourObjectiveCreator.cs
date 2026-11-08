#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

internal static class RoKForgeFavourObjectiveCreator
{
    private const string ObjectiveFolder =
        "Assets/ContentStudio/Favours/Objectives";

    internal static VisualElement Create(
        FavourData favour,
        Action<ScriptableObject> openAsset)
    {
        Foldout panel = new()
        {
            text = "Create & Attach Objective",
            value = true
        };

        panel.style.marginTop = 12;
        panel.style.marginBottom = 12;

        List<Type> types = new();

        foreach (Type type in TypeCache.GetTypesDerivedFrom<FavourObjectiveData>())
        {
            if (!type.IsAbstract && !type.ContainsGenericParameters)
                types.Add(type);
        }

        if (types.Count == 0)
        {
            panel.Add(new Label("No objective types found."));
            return panel;
        }

        types.Sort((left, right) =>
            string.CompareOrdinal(left.Name, right.Name));

        List<string> labels = new();

        foreach (Type type in types)
        {
            const string suffix = "ObjectiveData";
            string name = type.Name;

            if (name.EndsWith(suffix, StringComparison.Ordinal))
                name = name.Substring(0, name.Length - suffix.Length);

            labels.Add(ObjectNames.NicifyVariableName(name));
        }

        DropdownField typeField = new("Objective Type")
        {
            choices = labels,
            index = 0
        };

        TextField nameField = new("Asset Name");

        Button createButton = new(() =>
        {
            int index = typeField.index;
            string assetName = nameField.value?.Trim();

            if (index < 0 || index >= types.Count ||
                string.IsNullOrWhiteSpace(assetName))
            {
                return;
            }

            Type objectiveType = types[index];

            EditorApplication.delayCall += () =>
            {
                if (favour == null ||
                    !AssetDatabase.Contains(favour) ||
                    EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                ScriptableObject created;

                try
                {
                    created = RoKForgeAssetAttachment.CreateAndAttach(
                     favour,
                     "objectives",
                     objectiveType,
                     assetName,
                     ObjectiveFolder,
                     "Create & Attach Favour Objective");
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);

                    EditorUtility.DisplayDialog(
                        "Objective Creation Failed",
                        exception.Message,
                        "OK");
                    return;
                }

                nameField.value = string.Empty;
                openAsset?.Invoke(created);
            };
        })
        {
            text = "Create & Attach"
        };

        createButton.SetEnabled(false);

        nameField.RegisterValueChangedCallback(evt =>
            createButton.SetEnabled(!string.IsNullOrWhiteSpace(evt.newValue)));

        panel.Add(typeField);
        panel.Add(nameField);
        panel.Add(createButton);

        return panel;
    }

    private static FavourObjectiveData CreateAndAttach(
        FavourData favour,
        Type objectiveType,
        string assetName)
    {
        ScriptableObject temporary = null;
        string createdPath = null;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create & Attach Favour Objective");

        try
        {
            temporary = RoKForgeDraftSession.CreateTemporary(objectiveType);

            FavourObjectiveData created =
                (FavourObjectiveData)RoKForgeDraftSession.SaveCopy(
                    temporary,
                    assetName,
                    ObjectiveFolder);

            createdPath = AssetDatabase.GetAssetPath(created);

            SerializedObject serialized = new(favour);
            SerializedProperty objectives = serialized.FindProperty("objectives");

            if (objectives == null || !objectives.isArray)
                throw new InvalidOperationException("The Favour has no objectives list.");

            int index = objectives.arraySize;
            objectives.arraySize++;
            objectives.GetArrayElementAtIndex(index).objectReferenceValue = created;

            if (!serialized.ApplyModifiedProperties())
                throw new InvalidOperationException("The objective could not be attached.");

            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);

            return created;
        }
        catch
        {
            Undo.RevertAllDownToGroup(undoGroup);

            if (!string.IsNullOrEmpty(createdPath) &&
                AssetDatabase.LoadMainAssetAtPath(createdPath) != null)
            {
                AssetDatabase.DeleteAsset(createdPath);
            }

            throw;
        }
        finally
        {
            RoKForgeDraftSession.DiscardTemporary(temporary);
        }
    }
}

#endif
