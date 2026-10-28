using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central representation av ett sammanhängande vägnät.
///
/// Ansvar:
/// - hittar RoadPaths under samma root
/// - upptäcker junctions mellan vägar
/// - ger presentationen junction-influence
/// - undertrycker edge-decoration vid junctions
/// - erbjuder framtida gameplay-query:
///     IsPointOnRoad / TryGetRoadAtPoint
///
/// RoadNetwork spelar INTE musik, footsteps eller applicerar stats.
/// Det beskriver endast själva vägnätet.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class RoadNetwork :
    MonoBehaviour
{
    // =========================================================
    // DETECTION
    // =========================================================

    [Header("Junction Detection")]

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Extra tolerans när en RoadPath-endpoint ligger nära " +
        "en annan väg. Gör det lättare att rita T-junctions " +
        "utan att centerlinjerna måste träffa exakt."
    )]
    private float connectionPadding =
        0.15f;


    [SerializeField]
    [Min(0.01f)]
    [Tooltip(
        "Junction-punkter som ligger närmare än detta slås ihop " +
        "så samma korsning inte registreras flera gånger."
    )]
    private float junctionMergeDistance =
        0.35f;


    // =========================================================
    // PRESENTATION
    // =========================================================

    [Header("Junction Presentation")]

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Hur mycket varje vägkant får flara ut nära en junction. " +
        "Ett litet värde tar bort den mest perfekta 90-graderskänslan."
    )]
    private float cornerWideningPerSide =
        0.18f;


    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Extra område utanför den bredaste vägens halva bredd " +
        "där junction-blending tonas in."
    )]
    private float blendRadiusPadding =
        0.35f;


    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Extra område runt junction där Splatter och Edge Doodads " +
        "ska undertryckas."
    )]
    private float decorationSuppressionPadding =
        0.25f;


    // =========================================================
    // GAMEPLAY
    // =========================================================

    [Header("Gameplay Query")]

    [SerializeField]
    [Range(0.1f, 1.25f)]
    [Tooltip(
        "Hur stor del av den visuella vägbredden som räknas som " +
        "'på väg' för gameplay. 0.9 lämnar lite tolerans vid kanten."
    )]
    private float gameplayWidthScale =
        0.9f;


    // =========================================================
    // RUNTIME DATA
    // =========================================================

    private readonly List<RoadCache>
        roadCaches =
            new();


    private readonly List<Junction>
        junctions =
            new();


    private bool isRebuilding;


    // =========================================================
    // INTERNAL TYPES
    // =========================================================

    private sealed class RoadCache
    {
        public RoadPath road;

        public readonly List<Vector2>
            centerLine =
                new();

        public Rect bounds;
    }


    private sealed class Junction
    {
        public RoadPath roadA;
        public RoadPath roadB;

        public Vector2 position;

        public float blendRadius;
        public float decorationRadius;
    }


    // =========================================================
    // PUBLIC
    // =========================================================

    public float CornerWideningPerSide =>
        cornerWideningPerSide;


    public int RoadCount =>
        roadCaches.Count;


    public int JunctionCount =>
        junctions.Count;


    // =========================================================
    // UNITY
    // =========================================================

    private void OnEnable()
    {
        RebuildNetwork();
    }


    private void OnTransformChildrenChanged()
    {
        if (isRebuilding)
            return;

        RebuildNetwork();
    }


    private void OnValidate()
    {
        connectionPadding =
            Mathf.Max(
                0f,
                connectionPadding
            );


        junctionMergeDistance =
            Mathf.Max(
                0.01f,
                junctionMergeDistance
            );


        cornerWideningPerSide =
            Mathf.Max(
                0f,
                cornerWideningPerSide
            );


        blendRadiusPadding =
            Mathf.Max(
                0f,
                blendRadiusPadding
            );


        decorationSuppressionPadding =
            Mathf.Max(
                0f,
                decorationSuppressionPadding
            );


        gameplayWidthScale =
            Mathf.Clamp(
                gameplayWidthScale,
                0.1f,
                1.25f
            );
    }


    // =========================================================
    // REBUILD
    // =========================================================

    /// <summary>
    /// Anropas av RoadPath när authored road-data förändras.
    ///
    /// Eftersom en ändrad väg också kan påverka sina grannar
    /// rebuildas hela nätverket.
    /// </summary>
    public void NotifyRoadChanged(
        RoadPath changedRoad)
    {
        if (isRebuilding)
            return;


        RebuildNetwork();
    }


    [ContextMenu(
        "Rebuild Road Network"
    )]
    public void RebuildNetwork()
    {
        if (isRebuilding)
            return;


        isRebuilding =
            true;


        try
        {
            BuildRoadCaches();

            BuildJunctions();


            /*
             * Junction-information påverkar även andra
             * RoadPaths än den som eventuellt ändrades.
             */
            for (int i = 0;
                 i < roadCaches.Count;
                 i++)
            {
                RoadPath road =
                    roadCaches[i].road;


                if (road == null ||
                    !road.isActiveAndEnabled)
                {
                    continue;
                }


                RoadMeshBuilder.Rebuild(
                    road
                );
            }
        }
        finally
        {
            isRebuilding =
                false;
        }
    }


    // =========================================================
    // JUNCTION PRESENTATION QUERY
    // =========================================================

    /// <summary>
    /// 0 = ingen junction-influence.
    /// 1 = exakt i junctionens centrum.
    ///
    /// Används bland annat för:
    /// - lätt road widening
    /// - erosion suppression
    /// - shoulder suppression
    /// </summary>
    public float GetJunctionInfluence(
        RoadPath road,
        Vector2 worldPosition)
    {
        if (road == null)
            return 0f;


        float strongestInfluence =
            0f;


        for (int i = 0;
             i < junctions.Count;
             i++)
        {
            Junction junction =
                junctions[i];


            if (!ContainsRoad(
                    junction,
                    road))
            {
                continue;
            }


            float radius =
                Mathf.Max(
                    0.001f,
                    junction.blendRadius
                );


            float distance =
                Vector2.Distance(
                    worldPosition,
                    junction.position
                );


            if (distance >= radius)
                continue;


            float influence =
                1f -
                distance /
                radius;


            influence =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    influence
                );


            strongestInfluence =
                Mathf.Max(
                    strongestInfluence,
                    influence
                );
        }


        return
            strongestInfluence;
    }


    public bool IsDecorationSuppressed(
        RoadPath road,
        Vector2 worldPosition)
    {
        if (road == null)
            return false;


        for (int i = 0;
             i < junctions.Count;
             i++)
        {
            Junction junction =
                junctions[i];


            if (!ContainsRoad(
                    junction,
                    road))
            {
                continue;
            }


            float radius =
                junction.decorationRadius;


            if ((worldPosition -
                 junction.position)
                    .sqrMagnitude <=
                radius *
                radius)
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // GAMEPLAY QUERY
    // =========================================================

    public bool IsPointOnRoad(
        Vector2 worldPosition)
    {
        return
            TryGetRoadAtPoint(
                worldPosition,
                out _,
                out _
            );
    }


    public bool TryGetRoadAtPoint(
        Vector2 worldPosition,
        out RoadPath road,
        out RoadProfile profile)
    {
        road =
            null;


        profile =
            null;


        float bestNormalizedDistance =
            float.MaxValue;


        for (int cacheIndex = 0;
             cacheIndex < roadCaches.Count;
             cacheIndex++)
        {
            RoadCache cache =
                roadCaches[
                    cacheIndex
                ];


            if (cache.road == null ||
                cache.centerLine.Count < 2)
            {
                continue;
            }


            if (!cache.bounds.Contains(
                    worldPosition))
            {
                continue;
            }


            for (int i = 0;
                 i <
                 cache.centerLine.Count - 1;
                 i++)
            {
                Vector2 start =
                    cache.centerLine[i];


                Vector2 end =
                    cache.centerLine[i + 1];


                Vector2 segment =
                    end -
                    start;


                if (segment.sqrMagnitude <=
                    0.000001f)
                {
                    continue;
                }


                Vector2 tangent =
                    segment.normalized;


                Vector2 closest =
                    ClosestPointOnSegment(
                        worldPosition,
                        start,
                        end
                    );


                float halfWidth =
                    cache.road
                        .GetWidthForDirection(
                            tangent
                        ) *
                    0.5f *
                    gameplayWidthScale;


                halfWidth =
                    Mathf.Max(
                        0.001f,
                        halfWidth
                    );


                float distance =
                    Vector2.Distance(
                        worldPosition,
                        closest
                    );


                if (distance >
                    halfWidth)
                {
                    continue;
                }


                float normalizedDistance =
                    distance /
                    halfWidth;


                if (normalizedDistance >=
                    bestNormalizedDistance)
                {
                    continue;
                }


                bestNormalizedDistance =
                    normalizedDistance;


                road =
                    cache.road;


                profile =
                    cache.road.Profile;
            }
        }


        return
            road != null;
    }


    // =========================================================
    // CACHE
    // =========================================================

    private void BuildRoadCaches()
    {
        roadCaches.Clear();


        RoadPath[] roads =
            GetComponentsInChildren<
                RoadPath
            >(true);


        for (int i = 0;
             i < roads.Length;
             i++)
        {
            RoadPath road =
                roads[i];


            if (road == null ||
                road.ControlPointCount < 2)
            {
                continue;
            }


            RoadCache cache =
                new RoadCache
                {
                    road =
                        road
                };


            RoadMeshBuilder
                .BuildSmoothCenterLine(
                    road,
                    cache.centerLine
                );


            if (cache.centerLine.Count < 2)
                continue;


            cache.bounds =
                CalculateRoadBounds(
                    road,
                    cache.centerLine
                );


            roadCaches.Add(
                cache
            );
        }
    }


    private Rect CalculateRoadBounds(
        RoadPath road,
        IReadOnlyList<Vector2> points)
    {
        float minimumX =
            points[0].x;


        float maximumX =
            points[0].x;


        float minimumY =
            points[0].y;


        float maximumY =
            points[0].y;


        for (int i = 1;
             i < points.Count;
             i++)
        {
            Vector2 point =
                points[i];


            minimumX =
                Mathf.Min(
                    minimumX,
                    point.x
                );


            maximumX =
                Mathf.Max(
                    maximumX,
                    point.x
                );


            minimumY =
                Mathf.Min(
                    minimumY,
                    point.y
                );


            maximumY =
                Mathf.Max(
                    maximumY,
                    point.y
                );
        }


        float padding =
            road.EffectiveBaseWidth *
            0.6f +
            connectionPadding +
            blendRadiusPadding;


        return
            Rect.MinMaxRect(
                minimumX - padding,
                minimumY - padding,
                maximumX + padding,
                maximumY + padding
            );
    }


    // =========================================================
    // JUNCTION DETECTION
    // =========================================================

    private void BuildJunctions()
    {
        junctions.Clear();


        for (int firstIndex = 0;
             firstIndex <
             roadCaches.Count - 1;
             firstIndex++)
        {
            RoadCache first =
                roadCaches[
                    firstIndex
                ];


            for (int secondIndex =
                     firstIndex + 1;
                 secondIndex <
                 roadCaches.Count;
                 secondIndex++)
            {
                RoadCache second =
                    roadCaches[
                        secondIndex
                    ];


                if (!first.bounds
                        .Overlaps(
                            second.bounds))
                {
                    continue;
                }


                DetectCenterLineIntersections(
                    first,
                    second
                );


                DetectEndpointConnections(
                    first,
                    second
                );


                DetectEndpointConnections(
                    second,
                    first
                );
            }
        }
    }


    // =========================================================
    // TRUE CENTERLINE INTERSECTIONS
    // =========================================================

    private void DetectCenterLineIntersections(
        RoadCache first,
        RoadCache second)
    {
        for (int firstSegment = 0;
             firstSegment <
             first.centerLine.Count - 1;
             firstSegment++)
        {
            Vector2 firstStart =
                first.centerLine[
                    firstSegment
                ];


            Vector2 firstEnd =
                first.centerLine[
                    firstSegment + 1
                ];


            Vector2 firstDirection =
                firstEnd -
                firstStart;


            if (firstDirection.sqrMagnitude <=
                0.000001f)
            {
                continue;
            }


            for (int secondSegment = 0;
                 secondSegment <
                 second.centerLine.Count - 1;
                 secondSegment++)
            {
                Vector2 secondStart =
                    second.centerLine[
                        secondSegment
                    ];


                Vector2 secondEnd =
                    second.centerLine[
                        secondSegment + 1
                    ];


                Vector2 secondDirection =
                    secondEnd -
                    secondStart;


                if (secondDirection.sqrMagnitude <=
                    0.000001f)
                {
                    continue;
                }


                if (!TryGetSegmentIntersection(
                        firstStart,
                        firstEnd,
                        secondStart,
                        secondEnd,
                        out Vector2 intersection))
                {
                    continue;
                }


                AddOrMergeJunction(
                    first.road,
                    second.road,
                    intersection,
                    firstDirection.normalized,
                    secondDirection.normalized
                );
            }
        }
    }


    // =========================================================
    // ENDPOINT -> ROAD CONNECTION
    // =========================================================

    private void DetectEndpointConnections(
        RoadCache source,
        RoadCache target)
    {
        if (source.centerLine.Count < 2 ||
            target.centerLine.Count < 2)
        {
            return;
        }


        TryConnectEndpoint(
            source,
            target,
            0
        );


        TryConnectEndpoint(
            source,
            target,
            source.centerLine.Count - 1
        );
    }


    private void TryConnectEndpoint(
        RoadCache source,
        RoadCache target,
        int endpointIndex)
    {
        Vector2 endpoint =
            source.centerLine[
                endpointIndex
            ];


        Vector2 sourceTangent;


        if (endpointIndex == 0)
        {
            sourceTangent =
                source.centerLine[1] -
                source.centerLine[0];
        }
        else
        {
            int last =
                source.centerLine.Count - 1;


            sourceTangent =
                source.centerLine[last] -
                source.centerLine[last - 1];
        }


        if (sourceTangent.sqrMagnitude <=
            0.000001f)
        {
            return;
        }


        sourceTangent.Normalize();


        if (!TryGetClosestPointOnRoad(
                target,
                endpoint,
                out Vector2 closest,
                out Vector2 targetTangent,
                out float distance))
        {
            return;
        }


        float targetHalfWidth =
            target.road
                .GetWidthForDirection(
                    targetTangent
                ) *
            0.5f;


        float allowedDistance =
            targetHalfWidth +
            connectionPadding;


        if (distance >
            allowedDistance)
        {
            return;
        }


        /*
         * Mittpunkten fungerar bra både när endpointen träffar
         * target-centerline exakt och när användaren bara ritat
         * en bit in över target-roadens kant.
         */
        Vector2 junctionPosition =
            Vector2.Lerp(
                endpoint,
                closest,
                0.5f
            );


        AddOrMergeJunction(
            source.road,
            target.road,
            junctionPosition,
            sourceTangent,
            targetTangent
        );
    }


    private bool TryGetClosestPointOnRoad(
        RoadCache cache,
        Vector2 point,
        out Vector2 closestPoint,
        out Vector2 tangent,
        out float distance)
    {
        closestPoint =
            Vector2.zero;


        tangent =
            Vector2.right;


        distance =
            float.MaxValue;


        bool found =
            false;


        float bestDistanceSquared =
            float.MaxValue;


        for (int i = 0;
             i <
             cache.centerLine.Count - 1;
             i++)
        {
            Vector2 start =
                cache.centerLine[i];


            Vector2 end =
                cache.centerLine[i + 1];


            Vector2 segment =
                end -
                start;


            if (segment.sqrMagnitude <=
                0.000001f)
            {
                continue;
            }


            Vector2 candidate =
                ClosestPointOnSegment(
                    point,
                    start,
                    end
                );


            float candidateDistanceSquared =
                (
                    point -
                    candidate
                ).sqrMagnitude;


            if (candidateDistanceSquared >=
                bestDistanceSquared)
            {
                continue;
            }


            bestDistanceSquared =
                candidateDistanceSquared;


            closestPoint =
                candidate;


            tangent =
                segment.normalized;


            found =
                true;
        }


        if (!found)
            return false;


        distance =
            Mathf.Sqrt(
                bestDistanceSquared
            );


        return true;
    }


    // =========================================================
    // JUNCTION STORAGE
    // =========================================================

    private void AddOrMergeJunction(
        RoadPath roadA,
        RoadPath roadB,
        Vector2 position,
        Vector2 tangentA,
        Vector2 tangentB)
    {
        if (roadA == null ||
            roadB == null ||
            roadA == roadB)
        {
            return;
        }


        float halfWidthA =
            roadA.GetWidthForDirection(
                tangentA
            ) *
            0.5f;


        float halfWidthB =
            roadB.GetWidthForDirection(
                tangentB
            ) *
            0.5f;


        float largestHalfWidth =
            Mathf.Max(
                halfWidthA,
                halfWidthB
            );


        float blendRadius =
            largestHalfWidth +
            blendRadiusPadding;


        float decorationRadius =
            largestHalfWidth +
            decorationSuppressionPadding;


        float mergeDistanceSquared =
            junctionMergeDistance *
            junctionMergeDistance;


        for (int i = 0;
             i < junctions.Count;
             i++)
        {
            Junction existing =
                junctions[i];


            if (!SameRoadPair(
                    existing,
                    roadA,
                    roadB))
            {
                continue;
            }


            if ((existing.position -
                 position)
                    .sqrMagnitude >
                mergeDistanceSquared)
            {
                continue;
            }


            existing.position =
                (
                    existing.position +
                    position
                ) *
                0.5f;


            existing.blendRadius =
                Mathf.Max(
                    existing.blendRadius,
                    blendRadius
                );


            existing.decorationRadius =
                Mathf.Max(
                    existing.decorationRadius,
                    decorationRadius
                );


            return;
        }


        junctions.Add(
            new Junction
            {
                roadA =
                    roadA,

                roadB =
                    roadB,

                position =
                    position,

                blendRadius =
                    blendRadius,

                decorationRadius =
                    decorationRadius
            }
        );
    }


    // =========================================================
    // GEOMETRY HELPERS
    // =========================================================

    private static bool TryGetSegmentIntersection(
        Vector2 firstStart,
        Vector2 firstEnd,
        Vector2 secondStart,
        Vector2 secondEnd,
        out Vector2 intersection)
    {
        intersection =
            Vector2.zero;


        Vector2 firstDirection =
            firstEnd -
            firstStart;


        Vector2 secondDirection =
            secondEnd -
            secondStart;


        float cross =
            Cross(
                firstDirection,
                secondDirection
            );


        if (Mathf.Abs(
                cross) <=
            0.000001f)
        {
            return false;
        }


        Vector2 difference =
            secondStart -
            firstStart;


        float firstT =
            Cross(
                difference,
                secondDirection
            ) /
            cross;


        float secondT =
            Cross(
                difference,
                firstDirection
            ) /
            cross;


        const float tolerance =
            0.0001f;


        if (firstT <
                -tolerance ||
            firstT >
                1f + tolerance ||
            secondT <
                -tolerance ||
            secondT >
                1f + tolerance)
        {
            return false;
        }


        intersection =
            firstStart +
            firstDirection *
            Mathf.Clamp01(
                firstT
            );


        return true;
    }


    private static Vector2 ClosestPointOnSegment(
        Vector2 point,
        Vector2 start,
        Vector2 end)
    {
        Vector2 segment =
            end -
            start;


        float lengthSquared =
            segment.sqrMagnitude;


        if (lengthSquared <=
            0.000001f)
        {
            return start;
        }


        float t =
            Vector2.Dot(
                point -
                start,
                segment
            ) /
            lengthSquared;


        t =
            Mathf.Clamp01(
                t
            );


        return
            start +
            segment *
            t;
    }


    private static float Cross(
        Vector2 first,
        Vector2 second)
    {
        return
            first.x *
            second.y -
            first.y *
            second.x;
    }


    private static bool ContainsRoad(
        Junction junction,
        RoadPath road)
    {
        return
            junction.roadA ==
                road ||
            junction.roadB ==
                road;
    }


    private static bool SameRoadPair(
        Junction junction,
        RoadPath roadA,
        RoadPath roadB)
    {
        return
            (
                junction.roadA ==
                    roadA &&
                junction.roadB ==
                    roadB
            ) ||
            (
                junction.roadA ==
                    roadB &&
                junction.roadB ==
                    roadA
            );
    }
}
