using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only utility för att rensa WorldScatter-clutter
/// under en RoadPath.
///
/// WorldScatter placerar sina instanser som direkta children
/// under sitt Target Root. Road Painter använder samma regel.
///
/// Vi testar objektens placeringspunkt/pivot, inte deras
/// renderer-bounds. Det gör att grästuvor vid vägkanten får
/// sticka visuellt in över vägen.
/// </summary>
internal static class RoadClutterUtility
{
    // =========================================================
    // PUBLIC
    // =========================================================

    public static int ClearGroundClutter(
        RoadPath road,
        Transform clutterRoot)
    {
        if (road == null ||
            clutterRoot == null ||
            road.Profile == null ||
            road.ControlPointCount < 2)
        {
            return 0;
        }


        float clearWidthScale =
            Mathf.Clamp01(
                road.Profile
                    .GroundClutterClearWidthScale
            );


        if (clearWidthScale <= 0f)
            return 0;


        List<Vector2> centerLine =
            new();


        RoadMeshBuilder
            .BuildSmoothCenterLine(
                road,
                centerLine
            );


        if (centerLine.Count < 2)
            return 0;


        List<GameObject> objectsToDelete =
            new();


        /*
         * Samma ownership-regel som WorldScatterWindow:
         *
         * bara direkta children under Target Root
         * betraktas som scatter-instanser.
         *
         * Vi går INTE ner i prefabens egna children.
         */
        for (int i =
                 clutterRoot.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                clutterRoot.GetChild(i);


            if (child == null)
                continue;


            Vector2 position =
                new Vector2(
                    child.position.x,
                    child.position.y
                );


            if (IsInsideRoadClearZone(
                    road,
                    centerLine,
                    position,
                    clearWidthScale))
            {
                objectsToDelete.Add(
                    child.gameObject
                );
            }
        }


        if (objectsToDelete.Count == 0)
            return 0;


        foreach (GameObject objectToDelete
                 in objectsToDelete)
        {
            if (objectToDelete == null)
                continue;


            Undo.DestroyObjectImmediate(
                objectToDelete
            );
        }


        EditorUtility.SetDirty(
            clutterRoot.gameObject
        );


        return
            objectsToDelete.Count;
    }


    // =========================================================
    // ROAD TEST
    // =========================================================

    private static bool IsInsideRoadClearZone(
        RoadPath road,
        IReadOnlyList<Vector2> centerLine,
        Vector2 position,
        float clearWidthScale)
    {
        for (int i = 0;
             i < centerLine.Count - 1;
             i++)
        {
            Vector2 start =
                centerLine[i];

            Vector2 end =
                centerLine[i + 1];


            Vector2 segment =
                end -
                start;


            float segmentLengthSquared =
                segment.sqrMagnitude;


            if (segmentLengthSquared <=
                0.000001f)
            {
                continue;
            }


            float t =
                Vector2.Dot(
                    position - start,
                    segment
                ) /
                segmentLengthSquared;


            t =
                Mathf.Clamp01(
                    t
                );


            Vector2 closest =
                start +
                segment * t;


            Vector2 tangent =
                segment.normalized;


            /*
             * Samma direction-aware width som själva vägen.
             *
             * Därmed blir clear-zonen också smalare när
             * vägen går horisontellt.
             */
            float roadWidth =
                road.GetWidthForDirection(
                    tangent
                );


            float clearHalfWidth =
                roadWidth *
                clearWidthScale *
                0.5f;


            float distanceSquared =
                (
                    position -
                    closest
                ).sqrMagnitude;


            if (distanceSquared <=
                clearHalfWidth *
                clearHalfWidth)
            {
                return true;
            }
        }


        return false;
    }
}
