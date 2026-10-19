#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BuildingRoot))]
public sealed class BuildingRootEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10f);

        BuildingRoot root =
            (BuildingRoot)target;

        if (GUILayout.Button(
                "Rebuild Building Model"))
        {
            root.RebuildModel();
            EditorUtility.SetDirty(root);
            SceneView.RepaintAll();
        }

        if (GUILayout.Button(
                "Add Building Section"))
        {
            CreateSection(root);
        }

        EditorGUILayout.Space(8f);

        EditorGUILayout.HelpBox(
            "BuildingRoot may be placed freely in world space.\n\n" +
            "BuildingSections use the root-local 32px structural grid.\n\n" +
            "This first pass builds the union footprint, exterior " +
            "perimeter and merged perimeter colliders.",
            MessageType.Info
        );
    }

    private static void CreateSection(
        BuildingRoot root)
    {
        if (root == null)
            return;

        Transform authoringRoot =
            root.GetOrCreateAuthoringRoot();

        GameObject sectionObject =
            new GameObject(
                "Building Section"
            );

        Undo.RegisterCreatedObjectUndo(
            sectionObject,
            "Add Building Section"
        );

        sectionObject.transform.SetParent(
            authoringRoot,
            false
        );

        BuildingSection section =
            sectionObject.AddComponent<
                BuildingSection>();

        section.SyncTransformToGrid();

        root.RebuildModel();

        Selection.activeGameObject =
            sectionObject;
    }
}

#endif
