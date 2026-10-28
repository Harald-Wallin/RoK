using System.Collections.Generic;
using UnityEngine;

public static class BuildingIndoorZoneBuilder
{
    public const string IndoorZoneRootName =
        "IndoorZone";


    private readonly struct RowSpan
    {
        public readonly int Y;
        public readonly int StartX;
        public readonly int EndXExclusive;

        public RowSpan(
            int y,
            int startX,
            int endXExclusive)
        {
            Y = y;
            StartX = startX;
            EndXExclusive = endXExclusive;
        }
    }


    public static void Rebuild(
    BuildingRoot root,
    BuildingFootprint footprint,
    OcclusionFader[] roofFaders)
    {
        if (root == null ||
            footprint == null)
        {
            return;
        }


        Transform generatedRoot =
            root.GetOrCreateGeneratedRoot();


        Transform zoneRoot =
            FindOrCreateChild(
                generatedRoot,
                IndoorZoneRootName
            );


        zoneRoot.localPosition =
            Vector3.zero;


        int layer =
            LayerMask.NameToLayer(
                root.CollisionLayerName
            );


        if (layer >= 0)
        {
            zoneRoot.gameObject.layer =
                layer;
        }


        IndoorZone indoorZone =
            zoneRoot.GetComponent<
                IndoorZone>();


        if (indoorZone == null)
        {
            indoorZone =
                zoneRoot.gameObject
                    .AddComponent<
                        IndoorZone>();
        }


        List<RowSpan> spans =
            BuildRowSpans(
                footprint,
                root.StructureLevel
            );


        ConfigureColliders(
            zoneRoot.gameObject,
            spans
        );


        indoorZone.SetOcclusionFaders(
            roofFaders
        );


        indoorZone.RefreshSetup();
    }


    // =========================================================
    // FOOTPRINT -> HORIZONTAL SPANS
    // =========================================================

    private static List<RowSpan> BuildRowSpans(
        BuildingFootprint footprint,
        int structureLevel)
    {
        Dictionary<int, List<int>> rows =
            new();


        foreach (BuildingCell cell
                 in footprint.Cells)
        {
            if (cell.Level !=
                structureLevel)
            {
                continue;
            }


            if (!rows.TryGetValue(
                    cell.Y,
                    out List<int> xs))
            {
                xs =
                    new List<int>();

                rows.Add(
                    cell.Y,
                    xs
                );
            }


            xs.Add(
                cell.X
            );
        }


        List<RowSpan> result =
            new();


        foreach (
            KeyValuePair<int, List<int>> pair
            in rows)
        {
            int y =
                pair.Key;

            List<int> xs =
                pair.Value;


            if (xs == null ||
                xs.Count == 0)
            {
                continue;
            }


            xs.Sort();


            int spanStart =
                xs[0];

            int previous =
                xs[0];


            for (int i = 1;
                 i < xs.Count;
                 i++)
            {
                int current =
                    xs[i];


                if (current ==
                    previous + 1)
                {
                    previous =
                        current;

                    continue;
                }


                result.Add(
                    new RowSpan(
                        y,
                        spanStart,
                        previous + 1
                    )
                );


                spanStart =
                    current;

                previous =
                    current;
            }


            result.Add(
                new RowSpan(
                    y,
                    spanStart,
                    previous + 1
                )
            );
        }


        return result;
    }


    // =========================================================
    // COLLIDERS
    // =========================================================

    private static void ConfigureColliders(
        GameObject zoneObject,
        IReadOnlyList<RowSpan> spans)
    {
        if (zoneObject == null)
            return;


        BoxCollider2D[] boxes =
            zoneObject.GetComponents<
                BoxCollider2D>();


        int requiredCount =
            spans != null
                ? spans.Count
                : 0;


        /*
         * IndoorZone har RequireComponent(Collider2D),
         * så första BoxCollider2D finns normalt redan.
         *
         * Vi återanvänder colliders istället för att Destroy/Add
         * vid varje rebuild. Det gör även runtime rebuild säkert.
         */
        while (boxes.Length <
               requiredCount)
        {
            zoneObject.AddComponent<
                BoxCollider2D>();


            boxes =
                zoneObject.GetComponents<
                    BoxCollider2D>();
        }


        for (int i = 0;
             i < boxes.Length;
             i++)
        {
            BoxCollider2D box =
                boxes[i];


            if (box == null)
                continue;


            if (i >= requiredCount)
            {
                box.enabled =
                    false;

                continue;
            }


            RowSpan span =
                spans[i];


            float step =
                BuildingGrid.StructuralStep;


            float width =
                (
                    span.EndXExclusive -
                    span.StartX
                ) *
                step;


            float centerX =
                (
                    span.StartX +
                    span.EndXExclusive
                ) *
                0.5f *
                step;


            float centerY =
                (
                    span.Y +
                    0.5f
                ) *
                step;


            box.enabled =
                true;


            box.isTrigger =
                true;


            box.offset =
                new Vector2(
                    centerX,
                    centerY
                );


            box.size =
                new Vector2(
                    width,
                    step
                );
        }
    }


    // =========================================================
    // HIERARCHY
    // =========================================================

    private static Transform FindOrCreateChild(
        Transform parent,
        string childName)
    {
        Transform existing =
            parent.Find(
                childName
            );


        if (existing != null)
            return existing;


        GameObject child =
            new GameObject(
                childName
            );


        child.transform.SetParent(
            parent,
            false
        );


        return child.transform;
    }
}
