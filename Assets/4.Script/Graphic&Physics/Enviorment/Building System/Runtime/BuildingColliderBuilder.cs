using System.Collections.Generic;
using UnityEngine;

public static class BuildingColliderBuilder
{
    public const string CollisionRootName =
        "Collision";


    // =========================================================
    // ENTRY
    // =========================================================


    public static void Rebuild(
        BuildingRoot root,
        BuildingFootprint footprint)
    {
        if (root == null ||
            footprint == null)
        {
            return;
        }

        Transform generatedRoot =
            root.GetOrCreateGeneratedRoot();

        Transform collisionRoot =
            FindOrCreateChild(
                generatedRoot,
                CollisionRootName
            );

        // =====================================================
        // CAPTURE PREVIOUS GEOMETRY
        // =====================================================

        Bounds? previousColliderBounds =
            Application.isPlaying
                ? BuildingNavigationSync.GetColliderBounds(
                    collisionRoot
                )
                : null;

        // =====================================================
        // BUILD COLLISION
        // =====================================================

        ClearGeneratedChildren(
            collisionRoot
        );

        BuildPerimeterColliders(
            root,
            footprint,
            collisionRoot
        );

        if (root.GeneratePillarFootColliders)
        {
            BuildPillarFootColliders(
                root,
                generatedRoot,
                collisionRoot
            );
        }

        // =====================================================
        // NAVIGATION INTEGRATION
        // =====================================================

        BuildingNavigationSync.NotifyCollidersRebuilt(
            root,
            collisionRoot,
            previousColliderBounds
        );
    }



    // =========================================================
    // PERIMETER
    // =========================================================

    private readonly struct ColliderGap
    {
        public readonly float Start;
        public readonly float End;

        public ColliderGap(
            float start,
            float end)
        {
            Start = Mathf.Min(start, end);
            End = Mathf.Max(start, end);
        }
    }


    private static void BuildPerimeterColliders(
        BuildingRoot root,
        BuildingFootprint footprint,
        Transform collisionRoot)
    {
        /*
         * Use BuildingWallRun here instead of the older orientation-only
         * BuildingEdgeRun. Door openings need North/South/East/West identity,
         * and BuildingWallRun already owns that exact information.
         */
        List<BuildingWallRun> runs =
            BuildingWallRunBuilder.BuildRuns(
                footprint.ExteriorEdges,
                root.CollisionStructureLevel
            );

        for (int runIndex = 0;
             runIndex < runs.Count;
             runIndex++)
        {
            BuildingWallRun run =
                runs[runIndex];

            if (run.Length <= 0.0001f)
                continue;

            List<ColliderGap> gaps =
                CollectDoorGapsForRun(
                    root,
                    footprint,
                    run
                );

            float cursor =
                run.StartCoordinate;

            int segmentIndex = 0;

            for (int gapIndex = 0;
                 gapIndex < gaps.Count;
                 gapIndex++)
            {
                ColliderGap gap =
                    gaps[gapIndex];

                if (gap.Start >
                    cursor + 0.0001f)
                {
                    CreatePerimeterColliderSegment(
                        root,
                        collisionRoot,
                        run,
                        runIndex,
                        segmentIndex,
                        cursor,
                        gap.Start
                    );

                    segmentIndex++;
                }

                cursor =
                    Mathf.Max(
                        cursor,
                        gap.End
                    );
            }

            if (cursor <
                run.EndCoordinate -
                0.0001f)
            {
                CreatePerimeterColliderSegment(
                    root,
                    collisionRoot,
                    run,
                    runIndex,
                    segmentIndex,
                    cursor,
                    run.EndCoordinate
                );
            }
        }
    }


