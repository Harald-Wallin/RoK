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
            "Structure V1 generates union-footprint floors, exterior walls, " +
            "pillar/shadow details and N/S rear gables under Generated/Structure.\n\n" +
            "Openings/doors and final player/world sorting are intentionally " +
            "left for the next passes.",
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
