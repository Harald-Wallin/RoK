using TMPro;
using UnityEngine;

/// <summary>
/// Roof planner för RöKs två separata takgrammatiker.
///
/// EastWest:
/// - behåller det redan gröna 64px/32px-systemet.
///
/// NorthSouth:
/// - två diagonala takfall runt en N/S-ridge.
/// - 64x128 main roof sprites.
/// - varje nytt slope-course går 32px utåt + 32px nedåt.
/// - 64px progression längs ridgen.
/// - procedural frontgavel av 64x32 fill + 32x32 slope wedges.
/// - modulära windboards.
///
/// Alla roof pieces genereras på BuildingSections WallHeightWorld.
/// </summary>
public static class BuildingRoofPlanner
{
    private const int SortingStridePerRow = 10;
    private const int GableOrnamentDropPixels = 13;

    // N/S artwork contract.
    private const int NorthSouthTileLengthPixels = 64;
    private const int NorthSouthSlopeAdvancePixels = 32;
    private const int NorthSouthRidgeEndOverhangPixels = 2;
    private const int NorthSouthWindboardEdgeInsetPixels = 8;
    private const int NorthSouthBottomOverhangAnchorHeightPixels = 16;


    // =========================================================
    // ENTRY
    // =========================================================

    public static BuildingRoofBuildPlan BuildPlan(
        BuildingSection section)
    {
        BuildingRoofBuildPlan plan = new();

        if (section == null)
        {
            plan.Fail("Section is null.");
            return plan;
        }

        BuildingSectionRoofSettings roof =
            section.RoofSettings;

        if (roof == null ||
            !roof.Enabled)
        {
            plan.Fail("Roof generation is disabled.");
            return plan;
        }

        BuildingRoofProfile profile =
            roof.Profile;

        if (profile == null)
        {
            plan.Fail("No BuildingRoofProfile assigned.");
            return plan;
        }

        if (roof.Direction ==
            BuildingRoofDirection.EastWest)
        {
            BuildEastWestRoof(
                plan,
                section,
                roof,
                profile.EastWest
            );
        }
        else
        {
            BuildNorthSouthRoof(
                plan,
                section,
                roof,
                profile.NorthSouth
            );
        }

        return plan;
    }


    // =========================================================
    // EAST-WEST ROOF
    // =========================================================

