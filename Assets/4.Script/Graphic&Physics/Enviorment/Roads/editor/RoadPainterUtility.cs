using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Gemensamma editor-utilities för Road Painter.
///
/// Innehåller ingen väg-presentation.
/// Den hjälper endast editorverktygen med:
/// - mouse -> world position
/// - förenkling av freehand-points
/// - skapande av RoadPath
/// - geometriska hjälpfunktioner
/// </summary>
internal static class RoadPainterUtility
{
    // =========================================================
    // SCENE POSITION
    // =========================================================

    public static bool TryGetMouseWorldPoint(
        Vector2 guiPosition,
        float worldZ,
        out Vector2 worldPoint)
    {
        Ray ray =
            HandleUtility.GUIPointToWorldRay(
                guiPosition
            );

        Plane worldPlane =
            new Plane(
                Vector3.forward,
                new Vector3(
                    0f,
                    0f,
                    worldZ
                )
            );

        if (!worldPlane.Raycast(
                ray,
                out float enter))
        {
            worldPoint =
                Vector2.zero;

            return false;
        }

        Vector3 hitPoint =
            ray.GetPoint(
                enter
            );

        worldPoint =
            new Vector2(
                hitPoint.x,
                hitPoint.y
            );

        return true;
    }


    // =========================================================
    // ROAD CREATION
    // =========================================================

    public static RoadPath CreateRoad(
        IReadOnlyList<Vector2> worldPoints,
        RoadProfile profile,
        Transform preferredParent)
    {
        if (worldPoints == null ||
            worldPoints.Count < 2)
        {
            return null;
        }

        Transform parent =
            FindOrCreateRoadParent(
                preferredParent
            );

        string baseName =
            profile != null
                ? $"Road_{SanitizeName(profile.name)}"
                : "Road";

        string[] existingNames =
            new string[
                parent != null
                    ? parent.childCount
                    : 0
            ];

        if (parent != null)
        {
            for (int i = 0;
                 i < parent.childCount;
                 i++)
            {
                existingNames[i] =
                    parent
                        .GetChild(i)
                        .name;
            }
        }

        string uniqueName =
            ObjectNames.GetUniqueName(
                existingNames,
                baseName
            );

        GameObject roadObject =
            new GameObject(
                uniqueName
            );

        if (parent != null)
        {
            roadObject.transform.SetParent(
                parent,
                true
            );
        }

        float worldZ =
            parent != null
                ? parent.position.z
                : 0f;

        roadObject.transform.position =
            new Vector3(
                worldPoints[0].x,
                worldPoints[0].y,
                worldZ
            );

        Undo.RegisterCreatedObjectUndo(
            roadObject,
            "Draw Road"
        );

        RoadPath roadPath =
            Undo.AddComponent<RoadPath>(
                roadObject
            );

        roadPath.SetProfile(
            profile
        );

        roadPath.ReplaceWorldPoints(
            worldPoints
        );

        roadPath.RandomizeAppearanceSeed();

        EditorUtility.SetDirty(
            roadPath
        );

        Selection.activeGameObject =
            roadObject;

        return roadPath;
    }


    private static Transform FindOrCreateRoadParent(
        Transform preferredParent)
    {
        if (preferredParent != null)
        {
            return preferredParent;
        }

        GameObject existingRoads =
            GameObject.Find(
                "Roads"
            );

        if (existingRoads != null)
        {
            return
                existingRoads.transform;
        }

        GameObject roadsRoot =
            new GameObject(
                "Roads"
            );

        Undo.RegisterCreatedObjectUndo(
            roadsRoot,
            "Create Roads Root"
        );

        return
            roadsRoot.transform;
    }


