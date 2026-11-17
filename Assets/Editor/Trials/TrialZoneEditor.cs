#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TrialZone))]
public sealed class TrialZoneEditor : Editor
{
    private readonly HashSet<int> openStages = new();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        TrialZone zone = (TrialZone)target;
        DrawRewardPoint(zone);

        TrialSpawnPoint[] markers =
            zone.GetComponentsInChildren<TrialSpawnPoint>(true);

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Stage Placement", EditorStyles.boldLabel);

        for (int index = 0; index < zone.Stages.Count; index++)
        {
            TrialStageDefinition stage = zone.Stages[index];

            if (stage == null)
                continue;

            bool open = EditorGUILayout.Foldout(
                openStages.Contains(index),
                $"{stage.StageLabel}  {stage.name}",
                true);

            if (!open)
            {
                openStages.Remove(index);
                continue;
            }

            openStages.Add(index);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawStage(zone, stage, markers);
            }
        }
    }

    private void DrawRewardPoint(TrialZone zone)
    {
        serializedObject.Update();

        SerializedProperty pointProperty =
            serializedObject.FindProperty("rewards.rewardContainerPoint");

        EditorGUILayout.Space(8);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(
                pointProperty, new GUIContent("Reward Container Point"));

            serializedObject.ApplyModifiedProperties();
            Transform point = pointProperty.objectReferenceValue as Transform;

            if (point == null)
            {
                if (!GUILayout.Button("Create Reward Point"))
                    return;

                const string undoName = "Create Trial Reward Point";

                GameObject marker = new("RewardContainerPoint");
                Undo.RegisterCreatedObjectUndo(marker, undoName);
                Undo.SetTransformParent(marker.transform, zone.transform, undoName);
                Undo.RecordObject(marker.transform, undoName);

                marker.transform.localPosition = Vector3.zero;
                marker.transform.localRotation = Quaternion.identity;

                Undo.AddComponent<TrialRewardContainerPoint>(marker);

                serializedObject.Update();
                pointProperty =
                    serializedObject.FindProperty("rewards.rewardContainerPoint");
                pointProperty.objectReferenceValue = marker.transform;
                serializedObject.ApplyModifiedProperties();

                point = marker.transform;
            }
            else if (!GUILayout.Button("Place", GUILayout.Width(52)))
            {
                return;
            }

            Selection.activeGameObject = point.gameObject;
            Tools.current = Tool.Move;
            SceneView.RepaintAll();
            GUIUtility.ExitGUI();
        }
    }

    private static void DrawStage(
        TrialZone zone,
        TrialStageDefinition stage,
        TrialSpawnPoint[] markers)
    {
        using SerializedObject serializedStage = new(stage);
        serializedStage.Update();

        EditorGUILayout.PropertyField(
            serializedStage.FindProperty("objectives"), includeChildren: true);

        serializedStage.ApplyModifiedProperties();

        if (stage.Objectives.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "Add an Objective above to place its spawns here. " +
                "This stage currently uses its Legacy Setup.",
                MessageType.Info);
            return;
        }

        for (int index = 0; index < stage.Objectives.Length; index++)
        {
            TrialObjectiveDefinition objective = stage.Objectives[index];

            if (objective == null || string.IsNullOrWhiteSpace(objective.Id))
                continue;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    $"{objective.Type} · {objective.DisplayName} ({objective.RequiredCount})",
                    EditorStyles.boldLabel);

                GameObject defaultPrefab = DrawDefaultPrefab(stage, index);

                bool canAdd = IsPrefab(defaultPrefab);

                if (!canAdd)
                {
                    EditorGUILayout.HelpBox(
                        "Choose a Spawn Prefab from Project. New spawn rows will use it automatically.",
                        MessageType.Info);
                }

                foreach (TrialSpawnPoint marker in markers)
                {
                    if (!BelongsTo(marker, zone, stage, objective.Id))
                        continue;

                    DrawSpawnRow(marker);
                }

                using (new EditorGUI.DisabledScope(!canAdd))
                {
                    if (GUILayout.Button("Repair Empty/Invalid Rows"))
                        RepairRows(markers, zone, stage, objective.Id, defaultPrefab);

                    if (GUILayout.Button("+ Spawn"))
                    {
                        AddSpawn(zone, stage, objective, defaultPrefab);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }
    }

    private static GameObject DrawDefaultPrefab(
        TrialStageDefinition stage,
        int objectiveIndex)
    {
        using SerializedObject serializedStage = new(stage);
        serializedStage.Update();

        SerializedProperty property = serializedStage
            .FindProperty("objectives")
            .GetArrayElementAtIndex(objectiveIndex)
            .FindPropertyRelative("spawnPrefab");

        GameObject current = property.objectReferenceValue as GameObject;

        EditorGUI.BeginChangeCheck();

        GameObject selected = EditorGUILayout.ObjectField(
            new GUIContent("Spawn Prefab", "Prefab copied into new spawn rows."),
            current,
            typeof(GameObject),
            allowSceneObjects: false) as GameObject;

        if (!EditorGUI.EndChangeCheck())
            return current;

        if (selected != null && !IsPrefab(selected))
        {
            Debug.LogWarning("Select a prefab root from Project.", stage);
            return current;
        }

        property.objectReferenceValue = selected;
        serializedStage.ApplyModifiedProperties();
        return selected;
    }

    private static bool BelongsTo(
        TrialSpawnPoint marker,
        TrialZone zone,
        TrialStageDefinition stage,
        string objectiveId)
    {
        return marker != null &&
               marker.Zone == zone &&
               marker.Stage == stage &&
               marker.ObjectiveId == objectiveId;
    }

    internal static bool IsPrefab(GameObject prefab)
    {
        return prefab != null &&
               PrefabUtility.IsPartOfPrefabAsset(prefab) &&
               prefab.transform.parent == null &&
               prefab.GetComponent<TrialSpawnPoint>() == null;
    }

    internal static void DrawPrefabField(TrialSpawnPoint marker)
    {
        GameObject current = marker.Prefab;

        EditorGUI.BeginChangeCheck();

        GameObject selected = EditorGUILayout.ObjectField(
            new GUIContent("Prefab", "The prefab spawned at this marker."),
            current,
            typeof(GameObject),
            allowSceneObjects: false) as GameObject;

        if (!EditorGUI.EndChangeCheck())
            return;

        if (selected != null && !IsPrefab(selected))
        {
            Debug.LogWarning("Select a prefab root from Project.", marker);
            return;
        }

        WritePrefab(marker, selected);
    }

    private static void DrawSpawnRow(TrialSpawnPoint marker)
    {
        EditorGUILayout.LabelField(marker.name, EditorStyles.miniLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawPrefabField(marker);

            if (GUILayout.Button("Place", GUILayout.Width(52)))
            {
                Selection.activeGameObject = marker.gameObject;
                Tools.current = Tool.Move;
                SceneView.RepaintAll();
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("−", GUILayout.Width(26)))
            {
                Undo.DestroyObjectImmediate(marker.gameObject);
                GUIUtility.ExitGUI();
            }
        }

        if (marker.Prefab != null && !IsPrefab(marker.Prefab))
        {
            EditorGUILayout.HelpBox(
                "This row references a scene object or marker. Choose the objective's " +
                "Spawn Prefab and click Repair Empty/Invalid Rows.",
                MessageType.Error);
        }
    }

    private static void WritePrefab(TrialSpawnPoint marker, GameObject prefab)
    {
        using SerializedObject serializedMarker = new(marker);
        serializedMarker.Update();
        serializedMarker.FindProperty("prefab").objectReferenceValue = prefab;
        serializedMarker.ApplyModifiedProperties();
    }

    private static void RepairRows(
        TrialSpawnPoint[] markers,
        TrialZone zone,
        TrialStageDefinition stage,
        string objectiveId,
        GameObject prefab)
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Repair Trial Spawn Prefabs");

        foreach (TrialSpawnPoint marker in markers)
        {
            if (BelongsTo(marker, zone, stage, objectiveId) &&
                !IsPrefab(marker.Prefab))
            {
                WritePrefab(marker, prefab);
            }
        }

        Undo.CollapseUndoOperations(group);
    }

    private static void AddSpawn(
        TrialZone zone,
        TrialStageDefinition stage,
        TrialObjectiveDefinition objective,
        GameObject prefab)
    {
        const string undoName = "Add Trial Spawn";

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(undoName);

        GameObject markerObject =
            new($"Spawn {stage.StageLabel} · {objective.DisplayName}");

        Undo.RegisterCreatedObjectUndo(markerObject, undoName);
        Undo.SetTransformParent(markerObject.transform, zone.transform, undoName);
        Undo.RecordObject(markerObject.transform, undoName);

        markerObject.transform.localPosition = Vector3.zero;
        markerObject.transform.localRotation = Quaternion.identity;

        TrialSpawnPoint marker = Undo.AddComponent<TrialSpawnPoint>(markerObject);
        Undo.RecordObject(marker, undoName);
        marker.AssignObjective(stage, objective.Id);

        WritePrefab(marker, prefab);
        Undo.CollapseUndoOperations(group);
        SceneView.RepaintAll();
    }
}

