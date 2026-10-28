using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Genererar visuell presentation från en RoadPath.
///
/// Ansvar:
/// - smooth center line
/// - direction-aware width
/// - large-scale edge roughness
/// - surface mesh
/// - world-space UV
/// - shader edge-data
///
/// Pixel erosion och shoulder sker i RoadSurface.shader.
/// </summary>
public static class RoadMeshBuilder
{
    private const string GeneratedRootName =
        "Generated";

    private const string SurfaceObjectName =
        "Surface";

    private const string LegacyShoulderObjectName =
        "UpperShoulder";


    private const float CurveSampleSpacing =
        0.12f;


    private static Material
        fallbackSurfaceMaterial;


    private static readonly Dictionary<
        Texture2D,
        Material> surfaceTextureMaterials =
            new();


    private static readonly
        MaterialPropertyBlock
        surfacePropertyBlock =
            new();


    /*
     * Återanvändbara buffers för gameplay-query mot den redan
     * genererade Surface-meshen.
     *
     * Detta undviker allocations varje FixedUpdate när spelaren
     * frågar om den står på en väg.
     */
    private static readonly List<Vector3>
        surfaceQueryVertices =
            new();

    private static readonly List<int>
        surfaceQueryTriangles =
            new();


    // =========================================================
    // ROAD SAMPLE
    // =========================================================

    private struct RoadSample
    {
        public Vector2 center;

        public Vector2 tangent;
        public Vector2 normal;

        public float travelledDistance;
        public float junctionInfluence;

        public float leftDistance;
        public float rightDistance;


        public Vector2 LeftEdge =>
            center +
            normal *
            leftDistance;


        public Vector2 RightEdge =>
            center -
            normal *
            rightDistance;
    }


    // =========================================================
    // PUBLIC
    // =========================================================

    public static void Rebuild(
        RoadPath road)
    {
        if (road == null)
            return;


        if (road.ControlPointCount < 2)
        {
            SetGeneratedVisible(
                road,
                false
            );

            return;
        }


        List<Vector2> centerLine =
            new();


        BuildSmoothCenterLine(
            road,
            centerLine
        );


        if (centerLine.Count < 2)
        {
            SetGeneratedVisible(
                road,
                false
            );

            return;
        }


        List<RoadSample> samples =
            new();


        BuildRoadSamples(
            road,
            centerLine,
            samples
        );


        if (samples.Count < 2)
        {
            SetGeneratedVisible(
                road,
                false
            );

            return;
        }


        Transform generatedRoot =
            EnsureGeneratedRoot(
                road
            );


        BuildSurface(
            road,
            generatedRoot,
            samples
        );


        /*
         * Den gamla separata shoulder-meshen används
         * inte längre. Shadern gör shouldern från samma
         * pixelkontur som erosionen.
         */
        DisableLegacyShoulder(
            generatedRoot
        );


        RoadSplatterBuilder.Rebuild(
            road
        );


        RoadEdgeDoodadBuilder.Rebuild(
            road
        );


        generatedRoot.gameObject
            .SetActive(
                true
            );
    }


    // =========================================================
    // GAMEPLAY SURFACE QUERY
    // =========================================================