    private static void BuildEastWestRoof(
        BuildingRoofBuildPlan plan,
        BuildingSection section,
        BuildingSectionRoofSettings roof,
        BuildingRoofArtSet art)
    {
        if (art == null ||
            !art.SurfaceVariants.HasSprites)
        {
            plan.Fail("No East-West main roof sprites assigned.");
            return;
        }

        Vector2Int sectionSize =
            section.EffectiveSizeCells;

        int repeatSpanPixels =
            sectionSize.x *
            BuildingGrid.StructuralPixels;

        int rowSpanPixels =
            sectionSize.y *
            BuildingGrid.StructuralPixels;

        if (repeatSpanPixels %
            art.TileLengthPixels != 0)
        {
            plan.Fail(
                $"East-West roof span {repeatSpanPixels}px cannot be filled by " +
                $"{art.TileLengthPixels}px roof tiles."
            );

            return;
        }

        if (rowSpanPixels %
            art.RowAdvancePixels != 0)
        {
            plan.Fail(
                $"East-West roof depth {rowSpanPixels}px is not divisible by " +
                $"row advance {art.RowAdvancePixels}px."
            );

            return;
        }

        int columnCount =
            repeatSpanPixels /
            art.TileLengthPixels;

        int rowCount =
            rowSpanPixels /
            art.RowAdvancePixels;

        if (columnCount <= 0 ||
            rowCount <= 0)
        {
            plan.Fail("East-West roof has no tiles.");
            return;
        }

        ResolveEastWestBasis(
            section,
            out Vector2 origin,
            out Vector2 repeatDirection,
            out Vector2 rowDirection
        );

        float tileStep =
            art.TileLengthPixels /
            (float)BuildingGrid.PixelsPerUnit;

        float rowStep =
            art.RowAdvancePixels /
            (float)BuildingGrid.PixelsPerUnit;

        for (int row = 0;
             row < rowCount;
             row++)
        {
            int rowSortingOrder =
                art.BaseSortingOrder +
                (
                    rowCount -
                    1 -
                    row
                ) *
                SortingStridePerRow;

            for (int column = 0;
                 column < columnCount;
                 column++)
            {
                Vector2 tileStart =
                    origin +
                    repeatDirection *
                    (
                        column *
                        tileStep
                    ) +
                    rowDirection *
                    (
                        row *
                        rowStep
                    );

                Sprite surfaceSprite =
                    art.SurfaceVariants
                        .GetDeterministic(
                            CreateStableHash(
                                section,
                                roof.VariationSeed,
                                row,
                                column,
                                101
                            )
                        );

                if (surfaceSprite == null)
                    continue;

                plan.Add(
                    new BuildingRoofBuildPiece(
                        BuildingRoofPieceRole.Surface,
                        surfaceSprite,
                        tileStart,
                        BuildingSpriteAnchor.TopLeft,
                        false,
                        false,
                        rowSortingOrder
                    )
                );

                bool bottomRow =
                    row ==
                    rowCount - 1;

                // Left edge.
                if (column == 0)
                {
                    BuildingSpriteVariantSet variants =
                        bottomRow
                            ? art.SideOverhangBottomVariants
                            : art.SideOverhangVariants;

                    Sprite sideSprite =
                        variants.GetDeterministic(
                            CreateStableHash(
                                section,
                                roof.VariationSeed,
                                row,
                                column,
                                201
                            )
                        );

                    if (sideSprite != null)
                    {
                        plan.Add(
                            new BuildingRoofBuildPiece(
                                bottomRow
                                    ? BuildingRoofPieceRole.SideOverhangBottom
                                    : BuildingRoofPieceRole.SideOverhang,
                                sideSprite,
                                tileStart,
                                BuildingSpriteAnchor.TopRight,
                                false,
                                false,
                                rowSortingOrder + 1
                            )
                        );
                    }
                }

                // Right edge.
                if (column ==
                    columnCount - 1)
                {
                    BuildingSpriteVariantSet variants =
                        bottomRow
                            ? art.SideOverhangBottomVariants
                            : art.SideOverhangVariants;

                    Sprite sideSprite =
                        variants.GetDeterministic(
                            CreateStableHash(
                                section,
                                roof.VariationSeed,
                                row,
                                column,
                                202
                            )
                        );

                    if (sideSprite != null)
                    {
                        Vector2 tileEnd =
                            tileStart +
                            repeatDirection *
                            tileStep;

                        plan.Add(
                            new BuildingRoofBuildPiece(
                                bottomRow
                                    ? BuildingRoofPieceRole.SideOverhangBottom
                                    : BuildingRoofPieceRole.SideOverhang,
                                sideSprite,
                                tileEnd,
                                BuildingSpriteAnchor.TopLeft,
                                true,
                                false,
                                rowSortingOrder + 1
                            )
                        );
                    }
                }

                // Front/bottom roof edge.
                if (bottomRow &&
                    art.FrontOverhangVariants.HasSprites)
                {
                    Sprite frontSprite =
                        art.FrontOverhangVariants
                            .GetDeterministic(
                                CreateStableHash(
                                    section,
                                    roof.VariationSeed,
                                    row,
                                    column,
                                    301
                                )
                            );

                    if (frontSprite != null)
                    {
                        float ppu =
                            Mathf.Max(
                                0.0001f,
                                surfaceSprite.pixelsPerUnit
                            );

                        Vector2 target =
                            tileStart +
                            Vector2.down *
                            (
                                surfaceSprite.rect.height /
                                ppu
                            );

                        plan.Add(
                            new BuildingRoofBuildPiece(
                                BuildingRoofPieceRole.FrontOverhang,
                                frontSprite,
                                target,
                                BuildingSpriteAnchor.TopLeft,
                                false,
                                false,
                                rowSortingOrder + 1
                            )
                        );
                    }
                }
            }
        }

        BuildEastWestRidge(
            plan,
            section,
            roof,
            art,
            origin,
            repeatDirection,
            rowDirection,
            columnCount,
            tileStep,
            rowCount
        );

        BuildEastWestGableOrnaments(
            plan,
            section,
            roof,
            art,
            origin,
            repeatDirection,
            rowDirection,
            columnCount,
            tileStep,
            rowCount
        );
    }


    private static void ResolveEastWestBasis(
        BuildingSection section,
        out Vector2 origin,
        out Vector2 repeatDirection,
        out Vector2 rowDirection)
    {
        Vector2Int grid =
            section.GridPosition;

        Vector2Int size =
            section.EffectiveSizeCells;

        float step =
            BuildingGrid.StructuralStep;

        origin =
            new Vector2(
                grid.x * step,
                (grid.y + size.y) * step +
                section.WallHeightWorld
            );

        repeatDirection =
            Vector2.right;

        rowDirection =
            Vector2.down;
    }


    private static void BuildEastWestRidge(
        BuildingRoofBuildPlan plan,
        BuildingSection section,
        BuildingSectionRoofSettings roof,
        BuildingRoofArtSet art,
        Vector2 origin,
        Vector2 repeatDirection,
        Vector2 rowDirection,
        int columnCount,
        float tileStep,
        int rowCount)
    {
        if (!art.RidgeVariants.HasSprites)
            return;

        int topSurfaceSorting =
            art.BaseSortingOrder +
            (rowCount - 1) *
            SortingStridePerRow;

        Vector2 inset =
            rowDirection *
            (
                art.RidgeOverlapPixels /
                (float)BuildingGrid.PixelsPerUnit
            );

        for (int column = 0;
             column < columnCount;
             column++)
        {
            Sprite ridgeSprite =
                art.RidgeVariants
                    .GetDeterministic(
                        CreateStableHash(
                            section,
                            roof.VariationSeed,
                            0,
                            column,
                            401
                        )
                    );

            if (ridgeSprite == null)
                continue;

            Vector2 target =
                origin +
                repeatDirection *
                (
                    column *
                    tileStep
                ) +
                inset;

            plan.Add(
                new BuildingRoofBuildPiece(
                    BuildingRoofPieceRole.Ridge,
                    ridgeSprite,
                    target,
                    BuildingSpriteAnchor.BottomLeft,
                    false,
                    false,
                    topSurfaceSorting - 1
                )
            );
        }
    }