    private static List<ColliderGap>
        CollectDoorGapsForRun(
            BuildingRoot root,
            BuildingFootprint footprint,
            BuildingWallRun run)
    {
        List<ColliderGap> gaps =
            new();

        if (root.Openings == null)
            return gaps;

        for (int i = 0;
             i < root.Openings.Count;
             i++)
        {
            BuildingOpening opening =
                root.Openings[i];

            if (opening == null ||
                !opening.isActiveAndEnabled)
            {
                continue;
            }

            if (!BuildingOpeningResolver.TryResolve(
                    opening,
                    root,
                    footprint,
                    out BuildingResolvedOpening resolved))
            {
                continue;
            }

            if (resolved.Direction !=
                run.Direction)
            {
                continue;
            }

            if (Mathf.Abs(
                    resolved.FixedCoordinate -
                    run.FixedCoordinate) >
                0.0001f)
            {
                continue;
            }

            if (resolved.CenterCoordinate <
                    run.StartCoordinate -
                    0.0001f ||
                resolved.CenterCoordinate >
                    run.EndCoordinate +
                    0.0001f)
            {
                continue;
            }

            float passage =
                opening.GetPassageSizeWorld(
                    resolved.Direction
                );

            float half =
                passage *
                0.5f;

            float gapStart =
                Mathf.Max(
                    run.StartCoordinate,
                    resolved.CenterCoordinate -
                    half
                );

            float gapEnd =
                Mathf.Min(
                    run.EndCoordinate,
                    resolved.CenterCoordinate +
                    half
                );

            if (gapEnd <=
                gapStart +
                0.0001f)
            {
                continue;
            }

            gaps.Add(
                new ColliderGap(
                    gapStart,
                    gapEnd
                )
            );
        }

        if (gaps.Count <= 1)
            return gaps;

        gaps.Sort(
            (a, b) =>
                a.Start.CompareTo(
                    b.Start
                )
        );

        List<ColliderGap> merged =
            new();

        float mergedStart =
            gaps[0].Start;

        float mergedEnd =
            gaps[0].End;

        for (int i = 1;
             i < gaps.Count;
             i++)
        {
            ColliderGap next =
                gaps[i];

            if (next.Start <=
                mergedEnd +
                0.0001f)
            {
                mergedEnd =
                    Mathf.Max(
                        mergedEnd,
                        next.End
                    );

                continue;
            }

            merged.Add(
                new ColliderGap(
                    mergedStart,
                    mergedEnd
                )
            );

            mergedStart =
                next.Start;

            mergedEnd =
                next.End;
        }

        merged.Add(
            new ColliderGap(
                mergedStart,
                mergedEnd
            )
        );

        return merged;
    }


    private static void CreatePerimeterColliderSegment(
        BuildingRoot root,
        Transform collisionRoot,
        BuildingWallRun run,
        int runIndex,
        int segmentIndex,
        float startCoordinate,
        float endCoordinate)
    {
        float length =
            endCoordinate -
            startCoordinate;

        if (length <= 0.0001f)
            return;

        float thickness =
            run.Horizontal
                ? root.NorthSouthWallCollisionThickness
                : root.EastWestWallCollisionThickness;

        GameObject colliderObject =
            new GameObject(
                $"Wall_{run.Direction}_{runIndex:00}_{segmentIndex:00}"
            );

        colliderObject.transform.SetParent(
            collisionRoot,
            false
        );

        ApplyCollisionLayer(
            root,
            colliderObject
        );

        BoxCollider2D box =
            colliderObject.AddComponent<
                BoxCollider2D>();

        float centerAlong =
            (
                startCoordinate +
                endCoordinate
            ) *
            0.5f;

        if (run.Horizontal)
        {
            colliderObject.transform.localPosition =
                new Vector3(
                    centerAlong,
                    run.FixedCoordinate,
                    0f
                );

            box.size =
                new Vector2(
                    length,
                    thickness
                );
        }
        else
        {
            colliderObject.transform.localPosition =
                new Vector3(
                    run.FixedCoordinate,
                    centerAlong,
                    0f
                );

            box.size =
                new Vector2(
                    thickness,
                    length
                );
        }
    }