    /// <summary>
    /// Testar en world-position direkt mot vägens BEFINTLIGA
    /// genererade Surface-mesh.
    ///
    /// Inga extra colliders, triggers eller road-layers skapas.
    ///
    /// Shaderns pixel-erosion räknas medvetet inte som gameplay-hål:
    /// den underliggande Surface-formen är den stabila gameplay-ytan.
    ///
    /// centerLineDistanceSqr används för att välja en naturlig väg
    /// om flera RoadPaths överlappar i en junction.
    /// </summary>
    public static bool ContainsWorldPoint(
        RoadPath road,
        Vector2 worldPoint,
        out float centerLineDistanceSqr)
    {
        centerLineDistanceSqr =
            float.PositiveInfinity;


        if (road == null ||
            !road.isActiveAndEnabled)
        {
            return false;
        }


        Transform generatedRoot =
            road.transform.Find(
                GeneratedRootName
            );


        if (generatedRoot == null ||
            !generatedRoot.gameObject.activeInHierarchy)
        {
            return false;
        }


        Transform surfaceTransform =
            generatedRoot.Find(
                SurfaceObjectName
            );


        if (surfaceTransform == null ||
            !surfaceTransform.gameObject.activeInHierarchy)
        {
            return false;
        }


        MeshFilter meshFilter =
            surfaceTransform
                .GetComponent<MeshFilter>();


        MeshRenderer meshRenderer =
            surfaceTransform
                .GetComponent<MeshRenderer>();


        Mesh mesh =
            meshFilter != null
                ? meshFilter.sharedMesh
                : null;


        if (mesh == null ||
            mesh.vertexCount < 3)
        {
            return false;
        }


        /*
         * Billig world-space bounds-check först.
         * Nästan alla vägar sorteras bort här.
         */
        if (meshRenderer != null)
        {
            Bounds worldBounds =
                meshRenderer.bounds;


            Vector3 boundsPoint =
                new Vector3(
                    worldPoint.x,
                    worldPoint.y,
                    worldBounds.center.z
                );


            if (!worldBounds.Contains(
                    boundsPoint))
            {
                return false;
            }
        }


        Vector3 localPoint3 =
            surfaceTransform
                .InverseTransformPoint(
                    new Vector3(
                        worldPoint.x,
                        worldPoint.y,
                        surfaceTransform.position.z
                    )
                );


        Vector2 localPoint =
            new Vector2(
                localPoint3.x,
                localPoint3.y
            );


        Bounds localBounds =
            mesh.bounds;


        if (localPoint.x <
                localBounds.min.x ||
            localPoint.x >
                localBounds.max.x ||
            localPoint.y <
                localBounds.min.y ||
            localPoint.y >
                localBounds.max.y)
        {
            return false;
        }


        surfaceQueryVertices.Clear();
        surfaceQueryTriangles.Clear();


        mesh.GetVertices(
            surfaceQueryVertices
        );


        mesh.GetTriangles(
            surfaceQueryTriangles,
            0
        );


        bool isInside =
            false;


        for (int i = 0;
             i + 2 <
             surfaceQueryTriangles.Count;
             i += 3)
        {
            Vector2 a =
                surfaceQueryVertices[
                    surfaceQueryTriangles[i]
                ];

            Vector2 b =
                surfaceQueryVertices[
                    surfaceQueryTriangles[i + 1]
                ];

            Vector2 c =
                surfaceQueryVertices[
                    surfaceQueryTriangles[i + 2]
                ];


            if (!PointInTriangle(
                    localPoint,
                    a,
                    b,
                    c))
            {
                continue;
            }


            isInside =
                true;

            break;
        }


        if (!isInside)
        {
            return false;
        }


        /*
         * Surface-meshen har fyra vertices per road sample:
         *
         * 0 = left edge
         * 1 = center-left
         * 2 = center-right
         * 3 = right edge
         *
         * Center-left och center-right ligger på samma centerpunkt.
         * Vi kan därför återanvända dem för att mäta avståndet till
         * den faktiska centerline som redan byggde vägen.
         */
        for (int i = 0;
             i + 5 <
             surfaceQueryVertices.Count;
             i += 4)
        {
            Vector3 localA =
                surfaceQueryVertices[
                    i + 1
                ];

            Vector3 localB =
                surfaceQueryVertices[
                    i + 5
                ];


            Vector3 worldA3 =
                surfaceTransform
                    .TransformPoint(
                        localA
                    );

            Vector3 worldB3 =
                surfaceTransform
                    .TransformPoint(
                        localB
                    );


            float distanceSqr =
                DistancePointToSegmentSqr(
                    worldPoint,
                    new Vector2(
                        worldA3.x,
                        worldA3.y
                    ),
                    new Vector2(
                        worldB3.x,
                        worldB3.y
                    )
                );


            if (distanceSqr <
                centerLineDistanceSqr)
            {
                centerLineDistanceSqr =
                    distanceSqr;
            }
        }


        if (float.IsPositiveInfinity(
                centerLineDistanceSqr))
        {
            centerLineDistanceSqr =
                0f;
        }


        return true;
    }


    private static bool PointInTriangle(
        Vector2 point,
        Vector2 a,
        Vector2 b,
        Vector2 c)
    {
        const float epsilon =
            0.000001f;


        float crossAB =
            Cross2D(
                b - a,
                point - a
            );

        float crossBC =
            Cross2D(
                c - b,
                point - b
            );

        float crossCA =
            Cross2D(
                a - c,
                point - c
            );


        bool hasNegative =
            crossAB < -epsilon ||
            crossBC < -epsilon ||
            crossCA < -epsilon;


        bool hasPositive =
            crossAB > epsilon ||
            crossBC > epsilon ||
            crossCA > epsilon;


        return
            !(hasNegative &&
              hasPositive);
    }