    private static void BuildEastWestGableOrnaments(
        BuildingRoofBuildPlan plan,
        BuildingSection section,
        BuildingSectionRoofSettings roof,
        BuildingRoofArtSet art,
        Vector2 origin,
        Vector2 repeatDirection,
        Vector2 rowDirection,
        int columnCount,
        float tileStep,
        int rowCount)
    {
        if (!art.GableOrnamentVariants.HasSprites)
            return;

        Sprite ornament =
            art.GableOrnamentVariants
                .GetDeterministic(
                    CreateStableHash(
                        section,
                        roof.VariationSeed,
                        0,
                        0,
                        501
                    )
                );

        if (ornament == null)
            return;

        int topSurfaceSorting =
            art.BaseSortingOrder +
            (rowCount - 1) *
            SortingStridePerRow;

        Vector2 ridgeInset =
            rowDirection *
            (
                art.RidgeOverlapPixels /
                (float)BuildingGrid.PixelsPerUnit
            );

        Vector2 ornamentDrop =
            Vector2.down *
            (
                GableOrnamentDropPixels /
                (float)BuildingGrid.PixelsPerUnit
            );

        Vector2 ridgeStart =
            origin +
            ridgeInset +
            ornamentDrop;

        Vector2 ridgeEnd =
            origin +
            repeatDirection *
            (
                columnCount *
                tileStep
            ) +
            ridgeInset +
            ornamentDrop;

        plan.Add(
            new BuildingRoofBuildPiece(
                BuildingRoofPieceRole.GableOrnament,
                ornament,
                ridgeStart,
                BuildingSpriteAnchor.BottomRight,
                false,
                false,
                topSurfaceSorting - 1
            )
        );

        plan.Add(
            new BuildingRoofBuildPiece(
                BuildingRoofPieceRole.GableOrnament,
                ornament,
                ridgeEnd,
                BuildingSpriteAnchor.BottomLeft,
                true,
                false,
                topSurfaceSorting - 1
            )
        );
    }


    // =========================================================
    // NORTH-SOUTH ROOF
    // =========================================================

