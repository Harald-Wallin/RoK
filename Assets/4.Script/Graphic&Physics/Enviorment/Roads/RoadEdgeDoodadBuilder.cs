using UnityEngine;

/// <summary>
/// Genererar små dekorationssprites längs båda vägkanterna.
///
/// Exempel:
/// - små gräsbitar
/// - småsten
/// - rötter
/// - torra växtrester
///
/// Alla instanser kombineras till en enda mesh-presentation.
/// </summary>
public static class RoadEdgeDoodadBuilder
{
    private const int EdgeDoodadSeedSalt =
        913557421;


    public static void Rebuild(
        RoadPath road)
    {
        if (road == null)
            return;


        RoadProfile profile =
            road.Profile;


        if (profile == null)
            return;


        RoadEdgeDecorationMeshBuilder.Settings
            settings =
                new RoadEdgeDecorationMeshBuilder.Settings
                {
                    enabled =
                        profile.EnableEdgeDoodads,

                    objectName =
                        "EdgeDoodads",

                    meshNamePrefix =
                        "RoadEdgeDoodadMesh",

                    seedSalt =
                        EdgeDoodadSeedSalt,

                    sprites =
                        profile.EdgeDoodadSprites,

                    density =
                        profile.EdgeDoodadDensity,

                    minEdgeOffset =
                        profile
                            .EdgeDoodadMinEdgeOffset,

                    maxEdgeOffset =
                        profile
                            .EdgeDoodadMaxEdgeOffset,

                    scaleRange =
                        profile
                            .EdgeDoodadScaleRange,

                    upperEdgeOffsetScale =
                         profile
                            .UpperEdgeDoodadOffsetScale,

                    lowerEdgeOffsetScale =
                        profile
                            .LowerEdgeDoodadOffsetScale,

                    upperEdgeScale =
                        profile
                            .UpperEdgeDoodadScale,

                    /*
                     * Gräs och liknande ska normalt förbli
                     * world-space-upprätt.
                     */
                    randomQuarterTurns =
                        profile
                            .EdgeDoodadRandomQuarterTurns,

                    randomFlipX =
                        profile
                            .EdgeDoodadRandomFlipX,

                    randomFlipY =
                        false,

                    /*
                     * Doodads ska täcka båda vägkanterna mer
                     * konsekvent än vanlig splatter.
                     */
                    balancedSides =
                        true,

                    suppressAtJunctions =
                        true,

                    /*
                     * Surface = base
                     * Splatter = +1
                     * Edge Doodads = +2
                     */
                    sortingOrderOffset =
                        2
                };


        RoadEdgeDecorationMeshBuilder
            .Rebuild(
                road,
                settings
            );
    }
}