    private static float Cross2D(
        Vector2 a,
        Vector2 b)
    {
        return
            a.x * b.y -
            a.y * b.x;
    }


    private static float DistancePointToSegmentSqr(
        Vector2 point,
        Vector2 segmentStart,
        Vector2 segmentEnd)
    {
        Vector2 segment =
            segmentEnd -
            segmentStart;


        float lengthSqr =
            segment.sqrMagnitude;


        if (lengthSqr <=
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
            lengthSqr;


        t =
            Mathf.Clamp01(
                t
            );


        Vector2 closest =
            segmentStart +
            segment *
            t;


        return
            (
                point -
                closest
            ).sqrMagnitude;
    }


    // =========================================================
    // SMOOTH CENTER LINE
    // =========================================================

    public static void BuildSmoothCenterLine(
        RoadPath road,
        List<Vector2> output)
    {
        output.Clear();


        if (road == null ||
            road.ControlPointCount < 2)
        {
            return;
        }


        int count =
            road.ControlPointCount;


        for (int segmentIndex = 0;
             segmentIndex < count - 1;
             segmentIndex++)
        {
            Vector2 p1 =
                road.GetWorldPoint(
                    segmentIndex
                );


            Vector2 p2 =
                road.GetWorldPoint(
                    segmentIndex + 1
                );


            Vector2 p0 =
                segmentIndex > 0
                    ? road.GetWorldPoint(
                        segmentIndex - 1
                    )
                    : p1 + (p1 - p2);


            Vector2 p3 =
                segmentIndex + 2 < count
                    ? road.GetWorldPoint(
                        segmentIndex + 2
                    )
                    : p2 + (p2 - p1);


            float segmentLength =
                Vector2.Distance(
                    p1,
                    p2
                );


            int steps =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        segmentLength /
                        CurveSampleSpacing
                    )
                );


            for (int step = 0;
                 step < steps;
                 step++)
            {
                float t =
                    step /
                    (float)steps;


                Vector2 point =
                    EvaluateCatmullRom(
                        p0,
                        p1,
                        p2,
                        p3,
                        t
                    );


                AddPointIfDistinct(
                    output,
                    point
                );
            }
        }


