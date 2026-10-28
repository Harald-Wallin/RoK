using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reducerar en rå grid-path till färre waypoints.
///
/// Seamless-versionen kan verifiera line-of-travel över flera
/// NavigationRegions utan att region-borders blir waypoints.
/// </summary>
public static class NavigationPathSmoother
{
    // =========================================================
    // SEAMLESS WORLD
    // =========================================================

    public static List<Vector2> Smooth(
        NavigationWorld world,
        NavigationRegion clearanceRegion,
        IReadOnlyList<Vector2> rawPoints)
    {
        List<Vector2> result =
            new();

        if (clearanceRegion == null ||
            rawPoints == null ||
            rawPoints.Count == 0)
        {
            return result;
        }

        if (rawPoints.Count == 1)
        {
            result.Add(
                rawPoints[0]
            );

            return result;
        }

        int anchorIndex =
            0;

        result.Add(
            rawPoints[
                anchorIndex
            ]
        );

        while (anchorIndex <
               rawPoints.Count - 1)
        {
            int furthestVisible =
                -1;

            /*
             * Börja från slutet och hitta den längsta waypoint
             * som går att nå med full navigation-clearance.
             *
             * Region-borders spelar ingen roll.
             */
            for (int candidate =
                     rawPoints.Count - 1;
                 candidate >
                     anchorIndex;
                 candidate--)
            {
                bool clear =
                    world != null
                        ? world.IsDirectPathClear(
                            rawPoints[
                                anchorIndex
                            ],
                            rawPoints[
                                candidate
                            ],
                            clearanceRegion
                        )
                        : clearanceRegion
                            .IsDirectPathClear(
                                rawPoints[
                                    anchorIndex
                                ],
                                rawPoints[
                                    candidate
                                ]
                            );

                if (!clear)
                {
                    continue;
                }

                furthestVisible =
                    candidate;

                break;
            }

            if (furthestVisible < 0)
            {
                break;
            }

            Vector2 nextPoint =
                rawPoints[
                    furthestVisible
                ];

            Vector2 previousPoint =
                result[
                    result.Count -
                    1
                ];

            if ((nextPoint -
                 previousPoint)
                    .sqrMagnitude >
                0.0001f)
            {
                result.Add(
                    nextPoint
                );
            }

            anchorIndex =
                furthestVisible;
        }

        return result;
    }


    // =========================================================
    // BACKWARDS COMPATIBILITY
    // =========================================================

    public static List<Vector2> Smooth(
        NavigationRegion region,
        IReadOnlyList<Vector2> rawPoints)
    {
        return Smooth(
            null,
            region,
            rawPoints
        );
    }
}
