using UnityEngine;

/// <summary>
/// Konfigurerar det generiska road-edge-decoration-systemet
/// för vägarnas splatter-lager.
/// </summary>
public static class RoadSplatterBuilder
{
    private const int SplatterSeedSalt =
        1374496523;


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
                        profile.EnableSplatter,

                    objectName =
                        "Splatter",

                    meshNamePrefix =
                        "RoadSplatterMesh",

                    seedSalt =
                        SplatterSeedSalt,

                    sprites =
                        profile.SplatterSprites,

                    density =
                        profile.SplatterDensity,

                    minEdgeOffset =
                        profile
                            .SplatterMinEdgeOffset,

                    maxEdgeOffset =
                        profile
                            .SplatterMaxEdgeOffset,

                    scaleRange =
                        profile
                            .SplatterScaleRange,

                    upperEdgeOffsetScale =
                        profile
                            .UpperEdgeSplatterOffsetScale,

                    lowerEdgeOffsetScale =
                    1f,

                    upperEdgeScale =
                        profile
                            .UpperEdgeSplatterScale,

                    randomQuarterTurns =
                        true,

                    randomFlipX =
                        true,

                    randomFlipY =
                        true,

                    /*
                     * Splatter får fortfarande vara lite mer
                     * slumpmässigt fördelat mellan sidorna.
                     */
                    balancedSides =
                        false,

                    suppressAtJunctions =
                        true,

                    sortingOrderOffset =
                        1
                };


        RoadEdgeDecorationMeshBuilder
            .Rebuild(
                road,
                settings
            );
    }
}