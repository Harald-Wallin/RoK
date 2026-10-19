using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Delad mesh-builder för små road-edge-decorationer.
///
/// Används av bland annat:
/// - RoadSplatterBuilder
/// - RoadEdgeDoodadBuilder
///
/// Många sprites kombineras till ett enda genererat
/// GameObject + MeshRenderer.
///
/// Om sprites kommer från samma texture/spritesheet kan
/// allt dessutom använda samma material/submesh.
/// </summary>
public static class RoadEdgeDecorationMeshBuilder
{
    private const string GeneratedRootName =
        "Generated";


    private static readonly Dictionary<
        Texture2D,
        Material> materialCache =
            new();


    // =========================================================
    // SETTINGS
    // =========================================================

    public struct Settings
    {
        public bool enabled;

        public string objectName;
        public string meshNamePrefix;

        public int seedSalt;

        public Sprite[] sprites;

        public float density;

        public float minEdgeOffset;
        public float maxEdgeOffset;

        public Vector2 scaleRange;

        public float upperEdgeOffsetScale;
        public float lowerEdgeOffsetScale;
        public float upperEdgeScale;

        public bool randomQuarterTurns;
        public bool randomFlipX;
        public bool randomFlipY;

        public bool balancedSides;

        public int sortingOrderOffset;
        public bool suppressAtJunctions;
    }


    // =========================================================
    // PUBLIC
    // =========================================================

    public static void Rebuild(
        RoadPath road,
        Settings settings)
    {
        if (road == null)
            return;


        Transform generatedRoot =
            road.transform.Find(
                GeneratedRootName
            );


        if (generatedRoot == null)
            return;


        if (string.IsNullOrWhiteSpace(
                settings.objectName))
        {
            return;
        }


        GameObject decorationObject =
            FindOrCreateChild(
                generatedRoot,
                settings.objectName
            );


        RoadProfile profile =
            road.Profile;


        if (profile == null ||
            !settings.enabled ||
            settings.density <= 0f)
        {
            decorationObject.SetActive(
                false
            );

            return;
        }


        List<Sprite> validSprites =
            GetValidSprites(
                settings.sprites
            );


        if (validSprites.Count == 0)
        {
            decorationObject.SetActive(
                false
            );

            return;
        }


        List<Vector2> centerLine =
            new();


        RoadMeshBuilder
            .BuildSmoothCenterLine(
                road,
                centerLine
            );


        if (centerLine.Count < 2)
        {
            decorationObject.SetActive(
                false
            );

            return;
        }


        MeshFilter meshFilter =
            GetOrAdd<MeshFilter>(
                decorationObject
            );


        MeshRenderer meshRenderer =
            GetOrAdd<MeshRenderer>(
                decorationObject
            );


        ConfigureRenderer(
            meshRenderer
        );


        Mesh mesh =
            GetOrCreateMesh(
                road,
                meshFilter,
                settings.meshNamePrefix
            );


        bool built =
            BuildDecorationMesh(
                road,
                profile,
                centerLine,
                validSprites,
                settings,
                mesh,
                meshRenderer
            );


        decorationObject.SetActive(
            built
        );


        if (built)
        {
            meshRenderer.sortingLayerName =
                profile.SortingLayerName;


            meshRenderer.sortingOrder =
                profile.SortingOrder +
                settings.sortingOrderOffset;
        }
    }


    // =========================================================
    // BUILD
    // =========================================================