    private static string SanitizeName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return "Road";
        }

        return
            value
                .Replace(
                    " ",
                    "_"
                )
                .Replace(
                    "/",
                    "_"
                );
    }


    // =========================================================
    // FREEHAND SIMPLIFICATION
    // =========================================================

    /// <summary>
    /// Reducerar antalet sampled mouse-points utan att
    /// förändra den ritade formen märkbart.
    ///
    /// Bygger på Ramer-Douglas-Peucker-principen:
    /// punkter som inte bidrar till formen tas bort.
    /// </summary>
    public static List<Vector2> SimplifyPoints(
        IReadOnlyList<Vector2> source,
        float tolerance)
    {
        List<Vector2> result =
            new();

        if (source == null ||
            source.Count == 0)
        {
            return result;
        }

        if (source.Count <= 2)
        {
            for (int i = 0;
                 i < source.Count;
                 i++)
            {
                result.Add(
                    source[i]
                );
            }

            return result;
        }

        tolerance =
            Mathf.Max(
                0f,
                tolerance
            );

        bool[] keep =
            new bool[source.Count];

        keep[0] =
            true;

        keep[source.Count - 1] =
            true;

        SimplifySection(
            source,
            0,
            source.Count - 1,
            tolerance * tolerance,
            keep
        );

        for (int i = 0;
             i < source.Count;
             i++)
        {
            if (keep[i])
            {
                result.Add(
                    source[i]
                );
            }
        }

        return result;
    }


    private static void SimplifySection(
        IReadOnlyList<Vector2> points,
        int firstIndex,
        int lastIndex,
        float toleranceSquared,
        bool[] keep)
    {
        if (lastIndex <=
            firstIndex + 1)
        {
            return;
        }

        Vector2 segmentStart =
            points[firstIndex];

        Vector2 segmentEnd =
            points[lastIndex];

        float largestDistanceSquared =
            -1f;

        int largestIndex =
            -1;

        for (int i = firstIndex + 1;
             i < lastIndex;
             i++)
        {
            float distanceSquared =
                DistanceToSegmentSquared(
                    points[i],
                    segmentStart,
                    segmentEnd
                );

            if (distanceSquared >
                largestDistanceSquared)
            {
                largestDistanceSquared =
                    distanceSquared;

                largestIndex =
                    i;
            }
        }

        if (largestIndex < 0 ||
            largestDistanceSquared <=
            toleranceSquared)
        {
            return;
        }

        keep[largestIndex] =
            true;

        SimplifySection(
            points,
            firstIndex,
            largestIndex,
            toleranceSquared,
            keep
        );

        SimplifySection(
            points,
            largestIndex,
            lastIndex,
            toleranceSquared,
            keep
        );
    }


    private static float
        DistanceToSegmentSquared(
            Vector2 point,
            Vector2 segmentStart,
            Vector2 segmentEnd)
    {
        Vector2 segment =
            segmentEnd -
            segmentStart;

        float segmentLengthSquared =
            segment.sqrMagnitude;

        if (segmentLengthSquared <=
            0.000001f)
        {
            return
                (
                    point -
                    segmentStart
                ).sqrMagnitude;
        }

        float t =
            Vector2.Dot(
                point -
                segmentStart,
                segment
            ) /
            segmentLengthSquared;

        t =
            Mathf.Clamp01(
                t
            );

        Vector2 closest =
            segmentStart +
            segment * t;

        return
            (
                point -
                closest
            ).sqrMagnitude;
    }


    // =========================================================
    // EDITING GEOMETRY
    // =========================================================

    public static Vector2 ClosestPointOnSegment(
        Vector2 point,
        Vector2 segmentStart,
        Vector2 segmentEnd)
    {
        Vector2 segment =
            segmentEnd -
            segmentStart;

        float lengthSquared =
            segment.sqrMagnitude;

        if (lengthSquared <=
            0.000001f)
        {
            return segmentStart;
        }

        float t =
            Vector2.Dot(
                point -
                segmentStart,
                segment
            ) /
            lengthSquared;

        t =
            Mathf.Clamp01(
                t
            );

        return
            segmentStart +
            segment * t;
    }
}
