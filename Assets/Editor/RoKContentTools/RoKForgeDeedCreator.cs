#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

internal static class RoKForgeDeedCreator
{
    private const string DeedFolder = "Assets/12.Deeds/Resources/Deeds";
    private const string ObjectiveFolder = "Assets/12.Deeds/Objectives";

    internal sealed class ObjectiveDraftState
    {
        internal readonly RoKForgeDraftSession Session = new();

        internal Type SelectedType;
        internal string AssetName;

        internal void Reset()
        {
            Session.Cancel();
            SelectedType = null;
            AssetName = null;
        }
    }

    internal static VisualElement Create(
        ScrollView workspace,
        DeedDefinition draft,
        ObjectiveDraftState objectiveState,
        Action<DeedDefinition> onCreated,
        Action onCancel,
        Action refreshLibrary)
    {
        VisualElement deedColumn = new();
        deedColumn.Add(new InspectorElement(draft));

        HelpBox rewardError = new(string.Empty, HelpBoxMessageType.Error);
        rewardError.style.display = DisplayStyle.None;
        deedColumn.Add(rewardError);

        HelpBox saveError = new(string.Empty, HelpBoxMessageType.Error);
        saveError.style.display = DisplayStyle.None;
        deedColumn.Add(saveError);

        VisualElement actions = new();
        actions.style.flexDirection = FlexDirection.Row;

        Button createButton = new() { text = "Create Deed" };

        bool RefreshRewardState()
        {
            if (draft == null)
            {
                createButton.SetEnabled(false);
                return false;
            }

            List<string> issues = RoKForgeDeedReview.GetRewardIssues(draft);
            bool canCreate = issues.Count == 0;

            rewardError.text = string.Join("\n", issues);
            rewardError.style.display = canCreate
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            createButton.SetEnabled(canCreate);
            return canCreate;
        }

        createButton.clicked += () =>
        {
            saveError.style.display = DisplayStyle.None;

            if (!RefreshRewardState())
                return;

            DeedDefinition created;

            try
            {
                created = (DeedDefinition)RoKForgeDraftSession.SaveCopy(
                    draft, draft.DisplayName, DeedFolder, ValidateCreatedDeed);
            }
            catch (Exception exception)
            {
                saveError.text = exception.Message;
                saveError.style.display = DisplayStyle.Flex;
                return;
            }

            onCreated(created);
        };

        actions.Add(new Button(onCancel) { text = "Cancel" });
        actions.Add(createButton);
        deedColumn.Add(actions);

        SerializedObject trackedDraft = new(draft);
        actions.TrackSerializedObjectValue(trackedDraft, _ => RefreshRewardState());

        actions.RegisterCallback<DetachFromPanelEvent>(evt =>
        {
            if (evt.target != actions)
                return;

            actions.Unbind();
            trackedDraft.Dispose();
        });

        RefreshRewardState();

        VisualElement objectiveColumn =
            CreateObjectiveColumn(objectiveState, refreshLibrary);

        return RoKForgeFormLayout.CreateColumns(
            workspace, deedColumn, objectiveColumn);
    }

    private static VisualElement CreateObjectiveColumn(
        ObjectiveDraftState state,
        Action refreshLibrary)
    {
        VisualElement column = new();

        Label heading = new("CREATE REUSABLE OBJECTIVE");
        heading.style.unityFontStyleAndWeight = FontStyle.Bold;
        column.Add(heading);

        List<Type> types = new();

        foreach (Type type in TypeCache.GetTypesDerivedFrom<DeedObjectiveDefinition>())
        {
            if (!type.IsAbstract && !type.ContainsGenericParameters)
                types.Add(type);
        }

        types.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(
            GetObjectiveTypeLabel(left), GetObjectiveTypeLabel(right)));

        if (types.Count == 0)
        {
            column.Add(new HelpBox(
                "No deed objective types found.", HelpBoxMessageType.Info));
            return column;
        }

        int selectedIndex = Mathf.Max(0, types.IndexOf(state.SelectedType));

        PopupField<Type> typeField = new(
            "Objective Type",
            types,
            selectedIndex,
            GetObjectiveTypeLabel,
            GetObjectiveTypeLabel);

        state.SelectedType = typeField.value;
        column.Add(typeField);

        VisualElement fields = new();

        HelpBox status = new(string.Empty, HelpBoxMessageType.Info);
        status.style.display = DisplayStyle.None;

        void DrawObjectiveFields()
        {
            fields.Clear();

            if (state.Session.Draft is not DeedObjectiveDefinition objective)
                return;

            TextField assetName = new("Asset Name")
            {
                value = state.AssetName ?? string.Empty
            };

            assetName.RegisterValueChangedCallback(evt =>
                state.AssetName = evt.newValue);

            fields.Add(assetName);
            fields.Add(new InspectorElement(objective));

            fields.Add(new Button(() =>
            {
                DeedObjectiveDefinition created;

                try
                {
                    created = (DeedObjectiveDefinition)
                        state.Session.Commit(state.AssetName, ObjectiveFolder);
                }
                catch (Exception exception)
                {
                    status.messageType = HelpBoxMessageType.Error;
                    status.text = exception.Message;
                    status.style.display = DisplayStyle.Flex;
                    return;
                }

                state.AssetName = null;
                fields.Clear();
                refreshLibrary();
                EditorGUIUtility.PingObject(created);

                status.messageType = HelpBoxMessageType.Info;
                status.text =
                    $"Saved {created.name}. Select it in the Deed's Objective field or on a rank.";
                status.style.display = DisplayStyle.Flex;
            })
            { text = "Save Objective Asset" });
        }

        typeField.RegisterValueChangedCallback(evt =>
        {
            fields.Clear();
            state.Session.Cancel();
            state.SelectedType = evt.newValue;
            state.AssetName = null;
            status.style.display = DisplayStyle.None;
        });

        Button configure = new(() =>
        {
            status.style.display = DisplayStyle.None;

            if (state.Session.Draft == null)
            {
                state.Session.Begin(typeField.value);
                state.AssetName = GetObjectiveTypeLabel(typeField.value);
            }

            DrawObjectiveFields();
        })
        { text = "Configure Objective" };

        column.Add(configure);
        column.Add(fields);
        column.Add(status);

        DrawObjectiveFields();
        return column;
    }

    private static string GetObjectiveTypeLabel(Type type)
    {
        const string suffix = "DeedObjectiveDefinition";
        string name = type.Name;

        if (name.EndsWith(suffix, StringComparison.Ordinal))
            name = name.Substring(0, name.Length - suffix.Length);

        return ObjectNames.NicifyVariableName(name);
    }

    private static void ValidateCreatedDeed(ScriptableObject asset)
    {
        DeedDefinition deed = (DeedDefinition)asset;
        string path = AssetDatabase.GetAssetPath(deed);
        string guid = AssetDatabase.AssetPathToGUID(path);

        if (string.IsNullOrWhiteSpace(guid))
            throw new InvalidOperationException("Unity did not assign a Deed asset GUID.");

        if (deed.PersistentId != guid)
        {
            using SerializedObject serialized = new(deed);
            serialized.FindProperty("persistentId").stringValue = guid;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        if (!deed.IsValid)
        {
            throw new InvalidOperationException(
                "Add at least one rank and assign the required objectives. " +
                "When using one shared objective, rank thresholds must increase.");
        }
    }
}

#endif