    private static bool BuildDecorationMesh(
        RoadPath road,
        RoadProfile profile,
        IReadOnlyList<Vector2> centerLine,
        IReadOnlyList<Sprite> sprites,
        Settings settings,
        Mesh mesh,
        MeshRenderer renderer)
    {
        List<float> distances =
            BuildDistanceTable(
                centerLine
            );


        if (distances.Count == 0)
            return false;


        float totalLength =
            distances[
                distances.Count - 1
            ];


        if (totalLength <= 0.001f)
            return false;


        int decorationCount =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    totalLength *
                    settings.density
                )
            );


        System.Random random =
            new System.Random(
                road.AppearanceSeed ^
                settings.seedSalt
            );


        List<Vector3> vertices =
            new(
                decorationCount * 4
            );


        List<Vector2> uvs =
            new(
                decorationCount * 4
            );


        List<Vector3> normals =
            new(
                decorationCount * 4
            );


        Dictionary<Texture2D, int>
            textureIndices =
                new();


        List<Texture2D> textures =
            new();


        List<List<int>>
            trianglesByTexture =
                new();


        float interval =
            totalLength /
            decorationCount;


        bool startPositiveSide =
            random.NextDouble() <
            0.5;

        RoadNetwork roadNetwork =
            road.GetComponentInParent<
                RoadNetwork
         >();


        for (int i = 0;
             i < decorationCount;
             i++)
        {
            float jitter =
                RandomRange(
                    random,
                    -0.35f,
                    0.35f
                ) *
                interval;


            float targetDistance =
                (
                    i +
                    0.5f
                ) *
                interval +
                jitter;


            targetDistance =
                Mathf.Clamp(
                    targetDistance,
                    0f,
                    totalLength
                );


            int segmentIndex =
                FindSegmentForDistance(
                    distances,
                    targetDistance
                );


            int nextIndex =
                Mathf.Min(
                    segmentIndex + 1,
                    centerLine.Count - 1
                );


            float segmentStartDistance =
                distances[
                    segmentIndex
                ];


            float segmentEndDistance =
                distances[
                    nextIndex
                ];


            float segmentLength =
                segmentEndDistance -
                segmentStartDistance;


            float t =
                segmentLength >
                0.000001f
                    ? (
                        targetDistance -
                        segmentStartDistance
                    ) /
                    segmentLength
                    : 0f;


            Vector2 first =
                centerLine[
                    segmentIndex
                ];


            Vector2 second =
                centerLine[
                    nextIndex
                ];


            Vector2 center =
                Vector2.Lerp(
                    first,
                    second,
                    t
                );


            Vector2 tangent =
                second -
                first;


            if (tangent.sqrMagnitude <=
                0.000001f)
            {
                continue;
            }


            tangent.Normalize();


            Vector2 normal =
                new Vector2(
                    -tangent.y,
                    tangent.x
                );


            float side;


            if (settings.balancedSides)
            {
                bool positiveSide =
                    (
                        i %
                        2 ==
                        0
                    ) ==
                    startPositiveSide;


                side =
                    positiveSide
                        ? 1f
                        : -1f;
            }
            else
            {
                side =
                    random.NextDouble() <
                    0.5
                        ? 1f
                        : -1f;
            }


            float halfWidth =
                road.GetWidthForDirection(
                    tangent
                ) *
                0.5f;


            Vector2 edgePoint =
                center +
                normal *
                halfWidth *
                side;


            Vector2 outward =
                normal *
                side;


            float edgeOffset =
                RandomRange(
                    random,
                    settings.minEdgeOffset,
                    settings.maxEdgeOffset
                );


            float horizontalness =
                Mathf.Abs(
                    Vector2.Dot(
                        tangent,
                        Vector2.right
                    )
                );


            float perspectiveStrength =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    horizontalness
                );


            bool isUpperEdge =
                outward.y >
                0.0001f;


            float perspectiveOffsetScale;


            if (isUpperEdge)
            {
                perspectiveOffsetScale =
                    Mathf.Lerp(
                        1f,
                        settings.upperEdgeOffsetScale,
                        perspectiveStrength
                    );
            }
            else
            {
                perspectiveOffsetScale =
                    Mathf.Lerp(
                        1f,
                        settings.lowerEdgeOffsetScale,
                        perspectiveStrength
                    );
            }


            edgeOffset *=
                perspectiveOffsetScale;


            Vector2 position =
                edgePoint +
                outward *
                edgeOffset;

            if (settings.suppressAtJunctions && roadNetwork != null &&  roadNetwork.IsDecorationSuppressed(road,position)){
                continue;
            }


            Sprite sprite =
                sprites[
                    random.Next(
                        0,
                        sprites.Count
                    )
                ];


            if (sprite == null ||
                sprite.texture == null)
            {
                continue;
            }


            float scale =
                RandomRange(
                    random,
                    settings.scaleRange.x,
                    settings.scaleRange.y
                );


            if (isUpperEdge)
            {
                float upperScaleMultiplier =
                    Mathf.Lerp(
                        1f,
                        settings.upperEdgeScale,
                        perspectiveStrength
                    );


                scale *=
                    upperScaleMultiplier;
            }


            int quarterTurns =
                settings.randomQuarterTurns
                    ? random.Next(
                        0,
                        4
                    )
                    : 0;


            bool flipX =
                settings.randomFlipX &&
                random.NextDouble() <
                0.5;


            bool flipY =
                settings.randomFlipY &&
                random.NextDouble() <
                0.5;


            AddSpriteQuad(
                road,
                sprite,
                position,
                scale,
                quarterTurns,
                flipX,
                flipY,
                vertices,
                uvs,
                normals,
                textureIndices,
                textures,
                trianglesByTexture
            );
        }


        if (vertices.Count == 0 ||
            textures.Count == 0)
        {
            mesh.Clear();

            return false;
        }


        mesh.Clear();


        mesh.SetVertices(
            vertices
        );


        mesh.SetUVs(
            0,
            uvs
        );


        mesh.SetNormals(
            normals
        );


        mesh.subMeshCount =
            textures.Count;


        for (int i = 0;
             i < trianglesByTexture.Count;
             i++)
        {
            mesh.SetTriangles(
                trianglesByTexture[i],
                i,
                false
            );
        }


        mesh.RecalculateBounds();


        Material[] materials =
            new Material[
                textures.Count
            ];


        for (int i = 0;
             i < textures.Count;
             i++)
        {
            materials[i] =
                GetMaterialForTexture(
                    textures[i]
                );
        }


        renderer.sharedMaterials =
            materials;


        return true;
    }


    // =========================================================
    // SPRITE QUAD
    // =========================================================

    private static void AddSpriteQuad(
        RoadPath road,
        Sprite sprite,
        Vector2 worldPosition,
        float scale,
        int quarterTurns,
        bool flipX,
        bool flipY,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<Vector3> normals,
        Dictionary<Texture2D, int> textureIndices,
        List<Texture2D> textures,
        List<List<int>> trianglesByTexture)
    {
        Texture2D texture =
            sprite.texture;


        if (!textureIndices.TryGetValue(
                texture,
                out int materialIndex))
        {
            materialIndex =
                textures.Count;


            textureIndices.Add(
                texture,
                materialIndex
            );


            textures.Add(
                texture
            );


            trianglesByTexture.Add(
                new List<int>()
            );
        }


        float pixelsPerUnit =
            Mathf.Max(
                0.01f,
                sprite.pixelsPerUnit
            );


        Vector2 size =
            sprite.rect.size /
            pixelsPerUnit *
            scale;


        /*
         * Till skillnad från gamla SplatterBuilder respekterar
         * den här gemensamma buildern Sprite-pivoten.
         *
         * För exempelvis gräs rekommenderas Bottom Center.
         */
        Vector2 pivot =
            sprite.pivot /
            pixelsPerUnit *
            scale;


        Vector2 bottomLeft =
            new Vector2(
                -pivot.x,
                -pivot.y
            );


        Vector2 topLeft =
            new Vector2(
                -pivot.x,
                size.y -
                pivot.y
            );


        Vector2 topRight =
            new Vector2(
                size.x -
                pivot.x,
                size.y -
                pivot.y
            );


        Vector2 bottomRight =
            new Vector2(
                size.x -
                pivot.x,
                -pivot.y
            );


        bottomLeft =
            RotateQuarterTurns(
                bottomLeft,
                quarterTurns
            );


        topLeft =
            RotateQuarterTurns(
                topLeft,
                quarterTurns
            );


        topRight =
            RotateQuarterTurns(
                topRight,
                quarterTurns
            );


        bottomRight =
            RotateQuarterTurns(
                bottomRight,
                quarterTurns
            );


        int baseVertex =
            vertices.Count;


        vertices.Add(
            WorldToRoadLocal(
                road,
                worldPosition +
                bottomLeft
            )
        );


        vertices.Add(
            WorldToRoadLocal(
                road,
                worldPosition +
                topLeft
            )
        );


        vertices.Add(
            WorldToRoadLocal(
                road,
                worldPosition +
                topRight
            )
        );


        vertices.Add(
            WorldToRoadLocal(
                road,
                worldPosition +
                bottomRight
            )
        );


        Rect textureRect =
            sprite.textureRect;


        float uMin =
            textureRect.xMin /
            texture.width;


        float uMax =
            textureRect.xMax /
            texture.width;


        float vMin =
            textureRect.yMin /
            texture.height;


        float vMax =
            textureRect.yMax /
            texture.height;


        if (flipX)
        {
            (
                uMin,
                uMax
            ) =
            (
                uMax,
                uMin
            );
        }


        if (flipY)
        {
            (
                vMin,
                vMax
            ) =
            (
                vMax,
                vMin
            );
        }


        uvs.Add(
            new Vector2(
                uMin,
                vMin
            )
        );


        uvs.Add(
            new Vector2(
                uMin,
                vMax
            )
        );


        uvs.Add(
            new Vector2(
                uMax,
                vMax
            )
        );


        uvs.Add(
            new Vector2(
                uMax,
                vMin
            )
        );


        normals.Add(
            Vector3.back
        );

        normals.Add(
            Vector3.back
        );

        normals.Add(
            Vector3.back
        );

        normals.Add(
            Vector3.back
        );


        List<int> triangles =
            trianglesByTexture[
                materialIndex
            ];


        triangles.Add(
            baseVertex
        );

        triangles.Add(
            baseVertex + 1
        );

        triangles.Add(
            baseVertex + 3
        );


        triangles.Add(
            baseVertex + 3
        );

        triangles.Add(
            baseVertex + 1
        );

        triangles.Add(
            baseVertex + 2
        );
    }


    private static Vector2 RotateQuarterTurns(
        Vector2 value,
        int quarterTurns)
    {
        int turns =
            (
                quarterTurns %
                4 +
                4
            ) %
            4;


        switch (turns)
        {
            default:
            case 0:
                return value;

            case 1:
                return new Vector2(
                    -value.y,
                    value.x
                );

            case 2:
                return -value;

            case 3:
                return new Vector2(
                    value.y,
                    -value.x
                );
        }
    }


    // =========================================================
    // DISTANCE
    // =========================================================

    private static List<float>
        BuildDistanceTable(
            IReadOnlyList<Vector2> points)
    {
        List<float> distances =
            new(
                points.Count
            );


        if (points.Count == 0)
            return distances;


        float total =
            0f;


        distances.Add(
            0f
        );


        for (int i = 1;
             i < points.Count;
             i++)
        {
            total +=
                Vector2.Distance(
                    points[i - 1],
                    points[i]
                );


            distances.Add(
                total
            );
        }


        return distances;
    }


    private static int
        FindSegmentForDistance(
            IReadOnlyList<float> distances,
            float targetDistance)
    {
        int segmentCount =
            distances.Count - 1;


        if (segmentCount <= 1)
            return 0;


        int low =
            0;


        int high =
            segmentCount - 1;


        while (low <= high)
        {
            int mid =
                (
                    low +
                    high
                ) /
                2;


            float start =
                distances[mid];


            float end =
                distances[mid + 1];


            if (targetDistance <
                start)
            {
                high =
                    mid - 1;
            }
            else if (
                targetDistance >
                end)
            {
                low =
                    mid + 1;
            }
            else
            {
                return mid;
            }
        }


        return
            Mathf.Clamp(
                low,
                0,
                segmentCount - 1
            );
    }


    // =========================================================
    // SPRITES
    // =========================================================

    private static List<Sprite>
        GetValidSprites(
            Sprite[] source)
    {
        List<Sprite> result =
            new();


        if (source == null)
            return result;


        for (int i = 0;
             i < source.Length;
             i++)
        {
            Sprite sprite =
                source[i];


            if (sprite == null ||
                sprite.texture == null)
            {
                continue;
            }


            result.Add(
                sprite
            );
        }


        return result;
    }


    // =========================================================
    // OBJECT / MESH
    // =========================================================

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
        string meshNamePrefix)
    {
        string safePrefix =
            string.IsNullOrWhiteSpace(
                meshNamePrefix
            )
                ? "RoadDecorationMesh"
                : meshNamePrefix;


        string expectedName =
            $"{safePrefix}_" +
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


    // =========================================================
    // MATERIALS
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


    private static Material
        GetMaterialForTexture(
            Texture2D texture)
    {
        if (texture == null)
            return null;


        if (materialCache.TryGetValue(
                texture,
                out Material material) &&
            material != null)
        {
            return material;
        }


        Shader shader =
            Shader.Find(
                "Sprites/Default"
            );


        if (shader == null)
        {
            Debug.LogError(
                "RoadEdgeDecorationMeshBuilder kunde inte hitta " +
                "Sprites/Default."
            );

            return null;
        }


        material =
            new Material(
                shader
            )
            {
                name =
                    $"Road Decoration - " +
                    $"{texture.name}",

                hideFlags =
                    HideFlags
                        .HideAndDontSave
            };


        if (material.HasProperty(
                "_MainTex"))
        {
            material.SetTexture(
                "_MainTex",
                texture
            );
        }


        if (material.HasProperty(
                "_Color"))
        {
            material.SetColor(
                "_Color",
                Color.white
            );
        }


        materialCache[
            texture
        ] =
            material;


        return material;
    }


    // =========================================================
    // HELPERS
    // =========================================================

    private static float RandomRange(
        System.Random random,
        float minimum,
        float maximum)
    {
        if (maximum <= minimum)
            return minimum;


        float t =
            (float)
            random.NextDouble();


        return
            Mathf.Lerp(
                minimum,
                maximum,
                t
            );
    }


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
}
