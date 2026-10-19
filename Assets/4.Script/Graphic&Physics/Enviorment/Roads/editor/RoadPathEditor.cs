using UnityEditor;
using UnityEngine;

/// <summary>
/// Scene View-editor för RoadPath.
///
/// Edit mode:
/// - kontrollpunkter kan dras fritt
/// - Shift + LMB lägger till point på närmaste segment
/// - Shift + RMB tar bort närmaste point
/// </summary>
[CustomEditor(typeof(RoadPath))]
public sealed class RoadPathEditor :
    Editor
{
    private const float
        RemovePointPixelDistance =
            14f;


    // =========================================================
    // INSPECTOR
    // =========================================================

    public override void OnInspectorGUI()
    {
        RoadPath road =  (RoadPath)target;

        EditorGUI.BeginChangeCheck();

        DrawDefaultInspector();

        if (EditorGUI.EndChangeCheck())
        {
                road.RebuildVisual();

                EditorUtility.SetDirty(road );

                SceneView.RepaintAll();

                EditorGUILayout.Space(8f);
        }

        EditorGUILayout.HelpBox(
            "Scene Edit:\n" +
            "Dra vita punkter för att ändra vägens form.\n" +
            "Shift + vänsterklick = lägg till punkt.\n" +
            "Shift + högerklick nära punkt = ta bort punkt.",
            MessageType.Info
        );

        if (GUILayout.Button("Rebuild Visual"))
        {
            road.RebuildVisual();

            SceneView.RepaintAll();
        }

        if (GUILayout.Button(
                "Randomize Appearance Seed"))
        {
            Undo.RecordObject(road,"Randomize Road Appearance");

            road.RandomizeAppearanceSeed();

            EditorUtility.SetDirty(
                road
            );

            SceneView.RepaintAll();
        }
    }


    // =========================================================
    // SCENE EDITING
    // =========================================================

    private void OnSceneGUI()
    {
        if (RoadPainterWindow
            .IsDrawModeActive)
        {
            return;
        }

        RoadPath road =
            (RoadPath)target;

        if (road == null ||
            road.ControlPointCount == 0)
        {
            return;
        }


        HandlePointInsertion(
            road
        );

        HandlePointRemoval(
            road
        );

        DrawPathLine(
            road
        );

        DrawPointHandles(
            road
        );
    }


    // =========================================================
    // DRAG POINTS
    // =========================================================

    private void DrawPointHandles(
        RoadPath road)
    {
        Handles.color =
            Color.white;

        for (int i = 0;
             i < road.ControlPointCount;
             i++)
        {
            Vector2 worldPoint =
                road.GetWorldPoint(i);

            Vector3 handlePosition =
                new Vector3(
                    worldPoint.x,
                    worldPoint.y,
                    road.transform.position.z
                );

            float handleSize =
                HandleUtility.GetHandleSize(
                    handlePosition
                ) *
                0.06f;


            EditorGUI.BeginChangeCheck();

            var fmh_140_21_639267605611631551 = Quaternion.identity; Vector3 movedPosition =
                Handles.FreeMoveHandle(
                    handlePosition,
                    handleSize,
                    Vector3.zero,
                    Handles.CircleHandleCap
                );

            if (!EditorGUI.EndChangeCheck())
                continue;


            Undo.RecordObject(
                road,
                "Move Road Point"
            );

            road.SetWorldPoint(
                i,
                new Vector2(
                    movedPosition.x,
                    movedPosition.y
                )
            );

            EditorUtility.SetDirty(
                road
            );

            SceneView.RepaintAll();
        }
    }


    // =========================================================
    // INSERT POINT
    // =========================================================

    private void HandlePointInsertion(
        RoadPath road)
    {
        Event currentEvent =
            Event.current;

        if (currentEvent == null ||
            currentEvent.type !=
                EventType.MouseDown ||
            currentEvent.button != 0 ||
            !currentEvent.shift ||
            currentEvent.alt)
        {
            return;
        }

        if (road.ControlPointCount < 2)
            return;


        if (!RoadPainterUtility
                .TryGetMouseWorldPoint(
                    currentEvent.mousePosition,
                    road.transform.position.z,
                    out Vector2 mouseWorld))
        {
            return;
        }


        int bestSegmentIndex =
            -1;

        float bestDistanceSquared =
            float.MaxValue;

        Vector2 bestPoint =
            Vector2.zero;


        for (int i = 0;
             i <
             road.ControlPointCount - 1;
             i++)
        {
            Vector2 start =
                road.GetWorldPoint(i);

            Vector2 end =
                road.GetWorldPoint(i + 1);

            Vector2 closest =
                RoadPainterUtility
                    .ClosestPointOnSegment(
                        mouseWorld,
                        start,
                        end
                    );

            float distanceSquared =
                (
                    mouseWorld -
                    closest
                ).sqrMagnitude;

            if (distanceSquared <
                bestDistanceSquared)
            {
                bestDistanceSquared =
                    distanceSquared;

                bestSegmentIndex =
                    i;

                bestPoint =
                    closest;
            }
        }


        if (bestSegmentIndex < 0)
            return;


        Undo.RecordObject(
            road,
            "Insert Road Point"
        );

        road.InsertWorldPoint(
            bestSegmentIndex + 1,
            bestPoint
        );

        EditorUtility.SetDirty(
            road
        );

        currentEvent.Use();

        SceneView.RepaintAll();
    }


    // =========================================================
    // REMOVE POINT
    // =========================================================

    private void HandlePointRemoval(
        RoadPath road)
    {
        Event currentEvent =
            Event.current;

        if (currentEvent == null ||
            currentEvent.type !=
                EventType.MouseDown ||
            currentEvent.button != 1 ||
            !currentEvent.shift ||
            currentEvent.alt)
        {
            return;
        }


        /*
         * En väg behöver minst två punkter.
         */
        if (road.ControlPointCount <= 2)
            return;


        int closestIndex =
            -1;

        float closestPixelDistance =
            float.MaxValue;


        for (int i = 0;
             i < road.ControlPointCount;
             i++)
        {
            Vector2 worldPoint =
                road.GetWorldPoint(i);

            Vector2 guiPoint =
                HandleUtility
                    .WorldToGUIPoint(
                        new Vector3(
                            worldPoint.x,
                            worldPoint.y,
                            road.transform.position.z
                        )
                    );

            float pixelDistance =
                Vector2.Distance(
                    guiPoint,
                    currentEvent.mousePosition
                );

            if (pixelDistance <
                closestPixelDistance)
            {
                closestPixelDistance =
                    pixelDistance;

                closestIndex =
                    i;
            }
        }


        if (closestIndex < 0 ||
            closestPixelDistance >
                RemovePointPixelDistance)
        {
            return;
        }


        Undo.RecordObject(
            road,
            "Remove Road Point"
        );

        road.RemovePoint(
            closestIndex
        );

        EditorUtility.SetDirty(
            road
        );

        currentEvent.Use();

        SceneView.RepaintAll();
    }


    // =========================================================
    // PATH PREVIEW
    // =========================================================

    private void DrawPathLine(
        RoadPath road)
    {
        if (road.ControlPointCount < 2)
            return;

        Vector3[] centerLine =
            new Vector3[
                road.ControlPointCount
            ];

        Vector3[] leftEdge =
            new Vector3[
                road.ControlPointCount
            ];

        Vector3[] rightEdge =
            new Vector3[
                road.ControlPointCount
            ];


        for (int i = 0;
             i < road.ControlPointCount;
             i++)
        {
            Vector2 center =
                road.GetWorldPoint(i);

            Vector2 tangent =
                GetTangent(
                    road,
                    i
                );

            Vector2 normal =
                new Vector2(
                    -tangent.y,
                    tangent.x
                );

            float halfWidth =
                road.GetWidthForDirection(
                    tangent
                ) *
                0.5f;


            centerLine[i] =
                new Vector3(
                    center.x,
                    center.y,
                    road.transform.position.z
                );

            Vector2 left =
                center +
                normal *
                halfWidth;

            leftEdge[i] =
                new Vector3(
                    left.x,
                    left.y,
                    road.transform.position.z
                );

            Vector2 right =
                center -
                normal *
                halfWidth;

            rightEdge[i] =
                new Vector3(
                    right.x,
                    right.y,
                    road.transform.position.z
                );
        }


        Handles.color =
            new Color(
                1f,
                0.75f,
                0.15f,
                1f
            );

        Handles.DrawAAPolyLine(
            3f,
            centerLine
        );


        Handles.color =
            new Color(
                1f,
                0.5f,
                0.1f,
                0.65f
            );

        Handles.DrawAAPolyLine(
            2f,
            leftEdge
        );

        Handles.DrawAAPolyLine(
            2f,
            rightEdge
        );
    }


    private static Vector2 GetTangent(
        RoadPath road,
        int index)
    {
        if (road.ControlPointCount < 2)
        {
            return
                Vector2.right;
        }

        Vector2 previous;
        Vector2 next;


        if (index <= 0)
        {
            previous =
                road.GetWorldPoint(0);

            next =
                road.GetWorldPoint(1);
        }
        else if (
            index >=
            road.ControlPointCount - 1)
        {
            previous =
                road.GetWorldPoint(
                    road.ControlPointCount - 2
                );

            next =
                road.GetWorldPoint(
                    road.ControlPointCount - 1
                );
        }
        else
        {
            previous =
                road.GetWorldPoint(
                    index - 1
                );

            next =
                road.GetWorldPoint(
                    index + 1
                );
        }


        Vector2 direction =
            next -
            previous;

        if (direction.sqrMagnitude <=
            0.000001f)
        {
            return
                Vector2.right;
        }

        return
            direction.normalized;
    }
}