    private static void BuildNorthSouthRoof(
    BuildingRoofBuildPlan plan,
    BuildingSection section,
    BuildingSectionRoofSettings roof,
    BuildingNorthSouthRoofArtSet art)
    {
        if (art == null ||
            !art.SurfaceVariants.HasSprites)
        {
            plan.Fail(
                "No North-South main roof sprites assigned."
            );

            return;
        }


        Vector2Int size =
            section.EffectiveSizeCells;


        int widthPixels =
            size.x *
            BuildingGrid.StructuralPixels;


        int depthPixels =
            size.y *
            BuildingGrid.StructuralPixels;


        if (widthPixels %
            NorthSouthTileLengthPixels != 0)
        {
            plan.Fail(
                $"North-South roof width {widthPixels}px must be divisible by " +
                $"{NorthSouthTileLengthPixels}px."
            );

            return;
        }


        if (depthPixels %
            NorthSouthTileLengthPixels != 0)
        {
            plan.Fail(
                $"North-South roof depth {depthPixels}px must be divisible by " +
                $"{NorthSouthTileLengthPixels}px."
            );

            return;
        }


        // =========================================================
        // COUNTS
        // =========================================================

        /*
         * Gaveln använder fortfarande 32px höjd per 64px
         * total byggnadsbredd.
         */
        int gableCourseCount =
            widthPixels /
            NorthSouthTileLengthPixels;


        /*
         * Main-roof behöver endast täcka halva byggnadsbredden.
         * Generatorn använder minsta antal 64px roof sprites
         * som faktiskt krävs.
         */
        int halfWidthPixels =
            widthPixels /
            2;


        int roofSlopeTileCount =
            GetNorthSouthSlopeTileCount(
                halfWidthPixels
            );


        int ridgeSegmentCount =
            depthPixels /
            NorthSouthTileLengthPixels;


        if (gableCourseCount <= 0 ||
            roofSlopeTileCount <= 0 ||
            ridgeSegmentCount <= 0)
        {
            plan.Fail(
                "North-South roof has no tiles."
            );

            return;
        }


        // =========================================================
        // GEOMETRY
        // =========================================================

        ResolveNorthSouthGeometry(
            section,
            widthPixels,
            out float minX,
            out float maxX,
            out float frontWallY,
            out float backWallY,
            out Vector2 ridgeFront
        );


        float ppu =
            BuildingGrid.PixelsPerUnit;


        float tileStep =
            NorthSouthTileLengthPixels /
            ppu;


        float halfWidthWorld =
            halfWidthPixels /
            ppu;


        /*
         * Rear ridge är exakt samma roof-peak som front,
         * fast vid byggnadens bakersta vägg.
         */
        Vector2 ridgeBack =
            new Vector2(
                (minX + maxX) * 0.5f,
                backWallY + halfWidthWorld
            );


        int outermostOffsetPixels =
            Mathf.Max(
                0,
                halfWidthPixels -
                NorthSouthTileLengthPixels
            );


        int outermostDepthUnits =
            outermostOffsetPixels /
            NorthSouthSlopeAdvancePixels;


        int maxSurfaceSorting =
            GetNorthSouthSurfaceSorting(
                art.BaseSortingOrder,
                0,
                outermostDepthUnits,
                ridgeSegmentCount
            );


        // =========================================================
        // MAIN ROOF + RIDGE
        // =========================================================

        for (int segment = 0;
             segment < ridgeSegmentCount;
             segment++)
        {
            Vector2 ridgeSegmentStart =
                ridgeFront +
                Vector2.up *
                (
                    segment *
                    tileStep
                );


            for (int roofTileIndex = 0;
                 roofTileIndex < roofSlopeTileCount;
                 roofTileIndex++)
            {
                int outwardPixels =
                    GetNorthSouthSlopeTileOffsetPixels(
                        roofTileIndex,
                        halfWidthPixels
                    );


                float outward =
                    outwardPixels /
                    ppu;


                Vector2 leftTarget =
                    ridgeSegmentStart +
                    new Vector2(
                        -outward,
                        -outward
                    );


                Vector2 rightTarget =
                    ridgeSegmentStart +
                    new Vector2(
                        outward,
                        -outward
                    );


                int slopeDepthUnits =
                    outwardPixels /
                    NorthSouthSlopeAdvancePixels;


                int sortingOrder =
                    GetNorthSouthSurfaceSorting(
                        art.BaseSortingOrder,
                        segment,
                        slopeDepthUnits,
                        ridgeSegmentCount
                    );


                Sprite surfaceSprite =
                    art.SurfaceVariants
                        .GetDeterministic(
                            CreateStableHash(
                                section,
                                roof.VariationSeed,
                                segment,
                                roofTileIndex,
                                601
                            )
                        );


                if (surfaceSprite == null)
                    continue;


                // LEFT ROOF SLOPE
                plan.Add(
                    new BuildingRoofBuildPiece(
                        BuildingRoofPieceRole.Surface,
                        surfaceSprite,
                        leftTarget,
                        BuildingSpriteAnchor.MiddleRight,
                        false,
                        false,
                        sortingOrder
                    )
                );


                // RIGHT ROOF SLOPE
                plan.Add(
                    new BuildingRoofBuildPiece(
                        BuildingRoofPieceRole.Surface,
                        surfaceSprite,
                        rightTarget,
                        BuildingSpriteAnchor.MiddleLeft,
                        true,
                        false,
                        sortingOrder
                    )
                );


                /*
                 * Roof bottom overhang endast på den yttersta
                 * main-roof-spriten.
                 */
                if (roofTileIndex ==
                    roofSlopeTileCount - 1)
                {
                    BuildNorthSouthOuterOverhangPair(
                        plan,
                        section,
                        roof,
                        art,
                        surfaceSprite,
                        leftTarget,
                        rightTarget,
                        sortingOrder,
                        segment,
                        roofTileIndex
                    );
                }
            }


            BuildNorthSouthRidgeSegment(
                plan,
                section,
                roof,
                art,
                ridgeSegmentStart,
                segment,
                ridgeSegmentCount
            );
        }


        // =========================================================
        // REAR WINDBOARDS
        // =========================================================

        /*
         * Baksidan får INGEN gavel.
         *
         * Däremot får den exakt samma WindboardTop / Middle /
         * Bottom-geometri som framsidan.
         *
         * Windboard-metoden använder:
         *   sortingOrder     för Middle
         *   sortingOrder + 1 för Bottom
         *   sortingOrder + 2 för Top
         *
         * Den bakersta roof-spritens lägsta sorting är
         * art.BaseSortingOrder.
         *
         * Därför startar rear windboards på Base - 3:
         *
         * Middle = Base - 3
         * Bottom = Base - 2
         * Top    = Base - 1
         *
         * ALLT ligger alltså bakom själva taket.
         */
        int rearWindboardSorting =
            art.BaseSortingOrder -
            3;


        BuildNorthSouthWindboardsForEnd(
            plan,
            section,
            roof,
            art,
            minX,
            maxX,
            backWallY,
            ridgeBack,
            gableCourseCount,
            rearWindboardSorting,
            true
        );


        // =========================================================
        // FRONT GABLE + FRONT WINDBOARDS
        // =========================================================

        BuildNorthSouthFrontGable(
            plan,
            section,
            roof,
            art,
            minX,
            maxX,
            frontWallY,
            ridgeFront,
            gableCourseCount,
            maxSurfaceSorting
        );
    }