        AddPointIfDistinct(
            output,
            road.GetWorldPoint(
                count - 1
            )
        );
    }


    private static Vector2 EvaluateCatmullRom(
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3,
        float t)
    {
        float t2 =
            t * t;

        float t3 =
            t2 * t;


        return
            0.5f *
            (
                2f * p1 +

                (-p0 + p2) *
                t +

                (
                    2f * p0 -
                    5f * p1 +
                    4f * p2 -
                    p3
                ) *
                t2 +

                (
                    -p0 +
                    3f * p1 -
                    3f * p2 +
                    p3
                ) *
                t3
            );
    }


    private static void AddPointIfDistinct(
        List<Vector2> points,
        Vector2 point)
    {
        if (points.Count == 0)
        {
            points.Add(
                point
            );

            return;
        }


        Vector2 previous =
            points[
                points.Count - 1
            ];


        if ((point - previous)
                .sqrMagnitude <
            0.000001f)
        {
            return;
        }


        points.Add(
            point
        );
    }


    // =========================================================
    // ROAD SAMPLES
    // =========================================================

    private static void BuildRoadSamples(
        RoadPath road,
        IReadOnlyList<Vector2> centerLine,
        List<RoadSample> output)
    {
        output.Clear();


        RoadProfile profile =
            road.Profile;

        RoadNetwork roadNetwork =
            road.GetComponentInParent<
            RoadNetwork
           >();


        float roughness =
            profile != null
                ? Mathf.Max(
                    0f,
                    profile.EdgeRoughness
                )
                : 0f;


        float noiseLength =
            profile != null
                ? Mathf.Max(
                    0.05f,
                    profile.EdgeNoiseLength
                )
                : 1f;


        uint seed =
            unchecked(
                (uint)road.AppearanceSeed
            );


        float leftSeedOffset =
            (seed & 0xFFFFu) *
            0.0137f +
            17.31f;


        float rightSeedOffset =
            ((seed >> 16) & 0xFFFFu) *
            0.0191f +
            83.17f;


        float travelledDistance =
            0f;


        for (int i = 0;
             i < centerLine.Count;
             i++)
        {
            Vector2 center =
                centerLine[i];


            if (i > 0)
            {
                travelledDistance +=
                    Vector2.Distance(
                        centerLine[i - 1],
                        center
                    );
            }


            Vector2 tangent =
                GetTangent(
                    centerLine,
                    i
                );


            Vector2 normal =
                new Vector2(
                    -tangent.y,
                    tangent.x
                );


            float halfWidth =
                road.GetWidthForDirection(
                    tangent
                ) *
                0.5f;

            float junctionInfluence =
                roadNetwork != null
                ? roadNetwork
            .GetJunctionInfluence(
                road,
                center
            )
        : 0f;


            /*
             * En liten flare nära junctionen.
             *
             * Det här är INTE en stor blob.
             * Vi breddar bara kanten subtilt så perfekta,
             * hårda T-hörn mjukas upp.
             */
            if (roadNetwork != null)
            {
                halfWidth +=
                    roadNetwork
                        .CornerWideningPerSide *
                    junctionInfluence;
            }


            float safeRoughness =
                Mathf.Min(
                    roughness,
                    halfWidth *
                    0.45f
                );


            float noisePosition =
                travelledDistance /
                noiseLength;


            float leftNoise =
                Mathf.PerlinNoise(
                    leftSeedOffset,
                    noisePosition
                ) *
                2f -
                1f;


            float rightNoise =
                Mathf.PerlinNoise(
                    rightSeedOffset,
                    noisePosition
                ) *
                2f -
                1f;


            float leftDistance =
                Mathf.Max(
                    0.025f,
                    halfWidth +
                    leftNoise *
                    safeRoughness
                );


            float rightDistance =
                Mathf.Max(
                    0.025f,
                    halfWidth +
                    rightNoise *
                    safeRoughness
                );


            output.Add(
                new RoadSample
                {
                    center =
                        center,

                    tangent =
                        tangent,

                    normal =
                        normal,

                    travelledDistance =
                        travelledDistance,

                    leftDistance =
                        leftDistance,

                    rightDistance =
                        rightDistance,

                    junctionInfluence =
                        junctionInfluence
                }
            );
        }
    }


    // =========================================================
    // SURFACE
    // =========================================================

    private static void BuildSurface(
        RoadPath road,
        Transform generatedRoot,
        IReadOnlyList<RoadSample> samples)
    {
        GameObject surfaceObject =
            FindOrCreateChild(
                generatedRoot,
                SurfaceObjectName
            );


        MeshFilter meshFilter =
            GetOrAdd<MeshFilter>(
                surfaceObject
            );


        MeshRenderer meshRenderer =
            GetOrAdd<MeshRenderer>(
                surfaceObject
            );


        ConfigureRenderer(
            meshRenderer
        );


        Mesh mesh =
            GetOrCreateMesh(
                road,
                meshFilter,
                "RoadSurfaceMesh"
            );


        BuildSurfaceMesh(
            road,
            samples,
            mesh
        );


        ApplySurfaceRendererSettings(
            road,
            meshRenderer
        );


        surfaceObject.SetActive(
            true
        );
    }


    private static void BuildSurfaceMesh(
        RoadPath road,
        IReadOnlyList<RoadSample> samples,
        Mesh mesh)
    {
        int pointCount =
            samples.Count;


        /*
         * Fyra vertices per sample:
         *
         * 0 = left edge
         * 1 = center, belonging to left half
         * 2 = center, belonging to right half
         * 3 = right edge
         *
         * Center dupliceras eftersom left/right roughness
         * kan ge olika verkligt avstånd till respektive edge.
         */
        int vertexCount =
            pointCount *
            4;


        Vector3[] vertices =
            new Vector3[
                vertexCount
            ];


        Vector2[] uvs =
            new Vector2[
                vertexCount
            ];


        Vector3[] normals =
            new Vector3[
                vertexCount
            ];


        Vector4[] edgeData =
            new Vector4[
                vertexCount
            ];

        Vector2[] junctionData =
            new Vector2[
            vertexCount
        ];


        int[] triangles =
            new int[
                (pointCount - 1) *
                12
            ];


        for (int i = 0;
             i < pointCount;
             i++)
        {
            RoadSample sample =
                samples[i];


            Vector2 leftWorld =
                sample.LeftEdge;


            Vector2 centerWorld =
                sample.center;


            Vector2 rightWorld =
                sample.RightEdge;


            int baseIndex =
                i *
                4;


            int leftIndex =
                baseIndex;

            int centerLeftIndex =
                baseIndex + 1;

            int centerRightIndex =
                baseIndex + 2;

            int rightIndex =
                baseIndex + 3;


            vertices[leftIndex] =
                WorldToRoadLocal(
                    road,
                    leftWorld
                );


            vertices[centerLeftIndex] =
                WorldToRoadLocal(
                    road,
                    centerWorld
                );


            vertices[centerRightIndex] =
                WorldToRoadLocal(
                    road,
                    centerWorld
                );


            vertices[rightIndex] =
                WorldToRoadLocal(
                    road,
                    rightWorld
                );


            uvs[leftIndex] =
                GetSurfaceWorldUV(
                    road,
                    leftWorld
                );


            uvs[centerLeftIndex] =
                GetSurfaceWorldUV(
                    road,
                    centerWorld
                );


            uvs[centerRightIndex] =
                GetSurfaceWorldUV(
                    road,
                    centerWorld
                );


            uvs[rightIndex] =
                GetSurfaceWorldUV(
                    road,
                    rightWorld
                );


            normals[leftIndex] =
                Vector3.back;

            normals[centerLeftIndex] =
                Vector3.back;

            normals[centerRightIndex] =
                Vector3.back;

            normals[rightIndex] =
                Vector3.back;


            /*
             * edgeData:
             *
             * x = world-distance från aktuell edge
             *
             * y = den aktuella sidans normals signed world-Y.
             *     Positiv = upper edge.
             *
             * z = side ID:
             *     +1 left
             *     -1 right
             *
             * w = travelled world-distance längs vägen
             */
            float leftNormalY =
                sample.normal.y;


            float rightNormalY =
                -sample.normal.y;


            edgeData[leftIndex] =
                new Vector4(
                    0f,
                    leftNormalY,
                    1f,
                    sample.travelledDistance
                );


            edgeData[centerLeftIndex] =
                new Vector4(
                    sample.leftDistance,
                    leftNormalY,
                    1f,
                    sample.travelledDistance
                );


            edgeData[centerRightIndex] =
                new Vector4(
                    sample.rightDistance,
                    rightNormalY,
                    -1f,
                    sample.travelledDistance
                );


            edgeData[rightIndex] =
                new Vector4(
                    0f,
                    rightNormalY,
                    -1f,
                    sample.travelledDistance
                );

            Vector2 currentJunctionData =
                new Vector2(
                    sample.junctionInfluence,
                    0f
                );


            junctionData[leftIndex] =
                currentJunctionData;

            junctionData[centerLeftIndex] =
                currentJunctionData;

            junctionData[centerRightIndex] =
                currentJunctionData;

            junctionData[rightIndex] =
                currentJunctionData;
        }


        int triangleIndex =
            0;


        for (int i = 0;
             i < pointCount - 1;
             i++)
        {
            int current =
                i *
                4;


            int next =
                (i + 1) *
                4;


            int leftCurrent =
                current;

            int centerLeftCurrent =
                current + 1;

            int centerRightCurrent =
                current + 2;

            int rightCurrent =
                current + 3;


            int leftNext =
                next;

            int centerLeftNext =
                next + 1;

            int centerRightNext =
                next + 2;

            int rightNext =
                next + 3;


            // Left half

            triangles[triangleIndex++] =
                leftCurrent;

            triangles[triangleIndex++] =
                leftNext;

            triangles[triangleIndex++] =
                centerLeftCurrent;


            triangles[triangleIndex++] =
                centerLeftCurrent;

            triangles[triangleIndex++] =
                leftNext;

            triangles[triangleIndex++] =
                centerLeftNext;


            // Right half

            triangles[triangleIndex++] =
                centerRightCurrent;

            triangles[triangleIndex++] =
                centerRightNext;

            triangles[triangleIndex++] =
                rightCurrent;


            triangles[triangleIndex++] =
                rightCurrent;

            triangles[triangleIndex++] =
                centerRightNext;

            triangles[triangleIndex++] =
                rightNext;
        }


        mesh.Clear();


        mesh.vertices =
            vertices;


        mesh.uv =
            uvs;


        mesh.normals =
            normals;


        /*
         * UV channel 1 / TEXCOORD1 används av RoadSurface.shader.
         */
        mesh.SetUVs(
            1,
            new List<Vector4>(
                edgeData
            )
        );

        /*
         * UV channel 2 / TEXCOORD2:
         *
         * x = junction influence
         * y = reserverad för framtida network-data
         */
        mesh.SetUVs(
            2,
            new List<Vector2>(
                junctionData
            )
        );


        mesh.triangles =
            triangles;


        mesh.RecalculateBounds();
    }


    // =========================================================
    // WORLD-SPACE SURFACE UV
    // =========================================================

    private static Vector2 GetSurfaceWorldUV(
        RoadPath road,
        Vector2 worldPosition)
    {
        RoadProfile profile =
            road.Profile;


        float pixelsPerUnit =
            profile != null
                ? Mathf.Max(
                    1f,
                    profile.SurfacePixelsPerUnit
                )
                : 32f;


        Texture2D texture =
            GetSurfaceMappingTexture(
                profile
            );


        float textureWidth =
            texture != null
                ? Mathf.Max(
                    1,
                    texture.width
                )
                : 64f;


        float textureHeight =
            texture != null
                ? Mathf.Max(
                    1,
                    texture.height
                )
                : 64f;


        float u =
            worldPosition.x *
            pixelsPerUnit /
            textureWidth;


        float v =
            worldPosition.y *
            pixelsPerUnit /
            textureHeight;


        return
            new Vector2(
                u,
                v
            );
    }


    private static Texture2D
        GetSurfaceMappingTexture(
            RoadProfile profile)
    {
        if (profile == null)
            return null;


        if (profile.SurfaceTexture != null)
        {
            return
                profile.SurfaceTexture;
        }


        if (profile.SurfaceMaterial != null &&
            profile.SurfaceMaterial.mainTexture
                is Texture2D materialTexture)
        {
            return
                materialTexture;
        }


        return null;
    }


    // =========================================================
    // TANGENT
    // =========================================================

    private static Vector2 GetTangent(
        IReadOnlyList<Vector2> points,
        int index)
    {
        if (points.Count < 2)
        {
            return
                Vector2.right;
        }


        Vector2 previous;
        Vector2 next;


        if (index <= 0)
        {
            previous =
                points[0];

            next =
                points[1];
        }
        else if (
            index >=
            points.Count - 1)
        {
            previous =
                points[
                    points.Count - 2
                ];

            next =
                points[
                    points.Count - 1
                ];
        }
        else
        {
            previous =
                points[index - 1];

            next =
                points[index + 1];
        }


        Vector2 tangent =
            next -
            previous;


        if (tangent.sqrMagnitude <=
            0.000001f)
        {
            return
                Vector2.right;
        }


        return
            tangent.normalized;
    }


    // =========================================================
    // GENERATED OBJECTS
    // =========================================================

    private static Transform EnsureGeneratedRoot(
        RoadPath road)
    {
        Transform generatedRoot =
            road.transform.Find(
                GeneratedRootName
            );


        if (generatedRoot != null)
        {
            return generatedRoot;
        }


        GameObject rootObject =
            new GameObject(
                GeneratedRootName
            );


        generatedRoot =
            rootObject.transform;


        generatedRoot.SetParent(
            road.transform,
            false
        );


        return generatedRoot;
    }


    private static GameObject FindOrCreateChild(
        Transform parent,
        string childName)
    {
        Transform existing =
            parent.Find(
                childName
            );


        GameObject childObject;


        if (existing != null)
        {
            childObject =
                existing.gameObject;
        }
        else
        {
            childObject =
                new GameObject(
                    childName
                );


            childObject.transform.SetParent(
                parent,
                false
            );
        }


        childObject.transform.localPosition =
            Vector3.zero;

        childObject.transform.localRotation =
            Quaternion.identity;

        childObject.transform.localScale =
            Vector3.one;


        childObject.layer =
            parent.gameObject.layer;


        return childObject;
    }


    private static T GetOrAdd<T>(
        GameObject target)
        where T : Component
    {
        T component =
            target.GetComponent<T>();


        if (component == null)
        {
            component =
                target.AddComponent<T>();
        }


        return component;
    }


    private static Mesh GetOrCreateMesh(
        RoadPath road,
        MeshFilter meshFilter,
        string baseName)
    {
        string expectedName =
            $"{baseName}_" +
            $"{road.GetInstanceID()}";


        Mesh mesh =
            meshFilter.sharedMesh;


        if (mesh == null ||
            mesh.name != expectedName)
        {
            mesh =
                new Mesh
                {
                    name =
                        expectedName,

                    hideFlags =
                        HideFlags
                            .HideAndDontSave
                };


            meshFilter.sharedMesh =
                mesh;
        }


        return mesh;
    }


    private static void DisableLegacyShoulder(
        Transform generatedRoot)
    {
        if (generatedRoot == null)
            return;


        Transform legacyShoulder =
            generatedRoot.Find(
                LegacyShoulderObjectName
            );


        if (legacyShoulder != null)
        {
            legacyShoulder.gameObject
                .SetActive(
                    false
                );
        }
    }


    // =========================================================
    // RENDERING
    // =========================================================

    private static void ConfigureRenderer(
        MeshRenderer renderer)
    {
        renderer.shadowCastingMode =
            ShadowCastingMode.Off;

        renderer.receiveShadows =
            false;

        renderer.lightProbeUsage =
            LightProbeUsage.Off;

        renderer.reflectionProbeUsage =
            ReflectionProbeUsage.Off;
    }


    private static void ApplySurfaceRendererSettings(
        RoadPath road,
        MeshRenderer renderer)
    {
        RoadProfile profile =
            road.Profile;


        if (profile == null)
        {
            renderer.sortingLayerName =
                "Default";

            renderer.sortingOrder =
                0;

            renderer.sharedMaterial =
                GetFallbackSurfaceMaterial();

            renderer.SetPropertyBlock(
                null
            );

            return;
        }


        renderer.sortingLayerName =
            profile.SortingLayerName;


        renderer.sortingOrder =
            profile.SortingOrder;


        if (profile.SurfaceMaterial != null)
        {
            renderer.sharedMaterial =
                profile.SurfaceMaterial;
        }
        else if (
            profile.SurfaceTexture != null)
        {
            renderer.sharedMaterial =
                GetSurfaceMaterialForTexture(
                    profile.SurfaceTexture
                );
        }
        else
        {
            renderer.sharedMaterial =
                GetFallbackSurfaceMaterial();
        }


        ApplySurfacePropertyBlock(
            road,
            renderer
        );
    }


    private static void ApplySurfacePropertyBlock(
        RoadPath road,
        MeshRenderer renderer)
    {
        RoadProfile profile =
            road.Profile;


        if (profile == null)
        {
            renderer.SetPropertyBlock(
                null
            );

            return;
        }


        surfacePropertyBlock.Clear();


        if (profile.SurfaceTexture != null)
        {
            surfacePropertyBlock.SetTexture(
                "_MainTex",
                profile.SurfaceTexture
            );
        }


        surfacePropertyBlock.SetColor(
            "_Color",
            Color.white
        );


        surfacePropertyBlock.SetFloat(
            "_SurfacePixelsPerUnit",
            profile.SurfacePixelsPerUnit
        );


        /*
         * Shader-float behöver inte hela 32-bitars seed.
         * Ett stabilt femsiffrigt värde räcker gott till
         * variationen och undviker onödig float precision.
         */
        uint unsignedSeed =
            unchecked(
                (uint)road.AppearanceSeed
            );


        float shaderSeed =
            unsignedSeed %
            1024u;


        surfacePropertyBlock.SetFloat(
            "_RoadSeed",
            shaderSeed
        );


        surfacePropertyBlock.SetFloat(
            "_EdgeErosionEnabled",
            profile.EnableEdgeErosion
                ? 1f
                : 0f
        );


        surfacePropertyBlock.SetFloat(
            "_EdgeErosionDepthPixels",
            profile.EdgeErosionDepthPixels
        );


        surfacePropertyBlock.SetFloat(
            "_EdgeErosionStrength",
            profile.EdgeErosionStrength
        );


        surfacePropertyBlock.SetFloat(
            "_HorizontalErosionScale",
            profile.HorizontalErosionScale
        );


        surfacePropertyBlock.SetFloat(
            "_InnerPinholeChance",
            profile.InnerPinholeChance
        );


        surfacePropertyBlock.SetFloat(
            "_InnerPinholeDepthPixels",
            profile.InnerPinholeDepthPixels
        );


        surfacePropertyBlock.SetFloat(
            "_ShoulderEnabled",
            profile.EnableShoulder
                ? 1f
                : 0f
        );


        surfacePropertyBlock.SetFloat(
            "_ShoulderWidthPixels",
            profile.ShoulderWidthPixels
        );


        surfacePropertyBlock.SetColor(
            "_ShoulderColor",
            profile.ShoulderColor
        );


        surfacePropertyBlock.SetFloat(
            "_UpperShoulderStrength",
            profile.UpperShoulderStrength
        );


        surfacePropertyBlock.SetFloat(
            "_LowerShoulderStrength",
            profile.LowerShoulderStrength
        );


        renderer.SetPropertyBlock(
            surfacePropertyBlock
        );
    }


    // =========================================================
    // MATERIALS
    // =========================================================

    private static Material
        GetSurfaceMaterialForTexture(
            Texture2D texture)
    {
        if (texture == null)
        {
            return
                GetFallbackSurfaceMaterial();
        }


        if (surfaceTextureMaterials.TryGetValue(
                texture,
                out Material existingMaterial) &&
            existingMaterial != null)
        {
            return
                existingMaterial;
        }


        Shader shader =
            Shader.Find(
                "RöK/Road Surface"
            );


        if (shader == null)
        {
            Debug.LogError(
                "RoadMeshBuilder kunde inte hitta shadern " +
                "'RöK/Road Surface'. Kontrollera att " +
                "RoadSurface.shader kompilerar."
            );


            return
                GetFallbackSurfaceMaterial();
        }


        Material material =
            new Material(
                shader
            )
            {
                name =
                    $"Road Surface - " +
                    $"{texture.name}",

                hideFlags =
                    HideFlags
                        .HideAndDontSave
            };


        material.SetTexture(
            "_MainTex",
            texture
        );


        material.SetColor(
            "_Color",
            Color.white
        );


        surfaceTextureMaterials[
            texture
        ] =
            material;


        return material;
    }


    private static Material
        GetFallbackSurfaceMaterial()
    {
        if (fallbackSurfaceMaterial != null)
        {
            return
                fallbackSurfaceMaterial;
        }


        Shader shader =
            Shader.Find(
                "Sprites/Default"
            );


        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Unlit/Color"
                );
        }


        if (shader == null)
        {
            Debug.LogError(
                "RoadMeshBuilder kunde inte hitta något fallback-shader."
            );

            return null;
        }


        fallbackSurfaceMaterial =
            new Material(
                shader
            )
            {
                name =
                    "Road Fallback Surface",

                hideFlags =
                    HideFlags
                        .HideAndDontSave
            };


        Color dirtColor =
            new Color(
                0.36f,
                0.22f,
                0.12f,
                1f
            );


        SetMaterialColor(
            fallbackSurfaceMaterial,
            dirtColor
        );


        return
            fallbackSurfaceMaterial;
    }


    private static void SetMaterialColor(
        Material material,
        Color color)
    {
        if (material == null)
            return;


        if (material.HasProperty(
                "_Color"))
        {
            material.SetColor(
                "_Color",
                color
            );
        }


        if (material.HasProperty(
                "_BaseColor"))
        {
            material.SetColor(
                "_BaseColor",
                color
            );
        }
    }


    // =========================================================
    // SPACE
    // =========================================================

    private static Vector3 WorldToRoadLocal(
        RoadPath road,
        Vector2 worldPoint)
    {
        Vector3 local =
            road.transform
                .InverseTransformPoint(
                    new Vector3(
                        worldPoint.x,
                        worldPoint.y,
                        road.transform.position.z
                    )
                );


        return
            new Vector3(
                local.x,
                local.y,
                0f
            );
    }


    // =========================================================
    // VISIBILITY
    // =========================================================

    private static void SetGeneratedVisible(
        RoadPath road,
        bool visible)
    {
        Transform generatedRoot =
            road.transform.Find(
                GeneratedRootName
            );


        if (generatedRoot != null)
        {
            generatedRoot.gameObject
                .SetActive(
                    visible
                );
        }
    }
}