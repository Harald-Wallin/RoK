using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Structure V1 renderer.
///
/// Bygger visuella byggnadsdelar från BuildingRoots union-footprint:
/// - 32x32 floor tiles
/// - horisontella North/South-väggar via 64/128 modulsolver
/// - vertikala East/West side-wall segments i 32px logiska steg
/// - fulla träpelare på horisontella wall seams
/// - samma träpelare på sidoväggar med separat side-placement
/// - beam shadows runt horisontella pelare
/// - rear/interior gable för N/S-roof sections
///
/// Colliders ägs fortfarande av BuildingColliderBuilder.
/// </summary>
public static class BuildingStructureRenderer
{
    public const string StructureRootName =
        "Structure";

    private const string FloorsRootName =
        "Floors";

    private const string WallsRootName =
        "Walls";

    private const string ShadowsRootName =
        "PillarShadows";

    private const string PillarsRootName =
        "Pillars";

    private const string RearGablesRootName =
        "RearGables";

    private const int HorizontalModulePixels = 64;
    private const int GableFillPixels = 64;
    private const int GableSlopePixels = 32;


    private enum StructureSpriteAnchor
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        Center,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }


    private readonly struct GeneratedRoots
    {
        public readonly Transform Structure;
        public readonly Transform Floors;
        public readonly Transform Walls;
        public readonly Transform Shadows;
        public readonly Transform Pillars;
        public readonly Transform RearGables;

        public GeneratedRoots(
            Transform structure,
            Transform floors,
            Transform walls,
            Transform shadows,
            Transform pillars,
            Transform rearGables)
        {
            Structure = structure;
            Floors = floors;
            Walls = walls;
            Shadows = shadows;
            Pillars = pillars;
            RearGables = rearGables;
        }
    }


    // =========================================================
    // ENTRY
    // =========================================================

    public static void Clear(
        BuildingRoot root)
    {
        if (root == null)
            return;

        Transform generatedRoot =
            root.GetOrCreateGeneratedRoot();

        Transform existing =
            generatedRoot.Find(
                StructureRootName
            );

        if (existing == null)
            return;

        ClearChildren(existing);
    }


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

        Transform structureRoot =
            FindOrCreateChild(
                generatedRoot,
                StructureRootName
            );

        ClearChildren(
            structureRoot
        );

        BuildingStructureStyle style =
            root.StructureStyle;

        if (style == null)
            return;

        GeneratedRoots roots =
            CreateGeneratedRoots(
                structureRoot
            );

        BuildFloors(
            root,
            footprint,
            style,
            roots.Floors
        );

        List<BuildingWallRun> wallRuns =
            BuildingWallRunBuilder.BuildRuns(
                footprint.ExteriorEdges,
                root.StructureLevel
            );

        HashSet<Vector2Int> fullPillarPoints =
            new();

        BuildHorizontalWalls(
            root,
            style,
            wallRuns,
            roots,
            fullPillarPoints
        );

        BuildVerticalWalls(
            root,
            style,
            wallRuns,
            roots,
            fullPillarPoints
        );

        BuildRearGables(
            root,
            footprint,
            style,
            roots.RearGables
        );
    }


    // =========================================================
    // FLOOR
    // =========================================================

    private static void BuildFloors(
        BuildingRoot root,
        BuildingFootprint footprint,
        BuildingStructureStyle style,
        Transform floorRoot)
    {
        Sprite floorSprite =
            style.FloorSprite;

        if (floorSprite == null)
            return;

        int index = 0;
        float step =
            BuildingGrid.StructuralStep;

        foreach (BuildingCell cell in footprint.Cells)
        {
            if (cell.Level != root.StructureLevel)
                continue;

            Vector2 bottomLeft =
                new Vector2(
                    cell.X * step,
                    cell.Y * step
                );

            CreateSpritePiece(
                floorRoot,
                $"Floor_{index:0000}",
                floorSprite,
                bottomLeft,
                StructureSpriteAnchor.BottomLeft,
                false,
                false,
                style.SortingLayerName,
                style.FloorSortingOrder
            );

            index++;
        }
    }


    // =========================================================
    // HORIZONTAL WALLS
    // =========================================================

    private static void BuildHorizontalWalls(
        BuildingRoot root,
        BuildingStructureStyle style,
        IReadOnlyList<BuildingWallRun> runs,
        GeneratedRoots roots,
        HashSet<Vector2Int> fullPillarPoints)
    {
        if (runs == null)
            return;

        Transform horizontalRoot =
            FindOrCreateChild(
                roots.Walls,
                "Horizontal"
            );

        for (int i = 0; i < runs.Count; i++)
        {
            BuildingWallRun run =
                runs[i];

            if (!run.Horizontal)
                continue;

            BuildHorizontalWallRun(
                root,
                style,
                run,
                i,
                horizontalRoot,
                roots.Shadows,
                roots.Pillars,
                fullPillarPoints
            );
        }
    }


    private static void BuildHorizontalWallRun(
        BuildingRoot root,
        BuildingStructureStyle style,
        BuildingWallRun run,
        int runIndex,
        Transform wallRoot,
        Transform shadowRoot,
        Transform pillarRoot,
        HashSet<Vector2Int> fullPillarPoints)
    {
        bool interiorFacing =
            run.Direction == BuildingEdgeDirection.North;

        Sprite sprite64 =
            interiorFacing
                ? style.InteriorWall64
                : style.ExteriorWall64;

        Sprite sprite128 =
            interiorFacing
                ? style.InteriorWall128
                : style.ExteriorWall128;

        int runLengthPixels =
            Mathf.RoundToInt(
                run.Length *
                BuildingGrid.PixelsPerUnit
            );

        if (!BuildingWallModuleSolver.TrySolve(
                runLengthPixels,
                sprite64,
                sprite128,
                out List<BuildingWallModuleChoice> modules))
        {
#if UNITY_EDITOR
            Debug.LogWarning(
                $"[BUILDING STRUCTURE] {root.name}: " +
                $"Could not solve horizontal wall run {run.Direction} " +
                $"({runLengthPixels}px). Structure V1 currently needs an exact " +
                $"64/128px combination.",
                root
            );
#endif
            return;
        }

        int wallSorting =
            run.Direction == BuildingEdgeDirection.South
                ? style.FrontWallSortingOrder
                : style.RearWallSortingOrder;

        float cursor =
            run.StartCoordinate;

        List<float> seams =
            new()
            {
                cursor
            };

        for (int moduleIndex = 0;
             moduleIndex < modules.Count;
             moduleIndex++)
        {
            BuildingWallModuleChoice module =
                modules[moduleIndex];

            CreateSpritePiece(
                wallRoot,
                $"Wall_{run.Direction}_{runIndex:00}_{moduleIndex:00}_{module.LengthPixels}px",
                module.Sprite,
                new Vector2(
                    cursor,
                    run.FixedCoordinate
                ),
                StructureSpriteAnchor.BottomLeft,
                false,
                false,
                style.SortingLayerName,
                wallSorting
            );

            cursor +=
                module.LengthPixels /
                (float)BuildingGrid.PixelsPerUnit;

            seams.Add(
                cursor
            );
        }

        for (int seamIndex = 0;
             seamIndex < seams.Count;
             seamIndex++)
        {
            float seamX =
                seams[seamIndex];

            Vector2 structuralPoint =
                new Vector2(
                    seamX,
                    run.FixedCoordinate
                );

            Vector2Int pointKey =
                GetPixelPointKey(
                    structuralPoint
                );

            bool hasWallOnLeft =
                seamIndex > 0;

            bool hasWallOnRight =
                seamIndex < seams.Count - 1;

            /*
             * North / rear wall:
             * interior seams used to generate full pillars + beam shadows.
             * With the rear wall + rear gable in place those posts are hidden
             * and only add visual noise / unnecessary generated objects.
             *
             * Keep the two NORTH CORNER pillars (and their one-sided shadows),
             * but completely skip interior rear pillars and their shadows.
             */
            bool rearInteriorSeam =
                run.Direction == BuildingEdgeDirection.North &&
                hasWallOnLeft &&
                hasWallOnRight;

            if (rearInteriorSeam)
                continue;

            BuildHorizontalPillarShadows(
                style,
                shadowRoot,
                run,
                runIndex,
                seamIndex,
                seamX,
                hasWallOnLeft,
                hasWallOnRight,
                wallSorting + 1
            );

            if (!fullPillarPoints.Add(pointKey))
                continue;

            BuildHorizontalPillar(
                style,
                pillarRoot,
                run,
                runIndex,
                seamIndex,
                seamX,
                wallSorting + 2
            );
        }
    }


    private static void BuildHorizontalPillarShadows(
        BuildingStructureStyle style,
        Transform shadowRoot,
        BuildingWallRun run,
        int runIndex,
        int seamIndex,
        float seamX,
        bool hasWallOnLeft,
        bool hasWallOnRight,
        int sortingOrder)
    {
        Sprite shadowSprite =
            style.BeamShadowSprite;

        if (shadowSprite == null)
            return;

        float offset =
            style.BeamShadowOffsetPixels /
            (float)BuildingGrid.PixelsPerUnit;

        if (hasWallOnLeft)
        {
            // Source gradient is light on left, dark on right.
            // On the wall left of the pillar, dark side points toward seam.
            CreateSpritePiece(
                shadowRoot,
                $"Shadow_{run.Direction}_{runIndex:00}_{seamIndex:00}_L",
                shadowSprite,
                new Vector2(
                    seamX - offset,
                    run.FixedCoordinate
                ),
                StructureSpriteAnchor.BottomCenter,
                false,
                false,
                style.SortingLayerName,
                sortingOrder
            );
        }

        if (hasWallOnRight)
        {
            // Right-hand wall needs the same gradient mirrored so its dark
            // side points back toward the pillar.
            CreateSpritePiece(
                shadowRoot,
                $"Shadow_{run.Direction}_{runIndex:00}_{seamIndex:00}_R",
                shadowSprite,
                new Vector2(
                    seamX + offset,
                    run.FixedCoordinate
                ),
                StructureSpriteAnchor.BottomCenter,
                true,
                false,
                style.SortingLayerName,
                sortingOrder
            );
        }
    }


    private static void BuildHorizontalPillar(
        BuildingStructureStyle style,
        Transform pillarRoot,
        BuildingWallRun run,
        int runIndex,
        int seamIndex,
        float seamX,
        int sortingOrder)
    {
        Sprite pillarSprite =
            style.PillarSprite;

        if (pillarSprite == null)
            return;

        float drop =
            style.HorizontalPillarDropPixels /
            (float)BuildingGrid.PixelsPerUnit;

        CreateSpritePiece(
            pillarRoot,
            $"Pillar_Full_{run.Direction}_{runIndex:00}_{seamIndex:00}",
            pillarSprite,
            new Vector2(
                seamX,
                run.FixedCoordinate - drop
            ),
            StructureSpriteAnchor.BottomCenter,
            false,
            false,
            style.SortingLayerName,
            sortingOrder
        );
    }


    // =========================================================
    // VERTICAL / SIDE WALLS
    // =========================================================

    private static void BuildVerticalWalls(
        BuildingRoot root,
        BuildingStructureStyle style,
        IReadOnlyList<BuildingWallRun> runs,
        GeneratedRoots roots,
        HashSet<Vector2Int> fullPillarPoints)
    {
        if (runs == null)
            return;

        Transform verticalRoot =
            FindOrCreateChild(
                roots.Walls,
                "Vertical"
            );

        for (int i = 0; i < runs.Count; i++)
        {
            BuildingWallRun run =
                runs[i];

            if (run.Horizontal)
                continue;

            BuildVerticalWallRun(
                root,
                style,
                run,
                i,
                verticalRoot,
                fullPillarPoints
            );
        }
    }


    private static void BuildVerticalWallRun(
        BuildingRoot root,
        BuildingStructureStyle style,
        BuildingWallRun run,
        int runIndex,
        Transform verticalRoot,
        HashSet<Vector2Int> fullPillarPoints)
    {
        float step =
            BuildingGrid.StructuralStep;

        int segmentCount =
            Mathf.RoundToInt(
                run.Length /
                step
            );

        if (segmentCount <= 0)
            return;

        float visualLift =
            style.SideWallVisualLiftPixels /
            (float)BuildingGrid.PixelsPerUnit;

        float visualRunStartY =
            run.StartCoordinate +
            visualLift;

        /*
         * IMPORTANT:
         *
         * run.StartCoordinate / run.EndCoordinate remain the TRUE structural
         * footprint coordinates.
         *
         * Only the presentation is lifted upward. This gives the side wall the
         * same "depth" illusion as the roof without changing floor, footprint
         * or collision geometry.
         *
         * With the default 64px lift:
         * - the whole visible side wall moves two 32px cells north/up-screen;
         * - it therefore naturally continues two wall pieces beyond the rear
         *   floor edge;
         * - the two front-most logical cells are hidden beneath the 96px front
         *   wall/pillars instead of creating a visual gap.
         *
         * Side pillars use the exact same visual lift, so their symmetry is now
         * evaluated against the wall the player actually SEES rather than the
         * flat floor rectangle.
         */

        /*
         * One SortingGroup per N/S side-wall run.
         *
         * Inside the group the wall pieces interleave with side pillars:
         *
         * front wall-piece > pillar > rear wall-piece.
         *
         * Translating the entire presentation does not change these relative
         * orders, so the existing overlap/Y-depth behaviour remains stable.
         */
        Transform runRoot =
            FindOrCreateChild(
                verticalRoot,
                $"Run_{run.Direction}_{runIndex:00}"
            );

        SortingGroup sortingGroup =
            runRoot.GetComponent<SortingGroup>();

        if (sortingGroup == null)
        {
            sortingGroup =
                runRoot.gameObject.AddComponent<SortingGroup>();
        }

        if (!string.IsNullOrWhiteSpace(
                style.SortingLayerName))
        {
            sortingGroup.sortingLayerName =
                style.SortingLayerName;
        }

        sortingGroup.sortingOrder =
            style.SideWallFrontSortingOrder;

        Sprite wallSprite =
            style.SideWallSegment;

        bool flipX =
            run.Direction == BuildingEdgeDirection.East &&
            style.FlipSideWallOnEast;

        if (wallSprite != null)
        {
            for (int segmentIndex = 0;
                 segmentIndex < segmentCount;
                 segmentIndex++)
            {
                float segmentStartY =
                    visualRunStartY +
                    segmentIndex *
                    step;

                int sortingOrder =
                    GetSideWallInternalSorting(
                        segmentIndex,
                        segmentCount
                    );

                CreateSpritePiece(
                    runRoot,
                    $"SideWall_{run.Direction}_{runIndex:00}_{segmentIndex:00}",
                    wallSprite,
                    new Vector2(
                        run.FixedCoordinate,
                        segmentStartY
                    ),
                    StructureSpriteAnchor.BottomCenter,
                    flipX,
                    false,
                    style.SortingLayerName,
                    sortingOrder
                );
            }
        }

        if (style.PillarSprite == null)
            return;

        List<int> pillarSeams =
            GetSymmetricSidePillarSeams(
                segmentCount,
                style.SidePillarSpacingCells
            );

        for (int i = 0;
             i < pillarSeams.Count;
             i++)
        {
            int seamIndex =
                pillarSeams[i];

            /*
             * Keep the structural seam for ownership/corner checks.
             * This ensures the true north/south corner pillars still win even
             * though the side-wall presentation itself is shifted upward.
             */
            float structuralSeamY =
                run.StartCoordinate +
                seamIndex *
                step;

            Vector2 structuralPoint =
                new Vector2(
                    run.FixedCoordinate,
                    structuralSeamY
                );

            Vector2Int pointKey =
                GetPixelPointKey(
                    structuralPoint
                );

            // Full horizontal-wall corner pillar owns the real corner.
            if (fullPillarPoints.Contains(pointKey))
                continue;

            float visualSeamY =
                structuralSeamY +
                visualLift;

            /*
             * The tall 112px post may overlap south/front visually, which is
             * intentional, but its foot must not extend beyond the REAL side
             * wall footprint. The test therefore compares the visually lifted
             * pillar against the unchanged structural run.StartCoordinate.
             */
            if (SidePillarWouldExtendPastFront(
                    style,
                    run,
                    visualSeamY))
            {
                continue;
            }

            int sortingOrder =
                GetSidePillarInternalSorting(
                    seamIndex,
                    segmentCount
                );

            BuildSidePillar(
                style,
                runRoot,
                run,
                runIndex,
                seamIndex,
                visualSeamY,
                sortingOrder
            );
        }
    }


    private static void BuildSidePillar(
        BuildingStructureStyle style,
        Transform pillarRoot,
        BuildingWallRun run,
        int runIndex,
        int seamIndex,
        float visualSeamY,
        int sortingOrder)
    {
        Sprite pillarSprite =
            style.PillarSprite;

        if (pillarSprite == null)
            return;

        float lift =
            style.SidePillarLiftPixels /
            (float)BuildingGrid.PixelsPerUnit;

        Vector2 targetTop =
            new Vector2(
                run.FixedCoordinate,
                visualSeamY + lift
            );

        CreateSpritePiece(
            pillarRoot,
            $"Pillar_Side_{run.Direction}_{runIndex:00}_{seamIndex:00}",
            pillarSprite,
            targetTop,
            StructureSpriteAnchor.TopCenter,
            false,
            false,
            style.SortingLayerName,
            sortingOrder
        );
    }


    private static bool SidePillarWouldExtendPastFront(
        BuildingStructureStyle style,
        BuildingWallRun run,
        float visualSeamY)
    {
        Sprite pillarSprite =
            style.PillarSprite;

        if (pillarSprite == null)
            return true;

        float ppu =
            Mathf.Max(
                0.0001f,
                pillarSprite.pixelsPerUnit
            );

        float lift =
            style.SidePillarLiftPixels /
            (float)BuildingGrid.PixelsPerUnit;

        float visualTopY =
            visualSeamY + lift;

        float visualBottomY =
            visualTopY -
            pillarSprite.rect.height /
            ppu;

        return
            visualBottomY <
            run.StartCoordinate -
            0.0001f;
    }


    private static List<int> GetSymmetricSidePillarSeams(
        int segmentCount,
        int spacingCells)
    {
        SortedSet<int> seams =
            new()
            {
                0,
                segmentCount
            };

        int spacing =
            Mathf.Max(
                1,
                spacingCells
            );

        for (int distance = spacing;
             distance <= segmentCount / 2;
             distance += spacing)
        {
            int fromStart =
                distance;

            int fromEnd =
                segmentCount -
                distance;

            seams.Add(fromStart);
            seams.Add(fromEnd);
        }

        return new List<int>(seams);
    }


    private static int GetSideWallInternalSorting(
        int segmentIndex,
        int segmentCount)
    {
        /*
         * Example, six segments:
         *
         * segment 0 = 12  (frontmost)
         * segment 1 = 10
         * segment 2 =  8
         * ...
         * segment 5 =  2  (rearmost)
         */
        return
            Mathf.Max(
                0,
                (segmentCount - segmentIndex) * 2
            );
    }


    private static int GetSidePillarInternalSorting(
        int seamIndex,
        int segmentCount)
    {
        /*
         * A pillar on seam K sits between:
         *
         * segment K-1 (south/front)
         * and
         * segment K   (north/back)
         *
         * e.g. seam 2 in a 6-cell wall:
         * segment 1 = 10
         * pillar    =  9
         * segment 2 =  8
         *
         * This is the exact visual rule we want: the wall approaching from
         * the front renders over the post, while the next wall segment starts
         * behind it. The existing +16px pillar lift determines where that
         * hand-off happens visually.
         */
        return
            Mathf.Max(
                0,
                (segmentCount - seamIndex) * 2 + 1
            );
    }


    // =========================================================
    // REAR / INTERIOR GABLE
    // =========================================================

    private static void BuildRearGables(
        BuildingRoot root,
        BuildingFootprint footprint,
        BuildingStructureStyle style,
        Transform rearGableRoot)
    {
        if (root.Sections == null)
            return;

        if (style.RearGableSlope32x32 == null &&
            style.RearGableFill64x32 == null &&
            style.RearGableBaseTrim32x7 == null)
        {
            return;
        }

        HashSet<string> generatedKeys =
            new();

        for (int i = 0;
             i < root.Sections.Count;
             i++)
        {
            BuildingSection section =
                root.Sections[i];

            if (section == null ||
                !section.isActiveAndEnabled ||
                section.StructureLevel != root.StructureLevel)
            {
                continue;
            }

            BuildingSectionRoofSettings roof =
                section.RoofSettings;

            if (roof == null ||
                !roof.Enabled ||
                roof.Direction != BuildingRoofDirection.NorthSouth)
            {
                continue;
            }

            if (!IsSectionNorthEdgeExterior(
                    section,
                    footprint))
            {
                continue;
            }

            Vector2Int size =
                section.EffectiveSizeCells;

            int widthPixels =
                size.x *
                BuildingGrid.StructuralPixels;

            if (widthPixels % GableFillPixels != 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"[BUILDING STRUCTURE] {root.name} / {section.name}: " +
                    $"Rear gable width {widthPixels}px must be divisible by " +
                    $"{GableFillPixels}px in Structure V1.",
                    section
                );
#endif
                continue;
            }

            float step =
                BuildingGrid.StructuralStep;

            float minX =
                section.GridPosition.x *
                step;

            float maxX =
                (section.GridPosition.x + size.x) *
                step;

            float rearWallBaseY =
                (section.GridPosition.y + size.y) *
                step;

            float wallTopY =
                rearWallBaseY +
                section.WallHeightWorld;

            string key =
                $"{section.StructureLevel}:" +
                $"{Mathf.RoundToInt(minX * BuildingGrid.PixelsPerUnit)}:" +
                $"{Mathf.RoundToInt(maxX * BuildingGrid.PixelsPerUnit)}:" +
                $"{Mathf.RoundToInt(rearWallBaseY * BuildingGrid.PixelsPerUnit)}";

            if (!generatedKeys.Add(key))
                continue;

            BuildRearGableForSection(
                style,
                rearGableRoot,
                section,
                i,
                minX,
                maxX,
                wallTopY,
                widthPixels
            );
        }
    }


    private static bool IsSectionNorthEdgeExterior(
        BuildingSection section,
        BuildingFootprint footprint)
    {
        Vector2Int origin =
            section.GridPosition;

        Vector2Int size =
            section.EffectiveSizeCells;

        int northNeighbourY =
            origin.y +
            size.y;

        for (int x = 0;
             x < size.x;
             x++)
        {
            if (footprint.Contains(
                    section.StructureLevel,
                    origin.x + x,
                    northNeighbourY))
            {
                return false;
            }
        }

        return true;
    }


    private static void BuildRearGableForSection(
        BuildingStructureStyle style,
        Transform rearGableRoot,
        BuildingSection section,
        int sectionIndex,
        float minX,
        float maxX,
        float wallTopY,
        int widthPixels)
    {
        Transform sectionRoot =
            FindOrCreateChild(
                rearGableRoot,
                $"RearGable_{sectionIndex:00}_{section.name}"
            );

        int courseCount =
            widthPixels /
            GableFillPixels;

        float slopeStep =
            GableSlopePixels /
            (float)BuildingGrid.PixelsPerUnit;

        float fillStep =
            GableFillPixels /
            (float)BuildingGrid.PixelsPerUnit;

        int gableSorting =
            style.RearWallSortingOrder +
            1;

        for (int row = 0;
             row < courseCount;
             row++)
        {
            float inset =
                row *
                slopeStep;

            float rowY =
                wallTopY +
                inset;

            float leftX =
                minX +
                inset;

            float rightX =
                maxX -
                inset;

            if (style.RearGableSlope32x32 != null)
            {
                CreateSpritePiece(
                    sectionRoot,
                    $"Slope_L_{row:00}",
                    style.RearGableSlope32x32,
                    new Vector2(
                        leftX,
                        rowY
                    ),
                    StructureSpriteAnchor.BottomLeft,
                    false,
                    false,
                    style.SortingLayerName,
                    gableSorting
                );

                CreateSpritePiece(
                    sectionRoot,
                    $"Slope_R_{row:00}",
                    style.RearGableSlope32x32,
                    new Vector2(
                        rightX,
                        rowY
                    ),
                    StructureSpriteAnchor.BottomRight,
                    true,
                    false,
                    style.SortingLayerName,
                    gableSorting
                );
            }

            int fillCount =
                courseCount -
                row -
                1;

            if (style.RearGableFill64x32 == null)
                continue;

            for (int fillIndex = 0;
                 fillIndex < fillCount;
                 fillIndex++)
            {
                CreateSpritePiece(
                    sectionRoot,
                    $"Fill_{row:00}_{fillIndex:00}",
                    style.RearGableFill64x32,
                    new Vector2(
                        leftX +
                        slopeStep +
                        fillIndex *
                        fillStep,
                        rowY
                    ),
                    StructureSpriteAnchor.BottomLeft,
                    false,
                    false,
                    style.SortingLayerName,
                    gableSorting
                );
            }
        }

        if (style.RearGableBaseTrim32x7 == null)
            return;

        int trimCount =
            widthPixels /
            BuildingGrid.StructuralPixels;

        float trimStep =
            BuildingGrid.StructuralStep;

        for (int i = 0;
             i < trimCount;
             i++)
        {
            /*
             * TopLeft vid wallTop gör att 7px-listen hänger ned över
             * den översta delen av den 96px höga rear wall-spriten.
             */
            CreateSpritePiece(
                sectionRoot,
                $"BaseTrim_{i:00}",
                style.RearGableBaseTrim32x7,
                new Vector2(
                    minX +
                    i *
                    trimStep,
                    wallTopY
                ),
                StructureSpriteAnchor.TopLeft,
                false,
                false,
                style.SortingLayerName,
                gableSorting + 1
            );
        }
    }


    // =========================================================
    // GENERATED ROOTS
    // =========================================================

    private static GeneratedRoots CreateGeneratedRoots(
        Transform structureRoot)
    {
        Transform floors =
            FindOrCreateChild(
                structureRoot,
                FloorsRootName
            );

        Transform walls =
            FindOrCreateChild(
                structureRoot,
                WallsRootName
            );

        Transform shadows =
            FindOrCreateChild(
                structureRoot,
                ShadowsRootName
            );

        Transform pillars =
            FindOrCreateChild(
                structureRoot,
                PillarsRootName
            );

        Transform rearGables =
            FindOrCreateChild(
                structureRoot,
                RearGablesRootName
            );

        return new GeneratedRoots(
            structureRoot,
            floors,
            walls,
            shadows,
            pillars,
            rearGables
        );
    }


    // =========================================================
    // SPRITE PLACEMENT
    // =========================================================

    private static SpriteRenderer CreateSpritePiece(
        Transform parent,
        string objectName,
        Sprite sprite,
        Vector2 targetAnchorLocal,
        StructureSpriteAnchor anchor,
        bool flipX,
        bool flipY,
        string sortingLayerName,
        int sortingOrder)
    {
        if (parent == null ||
            sprite == null)
        {
            return null;
        }

        GameObject pieceObject =
            new GameObject(
                objectName
            );

        Transform pieceTransform =
            pieceObject.transform;

        pieceTransform.SetParent(
            parent,
            false
        );

        SpriteRenderer renderer =
            pieceObject.AddComponent<
                SpriteRenderer>();

        renderer.sprite =
            sprite;

        renderer.flipX =
            flipX;

        renderer.flipY =
            flipY;

        if (!string.IsNullOrWhiteSpace(
                sortingLayerName))
        {
            renderer.sortingLayerName =
                sortingLayerName;
        }

        renderer.sortingOrder =
            sortingOrder;

        pieceTransform.localPosition =
            GetTransformPositionForAnchor(
                sprite,
                targetAnchorLocal,
                anchor,
                flipX,
                flipY
            );

        return renderer;
    }


    private static Vector3 GetTransformPositionForAnchor(
        Sprite sprite,
        Vector2 targetAnchorLocal,
        StructureSpriteAnchor renderedAnchor,
        bool flipX,
        bool flipY)
    {
        Vector2 sourceAnchorPixel =
            GetSourceAnchorPixel(
                sprite,
                renderedAnchor,
                flipX,
                flipY
            );

        Vector2 sourceOffsetFromPivot =
            sourceAnchorPixel -
            sprite.pivot;

        if (flipX)
        {
            sourceOffsetFromPivot.x *=
                -1f;
        }

        if (flipY)
        {
            sourceOffsetFromPivot.y *=
                -1f;
        }

        float ppu =
            Mathf.Max(
                0.0001f,
                sprite.pixelsPerUnit
            );

        Vector2 renderedOffset =
            sourceOffsetFromPivot /
            ppu;

        Vector2 transformLocal =
            targetAnchorLocal -
            renderedOffset;

        return new Vector3(
            transformLocal.x,
            transformLocal.y,
            0f
        );
    }


    private static Vector2 GetSourceAnchorPixel(
        Sprite sprite,
        StructureSpriteAnchor renderedAnchor,
        bool flipX,
        bool flipY)
    {
        Vector2 renderedPixel =
            GetAnchorPixel(
                sprite,
                renderedAnchor
            );

        if (flipX)
        {
            renderedPixel.x =
                sprite.rect.width -
                renderedPixel.x;
        }

        if (flipY)
        {
            renderedPixel.y =
                sprite.rect.height -
                renderedPixel.y;
        }

        return renderedPixel;
    }


    private static Vector2 GetAnchorPixel(
        Sprite sprite,
        StructureSpriteAnchor anchor)
    {
        float width =
            sprite.rect.width;

        float height =
            sprite.rect.height;

        switch (anchor)
        {
            case StructureSpriteAnchor.TopLeft:
                return new Vector2(0f, height);

            case StructureSpriteAnchor.TopCenter:
                return new Vector2(width * 0.5f, height);

            case StructureSpriteAnchor.TopRight:
                return new Vector2(width, height);

            case StructureSpriteAnchor.MiddleLeft:
                return new Vector2(0f, height * 0.5f);

            case StructureSpriteAnchor.Center:
                return new Vector2(width * 0.5f, height * 0.5f);

            case StructureSpriteAnchor.MiddleRight:
                return new Vector2(width, height * 0.5f);

            case StructureSpriteAnchor.BottomLeft:
                return Vector2.zero;

            case StructureSpriteAnchor.BottomCenter:
                return new Vector2(width * 0.5f, 0f);

            case StructureSpriteAnchor.BottomRight:
                return new Vector2(width, 0f);
        }

        return Vector2.zero;
    }


    // =========================================================
    // HELPERS
    // =========================================================

    private static Vector2Int GetPixelPointKey(
        Vector2 localPoint)
    {
        return new Vector2Int(
            Mathf.RoundToInt(
                localPoint.x *
                BuildingGrid.PixelsPerUnit
            ),
            Mathf.RoundToInt(
                localPoint.y *
                BuildingGrid.PixelsPerUnit
            )
        );
    }


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


    private static void ClearChildren(
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

            Object.Destroy(
                child.gameObject
            );
        }
    }
}