    private static int GetNorthSouthSlopeTileCount(
    int halfWidthPixels)
    {
        if (halfWidthPixels <= 0)
            return 0;


        return Mathf.CeilToInt(
            halfWidthPixels /
            (float)NorthSouthTileLengthPixels
        );
    }


    private static int GetNorthSouthSlopeTileOffsetPixels(
        int tileIndex,
        int halfWidthPixels)
    {
        /*
         * Main-roof-spriten täcker själv 64px från sin anchor
         * ut mot takfoten.
         *
         * Därför ska den sista spritens anchor ligga:
         *
         * halfWidth - 64
         *
         * från ridgen.
         *
         * Vi använder annars 64px steg. Om sista steget bara
         * behöver vara 32px uppstår automatiskt exakt en
         * 32px-overlap.
         */

        int maximumOffset =
            Mathf.Max(
                0,
                halfWidthPixels -
                NorthSouthTileLengthPixels
            );


        return Mathf.Min(
            tileIndex *
            NorthSouthTileLengthPixels,
            maximumOffset
        );
    }


    private static void ResolveNorthSouthGeometry(
        BuildingSection section,
        int widthPixels,
        out float minX,
        out float maxX,
        out float frontWallY,
        out float backWallY,
        out Vector2 ridgeFront)
    {
        Vector2Int grid =
            section.GridPosition;

        Vector2Int size =
            section.EffectiveSizeCells;

        float step =
            BuildingGrid.StructuralStep;

        minX =
            grid.x *
            step;

        maxX =
            (grid.x + size.x) *
            step;

        frontWallY =
            grid.y *
            step +
            section.WallHeightWorld;

        backWallY =
            (grid.y + size.y) *
            step +
            section.WallHeightWorld;

        float halfWidthWorld =
            (widthPixels * 0.5f) /
            BuildingGrid.PixelsPerUnit;

        ridgeFront =
            new Vector2(
                (minX + maxX) * 0.5f,
                frontWallY +
                halfWidthWorld
            );
    }


    private static int GetNorthSouthSurfaceSorting(
    int baseSortingOrder,
    int segment,
    int slopeDepthUnits,
    int ridgeSegmentCount)
    {
        /*
         * Ett N/S main tile flyttas:
         *
         * - 64px ned per ridge-segment mot fronten
         *   = två 32px depth-units.
         *
         * - slopeDepthUnits kommer från den FAKTISKA
         *   diagonala offseten på takfallet.
         *
         * Därför fungerar sorting både när roof pieces:
         *
         * - ligger kant mot kant med 64px steg
         * - överlappar med 32px steg
         */

        int frontDepthUnits =
            (
                ridgeSegmentCount -
                1 -
                segment
            ) *
            2 +
            slopeDepthUnits;


        return
            baseSortingOrder +
            frontDepthUnits *
            SortingStridePerRow;
    }


    private static void BuildNorthSouthRidgeSegment(
        BuildingRoofBuildPlan plan,
        BuildingSection section,
        BuildingSectionRoofSettings roof,
        BuildingNorthSouthRoofArtSet art,
        Vector2 ridgeSegmentStart,
        int segment,
        int ridgeSegmentCount)
    {
        if (!art.RidgeVariants.HasSprites)
            return;

        Sprite ridgeSprite =
            art.RidgeVariants
                .GetDeterministic(
                    CreateStableHash(
                        section,
                        roof.VariationSeed,
                        segment,
                        0,
                        701
                    )
                );

        if (ridgeSprite == null)
            return;

        Vector2 target =
            ridgeSegmentStart +
            Vector2.down *
            (
                NorthSouthRidgeEndOverhangPixels /
                (float)BuildingGrid.PixelsPerUnit
            );

        int sortingOrder =
            GetNorthSouthSurfaceSorting(
                art.BaseSortingOrder,
                segment,
                0,
                ridgeSegmentCount
            ) +
            2;

        /*
         * Ridge = 6x68.
         * BottomCenter placeras 2px framför segmentets 64px start.
         * Därmed får ridgen automatiskt 2px överhäng i båda ändar
         * och 3px täckning på respektive taksida.
         */
        plan.Add(
            new BuildingRoofBuildPiece(
                BuildingRoofPieceRole.Ridge,
                ridgeSprite,
                target,
                BuildingSpriteAnchor.BottomCenter,
                false,
                false,
                sortingOrder
            )
        );
    }


