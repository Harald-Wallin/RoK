using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Återanvändbar runtime-renderer för targetingformer.
///
/// Klassen innehåller ingen gameplaylogik.
/// Den visualiserar endast redan beräknad targetingdata.
///
/// Stöder:
/// - Self
/// - SingleTarget
/// - Point
/// - Circle
/// - Cone
/// - Line
///
/// Renderern kan även:
/// - skala hela formen
/// - rendera med eller utan outline
/// - använda en runtime-override för range
/// </summary>
public sealed class TargetShapeRenderer :
    MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private LineRenderer outlineRenderer;

    private Mesh shapeMesh;

    private Material fillMaterial;
    private Material outlineMaterial;

    private int circleSegments = 48;
    private int coneSegments = 32;

    private float pointRadius = 0.15f;
    private float selfRadius = 0.5f;
    private float outlineWidth = 0.04f;

    private bool showOutline = true;
    private bool initialized;

    private readonly List<Vector3> vertices =
        new();

    private readonly List<int> triangles =
        new();

    private readonly List<Vector3> outlinePoints =
        new();

    /// <summary>
    /// Skapar renderer-komponenter och runtime-material.
    ///
    /// Ska normalt anropas exakt en gång efter att komponenten
    /// skapats.
    /// </summary>
    public bool Initialize(
        string sortingLayerName,
        int sortingOrder,
        int circleSegmentCount,
        int coneSegmentCount,
        float configuredPointRadius,
        float configuredSelfRadius,
        float configuredOutlineWidth,
        bool renderOutline = true)
    {
        if (initialized)
            return true;

        circleSegments =
            Mathf.Clamp(
                circleSegmentCount,
                12,
                128
            );

        coneSegments =
            Mathf.Clamp(
                coneSegmentCount,
                4,
                128
            );

        pointRadius =
            Mathf.Max(
                0.01f,
                configuredPointRadius
            );

        selfRadius =
            Mathf.Max(
                0.01f,
                configuredSelfRadius
            );

        outlineWidth =
            Mathf.Max(
                0.001f,
                configuredOutlineWidth
            );

        showOutline =
            renderOutline;

        Shader spriteShader =
            Shader.Find(
                "Sprites/Default"
            );

        if (spriteShader == null)
        {
            Debug.LogError(
                "TargetShapeRenderer kunde inte hitta " +
                "shadern 'Sprites/Default'.",
                this
            );

            gameObject.SetActive(
                false
            );

            return false;
        }

        meshFilter =
            gameObject.AddComponent<
                MeshFilter>();

        meshRenderer =
            gameObject.AddComponent<
                MeshRenderer>();

        shapeMesh =
            new Mesh
            {
                name =
                    $"{gameObject.name} Mesh Runtime"
            };

        fillMaterial =
            new Material(
                spriteShader)
            {
                name =
                    $"{gameObject.name} Fill Material Runtime"
            };

        meshFilter.sharedMesh =
            shapeMesh;

        meshRenderer.material =
            fillMaterial;

        meshRenderer.sortingLayerName =
            sortingLayerName;

        meshRenderer.sortingOrder =
            sortingOrder;

        if (showOutline)
        {
            outlineRenderer =
                gameObject.AddComponent<
                    LineRenderer>();

            outlineMaterial =
                new Material(
                    spriteShader)
                {
                    name =
                        $"{gameObject.name} Outline Material Runtime"
                };

            outlineRenderer.material =
                outlineMaterial;

            outlineRenderer.sortingLayerName =
                sortingLayerName;

            outlineRenderer.sortingOrder =
                sortingOrder + 1;

            outlineRenderer.useWorldSpace =
                false;

            outlineRenderer.loop =
                true;

            outlineRenderer.numCapVertices =
                4;

            outlineRenderer.numCornerVertices =
                4;

            outlineRenderer.startWidth =
                outlineWidth;

            outlineRenderer.endWidth =
                outlineWidth;
        }

        initialized =
            true;

        Clear();

        return true;
    }

    /// <summary>
    /// Renderar ett färdigt TargetingResult.
    ///
    /// EffectiveRange används så att charge-baserad runtime-range
    /// även syns korrekt för exempelvis Cone och Line.
    /// </summary>
    public bool Render(
        TargetingResult targeting,
        Color fillColor,
        Color outlineColor)
    {
        return Render(
            targeting,
            fillColor,
            outlineColor,
            1f
        );
    }

    /// <summary>
    /// Renderar ett färdigt TargetingResult med en skalfaktor.
    ///
    /// shapeScale:
    /// - 0 = nästan ingen form
    /// - 0.5 = halva formen
    /// - 1 = hela formen
    /// </summary>
    public bool Render(
    TargetingResult targeting,
    Color fillColor,
    Color outlineColor,
    float shapeScale,
    Vector2 terminalOffset = default)
    {
        if (targeting == null ||
            targeting.Settings == null)
        {
            Clear();

            return false;
        }

        return Render(
            targeting.Settings,
            targeting.Origin,
            targeting.TargetPoint,
            targeting.Direction,
            targeting.Distance,
            targeting.PrimaryTarget,
            fillColor,
            outlineColor,
            shapeScale,
            targeting.EffectiveRange,
            terminalOffset
        );
    }

    /// <summary>
    /// Renderar en targetingform utan ett TargetingResult.
    ///
    /// rangeOverride används exempelvis för att visa abilityns
    /// maximala range samtidigt som den verkliga chargade formen
    /// använder en lägre EffectiveRange.
    ///
    /// Ett negativt rangeOverride innebär att settings.Range
    /// används.
    /// </summary>
    public bool Render(
        AbilityTargetingSettings settings,
        Vector2 origin,
        Vector2 targetPoint,
        Vector2 direction,
        float distance,
        GameObject primaryTarget,
        Color fillColor,
        Color outlineColor,
        float shapeScale = 1f,
        float rangeOverride = -1f,
        Vector2 terminalOffset = default)
    {
        if (!initialized ||
            settings == null)
        {
            Clear();

            return false;
        }

        shapeScale =
            Mathf.Clamp01(
                shapeScale
            );

        float effectiveRange =
            rangeOverride >= 0f
                ? rangeOverride
                : settings.Range;

        effectiveRange =
            Mathf.Max(
                0f,
                effectiveRange
            );

        ClearGeometry();

        bool shapeBuilt =
            BuildShape(
            settings,
            origin,
            targetPoint,
            direction,
            distance,
            primaryTarget,
            shapeScale,
            effectiveRange,
            terminalOffset
                );

        if (!shapeBuilt)
        {
            Clear();

            return false;
        }

        ApplyGeometry();

        ApplyColors(
            fillColor,
            outlineColor
        );

        gameObject.SetActive(
            true
        );

        return true;
    }

    public void Clear()
    {
        if (shapeMesh != null)
        {
            shapeMesh.Clear();
        }

        if (outlineRenderer != null)
        {
            outlineRenderer.positionCount =
                0;
        }

        ClearGeometry();

        if (gameObject != null)
        {
            gameObject.SetActive(
                false
            );
        }
    }

    private bool BuildShape(
        AbilityTargetingSettings settings,
        Vector2 origin,
        Vector2 targetPoint,
        Vector2 direction,
        float distance,
        GameObject primaryTarget,
        float shapeScale,
        float effectiveRange,
        Vector2 terminalOffset)
    {
        switch (settings.TargetingMode)
        {
            case TargetingMode.Self:
                BuildCircle(
                    origin,
                    selfRadius *
                    shapeScale
                );

                return true;

            case TargetingMode.SingleTarget:
                BuildSingleTarget(
                    targetPoint,
                    primaryTarget,
                    shapeScale
                );

                return true;

            case TargetingMode.Point:
                BuildCircle(
                    targetPoint,
                    pointRadius *
                    shapeScale
                );

                return true;

            case TargetingMode.Circle:
                BuildCircle(
                    targetPoint,
                    settings.Radius *
                    shapeScale
                );

                return true;

            case TargetingMode.Cone:
                BuildCone(
                    origin,
                    direction,
                    effectiveRange *
                    shapeScale,
                    settings.ConeAngle
                );

                return true;

            case TargetingMode.Line:
                {
                    float resolvedDistance =
                        distance > 0.001f
                            ? distance
                            : effectiveRange;

                    /*
                     * En Line får aldrig renderas längre än dess
                     * aktuella EffectiveRange.
                     */
                    resolvedDistance =
                        Mathf.Min(
                            resolvedDistance,
                            effectiveRange
                        );

                    BuildLine(
                        origin,
                        direction,
                        resolvedDistance *
                        shapeScale,
                        settings.LineWidth,
                        terminalOffset
                    );

                    return true;
                }

            default:
                return false;
        }
    }

    private void BuildSingleTarget(
        Vector2 targetPoint,
        GameObject primaryTarget,
        float shapeScale)
    {
        if (primaryTarget == null ||
            !TryGetTargetBounds(
                primaryTarget,
                out Bounds bounds))
        {
            BuildCircle(
                targetPoint,
                pointRadius *
                shapeScale
            );

            return;
        }

        float radius =
            Mathf.Max(
                bounds.extents.x,
                bounds.extents.y,
                pointRadius
            );

        BuildCircle(
            bounds.center,
            radius *
            1.1f *
            shapeScale
        );
    }

    private void BuildCircle(
        Vector2 worldCenter,
        float radius)
    {
        radius =
            Mathf.Max(
                0.001f,
                radius
            );

        Vector3 localCenter =
            WorldToLocal(
                worldCenter
            );

        vertices.Add(
            localCenter
        );

        for (int i = 0;
             i < circleSegments;
             i++)
        {
            float angle =
                i /
                (float)circleSegments *
                Mathf.PI *
                2f;

            Vector2 worldPoint =
                worldCenter +
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                ) *
                radius;

            Vector3 localPoint =
                WorldToLocal(
                    worldPoint
                );

            vertices.Add(
                localPoint
            );

            if (showOutline)
            {
                outlinePoints.Add(
                    localPoint
                );
            }
        }

        for (int i = 0;
             i < circleSegments;
             i++)
        {
            int current =
                i + 1;

            int next =
                (
                    (i + 1) %
                    circleSegments
                ) +
                1;

            triangles.Add(0);
            triangles.Add(current);
            triangles.Add(next);
        }
    }

    private void BuildCone(
        Vector2 worldOrigin,
        Vector2 direction,
        float range,
        float totalAngle)
    {
        range =
            Mathf.Max(
                0.001f,
                range
            );

        totalAngle =
            Mathf.Clamp(
                totalAngle,
                0f,
                360f
            );

        if (totalAngle >= 359.9f)
        {
            BuildCircle(
                worldOrigin,
                range
            );

            return;
        }

        direction =
            GetSafeDirection(
                direction
            );

        float centerAngle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) *
            Mathf.Rad2Deg;

        float startAngle =
            centerAngle -
            totalAngle *
            0.5f;

        Vector3 localOrigin =
            WorldToLocal(
                worldOrigin
            );

        vertices.Add(
            localOrigin
        );

        if (showOutline)
        {
            outlinePoints.Add(
                localOrigin
            );
        }

        for (int i = 0;
             i <= coneSegments;
             i++)
        {
            float progress =
                i /
                (float)coneSegments;

            float angleDegrees =
                Mathf.Lerp(
                    startAngle,
                    startAngle +
                    totalAngle,
                    progress
                );

            float angleRadians =
                angleDegrees *
                Mathf.Deg2Rad;

            Vector2 worldPoint =
                worldOrigin +
                new Vector2(
                    Mathf.Cos(
                        angleRadians),
                    Mathf.Sin(
                        angleRadians)
                ) *
                range;

            Vector3 localPoint =
                WorldToLocal(
                    worldPoint
                );

            vertices.Add(
                localPoint
            );

            if (showOutline)
            {
                outlinePoints.Add(
                    localPoint
                );
            }
        }

        for (int i = 0;
             i < coneSegments;
             i++)
        {
            triangles.Add(0);
            triangles.Add(i + 1);
            triangles.Add(i + 2);
        }
    }

    private void BuildLine(
    Vector2 worldOrigin,
    Vector2 direction,
    float length,
    float width,
    Vector2 terminalOffset)
    {
        length =
            Mathf.Max(
                0.001f,
                length
            );

        width =
            Mathf.Max(
                0.001f,
                width
            );

        direction =
            GetSafeDirection(
                direction
            );

        float halfWidth =
            width * 0.5f;

        /*
         * Starten använder abilityns fasta aim-riktning.
         */
        Vector2 startPerpendicular =
            new Vector2(
                -direction.y,
                direction.x
            );

        /*
         * Endast linjens terminal flyttas av charge-effekten.
         */
        Vector2 worldEnd =
            worldOrigin +
            direction * length +
            terminalOffset;

        /*
         * När terminalen skakar åt sidan förändras den visuella
         * centerlinjen en liten aning.
         *
         * Ändkantens perpendicular räknas därför från den faktiska
         * linjen mellan origin och terminal. Det håller fillens
         * ytterände symmetrisk.
         */
        Vector2 visualDirection =
            worldEnd -
            worldOrigin;

        if (visualDirection.sqrMagnitude <=
            0.0001f)
        {
            visualDirection =
                direction;
        }
        else
        {
            visualDirection.Normalize();
        }

        Vector2 endPerpendicular =
            new Vector2(
                -visualDirection.y,
                visualDirection.x
            );

        Vector2 point0 =
            worldOrigin +
            startPerpendicular *
            halfWidth;

        Vector2 point1 =
            worldEnd +
            endPerpendicular *
            halfWidth;

        Vector2 point2 =
            worldEnd -
            endPerpendicular *
            halfWidth;

        Vector2 point3 =
            worldOrigin -
            startPerpendicular *
            halfWidth;

        Vector3 local0 =
            WorldToLocal(
                point0
            );

        Vector3 local1 =
            WorldToLocal(
                point1
            );

        Vector3 local2 =
            WorldToLocal(
                point2
            );

        Vector3 local3 =
            WorldToLocal(
                point3
            );

        vertices.Add(
            local0
        );

        vertices.Add(
            local1
        );

        vertices.Add(
            local2
        );

        vertices.Add(
            local3
        );

        triangles.Add(0);
        triangles.Add(1);
        triangles.Add(2);

        triangles.Add(0);
        triangles.Add(2);
        triangles.Add(3);

        if (!showOutline)
            return;

        outlinePoints.Add(
            local0
        );

        outlinePoints.Add(
            local1
        );

        outlinePoints.Add(
            local2
        );

        outlinePoints.Add(
            local3
        );
    }

    private void ApplyGeometry()
    {
        shapeMesh.Clear();

        shapeMesh.SetVertices(
            vertices
        );

        shapeMesh.SetTriangles(
            triangles,
            0
        );

        shapeMesh.RecalculateBounds();

        if (outlineRenderer == null)
            return;

        outlineRenderer.positionCount =
            outlinePoints.Count;

        for (int i = 0;
             i < outlinePoints.Count;
             i++)
        {
            outlineRenderer.SetPosition(
                i,
                outlinePoints[i]
            );
        }

        outlineRenderer.loop =
            outlinePoints.Count >=
            3;
    }

    private void ApplyColors(
        Color fillColor,
        Color outlineColor)
    {
        if (fillMaterial != null)
        {
            fillMaterial.color =
                fillColor;
        }

        if (outlineMaterial != null)
        {
            outlineMaterial.color =
                outlineColor;
        }

        if (outlineRenderer == null)
            return;

        outlineRenderer.startColor =
            outlineColor;

        outlineRenderer.endColor =
            outlineColor;

        outlineRenderer.startWidth =
            outlineWidth;

        outlineRenderer.endWidth =
            outlineWidth;
    }

    private void ClearGeometry()
    {
        vertices.Clear();
        triangles.Clear();
        outlinePoints.Clear();
    }

    private Vector3 WorldToLocal(
        Vector2 worldPoint)
    {
        return
            transform.InverseTransformPoint(
                new Vector3(
                    worldPoint.x,
                    worldPoint.y,
                    0f
                )
            );
    }

    private static Vector2 GetSafeDirection(
        Vector2 direction)
    {
        if (direction.sqrMagnitude <=
            0.0001f)
        {
            return Vector2.down;
        }

        return direction.normalized;
    }

    private static bool TryGetTargetBounds(
        GameObject target,
        out Bounds bounds)
    {
        bounds =
            new Bounds();

        if (target == null)
            return false;

        SpriteRenderer[] spriteRenderers =
            target.GetComponentsInChildren<
                SpriteRenderer>(
                true
            );

        bool hasBounds =
            false;

        for (int i = 0;
             i < spriteRenderers.Length;
             i++)
        {
            SpriteRenderer spriteRenderer =
                spriteRenderers[i];

            if (spriteRenderer == null ||
                spriteRenderer.sprite == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds =
                    spriteRenderer.bounds;

                hasBounds =
                    true;
            }
            else
            {
                bounds.Encapsulate(
                    spriteRenderer.bounds
                );
            }
        }

        if (hasBounds)
            return true;

        Collider2D[] colliders =
            target.GetComponentsInChildren<
                Collider2D>(
                true
            );

        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            Collider2D targetCollider =
                colliders[i];

            if (targetCollider == null)
                continue;

            if (!hasBounds)
            {
                bounds =
                    targetCollider.bounds;

                hasBounds =
                    true;
            }
            else
            {
                bounds.Encapsulate(
                    targetCollider.bounds
                );
            }
        }

        return hasBounds;
    }

    private void OnDestroy()
    {
        DestroyRuntimeObject(
            shapeMesh
        );

        DestroyRuntimeObject(
            fillMaterial
        );

        DestroyRuntimeObject(
            outlineMaterial
        );
    }

    private static void DestroyRuntimeObject(
        Object runtimeObject)
    {
        if (runtimeObject != null)
        {
            Destroy(
                runtimeObject
            );
        }
    }
}