[CustomEditor(typeof(TrialSpawnPoint))]
internal sealed class TrialSpawnPointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TrialSpawnPoint marker = (TrialSpawnPoint)target;
        TrialZoneEditor.DrawPrefabField(marker);
        TrialZoneOutline.DrawBackButton(marker.Zone);
    }

    private void OnSceneGUI()
    {
        TrialZoneOutline.Draw(((TrialSpawnPoint)target).Zone);
    }
}

[CustomEditor(typeof(TrialRewardContainerPoint))]
internal sealed class TrialRewardContainerPointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TrialZoneOutline.DrawBackButton(((TrialRewardContainerPoint)target).Zone);
    }

    private void OnSceneGUI()
    {
        TrialZoneOutline.Draw(((TrialRewardContainerPoint)target).Zone);
    }
}

internal static class TrialZoneOutline
{
    internal static void DrawBackButton(TrialZone zone)
    {
        if (zone != null && GUILayout.Button("Back to Trial Zone"))
            Selection.activeGameObject = zone.gameObject;
    }

    internal static void Draw(TrialZone zone)
    {
        if (Event.current.type != EventType.Repaint ||
            zone == null ||
            !zone.TryGetComponent(out PolygonCollider2D polygon))
        {
            return;
        }

        using (new Handles.DrawingScope(
            new Color(0.3f, 1f, 0.5f, 1f),
            polygon.transform.localToWorldMatrix))
        {
            for (int pathIndex = 0; pathIndex < polygon.pathCount; pathIndex++)
            {
                Vector2[] path = polygon.GetPath(pathIndex);

                if (path.Length < 2)
                    continue;

                Vector3[] outline = new Vector3[path.Length + 1];

                for (int i = 0; i < path.Length; i++)
                    outline[i] = path[i] + polygon.offset;

                outline[path.Length] = outline[0];
                Handles.DrawAAPolyLine(3f, outline);
            }
        }
    }
}

#endif