    private static void BuildNorthSouthOuterOverhangPair(
        BuildingRoofBuildPlan plan,
        BuildingSection section,
        BuildingSectionRoofSettings roof,
        BuildingNorthSouthRoofArtSet art,
        Sprite surfaceSprite,
        Vector2 leftSurfaceTarget,
        Vector2 rightSurfaceTarget,
        int surfaceSorting,
        int segment,
        int course)
    {
        if (!art.BottomOverhangVariants.HasSprites)
            return;

        Sprite overhangSprite =
            art.BottomOverhangVariants
                .GetDeterministic(
                    CreateStableHash(
                        section,
                        roof.VariationSeed,
                        segment,
                        course,
                        801
                    )
                );

        if (overhangSprite == null)
            return;

        float surfacePpu =
            Mathf.Max(
                0.0001f,
                surfaceSprite.pixelsPerUnit
            );

        float widthWorld =
            surfaceSprite.rect.width /
            surfacePpu;

        float halfHeightWorld =
            surfaceSprite.rect.height *
            0.5f /
            surfacePpu;

        // Main roof left piece is anchored at its rendered MiddleRight.
        Vector2 leftRoofOuterBottom =
            leftSurfaceTarget +
            new Vector2(
                -widthWorld,
                -halfHeightWorld
            );

        // Mirrored right piece is anchored at its rendered MiddleLeft.
        Vector2 rightRoofOuterBottom =
            rightSurfaceTarget +
            new Vector2(
                widthWorld,
                -halfHeightWorld
            );

        Vector2 overhangAnchorOffset =
            new Vector2(
                0f,
                NorthSouthBottomOverhangAnchorHeightPixels
            );

        /*
         * Source artwork är vänster sida:
         * lower-right attachment point ligger 16px ovanför PNG-botten.
         */
        plan.Add(
            new BuildingRoofBuildPiece(
                BuildingRoofPieceRole.FrontOverhang,
                overhangSprite,
                leftRoofOuterBottom,
                BuildingSpriteAnchor.BottomRight,
                false,
                false,
                surfaceSorting + 1,
                overhangAnchorOffset
            )
        );

        plan.Add(
            new BuildingRoofBuildPiece(
                BuildingRoofPieceRole.FrontOverhang,
                overhangSprite,
                rightRoofOuterBottom,
                BuildingSpriteAnchor.BottomLeft,
                true,
                false,
                surfaceSorting + 1,
                overhangAnchorOffset
            )
        );
    }


    // =========================================================
    // NORTH-SOUTH PROCEDURAL FRONT GABLE
    // =========================================================

    private static void BuildNorthSouthFrontGable(
    BuildingRoofBuildPlan plan,
    BuildingSection section,
    BuildingSectionRoofSettings roof,
    BuildingNorthSouthRoofArtSet art,
    float minX,
    float maxX,
    float frontWallY,
    Vector2 ridgeFront,
    int slopeCourseCount,
    int maxSurfaceSorting)
    {
        int gableSorting =
            maxSurfaceSorting +
            3;


        // =========================================================
        // FRONT GABLE FILL
        // =========================================================

        BuildNorthSouthGableFill(
            plan,
            section,
            roof,
            art,
            minX,
            maxX,
            frontWallY,
            slopeCourseCount,
            gableSorting
        );


        // =========================================================
        // FRONT GABLE BOTTOM OVERHANG
        // =========================================================

        BuildNorthSouthGableBottomOverhang(
            plan,
            section,
            roof,
            art,
            minX,
            frontWallY,
            slopeCourseCount,
            gableSorting + 1
        );


        // =========================================================
        // FRONT WINDBOARDS
        // =========================================================

        BuildNorthSouthWindboardsForEnd(
            plan,
            section,
            roof,
            art,
            minX,
            maxX,
            frontWallY,
            ridgeFront,
            slopeCourseCount,
            gableSorting + 2,
            false
        );
    }


    private static void BuildNorthSouthGableFill(
        BuildingRoofBuildPlan plan,
        BuildingSection section,
        BuildingSectionRoofSettings roof,
        BuildingNorthSouthRoofArtSet art,
        float minX,
        float maxX,
        float frontWallY,
        int slopeCourseCount,
        int sortingOrder)
    {
        if (!art.GableSlopeVariants.HasSprites)
            return;

        float halfStep =
            NorthSouthSlopeAdvancePixels /
            (float)BuildingGrid.PixelsPerUnit;

        float fillStep =
            NorthSouthTileLengthPixels /
            (float)BuildingGrid.PixelsPerUnit;

        for (int row = 0;
             row < slopeCourseCount;
             row++)
        {
            float rowInset =
                row *
                halfStep;

            float rowY =
                frontWallY +
                rowInset;

            float leftX =
                minX +
                rowInset;

            float rightX =
                maxX -
                rowInset;

            Sprite slopeSprite =
                art.GableSlopeVariants
                    .GetDeterministic(
                        CreateStableHash(
                            section,
                            roof.VariationSeed,
                            row,
                            0,
                            901
                        )
                    );

            if (slopeSprite != null)
            {
                // Source = vänster 45-degree wedge.
                plan.Add(
                    new BuildingRoofBuildPiece(
                        BuildingRoofPieceRole.GableSlope,
                        slopeSprite,
                        new Vector2(
                            leftX,
                            rowY
                        ),
                        BuildingSpriteAnchor.BottomLeft,
                        false,
                        false,
                        sortingOrder
                    )
                );

                // Right wedge uses same sprite mirrored.
                plan.Add(
                    new BuildingRoofBuildPiece(
                        BuildingRoofPieceRole.GableSlope,
                        slopeSprite,
                        new Vector2(
                            rightX,
                            rowY
                        ),
                        BuildingSpriteAnchor.BottomRight,
                        true,
                        false,
                        sortingOrder
                    )
                );
            }

            int fillCount =
                slopeCourseCount -
                row -
                1;

            if (fillCount <= 0 ||
                !art.GableFillVariants.HasSprites)
            {
                continue;
            }

            Vector2 fillStart =
                new Vector2(
                    leftX +
                    halfStep,
                    rowY
                );

            for (int fillIndex = 0;
                 fillIndex < fillCount;
                 fillIndex++)
            {
                Sprite fillSprite =
                    art.GableFillVariants
                        .GetDeterministic(
                            CreateStableHash(
                                section,
                                roof.VariationSeed,
                                row,
                                fillIndex,
                                902
                            )
                        );

                if (fillSprite == null)
                    continue;

                plan.Add(
                    new BuildingRoofBuildPiece(
                        BuildingRoofPieceRole.GableFill,
                        fillSprite,
                        fillStart +
                        Vector2.right *
                        (
                            fillIndex *
                            fillStep
                        ),
                        BuildingSpriteAnchor.BottomLeft,
                        false,
                        false,
                        sortingOrder
                    )
                );
            }
        }
    }


