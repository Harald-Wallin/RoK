#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// Scene View-feedback för BuildingSection.
///
/// Själva förflyttningen använder Unitys VANLIGA Move Tool.
/// BuildingSection snappar Transformen till sitt 32px-grid.
///
/// När en section är vald visas:
/// - vald section
/// - sibling sections under samma BuildingRoot
/// - den kombinerade union-footprintens exterior perimeter
/// - grid position och cellstorlek
/// </summary>
[CustomEditor(typeof(BuildingSection))]
public sealed class BuildingSectionEditor :
    Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();


        EditorGUILayout.Space(
            8f
        );


        EditorGUILayout.HelpBox(
            "Flytta sectionen med Unitys vanliga Move Tool.\n\n" +
            "Transform-positionen snappar automatiskt till " +
            "BuildingRoots lokala 32px structural grid.\n\n" +
            "Grön = vald section\n" +
            "Orange = andra sections under samma root\n" +
            "Cyan = kombinerad exterior perimeter",
            MessageType.Info
        );
    }


    private void OnSceneGUI()
    {
        BuildingSection section =
            (BuildingSection)target;


        BuildingRoot root =
            section.Root;


        if (root == null)
            return;


        DrawSiblingSections(
            root,
            section
        );


        DrawCombinedExteriorPerimeter(
            root
        );


        DrawSelectedSectionLabel(
            root,
            section
        );
    }


    // =========================================================
    // SIBLING SECTIONS
    // =========================================================

    private static void DrawSiblingSections(
        BuildingRoot root,
        BuildingSection selectedSection)
    {
        BuildingSection[] sections =
            root.GetComponentsInChildren<
                BuildingSection
            >(true);


        for (int i = 0;
             i < sections.Length;
             i++)
        {
            BuildingSection section =
                sections[i];


            if (section == null ||
                section.Root !=
                    root)
            {
                continue;
            }


            DrawSectionOutline(
                root,
                section,
                section ==
                    selectedSection
            );
        }
    }


    private static void DrawSectionOutline(
        BuildingRoot root,
        BuildingSection section,
        bool selected)
    {
        Vector2Int grid =
            section.GridPosition;

        Vector2Int size =
            section.EffectiveSizeCells;

        float step =
            BuildingGrid.StructuralStep;


        Vector3[] corners =
        {
            ToWorld(
                root,
                grid.x *
                step,
                grid.y *
                step
            ),

            ToWorld(
                root,
                (
                    grid.x +
                    size.x
                ) *
                step,
                grid.y *
                step
            ),

            ToWorld(
                root,
                (
                    grid.x +
                    size.x
                ) *
                step,
                (
                    grid.y +
                    size.y
                ) *
                step
            ),

            ToWorld(
                root,
                grid.x *
                step,
                (
                    grid.y +
                    size.y
                ) *
                step
            )
        };


        Color previousColor =
            Handles.color;


        Handles.color =
            selected
                ? new Color(
                    0.2f,
                    1f,
                    0.35f,
                    1f
                )
                : new Color(
                    1f,
                    0.65f,
                    0.1f,
                    0.75f
                );


        Handles.DrawAAPolyLine(
            selected
                ? 4f
                : 2f,

            corners[0],
            corners[1],
            corners[2],
            corners[3],
            corners[0]
        );


        Handles.color =
            previousColor;
    }


    // =========================================================
    // COMBINED BUILDING PERIMETER
    // =========================================================

    private static void DrawCombinedExteriorPerimeter(
        BuildingRoot root)
    {
        BuildingFootprint footprint =
            root.Footprint;


        if (footprint == null ||
            footprint.ExteriorEdges == null)
        {
            return;
        }


        Color previousColor =
            Handles.color;


        Handles.color =
            new Color(
                0.15f,
                0.95f,
                1f,
                1f
            );


        for (int i = 0;
             i <
             footprint.ExteriorEdges.Count;
             i++)
        {
            BuildingEdge edge =
                footprint.ExteriorEdges[i];


            if (edge.Cell.Level !=
                0)
            {
                continue;
            }


            Vector3 start =
                root.transform
                    .TransformPoint(
                        edge.LocalStart
                    );


            Vector3 end =
                root.transform
                    .TransformPoint(
                        edge.LocalEnd
                    );


            Handles.DrawAAPolyLine(
                5f,
                start,
                end
            );
        }


        Handles.color =
            previousColor;
    }


    // =========================================================
    // LABEL
    // =========================================================

    private static void DrawSelectedSectionLabel(
        BuildingRoot root,
        BuildingSection section)
    {
        Vector2Int grid =
            section.GridPosition;

        Vector2Int size =
            section.EffectiveSizeCells;

        float step =
            BuildingGrid.StructuralStep;


        Vector3 localLabelPosition =
            new Vector3(
                (
                    grid.x +
                    size.x *
                    0.5f
                ) *
                step,

                (
                    grid.y +
                    size.y
                ) *
                step +
                BuildingGrid.FineStep,

                0f
            );


        Vector3 worldLabelPosition =
            root.transform.TransformPoint(
                localLabelPosition
            );


        GUIStyle style =
            new GUIStyle(
                EditorStyles.boldLabel
            );


        style.alignment =
            TextAnchor.MiddleCenter;


        string label =
            $"{section.name}\n" +
            $"Grid {grid.x},{grid.y}   " +
            $"{size.x}×{size.y} cells";


        Handles.Label(
            worldLabelPosition,
            label,
            style
        );
    }


    // =========================================================
    // UTILITY
    // =========================================================

    private static Vector3 ToWorld(
        BuildingRoot root,
        float x,
        float y)
    {
        return
            root.transform
                .TransformPoint(
                    new Vector3(
                        x,
                        y,
                        0f
                    )
                );
    }
}

#endif