    // =========================================================
    // PILLAR FEET
    // =========================================================

    private static void BuildPillarFootColliders(
        BuildingRoot root,
        Transform generatedRoot,
        Transform collisionRoot)
    {
        Transform structureRoot =
            generatedRoot.Find(
                BuildingStructureRenderer.StructureRootName
            );


        if (structureRoot == null)
            return;


        SpriteRenderer[] renderers =
            structureRoot.GetComponentsInChildren<
                SpriteRenderer
            >(true);


        int colliderIndex = 0;


        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            SpriteRenderer renderer =
                renderers[i];


            if (renderer == null ||
                renderer.sprite == null)
            {
                continue;
            }


            /*
             * Structure-generatorn namnger båda typerna:
             *
             * Pillar_Full_...
             * Pillar_Side_...
             *
             * Genom att läsa den faktiska renderade pelaren
             * slipper collision-systemet duplicera:
             *
             * - side-wall visual lift
             * - side pillar lift
             * - horizontal pillar drop
             * - symmetri
             * - specialfall för hörn
             */
            if (!renderer.gameObject.name.StartsWith(
                    "Pillar_"))
            {
                continue;
            }


            Bounds bounds =
                renderer.bounds;


            /*
             * Foten definieras av den VISUELLA sprite-botten.
             *
             * Detta är viktigt för side-pillars eftersom de
             * numera är perspektivförskjutna tillsammans med
             * sidoväggen.
             */
            Vector3 worldFootBottom =
                new Vector3(
                    bounds.center.x,
                    bounds.min.y,
                    bounds.center.z
                );


            Vector3 localFootBottom =
                collisionRoot.InverseTransformPoint(
                    worldFootBottom
                );


            float colliderWidth =
                root.PillarFootColliderWidth;


            float colliderHeight =
                root.PillarFootColliderHeight;


            GameObject colliderObject =
                new GameObject(
                    $"PillarFoot_{colliderIndex:00}_{renderer.gameObject.name}"
                );


            colliderObject.transform.SetParent(
                collisionRoot,
                false
            );


            /*
             * Collider-boxens nederkant sätts på sprite-foten.
             * Därför flyttar vi dess center upp med halva höjden.
             */
            colliderObject.transform.localPosition =
                new Vector3(
                    localFootBottom.x,
                    localFootBottom.y +
                    colliderHeight *
                    0.5f,
                    0f
                );


            ApplyCollisionLayer(
                root,
                colliderObject
            );


            CapsuleCollider2D capsule =
    colliderObject.AddComponent<
        CapsuleCollider2D>();


            capsule.direction =
                CapsuleDirection2D.Horizontal;


            capsule.size =
                new Vector2(
                    colliderWidth,
                    colliderHeight
                );


            colliderIndex++;
        }
    }


    // =========================================================
    // LAYER
    // =========================================================

    private static void ApplyCollisionLayer(
        BuildingRoot root,
        GameObject target)
    {
        if (root == null ||
            target == null)
        {
            return;
        }


        int layer =
            LayerMask.NameToLayer(
                root.CollisionLayerName
            );


        if (layer >= 0)
        {
            target.layer =
                layer;
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


    private static void ClearGeneratedChildren(
        Transform parent)
    {
        if (parent == null)
            return;


        for (int i = parent.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                parent.GetChild(i);


#if UNITY_EDITOR

            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(
                    child.gameObject
                );

                continue;
            }

#endif


            /*
             * Samma runtime-säkerhet som vi nyligen lade
             * till i StructureRenderer:
             *
             * Destroy() sker först vid frame-end.
             *
             * Koppla därför loss och stäng av gamla collision
             * direkt så rebuild aldrig får en frame med dubbla
             * aktiva colliders.
             */
            child.gameObject.SetActive(
                false
            );


            child.SetParent(
                null,
                false
            );


            Object.Destroy(
                child.gameObject
            );
        }
    }
}