    private static void BuildNorthSouthGableBottomOverhang(
        BuildingRoofBuildPlan plan,
        BuildingSection section,
        BuildingSectionRoofSettings roof,
        BuildingNorthSouthRoofArtSet art,
        float minX,
        float frontWallY,
        int slopeCourseCount,
        int sortingOrder)
    {
        if (!art.GableBottomOverhangVariants.HasSprites)
            return;

        float moduleStep =
            NorthSouthTileLengthPixels /
            (float)BuildingGrid.PixelsPerUnit;

        for (int i = 0;
             i < slopeCourseCount;
             i++)
        {
            Sprite sprite =
                art.GableBottomOverhangVariants
                    .GetDeterministic(
                        CreateStableHash(
                            section,
                            roof.VariationSeed,
                            0,
                            i,
                            903
                        )
                    );

            if (sprite == null)
                continue;

            plan.Add(
                new BuildingRoofBuildPiece(
                    BuildingRoofPieceRole.GableBottomOverhang,
                    sprite,
                    new Vector2(
                        minX +
                        i *
                        moduleStep,
                        frontWallY
                    ),
                    BuildingSpriteAnchor.TopLeft,
                    false,
                    false,
                    sortingOrder
                )
            );
        }
    }


    private static void BuildNorthSouthWindboardsForEnd(
    BuildingRoofBuildPlan plan,
    BuildingSection section,
    BuildingSectionRoofSettings roof,
    BuildingNorthSouthRoofArtSet art,
    float minX,
    float maxX,
    float wallY,
    Vector2 ridgePoint,
    int slopeCourseCount,
    int sortingOrder,
    bool rear)
    {
        BuildingRoofPieceRole middleRole =
    rear
        ? BuildingRoofPieceRole.RearWindboardMiddle
        : BuildingRoofPieceRole.WindboardMiddle;

        BuildingRoofPieceRole bottomRole =
            rear
                ? BuildingRoofPieceRole.RearWindboardBottom
                : BuildingRoofPieceRole.WindboardBottom;

        BuildingRoofPieceRole topRole =
            rear
                ? BuildingRoofPieceRole.RearWindboardTop
                : BuildingRoofPieceRole.WindboardTop;

        Vector2 leftEave =
            new Vector2(
                minX,
                wallY
            );


        Vector2 rightEave =
            new Vector2(
                maxX,
                wallY
            );


        /*
         * slopeCourseCount är gavelns logiska course-count.
         *
         * Varje course motsvarar 32px på en sida av taket.
         */
        int halfWidthPixels =
            slopeCourseCount *
            NorthSouthSlopeAdvancePixels;


        int roofSlopeTileCount =
            GetNorthSouthSlopeTileCount(
                halfWidthPixels
            );


        // =========================================================
        // MIDDLE
        // =========================================================

        /*
         * En WindboardMiddle per faktisk main-roof-sprite.
         *
         * Vi bygger från eave mot ridge och använder exakt samma
         * roof-layout som main roof använder.
         */
        if (art.WindboardMiddleVariants.HasSprites)
        {
            for (int visualIndex = 0;
                 visualIndex < roofSlopeTileCount;
                 visualIndex++)
            {
                int roofTileIndex =
                    roofSlopeTileCount -
                    1 -
                    visualIndex;


                int outwardOffsetPixels =
                    GetNorthSouthSlopeTileOffsetPixels(
                        roofTileIndex,
                        halfWidthPixels
                    );


                int tileOuterEdgeDistanceFromRidge =
                    outwardOffsetPixels +
                    NorthSouthTileLengthPixels;


                int inwardFromEavePixels =
                    Mathf.Max(
                        0,
                        halfWidthPixels -
                        tileOuterEdgeDistanceFromRidge
                    );


                float inward =
                    inwardFromEavePixels /
                    (float)BuildingGrid.PixelsPerUnit;


                Sprite middleSprite =
                    art.WindboardMiddleVariants
                        .GetDeterministic(
                            CreateStableHash(
                                section,
                                roof.VariationSeed,
                                visualIndex,
                                0,
                                1001
                            )
                        );


                if (middleSprite == null)
                    continue;


                Vector2 leftTarget =
                    leftEave +
                    new Vector2(
                        inward,
                        inward
                    );


                Vector2 rightTarget =
                    rightEave +
                    new Vector2(
                        -inward,
                        inward
                    );


                // LEFT
                plan.Add(
                    new BuildingRoofBuildPiece(
                        middleRole,
                        middleSprite,
                        leftTarget,
                        BuildingSpriteAnchor.BottomLeft,
                        false,
                        false,
                        sortingOrder,
                        new Vector2(
                            NorthSouthWindboardEdgeInsetPixels,
                            0f
                        )
                    )
                );


                // RIGHT
                plan.Add(
                    new BuildingRoofBuildPiece(
                        middleRole,
                        middleSprite,
                        rightTarget,
                        BuildingSpriteAnchor.BottomRight,
                        true,
                        false,
                        sortingOrder,
                        new Vector2(
                            -NorthSouthWindboardEdgeInsetPixels,
                            0f
                        )
                    )
                );
            }
        }


        // =========================================================
        // BOTTOM CAPS
        // =========================================================

        /*
         * Bottom-cap sitter vid takets yttersta/eave-endpoint och
         * sticker vidare ut från taket.
         */
        if (art.WindboardBottomVariants.HasSprites)
        {
            Sprite bottomSprite =
                art.WindboardBottomVariants
                    .GetDeterministic(
                        CreateStableHash(
                            section,
                            roof.VariationSeed,
                            0,
                            0,
                            1002
                        )
                    );


            if (bottomSprite != null)
            {
                // LEFT
                plan.Add(
                    new BuildingRoofBuildPiece(
                        bottomRole,
                        bottomSprite,
                        leftEave,
                        BuildingSpriteAnchor.TopRight,
                        false,
                        false,
                        sortingOrder + 1
                    )
                );


                // RIGHT
                plan.Add(
                    new BuildingRoofBuildPiece(
                        bottomRole,
                        bottomSprite,
                        rightEave,
                        BuildingSpriteAnchor.TopLeft,
                        true,
                        false,
                        sortingOrder + 1
                    )
                );
            }
        }


        // =========================================================
        // TOP CAPS
        // =========================================================

        /*
         * Top-caps korsar varandra till ett X.
         *
         * VIKTIGT:
         * Vi behåller den verifierade 8px-korrigeringen:
         *
         * NorthSouthWindboardEdgeInsetPixels * 1f
         *
         * På grund av artworkets anchor/flip-semantik flyttas:
         *
         * LEFT source åt vänster
         * RIGHT mirrored source åt höger
         *
         * vilket visuellt för topparna IN mot varandra.
         */
        if (art.WindboardTopVariants.HasSprites)
        {
            Sprite topSprite =
                art.WindboardTopVariants
                    .GetDeterministic(
                        CreateStableHash(
                            section,
                            roof.VariationSeed,
                            0,
                            0,
                            1003
                        )
                    );


            if (topSprite != null)
            {
                float topHorizontalShift =
                    (
                        NorthSouthWindboardEdgeInsetPixels *
                        1f
                    ) /
                    BuildingGrid.PixelsPerUnit;


                Vector2 leftTopTarget =
                    ridgePoint +
                    Vector2.left *
                    topHorizontalShift;


                Vector2 rightTopTarget =
                    ridgePoint +
                    Vector2.right *
                    topHorizontalShift;


                // LEFT
                plan.Add(
                    new BuildingRoofBuildPiece(
                        topRole,
                        topSprite,
                        leftTopTarget,
                        BuildingSpriteAnchor.BottomLeft,
                        false,
                        false,
                        sortingOrder + 2
                    )
                );


                // RIGHT
                plan.Add(
                    new BuildingRoofBuildPiece(
                        topRole,
                        topSprite,
                        rightTopTarget,
                        BuildingSpriteAnchor.BottomRight,
                        true,
                        false,
                        sortingOrder + 2
                    )
                );
            }
        }
    }


    // =========================================================
    // STABLE VARIATION
    // =========================================================

    private static int CreateStableHash(
        BuildingSection section,
        int seed,
        int row,
        int column,
        int salt)
    {
        unchecked
        {
            int hash = seed;

            hash = hash * 397 ^ section.GridPosition.x;
            hash = hash * 397 ^ section.GridPosition.y;
            hash = hash * 397 ^ section.StructureLevel;
            hash = hash * 397 ^ row;
            hash = hash * 397 ^ column;
            hash = hash * 397 ^ salt;

            return hash;
        }
    }
}
